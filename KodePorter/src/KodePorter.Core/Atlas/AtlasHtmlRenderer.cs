using System.Text;
using System.Text.Json;

namespace KodePorter.Core.Atlas;

/// <summary>
/// Turns an <see cref="AtlasData"/> into the single self-contained HTML file (CONTRACT.md §8,
/// CONTRACT-M15.md §6 "Atlas v2"): inline CSS, a JSON data island, and vanilla-JS for cross-tree
/// correspondence highlighting, the overview treemaps' click-to-focus, and — the v2 scale
/// requirement — lazy client-side tree rendering. The source/target entity trees are NOT
/// pre-rendered server-side past the container element: at real-world scale (§6.1: "≥40k
/// entities") a `&lt;details&gt;`-per-entity document would blow both the render budget and the
/// 15MB file-size budget, so only the JSON data island carries the full tree; the DOM is built
/// lazily by <see cref="Js"/> as branches are expanded, with sibling pagination.
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

        sb.Append("<input type=\"radio\" name=\"kp-tab\" id=\"tab-overview\" class=\"kp-tab-radio\" checked>\n");
        sb.Append("<input type=\"radio\" name=\"kp-tab\" id=\"tab-corr\" class=\"kp-tab-radio\">\n");
        sb.Append("<input type=\"radio\" name=\"kp-tab\" id=\"tab-units\" class=\"kp-tab-radio\">\n");
        sb.Append("<input type=\"radio\" name=\"kp-tab\" id=\"tab-claims\" class=\"kp-tab-radio\">\n");
        sb.Append("<input type=\"radio\" name=\"kp-tab\" id=\"tab-runs\" class=\"kp-tab-radio\">\n");
        sb.Append("<input type=\"radio\" name=\"kp-tab\" id=\"tab-identity\" class=\"kp-tab-radio\">\n");

        sb.Append(RenderHeader(data.Header));
        sb.Append(RenderHealth(data.Health));
        sb.Append(RenderAbsenceDrilldown(data.Health));
        sb.Append(RenderFilterBar());

        sb.Append("<main class=\"layout\">\n");
        sb.Append("<section class=\"tree-pane\" id=\"source-tree-pane\">\n<h2>Source</h2>\n")
          .Append(RenderTreeContainer("source")).Append("</section>\n");

        sb.Append("<section class=\"tabs\">\n");
        sb.Append("<div class=\"tab-bar\">\n");
        sb.Append("<label for=\"tab-overview\">Overview</label>\n");
        sb.Append("<label for=\"tab-corr\">Correspondences</label>\n");
        sb.Append("<label for=\"tab-units\">Units</label>\n");
        sb.Append("<label for=\"tab-claims\">Claims</label>\n");
        sb.Append("<label for=\"tab-runs\">Runs</label>\n");
        sb.Append("<label for=\"tab-identity\">Identity</label>\n");
        sb.Append("</div>\n");
        sb.Append("<div class=\"tab-panels\">\n");
        sb.Append("<section class=\"tab-panel\" id=\"panel-overview\">\n").Append(RenderOverviewTab(data.Overview)).Append("</section>\n");
        sb.Append("<section class=\"tab-panel\" id=\"panel-corr\">\n").Append(RenderCorrespondencesTab(data.Correspondences)).Append("</section>\n");
        sb.Append("<section class=\"tab-panel\" id=\"panel-units\">\n").Append(RenderUnitsTab(data.Units)).Append("</section>\n");
        sb.Append("<section class=\"tab-panel\" id=\"panel-claims\">\n").Append(RenderClaimsTab(data.Claims, data.Label)).Append("</section>\n");
        sb.Append("<section class=\"tab-panel\" id=\"panel-runs\">\n").Append(RenderRunsTab(data.Runs)).Append("</section>\n");
        sb.Append("<section class=\"tab-panel\" id=\"panel-identity\">\n").Append(RenderIdentityTab()).Append("</section>\n");
        sb.Append("</div>\n");
        sb.Append("</section>\n");

        sb.Append("<section class=\"tree-pane\" id=\"target-tree-pane\">\n<h2>Target</h2>\n")
          .Append(RenderTreeContainer("target")).Append("</section>\n");
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
        sb.Append(TileLink("mapped", h.Mapped, anchor: "source-tree-pane"));
        sb.Append(TileLink("corresponded", h.Corresponded, tab: "tab-corr"));
        sb.Append(TileLink("candidates", h.Candidates, tab: "tab-corr"));
        sb.Append(TileLink("implemented", h.Implemented, tab: "tab-units"));
        sb.Append(TileLink("verified", h.Verified, tab: "tab-runs"));
        sb.Append(TileLink("stale", h.Stale, tab: "tab-claims"));
        sb.Append(HealthDrilldownTrigger("unknown", h.Absence.Unknown, "absence-unknown"));
        sb.Append(HealthDrilldownTrigger("not-yet-ported", h.Absence.NotYetPorted, "absence-not-yet-ported"));
        sb.Append(HealthDrilldownTrigger("deliberately-dropped", h.Absence.DeliberatelyDropped, "absence-deliberately-dropped"));
        sb.Append(HealthDrilldownTrigger("unexplained", h.TargetOnly.Unexplained, "absence-unexplained"));
        sb.Append(HealthDrilldownTrigger("intentional", h.TargetOnly.Intentional, "absence-intentional"));
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

    /// <summary>A health-strip tile that opens/closes an absence drill-down list rather than
    /// switching tabs (CONTRACT-M15.md §6.3: "absence kinds in health strip with drill-down
    /// lists"). Uses a checkbox+label pair (same no-script-required pattern as the tab radios) so
    /// the tile itself needs no click handler; the lazily-populated list underneath is filled by
    /// <see cref="Js"/> the first time it becomes visible.</summary>
    private static string HealthDrilldownTrigger(string dim, int count, string targetId)
    {
        string cls = "health-tile dim-" + dim + (count > 0 ? " nonzero" : "");
        string inner = $"<span class=\"tile-count\">{count}</span><span class=\"tile-label\">{Html.Escape(dim)}</span>";
        return $"<label class=\"{cls}\" for=\"drilldown-{Html.Attr(targetId)}\">{inner}</label>\n";
    }

    /// <summary>Checkbox-driven (no-JS-required to toggle open/closed) drill-down panels for each
    /// absence bucket; population of each list is lazy (CONTRACT-M15.md §6.1) via <see cref="Js"/>,
    /// triggered on first expand.</summary>
    private static string RenderAbsenceDrilldown(HealthReport h)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"absence-drilldowns\">\n");
        AppendBucket(sb, "absence-unknown", "Absence: unknown", h.Absence.Unknown);
        AppendBucket(sb, "absence-not-yet-ported", "Absence: not yet ported", h.Absence.NotYetPorted);
        AppendBucket(sb, "absence-deliberately-dropped", "Absence: deliberately dropped", h.Absence.DeliberatelyDropped);
        AppendBucket(sb, "absence-unexplained", "Target-only: unexplained", h.TargetOnly.Unexplained);
        AppendBucket(sb, "absence-intentional", "Target-only: intentional", h.TargetOnly.Intentional);
        sb.Append("</div>\n");
        return sb.ToString();

        static void AppendBucket(StringBuilder sb, string id, string title, int count)
        {
            sb.Append("<input type=\"checkbox\" class=\"drilldown-toggle\" id=\"drilldown-").Append(Html.Attr(id)).Append("\">\n");
            sb.Append("<div class=\"absence-panel\" id=\"panel-").Append(Html.Attr(id)).Append("\">\n");
            sb.Append("<div class=\"absence-panel-title\">").Append(Html.Escape(title)).Append(" (").Append(count).Append(")")
              .Append(" <label class=\"close-drilldown\" for=\"drilldown-").Append(Html.Attr(id)).Append("\">close</label></div>\n");
            sb.Append("<div class=\"lazy-list\" data-lazy-list=\"1\" data-source=\"").Append(Html.Attr(id)).Append("\"></div>\n");
            sb.Append("</div>\n");
        }
    }

    // ---- Filter bar --------------------------------------------------------------------------

    /// <summary>CONTRACT-M15.md §6.4: throttled symbolPath search, kind filter, "hide tests"
    /// (default ON — the non-test product surface is the load-bearing default per the FrankenTui
    /// probe: ~1/8 of a real map). Shared by both side trees; <see cref="Js"/> re-renders both on
    /// change.</summary>
    private static string RenderFilterBar()
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"filter-bar\">\n");
        sb.Append("<input type=\"search\" id=\"kp-search\" class=\"kp-search\" placeholder=\"Search symbolPath…\" autocomplete=\"off\">\n");
        sb.Append("<select id=\"kp-kind-filter\" class=\"kp-kind-filter\"><option value=\"\">all kinds</option></select>\n");
        sb.Append("<label class=\"kp-toggle\"><input type=\"checkbox\" id=\"kp-hide-tests\" checked> hide tests</label>\n");
        sb.Append("<span class=\"kp-filter-status\" id=\"kp-filter-status\"></span>\n");
        sb.Append("</div>\n");
        return sb.ToString();
    }

    // ---- Overview tab (CONTRACT-M15.md §6.2) --------------------------------------------------

    private static string RenderOverviewTab(AtlasOverview overview)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"overview\">\n");
        sb.Append("<p class=\"overview-intro\">Area = non-test entity count per top-level crate/namespace. ")
          .Append("Click a rectangle to focus that subtree in the detail tree and see its second-level breakdown.</p>\n");
        sb.Append("<div class=\"overview-legend\">\n")
          .Append("<span class=\"legend-item\"><span class=\"legend-swatch cov-corresponded\"></span> corresponded</span>\n")
          .Append("<span class=\"legend-item\"><span class=\"legend-swatch cov-candidate-only\"></span> candidate-only</span>\n")
          .Append("<span class=\"legend-item\"><span class=\"legend-swatch cov-uncovered\"></span> uncovered</span>\n")
          .Append("<span class=\"legend-item\"><span class=\"legend-swatch stale-swatch\"></span> amber border = contains stale</span>\n")
          .Append("</div>\n");
        sb.Append("<div class=\"overview-grid\">\n");
        sb.Append(RenderOverviewSide("source", overview.Source));
        sb.Append(RenderOverviewSide("target", overview.Target));
        sb.Append("</div>\n");
        sb.Append("</div>\n");
        return sb.ToString();
    }

    private static string RenderOverviewSide(string side, AtlasOverviewSide s)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"overview-side\">\n");
        sb.Append("<h3>").Append(Html.Escape(side)).Append("</h3>\n");
        // Visual-review defect fix: s.Groups already excludes test-only groups (zero non-test
        // entities, e.g. per-file Rust integration-test crates) from the primary count — reported
        // honestly here alongside TestOnlyGroupCount/TestTotal rather than silently dropped.
        sb.Append("<p class=\"overview-counts\">").Append(s.Groups.Count).Append(" groups · ")
          .Append(s.NonTestTotal).Append(" non-test entities");
        if (s.TestOnlyGroupCount > 0)
        {
            sb.Append(" (").Append(s.TestOnlyGroupCount).Append(" test-only groups not shown · ")
              .Append(s.TestTotal).Append(" test entities hidden by default)");
        }
        else if (s.TestTotal > 0)
        {
            sb.Append(" · ").Append(s.TestTotal).Append(" test entities (hidden by default)");
        }
        sb.Append("</p>\n");
        sb.Append("<div class=\"treemap-wrap\" data-treemap-side=\"").Append(Html.Attr(side)).Append("\">\n");
        sb.Append(s.Svg).Append('\n');
        sb.Append("</div>\n");
        sb.Append("<div class=\"treemap-drill\" id=\"treemap-drill-").Append(Html.Attr(side)).Append("\"></div>\n");
        sb.Append("</div>\n");
        return sb.ToString();
    }

    // ---- Trees (lazy, CONTRACT-M15.md §6.1) ----------------------------------------------------

    /// <summary>
    /// The full entity tree is NOT pre-rendered here — only the empty lazy-tree container. The
    /// data island (<c>kp-atlas-data</c>, <c>sourceTree</c>/<c>targetTree</c>) carries every node;
    /// <see cref="Js"/> builds root rows immediately and descendant rows on expand, paginating long
    /// sibling lists at 200. This is the scale-required departure from Atlas v1's server-rendered
    /// `&lt;details&gt;`-per-entity tree (CONTRACT-M15.md §6.1: "no `&lt;details&gt;`-per-entity
    /// pre-rendering at scale").
    /// </summary>
    private static string RenderTreeContainer(string side) =>
        $"<div class=\"lazy-tree\" data-lazy-tree=\"1\" data-side=\"{Html.Attr(side)}\" id=\"tree-{Html.Attr(side)}\"><p class=\"empty\">No basis pinned yet.</p></div>\n";

    // ---- Correspondences tab -------------------------------------------------------------------

    private static string RenderCorrespondencesTab(IReadOnlyList<AtlasCorrespondence> items)
    {
        var sb = new StringBuilder();
        sb.Append("<label class=\"kp-toggle corr-candidates-toggle\"><input type=\"checkbox\" id=\"kp-candidates-only\"> candidates only</label>\n");
        if (items.Count == 0)
        {
            sb.Append("<p class=\"empty\">No correspondences yet.</p>\n");
            return sb.ToString();
        }

        sb.Append("<table class=\"data-table\">\n<thead><tr><th>Type</th><th>Provenance</th><th>Source</th><th>Target</th><th>Unit</th><th>Criterion</th><th>Status</th></tr></thead>\n<tbody>\n");
        foreach (var c in items)
        {
            sb.Append("<tr class=\"corr-row").Append(c.Stale ? " stale" : "").Append('"')
              .Append(" data-corr-row=\"1\"")
              .Append(" data-provenance=\"").Append(Html.Attr(c.Provenance)).Append('"')
              .Append(" data-source-entity=\"").Append(Html.Attr(c.SourceEntityId ?? "")).Append('"')
              .Append(" data-target-entity=\"").Append(Html.Attr(c.TargetEntityId ?? "")).Append("\">\n");
            sb.Append("<td><span class=\"badge type-").Append(Html.Attr(c.Type)).Append("\">").Append(Html.Escape(c.Type)).Append("</span>");
            if (c.DivergenceKind is not null)
                sb.Append(" <span class=\"muted\">(").Append(Html.Escape(c.DivergenceKind)).Append(")</span>");
            sb.Append("</td>\n");
            sb.Append("<td>").Append(ProvenanceBadge(c.Provenance)).Append("</td>\n");
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

    /// <summary>CONTRACT-M15.md §6.3: candidate = dashed/gray, asserted = solid, verified = green
    /// tick. `verified` itself is never a stored provenance value (CONTRACT-M15.md §1.3) — this
    /// renderer only ever sees `candidate`/`asserted` from the data model; a future increment that
    /// wants the green-tick "verified" display state can derive it (correspondence's unit+criterion
    /// covered by an accepted kp.verification pass) and pass that string in here instead.</summary>
    private static string ProvenanceBadge(string provenance) =>
        provenance == "candidate"
            ? "<span class=\"badge prov-candidate\">candidate</span>"
            : "<span class=\"badge prov-asserted\">asserted</span>";

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
            sb.Append(" · depth: <span class=\"badge depth-").Append(Html.Attr(u.Depth)).Append("\">").Append(Html.Escape(u.Depth)).Append("</span>");
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
        sb.Append("<table class=\"data-table\">\n<thead><tr><th>Unit</th><th>Criterion</th><th>Verdict</th><th>Independence</th><th>Cases</th><th>Mismatches</th><th>Rerun</th></tr></thead>\n<tbody>\n");
        foreach (var r in runs)
        {
            sb.Append("<tr class=\"").Append(r.Verdict == "fail" ? "fail" : "pass").Append("\">\n");
            sb.Append("<td>").Append(Html.Escape(r.Unit)).Append("</td>\n");
            sb.Append("<td class=\"mono\">").Append(Html.Escape(r.Criterion)).Append("</td>\n");
            sb.Append("<td>").Append(StatusBadge(r.Verdict)).Append("</td>\n");
            sb.Append("<td><span class=\"badge indep-").Append(Html.Attr(SlugifyStatus(r.Independence))).Append("\">")
              .Append(Html.Escape(r.Independence)).Append("</span></td>\n");
            sb.Append("<td>").Append(r.PassCount).Append(" pass / ").Append(r.FailCount).Append(" fail</td>\n");
            sb.Append("<td>").Append(r.Mismatches.Count == 0 ? "—" : Html.Escape(string.Join(", ", r.Mismatches))).Append("</td>\n");
            sb.Append("<td><code class=\"rerun\">").Append(Html.Escape(r.RerunCommand)).Append("</code></td>\n");
            sb.Append("</tr>\n");
        }
        sb.Append("</tbody>\n</table>\n");
        return sb.ToString();
    }

    // ---- Identity tab (CONTRACT-M15.md §1.2/§6.3) ------------------------------------------------

    /// <summary>The small "identity" section: continuity candidates, never auto-confirmed, listed
    /// for human review. Populated lazily from the data island like the absence drill-downs.</summary>
    private static string RenderIdentityTab()
    {
        var sb = new StringBuilder();
        sb.Append("<p class=\"overview-intro\">Continuity candidates: machine-suggested identity continuations across bases ")
          .Append("(name-kind heuristic). Never auto-confirmed — for human review only.</p>\n");
        sb.Append("<div class=\"lazy-list\" data-lazy-list=\"1\" data-source=\"continuity\"></div>\n");
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
          --cov-corresponded: #86efac;
          --cov-candidate: #fde68a;
          --cov-uncovered: #fca5a5;
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
            --cov-corresponded: #166534;
            --cov-candidate: #854d0e;
            --cov-uncovered: #7f1d1d;
          }
        }
        :root[data-theme="dark"] {
          --bg: #0f1115; --fg: #e5e7eb; --muted: #9ca3af; --border: #262a33; --accent: #60a5fa;
          --amber: #fbbf24; --amber-bg: #3f2d05; --green: #4ade80; --green-bg: #0f2e1a;
          --gray: #9ca3af; --gray-bg: #1a1d24; --red: #f87171; --red-bg: #3a1414; --card-bg: #171a20;
          --cov-corresponded: #166534; --cov-candidate: #854d0e; --cov-uncovered: #7f1d1d;
        }
        :root[data-theme="light"] {
          --bg: #ffffff; --fg: #1a1a1a; --muted: #6b7280; --border: #e5e7eb; --accent: #2563eb;
          --amber: #b45309; --amber-bg: #fef3c7; --green: #15803d; --green-bg: #dcfce7;
          --gray: #6b7280; --gray-bg: #f3f4f6; --red: #b91c1c; --red-bg: #fee2e2; --card-bg: #f9fafb;
          --cov-corresponded: #86efac; --cov-candidate: #fde68a; --cov-uncovered: #fca5a5;
        }
        .kp-tab-radio, .drilldown-toggle { position: absolute; opacity: 0; pointer-events: none; }
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
        .dim-unknown.nonzero, .dim-unexplained.nonzero { border-color: var(--red); }
        .dim-unknown.nonzero .tile-count, .dim-unexplained.nonzero .tile-count { color: var(--red); }
        .dim-verified.nonzero .tile-count { color: var(--green); }

        .absence-panel { display: none; padding: 0.75rem 2rem; border-bottom: 1px solid var(--border); background: var(--card-bg); }
        .absence-panel-title { font-weight: 600; font-size: 0.85rem; margin-bottom: 0.5rem; }
        .close-drilldown { font-weight: 400; color: var(--accent); cursor: pointer; font-size: 0.78rem; margin-left: 0.5rem; }
        #drilldown-absence-unknown:checked ~ #panel-absence-unknown,
        #drilldown-absence-not-yet-ported:checked ~ #panel-absence-not-yet-ported,
        #drilldown-absence-deliberately-dropped:checked ~ #panel-absence-deliberately-dropped,
        #drilldown-absence-unexplained:checked ~ #panel-absence-unexplained,
        #drilldown-absence-intentional:checked ~ #panel-absence-intentional { display: block; }

        .filter-bar {
          display: flex;
          flex-wrap: wrap;
          gap: 0.6rem;
          align-items: center;
          padding: 0.6rem 2rem;
          border-bottom: 1px solid var(--border);
          background: var(--card-bg);
          font-size: 0.85rem;
        }
        .kp-search { flex: 1 1 16rem; padding: 0.35rem 0.6rem; border: 1px solid var(--border); border-radius: 6px; background: var(--bg); color: var(--fg); }
        .kp-kind-filter { padding: 0.35rem 0.5rem; border: 1px solid var(--border); border-radius: 6px; background: var(--bg); color: var(--fg); }
        .kp-toggle { display: inline-flex; align-items: center; gap: 0.3rem; color: var(--muted); cursor: pointer; }
        .kp-filter-status { color: var(--muted); font-size: 0.78rem; margin-left: auto; }

        .layout {
          display: grid;
          grid-template-columns: minmax(220px, 260px) minmax(0, 1fr) minmax(220px, 260px);
          gap: 0;
        }
        @media (max-width: 900px) {
          .layout { grid-template-columns: 1fr; }
        }
        .tree-pane { padding: 1rem 1.25rem; border-right: 1px solid var(--border); overflow-x: auto; max-height: 80vh; overflow-y: auto; }
        #target-tree-pane { border-right: none; border-left: 1px solid var(--border); }
        .tree-pane h2 { font-size: 0.85rem; text-transform: uppercase; letter-spacing: 0.04em; color: var(--muted); margin: 0 0 0.75rem; }

        .lazy-tree { font-size: 0.85rem; }
        .tree-row { display: flex; align-items: center; gap: 0.3rem; padding: 0.12rem 0; cursor: default; }
        .tree-row.has-children { cursor: pointer; }
        .tree-row .toggle { display: inline-block; width: 1em; color: var(--muted); flex: none; }
        .tree-row.has-children .toggle::before { content: "\25B8"; }
        .tree-row.has-children.expanded .toggle::before { content: "\25BE"; }
        .tree-children { margin-left: 1.1rem; display: none; }
        .tree-children.open { display: block; }
        .tree-row.stale { border-left: 3px solid var(--amber); padding-left: 0.3rem; }
        .tree-row.is-test { opacity: 0.5; }
        .tree-row.res-degraded .kind-badge, .tree-row.res-gap .kind-badge { text-decoration: line-through wavy; }
        .tree-row.res-gap { background: repeating-linear-gradient(45deg, transparent, transparent 4px, var(--red-bg) 4px, var(--red-bg) 8px); }
        .kind-badge { font-size: 0.65rem; text-transform: uppercase; color: var(--muted); border: 1px solid var(--border); border-radius: 4px; padding: 0 0.3rem; flex: none; }
        .node-name { font-family: ui-monospace, SFMono-Regular, Consolas, "Liberation Mono", monospace; overflow-wrap: anywhere; }
        .test-badge { font-size: 0.6rem; color: var(--muted); border-radius: 4px; padding: 0 0.25rem; background: var(--gray-bg); flex: none; }
        .tree-hit { outline: 2px solid var(--accent); outline-offset: 2px; border-radius: 3px; }
        .show-more { margin: 0.25rem 0 0.25rem 1.1rem; font-size: 0.78rem; padding: 0.2rem 0.55rem; border: 1px solid var(--border); border-radius: 6px; background: var(--card-bg); color: var(--accent); cursor: pointer; }
        .show-more[hidden] { display: none; }

        .lazy-list-item { padding: 0.2rem 0; border-bottom: 1px solid var(--border); font-size: 0.83rem; }
        .lazy-list-item .mono { display: block; }
        .lazy-list-item .note { color: var(--muted); font-size: 0.78rem; }

        .tabs { padding: 1rem 1.5rem; min-width: 0; }
        .tab-bar { display: flex; gap: 0.25rem; border-bottom: 1px solid var(--border); margin-bottom: 1rem; flex-wrap: wrap; }
        .tab-bar label { padding: 0.5rem 0.9rem; cursor: pointer; font-size: 0.9rem; color: var(--muted); border-bottom: 2px solid transparent; }
        .tab-panel { display: none; }
        #tab-overview:checked ~ main .tab-panels #panel-overview,
        #tab-corr:checked ~ main .tab-panels #panel-corr,
        #tab-units:checked ~ main .tab-panels #panel-units,
        #tab-claims:checked ~ main .tab-panels #panel-claims,
        #tab-runs:checked ~ main .tab-panels #panel-runs,
        #tab-identity:checked ~ main .tab-panels #panel-identity { display: block; }
        #tab-overview:checked ~ main .tab-bar label[for="tab-overview"],
        #tab-corr:checked ~ main .tab-bar label[for="tab-corr"],
        #tab-units:checked ~ main .tab-bar label[for="tab-units"],
        #tab-claims:checked ~ main .tab-bar label[for="tab-claims"],
        #tab-runs:checked ~ main .tab-bar label[for="tab-runs"],
        #tab-identity:checked ~ main .tab-bar label[for="tab-identity"] { color: var(--fg); border-bottom-color: var(--accent); font-weight: 600; }

        .overview-intro { color: var(--muted); font-size: 0.85rem; max-width: 60ch; }
        .overview-legend { display: flex; flex-wrap: wrap; gap: 0.9rem; font-size: 0.78rem; color: var(--muted); margin-bottom: 0.75rem; }
        .legend-item { display: inline-flex; align-items: center; gap: 0.3rem; }
        .legend-swatch { width: 0.85em; height: 0.85em; border-radius: 3px; display: inline-block; border: 1px solid var(--border); }
        .legend-swatch.cov-corresponded { background: var(--cov-corresponded); }
        .legend-swatch.cov-candidate-only { background: var(--cov-candidate); }
        .legend-swatch.cov-uncovered { background: var(--cov-uncovered); }
        .legend-swatch.stale-swatch { background: transparent; border: 2px solid var(--amber); }
        .overview-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 1.5rem; }
        @media (max-width: 720px) { .overview-grid { grid-template-columns: 1fr; } }
        .overview-side h3 { font-size: 0.85rem; text-transform: uppercase; letter-spacing: 0.04em; color: var(--muted); margin: 0 0 0.35rem; }
        .overview-counts { font-size: 0.78rem; color: var(--muted); margin: 0 0 0.5rem; }
        .treemap-wrap { border: 1px solid var(--border); border-radius: 10px; overflow: hidden; background: var(--card-bg); }
        .treemap-svg { width: 100%; height: auto; display: block; }
        .treemap-rect { stroke: var(--border); stroke-width: 1; cursor: pointer; }
        .treemap-rect.cov-corresponded { fill: var(--cov-corresponded); }
        .treemap-rect.cov-candidate-only { fill: var(--cov-candidate); }
        .treemap-rect.cov-uncovered { fill: var(--cov-uncovered); }
        .treemap-rect.stale { stroke: var(--amber); stroke-width: 3; }
        .treemap-rect:hover { opacity: 0.85; }
        .treemap-label { font-size: 8px; fill: var(--fg); pointer-events: none; font-family: ui-monospace, SFMono-Regular, Consolas, monospace; }
        .treemap-empty { fill: var(--muted); font-size: 12px; }
        .treemap-drill { margin-top: 0.6rem; font-size: 0.78rem; }
        .treemap-drill-title { color: var(--muted); margin-bottom: 0.3rem; }
        .treemap-drill-bar { display: flex; height: 1.4rem; border-radius: 6px; overflow: hidden; border: 1px solid var(--border); }
        .treemap-drill-seg { display: flex; align-items: center; justify-content: center; overflow: hidden; white-space: nowrap; font-size: 0.68rem; color: var(--fg); cursor: pointer; }
        .treemap-drill-legend { display: flex; flex-wrap: wrap; gap: 0.4rem 0.8rem; margin-top: 0.3rem; color: var(--muted); }

        .data-table { width: 100%; border-collapse: collapse; font-size: 0.85rem; }
        .data-table th, .data-table td { text-align: left; padding: 0.5rem 0.6rem; border-bottom: 1px solid var(--border); vertical-align: top; }
        .data-table th { color: var(--muted); font-weight: 600; font-size: 0.75rem; text-transform: uppercase; }
        tr.stale { background: var(--amber-bg); }
        tr.selected { outline: 2px solid var(--accent); outline-offset: -2px; }
        tr.fail { background: var(--red-bg); }
        tr.corr-row.candidates-hidden { display: none; }

        .corr-candidates-toggle { margin-bottom: 0.6rem; display: inline-flex; }

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
        .prov-candidate { background: var(--gray-bg); color: var(--muted); border: 1px dashed var(--muted); }
        .prov-asserted { background: var(--card-bg); color: var(--fg); border: 1px solid var(--border); }
        .prov-verified { background: var(--green-bg); color: var(--green); border: 1px solid var(--green); }
        .depth-thin { background: var(--gray-bg); color: var(--muted); }
        .depth-dossiered { background: var(--green-bg); color: var(--green); }
        .indep-independentlyderived { background: var(--green-bg); color: var(--green); }
        .indep-implementationcoupled { background: var(--amber-bg); color: var(--amber); }
        .indep-unknown { background: var(--gray-bg); color: var(--muted); }

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

          var DATA = JSON.parse(document.getElementById("kp-atlas-data").textContent);
          var PAGE_SIZE = 200;

          var state = { search: "", kind: "", hideTests: true };

          // ---- generic paginated list rendering (CONTRACT-M15.md §6.1: sibling pagination) ----

          function renderPaged(container, items, renderItem) {
            container.innerHTML = "";
            if (items.length === 0) {
              var p = document.createElement("p");
              p.className = "empty";
              p.textContent = "None.";
              container.appendChild(p);
              return;
            }
            var list = document.createElement("div");
            container.appendChild(list);
            var shown = 0;
            var btn = document.createElement("button");
            btn.type = "button";
            btn.className = "show-more";
            function renderMore() {
              var end = Math.min(items.length, shown + PAGE_SIZE);
              var frag = document.createDocumentFragment();
              for (var i = shown; i < end; i++) { frag.appendChild(renderItem(items[i])); }
              list.appendChild(frag);
              shown = end;
              if (shown < items.length) {
                btn.textContent = "Show more (" + (items.length - shown) + " remaining)";
                btn.hidden = false;
              } else {
                btn.hidden = true;
              }
            }
            btn.addEventListener("click", renderMore);
            container.appendChild(btn);
            renderMore();
          }

          // ---- entity tree (lazy, filtered) ----

          function topLevelKey(symbolPath) {
            var idx2 = symbolPath.indexOf("::");
            if (idx2 >= 0) { return symbolPath.slice(0, idx2); }
            var idx1 = symbolPath.indexOf(".");
            if (idx1 >= 0) { return symbolPath.slice(0, idx1); }
            return symbolPath;
          }

          // Mirrors AtlasOverviewBuilder.TwoSegmentKey (C#) exactly: the first two `::`/`.`-
          // segments, falling back to the whole symbolPath when there is no second segment.
          function twoSegmentKey(symbolPath) {
            var idx2a = symbolPath.indexOf("::");
            if (idx2a >= 0) {
              var idx2b = symbolPath.indexOf("::", idx2a + 2);
              return idx2b >= 0 ? symbolPath.slice(0, idx2b) : symbolPath;
            }
            var idx1a = symbolPath.indexOf(".");
            if (idx1a >= 0) {
              var idx1b = symbolPath.indexOf(".", idx1a + 1);
              return idx1b >= 0 ? symbolPath.slice(0, idx1b) : symbolPath;
            }
            return symbolPath;
          }

          // The overview treemap's actual grouping key for one entity: topLevelKey, UNLESS this
          // entity falls under the one dominant first-segment group the server split into
          // two-segment groups (AtlasOverviewBuilder.FindDominantGroupToSplit) — then
          // twoSegmentKey instead. Kept in lockstep with the server via
          // DATA.source/targetTreemapSplitPrefix so tree-row tagging (data-topkey) and the
          // drill-down breakdown always key entities the same way the server-rendered treemap
          // rectangles (data-key) did.
          function groupKey(side, symbolPath) {
            var top = topLevelKey(symbolPath);
            var splitPrefix = side === "source" ? DATA.sourceTreemapSplitPrefix : DATA.targetTreemapSplitPrefix;
            return splitPrefix && top === splitPrefix ? twoSegmentKey(symbolPath) : top;
          }

          function makeTreeController(side, containerId, nodes) {
            var byId = {};
            var childrenByParent = {};
            var idSet = {};
            for (var i = 0; i < nodes.length; i++) { idSet[nodes[i].id] = true; }
            for (i = 0; i < nodes.length; i++) {
              var n = nodes[i];
              byId[n.id] = n;
              var pid = n.parentId && idSet[n.parentId] ? n.parentId : "__root__";
              if (!childrenByParent[pid]) { childrenByParent[pid] = []; }
              childrenByParent[pid].push(n);
            }
            var container = document.getElementById(containerId);
            var kindSet = {};
            for (i = 0; i < nodes.length; i++) { kindSet[nodes[i].kind] = true; }

            function passes(node) {
              if (state.hideTests && node.isTest) { return false; }
              if (state.kind && node.kind !== state.kind) { return false; }
              return true;
            }

            function badgesFor(node) {
              var cls = "tree-row";
              var kids = childrenByParent[node.id];
              if (kids && kids.length > 0) { cls += " has-children"; }
              if (node.stale) { cls += " stale"; }
              if (node.isTest) { cls += " is-test"; }
              if (node.resolution && node.resolution !== "clean") { cls += " res-" + node.resolution; }
              return cls;
            }

            function makeRow(node) {
              var row = document.createElement("div");
              row.className = badgesFor(node);
              row.setAttribute("data-entity-id", node.id);
              row.setAttribute("data-topkey", groupKey(side, node.symbolPath));
              var toggle = document.createElement("span");
              toggle.className = "toggle";
              row.appendChild(toggle);
              var kind = document.createElement("span");
              kind.className = "kind-badge kind-" + node.kind;
              kind.textContent = node.kind;
              row.appendChild(kind);
              var name = document.createElement("span");
              name.className = "node-name";
              name.textContent = node.name;
              row.appendChild(name);
              if (node.isTest) {
                var t = document.createElement("span");
                t.className = "test-badge";
                t.textContent = "test";
                row.appendChild(t);
              }
              if (node.resolution && node.resolution !== "clean") {
                var r = document.createElement("span");
                r.className = "test-badge";
                r.textContent = node.resolution;
                row.appendChild(r);
              }

              var kids = childrenByParent[node.id];
              var childContainer = null;
              if (kids && kids.length > 0) {
                childContainer = document.createElement("div");
                childContainer.className = "tree-children";
                row.addEventListener("click", function () {
                  var willOpen = !childContainer.classList.contains("open");
                  if (willOpen && !childContainer.hasChildNodes()) {
                    renderPaged(childContainer, kids.filter(passes), makeRow);
                  }
                  childContainer.classList.toggle("open", willOpen);
                  row.classList.toggle("expanded", willOpen);
                });
              }

              var wrap = document.createElement("div");
              wrap.appendChild(row);
              if (childContainer) { wrap.appendChild(childContainer); }
              return wrap;
            }

            function rebuild() {
              if (state.search) {
                var q = state.search.toLowerCase();
                var matches = nodes.filter(function (n) {
                  return n.symbolPath.toLowerCase().indexOf(q) >= 0 && passes(n);
                });
                renderPaged(container, matches, function (n) {
                  var d = document.createElement("div");
                  d.className = "lazy-list-item";
                  var badge = "<span class=\"kind-badge kind-" + n.kind + "\">" + n.kind + "</span>";
                  d.innerHTML = badge + " <span class=\"mono\">" + n.symbolPath.replace(/[<>&]/g, function (c) {
                    return c === "<" ? "&lt;" : c === ">" ? "&gt;" : "&amp;";
                  }) + "</span>";
                  d.setAttribute("data-entity-id", n.id);
                  return d;
                });
                return;
              }
              var roots = (childrenByParent["__root__"] || []).filter(passes);
              renderPaged(container, roots, makeRow);
            }

            return { rebuild: rebuild, kinds: kindSet, focus: function (key) {
              rebuild();
              var rows = container.querySelectorAll('[data-topkey="' + cssEscape(key) + '"]');
              if (rows.length === 0) { return; }
              var first = rows[0];
              first.classList.add("tree-hit");
              first.scrollIntoView({ block: "center" });
              var toggle = first.classList.contains("has-children") ? first : null;
              if (toggle && !toggle.classList.contains("expanded")) { toggle.click(); }
              setTimeout(function () { first.classList.remove("tree-hit"); }, 2000);
            } };
          }

          function cssEscape(s) {
            return s.replace(/[^a-zA-Z0-9_-]/g, function (c) { return "\\" + c; });
          }

          var sourceTree = makeTreeController("source", "tree-source", DATA.sourceTree || []);
          var targetTree = makeTreeController("target", "tree-target", DATA.targetTree || []);

          // ---- filter bar ----

          var kindFilterEl = document.getElementById("kp-kind-filter");
          var allKinds = {};
          Object.keys(sourceTree.kinds).forEach(function (k) { allKinds[k] = true; });
          Object.keys(targetTree.kinds).forEach(function (k) { allKinds[k] = true; });
          Object.keys(allKinds).sort().forEach(function (k) {
            var opt = document.createElement("option");
            opt.value = k;
            opt.textContent = k;
            kindFilterEl.appendChild(opt);
          });

          function updateFilterStatus() {
            var el = document.getElementById("kp-filter-status");
            var bits = [];
            if (state.hideTests) { bits.push("tests hidden"); }
            if (state.kind) { bits.push("kind=" + state.kind); }
            if (state.search) { bits.push('search="' + state.search + '"'); }
            el.textContent = bits.join(" · ");
          }

          function rebuildTrees() {
            sourceTree.rebuild();
            targetTree.rebuild();
            updateFilterStatus();
          }

          var searchTimer = null;
          document.getElementById("kp-search").addEventListener("input", function (ev) {
            var value = ev.target.value;
            if (searchTimer) { clearTimeout(searchTimer); }
            searchTimer = setTimeout(function () {
              state.search = value.trim();
              rebuildTrees();
            }, 150); // throttled (CONTRACT-M15.md §6.4)
          });
          kindFilterEl.addEventListener("change", function (ev) {
            state.kind = ev.target.value;
            rebuildTrees();
          });
          document.getElementById("kp-hide-tests").addEventListener("change", function (ev) {
            state.hideTests = ev.target.checked;
            rebuildTrees();
          });

          rebuildTrees();

          // ---- correspondences: candidates-only toggle ----

          var candidatesOnlyEl = document.getElementById("kp-candidates-only");
          if (candidatesOnlyEl) {
            candidatesOnlyEl.addEventListener("change", function (ev) {
              var rows = document.querySelectorAll("tr[data-corr-row]");
              for (var i = 0; i < rows.length; i++) {
                var isCandidate = rows[i].getAttribute("data-provenance") === "candidate";
                rows[i].classList.toggle("candidates-hidden", ev.target.checked && !isCandidate);
              }
            });
          }

          // ---- correspondence row -> tree highlight ----

          var rows = document.querySelectorAll("tr[data-corr-row]");
          for (var ri = 0; ri < rows.length; ri++) {
            rows[ri].addEventListener("click", function (ev) {
              var row = ev.currentTarget;
              var already = row.classList.contains("selected");
              var selected = document.querySelectorAll(".selected");
              for (var j = 0; j < selected.length; j++) { selected[j].classList.remove("selected"); }
              var hits = document.querySelectorAll(".tree-hit");
              for (var k = 0; k < hits.length; k++) { hits[k].classList.remove("tree-hit"); }
              if (already) { return; }
              row.classList.add("selected");
              highlightEntity(row.getAttribute("data-source-entity"));
              highlightEntity(row.getAttribute("data-target-entity"));
            });
          }

          function highlightEntity(entityId) {
            if (!entityId) { return; }
            var node = findByEntityId(entityId);
            if (!node) { return; }
            node.classList.add("tree-hit");
            var d = node.closest(".tree-children");
            while (d) {
              d.classList.add("open");
              var toggleRow = d.previousElementSibling;
              if (toggleRow) { toggleRow.classList.add("expanded"); }
              var parentWrap = d.parentElement;
              d = parentWrap ? parentWrap.closest(".tree-children") : null;
            }
            node.scrollIntoView({ block: "nearest" });
          }

          function findByEntityId(id) {
            return document.querySelector('[data-entity-id="' + cssEscape(id) + '"]');
          }

          // ---- lazy lists (absence drill-downs + continuity candidates) ----

          var absenceSources = {
            "absence-unknown": DATA.absences.sourceUnknown,
            "absence-not-yet-ported": DATA.absences.sourceNotYetPorted,
            "absence-deliberately-dropped": DATA.absences.sourceDeliberatelyDropped,
            "absence-unexplained": DATA.absences.targetUnexplained,
            "absence-intentional": DATA.absences.targetIntentional
          };

          function renderAbsenceItem(item) {
            var d = document.createElement("div");
            d.className = "lazy-list-item";
            var html = "<span class=\"mono\">" + escapeHtml(item.symbolPath) + "</span>";
            if (item.note) { html += "<span class=\"note\">" + escapeHtml(item.note) + "</span>"; }
            d.innerHTML = html;
            return d;
          }

          function renderContinuityItem(item) {
            var d = document.createElement("div");
            d.className = "lazy-list-item";
            d.innerHTML = "<span class=\"kind-badge\">" + escapeHtml(item.kind) + "</span> " +
              "<span class=\"mono\">" + escapeHtml(item.fromSymbolPath) + "</span> → " +
              "<span class=\"mono\">" + escapeHtml(item.toSymbolPath) + "</span> " +
              "<span class=\"note\">(" + escapeHtml(item.heuristic) + ", " + escapeHtml(item.status) + ")</span>";
            return d;
          }

          function escapeHtml(s) {
            return String(s).replace(/[&<>"]/g, function (c) {
              return c === "&" ? "&amp;" : c === "<" ? "&lt;" : c === ">" ? "&gt;" : "&quot;";
            });
          }

          var lazyLists = document.querySelectorAll("[data-lazy-list]");
          var lazyListRendered = {};
          function populateLazyList(el) {
            var src = el.getAttribute("data-source");
            if (lazyListRendered[src]) { return; }
            lazyListRendered[src] = true;
            if (src === "continuity") {
              renderPaged(el, DATA.continuityCandidates || [], renderContinuityItem);
              return;
            }
            renderPaged(el, absenceSources[src] || [], renderAbsenceItem);
          }
          for (var li = 0; li < lazyLists.length; li++) {
            (function (el) {
              var checkbox = document.getElementById(el.getAttribute("data-source") ? "drilldown-" + el.getAttribute("data-source") : "");
              if (checkbox) {
                checkbox.addEventListener("change", function () { if (checkbox.checked) { populateLazyList(el); } });
              } else {
                // Identity tab's list has no drilldown checkbox — populate once visible via the tab radio.
                var tabRadio = document.getElementById("tab-identity");
                if (tabRadio) {
                  tabRadio.addEventListener("change", function () { if (tabRadio.checked) { populateLazyList(el); } });
                  if (tabRadio.checked) { populateLazyList(el); }
                }
              }
            })(lazyLists[li]);
          }

          // ---- overview treemaps: click focuses tree + shows a second-level breakdown ----

          function secondLevelKey(symbolPath, prefix) {
            var rest = symbolPath.slice(prefix.length);
            if (rest.indexOf("::") === 0) { rest = rest.slice(2); }
            else if (rest.indexOf(".") === 0) { rest = rest.slice(1); }
            var idx2 = rest.indexOf("::");
            if (idx2 >= 0) { return rest.slice(0, idx2); }
            var idx1 = rest.indexOf(".");
            if (idx1 >= 0) { return rest.slice(0, idx1); }
            return rest || "(self)";
          }

          function renderDrill(side, key) {
            var el = document.getElementById("treemap-drill-" + side);
            var nodes = side === "source" ? (DATA.sourceTree || []) : (DATA.targetTree || []);
            var inGroup = nodes.filter(function (n) { return !n.isTest && groupKey(side, n.symbolPath) === key; });
            var counts = {};
            var order = [];
            for (var i = 0; i < inGroup.length; i++) {
              var k = secondLevelKey(inGroup[i].symbolPath, key);
              if (!counts[k]) { counts[k] = 0; order.push(k); }
              counts[k]++;
            }
            order.sort(function (a, b) { return counts[b] - counts[a] || (a < b ? -1 : 1); });
            var total = inGroup.length;
            el.innerHTML = "";
            var title = document.createElement("div");
            title.className = "treemap-drill-title";
            title.textContent = key + " — " + total + " non-test entities, " + order.length + " subgroups (level 2):";
            el.appendChild(title);
            var bar = document.createElement("div");
            bar.className = "treemap-drill-bar";
            var legend = document.createElement("div");
            legend.className = "treemap-drill-legend";
            var palette = ["#60a5fa", "#f472b6", "#34d399", "#fbbf24", "#a78bfa", "#f87171", "#22d3ee", "#a3e635"];
            order.slice(0, 12).forEach(function (k, idx) {
              var seg = document.createElement("div");
              seg.className = "treemap-drill-seg";
              var pct = total > 0 ? (counts[k] / total * 100) : 0;
              seg.style.width = pct + "%";
              seg.style.background = palette[idx % palette.length];
              seg.title = k + ": " + counts[k];
              seg.addEventListener("click", function () {
                var controller = side === "source" ? sourceTree : targetTree;
                controller.focus(key);
              });
              bar.appendChild(seg);
              var item = document.createElement("span");
              item.textContent = k + " (" + counts[k] + ")";
              legend.appendChild(item);
            });
            el.appendChild(bar);
            el.appendChild(legend);
          }

          var treemapWraps = document.querySelectorAll("[data-treemap-side]");
          for (var tw = 0; tw < treemapWraps.length; tw++) {
            (function (wrap) {
              var side = wrap.getAttribute("data-treemap-side");
              wrap.addEventListener("click", function (ev) {
                var target = ev.target;
                var key = target.getAttribute && target.getAttribute("data-key");
                if (!key) { return; }
                var controller = side === "source" ? sourceTree : targetTree;
                controller.focus(key);
                renderDrill(side, key);
              });
            })(treemapWraps[tw]);
          }
        })();
        """;
}
