using KodePorter.Core.Domain;
using KodePorter.Core.Model;
using KodePorter.Core.Store;

namespace KodePorter.Core.Absence;

/// <summary>One resolved absence classification — either the computed default or an override
/// from .kodeporter/absences.yaml (CONTRACT-M15.md §1.5).</summary>
public sealed record ResolvedAbsence(string SymbolPath, string Side, string Kind, string? Note, bool IsOverride);

/// <summary>
/// Computes typed absence classification per source entity (CONTRACT-M15.md §1.5): the COMPUTED
/// default for any eligible source entity (non-test fn/method/struct/enum/class not covered by
/// any unit anchor or correspondence) is `unknown`; overrides recorded in absences.yaml take
/// precedence. Target-only entities (same eligibility, target side) get the mirror
/// classification, default `unexplained`.
/// </summary>
public static class AbsenceCalculator
{
    /// <summary>The closed kind set eligible for absence classification, spanning both languages
    /// (CONTRACT-M15.md §1.5: "fn/method/struct/enum/class"). Each side naturally only produces
    /// entities of the kinds its language has (rust never emits `class`; C# never emits `fn`).</summary>
    private static readonly HashSet<string> EligibleKinds = new(StringComparer.Ordinal) { "fn", "method", "struct", "enum", "class" };

    public static IReadOnlyList<ResolvedAbsence> Compute(string workspaceDir, MapStore store)
    {
        ArgumentException.ThrowIfNullOrEmpty(workspaceDir);
        ArgumentNullException.ThrowIfNull(store);

        var overrides = AbsencesYaml.Read(workspaceDir)
            .ToDictionary(o => (o.Side, o.SymbolPath), o => o, new SideSymbolComparer());

        var sourceEntities = LatestEntities(store, BasisSide.Source);
        var targetEntities = LatestEntities(store, BasisSide.Target);
        var units = UnitYaml.ReadAll(workspaceDir);
        var correspondences = CorrespondencesYaml.Read(workspaceDir);

        // "not covered by any unit anchor or correspondence" (CONTRACT-M15.md §1.5) — no
        // provenance qualifier, so candidate correspondences count as coverage here too (they are
        // excluded from health's "corresponded" count, but a candidate link is still a link).
        var sourceCovered = units.SelectMany(u => u.SourceAnchors).Select(a => a.SymbolPath)
            .Concat(correspondences.Where(c => c.Source is not null).Select(c => c.Source!.SymbolPath))
            .ToHashSet(StringComparer.Ordinal);
        var targetCovered = units.SelectMany(u => u.TargetAnchors).Select(a => a.SymbolPath)
            .Concat(correspondences.Where(c => c.Target is not null).Select(c => c.Target!.SymbolPath))
            .ToHashSet(StringComparer.Ordinal);

        var result = new List<ResolvedAbsence>();

        foreach (var e in sourceEntities
            .Where(e => EligibleKinds.Contains(e.Kind) && !e.IsTest && !sourceCovered.Contains(e.SymbolPath))
            .OrderBy(e => e.SymbolPath, StringComparer.Ordinal))
        {
            result.Add(overrides.TryGetValue(("source", e.SymbolPath), out var o)
                ? new ResolvedAbsence(e.SymbolPath, "source", o.Kind, o.Note, true)
                : new ResolvedAbsence(e.SymbolPath, "source", "unknown", null, false));
        }

        foreach (var e in targetEntities
            .Where(e => EligibleKinds.Contains(e.Kind) && !e.IsTest && !targetCovered.Contains(e.SymbolPath))
            .OrderBy(e => e.SymbolPath, StringComparer.Ordinal))
        {
            result.Add(overrides.TryGetValue(("target", e.SymbolPath), out var o)
                ? new ResolvedAbsence(e.SymbolPath, "target", o.Kind, o.Note, true)
                : new ResolvedAbsence(e.SymbolPath, "target", "unexplained", null, false));
        }

        return result;
    }

    private static IReadOnlyList<Entity> LatestEntities(MapStore store, BasisSide side)
    {
        var bases = store.ListBases(side);
        return bases.Count == 0 ? Array.Empty<Entity>() : store.GetEntities(bases[^1].Id);
    }

    private sealed class SideSymbolComparer : IEqualityComparer<(string Side, string SymbolPath)>
    {
        public bool Equals((string Side, string SymbolPath) x, (string Side, string SymbolPath) y) =>
            string.Equals(x.Side, y.Side, StringComparison.Ordinal) && string.Equals(x.SymbolPath, y.SymbolPath, StringComparison.Ordinal);
        public int GetHashCode((string Side, string SymbolPath) obj) =>
            HashCode.Combine(StringComparer.Ordinal.GetHashCode(obj.Side), StringComparer.Ordinal.GetHashCode(obj.SymbolPath));
    }
}
