using System.Globalization;

namespace GovernanceLedger;

/// <summary>
/// Parses/formats wall-clock strings in the exact shape Gneiss.Cell's internal
/// <c>Hashing.FormatWall</c> writes them (CONTRACT.md section 5: ISO-8601 UTC,
/// <c>yyyy-MM-ddTHH:mm:ss.fffffffZ</c>). Gneiss.Cell's formatter is internal, so this tool keeps
/// its own copy; round-tripping a parsed value back through <see cref="DateTimeOffset"/> and into
/// Gneiss.Cell reproduces the identical wire string, which is what makes `rebuild`'s re-export
/// byte-identical to the original.
/// </summary>
internal static class WallClock
{
    private const string Format = "yyyy-MM-ddTHH:mm:ss.fffffffZ";

    /// <summary>Parses an ISO-8601 UTC wall string, either in Gneiss.Cell's canonical
    /// 7-fractional-digit shape or any format <see cref="DateTimeOffset.Parse(string)"/> accepts
    /// (for CLI-supplied --wall values).</summary>
    public static DateTimeOffset Parse(string s)
    {
        if (DateTimeOffset.TryParseExact(s, Format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var exact))
            return exact;
        return DateTimeOffset.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    }

    /// <summary>Fixed UTC instant from literal components — used for the hardcoded seed dates so
    /// no DateTimeOffset.UtcNow ever appears in seed content.</summary>
    public static DateTimeOffset Utc(int year, int month, int day, int hour = 0, int minute = 0, int second = 0) =>
        new(year, month, day, hour, minute, second, TimeSpan.Zero);
}
