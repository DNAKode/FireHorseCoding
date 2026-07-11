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

    /// <summary>
    /// FrankenTui-probe visual-review defect (PROBE-REPORT.md §7 finding 1 / CONTRACT-M15.md §6.2):
    /// grouping purely by the first `::`/`.`-segment can leave one group holding almost the whole
    /// side (e.g. every C# namespace nested under one product prefix), rendering as a single giant,
    /// uninformative rectangle. The fixture's "FrankenTui" first-segment group holds 5 of the
    /// target side's 7 non-test entities (~71%, over the 50% threshold) and has real substructure
    /// to split on — it must be re-keyed by its first TWO segments into "FrankenTui.Runtime" (3:
    /// Alpha, Beta, and the 3-segment Deep.Nested, which must collapse into the two-segment key,
    /// not get its own third-level rectangle) and "FrankenTui.Widgets" (2: Gamma, Delta). "Other"
    /// stays well under 50% and single-segment, untouched by the rule.
    /// </summary>
    [Fact]
    public void DominantFirstSegmentGroupSplitsIntoTwoSegmentGroupsWhileOthersStaySingleSegment()
    {
        using var scratch = new TempDirectory();
        string workspaceDir = BuildOverviewWorkspace(scratch.Path, TrivialSourceEntities, DominantSplitTargetEntities);

        using var store = new MapStore(Path.Combine(workspaceDir, "kpmap.db"));
        using var binding = GneissBinding.Initialize(workspaceDir);

        string html = AtlasGenerator.Generate(workspaceDir, store, binding, new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero));
        var svgs = ExtractTreemapSvgs(html);
        Assert.Equal(2, svgs.Count);
        string targetSvg = svgs[1];

        Assert.Contains("data-key=\"FrankenTui.Runtime\" data-nontest=\"3\" data-test=\"0\"", targetSvg, StringComparison.Ordinal);
        Assert.Contains("data-key=\"FrankenTui.Widgets\" data-nontest=\"2\" data-test=\"0\"", targetSvg, StringComparison.Ordinal);
        Assert.Contains("data-key=\"Other\" data-nontest=\"2\" data-test=\"0\"", targetSvg, StringComparison.Ordinal);

        // The pre-split "FrankenTui" umbrella rectangle must be gone (proves the split actually
        // happened, not a vacuous pass)...
        Assert.DoesNotContain("data-key=\"FrankenTui\"", targetSvg, StringComparison.Ordinal);
        // ...and the split must not go a third segment deep (two-segment max depth: recursion
        // applies at most once).
        Assert.DoesNotContain("data-key=\"FrankenTui.Runtime.Deep\"", targetSvg, StringComparison.Ordinal);
        // "Other" must stay single-segment (the rule only ever touches the ONE dominant group).
        Assert.DoesNotContain("data-key=\"Other.Epsilon\"", targetSvg, StringComparison.Ordinal);

        // The split prefix is threaded into the data island for the client-side tree/drill-down JS
        // to key entities identically to the server-rendered rectangles.
        using var doc = JsonDocument.Parse(ExtractDataIsland(html));
        Assert.Equal("FrankenTui", doc.RootElement.GetProperty("targetTreemapSplitPrefix").GetString());
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("sourceTreemapSplitPrefix").ValueKind);
    }

    /// <summary>
    /// FrankenTui-probe visual-review defect (PROBE-REPORT.md §2/§7 finding 1): per-file Rust
    /// integration-test crates group cleanly by <c>TopLevelKey</c> but contain zero non-test
    /// entities, so they must render no rectangle (area = non-test entity count) AND must not
    /// inflate the primary "N groups" figure — the caption states both the excluded-group count and
    /// the hidden-test-entity count honestly instead of silently dropping them.
    /// </summary>
    [Fact]
    public void TestOnlyGroupsAreExcludedFromLayoutAndPrimaryCountButCaptionReportsBothNumbers()
    {
        using var scratch = new TempDirectory();
        string workspaceDir = BuildOverviewWorkspace(scratch.Path, TestOnlyGroupsSourceEntities, TrivialTargetEntities);

        using var store = new MapStore(Path.Combine(workspaceDir, "kpmap.db"));
        using var binding = GneissBinding.Initialize(workspaceDir);

        string html = AtlasGenerator.Generate(workspaceDir, store, binding, new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero));
        var svgs = ExtractTreemapSvgs(html);
        Assert.Equal(2, svgs.Count);
        string sourceSvg = svgs[0];

        // Only the 2 real (non-test) groups render rectangles; the 3 test-only per-file-crate-style
        // groups render nothing and must not appear at all.
        Assert.Equal(2, Regex.Matches(sourceSvg, "<rect class=\"treemap-rect").Count);
        Assert.Contains("data-key=\"pkg_a\"", sourceSvg, StringComparison.Ordinal);
        Assert.Contains("data-key=\"pkg_b\"", sourceSvg, StringComparison.Ordinal);
        Assert.DoesNotContain("data-key=\"baseline_capture\"", sourceSvg, StringComparison.Ordinal);
        Assert.DoesNotContain("data-key=\"shadow_run_comparator\"", sourceSvg, StringComparison.Ordinal);
        Assert.DoesNotContain("data-key=\"mpc_vs_pi_evaluation\"", sourceSvg, StringComparison.Ordinal);

        // Hand-computed against the fixture: 5 first-segment groups total (pkg_a, pkg_b,
        // baseline_capture, shadow_run_comparator, mpc_vs_pi_evaluation) — 3 of them test-only (0
        // non-test entities each, 4 test entities total) — so the primary count must read 2 groups
        // / 6 non-test entities, with both excluded numbers stated in the parenthetical.
        Assert.Contains(
            "<p class=\"overview-counts\">2 groups · 6 non-test entities (3 test-only groups not shown · 4 test entities hidden by default)</p>",
            html, StringComparison.Ordinal);
    }

    /// <summary>TESTS (c): both defect fixes above stay deterministic together — two generations
    /// over the same workspace produce byte-identical treemap SVGs.</summary>
    [Fact]
    public void AdaptiveSplitAndTestOnlyGroupExclusionStayDeterministicAcrossTwoBuilds()
    {
        using var scratch = new TempDirectory();
        string workspaceDir = BuildOverviewWorkspace(scratch.Path, TestOnlyGroupsSourceEntities, DominantSplitTargetEntities);

        using var store = new MapStore(Path.Combine(workspaceDir, "kpmap.db"));
        using var binding = GneissBinding.Initialize(workspaceDir);

        string html1 = AtlasGenerator.Generate(workspaceDir, store, binding, new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero));
        string html2 = AtlasGenerator.Generate(workspaceDir, store, binding, new DateTimeOffset(2026, 7, 10, 12, 5, 30, TimeSpan.Zero));

        var svgs1 = ExtractTreemapSvgs(html1);
        var svgs2 = ExtractTreemapSvgs(html2);
        Assert.Equal(2, svgs1.Count);
        Assert.Equal(svgs1.Count, svgs2.Count);
        for (int i = 0; i < svgs1.Count; i++)
            Assert.Equal(svgs1[i], svgs2[i]);

        // Sanity: this really exercised both defects together, not vacuously.
        Assert.Contains("data-key=\"FrankenTui.Runtime\"", svgs1[1], StringComparison.Ordinal);
        Assert.DoesNotContain("data-key=\"baseline_capture\"", svgs1[0], StringComparison.Ordinal);
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

    // ---- Adaptive-split / test-only-group fixtures (defect-fix tests above) ---------------------

    /// <summary>Builds a workspace from directly-supplied source/target entity lists (bypassing the
    /// provider pipeline, same discipline as <see cref="BuildScaleWorkspace"/>) — used by the small,
    /// hand-computable fixtures for the two Overview-treemap defect fixes.</summary>
    private static string BuildOverviewWorkspace(
        string scratchDir, Func<string, IEnumerable<Entity>> buildSourceEntities, Func<string, IEnumerable<Entity>> buildTargetEntities)
    {
        string workspaceDir = Path.Combine(scratchDir, "workspace");
        Directory.CreateDirectory(workspaceDir);

        ProjectYaml.Write(workspaceDir, new ProjectYamlDoc("overview-fixture-port", "rust->csharp", "fixtures/overview/rust", "fixtures/overview/csharp", "kp-default@1"));

        using var store = new MapStore(Path.Combine(workspaceDir, "kpmap.db"));
        using var binding = GneissBinding.Initialize(workspaceDir);

        var created = new DateTimeOffset(2026, 7, 10, 0, 0, 0, TimeSpan.Zero);
        string sourceTreeHash = new string('a', 64);
        string targetTreeHash = new string('b', 64);
        var sourceBasis = new Basis(
            EntityIdCalculator.ComputeBasisId(BasisSide.Source, "d1", sourceTreeHash),
            BasisSide.Source, "d1", "fixtures/overview/rust", sourceTreeHash, Toolchain: null, Analyzer: "rust-syn", Created: created);
        var targetBasis = new Basis(
            EntityIdCalculator.ComputeBasisId(BasisSide.Target, "base", targetTreeHash),
            BasisSide.Target, "base", "fixtures/overview/csharp", targetTreeHash, Toolchain: null, Analyzer: "csharp-roslyn", Created: created);
        store.InsertBasis(sourceBasis);
        store.InsertBasis(targetBasis);

        store.InsertEntities(sourceBasis.Id, buildSourceEntities(sourceBasis.Id).ToList());
        store.InsertEntities(targetBasis.Id, buildTargetEntities(targetBasis.Id).ToList());

        return workspaceDir;
    }

    private static Entity MakeEntity(BasisSide side, string basisId, string kind, string name, string symbolPath, bool isTest) =>
        new(EntityIdCalculator.ComputeEntityId(side, kind, symbolPath), basisId, kind, name, symbolPath,
            side == BasisSide.Source ? "src/lib.rs" : "File.cs", 1, 2, Hash(symbolPath), null, "clean", isTest);

    /// <summary>Two bare top-level modules (no "::" at all, so neither has a second segment to
    /// split on regardless) in separate ~50%-share groups — safely inert for whichever side of a
    /// test doesn't care about that side's split/test-only-group behavior.</summary>
    private static IEnumerable<Entity> TrivialSourceEntities(string basisId)
    {
        yield return MakeEntity(BasisSide.Source, basisId, "module", "srcmod", "srcmod", isTest: false);
        yield return MakeEntity(BasisSide.Source, basisId, "module", "othermod", "othermod", isTest: false);
    }

    /// <summary>Mirrors <see cref="TrivialSourceEntities"/> on the C#-shaped target side.</summary>
    private static IEnumerable<Entity> TrivialTargetEntities(string basisId)
    {
        yield return MakeEntity(BasisSide.Target, basisId, "namespace", "Ns0", "Ns0", isTest: false);
        yield return MakeEntity(BasisSide.Target, basisId, "namespace", "Ns1", "Ns1", isTest: false);
    }

    /// <summary>"FrankenTui" holds 5 of 7 (~71%) non-test entities on this side — over the 50%
    /// threshold — split across two real subgroups ("Runtime", "Widgets") plus a 3-segment entity
    /// to prove the split caps at two segments. "Other" (2/7, ~29%) stays under the threshold and
    /// single-segment.</summary>
    private static IEnumerable<Entity> DominantSplitTargetEntities(string basisId)
    {
        yield return MakeEntity(BasisSide.Target, basisId, "class", "Alpha", "FrankenTui.Runtime.Alpha", isTest: false);
        yield return MakeEntity(BasisSide.Target, basisId, "class", "Beta", "FrankenTui.Runtime.Beta", isTest: false);
        yield return MakeEntity(BasisSide.Target, basisId, "class", "Nested", "FrankenTui.Runtime.Deep.Nested", isTest: false);
        yield return MakeEntity(BasisSide.Target, basisId, "class", "Gamma", "FrankenTui.Widgets.Gamma", isTest: false);
        yield return MakeEntity(BasisSide.Target, basisId, "class", "Delta", "FrankenTui.Widgets.Delta", isTest: false);
        yield return MakeEntity(BasisSide.Target, basisId, "class", "Epsilon", "Other.Epsilon", isTest: false);
        yield return MakeEntity(BasisSide.Target, basisId, "class", "Zeta", "Other.Zeta", isTest: false);
    }

    /// <summary>2 real crates ("pkg_a", "pkg_b", 3 non-test entities each — an exact 50/50 tie,
    /// deliberately at the "not strictly greater than half" boundary so neither triggers the
    /// adaptive split) plus 3 per-file-test-crate-style groups whose entities are ALL is_test — 0
    /// non-test entities each, mirroring the FrankenTui probe's per-file Rust integration-test
    /// crates (PROBE-REPORT.md §7 finding 1).</summary>
    private static IEnumerable<Entity> TestOnlyGroupsSourceEntities(string basisId)
    {
        yield return MakeEntity(BasisSide.Source, basisId, "fn", "A", "pkg_a::A", isTest: false);
        yield return MakeEntity(BasisSide.Source, basisId, "fn", "B", "pkg_a::B", isTest: false);
        yield return MakeEntity(BasisSide.Source, basisId, "fn", "C", "pkg_a::C", isTest: false);
        yield return MakeEntity(BasisSide.Source, basisId, "fn", "A", "pkg_b::A", isTest: false);
        yield return MakeEntity(BasisSide.Source, basisId, "fn", "B", "pkg_b::B", isTest: false);
        yield return MakeEntity(BasisSide.Source, basisId, "fn", "C", "pkg_b::C", isTest: false);
        yield return MakeEntity(BasisSide.Source, basisId, "fn", "case1", "baseline_capture::case1", isTest: true);
        yield return MakeEntity(BasisSide.Source, basisId, "fn", "case2", "baseline_capture::case2", isTest: true);
        yield return MakeEntity(BasisSide.Source, basisId, "fn", "case1", "shadow_run_comparator::case1", isTest: true);
        yield return MakeEntity(BasisSide.Source, basisId, "fn", "case1", "mpc_vs_pi_evaluation::case1", isTest: true);
    }
}
