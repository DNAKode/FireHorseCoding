using System.Text;
using System.Text.Json;

namespace KodePorter.Core.Atlas;

/// <summary>
/// Turns an <see cref="AtlasData"/> into the single self-contained HTML file (CONTRACT.md §8):
/// inline CSS, a JSON data island, and a small vanilla-JS snippet for cross-tree correspondence
/// highlighting. Tabs and tree collapsing are pure CSS/HTML (radio-driven tabs, native
/// &lt;details&gt; trees) — no script required for either, so the one script block that does
/// exist stays small and auditable.
/// </summary>
internal static class AtlasHtmlRenderer
{
    private static readonly JsonSerializerOptions DataIslandOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string Render(AtlasData data)
    {
        string dataJson = JsonSerializer.Serialize(data, DataIslandOptions).Replace("</", "<\\/", StringComparison.Ordinal);

        var sb = new StringBuilder();
        sb.Append("<!doctype html>\n<html lang=\"en\">\n<head>\n<meta charset=\"utf-8\">\n");
        sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n");
        sb.Append("<title>").Append(Html.Escape(data.Header.ProjectName)).Append(" — Port Atlas</title>\n");
        sb.Append("<style>\n").Append(Css).Append("\n</style>\n");
        sb.Append("</head>\n<body>\n");

        sb.Append("<input type=\"radio\" name=\"kp-tab\" id=\"tab-corr\" class=\"kp-tab-radio\" checked>\n");
        sb.Append("<input type=\"radio\" name=\"kp-tab\" id=\"tab-units\" class=\"kp-tab-radio\">\n");
        sb.Append("<input type=\"radio\" name=\"kp-tab\" id=\"tab-claims\" class=\"kp-tab-radio\">\n");
        sb.Append("<input type=\"radio\" name=\"kp-tab\" id=\"tab-runs\" class=\"kp-tab-radio\">\n");

        sb.Append(RenderHeader(data.Header));
        sb.Append(RenderHealth(data.Health));

        sb.Append("<main class=\"layout\">\n");
        sb.Append("<section class=\"tree-pane\" id=\"source-tree\">\n<h2>Source</h2>\n")
          .Append(RenderTree(data.SourceTree, "tree-source")).Append("</section>\n");

        sb.Append("<section class=\"tabs\">\n");
        sb.Append("<div class=\"tab-bar\">\n");
        sb.Append("<label for=\"tab-corr\">Correspondences</label>\n");
        sb.Append("<label for=\"tab-units\">Units</label>\n");
        sb.Append("<label for=\"tab-claims\">Claims</label>\n");
        sb.Append("<label for=\"tab-runs\">Runs</label>\n");
        sb.Append("</div>\n");
        sb.Append("<div class=\"tab-panels\">\n");
        sb.Append("<section class=\"tab-panel\" id=\"panel-corr\">\n").Append(RenderCorrespondencesTab(data.Correspondences)).Append("</section>\n");
        sb.Append("<section class=\"tab-panel\" id=\"panel-units\">\n").Append(RenderUnitsTab(data.Units)).Append("</section>\n");
        sb.Append("<section class=\"tab-panel\" id=\"panel-claims\">\n").Append(RenderClaimsTab(data.Claims, data.Label)).Append("</section>\n");
        sb.Append("<section class=\"tab-panel\" id=\"panel-runs\">\n").Append(RenderRunsTab(data.Runs)).Append("</section>\n");
        sb.Append("</div>\n");
        sb.Append("</section>\n");

        sb.Append("<section class=\"tree-pane\" id=\"target-tree\">\n<h2>Target</h2>\n")
          .Append(RenderTree(data.TargetTree, "tree-target")).Append("</section>\n");
        sb.Append("</main>\n");

        sb.Append(RenderFooter(data.Footer));

        sb.Append("<script type=\"application/json\" id=\"kp-atlas-data\">").Append(dataJson).Append("</script>\n");
        sb.Append("<script>\n").Append(Js).Append("\n</script>\n");
        sb.Append("</body>\n</html>\n");
        return sb.ToString();
    }

    // ---- Header / health --------------------------------------------------------------------

    private static string RenderHeader(AtlasHeader h)
    {
        var sb = new StringBuilder();
        sb.Append("<header class=\"kp-header\">\n");
        sb.Append("<div class=\"kp-title\"><h1>").Append(Html.Escape(h.ProjectName)).Append("</h1>")
          .Append("<span class=\"direction-badge\">").Append(Html.Escape(h.Direction)).Append("</span></div>\n");
        sb.Append("<div class=\"kp-bases\">\n");
        sb.Append(RenderBasisList("Source", h.SourceBases));
        sb.Append(RenderBasisList("Target", h.TargetBases));
        sb.Append("</div>\n");
        sb.Append("<div class=\"kp-meta\"><span>Generated ").Append(Html.Escape(h.GeneratedAt)).Append("</span>")
          .Append("<span class=\"kp-version\">").Append(Html.Escape(h.ToolVersion)).Append("</span></div>\n");
        sb.Append("</header>\n");
        return sb.ToString();
    }

    private static string RenderBasisList(string label, IReadOnlyList<AtlasBasis> bases)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"basis-group\"><span class=\"basis-side\">").Append(Html.Escape(label)).Append("</span> ");
        if (bases.Count == 0)
        {
            sb.Append("<span class=\"muted\">none pinned</span>");
        }
        else
        {
            sb.Append(string.Join(", ", bases.Select(b =>
                $"<span class=\"basis-chip\">{Html.Escape(b.Label)} <code>{Html.Escape(b.ShortHash)}</code> · {b.EntityCount}</span>")));
        }
        sb.Append("</div>\n");
        return sb.ToString();
    }

    private static string RenderHealth(HealthReport h)
    {
        var sb = new StringBuilder();
        sb.Append("<nav class=\"health\">\n");
        sb.Append(TileLink("mapped", h.Mapped, anchor: "source-tree"));
        sb.Append(TileLink("corresponded", h.Corresponded, tab: "tab-corr"));
        sb.Append(TileLink("implemented", h.Implemented, tab: "tab-units"));
        sb.Append(TileLink("verified", h.Verified, tab: "tab-runs"));
        sb.Append(TileLink("stale", h.Stale, tab: "tab-claims"));
        sb.Append(TileLink("unknown", h.Unknown, anchor: "source-tree"));
        sb.Append("</nav>\n");
        return sb.ToString();
    }

    private static string TileLink(string dim, int count, string? anchor = null, string? tab = null)
    {
        string cls = "health-tile dim-" + dim + (count > 0 ? " nonzero" : "");
        string inner = $"<span class=\"tile-count\">{count}</span><span class=\"tile-label\">{Html.Escape(dim)}</span>";
        return tab is not null
            ? $"<label class=\"{cls}\" for=\"{Html.Attr(tab)}\">{inner}</label>\n"
            : $"<a class=\"{cls}\" href=\"#{Html.Attr(anchor)}\">{inner}</a>\n";
    }

    // ---- Trees --------------------------------------------------------------------------------

    private static string RenderTree(IReadOnlyList<AtlasEntityNode> nodes, string containerId)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"tree\" id=\"").Append(Html.Attr(containerId)).Append("\">\n");
        if (nodes.Count == 0)
        {
            sb.Append("<p class=\"empty\">No basis pinned yet.</p>\n");
        }
        else
        {
            var byParent = nodes.Where(n => n.ParentId is not null).ToLookup(n => n.ParentId!);
            var idSet = nodes.Select(n => n.Id).ToHashSet(StringComparer.Ordinal);
            var roots = nodes.Where(n => n.ParentId is null || !idSet.Contains(n.ParentId))
                .OrderBy(n => n.SymbolPath, StringComparer.Ordinal);
            foreach (var root in roots)
                RenderNode(sb, root, byParent, []);
        }
        sb.Append("</div>\n");
        return sb.ToString();
    }

    private static void RenderNode(StringBuilder sb, AtlasEntityNode node, ILookup<string, AtlasEntityNode> byParent, HashSet<string> ancestry)
    {
        var children = ancestry.Contains(node.Id)
            ? []
            : byParent[node.Id].OrderBy(c => c.SymbolPath, StringComparer.Ordinal).ToList();
        string staleClass = node.Stale ? " stale" : "";
        string entityAttr = $" data-entity-id=\"{Html.Attr(node.Id)}\"";
        string badgeAndName =
            $"<span class=\"kind-badge kind-{Html.Attr(node.Kind)}\">{Html.Escape(node.Kind)}</span> <span class=\"node-name\">{Html.Escape(node.Name)}</span>";

        if (children.Count == 0)
        {
            sb.Append("<div class=\"node leaf").Append(staleClass).Append('"').Append(entityAttr).Append('>')
              .Append(badgeAndName).Append("</div>\n");
            return;
        }

        sb.Append("<details open class=\"node branch").Append(staleClass).Append('"').Append(entityAttr).Append(">\n");
        sb.Append("<summary>").Append(badgeAndName).Append("</summary>\n");
        var childAncestry = new HashSet<string>(ancestry, StringComparer.Ordinal) { node.Id };
        foreach (var c in children)
            RenderNode(sb, c, byParent, childAncestry);
        sb.Append("</details>\n");
    }

    // ---- Correspondences tab -------------------------------------------------------------------

    private static string RenderCorrespondencesTab(IReadOnlyList<AtlasCorrespondence> items)
    {
        var sb = new StringBuilder();
        if (items.Count == 0)
            return "<p class=\"empty\">No correspondences yet.</p>\n";

        sb.Append("<table class=\"data-table\">\n<thead><tr><th>Type</th><th>Source</th><th>Target</th><th>Unit</th><th>Criterion</th><th>Status</th></tr></thead>\n<tbody>\n");
        foreach (var c in items)
        {
            sb.Append("<tr class=\"corr-row").Append(c.Stale ? " stale" : "").Append('"')
              .Append(" data-corr-row=\"1\"")
              .Append(" data-source-entity=\"").Append(Html.Attr(c.SourceEntityId ?? "")).Append('"')
              .Append(" data-target-entity=\"").Append(Html.Attr(c.TargetEntityId ?? "")).Append("\">\n");
            sb.Append("<td><span class=\"badge type-").Append(Html.Attr(c.Type)).Append("\">").Append(Html.Escape(c.Type)).Append("</span>");
            if (c.DivergenceKind is not null)
                sb.Append(" <span class=\"muted\">(").Append(Html.Escape(c.DivergenceKind)).Append(")</span>");
            sb.Append("</td>\n");
            sb.Append("<td class=\"mono\">").Append(Html.Escape(c.SourceSymbolPath ?? "—")).Append("</td>\n");
            sb.Append("<td class=\"mono\">").Append(Html.Escape(c.TargetSymbolPath ?? "—")).Append("</td>\n");
            sb.Append("<td>").Append(Html.Escape(c.Unit)).Append("</td>\n");
            sb.Append("<td>").Append(Html.Escape(c.Criterion ?? "—")).Append("</td>\n");
            sb.Append("<td>").Append(StatusBadge(c.Status));
            if (c.Stale)
                sb.Append(" <span class=\"badge stale\">stale</span>");
            sb.Append("</td>\n");
            sb.Append("</tr>\n");
        }
        sb.Append("</tbody>\n</table>\n");
        return sb.ToString();
    }

    /// <summary>
    /// Renders one status as a single badge. The class token is defensively slugified — lowercase,
    /// `[a-z0-9-]` only — so a caller that (by bug or future change) hands in something other than
    /// a genuine single-token status can never emit a malformed `class` attribute (e.g. a
    /// space-containing joined summary like "2 accepted, 1 proposed" collapsing the class list into
    /// "status-2 accepted, 1 proposed"). Anything that doesn't survive slugification becomes
    /// "mixed" — see <c>.status-mixed</c> in <see cref="Css"/>. Genuine multi-status summaries (the
    /// per-unit behavior-claim rollup) are rendered as several of these badges, one per status, by
    /// <see cref="StatusBadges"/> — never as one joined multi-word status.
    /// </summary>
    private static string StatusBadge(string status) => $"<span class=\"badge status-{Html.Attr(SlugifyStatus(status))}\">{Html.Escape(status)}</span>";

    private static string StatusBadges(IReadOnlyList<AtlasStatusCount> counts)
    {
        if (counts.Count == 0)
            return StatusBadge("none");
        return string.Join(" ", counts.Select(c => StatusBadge($"{c.Count} {c.Status}", cssToken: c.Status)));
    }

    private static string StatusBadge(string displayText, string cssToken) =>
        $"<span class=\"badge status-{Html.Attr(SlugifyStatus(cssToken))}\">{Html.Escape(displayText)}</span>";

    private static string SlugifyStatus(string status)
    {
        string lowered = status.ToLowerInvariant();
        var sb = new StringBuilder(lowered.Length);
        foreach (char c in lowered)
        {
            if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '-')
                sb.Append(c);
        }
        string slug = sb.ToString();
        return slug.Length > 0 ? slug : "mixed";
    }

    // ---- Units tab ------------------------------------------------------------------------------

    private static string RenderUnitsTab(IReadOnlyList<AtlasUnit> units)
    {
        if (units.Count == 0)
            return "<p class=\"empty\">No units yet.</p>\n";

        var sb = new StringBuilder();
        foreach (var u in units)
        {
            sb.Append("<article class=\"unit").Append(u.Stale ? " stale" : "").Append("\">\n");
            sb.Append("<h3>").Append(Html.Escape(u.Id)).Append(" — ").Append(Html.Escape(u.Name)).Append("</h3>\n");
            sb.Append("<p class=\"unit-meta\">status: ").Append(StatusBadge(u.Status));
            sb.Append(" · behavior claim: ").Append(StatusBadges(u.BehaviorClaimCounts));
            if (u.Stale)
                sb.Append(" <span class=\"badge stale\">stale</span>");
            sb.Append("</p>\n");

            sb.Append("<details class=\"anchors\"><summary>Anchors (")
              .Append(u.SourceAnchors.Count + u.TargetAnchors.Count).Append(")</summary>\n<ul class=\"mono\">\n");
            foreach (var a in u.SourceAnchors)
                sb.Append("<li>source: ").Append(Html.Escape(a.SymbolPath)).Append(" @ ").Append(Html.Escape(a.BasisLabel)).Append("</li>\n");
            foreach (var a in u.TargetAnchors)
                sb.Append("<li>target: ").Append(Html.Escape(a.SymbolPath)).Append(" @ ").Append(Html.Escape(a.BasisLabel)).Append("</li>\n");
            sb.Append("</ul></details>\n");

            sb.Append("<div class=\"unit-body\">\n").Append(u.BodyHtml).Append("</div>\n");
            sb.Append("</article>\n");
        }
        return sb.ToString();
    }

    // ---- Claims tab -----------------------------------------------------------------------------

    private static string RenderClaimsTab(IReadOnlyList<AtlasClaim> claims, AtlasLabelInfo label)
    {
        if (claims.Count == 0)
            return "<p class=\"empty\">No claims yet.</p>\n";

        var sb = new StringBuilder();
        sb.Append("<table class=\"data-table\">\n<thead><tr><th>Predicate</th><th>Subject</th><th>Value</th><th>Status</th><th>Decided by</th><th>Details</th></tr></thead>\n<tbody>\n");
        foreach (var c in claims)
        {
            sb.Append("<tr class=\"").Append(c.Stale ? "stale" : "").Append("\">\n");
            sb.Append("<td class=\"mono\">").Append(Html.Escape(c.Predicate)).Append("</td>\n");
            sb.Append("<td class=\"mono\">").Append(Html.Escape(c.Subject)).Append("</td>\n");
            sb.Append("<td>").Append(Html.Escape(c.ValueSummary)).Append("</td>\n");
            sb.Append("<td>").Append(StatusBadge(c.Status));
            if (c.AutoAdmitted)
                sb.Append(" <span class=\"badge auto\">AutoAdmitted</span>");
            if (c.Stale)
                sb.Append(" <span class=\"badge stale\">stale</span>");
            sb.Append("</td>\n");
            sb.Append("<td>").Append(Html.Escape(c.DecidedBy ?? "—")).Append("</td>\n");
            sb.Append("<td>\n");
            sb.Append(RenderLabelPopover(label));
            sb.Append(RenderWhy(c.Why));
            sb.Append("</td>\n</tr>\n");
        }
        sb.Append("</tbody>\n</table>\n");
        return sb.ToString();
    }

    private static string RenderLabelPopover(AtlasLabelInfo label)
    {
        var sb = new StringBuilder();
        sb.Append("<details class=\"popover\"><summary>label</summary>\n<dl class=\"kv\">\n");
        sb.Append("<dt>context</dt><dd class=\"mono\">").Append(Html.Escape(label.ContextName)).Append(" (").Append(Html.Escape(label.ContextHash)).Append(")</dd>\n");
        sb.Append("<dt>dataCut</dt><dd>").Append(label.DataCut).Append("</dd>\n");
        sb.Append("<dt>defCut</dt><dd>").Append(label.DefCut).Append("</dd>\n");
        sb.Append("<dt>consumed</dt><dd>").Append(label.ConsumedCount).Append("</dd>\n");
        sb.Append("<dt>receipt</dt><dd class=\"mono\">").Append(Html.Escape(label.Receipt)).Append("</dd>\n");
        sb.Append("</dl></details>\n");
        return sb.ToString();
    }

    private static string RenderWhy(AtlasWhyNode node)
    {
        var sb = new StringBuilder();
        sb.Append("<details class=\"why\"><summary>why: ").Append(Html.Escape(ShortAid(node.Aid)))
          .Append(" (").Append(Html.Escape(node.Status)).Append(")</summary>\n");
        if (node.DefeatedBy is not null)
            sb.Append("<p class=\"muted\">defeated by ").Append(Html.Escape(ShortAid(node.DefeatedBy))).Append("</p>\n");
        if (node.RuleVersions.Count > 0)
            sb.Append("<p class=\"muted\">rules: ").Append(Html.Escape(string.Join(", ", node.RuleVersions))).Append("</p>\n");
        if (node.Decisions.Count > 0)
            sb.Append("<p class=\"muted\">decisions: ").Append(Html.Escape(string.Join(", ", node.Decisions.Select(ShortAid)))).Append("</p>\n");
        if (node.Inputs.Count > 0)
        {
            sb.Append("<div class=\"why-inputs\">\n");
            foreach (var input in node.Inputs)
                sb.Append(RenderWhy(input));
            sb.Append("</div>\n");
        }
        sb.Append("</details>\n");
        return sb.ToString();
    }

    private static string ShortAid(string aid) => aid.Length > 10 ? aid[..10] : aid;

    // ---- Runs tab -------------------------------------------------------------------------------

    private static string RenderRunsTab(IReadOnlyList<AtlasRun> runs)
    {
        if (runs.Count == 0)
            return "<p class=\"empty\">No verification runs yet.</p>\n";

        var sb = new StringBuilder();
        sb.Append("<table class=\"data-table\">\n<thead><tr><th>Unit</th><th>Criterion</th><th>Verdict</th><th>Cases</th><th>Mismatches</th><th>Rerun</th></tr></thead>\n<tbody>\n");
        foreach (var r in runs)
        {
            sb.Append("<tr class=\"").Append(r.Verdict == "fail" ? "fail" : "pass").Append("\">\n");
            sb.Append("<td>").Append(Html.Escape(r.Unit)).Append("</td>\n");
            sb.Append("<td class=\"mono\">").Append(Html.Escape(r.Criterion)).Append("</td>\n");
            sb.Append("<td>").Append(StatusBadge(r.Verdict)).Append("</td>\n");
            sb.Append("<td>").Append(r.PassCount).Append(" pass / ").Append(r.FailCount).Append(" fail</td>\n");
            sb.Append("<td>").Append(r.Mismatches.Count == 0 ? "—" : Html.Escape(string.Join(", ", r.Mismatches))).Append("</td>\n");
            sb.Append("<td><code class=\"rerun\">").Append(Html.Escape(r.RerunCommand)).Append("</code></td>\n");
            sb.Append("</tr>\n");
        }
        sb.Append("</tbody>\n</table>\n");
        return sb.ToString();
    }

    // ---- Footer ---------------------------------------------------------------------------------

    private static string RenderFooter(AtlasFooter f) =>
        $"<footer class=\"kp-footer\">Ledger: <code>{Html.Escape(f.LedgerPath)}</code> · export sha256 <code>{Html.Escape(f.LedgerSha256)}</code></footer>\n";

    // ---- Static assets ----------------------------------------------------------------------------

    private const string Css = """
        * { box-sizing: border-box; }
        html, body { margin: 0; padding: 0; }
        body {
          font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
          background: var(--bg);
          color: var(--fg);
          line-height: 1.5;
        }
        :root {
          --bg: #ffffff;
          --fg: #1a1a1a;
          --muted: #6b7280;
          --border: #e5e7eb;
          --accent: #2563eb;
          --amber: #b45309;
          --amber-bg: #fef3c7;
          --green: #15803d;
          --green-bg: #dcfce7;
          --gray: #6b7280;
          --gray-bg: #f3f4f6;
          --red: #b91c1c;
          --red-bg: #fee2e2;
          --card-bg: #f9fafb;
        }
        @media (prefers-color-scheme: dark) {
          :root {
            --bg: #0f1115;
            --fg: #e5e7eb;
            --muted: #9ca3af;
            --border: #262a33;
            --accent: #60a5fa;
            --amber: #fbbf24;
            --amber-bg: #3f2d05;
            --green: #4ade80;
            --green-bg: #0f2e1a;
            --gray: #9ca3af;
            --gray-bg: #1a1d24;
            --red: #f87171;
            --red-bg: #3a1414;
            --card-bg: #171a20;
          }
        }
        .kp-tab-radio { position: absolute; opacity: 0; pointer-events: none; }
        a { color: var(--accent); }
        code, .mono { font-family: ui-monospace, SFMono-Regular, Consolas, "Liberation Mono", monospace; font-size: 0.9em; }

        .kp-header {
          padding: 1.5rem 2rem;
          border-bottom: 1px solid var(--border);
          display: flex;
          flex-wrap: wrap;
          align-items: baseline;
          gap: 1rem 2rem;
        }
        .kp-title { display: flex; align-items: baseline; gap: 0.75rem; }
        .kp-title h1 { font-size: 1.4rem; margin: 0; font-weight: 600; }
        .direction-badge { font-size: 0.8rem; color: var(--muted); border: 1px solid var(--border); padding: 0.1rem 0.5rem; border-radius: 999px; }
        .kp-bases { display: flex; flex-direction: column; gap: 0.25rem; font-size: 0.85rem; color: var(--muted); }
        .basis-group { display: flex; gap: 0.4rem; flex-wrap: wrap; align-items: center; }
        .basis-side { font-weight: 600; color: var(--fg); }
        .basis-chip { background: var(--card-bg); border: 1px solid var(--border); border-radius: 6px; padding: 0.05rem 0.5rem; }
        .kp-meta { margin-left: auto; display: flex; gap: 1rem; font-size: 0.8rem; color: var(--muted); }
        .kp-version { font-weight: 600; }

        .health {
          display: flex;
          flex-wrap: wrap;
          gap: 0.75rem;
          padding: 1rem 2rem;
          border-bottom: 1px solid var(--border);
        }
        .health-tile {
          display: flex;
          flex-direction: column;
          gap: 0.15rem;
          padding: 0.6rem 1rem;
          border: 1px solid var(--border);
          border-radius: 10px;
          background: var(--card-bg);
          text-decoration: none;
          color: inherit;
          cursor: pointer;
          min-width: 6rem;
        }
        .tile-count { font-size: 1.4rem; font-weight: 700; }
        .tile-label { font-size: 0.75rem; color: var(--muted); text-transform: uppercase; letter-spacing: 0.03em; }
        .dim-stale.nonzero { border-color: var(--amber); background: var(--amber-bg); }
        .dim-stale.nonzero .tile-count { color: var(--amber); }
        .dim-unknown.nonzero { border-color: var(--red); }
        .dim-unknown.nonzero .tile-count { color: var(--red); }
        .dim-verified.nonzero .tile-count { color: var(--green); }

        .layout {
          display: grid;
          grid-template-columns: minmax(220px, 260px) minmax(0, 1fr) minmax(220px, 260px);
          gap: 0;
        }
        @media (max-width: 900px) {
          .layout { grid-template-columns: 1fr; }
        }
        .tree-pane { padding: 1rem 1.25rem; border-right: 1px solid var(--border); overflow-x: auto; }
        #target-tree { border-right: none; border-left: 1px solid var(--border); }
        .tree-pane h2 { font-size: 0.85rem; text-transform: uppercase; letter-spacing: 0.04em; color: var(--muted); margin: 0 0 0.75rem; }
        .tree { font-size: 0.85rem; }
        .tree details { margin-left: 0.9rem; }
        .tree summary { cursor: pointer; list-style: none; padding: 0.15rem 0; }
        .tree summary::-webkit-details-marker { display: none; }
        .tree summary::before { content: "\25B8"; display: inline-block; width: 1em; color: var(--muted); }
        .tree details[open] > summary::before { content: "\25BE"; }
        .node.leaf { margin-left: 1.9rem; padding: 0.15rem 0; }
        details.node.stale > summary, .node.leaf.stale { border-left: 3px solid var(--amber); padding-left: 0.4rem; }
        .kind-badge { font-size: 0.65rem; text-transform: uppercase; color: var(--muted); border: 1px solid var(--border); border-radius: 4px; padding: 0 0.3rem; }
        .node-name { font-family: ui-monospace, SFMono-Regular, Consolas, "Liberation Mono", monospace; }
        .tree-hit { outline: 2px solid var(--accent); outline-offset: 2px; border-radius: 3px; }

        .tabs { padding: 1rem 1.5rem; min-width: 0; }
        .tab-bar { display: flex; gap: 0.25rem; border-bottom: 1px solid var(--border); margin-bottom: 1rem; flex-wrap: wrap; }
        .tab-bar label { padding: 0.5rem 0.9rem; cursor: pointer; font-size: 0.9rem; color: var(--muted); border-bottom: 2px solid transparent; }
        .tab-panel { display: none; }
        #tab-corr:checked ~ main .tab-panels #panel-corr,
        #tab-units:checked ~ main .tab-panels #panel-units,
        #tab-claims:checked ~ main .tab-panels #panel-claims,
        #tab-runs:checked ~ main .tab-panels #panel-runs { display: block; }
        #tab-corr:checked ~ main .tab-bar label[for="tab-corr"],
        #tab-units:checked ~ main .tab-bar label[for="tab-units"],
        #tab-claims:checked ~ main .tab-bar label[for="tab-claims"],
        #tab-runs:checked ~ main .tab-bar label[for="tab-runs"] { color: var(--fg); border-bottom-color: var(--accent); font-weight: 600; }

        .data-table { width: 100%; border-collapse: collapse; font-size: 0.85rem; }
        .data-table th, .data-table td { text-align: left; padding: 0.5rem 0.6rem; border-bottom: 1px solid var(--border); vertical-align: top; }
        .data-table th { color: var(--muted); font-weight: 600; font-size: 0.75rem; text-transform: uppercase; }
        tr.stale { background: var(--amber-bg); }
        tr.selected { outline: 2px solid var(--accent); outline-offset: -2px; }
        tr.fail { background: var(--red-bg); }

        .badge { display: inline-block; padding: 0.1rem 0.5rem; border-radius: 999px; font-size: 0.75rem; font-weight: 600; }
        .status-accepted, .status-pass { background: var(--green-bg); color: var(--green); }
        .status-proposed { background: var(--gray-bg); color: var(--gray); }
        .status-defeated, .status-fail { background: var(--red-bg); color: var(--red); }
        .status-contested { background: var(--amber-bg); color: var(--amber); }
        .status-mapped { background: var(--gray-bg); color: var(--gray); }
        .status-in-progress { background: var(--amber-bg); color: var(--amber); }
        .status-mixed, .status-none { background: var(--gray-bg); color: var(--muted); }
        .badge.stale { background: var(--amber-bg); color: var(--amber); }
        .badge.auto { background: var(--gray-bg); color: var(--muted); }

        .unit { border: 1px solid var(--border); border-radius: 10px; padding: 1rem 1.25rem; margin-bottom: 1rem; background: var(--card-bg); }
        .unit.stale { border-left: 3px solid var(--amber); }
        .unit h3 { margin: 0 0 0.35rem; font-size: 1rem; }
        .unit-meta { font-size: 0.8rem; color: var(--muted); margin: 0 0 0.5rem; }
        .unit-body h2 { font-size: 0.85rem; text-transform: uppercase; color: var(--muted); margin: 1rem 0 0.35rem; }
        .unit-body p { margin: 0.25rem 0; }
        .unit-body ul { margin: 0.25rem 0; padding-left: 1.25rem; }

        details.popover, details.why { display: inline-block; margin: 0.15rem 0.3rem 0 0; font-size: 0.8rem; vertical-align: top; }
        details.popover summary, details.why summary { cursor: pointer; color: var(--accent); list-style: none; }
        details.popover summary::-webkit-details-marker, details.why summary::-webkit-details-marker { display: none; }
        .kv { display: grid; grid-template-columns: auto 1fr; gap: 0.15rem 0.6rem; margin: 0.35rem 0 0; font-size: 0.78rem; }
        .kv dt { color: var(--muted); }
        .kv dd { margin: 0; }
        .why-inputs { margin-left: 1rem; border-left: 1px dashed var(--border); padding-left: 0.6rem; }

        .muted { color: var(--muted); }
        .empty { color: var(--muted); font-style: italic; }
        .kp-footer { padding: 1rem 2rem; border-top: 1px solid var(--border); font-size: 0.8rem; color: var(--muted); }
        """;

    private const string Js = """
        (function () {
          "use strict";
          var rows = document.querySelectorAll("tr[data-corr-row]");
          for (var i = 0; i < rows.length; i++) {
            rows[i].addEventListener("click", function (ev) {
              var row = ev.currentTarget;
              var already = row.classList.contains("selected");
              var selected = document.querySelectorAll(".selected");
              for (var j = 0; j < selected.length; j++) { selected[j].classList.remove("selected"); }
              var hits = document.querySelectorAll(".tree-hit");
              for (var k = 0; k < hits.length; k++) { hits[k].classList.remove("tree-hit"); }
              if (already) { return; }
              row.classList.add("selected");
              highlight(row.getAttribute("data-source-entity"));
              highlight(row.getAttribute("data-target-entity"));
            });
          }

          function highlight(entityId) {
            if (!entityId) { return; }
            var node = findByEntityId(entityId);
            if (!node) { return; }
            node.classList.add("tree-hit");
            var d = node.closest("details");
            while (d) {
              d.open = true;
              var parent = d.parentElement;
              d = parent ? parent.closest("details") : null;
            }
            node.scrollIntoView({ block: "nearest" });
          }

          function findByEntityId(id) {
            var nodes = document.querySelectorAll("[data-entity-id]");
            for (var i = 0; i < nodes.length; i++) {
              if (nodes[i].getAttribute("data-entity-id") === id) { return nodes[i]; }
            }
            return null;
          }
        })();
        """;
}
