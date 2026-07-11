using Gneiss.Cell;

namespace GovernanceLedger;

/// <summary>`record --dir --actor --reason --subject --predicate --value --wall [--decide
/// accept|reject|retract|supersede --target &lt;subject&gt;]` (CONTRACT-M15.md section 7):
/// appends one governed decision. `--wall` is required — this tool never calls
/// DateTimeOffset.UtcNow; the caller supplies the fixed instant.</summary>
internal static class RecordOp
{
    public sealed record RecordResult(string Aid, string? DecisionAid);

    public static RecordResult Run(ArgReader a)
    {
        string dir = a.Require("dir");
        string dbPath = LedgerPaths.DbPath(dir);
        if (!File.Exists(dbPath))
            throw new CliDomainException($"No ledger at '{dbPath}'. Run 'seed --dir {dir}' first.");

        string actor = a.Require("actor");
        string reason = TextNorm.Collapse(a.Require("reason"));
        string subject = a.Require("subject");
        string predicate = a.Require("predicate");
        string value = a.Require("value");
        var wall = WallClock.Parse(a.Require("wall"));
        var validFrom = a.Optional("valid-from") is { } vf ? WallClock.Parse(vf) : (DateTimeOffset?)null;
        string? source = a.Optional("source");
        string? method = a.Optional("method");
        bool proposed = a.Switch("proposed");

        using var ledger = GneissLedger.Open(dbPath);

        var env = new TxEnvelope(actor, reason, wall);
        var na = new NewAssertion(subject, predicate, GValue.Text(value),
            ValidFrom: validFrom, Proposed: proposed, Source: source, Method: method);
        var result = ledger.Append(env, [na]);
        string aid = result.Aids[0];

        string? decisionAid = null;
        string? decide = a.Optional("decide");
        if (decide is not null)
        {
            string target = a.Optional("target")
                ?? throw new CliUsageException("--decide requires --target <subject>.");
            var kind = ParseDecisionKind(decide);

            string targetAid;
            if (string.Equals(target, subject, StringComparison.Ordinal))
            {
                // Self-decide: accept/reject/retract/supersede the assertion just recorded above.
                targetAid = aid;
            }
            else
            {
                var view = ledger.Ask(LedgerPaths.GovContextName, new Question(Subject: target));
                var candidates = view.Accepted.Concat(view.Defeated).ToList();
                if (candidates.Count == 0)
                    throw new CliDomainException($"--target '{target}' matches no visible assertion in '{LedgerPaths.GovContextName}'.");
                if (candidates.Count > 1)
                    throw new CliDomainException($"--target '{target}' is ambiguous ({candidates.Count} matches); target the assertion's own subject precisely.");
                targetAid = candidates[0].Aid;
            }

            var decideEnv = new TxEnvelope(actor, reason, wall);
            var nd = new NewDecision(kind, TargetAid: targetAid);
            var decideResult = ledger.Append(decideEnv, [nd]);
            decisionAid = decideResult.Aids[0];
        }

        return new RecordResult(aid, decisionAid);
    }

    private static DecisionKind ParseDecisionKind(string raw) => raw switch
    {
        "accept" => DecisionKind.Accepts,
        "reject" => DecisionKind.Rejects,
        "retract" => DecisionKind.Retracts,
        "supersede" => DecisionKind.Supersedes,
        _ => throw new CliUsageException($"--decide must be one of accept|reject|retract|supersede (got '{raw}')."),
    };
}
