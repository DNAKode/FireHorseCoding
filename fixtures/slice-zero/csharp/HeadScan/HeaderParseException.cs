namespace HeadScan;

/// <summary>
/// Internal control-flow signal used to implement the Rust source's
/// <c>?</c>-operator early-return-on-error pattern inside the two-phase
/// parse pipeline (see <see cref="HeaderParser"/>). Caught at the top of
/// <see cref="HeaderParser.Parse"/> and converted into a
/// <see cref="ParseResult"/> — never observable outside this assembly,
/// and NOT part of the declared adaptation (that adaptation is the
/// public <see cref="ParseResult"/> return shape; this exception is a
/// private implementation detail of getting there).
/// </summary>
internal sealed class HeaderParseException : Exception
{
    public ParseError Error { get; }

    public HeaderParseException(ParseError error)
        : base(error.ToString())
    {
        Error = error;
    }
}
