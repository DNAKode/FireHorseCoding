namespace HeadScan;

/// <summary>
/// A parse error: an error code plus the 1-based line number of the
/// offending line (CONTRACT.md §1.1 rule 10, fail-fast).
/// </summary>
public sealed class ParseError
{
    public ErrorCode Code { get; }

    /// <summary>1-based line number of the offending line.</summary>
    public int Line { get; }

    public ParseError(ErrorCode code, int line)
    {
        Code = code;
        Line = line;
    }

    public override string ToString() => $"{Code.AsStr()} at line {Line}";
}
