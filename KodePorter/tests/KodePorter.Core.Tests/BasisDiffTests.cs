using System.Text.Json;
using KodePorter.Core.Diff;
using KodePorter.Core.Model;
using KodePorter.Core.Providers;
using KodePorter.Core.Store;
using KodePorter.Core.Tests.Support;

namespace KodePorter.Core.Tests;

/// <summary>
/// CONTRACT.md §10 test 4, scoped to what this increment builds (per BUILD step 6): the basis
/// diff service only — staleness propagation into correspondences/units/claims needs the
/// Gneiss binding and comes later. This proves the diff itself does not cry wolf: two roots
/// differing in exactly one function body produce exactly one changed entity, with everything
/// else (including an unrelated function) reported as neither added, removed, nor changed.
/// </summary>
public class BasisDiffTests
{
    private const string ChangedFnContentHash = "4f6c8a09d34e78387f2a037f1871545032ede00be0d27d7ab9f205db9f309d12";

    [Fact]
    public void TwoRootsDifferingInOneFunctionBodyProduceExactlyOneChangedEntityAndNoOthers()
    {
        using var dbDir = new TempDirectory();
        using var store = new MapStore(Path.Combine(dbDir.Path, "kpmap.db"));

        var d0 = MakeBasis("d0", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
        var d1 = MakeBasis("d1", "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
        store.InsertBasis(d0);
        store.InsertBasis(d1);

        using var tempDir = new TempDirectory();
        string d0DumpPath = WriteDump(tempDir.Path, "d0.json", parseContentHash: "19e77cb3c339a857e354630531d3314d7807a61edb0ff55fd9f2071247dee8d3");
        string d1DumpPath = WriteDump(tempDir.Path, "d1.json", parseContentHash: ChangedFnContentHash);

        new RustSynProvider().Import(store, d0, d0DumpPath);
        new RustSynProvider().Import(store, d1, d1DumpPath);

        var diff = BasisDiffService.Diff(store, d0.Id, d1.Id);

        Assert.Empty(diff.Added);
        Assert.Empty(diff.Removed);

        var changedEntity = Assert.Single(diff.Changed);
        Assert.Equal("headscan::parse", changedEntity.SymbolPath);
        Assert.Equal("fn", changedEntity.Kind);
        Assert.Equal(EntityIdCalculator.ComputeEntityId(BasisSide.Source, "fn", "headscan::parse"), changedEntity.EntityId);

        // Does not cry wolf: the module, struct, enum, and variant — untouched by the
        // change — are not reported as changed.
        var changedIds = diff.Changed.Select(c => c.EntityId).ToHashSet();
        Assert.DoesNotContain(EntityIdCalculator.ComputeEntityId(BasisSide.Source, "module", "headscan"), changedIds);
        Assert.DoesNotContain(EntityIdCalculator.ComputeEntityId(BasisSide.Source, "struct", "headscan::HeaderParser"), changedIds);
        Assert.DoesNotContain(EntityIdCalculator.ComputeEntityId(BasisSide.Source, "enum", "headscan::ParseErrorCode"), changedIds);
        Assert.DoesNotContain(EntityIdCalculator.ComputeEntityId(BasisSide.Source, "variant", "headscan::ParseErrorCode::MissingColon"), changedIds);
    }

    private static Basis MakeBasis(string label, string treeHash) => new(
        Id: EntityIdCalculator.ComputeBasisId(BasisSide.Source, label, treeHash),
        Side: BasisSide.Source,
        Label: label,
        Root: "fixtures/slice-zero/rust",
        TreeHash: treeHash,
        Toolchain: null,
        Analyzer: "rust-syn",
        Created: DateTimeOffset.UtcNow);

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
                new DumpEntity("variant", "MissingColon", "headscan::ParseErrorCode::MissingColon", "src/lib.rs", 43, 43,
                    "10d57317a049525312c2a5711fc164c6c12e9954c3328d2f7ffbbeb02a83165e", "headscan::ParseErrorCode"),
            ]);

        string path = Path.Combine(dir, fileName);
        File.WriteAllText(path, JsonSerializer.Serialize(dump));
        return path;
    }
}
