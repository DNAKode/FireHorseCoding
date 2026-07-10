using System.Text.Json;
using Gneiss.Cell;
using KodePorter.Core.Domain;
using KodePorter.Core.Gneiss;
using KodePorter.Core.Model;
using KodePorter.Core.Store;

namespace KodePorter.Core.Atlas;

/// <summary>The six K-D12 health dimensions (CONTRACT.md §8 health strip / §9 `kp status`), in the contract's order.</summary>
public sealed record HealthReport(
    int Mapped,
    int Corresponded,
    int Implemented,
    int Verified,
    int Stale,
    int Unknown);

/// <summary>
/// Computes <see cref="HealthReport"/> — shared by the Atlas health strip and `kp status`'s
/// plain-text output, so the two never drift (CONTRACT.md §8/§9: "same six numbers as the
/// Atlas"). Definitions (CONTRACT.md §8):
///   mapped        = entity count in the current (latest-pinned) basis of each side, summed.
///   corresponded  = entities in the current bases whose symbolPath is referenced by any
///                   correspondence's source/target anchor.
///   implemented   = units with at least one targetAnchor.
///   verified      = units with an ACCEPTED kp.verification claim whose verdict is "pass",
///                   for any criterion used by one of the unit's correspondences.
///   stale         = accepted kp.stale facts currently in force (Ask("kp-current", ...)).
///   unknown       = current-basis SOURCE entities of kind fn/method/struct/enum that are
///                   referenced by neither a unit's sourceAnchors nor any correspondence.
/// </summary>
public static class HealthCalculator
{
    private static readonly HashSet<string> UnknownEligibleKinds = new(StringComparer.Ordinal) { "fn", "method", "struct", "enum" };

    public static HealthReport Compute(string workspaceDir, MapStore store, GneissBinding binding)
    {
        ArgumentException.ThrowIfNullOrEmpty(workspaceDir);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(binding);

        var sourceEntities = LatestEntities(store, BasisSide.Source);
        var targetEntities = LatestEntities(store, BasisSide.Target);

        int mapped = sourceEntities.Count + targetEntities.Count;

        var correspondences = CorrespondencesYaml.Read(workspaceDir);
        var sourceCorrSymbols = correspondences.Where(c => c.Source is not null).Select(c => c.Source!.SymbolPath).ToHashSet(StringComparer.Ordinal);
        var targetCorrSymbols = correspondences.Where(c => c.Target is not null).Select(c => c.Target!.SymbolPath).ToHashSet(StringComparer.Ordinal);

        int corresponded = sourceEntities.Count(e => sourceCorrSymbols.Contains(e.SymbolPath))
                          + targetEntities.Count(e => targetCorrSymbols.Contains(e.SymbolPath));

        var units = UnitYaml.ReadAll(workspaceDir);
        int implemented = units.Count(u => u.TargetAnchors.Count > 0);

        var view = binding.CurrentView();

        int verified = units.Count(u => IsVerifiedPass(u, correspondences, view));

        int stale = view.Accepted.Count(e => e.Predicate == GneissBinding.PredStale);

        var unitSourceSymbols = units.SelectMany(u => u.SourceAnchors).Select(a => a.SymbolPath).ToHashSet(StringComparer.Ordinal);
        int unknown = sourceEntities.Count(e =>
            UnknownEligibleKinds.Contains(e.Kind) &&
            !unitSourceSymbols.Contains(e.SymbolPath) &&
            !sourceCorrSymbols.Contains(e.SymbolPath));

        return new HealthReport(mapped, corresponded, implemented, verified, stale, unknown);
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
