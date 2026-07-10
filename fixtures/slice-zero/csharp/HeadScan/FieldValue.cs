namespace HeadScan;

/// <summary>
/// The typed value of a successfully parsed field (CONTRACT.md §1.1 rule
/// 8). Mirrors the Rust source's <c>enum FieldValue { Text(String),
/// Count(u64), Ratio { value_nanos: i64 } }</c> as a small closed class
/// hierarchy.
/// </summary>
public abstract class FieldValue
{
    private FieldValue()
    {
    }

    public abstract string Kind { get; }

    public sealed class Text : FieldValue
    {
        public string Value { get; }

        public Text(string value) => Value = value;

        public override string Kind => "text";
    }

    public sealed class Count : FieldValue
    {
        public ulong Value { get; }

        public Count(ulong value) => Value = value;

        public override string Kind => "count";
    }

    /// <summary>
    /// <c>floor(v * 1e9 + 0.5)</c> computed in double (CONTRACT.md §1.1
    /// rule 8, <c>-ratio</c> bullet) — this exact expression, so no
    /// float-formatting differences can appear between the Rust and C#
    /// sides.
    /// </summary>
    public sealed class Ratio : FieldValue
    {
        public long ValueNanos { get; }

        public Ratio(long valueNanos) => ValueNanos = valueNanos;

        public override string Kind => "ratio";
    }
}
