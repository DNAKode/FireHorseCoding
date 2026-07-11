using System.Globalization;
using Gneiss.Cell.Internal;
using Microsoft.Data.Sqlite;

namespace Gneiss.Cell;

/// <summary>
/// A Gneiss ledger: one SQLite file, one writer. Implements E1 (ledger + belief fold) + E2
/// (labels + why()) + E3-lite (staleness) per CONTRACT.md.
/// </summary>
public sealed class GneissLedger : IDisposable
{
    private readonly SqliteConnection _conn;

    private GneissLedger(SqliteConnection conn)
    {
        _conn = conn;
    }

    public static GneissLedger Create(string path)
    {
        if (File.Exists(path))
        {
            throw new GneissException("AlreadyExists", $"Ledger file already exists: {path}");
        }

        var conn = OpenConnection(path, create: true);
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = Schema.Ddl;
            cmd.ExecuteNonQuery();
        }
        return new GneissLedger(conn);
    }

    public static GneissLedger Open(string path)
    {
        if (!File.Exists(path))
        {
            throw new GneissException("NotFound", $"Ledger file does not exist: {path}");
        }

        var conn = OpenConnection(path, create: false);
        return new GneissLedger(conn);
    }

    private static SqliteConnection OpenConnection(string path, bool create)
    {
        var csb = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = create ? SqliteOpenMode.ReadWriteCreate : SqliteOpenMode.ReadWrite,
        };
        var conn = new SqliteConnection(csb.ToString());
        conn.Open();
        using (var pragma = conn.CreateCommand())
        {
            pragma.CommandText = "PRAGMA foreign_keys = ON;";
            pragma.ExecuteNonQuery();
        }
        return conn;
    }

    public long HighWater
    {
        get
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT COALESCE(MAX(id), 0) FROM tx";
            return (long)cmd.ExecuteScalar()!;
        }
    }

    // ---- Append -------------------------------------------------------------------------------

    public AppendResult Append(TxEnvelope env, IReadOnlyList<IAppendItem> items)
    {
        using var tx = _conn.BeginTransaction();
        try
        {
            long txId = InsertTxRow(env, tx);

            var aids = new List<string>(items.Count);
            int ordinal = 0;
            foreach (var item in items)
            {
                switch (item)
                {
                    case NewAssertion na:
                        aids.Add(InsertAssertion(txId, ordinal, na, tx));
                        ordinal++;
                        break;
                    case NewDecision nd:
                        aids.Add(InsertDecision(txId, ordinal, nd, tx));
                        ordinal++;
                        break;
                    default:
                        throw new GneissException("InvalidArgument", $"Unknown IAppendItem type '{item.GetType()}'.");
                }
            }

            tx.Commit();
            return new AppendResult(new TxId(txId), aids);
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private long InsertTxRow(TxEnvelope env, SqliteTransaction tx)
    {
        using var cmd = _conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "INSERT INTO tx (wall, actor, reason, batch) VALUES ($wall, $actor, $reason, $batch); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("$wall", Hashing.FormatWall(env.Wall));
        cmd.Parameters.AddWithValue("$actor", env.Actor);
        cmd.Parameters.AddWithValue("$reason", env.Reason);
        cmd.Parameters.AddWithValue("$batch", (object?)env.Batch ?? DBNull.Value);
        return (long)cmd.ExecuteScalar()!;
    }

    private string InsertAssertion(long txId, int ordinal, NewAssertion na, SqliteTransaction tx)
    {
        var vfrom = na.ValidFrom.HasValue ? Hashing.FormatWall(na.ValidFrom.Value) : null;
        var vto = na.ValidTo.HasValue ? Hashing.FormatWall(na.ValidTo.Value) : null;
        var status = na.Proposed ? "proposed" : "fact";

        var ckey = ComputeCKey(na.Subject, na.Predicate, vfrom, vto);
        var aid = ComputeAid(txId, ordinal, na.Subject, na.Predicate, na.Value.Canonical, vfrom, vto, status, na.Source, na.Method);

        using (var cmd = _conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO assrt (aid, tx, subj, pred, val, valkind, vfrom, vto, status, src, meth, conf, ckey)
                VALUES ($aid, $tx, $subj, $pred, $val, $valkind, $vfrom, $vto, $status, $src, $meth, $conf, $ckey)
                """;
            cmd.Parameters.AddWithValue("$aid", aid);
            cmd.Parameters.AddWithValue("$tx", txId);
            cmd.Parameters.AddWithValue("$subj", na.Subject);
            cmd.Parameters.AddWithValue("$pred", na.Predicate);
            cmd.Parameters.AddWithValue("$val", na.Value.Canonical);
            cmd.Parameters.AddWithValue("$valkind", na.Value.Kind);
            cmd.Parameters.AddWithValue("$vfrom", (object?)vfrom ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$vto", (object?)vto ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$status", status);
            cmd.Parameters.AddWithValue("$src", (object?)na.Source ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$meth", (object?)na.Method ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$conf", (object?)na.ConfidenceBp ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$ckey", ckey);
            cmd.ExecuteNonQuery();
        }

        if (na.Justifications is { Count: > 0 })
        {
            foreach (var j in na.Justifications)
            {
                using var cmd = _conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "INSERT INTO just (aid, input_aid, rule_ver, role) VALUES ($aid, $input, $rulever, $role)";
                cmd.Parameters.AddWithValue("$aid", aid);
                cmd.Parameters.AddWithValue("$input", (object?)j.InputAid ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$rulever", (object?)j.RuleVersion ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$role", (object?)j.Role ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        return aid;
    }

    private string InsertDecision(long txId, int ordinal, NewDecision nd, SqliteTransaction tx)
    {
        bool hasAid = nd.TargetAid is not null;
        bool hasCKey = nd.TargetClaimKey is not null;
        if (hasAid == hasCKey)
        {
            throw new GneissException("InvalidArgument", "NewDecision must set exactly one of TargetAid / TargetClaimKey.");
        }

        // I6: an aid-targeted decision must target an assertion with strictly lower tx.
        // ckey-targeted decisions are exempt (they re-attach across rebuilds; kb/22 §8 Q2, D3).
        if (hasAid)
        {
            using var lookup = _conn.CreateCommand();
            lookup.Transaction = tx;
            lookup.CommandText = "SELECT tx FROM assrt WHERE aid = $aid";
            lookup.Parameters.AddWithValue("$aid", nd.TargetAid!);
            var targetTx = lookup.ExecuteScalar();
            if (targetTx is null)
            {
                throw new GneissException("UnknownTarget", $"Decision targets unknown aid '{nd.TargetAid}'.");
            }
            if ((long)targetTx >= txId)
            {
                throw new GneissException("I6Violation", $"Decision at tx {txId} targets aid '{nd.TargetAid}' at tx {(long)targetTx}, which is not strictly lower.");
            }
        }

        var subj = nd.TargetClaimKey ?? nd.TargetAid!;
        var kindWire = DeclarationCodec.KindToWire(nd.Kind);
        var val = DeclarationCodec.EncodeDecision(nd.Kind, nd.TargetAid, nd.TargetClaimKey);
        const string status = "fact";

        var ckey = ComputeCKey(subj, "gneiss.decision", null, null);
        var aid = ComputeAid(txId, ordinal, subj, "gneiss.decision", val, null, null, status, null, null);

        using (var cmd = _conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO assrt (aid, tx, subj, pred, val, valkind, vfrom, vto, status, src, meth, conf, ckey)
                VALUES ($aid, $tx, $subj, 'gneiss.decision', $val, 'json', NULL, NULL, $status, NULL, NULL, NULL, $ckey)
                """;
            cmd.Parameters.AddWithValue("$aid", aid);
            cmd.Parameters.AddWithValue("$tx", txId);
            cmd.Parameters.AddWithValue("$subj", subj);
            cmd.Parameters.AddWithValue("$val", val);
            cmd.Parameters.AddWithValue("$status", status);
            cmd.Parameters.AddWithValue("$ckey", ckey);
            cmd.ExecuteNonQuery();
        }

        using (var cmd = _conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT INTO dec (aid, kind, tgt_aid, tgt_ckey) VALUES ($aid, $kind, $tgtaid, $tgtckey)";
            cmd.Parameters.AddWithValue("$aid", aid);
            cmd.Parameters.AddWithValue("$kind", kindWire);
            cmd.Parameters.AddWithValue("$tgtaid", (object?)nd.TargetAid ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$tgtckey", (object?)nd.TargetClaimKey ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        return aid;
    }

    private static string ComputeCKey(string subj, string pred, string? vfrom, string? vto) =>
        Hashing.Sha256Hex($"{subj}|{pred}|{vfrom ?? string.Empty}|{vto ?? string.Empty}");

    private static string ComputeAid(long txId, int ordinal, string subj, string pred, string val, string? vfrom, string? vto, string status, string? src, string? meth) =>
        Hashing.Sha256Hex($"{txId}:{ordinal}|{subj}|{pred}|{val}|{vfrom ?? string.Empty}|{vto ?? string.Empty}|{status}|{src ?? string.Empty}|{meth ?? string.Empty}");

    // ---- Declarations ---------------------------------------------------------------------------

    public void DeclarePredicate(TxEnvelope env, PredicateDecl decl)
    {
        var val = DeclarationCodec.EncodePredicateDecl(decl);
        Append(env, new IAppendItem[] { new NewAssertion(decl.Name, "gneiss.predicate", GValue.Json(val)) });
    }

    public void DeclareContext(TxEnvelope env, ContextDecl decl)
    {
        var val = DeclarationCodec.EncodeContextDecl(decl);
        Append(env, new IAppendItem[] { new NewAssertion(decl.Name, "gneiss.context", GValue.Json(val)) });
    }

    // ---- Ask ----------------------------------------------------------------------------------

    public BeliefView Ask(string contextName, Question q)
    {
        var hw = HighWater;
        var ctx = BeliefFold.ResolveContext(_conn, contextName, hw);
        var outcome = BeliefFold.Compute(_conn, ctx);

        bool askAll = q.Subject is null && q.Predicate is null && q.ClaimKey is null;

        bool Matches(AssrtRow r) =>
            (q.ClaimKey is null || r.CKey == q.ClaimKey) &&
            (q.Subject is null || r.Subj == q.Subject) &&
            (q.Predicate is null || r.Pred == q.Predicate);

        var accepted = new List<BeliefEntry>();
        var defeated = new List<BeliefEntry>();

        foreach (var (aid, entry) in outcome.Entries)
        {
            if (entry.Status is not (EntryStatus.Accepted or EntryStatus.Defeated))
            {
                continue;
            }
            var row = outcome.VisibleAssrt[aid];
            if (!Matches(row))
            {
                continue;
            }
            var be = new BeliefEntry(
                Aid: row.Aid,
                Subject: row.Subj,
                Predicate: row.Pred,
                Value: new GValue(row.ValKind, row.Val),
                ClaimKey: row.CKey,
                AutoAdmitted: entry.AutoAdmitted,
                StaleViaJustification: outcome.Stale.TryGetValue(aid, out var s) && s,
                DefeatedBy: entry.DefeaterAid,
                DefeatReason: entry.DefeatReason);
            if (entry.Status == EntryStatus.Accepted)
            {
                accepted.Add(be);
            }
            else
            {
                defeated.Add(be);
            }
        }

        accepted = accepted.OrderBy(e => e.Aid, StringComparer.Ordinal).ToList();
        defeated = defeated.OrderBy(e => e.Aid, StringComparer.Ordinal).ToList();

        var contested = outcome.AllContestedGroups
            .Where(cg => askAll || cg.Aids.Any(a => outcome.VisibleAssrt.TryGetValue(a, out var r) && Matches(r)))
            .OrderBy(cg => cg.ClaimKey, StringComparer.Ordinal)
            .ToList();

        TypedMissing? missing = null;
        if (!askAll)
        {
            bool anyVisible = outcome.VisibleAssrt.Values.Any(Matches);
            if (!anyVisible)
            {
                missing = new TypedMissing("unknown");
            }
        }

        var consumedAids = ComputeConsumedAids(outcome, q, askAll, Matches);

        var resultJson = ResultCodec.BuildResultJson(accepted, defeated, contested, missing);
        var resultHash = Hashing.Sha256Hex(resultJson);

        var questionJson = CanonicalJsonWriter.ToJson(w => w.Obj(o => o
            .FieldNullableString("subject", q.Subject)
            .FieldNullableString("predicate", q.Predicate)
            .FieldNullableString("claimKey", q.ClaimKey)));

        // Deterministic, content-derived receipt id (CONTRACT-V01.md section 2): repeated identical
        // asks produce the same id, so the row upserts below instead of accumulating duplicates.
        var receiptId = Hashing.Sha256Hex(
            $"{ctx.ContextHash}|{ctx.DataCut.ToString(CultureInfo.InvariantCulture)}|{ctx.DefCut.ToString(CultureInfo.InvariantCulture)}|{questionJson}|{resultHash}");

        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = """
                INSERT OR REPLACE INTO receipt (id, question, ctx_name, ctx_hash, data_cut, def_cut, consumed, result_hash, created_wall)
                VALUES ($id, $question, $ctxname, $ctxhash, $datacut, $defcut, $consumed, $resulthash, $created)
                """;
            cmd.Parameters.AddWithValue("$id", receiptId);
            cmd.Parameters.AddWithValue("$question", questionJson);
            cmd.Parameters.AddWithValue("$ctxname", contextName);
            cmd.Parameters.AddWithValue("$ctxhash", ctx.ContextHash);
            cmd.Parameters.AddWithValue("$datacut", ctx.DataCut);
            cmd.Parameters.AddWithValue("$defcut", ctx.DefCut);
            cmd.Parameters.AddWithValue("$consumed", string.Join(",", consumedAids));
            cmd.Parameters.AddWithValue("$resulthash", resultHash);
            cmd.Parameters.AddWithValue("$created", Hashing.FormatWall(DateTimeOffset.UtcNow));
            cmd.ExecuteNonQuery();
        }

        var label = new Label(contextName, ctx.ContextHash, ctx.DataCut, ctx.DefCut, consumedAids, resultHash, receiptId);
        return new BeliefView(label, accepted, defeated, contested, missing);
    }

    private List<string> ComputeConsumedAids(FoldOutcome outcome, Question q, bool askAll, Func<AssrtRow, bool> matches)
    {
        var consumed = new HashSet<string>(StringComparer.Ordinal) { outcome.Ctx.DeclAid };

        if (askAll)
        {
            foreach (var aid in outcome.VisibleAssrt.Keys)
            {
                consumed.Add(aid);
            }
            foreach (var p in outcome.PredicatesUsed.Values)
            {
                if (p.DeclAid is not null)
                {
                    consumed.Add(p.DeclAid);
                }
            }
            return consumed.OrderBy(a => a, StringComparer.Ordinal).ToList();
        }

        var matchedRegular = outcome.VisibleAssrt.Values
            .Where(r => r.Pred != "gneiss.decision" && matches(r))
            .ToList();

        foreach (var r in matchedRegular)
        {
            consumed.Add(r.Aid);
        }

        // Close the consumed set transitively over the decision graph (kb defect-2 fix): starting from
        // the matched assertions' aids/ckeys, repeatedly add every visible decision whose tgt_aid or
        // tgt_ckey is in the frontier -- including decisions targeting decisions, via the decision
        // assertions' own aid/ckey -- to fixpoint. Without this, a decision-on-decision chain (e.g. D2
        // retracts D1 which retracts A) only ever records the one-hop decision (D1) as consumed, so a
        // later decision that flips D1's effectiveness by defeating D2 (not D1 itself) is invisible to
        // CheckStale.
        var allDecisions = LoadAllDecisions(outcome.Ctx.DataCut);
        var frontierAids = new HashSet<string>(matchedRegular.Select(r => r.Aid), StringComparer.Ordinal);
        var frontierCKeys = new HashSet<string>(matchedRegular.Select(r => r.CKey), StringComparer.Ordinal);
        var consumedDecisionAids = new HashSet<string>(StringComparer.Ordinal);

        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (var d in allDecisions)
            {
                if (consumedDecisionAids.Contains(d.Aid))
                {
                    continue;
                }
                bool hits = (d.TgtAid is not null && frontierAids.Contains(d.TgtAid)) ||
                            (d.TgtCKey is not null && frontierCKeys.Contains(d.TgtCKey));
                if (hits)
                {
                    consumedDecisionAids.Add(d.Aid);
                    frontierAids.Add(d.Aid);
                    frontierCKeys.Add(d.CKey);
                    changed = true;
                }
            }
        }

        foreach (var aid in consumedDecisionAids)
        {
            consumed.Add(aid);
        }

        var predCache = new Dictionary<string, ResolvedPredicate>();
        foreach (var predName in matchedRegular.Select(r => r.Pred).Distinct(StringComparer.Ordinal))
        {
            var resolved = BeliefFold.ResolvePredicate(_conn, outcome.Ctx.DefCut, predName, predCache);
            if (resolved.DeclAid is not null)
            {
                consumed.Add(resolved.DeclAid);
            }
        }

        return consumed.OrderBy(a => a, StringComparer.Ordinal).ToList();
    }

    /// <summary>All decisions visible at dataCut, with their own ckey (for closing the consumed set
    /// transitively over decisions-targeting-decisions; see <see cref="ComputeConsumedAids"/>).</summary>
    private List<(string Aid, string CKey, string? TgtAid, string? TgtCKey)> LoadAllDecisions(long dataCut)
    {
        var result = new List<(string, string, string?, string?)>();
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            SELECT d.aid, a.ckey, d.tgt_aid, d.tgt_ckey FROM dec d JOIN assrt a ON a.aid = d.aid
            WHERE a.tx <= $cut
            """;
        cmd.Parameters.AddWithValue("$cut", dataCut);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add((
                reader.GetString(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3)));
        }
        return result;
    }

    // ---- Why ----------------------------------------------------------------------------------

    public Explanation Why(string contextName, string aid)
    {
        var hw = HighWater;
        var ctx = BeliefFold.ResolveContext(_conn, contextName, hw);
        var outcome = BeliefFold.Compute(_conn, ctx);
        var justByAid = BeliefFold.LoadJust(_conn);
        return BuildExplanation(aid, ctx, outcome, justByAid, new HashSet<string>());
    }

    private Explanation BuildExplanation(string aid, ResolvedContext ctx, FoldOutcome outcome, Dictionary<string, List<Internal.JustRow>> justByAid, HashSet<string> visited)
    {
        string status;
        string? defeatedBy = null;

        if (!outcome.VisibleAssrt.TryGetValue(aid, out var row))
        {
            status = "not-visible";
        }
        else if (outcome.Entries.TryGetValue(aid, out var entry))
        {
            defeatedBy = entry.DefeaterAid;
            status = entry.Status switch
            {
                EntryStatus.Accepted => "accepted",
                EntryStatus.Defeated => "defeated",
                EntryStatus.Contested => "contested",
                EntryStatus.NotAdmitted => "proposed-unadmitted",
                _ => "not-visible",
            };
        }
        else
        {
            status = "not-visible";
        }

        var inputs = new List<Explanation>();
        var ruleVersions = new List<string>();

        if (justByAid.TryGetValue(aid, out var edges))
        {
            foreach (var rv in edges.Where(e => e.RuleVer is not null).Select(e => e.RuleVer!).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal))
            {
                ruleVersions.Add(rv);
            }

            if (visited.Count < 32)
            {
                foreach (var e in edges.Where(e => e.InputAid is not null).Select(e => e.InputAid!).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal))
                {
                    if (visited.Contains(e))
                    {
                        continue; // cycle-safe: do not re-descend into an ancestor already on the path
                    }
                    var childVisited = new HashSet<string>(visited) { e };
                    inputs.Add(BuildExplanation(e, ctx, outcome, justByAid, childVisited));
                }
            }
        }

        var decisions = new List<string>();
        var ckey = row?.CKey;
        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = """
                SELECT d.aid FROM dec d JOIN assrt a ON a.aid = d.aid
                WHERE a.tx <= $cut AND (d.tgt_aid = $aid OR ($ckey IS NOT NULL AND d.tgt_ckey = $ckey))
                ORDER BY d.aid
                """;
            cmd.Parameters.AddWithValue("$cut", ctx.DataCut);
            cmd.Parameters.AddWithValue("$aid", aid);
            cmd.Parameters.AddWithValue("$ckey", (object?)ckey ?? DBNull.Value);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                decisions.Add(reader.GetString(0));
            }
        }

        return new Explanation(aid, status, defeatedBy, inputs, ruleVersions, decisions);
    }

    // ---- GetAssertion ---------------------------------------------------------------------------

    /// <summary>Fetches a single assertion by aid (CONTRACT-V01.md section 3), independent of any
    /// context/fold; null if no assertion with that aid exists.</summary>
    public AssertionInfo? GetAssertion(string aid)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT aid, tx, subj, pred, val, valkind, ckey, status, src, meth, conf FROM assrt WHERE aid = $aid";
        cmd.Parameters.AddWithValue("$aid", aid);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new AssertionInfo(
            Aid: reader.GetString(0),
            Tx: reader.GetInt64(1),
            Subject: reader.GetString(2),
            Predicate: reader.GetString(3),
            Value: new GValue(reader.GetString(5), reader.GetString(4)),
            ClaimKey: reader.GetString(6),
            Status: reader.GetString(7),
            Source: reader.IsDBNull(8) ? null : reader.GetString(8),
            Method: reader.IsDBNull(9) ? null : reader.GetString(9),
            ConfidenceBp: reader.IsDBNull(10) ? null : reader.GetInt32(10));
    }

    // ---- CheckStale -----------------------------------------------------------------------------

    public StaleReport CheckStale(string receiptId)
    {
        long dataCut;
        List<string> consumedAids;
        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = "SELECT data_cut, consumed FROM receipt WHERE id = $id";
            cmd.Parameters.AddWithValue("$id", receiptId);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                throw new GneissException("NotFound", $"No receipt with id '{receiptId}'.");
            }
            dataCut = reader.GetInt64(0);
            var consumedRaw = reader.GetString(1);
            consumedAids = consumedRaw.Length == 0 ? new List<string>() : consumedRaw.Split(',').ToList();
        }

        var consumedSet = new HashSet<string>(consumedAids, StringComparer.Ordinal);
        var ckeys = new HashSet<string>(StringComparer.Ordinal);
        var declNames = new HashSet<(string Pred, string Subj)>();

        foreach (var aid in consumedAids)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT pred, subj, ckey FROM assrt WHERE aid = $aid";
            cmd.Parameters.AddWithValue("$aid", aid);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var pred = reader.GetString(0);
                var subj = reader.GetString(1);
                var ckey = reader.GetString(2);
                ckeys.Add(ckey);
                if (pred is "gneiss.context" or "gneiss.predicate")
                {
                    declNames.Add((pred, subj));
                }
            }
        }

        var causes = new List<string>();
        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = """
                SELECT a.aid, a.tx, a.pred, a.subj, a.ckey, d.tgt_aid, d.tgt_ckey
                FROM assrt a LEFT JOIN dec d ON d.aid = a.aid
                WHERE a.tx > $cut
                ORDER BY a.tx, a.aid
                """;
            cmd.Parameters.AddWithValue("$cut", dataCut);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var laterAid = reader.GetString(0);
                var laterTx = reader.GetInt64(1);
                var pred = reader.GetString(2);
                var subj = reader.GetString(3);
                var ckey = reader.GetString(4);
                var tgtAid = reader.IsDBNull(5) ? null : reader.GetString(5);
                var tgtCKey = reader.IsDBNull(6) ? null : reader.GetString(6);

                if (ckeys.Contains(ckey))
                {
                    causes.Add($"tx {laterTx} wrote assertion {laterAid} with a consumed ckey");
                }
                if (tgtAid is not null && consumedSet.Contains(tgtAid))
                {
                    causes.Add($"tx {laterTx} decision {laterAid} targets consumed aid {tgtAid}");
                }
                if (tgtCKey is not null && ckeys.Contains(tgtCKey))
                {
                    causes.Add($"tx {laterTx} decision {laterAid} targets consumed ckey");
                }
                if ((pred is "gneiss.context" or "gneiss.predicate") && declNames.Contains((pred, subj)))
                {
                    causes.Add($"tx {laterTx} re-declares consumed {pred} '{subj}'");
                }
            }
        }

        return new StaleReport(causes.Count > 0, causes);
    }

    // ---- Note ---------------------------------------------------------------------------------

    public string Note(TxEnvelope env, string text)
    {
        var id = Guid.NewGuid().ToString("n");
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "INSERT INTO note (id, wall, actor, text, promoted_aid) VALUES ($id, $wall, $actor, $text, NULL)";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$wall", Hashing.FormatWall(env.Wall));
        cmd.Parameters.AddWithValue("$actor", env.Actor);
        cmd.Parameters.AddWithValue("$text", text);
        cmd.ExecuteNonQuery();
        return id;
    }

    // ---- Export -------------------------------------------------------------------------------

    public IReadOnlyList<string> ExportLedgerJsonl()
    {
        var lines = new List<string>();

        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = "SELECT id, wall, actor, reason, batch FROM tx ORDER BY id";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lines.Add(CanonicalJsonWriter.ToJson(w => w.Obj(o => o
                    .Field("kind", "tx")
                    .FieldLong("id", reader.GetInt64(0))
                    .Field("wall", reader.GetString(1))
                    .Field("actor", reader.GetString(2))
                    .Field("reason", reader.GetString(3))
                    .FieldNullableString("batch", reader.IsDBNull(4) ? null : reader.GetString(4)))));
            }
        }

        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = "SELECT aid, tx, subj, pred, val, valkind, vfrom, vto, status, src, meth, conf, ckey FROM assrt ORDER BY tx, aid";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lines.Add(CanonicalJsonWriter.ToJson(w => w.Obj(o => o
                    .Field("kind", "assrt")
                    .Field("aid", reader.GetString(0))
                    .FieldLong("tx", reader.GetInt64(1))
                    .Field("subj", reader.GetString(2))
                    .Field("pred", reader.GetString(3))
                    .Field("val", reader.GetString(4))
                    .Field("valkind", reader.GetString(5))
                    .FieldNullableString("vfrom", reader.IsDBNull(6) ? null : reader.GetString(6))
                    .FieldNullableString("vto", reader.IsDBNull(7) ? null : reader.GetString(7))
                    .Field("status", reader.GetString(8))
                    .FieldNullableString("src", reader.IsDBNull(9) ? null : reader.GetString(9))
                    .FieldNullableString("meth", reader.IsDBNull(10) ? null : reader.GetString(10))
                    .FieldNullableInt("conf", reader.IsDBNull(11) ? null : reader.GetInt32(11))
                    .Field("ckey", reader.GetString(12)))));
            }
        }

        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = """
                SELECT d.aid, a.tx, d.kind, d.tgt_aid, d.tgt_ckey
                FROM dec d JOIN assrt a ON a.aid = d.aid
                ORDER BY a.tx, d.aid
                """;
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lines.Add(CanonicalJsonWriter.ToJson(w => w.Obj(o => o
                    .Field("kind", "dec")
                    .Field("aid", reader.GetString(0))
                    .FieldLong("tx", reader.GetInt64(1))
                    .Field("decisionKind", reader.GetString(2))
                    .FieldNullableString("tgtAid", reader.IsDBNull(3) ? null : reader.GetString(3))
                    .FieldNullableString("tgtCKey", reader.IsDBNull(4) ? null : reader.GetString(4)))));
            }
        }

        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = "SELECT aid, input_aid, rule_ver, role FROM just ORDER BY aid, COALESCE(input_aid, ''), COALESCE(rule_ver, ''), COALESCE(role, '')";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lines.Add(CanonicalJsonWriter.ToJson(w => w.Obj(o => o
                    .Field("kind", "just")
                    .Field("aid", reader.GetString(0))
                    .FieldNullableString("inputAid", reader.IsDBNull(1) ? null : reader.GetString(1))
                    .FieldNullableString("ruleVer", reader.IsDBNull(2) ? null : reader.GetString(2))
                    .FieldNullableString("role", reader.IsDBNull(3) ? null : reader.GetString(3)))));
            }
        }

        return lines;
    }

    public void Dispose()
    {
        _conn.Dispose();
    }
}
