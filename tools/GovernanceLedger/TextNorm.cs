namespace GovernanceLedger;

/// <summary>Collapses a (possibly multi-line, from a C# raw string literal) reason into single-line
/// flowing prose before it goes into the ledger — the committed jsonl and LENS.html should carry
/// clean text, not the source file's line-wrap artifacts.</summary>
internal static class TextNorm
{
    public static string Collapse(string s) =>
        string.Join(' ', s.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}
