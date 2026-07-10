namespace KodePorter.Core.Diff;

/// <summary>
/// Entity-level diff between two bases of the same side (CONTRACT.md §7 step 1). Staleness
/// propagation into correspondences/units/claims needs the Gneiss binding and is out of scope
/// here — this is just the entity diff the later staleness pass consumes.
/// </summary>
public sealed record BasisDiffResult(
    IReadOnlyList<EntityDiffEntry> Added,
    IReadOnlyList<EntityDiffEntry> Removed,
    IReadOnlyList<EntityDiffEntry> Changed);
