using System.Text.Json;
using KodePorter.Core.Advance;
using KodePorter.Core.Gneiss;
using KodePorter.Core.Model;
using KodePorter.Core.Providers;
using KodePorter.Core.Store;
using KodePorter.Core.Tests.Support;

namespace KodePorter.Core.Tests;

/// <summary>
/// CONTRACT-M15.md §1.2: continuity_candidate is populated during Advance using the `name-kind`
/// heuristic ONLY — same kind, exact name match, within one (removed, added) pair on one side.
/// </summary>
public class ContinuityCandidateTests
{
    [Fact]
    public void AdvanceRecordsANameKindCandidateForARenamedSymbolAndNoneForKindOrNameMismatches()
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
        new RustSynProvider().Import(store, d0, WriteDump(scratch.Path, "d0.json", DumpV0()));

        string d1DumpPath = WriteDump(scratch.Path, "d1.json", DumpV1());
        string d1Root = Path.Combine(scratch.Path, "d1-root");
        Directory.CreateDirectory(d1Root);

        var report = AdvanceService.Advance(
            workspaceDir, store, binding, BasisSide.Source, d1Root, "d1", d1DumpPath, analyzer: "rust-syn",
            timestamp: DateTimeOffset.UtcNow, actor: "kodeporter", reason: "advance to d1");

        // Sanity: the diff actually produced the removed/added pairs this test exercises.
        Assert.Contains(report.Diff.Removed, e => e.SymbolPath == "headscan::parse");
        Assert.Contains(report.Diff.Removed, e => e.SymbolPath == "headscan::HeaderParser");
        Assert.Contains(report.Diff.Added, e => e.SymbolPath == "headscan::io::parse");
        Assert.Contains(report.Diff.Added, e => e.SymbolPath == "headscan::helper");

        // Exactly one candidate: fn "headscan::parse" -> fn "headscan::io::parse" (same kind,
        // same simple name "parse"). No candidate links the removed struct HeaderParser (kind
        // mismatch against any added fn) or the added fn "helper" (no removed entity named "helper").
        Assert.Equal(1, report.ContinuityCandidatesCreated);

        var newBasis = store.ListBases(BasisSide.Source)[^1];
        var candidates = store.GetContinuityCandidates(d0.Id, newBasis.Id);
        var candidate = Assert.Single(candidates);

        Assert.Equal(d0.Id, candidate.BasisFrom);
        Assert.Equal(newBasis.Id, candidate.BasisTo);
        Assert.Equal(EntityIdCalculator.ComputeEntityId(BasisSide.Source, "fn", "headscan::parse"), candidate.FromId);
        Assert.Equal(EntityIdCalculator.ComputeEntityId(BasisSide.Source, "fn", "headscan::io::parse"), candidate.ToId);
        Assert.Equal("name-kind", candidate.Heuristic);
        Assert.Equal("candidate", candidate.Status); // never auto-confirmed
    }

    [Fact]
    public void FirstEverAdvanceWithNoPreviousBasisRecordsNoCandidates()
    {
        using var scratch = new TempDirectory();
        string workspaceDir = Path.Combine(scratch.Path, "workspace");
        Directory.CreateDirectory(workspaceDir);

        using var store = new MapStore(Path.Combine(workspaceDir, "kpmap.db"));
        using var binding = GneissBinding.Initialize(workspaceDir);

        string dumpPath = WriteDump(scratch.Path, "d0.json", DumpV0());
        string root = Path.Combine(scratch.Path, "d0-root");
        Directory.CreateDirectory(root);

        var report = AdvanceService.Advance(
            workspaceDir, store, binding, BasisSide.Source, root, "d0", dumpPath, analyzer: "rust-syn",
            timestamp: DateTimeOffset.UtcNow, actor: "kodeporter", reason: "first advance");

        Assert.Equal(0, report.ContinuityCandidatesCreated);
        Assert.Empty(store.GetContinuityCandidates());
    }

    private static ProviderDump DumpV0() => new(
        Provider: "rust-syn@0.1.0",
        Root: "fixtures/slice-zero/rust",
        Entities:
        [
            new DumpEntity("module", "headscan", "headscan", "src/lib.rs", 1, 60,
                "8253675aa9e3a708c5ee490141ae3defb76d7a159a7b043b968ecd2541a85255", null),
            new DumpEntity("fn", "parse", "headscan::parse", "src/lib.rs", 8, 40,
                "19e77cb3c339a857e354630531d3314d7807a61edb0ff55fd9f2071247dee8d3", "headscan"),
            new DumpEntity("struct", "HeaderParser", "headscan::HeaderParser", "src/lib.rs", 3, 6,
                "7c7199bcb42f0682acf5490fa493acf8edcd148740d0796a9d09cc2d3dd8258e", "headscan"),
        ]);

    private static ProviderDump DumpV1() => new(
        Provider: "rust-syn@0.1.0",
        Root: "fixtures/slice-zero/rust",
        Entities:
        [
            // Unchanged (same symbolPath + contentHash) -> neither added nor removed.
            new DumpEntity("module", "headscan", "headscan", "src/lib.rs", 1, 60,
                "8253675aa9e3a708c5ee490141ae3defb76d7a159a7b043b968ecd2541a85255", null),
            // Renamed/moved: same kind+name, new symbolPath -> removed+added pair -> candidate.
            new DumpEntity("fn", "parse", "headscan::io::parse", "src/io.rs", 8, 40,
                "3f6c8a09d34e78387f2a037f1871545032ede00be0d27d7ab9f205db9f309d1", "headscan"),
            // HeaderParser struct dropped entirely (no replacement of matching kind+name) ->
            // removed, no candidate.
            // A brand-new, unrelated fn -> added, no removed entity shares its name -> no candidate.
            new DumpEntity("fn", "helper", "headscan::helper", "src/lib.rs", 42, 44,
                "5a6c8a09d34e78387f2a037f1871545032ede00be0d27d7ab9f205db9f309d1", "headscan"),
        ]);

    private static string WriteDump(string dir, string fileName, ProviderDump dump)
    {
        string path = Path.Combine(dir, fileName);
        File.WriteAllText(path, JsonSerializer.Serialize(dump));
        return path;
    }
}
