namespace KodePorter.Core.Tests.Support;

/// <summary>
/// A small, fixed C# source tree used by the regeneration drill and the Roslyn provider
/// tests. V2 differs from V1 only inside the body of HeaderParser.Parse — used to prove
/// contentHash drift is scoped to the changed span (and its ancestors) and does not touch
/// unrelated siblings.
///
/// Line numbers (1-based), stable across V1/V2:
///   1  namespace HeadScan
///   3  public sealed class HeaderParser
///   5  private const int MaxLength = 4096;
///   7  public string Name { get; set; } = "";
///   9  public static string Parse(string input)     .. 12 closing brace
///   15 public enum ParseErrorCode                    .. 19 closing brace
///   17 MissingColon
///   18 BadKey
/// </summary>
internal static class HeaderParserSource
{
    public const string V1 = """
namespace HeadScan
{
    public sealed class HeaderParser
    {
        private const int MaxLength = 4096;

        public string Name { get; set; } = "";

        public static string Parse(string input)
        {
            return input.Trim();
        }
    }

    public enum ParseErrorCode
    {
        MissingColon,
        BadKey,
    }
}
""";

    public const string V2 = """
namespace HeadScan
{
    public sealed class HeaderParser
    {
        private const int MaxLength = 4096;

        public string Name { get; set; } = "";

        public static string Parse(string input)
        {
            return input.TrimEnd();
        }
    }

    public enum ParseErrorCode
    {
        MissingColon,
        BadKey,
    }
}
""";
}
