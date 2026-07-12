using KodePorter.Core.Model;
using KodePorter.Core.Store;

namespace KodePorter.Core.Providers;

/// <summary>
/// Post-processing shared by both providers: deterministic ordering (fixtures §6: sorted by
/// (file, startLine, symbolPath)), de-duplication by (kind, symbolPath) — the schema's entity
/// identity — and resolution of `parentSymbolPath` to a parent entity id within the same import.
/// </summary>
internal static class EntityResolution
{
    /// <summary>
    /// Sorts by (file, startLine, symbolPath) and drops later entities whose (kind, symbolPath)
    /// duplicates one already kept (e.g. a partial type's second declaration; the schema can only
    /// hold one row per (side, kind, symbolPath)). The first occurrence in sorted order wins.
    /// Equivalent to <see cref="SortAndDeduplicate(IEnumerable{DumpEntity}, out int)"/> with the
    /// dropped-duplicate count discarded, for callers that don't need to report it.
    /// </summary>
    public static List<DumpEntity> SortAndDeduplicate(IEnumerable<DumpEntity> candidates)
        => SortAndDeduplicate(candidates, out _);

    /// <summary>
    /// As the single-argument overload, and additionally reports via
    /// <paramref name="droppedDuplicateCount"/> how many candidates were discarded as (kind,
    /// symbolPath) duplicates — surfaced by callers (<c>ImportResult.DroppedDuplicateCount</c>)
    /// rather than silently vanishing (PROBE-REPORT.md §7 finding #2).
    /// </summary>
    public static List<DumpEntity> SortAndDeduplicate(IEnumerable<DumpEntity> candidates, out int droppedDuplicateCount)
    {
        var ordered = candidates
            .OrderBy(e => e.File, StringComparer.Ordinal)
            .ThenBy(e => e.StartLine)
            .ThenBy(e => e.SymbolPath, StringComparer.Ordinal)
            .ToList();

        var deduplicated = new List<DumpEntity>(ordered.Count);
        var seen = new HashSet<(string Kind, string SymbolPath)>();
        int dropped = 0;
        foreach (var candidate in ordered)
        {
            if (seen.Add((candidate.Kind, candidate.SymbolPath)))
                deduplicated.Add(candidate);
            else
                dropped++;
        }

        droppedDuplicateCount = dropped;
        return deduplicated;
    }

    /// <summary>
    /// Computes entity ids and resolves parentSymbolPath -> parent entity id. When a
    /// symbolPath is shared by more than one kind (e.g. a Rust `impl` block and the type it
    /// implements), the first occurrence in <paramref name="entities"/> wins the parent lookup
    /// — deterministic given the caller sorted first.
    /// </summary>
    public static List<Entity> ToEntities(IReadOnlyList<DumpEntity> entities, BasisSide side, string basisId)
    {
        var symbolPathToId = new Dictionary<string, string>();
        foreach (var e in entities)
            symbolPathToId.TryAdd(e.SymbolPath, EntityIdCalculator.ComputeEntityId(side, e.Kind, e.SymbolPath));

        var result = new List<Entity>(entities.Count);
        foreach (var e in entities)
        {
            string id = EntityIdCalculator.ComputeEntityId(side, e.Kind, e.SymbolPath);
            string? parentId = e.ParentSymbolPath != null && symbolPathToId.TryGetValue(e.ParentSymbolPath, out var pid)
                ? pid
                : null;
            // CONTRACT-M15.md §1.1: absent resolution/isTest -> clean/false.
            result.Add(new Entity(id, basisId, e.Kind, e.Name, e.SymbolPath, e.File, e.StartLine, e.EndLine, e.ContentHash, parentId,
                Resolution: e.Resolution ?? "clean", IsTest: e.IsTest ?? false));
        }

        return result;
    }
}
