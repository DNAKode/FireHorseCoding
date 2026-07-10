using KodePorter.Core.Model;
using KodePorter.Core.Store;

namespace KodePorter.Core.Diff;

/// <summary>
/// Computes added/removed/changed entity sets between two bases of one side, by entity id
/// (CONTRACT.md §7 step 1). Ordering is deterministic (symbolPath then id).
/// </summary>
public static class BasisDiffService
{
    public static BasisDiffResult Diff(MapStore store, string previousBasisId, string newBasisId)
    {
        ArgumentNullException.ThrowIfNull(store);

        var previous = store.GetEntities(previousBasisId).ToDictionary(e => e.Id);
        var current = store.GetEntities(newBasisId).ToDictionary(e => e.Id);

        var added = Order(current.Keys.Except(previous.Keys)
            .Select(id => ToEntry(current[id], EntityChangeKind.Added)));

        var removed = Order(previous.Keys.Except(current.Keys)
            .Select(id => ToEntry(previous[id], EntityChangeKind.Removed)));

        var changed = Order(previous.Keys.Intersect(current.Keys)
            .Where(id => previous[id].ContentHash != current[id].ContentHash)
            .Select(id => ToEntry(current[id], EntityChangeKind.Changed)));

        return new BasisDiffResult(added, removed, changed);
    }

    private static List<EntityDiffEntry> Order(IEnumerable<EntityDiffEntry> entries) => entries
        .OrderBy(e => e.SymbolPath, StringComparer.Ordinal)
        .ThenBy(e => e.EntityId, StringComparer.Ordinal)
        .ToList();

    private static EntityDiffEntry ToEntry(Entity e, EntityChangeKind changeKind) => new(e.Id, e.Kind, e.SymbolPath, changeKind);
}
