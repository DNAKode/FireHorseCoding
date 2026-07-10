using Microsoft.Data.Sqlite;

namespace Gneiss.Cell.Internal;

internal enum EntryStatus
{
    Accepted,
    Defeated,
    Contested,
    NotAdmitted,
}

internal sealed record EntryResult(EntryStatus Status, string? DefeaterAid, string? DefeatReason, bool AutoAdmitted);

internal sealed record FoldOutcome(
    ResolvedContext Ctx,
    IReadOnlyDictionary<string, AssrtRow> VisibleAssrt,
    IReadOnlyDictionary<string, EntryResult> Entries,
    IReadOnlyList<ContestedGroup> AllContestedGroups,
    IReadOnlyDictionary<string, bool> Stale,
    IReadOnlyDictionary<string, ResolvedPredicate> PredicatesUsed);

/// <summary>
/// The order-fixed left fold over the ledger, per CONTRACT.md section 3. L0: full recompute per Ask.
/// No search, no fixpoint iteration: decision effectiveness is one DESCENDING-tx pass (step 3);
/// admission/defeat/conflict are single passes over the resulting maps (steps 4-6); staleness is one
/// DFS per accepted aid over already-computed statuses (step 7). If any policy required iterating
/// steps 3-7 to convergence, that would be the E1 kill signal (CONTRACT.md, ROADMAP.md E1) -- it does
/// not occur here because I6 makes the decision-targets graph acyclic through tx order.
/// </summary>
internal static class BeliefFold
{
    // ---- context / predicate resolution -----------------------------------------------------

    // DIVERGENCE: CONTRACT.md section 3 step 1 says named-context declarations are found "with tx ≤
    // (declared defCut or ask-time highwater)", which is circular for the very first lookup of the
    // declaration itself (its own DefCut is a field of the row we are trying to find). THE-PAGE's
    // bootCtx (fixed, non-self-referencing, dataCut=defCut=highwater-at-ask) is the metacircular
    // bottom that resolves this in the general theory; v0 does not implement general ctx-of-ctx
    // resolution. We take the pragmatic reading: bootCtx IS the lookup context for finding a named
    // declaration, so the declaration search itself is always bounded by ask-time HighWater (not by
    // the not-yet-known DefCut of the declaration being resolved). This is sufficient for v0's single
    // declare-once-per-context fixtures; it would need revisiting if a context is redeclared and a
    // caller needs to pin resolution to an outer DefCut narrower than "now".
    internal static ResolvedContext ResolveContext(SqliteConnection conn, string name, long highWater)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT aid, val FROM assrt WHERE pred = 'gneiss.context' AND subj = $subj AND tx <= $hw ORDER BY tx DESC, aid DESC LIMIT 1";
        cmd.Parameters.AddWithValue("$subj", name);
        cmd.Parameters.AddWithValue("$hw", highWater);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            throw new GneissException("UnknownContext", $"No declared context named '{name}' visible at tx <= {highWater}.");
        }
        var declAid = reader.GetString(0);
        var decl = DeclarationCodec.DecodeContextDecl(reader.GetString(1));

        var dataCut = decl.DataCut ?? highWater;
        var defCut = decl.DefCut ?? highWater;

        var ctxHash = Hashing.Sha256Hex(CanonicalJsonWriter.ToJson(w => w.Obj(o => o
            .Field("name", name)
            .FieldLong("dataCut", dataCut)
            .FieldLong("defCut", defCut)
            .Field("admit", decl.Admit)
            .FieldNullableInt("admitThresholdBp", decl.AdmitThresholdBp)
            .Field("confPolicy", decl.ConfPolicy))));

        return new ResolvedContext(name, declAid, dataCut, defCut, decl.Admit, decl.AdmitThresholdBp, decl.ConfPolicy, ctxHash);
    }

    internal static ResolvedPredicate ResolvePredicate(SqliteConnection conn, long defCut, string predName, Dictionary<string, ResolvedPredicate> cache)
    {
        if (cache.TryGetValue(predName, out var cached))
        {
            return cached;
        }

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT aid, val FROM assrt WHERE pred = 'gneiss.predicate' AND subj = $subj AND tx <= $defcut ORDER BY tx DESC, aid DESC LIMIT 1";
        cmd.Parameters.AddWithValue("$subj", predName);
        cmd.Parameters.AddWithValue("$defcut", defCut);
        using var reader = cmd.ExecuteReader();

        ResolvedPredicate result;
        if (reader.Read())
        {
            var declAid = reader.GetString(0);
            var decl = DeclarationCodec.DecodePredicateDecl(reader.GetString(1));
            result = new ResolvedPredicate(predName, declAid, decl.Comparator, decl.TolAbs, decl.TolRel, decl.StopRung, decl.InstantSampled, decl.SourcePrecedence ?? Array.Empty<string>());
        }
        else
        {
            result = new ResolvedPredicate(predName, null, "exact", null, null, 6, false, Array.Empty<string>());
        }

        cache[predName] = result;
        return result;
    }

    // ---- the fold -----------------------------------------------------------------------------

    internal static FoldOutcome Compute(SqliteConnection conn, ResolvedContext ctx)
    {
        var visible = LoadVisible(conn, ctx.DataCut);
        var decisions = LoadDecisions(conn, visible);
        var predCache = new Dictionary<string, ResolvedPredicate>();

        // Step 3: decision effectiveness, single DESCENDING-tx pass.
        var decisionsOrdered = decisions.Values
            .OrderByDescending(d => d.Tx, Comparer<long>.Default)
            .ThenByDescending(d => d.Aid, StringComparer.Ordinal)
            .ToList();

        var decisionEffective = new Dictionary<string, bool>();
        var decisionDefeatedBy = new Dictionary<string, (string DefeaterAid, string Kind)>();

        // as we walk descending, "already known effective" = higher tx, already processed.
        var effectiveByTargetAid = new Dictionary<string, List<DecRow>>();
        var effectiveByTargetCKey = new Dictionary<string, List<DecRow>>();

        foreach (var d in decisionsOrdered)
        {
            bool admitted;
            if (d.Status == "fact")
            {
                admitted = true;
            }
            else
            {
                // proposed decision: needs an effective 'accepts' targeting it (already processed, higher tx).
                admitted = TargetsHit(effectiveByTargetAid, effectiveByTargetCKey, d.Aid, d.CKey)
                    .Any(x => x.Kind == "accepts");
            }

            (string DefeaterAid, string Kind)? defeater = null;
            foreach (var hit in TargetsHit(effectiveByTargetAid, effectiveByTargetCKey, d.Aid, d.CKey))
            {
                if (hit.Kind is "rejects" or "retracts" or "supersedes")
                {
                    defeater = (hit.Aid, hit.Kind);
                    break;
                }
            }

            bool effective = admitted && defeater is null;
            decisionEffective[d.Aid] = effective;
            if (defeater is not null)
            {
                decisionDefeatedBy[d.Aid] = defeater.Value;
            }

            if (effective)
            {
                if (d.TgtAid is not null)
                {
                    if (!effectiveByTargetAid.TryGetValue(d.TgtAid, out var list))
                    {
                        effectiveByTargetAid[d.TgtAid] = list = new List<DecRow>();
                    }
                    list.Add(d);
                }
                if (d.TgtCKey is not null)
                {
                    if (!effectiveByTargetCKey.TryGetValue(d.TgtCKey, out var list))
                    {
                        effectiveByTargetCKey[d.TgtCKey] = list = new List<DecRow>();
                    }
                    list.Add(d);
                }
            }
        }

        // Step 4/5: admission + decision-defeat for non-decision assertions.
        var entries = new Dictionary<string, EntryResult>();
        var regular = visible.Values.Where(a => a.Pred != "gneiss.decision").ToList();

        foreach (var a in regular)
        {
            bool admitted;
            bool autoAdmitted = false;

            if (a.Status == "fact")
            {
                admitted = true;
            }
            else
            {
                var acceptHits = TargetsHit(effectiveByTargetAid, effectiveByTargetCKey, a.Aid, a.CKey)
                    .Where(x => x.Kind == "accepts").ToList();
                if (acceptHits.Count > 0)
                {
                    admitted = true;
                }
                else if (ctx.Admit == "threshold" && a.Conf.HasValue && ctx.AdmitThresholdBp.HasValue && a.Conf.Value >= ctx.AdmitThresholdBp.Value)
                {
                    admitted = true;
                    autoAdmitted = true;
                }
                else
                {
                    admitted = false;
                }
            }

            if (!admitted)
            {
                entries[a.Aid] = new EntryResult(EntryStatus.NotAdmitted, null, null, false);
                continue;
            }

            var defeatHits = TargetsHit(effectiveByTargetAid, effectiveByTargetCKey, a.Aid, a.CKey)
                .Where(x => x.Kind is "rejects" or "retracts" or "supersedes")
                .OrderBy(x => x.Tx).ThenBy(x => x.Aid, StringComparer.Ordinal)
                .ToList();

            if (defeatHits.Count > 0)
            {
                var winner = defeatHits[0];
                entries[a.Aid] = new EntryResult(EntryStatus.Defeated, winner.Aid, $"decision:{winner.Kind}", autoAdmitted);
            }
            else
            {
                // tentatively accepted; conflict resolution (step 6) may still defeat/contest it.
                entries[a.Aid] = new EntryResult(EntryStatus.Accepted, null, null, autoAdmitted);
            }
        }

        // Step 6: conflicts, among admitted & not-yet-decision-defeated regular candidates.
        var candidates = regular.Where(a => entries[a.Aid].Status == EntryStatus.Accepted).ToList();
        var contestedGroups = new List<ContestedGroup>();

        foreach (var group in candidates.GroupBy(a => (a.Subj, a.Pred)))
        {
            var members = group.ToList();
            if (members.Count < 2)
            {
                continue;
            }

            var predicate = ResolvePredicate(conn, ctx.DefCut, group.Key.Pred, predCache);
            var components = ConnectedComponents(members, predicate);

            foreach (var component in components)
            {
                if (component.Count < 2)
                {
                    continue;
                }
                ResolveConflictGroup(component, predicate, entries, contestedGroups);
            }
        }

        // Step 3 results also become entries, for decisions.
        foreach (var d in decisions.Values)
        {
            if (decisionEffective[d.Aid])
            {
                entries[d.Aid] = new EntryResult(EntryStatus.Accepted, null, null, false);
            }
            else if (decisionDefeatedBy.TryGetValue(d.Aid, out var defeater))
            {
                entries[d.Aid] = new EntryResult(EntryStatus.Defeated, defeater.DefeaterAid, $"decision:{defeater.Kind}", false);
            }
            else
            {
                entries[d.Aid] = new EntryResult(EntryStatus.NotAdmitted, null, null, false);
            }
        }

        // Step 7: stale via justification, DFS per accepted aid over already-computed statuses.
        var justByAid = LoadJust(conn);
        var stale = new Dictionary<string, bool>();
        foreach (var aid in entries.Keys.Where(k => entries[k].Status == EntryStatus.Accepted))
        {
            stale[aid] = HasDefeatedOrContestedAncestor(aid, justByAid, entries, new HashSet<string>(), 0);
        }

        return new FoldOutcome(ctx, visible, entries, contestedGroups, stale, predCache);
    }

    private static bool HasDefeatedOrContestedAncestor(
        string aid,
        IReadOnlyDictionary<string, List<JustRow>> justByAid,
        IReadOnlyDictionary<string, EntryResult> entries,
        HashSet<string> visited,
        int depth)
    {
        if (depth > 32 || !visited.Add(aid))
        {
            return false;
        }

        if (!justByAid.TryGetValue(aid, out var edges))
        {
            return false;
        }

        foreach (var edge in edges)
        {
            if (edge.InputAid is null)
            {
                continue;
            }

            if (entries.TryGetValue(edge.InputAid, out var inputEntry) &&
                (inputEntry.Status == EntryStatus.Defeated || inputEntry.Status == EntryStatus.Contested))
            {
                return true;
            }

            if (HasDefeatedOrContestedAncestor(edge.InputAid, justByAid, entries, visited, depth + 1))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<DecRow> TargetsHit(
        Dictionary<string, List<DecRow>> byAid,
        Dictionary<string, List<DecRow>> byCKey,
        string aid,
        string ckey)
    {
        if (byAid.TryGetValue(aid, out var l1))
        {
            foreach (var d in l1)
            {
                yield return d;
            }
        }
        if (byCKey.TryGetValue(ckey, out var l2))
        {
            foreach (var d in l2)
            {
                yield return d;
            }
        }
    }

    private static List<List<AssrtRow>> ConnectedComponents(List<AssrtRow> members, ResolvedPredicate predicate)
    {
        var parent = new Dictionary<string, string>();
        string Find(string x)
        {
            while (parent[x] != x)
            {
                parent[x] = parent[parent[x]];
                x = parent[x];
            }
            return x;
        }
        void Union(string x, string y)
        {
            var rx = Find(x);
            var ry = Find(y);
            if (rx != ry)
            {
                parent[rx] = ry;
            }
        }

        foreach (var m in members)
        {
            parent[m.Aid] = m.Aid;
        }

        for (int i = 0; i < members.Count; i++)
        {
            for (int j = i + 1; j < members.Count; j++)
            {
                if (Comparators.IntervalsOverlap(members[i], members[j]) && Comparators.Incompatible(predicate, members[i], members[j]))
                {
                    Union(members[i].Aid, members[j].Aid);
                }
            }
        }

        return members.GroupBy(m => Find(m.Aid)).Select(g => g.ToList()).ToList();
    }

    /// <summary>Local 3+1-valued label used while computing grounded pairwise conflict resolution.</summary>
    private enum LocalStatus
    {
        Undecided,
        Accepted,
        Defeated,
        Contested,
    }

    private readonly record struct PairContest(string WinnerAid, string LoserAid, int Rung);

    /// <summary>
    /// Contests exactly two candidates through the strainer rungs (2..StopRung), reusing <see
    /// cref="ApplyRung"/> at each rung. Returns the (winner, loser, decidingRung) if the pair resolves
    /// to a unique winner at or before StopRung; returns null ("unresolved") if the pair is still tied
    /// once StopRung is reached -- per kb defect-1 fix, an unresolved pair is NOT an attack either way.
    /// </summary>
    private static PairContest? ContestPair(AssrtRow a, AssrtRow b, ResolvedPredicate predicate)
    {
        var remaining = new List<AssrtRow> { a, b };
        int rung = 2;
        while (remaining.Count > 1 && rung <= predicate.StopRung)
        {
            var survivors = ApplyRung(rung, remaining, predicate);
            if (survivors.Count < remaining.Count)
            {
                var winner = survivors[0];
                var loser = remaining.First(m => m.Aid != winner.Aid);
                return new PairContest(winner.Aid, loser.Aid, rung);
            }
            rung++;
        }
        return null;
    }

    /// <summary>
    /// Grounded pairwise conflict resolution (kb defect-1 fix): candidates are grouped into connected
    /// components by pairwise conflict edges (overlap + incompatible), same as before -- but each edge
    /// is now contested INDEPENDENTLY through the strainer (<see cref="ContestPair"/>), rather than the
    /// whole component being run through the strainer together as one N-way contest (the transitive-
    /// defeat bug: a component can be connected through a chain of edges even though some members never
    /// pairwise-conflict with each other).
    ///
    /// A pairwise contest either resolves into a directed attack (winner -> loser, at the deciding
    /// rung) or is left unresolved (StopRung reached without narrowing to one). Grounded labeling is
    /// then computed over the attack graph, PLUS unresolved edges block acceptance of either endpoint
    /// until the other is defeated by someone else -- folding CONTRACT step 6's "step 3 fixpoint" and
    /// "step 4 post-processing" into one synchronous per-pass recomputation (from the PREVIOUS pass's
    /// snapshot only, so results are order-independent and deterministic) is what makes this sound: it
    /// guarantees every recorded DefeatedBy attacker is ACCEPTED in that same converged snapshot (never
    /// a node that later gets pulled into Contested by its own unresolved rivalry), because at a
    /// fixpoint a node's classification and its inputs' classifications are the same snapshot. Bounded
    /// to component.Count (+2 margin) passes, per CONTRACT step 6's "terminates in <= n passes".
    /// Anything still Undecided when the loop stops is a genuine attack cycle (StoppedAtRung 6); nodes
    /// pulled into Contested via a still-live unresolved edge report the predicate's own StopRung.
    /// </summary>
    private static void ResolveConflictGroup(
        List<AssrtRow> component,
        ResolvedPredicate predicate,
        Dictionary<string, EntryResult> entries,
        List<ContestedGroup> contestedGroups)
    {
        var byAid = component.ToDictionary(m => m.Aid);
        var attacks = new Dictionary<string, List<(string AttackerAid, int Rung)>>(StringComparer.Ordinal);
        var unresolvedPartners = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var unresolvedEdges = new List<(string A, string B)>();
        var attackEdges = new List<(string From, string To)>();

        foreach (var m in component)
        {
            attacks[m.Aid] = new List<(string, int)>();
            unresolvedPartners[m.Aid] = new List<string>();
        }

        for (int i = 0; i < component.Count; i++)
        {
            for (int j = i + 1; j < component.Count; j++)
            {
                var a = component[i];
                var b = component[j];
                if (!Comparators.IntervalsOverlap(a, b) || !Comparators.Incompatible(predicate, a, b))
                {
                    continue;
                }

                var contest = ContestPair(a, b, predicate);
                if (contest is { } r)
                {
                    attacks[r.LoserAid].Add((r.WinnerAid, r.Rung));
                    attackEdges.Add((r.WinnerAid, r.LoserAid));
                }
                else
                {
                    unresolvedPartners[a.Aid].Add(b.Aid);
                    unresolvedPartners[b.Aid].Add(a.Aid);
                    unresolvedEdges.Add((a.Aid, b.Aid));
                }
            }
        }

        var status = component.ToDictionary(m => m.Aid, _ => LocalStatus.Undecided, StringComparer.Ordinal);
        var defeaterAid = new Dictionary<string, string>(StringComparer.Ordinal);
        var defeaterRung = new Dictionary<string, int>(StringComparer.Ordinal);

        int passes = component.Count + 2;
        for (int pass = 0; pass < passes; pass++)
        {
            var prev = new Dictionary<string, LocalStatus>(status, StringComparer.Ordinal);
            bool changed = false;

            foreach (var m in component)
            {
                var attackers = attacks[m.Aid];
                var acceptedAttackers = attackers.Where(x => prev[x.AttackerAid] == LocalStatus.Accepted).ToList();

                LocalStatus next;
                if (acceptedAttackers.Count > 0)
                {
                    var winner = acceptedAttackers
                        .OrderByDescending(x => byAid[x.AttackerAid].Tx)
                        .ThenByDescending(x => x.AttackerAid, StringComparer.Ordinal)
                        .First();
                    next = LocalStatus.Defeated;
                    defeaterAid[m.Aid] = winner.AttackerAid;
                    defeaterRung[m.Aid] = winner.Rung;
                }
                else if (attackers.All(x => prev[x.AttackerAid] == LocalStatus.Defeated))
                {
                    var blocked = unresolvedPartners[m.Aid].Any(p => prev[p] != LocalStatus.Defeated);
                    next = blocked ? LocalStatus.Contested : LocalStatus.Accepted;
                }
                else
                {
                    next = LocalStatus.Undecided;
                }

                if (next != status[m.Aid])
                {
                    changed = true;
                }
                status[m.Aid] = next;
            }

            if (!changed)
            {
                break;
            }
        }

        // Anything still Undecided after the fixpoint is a genuine attack cycle.
        var cycleNodes = component.Where(m => status[m.Aid] == LocalStatus.Undecided).Select(m => m.Aid).ToHashSet(StringComparer.Ordinal);
        foreach (var aid in cycleNodes)
        {
            status[aid] = LocalStatus.Contested;
        }

        foreach (var m in component)
        {
            switch (status[m.Aid])
            {
                case LocalStatus.Defeated:
                    entries[m.Aid] = new EntryResult(EntryStatus.Defeated, defeaterAid[m.Aid], $"conflict:rung{defeaterRung[m.Aid]}", entries[m.Aid].AutoAdmitted);
                    break;
                case LocalStatus.Contested:
                    entries[m.Aid] = new EntryResult(EntryStatus.Contested, null, null, entries[m.Aid].AutoAdmitted);
                    break;
                case LocalStatus.Accepted:
                    // already tentatively Accepted from step 4/5; nothing to change.
                    break;
            }
        }

        // Group the Contested nodes into ContestedGroup outputs by connectivity: unresolved edges and
        // attack edges between two nodes that both ended Contested cover both the "unresolved pair" and
        // "attack cycle" cases from CONTRACT.md step 6.
        var contestedSet = component.Where(m => status[m.Aid] == LocalStatus.Contested).Select(m => m.Aid).ToHashSet(StringComparer.Ordinal);
        if (contestedSet.Count == 0)
        {
            return;
        }

        var parent = contestedSet.ToDictionary(a => a, a => a, StringComparer.Ordinal);
        string Find(string x)
        {
            while (parent[x] != x)
            {
                parent[x] = parent[parent[x]];
                x = parent[x];
            }
            return x;
        }
        void Union(string x, string y)
        {
            var rx = Find(x);
            var ry = Find(y);
            if (rx != ry)
            {
                parent[rx] = ry;
            }
        }

        foreach (var (a, b) in unresolvedEdges)
        {
            if (contestedSet.Contains(a) && contestedSet.Contains(b))
            {
                Union(a, b);
            }
        }
        foreach (var (from, to) in attackEdges)
        {
            if (contestedSet.Contains(from) && contestedSet.Contains(to))
            {
                Union(from, to);
            }
        }

        foreach (var groupAids in contestedSet.GroupBy(Find))
        {
            var members = groupAids.OrderBy(a => a, StringComparer.Ordinal).ToList();
            var memberSet = new HashSet<string>(members, StringComparer.Ordinal);
            // A group's indeterminacy is attributed to the predicate's own StopRung when at least one
            // of its members is directly tied to another (a genuine unresolved pairwise contest); a
            // group held together ONLY through resolved attack edges (no unresolved pair inside it) can
            // only be indeterminate because of a genuine attack cycle, per CONTRACT.md step 6 ("6 for
            // cycles"). This also correctly covers a node whose sole issue is depending on a neighbor
            // that is itself part of an unresolved pair (e.g. defect-1 test (c)'s third chain node): it
            // shares a group with that unresolved pair, so the group reports the predicate's StopRung.
            var stoppedAtRung = unresolvedEdges.Any(e => memberSet.Contains(e.A) && memberSet.Contains(e.B))
                ? predicate.StopRung
                : 6;
            var groupCKey = members.Select(a => byAid[a].CKey).Distinct().Count() == 1
                ? byAid[members[0]].CKey
                : byAid[members.OrderBy(a => a, StringComparer.Ordinal).First()].CKey; // DIVERGENCE: heterogeneous-ckey groups report a representative ckey (v0 simplification; not exercised by required tests).

            contestedGroups.Add(new ContestedGroup(groupCKey, members, stoppedAtRung));
        }
    }

    private static List<AssrtRow> ApplyRung(int rung, List<AssrtRow> candidates, ResolvedPredicate predicate)
    {
        switch (rung)
        {
            case 2:
                {
                    int Rank(AssrtRow a)
                    {
                        var idx = predicate.SourcePrecedence.ToList().IndexOf(a.Src ?? string.Empty);
                        return idx >= 0 ? idx : predicate.SourcePrecedence.Count;
                    }
                    var minRank = candidates.Min(Rank);
                    return candidates.Where(a => Rank(a) == minRank).ToList();
                }
            case 3:
                {
                    // DIVERGENCE: CONTRACT.md section 3 step 6 states rung 3 pairwise ("only when one
                    // interval strictly contains the other: narrower wins") without specifying the
                    // >2-candidate case. We generalize: a candidate is eliminated iff some other tied
                    // candidate's interval is strictly narrower and contained within it; candidates that
                    // are incomparable (neither contains the other) both survive this rung.
                    var survivors = candidates.Where(x => !candidates.Any(y => y.Aid != x.Aid && Comparators.StrictlyContains(x, y))).ToList();
                    return survivors.Count == 0 ? candidates : survivors;
                }
            case 4:
                {
                    if (!predicate.InstantSampled)
                    {
                        return candidates;
                    }
                    var maxFrom = candidates.Max(a => a.VFrom ?? string.Empty);
                    return candidates.Where(a => (a.VFrom ?? string.Empty) == maxFrom).ToList();
                }
            case 5:
                {
                    var maxConf = candidates.Max(a => a.Conf ?? -1);
                    return candidates.Where(a => (a.Conf ?? -1) == maxConf).ToList();
                }
            case 6:
                {
                    var maxTx = candidates.Max(a => a.Tx);
                    var tied = candidates.Where(a => a.Tx == maxTx).ToList();
                    if (tied.Count == 1)
                    {
                        return tied;
                    }
                    // DIVERGENCE: rung 6 is specified as a total tiebreak ("later tx wins"), but two
                    // assertions from the same Append share a tx. We add aid as a final, deterministic
                    // secondary key so the pipeline always terminates in a unique winner when StopRung >= 6.
                    var maxAid = tied.Select(a => a.Aid).Max(StringComparer.Ordinal);
                    return tied.Where(a => a.Aid == maxAid).ToList();
                }
            default:
                return candidates;
        }
    }

    // ---- loading --------------------------------------------------------------------------------

    private static Dictionary<string, AssrtRow> LoadVisible(SqliteConnection conn, long dataCut)
    {
        var result = new Dictionary<string, AssrtRow>();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT aid, tx, subj, pred, val, valkind, vfrom, vto, status, src, meth, conf, ckey FROM assrt WHERE tx <= $cut ORDER BY tx, aid";
        cmd.Parameters.AddWithValue("$cut", dataCut);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var row = new AssrtRow(
                Aid: reader.GetString(0),
                Tx: reader.GetInt64(1),
                Subj: reader.GetString(2),
                Pred: reader.GetString(3),
                Val: reader.GetString(4),
                ValKind: reader.GetString(5),
                VFrom: reader.IsDBNull(6) ? null : reader.GetString(6),
                VTo: reader.IsDBNull(7) ? null : reader.GetString(7),
                Status: reader.GetString(8),
                Src: reader.IsDBNull(9) ? null : reader.GetString(9),
                Meth: reader.IsDBNull(10) ? null : reader.GetString(10),
                Conf: reader.IsDBNull(11) ? null : reader.GetInt32(11),
                CKey: reader.GetString(12));
            result[row.Aid] = row;
        }
        return result;
    }

    private static Dictionary<string, DecRow> LoadDecisions(SqliteConnection conn, Dictionary<string, AssrtRow> visible)
    {
        var result = new Dictionary<string, DecRow>();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT aid, kind, tgt_aid, tgt_ckey FROM dec";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var aid = reader.GetString(0);
            if (!visible.TryGetValue(aid, out var a))
            {
                continue; // decision not visible at this dataCut
            }
            result[aid] = new DecRow(
                Aid: aid,
                Tx: a.Tx,
                Kind: reader.GetString(1),
                TgtAid: reader.IsDBNull(2) ? null : reader.GetString(2),
                TgtCKey: reader.IsDBNull(3) ? null : reader.GetString(3),
                Status: a.Status,
                CKey: a.CKey,
                Subj: a.Subj);
        }
        return result;
    }

    internal static Dictionary<string, List<JustRow>> LoadJust(SqliteConnection conn)
    {
        var result = new Dictionary<string, List<JustRow>>();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT aid, input_aid, rule_ver, role FROM just";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var row = new JustRow(
                Aid: reader.GetString(0),
                InputAid: reader.IsDBNull(1) ? null : reader.GetString(1),
                RuleVer: reader.IsDBNull(2) ? null : reader.GetString(2),
                Role: reader.IsDBNull(3) ? null : reader.GetString(3));
            if (!result.TryGetValue(row.Aid, out var list))
            {
                result[row.Aid] = list = new List<JustRow>();
            }
            list.Add(row);
        }
        return result;
    }
}
