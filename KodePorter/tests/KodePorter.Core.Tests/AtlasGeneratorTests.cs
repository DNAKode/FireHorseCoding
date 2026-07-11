using System.Text.Json;
using System.Text.RegularExpressions;
using KodePorter.Core.Atlas;
using KodePorter.Core.Domain;
using KodePorter.Core.Gneiss;
using KodePorter.Core.Model;
using KodePorter.Core.Providers;
using KodePorter.Core.Store;
using KodePorter.Core.Tests.Support;
using KodePorter.Core.Verify;

namespace KodePorter.Core.Tests;

/// <summary>CONTRACT.md §10 test 7: Atlas determinism + self-containment (+ hand-computed health numbers).</summary>
public class AtlasGeneratorTests
{
    [Fact]
    public void GeneratingTwiceProducesIdenticalHtmlModuloTheTimestamp()
    {
        using var scratch = new TempDirectory();
        string workspaceDir = BuildFixtureWorkspace(scratch.Path);

        using var store = new MapStore(Path.Combine(workspaceDir, "kpmap.db"));
        using var binding = GneissBinding.Initialize(workspaceDir);

        var t1 = new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero);
        var t2 = new DateTimeOffset(2026, 7, 10, 12, 5, 30, TimeSpan.Zero);

        string html1 = AtlasGenerator.Generate(workspaceDir, store, binding, t1);
        string html2 = AtlasGenerator.Generate(workspaceDir, store, binding, t2);

        string masked1 = html1.Replace("2026-07-10T12:00:00Z", "TIMESTAMP", StringComparison.Ordinal);
        string masked2 = html2.Replace("2026-07-10T12:05:30Z", "TIMESTAMP", StringComparison.Ordinal);

        // Proves the timestamp actually varied between the two renders, so the equality below is meaningful.
        Assert.NotEqual(html1, html2);
        Assert.Equal(masked1, masked2);

        AssertNoMalformedStatusClassAttributes(html1);
    }

    /// <summary>
    /// No `class="..."` value anywhere in the document contains anything beyond well-formed,
    /// individually-valid class tokens — in particular, no `status-` badge ever leaks a joined
    /// multi-word summary (e.g. the historical "badge status-2 accepted, 1 proposed" defect) into
    /// the class list. The fixture's unit-struct has exactly one (proposed) behavior claim, which
    /// exercises the single-status path; <see cref="AtlasHtmlRenderer"/>'s per-status badge
    /// rendering additionally guarantees a multi-status rollup never gets joined into one token.
    /// </summary>
    private static void AssertNoMalformedStatusClassAttributes(string html)
    {
        var classAttr = new Regex("class=\"([^\"]*)\"", RegexOptions.None);
        bool sawStatusToken = false;
        foreach (Match m in classAttr.Matches(html))
        {
            string value = m.Groups[1].Value;
            foreach (string token in value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                Assert.Matches("^[a-zA-Z][a-zA-Z0-9-]*$", token);
                if (token.StartsWith("status-", StringComparison.Ordinal))
                    sawStatusToken = true;
            }
        }
        Assert.True(sawStatusToken, "fixture did not exercise any status- badge; test would pass vacuously");
    }

    [Fact]
    public void AtlasHasNoExternalRequestSubstringsAndEmbedsParsableJson()
    {
        using var scratch = new TempDirectory();
        string workspaceDir = BuildFixtureWorkspace(scratch.Path);

        using var store = new MapStore(Path.Combine(workspaceDir, "kpmap.db"));
        using var binding = GneissBinding.Initialize(workspaceDir);

        string html = AtlasGenerator.Generate(workspaceDir, store, binding, new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero));

        Assert.DoesNotContain("http://", html, StringComparison.Ordinal);
        Assert.DoesNotContain("https://", html, StringComparison.Ordinal);

        string embeddedJson = ExtractDataIsland(html);
        using var doc = JsonDocument.Parse(embeddedJson); // throws if not valid JSON
        var root = doc.RootElement;

        Assert.Equal("headscan-port", root.GetProperty("header").GetProperty("projectName").GetString());
        Assert.True(root.GetProperty("correspondences").GetArrayLength() >= 2);
        Assert.True(root.GetProperty("units").GetArrayLength() >= 2);
        Assert.True(root.GetProperty("claims").GetArrayLength() >= 4);
        Assert.True(root.GetProperty("runs").GetArrayLength() >= 1);
    }

    [Fact]
    public void HealthNumbersMatchAHandComputedFixtureExpectation()
    {
        using var scratch = new TempDirectory();
        string workspaceDir = BuildFixtureWorkspace(scratch.Path);

        using var store = new MapStore(Path.Combine(workspaceDir, "kpmap.db"));
        using var binding = GneissBinding.Initialize(workspaceDir);

        // Hand-computed against the fixture built by BuildFixtureWorkspace, per CONTRACT-M15.md
        // §1.7 (Health v2):
        //   mapped        = 5 source entities (module/struct/fn/fn/enum) + 8 target entities (HeaderParserSource.V1) = 13
        //   corresponded  = 2 source entities (parse, HeaderParser) + 1 target entity (Parse(string)) = 3
        //                   (both corr-parse and corr-struct default to provenance "asserted")
        //   candidates    = 0 (no candidate-provenance correspondences in this fixture)
        //   implemented   = 1 unit with a targetAnchor (unit-parse; unit-struct has none)
        //   verified      = 1 unit with an accepted kp.verification pass (unit-parse / io-agreement-v1)
        //   stale         = 1 accepted kp.stale fact (seeded on corr-struct)
        //   absence.unknown = 2 eligible source entities (fn/method/struct/enum/class), not is_test,
        //                   uncovered by any unit anchor or correspondence, with no absences.yaml
        //                   override -> default "unknown" (headscan::unused_helper, headscan::ParseErrorCode)
        //   absence.notYetPorted / deliberatelyDropped = 0 (no overrides recorded)
        //   targetOnly.unexplained = 2 eligible target entities, not is_test, uncovered by any unit
        //                   targetAnchor or correspondence target -> default "unexplained"
        //                   (HeadScan.HeaderParser class, HeadScan.ParseErrorCode enum)
        //   targetOnly.intentional = 0 (no overrides recorded)
        var expected = new HealthReport(
            Mapped: 13, Corresponded: 3, Candidates: 0, Implemented: 1, Verified: 1, Stale: 1,
            Absence: new AbsenceBreakdown(Unknown: 2, NotYetPorted: 0, DeliberatelyDropped: 0),
            TargetOnly: new TargetOnlyBreakdown(Unexplained: 2, Intentional: 0));

        var actual = HealthCalculator.Compute(workspaceDir, store, binding);

        Assert.Equal(expected, actual);

        // The Atlas's embedded health block must report the exact same numbers (CONTRACT.md §9:
        // status numbers must match the Atlas).
        string html = AtlasGenerator.Generate(workspaceDir, store, binding, new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero));
        using var doc = JsonDocument.Parse(ExtractDataIsland(html));
        var h = doc.RootElement.GetProperty("health");
        Assert.Equal(expected.Mapped, h.GetProperty("mapped").GetInt32());
        Assert.Equal(expected.Corresponded, h.GetProperty("corresponded").GetInt32());
        Assert.Equal(expected.Candidates, h.GetProperty("candidates").GetInt32());
        Assert.Equal(expected.Implemented, h.GetProperty("implemented").GetInt32());
        Assert.Equal(expected.Verified, h.GetProperty("verified").GetInt32());
        Assert.Equal(expected.Stale, h.GetProperty("stale").GetInt32());
        Assert.Equal(expected.Absence.Unknown, h.GetProperty("absence").GetProperty("unknown").GetInt32());
        Assert.Equal(expected.Absence.NotYetPorted, h.GetProperty("absence").GetProperty("notYetPorted").GetInt32());
        Assert.Equal(expected.Absence.DeliberatelyDropped, h.GetProperty("absence").GetProperty("deliberatelyDropped").GetInt32());
        Assert.Equal(expected.TargetOnly.Unexplained, h.GetProperty("targetOnly").GetProperty("unexplained").GetInt32());
        Assert.Equal(expected.TargetOnly.Intentional, h.GetProperty("targetOnly").GetProperty("intentional").GetInt32());
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
    /// Builds a small but realistic workspace: a rust source basis (5 entities), a C# target
    /// basis (the shared HeaderParserSource.V1 fixture, 8 entities), two units, two
    /// correspondences (one stale), a decided behavior claim, a proposed behavior claim, and one
    /// verification run promoted to an accepted pass — enough to exercise every Atlas tab and
    /// every claim status (accepted / proposed / stale) without pulling in the full rust toolchain.
    /// </summary>
    private static string BuildFixtureWorkspace(string scratchDir)
    {
        string workspaceDir = Path.Combine(scratchDir, "workspace");
        Directory.CreateDirectory(workspaceDir);

        ProjectYaml.Write(workspaceDir, new ProjectYamlDoc("headscan-port", "rust->csharp", "fixtures/slice-zero/rust", "fixtures/slice-zero/csharp", "kp-default@1"));
        PolicyYaml.Write(workspaceDir, new PolicyDoc("kp-default", "1",
            new Dictionary<string, bool> { ["kpVerification"] = true, ["kpBehavior"] = false },
            new Dictionary<string, IReadOnlyList<string>> { ["kpVerification"] = ["verification-run"] }));

        using (var store = new MapStore(Path.Combine(workspaceDir, "kpmap.db")))
        using (var binding = GneissBinding.Initialize(workspaceDir))
        {
            var created = new DateTimeOffset(2026, 7, 10, 0, 0, 0, TimeSpan.Zero);

            // ---- source (rust) basis --------------------------------------------------------------
            var sourceBasis = BasisPinner.Pin(store, BasisSide.Source, "fixtures/slice-zero/rust", "d1", analyzer: "rust-syn", created: created);
            string dumpPath = Path.Combine(scratchDir, "d1.json");
            File.WriteAllText(dumpPath, JsonSerializer.Serialize(new ProviderDump(
                Provider: "rust-syn@0.1.0",
                Root: "fixtures/slice-zero/rust",
                Entities:
                [
                    new DumpEntity("module", "headscan", "headscan", "src/lib.rs", 1, 60, "h-module-0000000000000000000000000000000000000000000000000000", null),
                    new DumpEntity("struct", "HeaderParser", "headscan::HeaderParser", "src/lib.rs", 3, 6, "h-struct-0000000000000000000000000000000000000000000000000", "headscan"),
                    new DumpEntity("fn", "parse", "headscan::parse", "src/lib.rs", 8, 40, "h-parse-000000000000000000000000000000000000000000000000000", "headscan"),
                    new DumpEntity("fn", "unused_helper", "headscan::unused_helper", "src/lib.rs", 42, 44, "h-unused-0000000000000000000000000000000000000000000000000", "headscan"),
                    new DumpEntity("enum", "ParseErrorCode", "headscan::ParseErrorCode", "src/lib.rs", 46, 49, "h-enum-00000000000000000000000000000000000000000000000000", "headscan"),
                ])));
            new RustSynProvider().Import(store, sourceBasis, dumpPath);

            // ---- target (csharp) basis -------------------------------------------------------------
            string csharpRoot = Path.Combine(scratchDir, "csharp-root");
            CSharpFixture.WriteSource(csharpRoot, "HeaderParser.cs", HeaderParserSource.V1);
            var targetBasis = BasisPinner.Pin(store, BasisSide.Target, csharpRoot, "base", created: created);
            new CSharpRoslynProvider().Import(store, targetBasis);

            var sourceEntities = store.GetEntities(sourceBasis.Id).ToDictionary(e => e.SymbolPath, StringComparer.Ordinal);
            var targetEntities = store.GetEntities(targetBasis.Id).ToDictionary(e => e.SymbolPath, StringComparer.Ordinal);

            // ---- units ------------------------------------------------------------------------------
            UnitYaml.Write(workspaceDir, new UnitDoc("unit-parse", "Parse", "mapped",
                SourceAnchors: [new AnchorRef("headscan::parse", "d1", sourceEntities["headscan::parse"].ContentHash)],
                TargetAnchors: [new AnchorRef("HeadScan.HeaderParser.Parse(string)", "base", targetEntities["HeadScan.HeaderParser.Parse(string)"].ContentHash)],
                Claims: [], Stale: false,
                Purpose: "Parses a header line into a trimmed token.",
                Contract: "- Input: raw line\n- Output: trimmed string",
                Questions: "", Evidence: ""));

            UnitYaml.Write(workspaceDir, new UnitDoc("unit-struct", "HeaderParser", "mapped",
                SourceAnchors: [new AnchorRef("headscan::HeaderParser", "d1", sourceEntities["headscan::HeaderParser"].ContentHash)],
                TargetAnchors: [], Claims: [], Stale: false,
                Purpose: "The parser configuration struct.", Contract: "", Questions: "", Evidence: ""));

            // ---- correspondences (corr-struct seeded stale) ------------------------------------------
            var corrParse = new Correspondence("corr-parse", "implements", null, "unit-parse",
                new AnchorRef("headscan::parse", "d1", sourceEntities["headscan::parse"].ContentHash),
                new AnchorRef("HeadScan.HeaderParser.Parse(string)", "base", targetEntities["HeadScan.HeaderParser.Parse(string)"].ContentHash),
                "io-agreement-v1", "Direct port.", ClaimAid: null, Stale: false);
            var corrStruct = new Correspondence("corr-struct", "maps-to", null, "unit-struct",
                new AnchorRef("headscan::HeaderParser", "d1", sourceEntities["headscan::HeaderParser"].ContentHash),
                null, null, null, ClaimAid: null, Stale: true);
            CorrespondencesYaml.Write(workspaceDir, [corrParse, corrStruct]);

            string corrParseAid = binding.ProposeCorrespondenceClaim("corr-parse",
                new CorrespondenceClaimValue("implements",
                    new AnchorRefValue("headscan::parse", "d1", sourceEntities["headscan::parse"].ContentHash),
                    new AnchorRefValue("HeadScan.HeaderParser.Parse(string)", "base", targetEntities["HeadScan.HeaderParser.Parse(string)"].ContentHash),
                    "unit-parse", "io-agreement-v1"),
                evidenceAids: null, actor: "kodeporter", reason: "kp corr add");
            binding.PolicyAutoAccept(corrParseAid, "kp-default", "1", "fixture: correspondence accepted");

            binding.ProposeCorrespondenceClaim("corr-struct",
                new CorrespondenceClaimValue("maps-to",
                    new AnchorRefValue("headscan::HeaderParser", "d1", sourceEntities["headscan::HeaderParser"].ContentHash),
                    null, "unit-struct", null),
                evidenceAids: null, actor: "kodeporter", reason: "kp corr add");
            // left proposed (undecided) deliberately, to exercise the "proposed" claim status.

            binding.AssertStale(GneissBinding.CorrespondenceSubject("corr-struct"),
                new StaleValue("d1", "anchor-drift", ["headscan::HeaderParser"]), "kodeporter", "seeded stale for the fixture");

            // ---- behavior claims (one decided, one left proposed) -------------------------------------
            string behaviorParseAid = binding.ProposeBehaviorClaim("unit-parse", "B1", "Parses a header line into a trimmed token.", [], "kodeporter", "generated");
            binding.HumanDecide(behaviorParseAid, KpVerdict.Accept, "matches the source contract", "govert");

            // unit-struct gets two behavior claims with two DIFFERENT statuses (one accepted, one
            // left proposed) so its Units-tab rollup exercises the multi-status badge path
            // (CONTRACT.md §8 / the "2 accepted, 1 proposed" defect): each status must render as
            // its own single-token badge, never joined into one multi-word class.
            string behaviorStructAid1 = binding.ProposeBehaviorClaim("unit-struct", "B1", "Represents the parser configuration.", [], "kodeporter", "generated");
            binding.HumanDecide(behaviorStructAid1, KpVerdict.Accept, "matches the source contract", "govert");
            binding.ProposeBehaviorClaim("unit-struct", "B2", "Owns the trim-window bounds.", [], "kodeporter", "generated");

            // ---- verification run (pass, auto-accepted) ------------------------------------------------
            string casesPath = Path.Combine(scratchDir, "cases.jsonl");
            File.WriteAllText(casesPath, "{\"name\":\"case1\"}\n");
            string sourceScript = WriteStubScript(scratchDir, "source.cmd", """{"name":"case1","result":{"value":1}}""");
            string targetScript = WriteStubScript(scratchDir, "target.cmd", """{"name":"case1","result":{"value":1}}""");

            var run = VerificationHarness.Run(workspaceDir, "unit-parse", "io-agreement-v1", casesPath,
                $"\"{sourceScript}\"", $"\"{targetScript}\"", "d1", "base",
                new DateTimeOffset(2026, 7, 10, 1, 0, 0, TimeSpan.Zero));

            var policy = PolicyYaml.Read(workspaceDir);
            VerificationHarness.PromoteResult(binding, policy, run, evidenceAids: null, actor: "kodeporter", reason: "kp verify run");
        }

        return workspaceDir;
    }

    private static string WriteStubScript(string dir, string fileName, string jsonLine)
    {
        string path = Path.Combine(dir, fileName);
        File.WriteAllText(path, "@echo off\r\necho " + jsonLine + "\r\n");
        return path;
    }
}
