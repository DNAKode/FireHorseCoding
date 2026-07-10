using System.Text;
using KodePorter.Core.Diff;
using KodePorter.Core.Domain;
using KodePorter.Core.Gneiss;
using KodePorter.Core.Model;
using KodePorter.Core.Providers;
using KodePorter.Core.Store;

namespace KodePorter.Core.Advance;

/// <summary>The result of one `kp advance` (CONTRACT.md §7).</summary>
public sealed record AdvanceReport(
    BasisDiffResult Diff,
    IReadOnlyList<string> StaleCorrespondenceIds,
    IReadOnlyList<string> StaleUnitIds,
    IReadOnlyList<string> StaleClaimSubjects,
    string ReportPath);

/// <summary>
/// K6-lite advance + staleness (CONTRACT.md §7): pins and imports a new basis, diffs it against
/// the side's previous basis, and marks anchor-drift staleness — in Gneiss (kp.stale facts), in
/// the yaml (stale: true, files stay readable), and in a delta report. Staleness v0 is ANCHOR
/// DRIFT ONLY, honestly labeled as such (never presented as semantic re-verification).
/// </summary>
public static class AdvanceService
{
    public static AdvanceReport Advance(
        string workspaceDir,
        MapStore store,
        GneissBinding binding,
        BasisSide side,
        string newRoot,
        string label,
        string? dumpPath,
        string? analyzer,
        DateTimeOffset timestamp,
        string actor,
        string reason)
    {
        var previousBases = store.ListBases(side);
        var previousBasis = previousBases.Count > 0 ? previousBases[^1] : null;

        var newBasis = BasisPinner.Pin(store, side, newRoot, label, analyzer: analyzer, created: timestamp);

        if (side == BasisSide.Source)
        {
            if (string.IsNullOrEmpty(dumpPath))
                throw new ArgumentException("A --dump path is required to advance the source (rust) side.", nameof(dumpPath));
            new RustSynProvider().Import(store, newBasis, dumpPath);
        }
        else
        {
            new CSharpRoslynProvider().Import(store, newBasis);
        }

        var diff = previousBasis is null
            ? new BasisDiffResult([], [], [])
            : BasisDiffService.Diff(store, previousBasis.Id, newBasis.Id);

        // Anchor-drift signal (CONTRACT.md §7 step 1/2): a symbolPath whose current content_hash
        // differs from what an anchor recorded, or that has disappeared entirely.
        var changedSymbolPaths = diff.Changed.Select(e => e.SymbolPath)
            .Concat(diff.Removed.Select(e => e.SymbolPath))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();
        var changedSet = new HashSet<string>(changedSymbolPaths, StringComparer.Ordinal);

        var staleValue = new StaleValue(label, "anchor-drift", changedSymbolPaths);

        // ---- Correspondences (step 2) ----------------------------------------------------------
        var correspondences = CorrespondencesYaml.Read(workspaceDir);
        var staleCorrIds = new List<string>();
        var updatedCorrespondences = new List<Correspondence>(correspondences.Count);
        foreach (var c in correspondences)
        {
            var anchorOnThisSide = side == BasisSide.Source ? c.Source : c.Target;
            bool becomesStale = anchorOnThisSide is not null && changedSet.Contains(anchorOnThisSide.SymbolPath);
            if (becomesStale && !c.Stale)
            {
                staleCorrIds.Add(c.Id);
                updatedCorrespondences.Add(c with { Stale = true });
            }
            else
            {
                updatedCorrespondences.Add(c);
            }
        }
        if (staleCorrIds.Count > 0)
            CorrespondencesYaml.Write(workspaceDir, updatedCorrespondences);
        staleCorrIds = [.. staleCorrIds.OrderBy(x => x, StringComparer.Ordinal)];

        // ---- Units + their claims (step 2/3) ---------------------------------------------------
        var units = UnitYaml.ReadAll(workspaceDir);
        var staleUnitIds = new List<string>();
        foreach (var u in units)
        {
            var anchors = side == BasisSide.Source ? u.SourceAnchors : u.TargetAnchors;
            bool becomesStale = anchors.Any(a => changedSet.Contains(a.SymbolPath));
            if (becomesStale && !u.Stale)
            {
                staleUnitIds.Add(u.Id);
                UnitYaml.Write(workspaceDir, u with { Stale = true });
            }
        }
        staleUnitIds = [.. staleUnitIds.OrderBy(x => x, StringComparer.Ordinal)];

        // ---- Gneiss kp.stale facts (corr:/unit:/verify: subjects) ------------------------------
        var staleClaimSubjects = new List<string>();
        foreach (string unitId in staleUnitIds)
        {
            string unitSubject = GneissBinding.UnitSubject(unitId);
            binding.AssertStale(unitSubject, staleValue, actor, reason);
            staleClaimSubjects.Add(unitSubject);

            var criteria = correspondences
                .Where(c => c.Unit == unitId && c.Criterion is not null)
                .Select(c => c.Criterion!)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal);
            foreach (string criterion in criteria)
            {
                string verifySubject = GneissBinding.VerificationSubject(unitId, criterion);
                binding.AssertStale(verifySubject, staleValue, actor, reason);
                staleClaimSubjects.Add(verifySubject);
            }
        }
        foreach (string corrId in staleCorrIds)
        {
            string corrSubject = GneissBinding.CorrespondenceSubject(corrId);
            binding.AssertStale(corrSubject, staleValue, actor, reason);
            staleClaimSubjects.Add(corrSubject);
        }

        // ---- Behavior claims (per-claim anchor drift; subjects behavior:<unit>:<id>) -----------
        // A claim's evidence anchors are its Gneiss justification inputs (kp.evidence.anchor
        // assertions carrying the symbolPath). If any anchored symbol changed or vanished, the
        // claim's evidence needs re-checking: mark the claim itself stale. This deliberately
        // over-approximates (anchor drift, not semantic impact) — cone precision is a measured
        // number, not a promise (K-D11).
        var ledgerIndex = Atlas.LedgerIndex.Build(binding.ExportLedgerJsonl());
        foreach (var u in units.OrderBy(u => u.Id, StringComparer.Ordinal))
        {
            foreach (string claimAid in u.Claims)
            {
                if (!ledgerIndex.AssrtByAid.TryGetValue(claimAid, out var claimAssrt) ||
                    claimAssrt.Predicate != GneissBinding.PredBehavior ||
                    !ledgerIndex.JustInputsByAid.TryGetValue(claimAid, out var inputs))
                    continue;

                bool drifted = false;
                foreach (string inputAid in inputs)
                {
                    if (!ledgerIndex.AssrtByAid.TryGetValue(inputAid, out var anchor) ||
                        anchor.Predicate != GneissBinding.PredEvidenceAnchor)
                        continue;
                    using var doc = System.Text.Json.JsonDocument.Parse(anchor.Value);
                    if (doc.RootElement.TryGetProperty("symbolPath", out var spEl) &&
                        spEl.ValueKind == System.Text.Json.JsonValueKind.String &&
                        changedSet.Contains(spEl.GetString()!))
                    {
                        drifted = true;
                        break;
                    }
                }

                if (drifted)
                {
                    binding.AssertStale(claimAssrt.Subject, staleValue, actor, reason);
                    staleClaimSubjects.Add(claimAssrt.Subject);
                }
            }
        }
        staleClaimSubjects = [.. staleClaimSubjects.OrderBy(x => x, StringComparer.Ordinal)];

        string runsDir = Path.Combine(workspaceDir, "runs");
        Directory.CreateDirectory(runsDir);
        string reportPath = Path.Combine(runsDir, $"advance-{label}.md");
        WriteReport(reportPath, side, label, diff, staleCorrIds, staleUnitIds, staleClaimSubjects);

        return new AdvanceReport(diff, staleCorrIds, staleUnitIds, staleClaimSubjects, reportPath);
    }

    private static void WriteReport(
        string path, BasisSide side, string label, BasisDiffResult diff,
        IReadOnlyList<string> staleCorrIds, IReadOnlyList<string> staleUnitIds, IReadOnlyList<string> staleClaimSubjects)
    {
        var sb = new StringBuilder();
        sb.Append("# Advance: ").Append(side.ToWireString()).Append(" -> ").Append(label).Append("\n\n");

        sb.Append("## Entity diff\n\n");
        sb.Append("- Added: ").Append(diff.Added.Count).Append('\n');
        sb.Append("- Removed: ").Append(diff.Removed.Count).Append('\n');
        sb.Append("- Changed: ").Append(diff.Changed.Count).Append("\n\n");

        AppendEntries(sb, "Added", diff.Added);
        AppendEntries(sb, "Removed", diff.Removed);
        AppendEntries(sb, "Changed", diff.Changed);

        sb.Append("## Cone (marked stale — anchor drift only)\n\n");
        AppendIds(sb, "Units", staleUnitIds);
        AppendIds(sb, "Correspondences", staleCorrIds);
        AppendIds(sb, "Claims", staleClaimSubjects);

        DomainFileIo.WriteLf(path, sb.ToString());
    }

    private static void AppendEntries(StringBuilder sb, string title, IReadOnlyList<EntityDiffEntry> entries)
    {
        sb.Append("### ").Append(title).Append('\n');
        if (entries.Count == 0)
        {
            sb.Append("(none)\n\n");
            return;
        }
        foreach (var e in entries.OrderBy(x => x.SymbolPath, StringComparer.Ordinal))
            sb.Append("- ").Append(e.Kind).Append(' ').Append(e.SymbolPath).Append('\n');
        sb.Append('\n');
    }

    private static void AppendIds(StringBuilder sb, string title, IReadOnlyList<string> ids)
    {
        sb.Append("### ").Append(title).Append('\n');
        if (ids.Count == 0)
        {
            sb.Append("(none)\n\n");
            return;
        }
        foreach (string id in ids)
            sb.Append("- ").Append(id).Append('\n');
        sb.Append('\n');
    }
}
