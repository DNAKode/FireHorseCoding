using KodePorter.Core.Model;
using KodePorter.Core.Providers;
using KodePorter.Core.Store;
using KodePorter.Core.Tests.Support;
using static KodePorter.Core.Tests.Support.EntityAssertions;

namespace KodePorter.Core.Tests;

/// <summary>CONTRACT.md §10 test 3: a committed sample dump JSON -> expected entities; provider field validated.</summary>
public class RustSynProviderTests
{
    private static string SampleDumpPath => Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample-rust-dump.json");

    [Fact]
    public void ImportsExpectedEntitiesFromTheCommittedSampleDump()
    {
        Assert.True(File.Exists(SampleDumpPath), $"Expected fixture at '{SampleDumpPath}' (check Fixtures\\*.json is copied to output).");

        using var dbDir = new TempDirectory();
        using var store = new MapStore(Path.Combine(dbDir.Path, "kpmap.db"));
        var basis = MakeBasis(BasisSide.Source, "base");
        store.InsertBasis(basis);

        var result = new RustSynProvider().Import(store, basis, SampleDumpPath);

        Assert.Equal(0, result.ErrorDiagnosticCount);
        Assert.Equal(5, result.EntityCount);

        var entities = store.GetEntities(basis.Id).ToDictionary(e => e.SymbolPath);
        Assert.Equal(5, entities.Count);

        var module = AssertEntity(entities, "headscan", "module", "headscan", 1, 60, parent: null);
        AssertEntity(entities, "headscan::HeaderParser", "struct", "HeaderParser", 3, 6, parent: module);
        var fn = AssertEntity(entities, "headscan::parse", "fn", "parse", 8, 40, parent: module);
        var enumEntity = AssertEntity(entities, "headscan::ParseErrorCode", "enum", "ParseErrorCode", 42, 49, parent: module);
        AssertEntity(entities, "headscan::ParseErrorCode::MissingColon", "variant", "MissingColon", 43, 43, parent: enumEntity);

        Assert.Equal(EntityIdCalculator.ComputeEntityId(BasisSide.Source, "fn", "headscan::parse"), fn.Id);
        Assert.Equal("19e77cb3c339a857e354630531d3314d7807a61edb0ff55fd9f2071247dee8d3", fn.ContentHash);
        Assert.Matches("^[0-9a-f]{64}$", fn.Id);
    }

    [Fact]
    public void RejectsADumpWhoseProviderFieldDoesNotStartWithRustSyn()
    {
        using var tempDir = new TempDirectory();
        string badDumpPath = Path.Combine(tempDir.Path, "bad-dump.json");
        File.WriteAllText(badDumpPath, """{"provider":"scip-rust@1.0.0","root":".","entities":[]}""");

        using var dbDir = new TempDirectory();
        using var store = new MapStore(Path.Combine(dbDir.Path, "kpmap.db"));
        var basis = MakeBasis(BasisSide.Source, "bad");
        store.InsertBasis(basis);

        var ex = Assert.Throws<InvalidDataException>(() => new RustSynProvider().Import(store, basis, badDumpPath));
        Assert.Contains("rust-syn@", ex.Message);

        Assert.Empty(store.GetEntities(basis.Id));
    }

    private static Basis MakeBasis(BasisSide side, string label)
    {
        const string fakeTreeHash = "deadbeefdeadbeefdeadbeefdeadbeefdeadbeefdeadbeefdeadbeefdeadbeef";
        return new Basis(
            Id: EntityIdCalculator.ComputeBasisId(side, label, fakeTreeHash),
            Side: side,
            Label: label,
            Root: "fixtures/slice-zero/rust",
            TreeHash: fakeTreeHash,
            Toolchain: null,
            Analyzer: "rust-syn",
            Created: DateTimeOffset.UtcNow);
    }
}
