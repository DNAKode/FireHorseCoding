using System.Text;
using KodePorter.Core.Atlas;
using KodePorter.Core.Domain;
using KodePorter.Core.Gneiss;

namespace KodePorter.Core.Export;

/// <summary>
/// The Port Atlas floor (K-V, CONTRACT.md §9): `kp export` emits PORTING.md from the domain files
/// plus the CURRENT BELIEF VIEW (statuses from Ask, never from the yaml alone) — honest and current.
/// </summary>
public static class ExportService
{
    public static string Export(string workspaceDir, GneissBinding binding, string outPath)
    {
        var project = ProjectYaml.Read(workspaceDir);
        var units = UnitYaml.ReadAll(workspaceDir);
        var correspondences = CorrespondencesYaml.Read(workspaceDir);

        PolicyDoc? policy = File.Exists(PolicyYaml.FilePath(workspaceDir)) ? PolicyYaml.Read(workspaceDir) : null;

        var sb = new StringBuilder();
        sb.Append("# ").Append(project.Name).Append(" — Port Atlas (KP-0 floor)\n\n");
        sb.Append("- Direction: ").Append(project.Direction).Append('\n');
        sb.Append("- Source root: ").Append(project.SourceRoot).Append('\n');
        sb.Append("- Target root: ").Append(project.TargetRoot).Append("\n\n");

        if (policy is not null)
        {
            sb.Append("## Policy\n\n");
            sb.Append("- ").Append(policy.Name).Append('@').Append(policy.Version).Append('\n');
            foreach (var kv in policy.AutoAccept.OrderBy(k => k.Key, StringComparer.Ordinal))
                sb.Append("  - autoAccept.").Append(kv.Key).Append(": ").Append(kv.Value ? "true" : "false").Append('\n');
            sb.Append('\n');
        }

        var index = LedgerIndex.Build(binding.ExportLedgerJsonl());

        sb.Append("## Units\n\n");
        if (units.Count == 0)
            sb.Append("(none)\n\n");
        foreach (var u in units.OrderBy(u => u.Id, StringComparer.Ordinal))
        {
            sb.Append("### ").Append(u.Id).Append(" — ").Append(u.Name).Append('\n');
            sb.Append("- status: ").Append(u.Status).Append(u.Stale ? " (stale)" : "").Append('\n');
            var claimSubjects = BehaviorSubjectsFor(index, u);
            if (claimSubjects.Count == 0)
                sb.Append("- kp.behavior claims: (none)\n");
            foreach (string subject in claimSubjects)
                sb.Append("- ").Append(subject).Append(": ").Append(ClaimStatus(binding, subject, GneissBinding.PredBehavior)).Append('\n');
            sb.Append('\n');
        }

        sb.Append("## Correspondences\n\n");
        if (correspondences.Count == 0)
            sb.Append("(none)\n\n");
        foreach (var c in correspondences.OrderBy(c => c.Id, StringComparer.Ordinal))
        {
            // ClaimStatus already carries the subject-level "(stale)" suffix; the yaml-level
            // c.Stale flag is written in lockstep with it by AdvanceService, so appending both
            // would print "(stale) (stale)".
            string claimStatus = ClaimStatus(binding, GneissBinding.CorrespondenceSubject(c.Id), GneissBinding.PredCorrespondence);
            sb.Append("- ").Append(c.Id).Append(" [").Append(c.Type).Append("] unit=").Append(c.Unit)
              .Append(" kp.correspondence claim=").Append(claimStatus).Append('\n');
        }
        sb.Append('\n');

        sb.Append("## Claims\n\n");
        sb.Append("### Behavior (kp.behavior)\n\n");
        bool anyBehavior = false;
        foreach (var u in units.OrderBy(u => u.Id, StringComparer.Ordinal))
        {
            foreach (string subject in BehaviorSubjectsFor(index, u))
            {
                anyBehavior = true;
                sb.Append("- ").Append(subject).Append(" -> ")
                  .Append(ClaimStatus(binding, subject, GneissBinding.PredBehavior)).Append('\n');
            }
        }
        if (!anyBehavior)
            sb.Append("(none)\n");
        sb.Append('\n');

        sb.Append("### Correspondence (kp.correspondence)\n\n");
        foreach (var c in correspondences.OrderBy(c => c.Id, StringComparer.Ordinal))
            sb.Append("- ").Append(GneissBinding.CorrespondenceSubject(c.Id)).Append(" -> ")
              .Append(ClaimStatus(binding, GneissBinding.CorrespondenceSubject(c.Id), GneissBinding.PredCorrespondence)).Append('\n');
        sb.Append('\n');

        var verifySubjects = correspondences
            .Where(c => c.Criterion is not null)
            .Select(c => (c.Unit, Criterion: c.Criterion!))
            .Distinct()
            .OrderBy(t => t.Unit, StringComparer.Ordinal)
            .ThenBy(t => t.Criterion, StringComparer.Ordinal)
            .ToList();
        sb.Append("### Verification (kp.verification)\n\n");
        if (verifySubjects.Count == 0)
            sb.Append("(none)\n\n");
        foreach (var (unitId, criterion) in verifySubjects)
        {
            string subject = GneissBinding.VerificationSubject(unitId, criterion);
            sb.Append("- ").Append(subject).Append(" -> ").Append(ClaimStatus(binding, subject, GneissBinding.PredVerification)).Append('\n');
        }
        sb.Append('\n');

        DomainFileIo.WriteLf(outPath, sb.ToString());
        return outPath;
    }

    /// <summary>
    /// Status of the claim with <paramref name="predicate"/> on <paramref name="subject"/>.
    /// MUST filter by predicate: a subject's view contains every assertion about it — notably
    /// kp.stale FACTS, which are accepted the moment they are asserted. Without the filter,
    /// every stale-marked subject reads as "accepted" regardless of its actual claim status
    /// (found by the M1 story, 2026-07-10: undecided B3 displayed as accepted purely because
    /// it had been stale-marked). Staleness is reported alongside, honestly and separately.
    /// </summary>
    private static string ClaimStatus(GneissBinding binding, string subject, string predicate)
    {
        var view = binding.AskClaim(subject);
        bool stale = view.Accepted.Any(e => e.Predicate == GneissBinding.PredStale);
        string status;
        if (view.Accepted.Any(e => e.Predicate == predicate)) status = "accepted";
        else if (view.Defeated.Any(e => e.Predicate == predicate)) status = "defeated";
        else if (view.Contested.Count > 0) status = "contested";
        else status = "proposed";
        return stale ? status + " (stale)" : status;
    }

    /// <summary>The unit's kp.behavior claim subjects (behavior:&lt;unit&gt;:&lt;id&gt;), resolved from the
    /// unit's recorded claim aids via the ledger export, in deterministic subject order.</summary>
    private static List<string> BehaviorSubjectsFor(LedgerIndex index, UnitDoc u) =>
        u.Claims
            .Where(aid => index.AssrtByAid.TryGetValue(aid, out var a) && a.Predicate == GneissBinding.PredBehavior)
            .Select(aid => index.AssrtByAid[aid].Subject)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();
}
