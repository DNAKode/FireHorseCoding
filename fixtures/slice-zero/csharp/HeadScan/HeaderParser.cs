using System.Globalization;

namespace HeadScan;

/// <summary>
/// Parses "FHC header documents" per CONTRACT.md §1.1. This is a
/// deliberately conservative, source-shaped port of the Rust
/// <c>headscan::parse</c> function (fixtures/slice-zero/rust/src/lib.rs)
/// preserving its two-phase structure:
///
///  1. <see cref="ScanLines"/> — a single top-to-bottom line scan that
///     resolves line endings, comments, blank lines, continuation, and
///     structural errors (<c>MissingColon</c>, <c>BadKey</c>,
///     <c>DanglingContinuation</c>, <c>ValueTooLong</c>), fail-fast, in
///     line order. Produces a list of raw occurrences (duplicates and
///     all).
///  2. Duplicate resolution (first-wins, rule 6) over those occurrences,
///     followed by typed-value validation (<c>BadNumber</c>,
///     <c>RatioOutOfRange</c>) "in field order" per rule 8 — i.e. over
///     the deduplicated field list, in first-appearance order.
///
/// See <see cref="ParseResult"/> for the one declared adaptation from the
/// Rust source (<c>Result&lt;HeaderDoc, ParseError&gt;</c> → an Ok/Error
/// result object).
/// </summary>
public static class HeaderParser
{
    private const int MaxValueLen = 4096;
    private const double RatioTolerance = 1e-9;

    /// <summary>
    /// One raw (pre-dedup, pre-typed) header occurrence collected during
    /// the line scan.
    /// </summary>
    private sealed class RawOccurrence
    {
        public string Key { get; }
        public int Line { get; }
        public string Value { get; set; }

        public RawOccurrence(string key, int line, string value)
        {
            Key = key;
            Line = line;
            Value = value;
        }
    }

    /// <summary>
    /// Parse <paramref name="input"/> (an FHC header document, already
    /// decoded to text by the caller) per CONTRACT.md §1.1.
    /// </summary>
    public static ParseResult Parse(string input)
    {
        try
        {
            HeaderDoc doc = ParseCore(input);
            return ParseResult.Ok(doc);
        }
        catch (HeaderParseException ex)
        {
            return ParseResult.Fail(ex.Error);
        }
    }

    private static HeaderDoc ParseCore(string input)
    {
        (List<string> lines, LineEnding lineEnding) = SplitLines(input);

        List<RawOccurrence> rawOccurrences = ScanLines(lines);

        var fields = new List<Field>(rawOccurrences.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        uint duplicates = 0;

        foreach (RawOccurrence occ in rawOccurrences)
        {
            if (seen.Contains(occ.Key))
            {
                duplicates += 1;
                continue;
            }

            seen.Add(occ.Key);
            fields.Add(new Field(occ.Key, occ.Line, new FieldValue.Text(occ.Value))); // placeholder, typed below
        }

        foreach (Field field in fields)
        {
            string rawValue = field.Value switch
            {
                FieldValue.Text t => t.Value,
                _ => throw new InvalidOperationException("fields are Text placeholders before typing"),
            };
            field.Value = TypeValue(field.Key, rawValue, field.Line);
        }

        return new HeaderDoc(fields, lineEnding, duplicates);
    }

    /// <summary>
    /// Split <paramref name="text"/> into logical lines (terminators
    /// stripped), and classify the overall line-ending style observed
    /// (rule 1).
    ///
    /// A trailing line with no terminator at all (including the
    /// degenerate empty-document case) contributes no observation; if
    /// *no* terminator was ever observed the line ending defaults to
    /// <see cref="LineEnding.Lf"/>.
    /// </summary>
    private static (List<string> Lines, LineEnding LineEnding) SplitLines(string text)
    {
        var lines = new List<string>();
        bool sawLf = false;
        bool sawCrlf = false;

        int len = text.Length;
        int start = 0;
        int i = 0;
        while (i < len)
        {
            if (text[i] == '\n')
            {
                int end = i;
                if (end > start && text[end - 1] == '\r')
                {
                    end -= 1;
                    sawCrlf = true;
                }
                else
                {
                    sawLf = true;
                }

                lines.Add(text.Substring(start, end - start));
                start = i + 1;
            }

            i += 1;
        }

        if (start < len)
        {
            lines.Add(text.Substring(start, len - start));
        }

        LineEnding lineEnding = (sawLf, sawCrlf) switch
        {
            (true, true) => LineEnding.Mixed,
            (true, false) => LineEnding.Lf,
            (false, true) => LineEnding.Crlf,
            (false, false) => LineEnding.Lf,
        };

        return (lines, lineEnding);
    }

    /// <summary>A line is blank if it is empty or consists only of spaces/tabs (rule 3).</summary>
    private static bool IsBlank(string line)
    {
        foreach (char c in line)
        {
            if (c != ' ' && c != '\t')
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Trim leading/trailing spaces and tabs only (rule 4/5 — not
    /// general Unicode whitespace).
    /// </summary>
    private static string TrimSpTab(string s) => s.Trim(' ', '\t');

    /// <summary><c>[A-Za-z][A-Za-z0-9-]*</c> (rule 4), case-sensitive, ASCII-only.</summary>
    private static bool IsValidKey(string key)
    {
        if (key.Length == 0 || !char.IsAsciiLetter(key[0]))
        {
            return false;
        }

        for (int i = 1; i < key.Length; i++)
        {
            char c = key[i];
            if (!char.IsAsciiLetterOrDigit(c) && c != '-')
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Unicode-scalar-value length (matches Rust's <c>chars().count()</c>,
    /// i.e. counts each surrogate pair as one, not UTF-16 code units).
    /// </summary>
    private static int CountChars(string s)
    {
        int count = 0;
        int i = 0;
        while (i < s.Length)
        {
            if (char.IsHighSurrogate(s[i]) && i + 1 < s.Length && char.IsLowSurrogate(s[i + 1]))
            {
                i += 2;
            }
            else
            {
                i += 1;
            }

            count += 1;
        }

        return count;
    }

    private static void CheckLen(string value, int lineNo)
    {
        if (CountChars(value) > MaxValueLen)
        {
            throw new HeaderParseException(new ParseError(ErrorCode.ValueTooLong, lineNo));
        }
    }

    /// <summary>
    /// Phase 1: single top-to-bottom scan producing raw occurrences
    /// (rules 2-5, 9-10 for structural errors).
    /// </summary>
    private static List<RawOccurrence> ScanLines(List<string> lines)
    {
        var occurrences = new List<RawOccurrence>();

        // Index into `occurrences` of the header currently eligible to
        // receive continuation lines ("in force"), or null if no header
        // is in force.
        int? current = null;

        for (int idx = 0; idx < lines.Count; idx++)
        {
            int lineNo = idx + 1;
            string line = lines[idx];

            // Rule 3: blank lines are ignored and end any continuation.
            if (IsBlank(line))
            {
                current = null;
                continue;
            }

            // Rule 2: comment lines (first character is literally '#')
            // are ignored entirely — continuation state is left
            // untouched. Note this means a line like " # not a comment"
            // (leading whitespace before the '#') is NOT a comment; it
            // is a continuation line whose trimmed text starts with '#',
            // by the letter of rule 2 ("first character").
            if (line.StartsWith('#'))
            {
                continue;
            }

            char first = line[0];
            if (first == ' ' || first == '\t')
            {
                // Rule 5: continuation.
                if (current is null)
                {
                    throw new HeaderParseException(new ParseError(ErrorCode.DanglingContinuation, lineNo));
                }

                int fieldIdx = current.Value;
                string contText = TrimSpTab(line);
                RawOccurrence occ = occurrences[fieldIdx];
                string newValue = occ.Value + " " + contText;
                CheckLen(newValue, lineNo);
                occurrences[fieldIdx].Value = newValue;
                continue;
            }

            // Rule 4: header line.
            int colonIdx = line.IndexOf(':');
            if (colonIdx < 0)
            {
                throw new HeaderParseException(new ParseError(ErrorCode.MissingColon, lineNo));
            }

            string key = line.Substring(0, colonIdx);
            if (!IsValidKey(key))
            {
                throw new HeaderParseException(new ParseError(ErrorCode.BadKey, lineNo));
            }

            string valueRaw = line.Substring(colonIdx + 1);
            string value = TrimSpTab(valueRaw);
            CheckLen(value, lineNo);

            occurrences.Add(new RawOccurrence(key, lineNo, value));
            current = occurrences.Count - 1;
        }

        return occurrences;
    }

    /// <summary>Phase 2 per-field typing (rule 8).</summary>
    private static FieldValue TypeValue(string key, string rawValue, int line)
    {
        if (key.EndsWith("-count", StringComparison.Ordinal))
        {
            if (rawValue.Length == 0 || !AllAsciiDigits(rawValue))
            {
                throw new HeaderParseException(new ParseError(ErrorCode.BadNumber, line));
            }

            if (!ulong.TryParse(rawValue, NumberStyles.None, CultureInfo.InvariantCulture, out ulong n))
            {
                throw new HeaderParseException(new ParseError(ErrorCode.BadNumber, line));
            }

            return new FieldValue.Count(n);
        }

        if (key.EndsWith("-ratio", StringComparison.Ordinal))
        {
            if (!IsValidRatioLiteral(rawValue))
            {
                throw new HeaderParseException(new ParseError(ErrorCode.BadNumber, line));
            }

            string normalized = NormalizeRatioLiteral(rawValue);
            if (!double.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double v))
            {
                throw new HeaderParseException(new ParseError(ErrorCode.BadNumber, line));
            }

            // This branch is unreachable given `IsValidRatioLiteral` above
            // (the format never admits a sign), but is kept to mirror
            // CONTRACT.md §1.1 rule 8 literally.
            if (v < 0.0)
            {
                throw new HeaderParseException(new ParseError(ErrorCode.RatioOutOfRange, line));
            }

            double clamped;
            if (v > 1.0)
            {
                if (v <= 1.0 + RatioTolerance)
                {
                    clamped = 1.0;
                }
                else
                {
                    throw new HeaderParseException(new ParseError(ErrorCode.RatioOutOfRange, line));
                }
            }
            else
            {
                clamped = v;
            }

            long valueNanos = (long)Math.Floor((clamped * 1e9) + 0.5);
            return new FieldValue.Ratio(valueNanos);
        }

        return new FieldValue.Text(rawValue);
    }

    private static bool AllAsciiDigits(string s)
    {
        foreach (char c in s)
        {
            if (!char.IsAsciiDigit(c))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// "digits, optional single '.'" (rule 8, <c>-ratio</c> bullet); at
    /// least one digit required, no sign.
    /// </summary>
    private static bool IsValidRatioLiteral(string s)
    {
        if (s.Length == 0)
        {
            return false;
        }

        int dotCount = 0;
        bool anyDigit = false;
        foreach (char c in s)
        {
            if (c == '.')
            {
                dotCount += 1;
            }
            else if (char.IsAsciiDigit(c))
            {
                anyDigit = true;
            }
            else
            {
                return false;
            }
        }

        return dotCount <= 1 && anyDigit;
    }

    /// <summary>
    /// Pad a bare leading/trailing '.' so <c>double.Parse</c> accepts
    /// forms like ".5" and "5." that the CONTRACT's ratio grammar
    /// permits.
    /// </summary>
    private static string NormalizeRatioLiteral(string s)
    {
        if (s.Length > 0 && s[0] == '.')
        {
            s = "0" + s;
        }

        if (s.Length > 0 && s[^1] == '.')
        {
            s += "0";
        }

        return s;
    }
}
