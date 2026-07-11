using System.Text.Json;
using Gneiss.Cell;
using KodePorter.Core.Absence;
using KodePorter.Core.Domain;
using KodePorter.Core.Gneiss;
using KodePorter.Core.Model;
using KodePorter.Core.Store;

namespace KodePorter.Core.Atlas;

/// <summary>Source-side absence breakdown (CONTRACT-M15.md §1.5/§1.7), excluding is_test entities.</summary>
public sealed record AbsenceBreakdown(int Unknown, int NotYetPorted, int DeliberatelyDropped);

/// <summary>Target-side "target-only" breakdown (CONTRACT-M15.md §1.5/§1.7), excluding is_test entities.</summary>
public sealed record TargetOnlyBreakdown(int Unexplained, int Intentional);

/// <summary>Health v2 (CONTRACT-M15.md §1.7): the K-D12 health strip's dimensions, extended with
/// the imperfection vocabulary. `kp status` prints all; the Atlas shows all.</summary>
/// <param name="Mapped">Entity count in the current (latest-pinned) basis of each side, summed.</param>
/// <param name="Corresponded">Entities in the current bases referenced by an `asserted` (or
/// verified-derived-from-asserted) correspondence's source/target anchor — `candidate`
/// correspondences are counted separately, never here.</param>
/// <param name="Candidates">Count of `candidate`-provenance correspondences.</param>
/// <param name="Implemented">Units with at least one targetAnchor.</param>
/// <param name="Verified">Units with an accepted kp.verification claim whose verdict is "pass",
/// for any criterion used by one of the unit's correspondences.</param>
/// <param name="Stale">Accepted kp.stale facts currently in force (Ask("kp-current", ...)).</param>
/// <param name="Absence">Source-side absence breakdown (CONTRACT-M15.md §1.5), excluding tests.</param>
/// <param name="TargetOnly">Target-side "target-only" breakdown, excluding tests.</param>
public sealed record HealthReport(
    int Mapped,
    int Corresponded,
    int Candidates,
    int Implemented,
    int Verified,
    int Stale,
    AbsenceBreakdown Absence,
    TargetOnlyBreakdown TargetOnly);

/// <summary>
/// Computes <see cref="HealthReport"/> — shared by the Atlas health strip and `kp status`'s
/// plain-text output, so the two never drift (CONTRACT.md §8/§9: "same numbers as the Atlas").
/// </summary>
public static class HealthCalculator
{
    public static HealthReport Compute(string workspaceDir, MapStore store, GneissBinding binding)
    {
        ArgumentException.ThrowIfNullOrEmpty(workspaceDir);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(binding);

        var sourceEntities = LatestEntities(store, BasisSide.Source);
        var targetEntities = LatestEntities(store, BasisSide.Target);

        int mapped = sourceEntities.Count + targetEntities.Count;

        var correspondences = CorrespondencesYaml.Read(workspaceDir);
        // CONTRACT-M15.md §1.7: "corresponded (asserted/verified correspondences only)" —
        // `verified` is a derived display state of an already-`asserted` row (never a distinct
        // stored provenance), so filtering to non-`candidate` rows covers both.
        var assertedCorrespondences = correspondences.Where(c => c.Provenance != "candidate").ToList();
        var sourceCorrSymbols = assertedCorrespondences.Where(c => c.Source is not null).Select(c => c.Source!.SymbolPath).ToHashSet(StringComparer.Ordinal);
        var targetCorrSymbols = assertedCorrespondences.Where(c => c.Target is not null).Select(c => c.Target!.SymbolPath).ToHashSet(StringComparer.Ordinal);

        int corresponded = sourceEntities.Count(e => sourceCorrSymbols.Contains(e.SymbolPath))
                          + targetEntities.Count(e => targetCorrSymbols.Contains(e.SymbolPath));

        int candidates = correspondences.Count(c => c.Provenance == "candidate");

        var units = UnitYaml.ReadAll(workspaceDir);
        int implemented = units.Count(u => u.TargetAnchors.Count > 0);

        var view = binding.CurrentView();

        int verified = units.Count(u => IsVerifiedPass(u, correspondences, view));

        int stale = view.Accepted.Count(e => e.Predicate == GneissBinding.PredStale);

        var resolvedAbsences = AbsenceCalculator.Compute(workspaceDir, store);
        var sourceAbsences = resolvedAbsences.Where(r => r.Side == "source").ToList();
        var absence = new AbsenceBreakdown(
            Unknown: sourceAbsences.Count(r => r.Kind == "unknown"),
            NotYetPorted: sourceAbsences.Count(r => r.Kind == "not-yet-ported"),
            DeliberatelyDropped: sourceAbsences.Count(r => r.Kind == "deliberately-dropped"));
        var targetAbsences = resolvedAbsences.Where(r => r.Side == "target").ToList();
        var targetOnly = new TargetOnlyBreakdown(
            Unexplained: targetAbsences.Count(r => r.Kind == "unexplained"),
            Intentional: targetAbsences.Count(r => r.Kind == "intentional"));

        return new HealthReport(mapped, corresponded, candidates, implemented, verified, stale, absence, targetOnly);
    }

    private static IReadOnlyList<Entity> LatestEntities(MapStore store, BasisSide side)
    {
        var bases = store.ListBases(side);
        return bases.Count == 0 ? Array.Empty<Entity>() : store.GetEntities(bases[^1].Id);
    }

    private static bool IsVerifiedPass(UnitDoc unit, IReadOnlyList<Correspondence> correspondences, BeliefView view)
    {
        var criteria = correspondences
            .Where(c => c.Unit == unit.Id && c.Criterion is not null)
            .Select(c => c.Criterion!)
            .Distinct(StringComparer.Ordinal);

        foreach (string criterion in criteria)
        {
            string subject = GneissBinding.VerificationSubject(unit.Id, criterion);
            bool acceptedPass = view.Accepted.Any(e =>
                e.Subject == subject &&
                e.Predicate == GneissBinding.PredVerification &&
                HasPassVerdict(e.Value));
            if (acceptedPass)
                return true;
        }
        return false;
    }

    private static bool HasPassVerdict(GValue value)
    {
        if (value.Kind != "json")
            return false;
        try
        {
            using var doc = JsonDocument.Parse(value.Canonical);
            return doc.RootElement.TryGetProperty("verdict", out var verdictProp) &&
                   verdictProp.GetString() == "pass";
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
