using KodePorter.Core.Hashing;
using KodePorter.Core.Model;

namespace KodePorter.Core.Store;

/// <summary>Pins a basis: computes the tree hash for a root and records the basis row.</summary>
public static class BasisPinner
{
    /// <summary>
    /// Computes the tree hash for <paramref name="root"/> (rust rules for
    /// <see cref="BasisSide.Source"/>, csharp rules for <see cref="BasisSide.Target"/>,
    /// per CONTRACT.md §2) and inserts the resulting basis row into <paramref name="store"/>.
    /// </summary>
    public static Basis Pin(
        MapStore store,
        BasisSide side,
        string root,
        string label,
        string? toolchain = null,
        string? analyzer = null,
        DateTimeOffset? created = null)
    {
        string treeHash = side switch
        {
            BasisSide.Source => TreeHasher.ComputeRustTreeHash(root),
            BasisSide.Target => TreeHasher.ComputeCSharpTreeHash(root),
            _ => throw new ArgumentOutOfRangeException(nameof(side), side, "Unknown basis side."),
        };

        string id = EntityIdCalculator.ComputeBasisId(side, label, treeHash);
        var basis = new Basis(id, side, label, root, treeHash, toolchain, analyzer, created ?? DateTimeOffset.UtcNow);
        store.InsertBasis(basis);
        return basis;
    }
}
