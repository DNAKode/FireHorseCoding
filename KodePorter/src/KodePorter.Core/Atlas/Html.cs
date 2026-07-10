using System.Net;

namespace KodePorter.Core.Atlas;

/// <summary>Tiny HTML-escaping helpers shared by the Atlas renderer (CONTRACT.md §8) — no templating package, just careful encoding.</summary>
internal static class Html
{
    /// <summary>HTML-encodes text content. Safe to also use for double-quoted attribute values (WebUtility encodes '"').</summary>
    public static string Escape(string? s) => WebUtility.HtmlEncode(s ?? "");

    /// <summary>Alias of <see cref="Escape"/> used at attribute-value call sites for readability.</summary>
    public static string Attr(string? s) => WebUtility.HtmlEncode(s ?? "");
}
