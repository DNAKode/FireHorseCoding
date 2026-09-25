using System.Globalization;
using System.Text;

namespace KodeWork.Core;

public static class MarkdownProjector
{
    public static string ItemFile(WorkItem item)
    {
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.Append("id: ").AppendLine(item.Id);
        sb.Append("kind: ").AppendLine(item.Kind);
        sb.Append("status: ").AppendLine(item.Status);
        if (item.ParentId is not null)
        {
            sb.Append("parent: ").AppendLine(item.ParentId);
        }
        sb.Append("order: ").AppendLine(item.Order.ToString(CultureInfo.InvariantCulture));
        if (!string.IsNullOrEmpty(item.Due))
        {
            sb.Append("due: ").AppendLine(item.Due);
        }
        sb.Append("tags: ").AppendLine(JsonLite.Array(item.Tags));
        sb.Append("refs: ").AppendLine(JsonLite.Array(item.Refs));
        sb.AppendLine("---");
        sb.AppendLine();
        sb.Append("# ").AppendLine(item.Title);
        if (!string.IsNullOrEmpty(item.Body))
        {
            sb.AppendLine();
            sb.AppendLine(item.Body.TrimEnd());
            sb.AppendLine();
        }
        return sb.ToString();
    }

    public static string TreeFile(BoardView board)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# KodeWork");
        sb.AppendLine();
        sb.AppendLine($"Context `{board.Label.ContextName}` · tx {board.Label.DataCut} · `{board.Label.ResultHash[..12]}`");
        sb.AppendLine();
        foreach (var root in board.Roots)
        {
            WriteNode(sb, root, 0);
        }
        if (board.Orphans.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Orphans");
            sb.AppendLine();
            foreach (var o in board.Orphans)
            {
                sb.Append("- ").AppendLine(FormatLine(o));
            }
        }
        return sb.ToString();
    }

    public static string SnapshotHtml(BoardView board)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<meta charset=\"utf-8\">");
        sb.AppendLine("<title>KodeWork snapshot</title>");
        sb.AppendLine("<style>body{font:16px/1.45 system-ui,sans-serif;max-width:52rem;margin:2rem auto;padding:0 1rem} .meta{color:#666;font-size:.9rem} ul{list-style:none;padding-left:1.2rem} .st{color:#555}</style>");
        sb.AppendLine($"<h1>KodeWork</h1><p class=\"meta\">tx {board.Label.DataCut} · {board.Label.ResultHash}</p>");
        sb.AppendLine("<ul>");
        foreach (var root in board.Roots)
        {
            WriteHtmlNode(sb, root);
        }
        sb.AppendLine("</ul>");
        return sb.ToString();
    }

    private static void WriteNode(StringBuilder sb, TreeNode node, int depth)
    {
        sb.Append(' ', depth * 2).Append("- ").AppendLine(FormatLine(node.Item));
        foreach (var child in node.Children)
        {
            WriteNode(sb, child, depth + 1);
        }
    }

    private static void WriteHtmlNode(StringBuilder sb, TreeNode node)
    {
        sb.Append("<li><span class=\"st\">[").Append(node.Item.Status).Append("]</span> ")
            .Append(System.Net.WebUtility.HtmlEncode(node.Item.Title))
            .Append(" <small>").Append(node.Item.Kind).Append(" <code>")
            .Append(node.Item.Id).Append("</code></small>");
        if (node.Children.Count > 0)
        {
            sb.Append("<ul>");
            foreach (var child in node.Children)
            {
                WriteHtmlNode(sb, child);
            }
            sb.Append("</ul>");
        }
        sb.AppendLine("</li>");
    }

    private static string FormatLine(WorkItem item) =>
        $"[{item.Status}] {item.Title} ({item.Kind}) `{item.Id}`";
}
