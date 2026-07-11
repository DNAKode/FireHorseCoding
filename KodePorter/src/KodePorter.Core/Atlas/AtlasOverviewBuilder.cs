using System.Globalization;
using System.Text;
using KodePorter.Core.Model;

namespace KodePorter.Core.Atlas;

/// <summary>
/// Builds the Overview panel's two build-time SVG treemaps (CONTRACT-M15.md §6.2): one rectangle
/// per top-level crate/namespace, area = non-test entity count, fill = coverage class, amber
/// border = contains stale. Pure SVG, deterministic (squarified layout over a sorted input),
/// no client-side layout for this level. A shallow, JS-driven second-level breakdown is rendered
/// on click by <see cref="AtlasHtmlRenderer"/>'s script from the data island directly — building
/// per-subgroup SVGs for every crate/namespace at generation time would not scale to 40k entities
/// (most groups are never clicked), so only level 1 is pre-rendered (CONTRACT-M15.md §6.2: "second
/// level on click-zoom, one level deep is enough").
/// </summary>
internal static class AtlasOverviewBuilder
{
    private const double CanvasWidth = 480;
    private const double CanvasHeight = 300;

    public static AtlasOverview Build(
        IReadOnlyList<Entity> sourceEntities, IReadOnlyList<Entity> targetEntities,
        ISet<string> sourceCorresponded, ISet<string> sourceCandidateOnly, ISet<string> sourceStale,
        ISet<string> targetCorresponded, ISet<string> targetCandidateOnly, ISet<string> targetStale)
    {
        var source = BuildSide(sourceEntities, sourceCorresponded, sourceCandidateOnly, sourceStale);
        var target = BuildSide(targetEntities, targetCorresponded, targetCandidateOnly, targetStale);
        return new AtlasOverview(source, target);
    }

    /// <summary>The top-level crate/namespace of a symbolPath (CONTRACT-M15.md §6.2): the first
    /// `::`-segment for rust-shaped paths, else the first `.`-segment for C#-shaped paths, else the
    /// whole path (a bare top-level module/type has no separator).</summary>
    public static string TopLevelKey(string symbolPath)
    {
        int idxColon = symbolPath.IndexOf("::", StringComparison.Ordinal);
        if (idxColon >= 0)
            return symbolPath[..idxColon];
        int idxDot = symbolPath.IndexOf('.');
        return idxDot >= 0 ? symbolPath[..idxDot] : symbolPath;
    }

    private static AtlasOverviewSide BuildSide(
        IReadOnlyList<Entity> entities, ISet<string> corresponded, ISet<string> candidateOnly, ISet<string> stale)
    {
        var byKey = entities.GroupBy(e => TopLevelKey(e.SymbolPath), StringComparer.Ordinal);

        var groups = new List<AtlasTreemapGroup>();
        int nonTestTotal = 0, testTotal = 0;
        foreach (var g in byKey)
        {
            int nonTest = g.Count(e => !e.IsTest);
            int test = g.Count(e => e.IsTest);
            nonTestTotal += nonTest;
            testTotal += test;

            // Coverage classification is over the group's non-test entities (mirrors "area = non-test
            // entity count" — a group's headline coverage class describes its non-test surface).
            var nonTestEntities = g.Where(e => !e.IsTest).ToList();
            string coverage =
                nonTestEntities.Any(e => corresponded.Contains(e.SymbolPath)) ? "corresponded"
                : nonTestEntities.Any(e => candidateOnly.Contains(e.SymbolPath)) ? "candidate-only"
                : "uncovered";
            bool groupStale = g.Any(e => stale.Contains(e.SymbolPath));

            groups.Add(new AtlasTreemapGroup(g.Key, nonTest, test, coverage, groupStale));
        }

        // Deterministic squarify input order: descending area, ties broken by key (K-D3 discipline
        // — no incidental ordering leaking through).
        var layoutOrder = groups
            .Where(g => g.NonTestCount > 0)
            .OrderByDescending(g => g.NonTestCount)
            .ThenBy(g => g.Key, StringComparer.Ordinal)
            .ToList();

        string svg = RenderSvg(layoutOrder);

        var displayOrder = groups.OrderByDescending(g => g.NonTestCount).ThenBy(g => g.Key, StringComparer.Ordinal).ToList();
        return new AtlasOverviewSide(svg, displayOrder, nonTestTotal, testTotal);
    }

    private static string RenderSvg(IReadOnlyList<AtlasTreemapGroup> groups)
    {
        var sb = new StringBuilder();
        // NB: no `xmlns` attribute — this SVG is inline in an HTML5 document (never served
        // standalone as `image/svg+xml`), where the HTML parser auto-namespaces `<svg>` content;
        // adding the XML namespace URI here would (harmlessly) trip a naive "contains http://"
        // self-containment scan (CONTRACT.md §8) despite being a namespace identifier, not a
        // network request.
        sb.Append("<svg class=\"treemap-svg\" viewBox=\"0 0 ").Append(F(CanvasWidth)).Append(' ').Append(F(CanvasHeight))
          .Append("\" role=\"img\" aria-label=\"treemap\">\n");

        if (groups.Count == 0)
        {
            sb.Append("<text x=\"8\" y=\"20\" class=\"treemap-empty\">No entities pinned.</text>\n</svg>");
            return sb.ToString();
        }

        var items = groups.Select(g => (Key: g.Key, Value: (double)g.NonTestCount)).ToList();
        var rects = Squarify.Layout(items, 0, 0, CanvasWidth, CanvasHeight);
        var byKey = groups.ToDictionary(g => g.Key, g => g, StringComparer.Ordinal);

        foreach (var (key, rect) in rects)
        {
            var g = byKey[key];
            double x = Math.Round(rect.X, 2), y = Math.Round(rect.Y, 2);
            double w = Math.Round(rect.W, 2), h = Math.Round(rect.H, 2);
            sb.Append("<rect class=\"treemap-rect cov-").Append(Html.Attr(g.Coverage)).Append(g.Stale ? " stale" : "").Append('"')
              .Append(" x=\"").Append(F(x)).Append("\" y=\"").Append(F(y))
              .Append("\" width=\"").Append(F(Math.Max(w, 0))).Append("\" height=\"").Append(F(Math.Max(h, 0)))
              .Append("\" data-key=\"").Append(Html.Attr(g.Key)).Append('"')
              .Append(" data-nontest=\"").Append(g.NonTestCount).Append('"')
              .Append(" data-test=\"").Append(g.TestCount).Append('"')
              .Append(" data-coverage=\"").Append(Html.Attr(g.Coverage)).Append("\">\n");
            sb.Append("<title>").Append(Html.Escape(g.Key)).Append(" — ").Append(g.NonTestCount)
              .Append(" entities (").Append(Html.Escape(g.Coverage)).Append(g.Stale ? ", stale" : "").Append(")</title>\n");
            sb.Append("</rect>\n");

            if (w > 34 && h > 16)
            {
                string label = g.Key.Length > 18 ? g.Key[..17] + "…" : g.Key;
                sb.Append("<text class=\"treemap-label\" x=\"").Append(F(x + 4)).Append("\" y=\"").Append(F(y + 14))
                  .Append("\" data-key=\"").Append(Html.Attr(g.Key)).Append("\">").Append(Html.Escape(label)).Append("</text>\n");
            }
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    private static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);
}

internal readonly record struct TreemapRect(double X, double Y, double W, double H);

/// <summary>
/// Squarified treemap layout (Bruls/Huizing/van Wijk), kept deliberately small: lays a sorted
/// (descending value) list of items into a rectangle, always growing rows along the rectangle's
/// current shorter side and closing a row once adding the next item would worsen its aspect
/// ratio. Pure function of its inputs — deterministic across runs (CONTRACT-M15.md §6: "two `kp
/// atlas` runs ... differ ONLY in the generated-timestamp field").
/// </summary>
internal static class Squarify
{
    public static List<(string Key, TreemapRect Rect)> Layout(IReadOnlyList<(string Key, double Value)> items, double x, double y, double w, double h)
    {
        var result = new List<(string, TreemapRect)>();
        if (items.Count == 0 || w <= 0 || h <= 0)
            return result;
        double total = items.Sum(i => i.Value);
        if (total <= 0)
            return result;
        double scale = (w * h) / total;
        var remaining = new Queue<(string Key, double Area)>(items.Select(i => (i.Key, Area: i.Value * scale)));
        LayoutRec(remaining, x, y, w, h, result);
        return result;
    }

    private static void LayoutRec(Queue<(string Key, double Area)> items, double x, double y, double w, double h, List<(string, TreemapRect)> result)
    {
        while (items.Count > 0)
        {
            double side = Math.Min(w, h);
            var pending = items.ToList();
            var row = new List<(string Key, double Area)> { pending[0] };
            double rowSum = pending[0].Area;
            double bestWorst = WorstAspect(row, rowSum, side);
            int taken = 1;

            while (taken < pending.Count)
            {
                var candidate = new List<(string, double)>(row) { pending[taken] };
                double candidateSum = rowSum + pending[taken].Area;
                double worst = WorstAspect(candidate, candidateSum, side);
                if (worst <= bestWorst)
                {
                    row = candidate;
                    rowSum = candidateSum;
                    bestWorst = worst;
                    taken++;
                }
                else
                {
                    break;
                }
            }

            for (int k = 0; k < taken; k++)
                items.Dequeue();

            double rowThickness = side <= 0 ? 0 : rowSum / side;
            bool rowAlongWidth = w <= h; // lay the row across the shorter dimension

            if (rowAlongWidth)
            {
                double cx = x;
                foreach (var (key, area) in row)
                {
                    double itemWidth = rowThickness <= 0 ? 0 : area / rowThickness;
                    result.Add((key, new TreemapRect(cx, y, itemWidth, rowThickness)));
                    cx += itemWidth;
                }
                y += rowThickness;
                h -= rowThickness;
            }
            else
            {
                double cy = y;
                foreach (var (key, area) in row)
                {
                    double itemHeight = rowThickness <= 0 ? 0 : area / rowThickness;
                    result.Add((key, new TreemapRect(x, cy, rowThickness, itemHeight)));
                    cy += itemHeight;
                }
                x += rowThickness;
                w -= rowThickness;
            }
        }
    }

    private static double WorstAspect(List<(string Key, double Area)> row, double rowSum, double side)
    {
        if (rowSum <= 0 || side <= 0)
            return double.MaxValue;
        double rowThickness = rowSum / side;
        double worst = 0;
        foreach (var (_, area) in row)
        {
            double itemLength = rowThickness <= 0 ? 0 : area / rowThickness;
            if (itemLength <= 0)
                return double.MaxValue;
            double ratio = Math.Max(rowThickness / itemLength, itemLength / rowThickness);
            if (double.IsNaN(ratio) || double.IsInfinity(ratio))
                return double.MaxValue;
            worst = Math.Max(worst, ratio);
        }
        return worst;
    }
}
