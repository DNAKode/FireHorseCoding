using System.Net;

namespace GovernanceLedger;

/// <summary>Tiny HTML-escaping helper for LENS.html generation — no templating package, just
/// careful encoding (mirrors the discipline KodePorter's Atlas renderer uses).</summary>
internal static class Html
{
    /// <summary>HTML-encodes text content. Safe to also use for double-quoted attribute values (WebUtility encodes '"').</summary>
    public static string Escape(string? s) => WebUtility.HtmlEncode(s ?? "");
}
