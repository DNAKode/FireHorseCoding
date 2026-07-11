using System.Text.Json;
using System.Text.RegularExpressions;
using KodePorter.Core.Atlas;
using KodePorter.Core.Domain;
using KodePorter.Core.Gneiss;
using KodePorter.Core.Model;
using KodePorter.Core.Store;
using KodePorter.Core.Tests.Support;

namespace KodePorter.Core.Tests;

/// <summary>CONTRACT-M15.md §6 (Atlas v2 — scale + imperfection-vocabulary): the synthetic-scale
/// fixture test, treemap-SVG determinism, and the hide-tests-default assertion required by §6.6.</summary>
public class AtlasV2Tests
{
    [Fact]
    public void ThreeThousandEntityWorkspaceStaysUnder15MbAndUsesLazyRenderingNotPerEntityPreRender()
    {
        using var scratch = new TempDirectory();
        string workspaceDir = BuildScaleWorkspace(scratch.Path, groupsPerSide: 30, itemsPerGroup: 50);

        using var store = new MapStore(Path.Combine(workspaceDir, "kpmap.db"));
        using var binding = GneissBinding.Initialize(workspaceDir);

        string html = AtlasGenerator.Generate(workspaceDir, store, binding, new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero));

        // Sanity: this really did exercise ~3k+ entities per side (30 groups * (50 items + 1
        // module) = 1530 per side, 3060 total across both sides) — big enough to be meaningful
        // for the scale budget without making the test itself slow.
        using (var doc = JsonDocument.Parse(ExtractDataIsland(html)))
        {
            var root = doc.RootElement;
            int sourceCount = root.GetProperty("sourceTree").GetArrayLength();
            int targetCount = root.GetProperty("targetTree").GetArrayLength();
            Assert.True(sourceCount + targetCount >= 3000, $"fixture only produced {sourceCount + targetCount} entities; scale test would pass vacuously");
        }

        long byteSize = System.Text.Encoding.UTF8.GetByteCount(html);
        Assert.True(byteSize < 15L * 1024 * 1024, $"Atlas HTML is {byteSize} bytes, over the 15MB budget (CONTRACT-M15.md §6.1)");

        // Lazy-render markers present (CONTRACT-M15.md §6.1: "the DOM renders lazily ... children
        // created on expand via vanilla JS; long sibling lists render in pages").
        Assert.Contains("data-lazy-tree=\"1\"", html, StringComparison.Ordinal);
        Assert.Contains("makeTreeController", html, StringComparison.Ordinal);
        Assert.Contains("renderPaged", html, StringComparison.Ordinal);
        Assert.Contains("PAGE_SIZE = 200", html, StringComparison.Ordinal);

        // No <details>-per-entity pre-rendering at scale: with ~3k entities per side, a
        // per-entity server-rendered marker would appear thousands of times; the lazy design
        // means a *literal, entity-id-bearing* `data-entity-id="<hex>"` attribute must never be
        // baked into the static document — only into the JSON data island (as `"id":"<hex>"`) and
        // the JS template string that builds rows at runtime (which contains the attribute name
        // but never a concrete id, e.g. `document.querySelector('[data-entity-id="' + id)`).
        Assert.Empty(Regex.Matches(html, "data-entity-id=\"[0-9a-f]{16,}\""));
        Assert.DoesNotContain("class=\"node leaf", html, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"node branch", html, StringComparison.Ordinal);
        int detailsCount = Regex.Matches(html, "<details", RegexOptions.None).Count;
        // The handful of legitimate <details> elements left (unit anchors, label popover, why-tree,
        // per-claim — all bounded by unit/claim count, not entity count) — nowhere near 3000.
        Assert.True(detailsCount < 50, $"expected only the small fixed set of non-entity <details> elements, found {detailsCount}");
    }

    [Fact]
    public void HideTestsDefaultsOnInTheGeneratedFilterBar()
    {
        using var scratch = new TempDirectory();
        string workspaceDir = BuildScaleWorkspace(scratch.Path, groupsPerSide: 2, itemsPerGroup: 5);

        using var store = new MapStore(Path.Combine(workspaceDir, "kpmap.db"));
        using var binding = GneissBinding.Initialize(workspaceDir);

        string html = AtlasGenerator.Generate(workspaceDir, store, binding, new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero));

        // CONTRACT-M15.md §6.4: "hide tests" default ON — both the static checkbox markup and the
        // JS filter state must agree, since either one alone (checkbox checked but JS state false,
        // or vice versa) would silently show tests by default despite looking otherwise.
        Assert.Contains("id=\"kp-hide-tests\" checked", html, StringComparison.Ordinal);
        Assert.Contains("hideTests: true", html, StringComparison.Ordinal);
    }

    [Fact]
    public void OverviewTreemapSvgIsByteIdenticalAcrossTwoGenerationsOverTheSameWorkspace()
    {
        using var scratch = new TempDirectory();
        string workspaceDir = BuildScaleWorkspace(scratch.Path, groupsPerSide: 6, itemsPerGroup: 12, withCorrespondences: true);

        using var store = new MapStore(Path.Combine(workspaceDir, "kpmap.db"));
        using var binding = GneissBinding.Initialize(workspaceDir);

        string html1 = AtlasGenerator.Generate(workspaceDir, store, binding, new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero));
        string html2 = AtlasGenerator.Generate(workspaceDir, store, binding, new DateTimeOffset(2026, 7, 10, 12, 5, 30, TimeSpan.Zero));

        var svgs1 = ExtractTreemapSvgs(html1);
        var svgs2 = ExtractTreemapSvgs(html2);

        Assert.Equal(2, svgs1.Count); // source + target
        Assert.Equal(svgs1.Count, svgs2.Count);
        for (int i = 0; i < svgs1.Count; i++)
            Assert.Equal(svgs1[i], svgs2[i]);

        // The layout must actually have produced real rectangles (not a vacuous empty-groups pass).
        Assert.Contains("treemap-rect", svgs1[0]);
        Assert.Contains("treemap-rect", svgs1[1]);

        // Coverage classes present given the fixture mixes asserted/candidate correspondences.
        Assert.Contains("cov-corresponded", string.Join("", svgs1));
        Assert.Contains("cov-candidate-only", string.Join("", svgs1));
        Assert.Contains("cov-uncovered", string.Join("", svgs1));
    }

    private static List<string> ExtractTreemapSvgs(string html)
    {
        var result = new List<string>();
        foreach (Match m in Regex.Matches(html, "<svg class=\"treemap-svg\".*?</svg>", RegexOptions.Singleline))
            result.Add(m.Value);
        return result;
    }

    private static string ExtractDataIsland(string html)
    {
        const string marker = "<script type=\"application/json\" id=\"kp-atlas-data\">";
        int start = html.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0, "data island script tag not found");
        start += marker.Length;
        int end = html.IndexOf("</script>", start, StringComparison.Ordinal);
        Assert.True(end >= 0, "data island closing script tag not found");
        return html[start..end];
    }

    /// <summary>
    /// Builds a workspace with <paramref name="groupsPerSide"/> top-level crates/namespaces per
    /// side, each containing a module/namespace entity plus <paramref name="itemsPerGroup"/> child
    /// entities (mixed kinds, ~1/8 marked is_test to mirror the FrankenTui-probe ratio, a few
    /// marked degraded/gap) — entities are inserted directly into the map store (bypassing the
    /// provider pipeline) so the fixture builds fast even at thousands of entities. When
    /// <paramref name="withCorrespondences"/>, seeds a handful of asserted and candidate-provenance
    /// correspondences spread across groups so every Overview coverage class is exercised.
    /// </summary>
    private static string BuildScaleWorkspace(string scratchDir, int groupsPerSide, int itemsPerGroup, bool withCorrespondences = false)
    {
        string workspaceDir = Path.Combine(scratchDir, "workspace");
        Directory.CreateDirectory(workspaceDir);

        ProjectYaml.Write(workspaceDir, new ProjectYamlDoc("scale-port", "rust->csharp", "fixtures/scale/rust", "fixtures/scale/csharp", "kp-default@1"));

        using var store = new MapStore(Path.Combine(workspaceDir, "kpmap.db"));
        using var binding = GneissBinding.Initialize(workspaceDir);

        var created = new DateTimeOffset(2026, 7, 10, 0, 0, 0, TimeSpan.Zero);
        string sourceTreeHash = new string('a', 64);
        string targetTreeHash = new string('b', 64);
        var sourceBasis = new Basis(
            EntityIdCalculator.ComputeBasisId(BasisSide.Source, "d1", sourceTreeHash),
            BasisSide.Source, "d1", "fixtures/scale/rust", sourceTreeHash, Toolchain: null, Analyzer: "rust-syn", Created: created);
        var targetBasis = new Basis(
            EntityIdCalculator.ComputeBasisId(BasisSide.Target, "base", targetTreeHash),
            BasisSide.Target, "base", "fixtures/scale/csharp", targetTreeHash, Toolchain: null, Analyzer: "csharp-roslyn", Created: created);
        store.InsertBasis(sourceBasis);
        store.InsertBasis(targetBasis);

        var sourceEntities = new List<Entity>();
        var targetEntities = new List<Entity>();
        var sourceGroupFirstItem = new Dictionary<int, string>();
        var targetGroupFirstItem = new Dictionary<int, string>();

        for (int g = 0; g < groupsPerSide; g++)
        {
            string modName = $"mod{g}";
            string modSymbol = modName;
            string modId = EntityIdCalculator.ComputeEntityId(BasisSide.Source, "module", modSymbol);
            sourceEntities.Add(new Entity(modId, sourceBasis.Id, "module", modName, modSymbol, "src/lib.rs", 1, 1000, Hash(modSymbol), null));

            string nsName = $"Ns{g}";
            string nsSymbol = nsName;

            for (int i = 0; i < itemsPerGroup; i++)
            {
                bool isTest = i % 8 == 0; // ~1/8 test-ness, mirroring the FrankenTui probe's 87.6% test ratio at scale
                string resolution = i % 47 == 0 ? "degraded" : i % 53 == 0 ? "gap" : "clean";
                string kind = i % 2 == 0 ? "fn" : "struct";
                string name = $"item{i}";
                string symbol = $"{modSymbol}::{name}";
                string id = EntityIdCalculator.ComputeEntityId(BasisSide.Source, kind, symbol);
                sourceEntities.Add(new Entity(id, sourceBasis.Id, kind, name, symbol, "src/lib.rs", i * 3 + 2, i * 3 + 4, Hash(symbol), modId, resolution, isTest));
                if (i == 1)
                    sourceGroupFirstItem[g] = symbol;

                string tKind = i % 2 == 0 ? "method" : "class";
                string tName = $"Item{i}";
                string tSymbol = $"{nsSymbol}.{tName}";
                string tId = EntityIdCalculator.ComputeEntityId(BasisSide.Target, tKind, tSymbol);
                targetEntities.Add(new Entity(tId, targetBasis.Id, tKind, tName, tSymbol, "Item.cs", i * 3 + 2, i * 3 + 4, Hash(tSymbol), null, resolution, isTest));
                if (i == 1)
                    targetGroupFirstItem[g] = tSymbol;
            }
        }

        store.InsertEntities(sourceBasis.Id, sourceEntities);
        store.InsertEntities(targetBasis.Id, targetEntities);

        if (withCorrespondences)
        {
            var correspondences = new List<Correspondence>();
            int corrN = 0;
            for (int g = 0; g < groupsPerSide; g++)
            {
                // group 0: asserted correspondence -> "corresponded"; group 1: candidate-only
                // correspondence -> "candidate-only"; remaining groups: no correspondence -> "uncovered".
                if (g == 0 && sourceGroupFirstItem.TryGetValue(g, out var sp0) && targetGroupFirstItem.TryGetValue(g, out var tp0))
                {
                    correspondences.Add(new Correspondence(
                        $"corr-{corrN++}", "implements", null, "unit-x",
                        new AnchorRef(sp0, "d1", Hash(sp0)), new AnchorRef(tp0, "base", Hash(tp0)),
                        null, null, ClaimAid: null, Stale: false, Provenance: "asserted"));
                }
                else if (g == 1 && sourceGroupFirstItem.TryGetValue(g, out var sp1))
                {
                    correspondences.Add(new Correspondence(
                        $"corr-{corrN++}", "maps-to", null, "unit-y",
                        new AnchorRef(sp1, "d1", Hash(sp1)), null,
                        null, null, ClaimAid: null, Stale: false, Provenance: "candidate"));
                }
            }
            CorrespondencesYaml.Write(workspaceDir, correspondences);
        }

        return workspaceDir;
    }

    private static string Hash(string s) => KodePorter.Core.Hashing.Sha256Util.HexOfUtf8(s);
}
