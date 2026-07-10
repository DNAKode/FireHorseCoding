using System.Diagnostics.CodeAnalysis;

namespace HeadScan;

/// <summary>
/// The result of <see cref="HeaderParser.Parse"/>: an Ok/Error union.
///
/// ADAPTATION (CONTRACT.md §5, "adapts"): the Rust source returns
/// <c>Result&lt;HeaderDoc, ParseError&gt;</c>. This is the ONE declared,
/// systematic, policy-level adaptation for this port: instead of Rust's
/// <c>Result</c> type (or throwing .NET exceptions for what are expected,
/// data-dependent parse outcomes), the C# port exposes an explicit
/// Ok/Error result-object. Every other rule in CONTRACT.md §1.1 is
/// ported behaviorally unchanged.
/// </summary>
public sealed class ParseResult
{
    public HeaderDoc? Doc { get; }

    public ParseError? Error { get; }

    [MemberNotNullWhen(true, nameof(Doc))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsOk => Doc is not null;

    private ParseResult(HeaderDoc? doc, ParseError? error)
    {
        Doc = doc;
        Error = error;
    }

    public static ParseResult Ok(HeaderDoc doc) => new(doc, null);

    public static ParseResult Fail(ParseError error) => new(null, error);
}
