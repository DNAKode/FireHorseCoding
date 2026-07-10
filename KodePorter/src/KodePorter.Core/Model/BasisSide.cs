namespace KodePorter.Core.Model;

/// <summary>
/// Which side of the port a basis belongs to. Wire/storage form is the lowercase
/// string "source" or "target" (CONTRACT.md §2 CHECK constraint on basis.side).
/// </summary>
public enum BasisSide
{
    Source,
    Target,
}

public static class BasisSideExtensions
{
    /// <summary>The lowercase wire string used in kpmap.db and in entityId hashing.</summary>
    public static string ToWireString(this BasisSide side) => side switch
    {
        BasisSide.Source => "source",
        BasisSide.Target => "target",
        _ => throw new ArgumentOutOfRangeException(nameof(side), side, "Unknown basis side."),
    };

    public static BasisSide ParseWireString(string value) => value switch
    {
        "source" => BasisSide.Source,
        "target" => BasisSide.Target,
        _ => throw new ArgumentException($"Unknown basis side '{value}'; expected 'source' or 'target'.", nameof(value)),
    };
}
