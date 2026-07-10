using System.Text.Json;
using KodePorter.Core.Advance;
using KodePorter.Core.Domain;
using KodePorter.Core.Gneiss;
using KodePorter.Core.Model;
using KodePorter.Core.Providers;
using KodePorter.Core.Store;
using KodePorter.Core.Tests.Support;

namespace KodePorter.Core.Tests;

/// <summary>
/// CONTRACT.md §10 test 4 (staleness half) + BUILD step 6's correspondence-stale-marking test:
/// a changed anchor marks the correspondence stale, an anchor on an untouched symbol does not
/// (d1 does not cry wolf) — both in the yaml and as a kp.stale fact in Gneiss.
/// </summary>
public class AdvanceServiceTests
{
    private const string ChangedFnContentHash = "4f6c8a09d34e78387f2a037f1871545032ede00be0d27d7ab9f205db9f309d12";

    [Fact]
    public void ChangedAnchorGoesStaleAndUnrelatedAnchorDoesNot()
    {
        using var scratch = new TempDirectory();
        string workspaceDir = Path.Combine(scratch.Path, "workspace");
        Directory.CreateDirectory(workspaceDir);

        using var store = new MapStore(Path.Combine(workspaceDir, "kpmap.db"));
        using var binding = GneissBinding.Initialize(workspaceDir);

        // Seed basis d0 directly (mirrors BasisDiffTests): a struct (unrelated) and a function
        // (about to change).
        var d0 = new Basis(
            Id: EntityIdCalculator.ComputeBasisId(BasisSide.Source, "d0", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
            Side: BasisSide.Source, Label: "d0", Root: "fixtures/slice-zero/rust",
            TreeHash: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            Toolchain: null, Analyzer: "rust-syn", Created: DateTimeOffset.UtcNow);
        store.InsertBasis(d0);

        string d0DumpPath = WriteDump(scratch.Path, "d0.json", parseContentHash: "19e77cb3c339a857e354630531d3314d7807a61edb0ff55fd9f2071247dee8d3");
        new RustSynProvider().Import(store, d0, d0DumpPath);

        var correspondences = new List<Correspondence>
        {
            new("corr-parse", "implements", null, "unit-parse",
                new AnchorRef("headscan::parse", "d0", "19e77cb3c339a857e354630531d3314d7807a61edb0ff55fd9f2071247dee8d3"),
                new AnchorRef("HeadScan.HeaderParser.Parse(string)", "base", "target-hash"),
                "io-agreement-v1", null, null, Stale: false),
            new("corr-struct", "maps-to", null, "unit-struct",
                new AnchorRef("headscan::HeaderParser", "d0", "7c7199bcb42f0682acf5490fa493acf8edcd148740d0796a9d09cc2d3dd8258e"),
                new AnchorRef("HeadScan.HeaderParser", "base", "target-hash-2"),
                null, null, null, Stale: false),
        };
        CorrespondencesYaml.Write(workspaceDir, correspondences);

        // Also propose kp.correspondence claims so we can check the kp.stale fact is a distinct,
        // separately-visible assertion (not conflated with the correspondence claim itself).
        binding.ProposeCorrespondenceClaim("corr-parse",
            new CorrespondenceClaimValue("implements", null, null, "unit-parse", "io-agreement-v1"),
            evidenceAids: null, actor: "kodeporter", reason: "generated");

        // Advance source to d1: parse's content hash changes; the struct is untouched.
        string d1DumpPath = WriteDump(scratch.Path, "d1.json", parseContentHash: ChangedFnContentHash);
        string d1Root = Path.Combine(scratch.Path, "d1-root");
        Directory.CreateDirectory(d1Root);

        var report = AdvanceService.Advance(
            workspaceDir, store, binding, BasisSide.Source, d1Root, "d1", d1DumpPath, analyzer: "rust-syn",
            timestamp: DateTimeOffset.UtcNow, actor: "kodeporter", reason: "advance to d1");

        // Entity diff: exactly the function changed.
        var changedEntity = Assert.Single(report.Diff.Changed);
        Assert.Equal("headscan::parse", changedEntity.SymbolPath);
        Assert.Empty(report.Diff.Added);
        Assert.Empty(report.Diff.Removed);

        // Correspondence staleness: corr-parse yes, corr-struct no.
        Assert.Equal(["corr-parse"], report.StaleCorrespondenceIds);

        var reReadCorrespondences = CorrespondencesYaml.Read(workspaceDir);
        var reReadParse = reReadCorrespondences.Single(c => c.Id == "corr-parse");
        var reReadStruct = reReadCorrespondences.Single(c => c.Id == "corr-struct");
        Assert.True(reReadParse.Stale);
        Assert.False(reReadStruct.Stale);

        // Gneiss kp.stale fact: present for corr:corr-parse, absent for corr:corr-struct.
        var parseStaleView = binding.AskClaim(GneissBinding.CorrespondenceSubject("corr-parse"));
        Assert.Single(parseStaleView.Accepted, e => e.Predicate == GneissBinding.PredStale);

        var structStaleView = binding.AskClaim(GneissBinding.CorrespondenceSubject("corr-struct"));
        Assert.DoesNotContain(structStaleView.Accepted, e => e.Predicate == GneissBinding.PredStale);

        // The delta report exists and mentions the changed symbol and the stale correspondence.
        Assert.True(File.Exists(report.ReportPath));
        string reportText = File.ReadAllText(report.ReportPath);
        Assert.Contains("headscan::parse", reportText);
        Assert.Contains("corr-parse", reportText);
        Assert.DoesNotContain("corr-struct", reportText); // not stale, so never listed
    }

    [Fact]
    public void UnitAnchorDriftMarksTheUnitAndItsBehaviorClaimSubjectStale()
    {
        using var scratch = new TempDirectory();
        string workspaceDir = Path.Combine(scratch.Path, "workspace");
        Directory.CreateDirectory(workspaceDir);

        using var store = new MapStore(Path.Combine(workspaceDir, "kpmap.db"));
        using var binding = GneissBinding.Initialize(workspaceDir);

        var d0 = new Basis(
            Id: EntityIdCalculator.ComputeBasisId(BasisSide.Source, "d0", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
            Side: BasisSide.Source, Label: "d0", Root: "fixtures/slice-zero/rust",
            TreeHash: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            Toolchain: null, Analyzer: "rust-syn", Created: DateTimeOffset.UtcNow);
        store.InsertBasis(d0);
        string d0DumpPath = WriteDump(scratch.Path, "d0.json", parseContentHash: "19e77cb3c339a857e354630531d3314d7807a61edb0ff55fd9f2071247dee8d3");
        new RustSynProvider().Import(store, d0, d0DumpPath);

        var unit = new UnitDoc("unit-parse", "Parse", "mapped",
            SourceAnchors: [new AnchorRef("headscan::parse", "d0", "19e77cb3c339a857e354630531d3314d7807a61edb0ff55fd9f2071247dee8d3")],
            TargetAnchors: [], Claims: [], Stale: false, Purpose: "", Contract: "", Questions: "", Evidence: "");
        UnitYaml.Write(workspaceDir, unit);
        CorrespondencesYaml.Write(workspaceDir, []);

        string d1DumpPath = WriteDump(scratch.Path, "d1.json", parseContentHash: ChangedFnContentHash);
        string d1Root = Path.Combine(scratch.Path, "d1-root");
        Directory.CreateDirectory(d1Root);

        var report = AdvanceService.Advance(
            workspaceDir, store, binding, BasisSide.Source, d1Root, "d1", d1DumpPath, analyzer: "rust-syn",
            timestamp: DateTimeOffset.UtcNow, actor: "kodeporter", reason: "advance to d1");

        Assert.Equal(["unit-parse"], report.StaleUnitIds);
        Assert.Contains(GneissBinding.UnitSubject("unit-parse"), report.StaleClaimSubjects);

        var reReadUnit = UnitYaml.Read(workspaceDir, "unit-parse");
        Assert.True(reReadUnit.Stale);

        var view = binding.AskClaim(GneissBinding.UnitSubject("unit-parse"));
        Assert.Single(view.Accepted, e => e.Predicate == GneissBinding.PredStale);
    }

    private static string WriteDump(string dir, string fileName, string parseContentHash)
    {
        var dump = new ProviderDump(
            Provider: "rust-syn@0.1.0",
            Root: "fixtures/slice-zero/rust",
            Entities:
            [
                new DumpEntity("module", "headscan", "headscan", "src/lib.rs", 1, 60,
                    "8253675aa9e3a708c5ee490141ae3defb76d7a159a7b043b968ecd2541a85255", null),
                new DumpEntity("struct", "HeaderParser", "headscan::HeaderParser", "src/lib.rs", 3, 6,
                    "7c7199bcb42f0682acf5490fa493acf8edcd148740d0796a9d09cc2d3dd8258e", "headscan"),
                new DumpEntity("fn", "parse", "headscan::parse", "src/lib.rs", 8, 40,
                    parseContentHash, "headscan"),
                new DumpEntity("enum", "ParseErrorCode", "headscan::ParseErrorCode", "src/lib.rs", 42, 49,
                    "ab828cd28e4c13f07c990c9507a3db288055bfb429e1f9b5fc5804d4d02159b0", "headscan"),
            ]);

        string path = Path.Combine(dir, fileName);
        File.WriteAllText(path, JsonSerializer.Serialize(dump));
        return path;
    }
}
