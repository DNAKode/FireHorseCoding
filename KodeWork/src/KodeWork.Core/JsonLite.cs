using System.Text;

namespace KodeWork.Core;

/// <summary>Tiny JSON array helper so KodeWork does not take a serializer dependency for tags/refs.</summary>
internal static class JsonLite
{
    public static string Array(IReadOnlyList<string> items)
    {
        var sb = new StringBuilder();
        sb.Append('[');
        for (int i = 0; i < items.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }
            sb.Append('"');
            sb.Append(Escape(items[i]));
            sb.Append('"');
        }
        sb.Append(']');
        return sb.ToString();
    }

    public static IReadOnlyList<string> ParseArray(string json)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(json))
        {
            return result;
        }
        string s = json.Trim();
        if (s.Length < 2 || s[0] != '[' || s[^1] != ']')
        {
            if (!string.IsNullOrWhiteSpace(s))
            {
                result.Add(s.Trim('"'));
            }
            return result;
        }
        var cur = new StringBuilder();
        bool inStr = false;
        bool escape = false;
        for (int i = 1; i < s.Length - 1; i++)
        {
            char c = s[i];
            if (escape)
            {
                cur.Append(c switch { 'n' => '\n', 'r' => '\r', 't' => '\t', '"' => '"', '\\' => '\\', _ => c });
                escape = false;
                continue;
            }
            if (c == '\\' && inStr)
            {
                escape = true;
                continue;
            }
            if (c == '"')
            {
                if (inStr)
                {
                    result.Add(cur.ToString());
                    cur.Clear();
                }
                inStr = !inStr;
                continue;
            }
            if (inStr)
            {
                cur.Append(c);
            }
        }
        return result;
    }

    private static string Escape(string s) =>
        s.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
}
