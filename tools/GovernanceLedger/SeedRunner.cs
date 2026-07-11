using Gneiss.Cell;

namespace GovernanceLedger;

/// <summary>
/// Runs <see cref="SeedTable"/> against a freshly created ledger: declares the `gov.decision`
/// predicate + `gov-current` context, then appends the nine seed entries in order (CONTRACT-M15.md
/// section 7). Every Append call carries exactly one item — never batched — so each transaction's
/// sole assertion/decision always has ordinal 0; `rebuild` (RebuildOp.cs) depends on this to
/// reconstruct content without needing to persist per-tx ordinals in the export.
/// </summary>
internal static class SeedRunner
{
    public static void Run(GneissLedger ledger, DateTimeOffset? wallOverride)
    {
        var bootWall = wallOverride ?? SeedTable.BootstrapWall;
        var bootEnv = new TxEnvelope("governance-ledger", "bootstrap: declare gov.decision predicate and gov-current context", bootWall);
        ledger.DeclarePredicate(bootEnv, new PredicateDecl(LedgerPaths.PredGovDecision, Comparator: "exact", StopRung: 6));
        ledger.DeclareContext(bootEnv, new ContextDecl(LedgerPaths.GovContextName, Admit: "decided-only"));

        var capturedAids = new Dictionary<string, string>(StringComparer.Ordinal);

        for (int i = 0; i < SeedTable.Decisions.Count; i++)
        {
            AppendDecision(ledger, SeedTable.Decisions[i], wallOverride, capturedAids);

            if (i == SeedTable.SupersessionAfterIndex)
            {
                AppendSupersession(ledger, SeedTable.Supersession, wallOverride, capturedAids);
            }
        }
    }

    private static void AppendDecision(GneissLedger ledger, SeedDecision d, DateTimeOffset? wallOverride, Dictionary<string, string> capturedAids)
    {
        var wall = wallOverride ?? d.Wall;
        var env = new TxEnvelope(d.Actor, TextNorm.Collapse(d.Reason), wall);
        var na = new NewAssertion(
            Subject: LedgerPaths.DecisionSubject(d.Id),
            Predicate: LedgerPaths.PredGovDecision,
            Value: GValue.Text(d.ValueText),
            ValidFrom: d.ValidFrom,
            Proposed: false,
            Source: $"commit:{d.CommitHash}",
            Method: d.Method);
        var result = ledger.Append(env, [na]);
        capturedAids[d.Id] = result.Aids[0];
    }

    private static void AppendSupersession(GneissLedger ledger, SeedSupersession s, DateTimeOffset? wallOverride, Dictionary<string, string> capturedAids)
    {
        var wall = wallOverride ?? s.Wall;
        var env = new TxEnvelope(s.Actor, TextNorm.Collapse(s.Reason), wall);
        if (!capturedAids.TryGetValue(s.TargetId, out var targetAid))
            throw new InvalidOperationException($"Seed supersession '{s.Id}' targets unknown seed id '{s.TargetId}' (must be seeded earlier).");
        var nd = new NewDecision(DecisionKind.Supersedes, TargetAid: targetAid);
        var result = ledger.Append(env, [nd]);
        capturedAids[s.Id] = result.Aids[0];
    }
}
