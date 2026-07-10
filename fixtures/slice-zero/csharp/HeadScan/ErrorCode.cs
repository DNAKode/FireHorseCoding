namespace HeadScan;

/// <summary>
/// The closed set of parse error codes (CONTRACT.md §1.1, "Error codes").
/// </summary>
public enum ErrorCode
{
    MissingColon,
    BadKey,
    DanglingContinuation,
    BadNumber,
    RatioOutOfRange,
    ValueTooLong,
}

public static class ErrorCodeExtensions
{
    public static string AsStr(this ErrorCode code) => code switch
    {
        ErrorCode.MissingColon => "MissingColon",
        ErrorCode.BadKey => "BadKey",
        ErrorCode.DanglingContinuation => "DanglingContinuation",
        ErrorCode.BadNumber => "BadNumber",
        ErrorCode.RatioOutOfRange => "RatioOutOfRange",
        ErrorCode.ValueTooLong => "ValueTooLong",
        _ => throw new ArgumentOutOfRangeException(nameof(code)),
    };
}
