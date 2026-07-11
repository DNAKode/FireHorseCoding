using System.Text;

namespace GovernanceLedger.Lens;

/// <summary>
/// Renders a <see cref="LensModel"/> to LENS.html — Lens-mini, Gneiss's first visual
/// (CONTRACT-M15.md section 7): a self-contained static page, inline CSS only, no external
/// requests, no script. Every interpolated value goes through <see cref="Html.Escape"/>. Nothing
/// here reads the clock or any ambient state — the model is the only input, so two exports of the
/// same ledger render byte-identical bytes.
/// </summary>
internal static class LensHtmlRenderer
{
    public static string Render(LensModel model)
    {
        var sb = new StringBuilder();
        sb.Append("<!doctype html>\n<html lang=\"en\">\n<head>\n<meta charset=\"utf-8\">\n");
        sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n");
        sb.Append("<title>FireHorseCoding — Governance Ledger (LENS)</title>\n");
        sb.Append("<style>\n").Append(Css).Append("\n</style>\n");
        sb.Append("</head>\n<body>\n");

        sb.Append("<header class=\"lens-header\">\n");
        sb.Append("<h1>FireHorseCoding <span class=\"muted\">/</span> Governance Ledger</h1>\n");
        sb.Append("<p class=\"tagline\">The meta-meta tier: every steward decision and delegate proposal that shaped this repository, in one append-only ledger.</p>\n");
        sb.Append("<p class=\"export-stamp\">export sha256 <code>").Append(Html.Escape(model.ExportSha256)).Append("</code></p>\n");
        sb.Append("</header>\n");

        sb.Append("<main>\n");
        sb.Append(RenderTimeline(model.Timeline));
        sb.Append(RenderCards(model.Cards));
        sb.Append("</main>\n");

        sb.Append("<footer class=\"lens-footer\">Rendered from <code>ledger-export.jsonl</code> — the committed durable artifact. Regenerate with <code>govledger export --dir governance</code>.</footer>\n");

        sb.Append("</body>\n</html>\n");
        return sb.ToString();
    }

    // ---- Timeline ---------------------------------------------------------------------------

    private static string RenderTimeline(IReadOnlyList<LensTxRow> rows)
    {
        var sb = new StringBuilder();
        sb.Append("<section class=\"timeline\">\n<h2>Timeline</h2>\n<ol class=\"tx-list\">\n");
        foreach (var row in rows)
        {
            sb.Append("<li class=\"tx-row\" id=\"tx-").Append(row.TxId).Append("\">\n");
            sb.Append("<span class=\"tx-wall\">").Append(Html.Escape(row.Wall)).Append("</span>\n");
            sb.Append("<span class=\"tx-actor\">").Append(Html.Escape(row.Actor)).Append("</span>\n");
            if (row.Kind is not null)
                sb.Append("<span class=\"tx-kind kind-").Append(Html.Escape(SlugKind(row.Kind))).Append("\">").Append(Html.Escape(row.Kind)).Append("</span>\n");
            sb.Append("<span class=\"tx-reason\">").Append(Html.Escape(FirstSentence(row.Reason))).Append("</span>\n");
            sb.Append("</li>\n");
        }
        sb.Append("</ol>\n</section>\n");
        return sb.ToString();
    }

    // ---- Cards --------------------------------------------------------------------------------

    private static string RenderCards(IReadOnlyList<LensCard> cards)
    {
        var sb = new StringBuilder();
        sb.Append("<section class=\"cards\">\n<h2>Decisions</h2>\n<div class=\"card-grid\">\n");
        foreach (var c in cards)
        {
            bool superseded = c.SupersededBy is not null;
            sb.Append("<article class=\"card status-").Append(Html.Escape(SlugKind(c.Status))).Append(superseded ? " superseded" : "").Append("\" id=\"card-").Append(Html.Escape(c.Id)).Append("\">\n");

            sb.Append("<div class=\"card-head\">\n");
            sb.Append("<h3 class=\"card-value\">").Append(Html.Escape(c.ValueText)).Append("</h3>\n");
            sb.Append("<span class=\"badge badge-").Append(Html.Escape(SlugKind(c.Status))).Append("\">").Append(Html.Escape(c.Status)).Append("</span>\n");
            sb.Append("</div>\n");

            sb.Append("<div class=\"card-meta\">\n");
            sb.Append("<span class=\"meta-actor\">").Append(Html.Escape(c.Actor)).Append("</span>\n");
            if (c.Method.Length > 0)
                sb.Append("<span class=\"meta-method\">").Append(Html.Escape(c.Method)).Append("</span>\n");
            sb.Append("<span class=\"meta-date\">valid from ").Append(Html.Escape(c.ValidFrom)).Append("</span>\n");
            if (c.Source is not null)
                sb.Append("<span class=\"meta-source\"><code>").Append(Html.Escape(c.Source)).Append("</code></span>\n");
            sb.Append("</div>\n");

            sb.Append("<blockquote class=\"card-reason\">").Append(Html.Escape(c.Reason)).Append("</blockquote>\n");

            if (c.SupersededBy is { } sup)
            {
                sb.Append("<div class=\"superseded-banner\">\n");
                sb.Append("Superseded by <a href=\"#tx-").Append(sup.TxId).Append("\">").Append(Html.Escape(sup.Actor)).Append(" on ").Append(Html.Escape(sup.Wall)).Append("</a>: “").Append(Html.Escape(FirstSentence(sup.Reason))).Append("”\n");
                sb.Append("</div>\n");
            }

            sb.Append("<details class=\"trail\">\n<summary>Decision trail").Append(c.Trail.Count > 0 ? $" ({c.Trail.Count})" : "").Append("</summary>\n");
            if (c.Trail.Count == 0)
            {
                sb.Append("<p class=\"trail-empty\">No decisions target this assertion.</p>\n");
            }
            else
            {
                sb.Append("<ul class=\"trail-list\">\n");
                foreach (var t in c.Trail)
                {
                    sb.Append("<li><span class=\"trail-kind kind-").Append(Html.Escape(SlugKind(t.Kind))).Append("\">").Append(Html.Escape(t.Kind)).Append("</span> ");
                    sb.Append("by <strong>").Append(Html.Escape(t.Actor)).Append("</strong> on ").Append(Html.Escape(t.Wall));
                    sb.Append(" — ").Append(Html.Escape(FirstSentence(t.Reason))).Append("</li>\n");
                }
                sb.Append("</ul>\n");
            }
            sb.Append("</details>\n");

            sb.Append("</article>\n");
        }
        sb.Append("</div>\n</section>\n");
        return sb.ToString();
    }

    // ---- Helpers ------------------------------------------------------------------------------

    /// <summary>The timeline row and trail lines show a compact lead-in of a (possibly long,
    /// quoted) reason; the full reason is always available on the owning card's blockquote.</summary>
    private static string FirstSentence(string reason)
    {
        const int max = 140;
        string collapsed = string.Join(' ', reason.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return collapsed.Length <= max ? collapsed : collapsed[..max].TrimEnd() + "…";
    }

    private static string SlugKind(string kind) => kind.ToLowerInvariant().Replace(' ', '-');

    // ---- Static assets ------------------------------------------------------------------------

    private const string Css = """
        * { box-sizing: border-box; }
        html, body { margin: 0; padding: 0; }
        body {
          font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
          background: var(--bg);
          color: var(--fg);
          line-height: 1.55;
        }
        :root {
          --bg: #ffffff;
          --fg: #1a1a1a;
          --muted: #6b7280;
          --border: #e5e7eb;
          --accent: #2563eb;
          --card-bg: #f9fafb;
          --green: #15803d;
          --green-bg: #dcfce7;
          --gray: #6b7280;
          --gray-bg: #f3f4f6;
          --red: #b91c1c;
          --red-bg: #fee2e2;
          --amber: #b45309;
          --amber-bg: #fef3c7;
        }
        @media (prefers-color-scheme: dark) {
          :root {
            --bg: #0f1115;
            --fg: #e5e7eb;
            --muted: #9ca3af;
            --border: #262a33;
            --accent: #60a5fa;
            --card-bg: #171a20;
            --green: #4ade80;
            --green-bg: #0f2e1a;
            --gray: #9ca3af;
            --gray-bg: #1a1d24;
            --red: #f87171;
            --red-bg: #3a1414;
            --amber: #fbbf24;
            --amber-bg: #3f2d05;
          }
        }
        :root[data-theme="dark"] {
          --bg: #0f1115; --fg: #e5e7eb; --muted: #9ca3af; --border: #262a33; --accent: #60a5fa;
          --card-bg: #171a20; --green: #4ade80; --green-bg: #0f2e1a; --gray: #9ca3af; --gray-bg: #1a1d24;
          --red: #f87171; --red-bg: #3a1414; --amber: #fbbf24; --amber-bg: #3f2d05;
        }
        :root[data-theme="light"] {
          --bg: #ffffff; --fg: #1a1a1a; --muted: #6b7280; --border: #e5e7eb; --accent: #2563eb;
          --card-bg: #f9fafb; --green: #15803d; --green-bg: #dcfce7; --gray: #6b7280; --gray-bg: #f3f4f6;
          --red: #b91c1c; --red-bg: #fee2e2; --amber: #b45309; --amber-bg: #fef3c7;
        }
        a { color: var(--accent); }
        code { font-family: ui-monospace, SFMono-Regular, Consolas, "Liberation Mono", monospace; font-size: 0.85em; }
        .muted { color: var(--muted); }

        .lens-header { padding: 2rem 2rem 1.25rem; border-bottom: 1px solid var(--border); }
        .lens-header h1 { margin: 0 0 0.4rem; font-size: 1.5rem; font-weight: 600; }
        .tagline { margin: 0 0 0.5rem; color: var(--muted); max-width: 60rem; }
        .export-stamp { margin: 0; font-size: 0.8rem; color: var(--muted); }

        main { max-width: 72rem; margin: 0 auto; padding: 1.5rem 2rem 3rem; }

        h2 { font-size: 1.05rem; font-weight: 600; margin: 0 0 0.75rem; color: var(--muted); text-transform: uppercase; letter-spacing: 0.04em; }

        .timeline { margin-bottom: 2.5rem; }
        .tx-list { list-style: none; margin: 0; padding: 0; border-left: 2px solid var(--border); }
        .tx-row {
          display: grid;
          grid-template-columns: 12rem 8rem auto 1fr;
          gap: 0.75rem;
          align-items: baseline;
          padding: 0.4rem 0 0.4rem 1rem;
          margin-left: -2px;
          border-left: 2px solid transparent;
          font-size: 0.85rem;
        }
        .tx-row:target { border-left: 2px solid var(--accent); background: var(--card-bg); }
        .tx-wall { font-family: ui-monospace, SFMono-Regular, Consolas, monospace; color: var(--muted); font-size: 0.8em; }
        .tx-actor { font-weight: 600; }
        .tx-kind { justify-self: start; font-size: 0.72rem; padding: 0.05rem 0.5rem; border-radius: 999px; background: var(--gray-bg); color: var(--gray); white-space: nowrap; }
        .tx-kind.kind-supersedes { background: var(--amber-bg); color: var(--amber); }
        .tx-kind.kind-bootstrap { background: var(--gray-bg); color: var(--muted); }
        .tx-reason { color: var(--fg); }

        .card-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(20rem, 1fr)); gap: 1rem; }
        .card {
          border: 1px solid var(--border);
          border-radius: 12px;
          background: var(--card-bg);
          padding: 1rem 1.1rem;
          display: flex;
          flex-direction: column;
          gap: 0.6rem;
        }
        .card.superseded { border-style: dashed; }
        .card-head { display: flex; justify-content: space-between; align-items: start; gap: 0.6rem; }
        .card-value { margin: 0; font-size: 1rem; font-weight: 600; line-height: 1.35; }
        .card.superseded .card-value { text-decoration: line-through; text-decoration-color: var(--muted); color: var(--muted); font-weight: 500; }
        .badge { font-size: 0.7rem; padding: 0.15rem 0.55rem; border-radius: 999px; white-space: nowrap; font-weight: 600; }
        .badge-accepted { background: var(--green-bg); color: var(--green); }
        .badge-defeated { background: var(--gray-bg); color: var(--gray); }
        .badge-contested { background: var(--amber-bg); color: var(--amber); }
        .badge-proposed-unadmitted, .badge-not-visible { background: var(--red-bg); color: var(--red); }

        .card-meta { display: flex; flex-wrap: wrap; gap: 0.5rem 0.9rem; font-size: 0.78rem; color: var(--muted); }
        .meta-actor { font-weight: 600; color: var(--fg); }
        .meta-method { border: 1px solid var(--border); border-radius: 999px; padding: 0 0.5rem; }

        .card-reason {
          margin: 0;
          padding-left: 0.75rem;
          border-left: 2px solid var(--border);
          font-size: 0.85rem;
          color: var(--fg);
          font-style: italic;
        }

        .superseded-banner {
          font-size: 0.8rem;
          background: var(--amber-bg);
          color: var(--amber);
          border-radius: 8px;
          padding: 0.5rem 0.7rem;
        }
        .superseded-banner a { color: inherit; font-weight: 600; text-decoration: underline; }

        details.trail { font-size: 0.8rem; }
        details.trail summary { cursor: pointer; color: var(--accent); font-weight: 600; }
        .trail-empty { color: var(--muted); margin: 0.4rem 0 0; }
        .trail-list { margin: 0.4rem 0 0; padding-left: 1.1rem; display: flex; flex-direction: column; gap: 0.3rem; }
        .trail-kind { font-weight: 600; text-transform: uppercase; font-size: 0.7em; padding: 0.05rem 0.4rem; border-radius: 999px; background: var(--gray-bg); color: var(--gray); }
        .trail-kind.kind-supersedes { background: var(--amber-bg); color: var(--amber); }

        .lens-footer { max-width: 72rem; margin: 0 auto; padding: 0 2rem 2.5rem; font-size: 0.78rem; color: var(--muted); }

        @media (max-width: 640px) {
          .tx-row { grid-template-columns: 1fr; gap: 0.15rem; }
        }
        """;
}
