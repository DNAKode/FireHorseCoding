namespace KodePorter.Core.Model;

/// <summary>
/// A pinned snapshot of one side's source tree (CONTRACT.md §2, table `basis`).
/// </summary>
/// <param name="Id">sha256(side|label|tree_hash), lowercase hex.</param>
/// <param name="Side">source or target.</param>
/// <param name="Label">Caller-supplied label (e.g. "base", "d1").</param>
/// <param name="Root">The root path as given at pin time (not necessarily absolute).</param>
/// <param name="TreeHash">See <see cref="Hashing.TreeHasher"/> for the exact canonical form.</param>
/// <param name="Toolchain">Optional free-form toolchain descriptor.</param>
/// <param name="Analyzer">Optional free-form analyzer descriptor.</param>
/// <param name="Created">Timestamp the basis was pinned.</param>
public sealed record Basis(
    string Id,
    BasisSide Side,
    string Label,
    string Root,
    string TreeHash,
    string? Toolchain,
    string? Analyzer,
    DateTimeOffset Created);
