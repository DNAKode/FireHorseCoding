using System.Text;

namespace KodePorter.Core.Atlas;

/// <summary>
/// The minimal markdown renderer required by CONTRACT.md §8 for the Units tab: headings,
/// paragraphs, lists, and inline code spans only. Deliberately narrow (no links, emphasis,
/// tables, or fenced code blocks) — unit dossier bodies are constrained to these shapes.
/// Rendered server-side (not via a JS markdown engine) so the Atlas needs no script for it.
/// </summary>
internal static class MiniMarkdown
{
    public static string Render(string markdown)
    {
        var sb = new StringBuilder();
        var lines = markdown.Replace("\r\n", "\n").Split('\n');

        var paragraph = new List<string>();
        var listItems = new List<string>();

        void FlushParagraph()
        {
            if (paragraph.Count == 0)
                return;
            sb.Append("<p>").Append(RenderInline(string.Join(' ', paragraph))).Append("</p>\n");
            paragraph.Clear();
        }

        void FlushList()
        {
            if (listItems.Count == 0)
                return;
            sb.Append("<ul>\n");
            foreach (var item in listItems)
                sb.Append("<li>").Append(RenderInline(item)).Append("</li>\n");
            sb.Append("</ul>\n");
            listItems.Clear();
        }

        foreach (string rawLine in lines)
        {
            string trimmed = rawLine.Trim();

            if (trimmed.Length == 0)
            {
                FlushParagraph();
                FlushList();
                continue;
            }

            var heading = MatchHeading(trimmed);
            if (heading is (int level, string text))
            {
                FlushParagraph();
                FlushList();
                sb.Append("<h").Append(level).Append('>').Append(RenderInline(text)).Append("</h").Append(level).Append(">\n");
                continue;
            }

            if (trimmed.StartsWith("- ", StringComparison.Ordinal))
            {
                FlushParagraph();
                listItems.Add(trimmed[2..].Trim());
                continue;
            }

            FlushList();
            paragraph.Add(trimmed);
        }

        FlushParagraph();
        FlushList();
        return sb.ToString();
    }

    private static (int Level, string Text)? MatchHeading(string trimmed)
    {
        int level = 0;
        int idx = 0;
        while (idx < trimmed.Length && trimmed[idx] == '#' && level < 6)
        {
            level++;
            idx++;
        }
        if (level == 0 || idx >= trimmed.Length || trimmed[idx] != ' ')
            return null;
        return (level, trimmed[(idx + 1)..].Trim());
    }

    private static string RenderInline(string text)
    {
        // Escape first (backtick survives HtmlEncode untouched), then turn `code` spans into <code>.
        string escaped = Html.Escape(text);
        var sb = new StringBuilder(escaped.Length);
        bool inCode = false;
        foreach (char c in escaped)
        {
            if (c == '`')
            {
                sb.Append(inCode ? "</code>" : "<code>");
                inCode = !inCode;
            }
            else
            {
                sb.Append(c);
            }
        }
        if (inCode)
            sb.Append("</code>"); // unterminated code span: close defensively rather than leak an open tag
        return sb.ToString();
    }
}
