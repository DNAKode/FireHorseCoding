namespace HeadScan;

/// <summary>
/// Which line-ending style(s) were observed in the input (CONTRACT.md
/// §1.1 rule 1).
/// </summary>
public enum LineEnding
{
    Lf,
    Crlf,
    Mixed,
}

public static class LineEndingExtensions
{
    public static string AsStr(this LineEnding lineEnding) => lineEnding switch
    {
        LineEnding.Lf => "lf",
        LineEnding.Crlf => "crlf",
        LineEnding.Mixed => "mixed",
        _ => throw new ArgumentOutOfRangeException(nameof(lineEnding)),
    };
}
