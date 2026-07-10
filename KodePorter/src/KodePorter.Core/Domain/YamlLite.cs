using System.Text;

namespace KodePorter.Core.Domain;

/// <summary>
/// File IO shared by the domain file readers/writers: LF line endings, UTF-8 no BOM
/// (CONTRACT.md §4 / repo-wide text-file convention).
/// </summary>
internal static class DomainFileIo
{
    public static void WriteLf(string path, string content)
    {
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        string normalized = content.Replace("\r\n", "\n");
        if (!normalized.EndsWith('\n'))
            normalized += "\n";

        File.WriteAllText(path, normalized, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    /// <summary>Reads a file as logical lines (LF-normalized, no trailing empty line for a final "\n").</summary>
    public static List<string> ReadLines(string path)
    {
        string content = File.ReadAllText(path).Replace("\r\n", "\n");
        var lines = content.Split('\n').ToList();
        if (lines.Count > 0 && lines[^1].Length == 0)
            lines.RemoveAt(lines.Count - 1);
        return lines;
    }
}

/// <summary>
/// Minimal exact YAML-subset scalar codec (CONTRACT.md §4): quote only when needed, round-trip
/// stable. Deliberately narrow — not a general YAML engine — to the shapes the four domain files
/// (project.yaml, units/&lt;id&gt;.md front matter, correspondences.yaml, policy.yaml) use.
/// </summary>
internal static class YamlScalarCodec
{
    public static string Quote(string value)
    {
        if (!NeedsQuoting(value))
            return value;

        var sb = new StringBuilder();
        sb.Append('"');
        foreach (char c in value)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                default: sb.Append(c); break;
            }
        }
        sb.Append('"');
        return sb.ToString();
    }

    private static bool NeedsQuoting(string value)
    {
        if (value.Length == 0) return true;
        if (value is "null" or "true" or "false") return true;
        if (char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[^1])) return true;
        if (value.Contains('\n') || value.Contains('"')) return true;
        if (value.Contains(": ") || value.EndsWith(':')) return true;
        if (value[0] is '-' or '#' or '[' or ']' or '{' or '}' or '&' or '*') return true;
        return false;
    }

    /// <summary>Unquotes a raw (already trimmed-of-surrounding-whitespace) scalar token.</summary>
    public static string Unquote(string raw)
    {
        string trimmed = raw.Trim();
        if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[^1] == '"')
        {
            var sb = new StringBuilder();
            for (int i = 1; i < trimmed.Length - 1; i++)
            {
                char c = trimmed[i];
                if (c == '\\' && i + 1 < trimmed.Length - 1)
                {
                    i++;
                    char e = trimmed[i];
                    sb.Append(e switch { '\\' => '\\', '"' => '"', 'n' => '\n', _ => e });
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
        return trimmed;
    }
}

/// <summary>Inline flow-list codec for <c>[a, b, c]</c> arrays of scalars.</summary>
internal static class YamlFlowList
{
    public static string Write(IEnumerable<string> items)
    {
        var list = items.ToList();
        return list.Count == 0 ? "[]" : "[" + string.Join(", ", list.Select(YamlScalarCodec.Quote)) + "]";
    }

    public static List<string> Parse(string flow)
    {
        string inner = flow.Trim();
        if (!inner.StartsWith('[') || !inner.EndsWith(']'))
            throw new FormatException($"Expected a flow list like '[a, b]'; got '{flow}'.");
        inner = inner[1..^1].Trim();
        if (inner.Length == 0)
            return [];

        var items = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;
        foreach (char c in inner)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
                current.Append(c);
            }
            else if (c == ',' && !inQuotes)
            {
                items.Add(YamlScalarCodec.Unquote(current.ToString().Trim()));
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        items.Add(YamlScalarCodec.Unquote(current.ToString().Trim()));
        return items;
    }
}

/// <summary>Splits a "key: value" line (value may be empty, meaning a nested block follows).</summary>
internal static class YamlLine
{
    public static (string Key, string Value) SplitKeyValue(string line)
    {
        int idx = FindTopLevelColon(line);
        if (idx < 0)
            throw new FormatException($"Expected 'key: value' line; got '{line}'.");
        string key = line[..idx].Trim();
        string value = idx + 1 < line.Length ? line[(idx + 1)..].Trim() : "";
        return (key, value);
    }

    private static int FindTopLevelColon(string line)
    {
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
                inQuotes = !inQuotes;
            else if (c == ':' && !inQuotes && (i + 1 == line.Length || line[i + 1] == ' '))
                return i;
        }
        return -1;
    }
}
