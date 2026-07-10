namespace HeadScan;

/// <summary>
/// A fully parsed header document.
/// </summary>
public sealed class HeaderDoc
{
    /// <summary>Fields in first-appearance order of their keys (rule 7).</summary>
    public IReadOnlyList<Field> Fields { get; }

    public LineEnding LineEnding { get; }

    public uint Duplicates { get; }

    public HeaderDoc(IReadOnlyList<Field> fields, LineEnding lineEnding, uint duplicates)
    {
        Fields = fields;
        LineEnding = lineEnding;
        Duplicates = duplicates;
    }
}
