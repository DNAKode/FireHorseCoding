using KodePorter.Core.Hashing;
using KodePorter.Core.Model;

namespace KodePorter.Core.Store;

/// <summary>Computes the content-addressed entity and basis ids used across kpmap.db.</summary>
public static class EntityIdCalculator
{
    /// <summary>entityId = sha256(side|kind|symbolPath) lowercase hex (CONTRACT.md §2/§3, fixtures §6).</summary>
    public static string ComputeEntityId(BasisSide side, string kind, string symbolPath)
        => Sha256Util.HexOfUtf8($"{side.ToWireString()}|{kind}|{symbolPath}");

    /// <summary>basisId = sha256(side|label|tree_hash) lowercase hex (CONTRACT.md §2).</summary>
    public static string ComputeBasisId(BasisSide side, string label, string treeHash)
        => Sha256Util.HexOfUtf8($"{side.ToWireString()}|{label}|{treeHash}");
}
