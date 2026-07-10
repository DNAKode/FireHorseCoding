namespace HeadScan;

/// <summary>
/// One header field, keyed by its first-appearance line (CONTRACT.md
/// §1.1 rules 6-7).
/// </summary>
public sealed class Field
{
    public string Key { get; }

    /// <summary>1-based line number of the key's *first* occurrence.</summary>
    public int Line { get; }

    /// <summary>
    /// Starts as a <see cref="FieldValue.Text"/> placeholder holding the
    /// raw assembled string (mirrors the Rust source's placeholder-then-
    /// retype two-phase construction) and is replaced with its typed
    /// value once typing (CONTRACT.md §1.1 rule 8) runs.
    /// </summary>
    public FieldValue Value { get; set; }

    public Field(string key, int line, FieldValue value)
    {
        Key = key;
        Line = line;
        Value = value;
    }
}
