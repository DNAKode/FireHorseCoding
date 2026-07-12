using KodePorter.Core.Model;
using KodePorter.Core.Providers;
using KodePorter.Core.Store;
using KodePorter.Core.Tests.Support;

namespace KodePorter.Core.Tests;

/// <summary>
/// PROBE-REPORT.md §7 finding #2: (kind, symbolPath) deduplication silently dropped 69/9,725
/// entities where per-file Rust integration-test crates collided across packages. Dedup itself
/// stays (legitimate cases -- e.g. a partial type's second declaration -- still need first-wins),
/// but the drop count is no longer silent: it is surfaced via
/// <see cref="ImportResult.DroppedDuplicateCount"/>.
/// </summary>
public class DeduplicationTests
{
    // EntityResolution is internal (an implementation detail shared by both providers, not
    // itself part of KodePorter.Core's public surface), so these tests exercise the dedup +
    // drop-counting behavior only through RustSynProvider.Import — the same public entry point
    // real callers use.

    [Fact]
    public void RustSynProviderReportsDroppedDuplicateCountOnAnIdentityCollision()
    {
        using var tempDir = new TempDirectory();
        string dumpPath = Path.Combine(tempDir.Path, "dump.json");
        // Simulates the exact pre-fix shape from PROBE-REPORT.md §7 finding #2: two
        // independently-compiled crates' per-file test roots emit the identical (kind,
        // symbolPath) because neither is qualified by its owning package. Written directly at
        // the dump-JSON level so this test exercises the C#-side counting/surfacing mechanism
        // independent of tools/rust-map-dump, which no longer produces this shape itself (its
        // v1.2 fix qualifies test-crate roots by package, e.g. "alpha#tests/smoke").
        File.WriteAllText(dumpPath, """
            {
              "provider": "rust-map-dump@0.3.0",
              "root": ".",
              "entities": [
                {
                  "kind": "module", "name": "smoke", "symbolPath": "smoke",
                  "file": "alpha/tests/smoke.rs", "startLine": 1, "endLine": 1,
                  "contentHash": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                  "parentSymbolPath": null, "isTest": true
                },
                {
                  "kind": "module", "name": "smoke", "symbolPath": "smoke",
                  "file": "beta/tests/smoke.rs", "startLine": 1, "endLine": 1,
                  "contentHash": "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
                  "parentSymbolPath": null, "isTest": true
                }
              ]
            }
            """);

        using var dbDir = new TempDirectory();
        using var store = new MapStore(Path.Combine(dbDir.Path, "kpmap.db"));
        const string fakeTreeHash = "deadbeefdeadbeefdeadbeefdeadbeefdeadbeefdeadbeefdeadbeefdeadbeef";
        var basis = new Basis(
            Id: EntityIdCalculator.ComputeBasisId(BasisSide.Source, "base", fakeTreeHash),
            Side: BasisSide.Source, Label: "base", Root: ".", TreeHash: fakeTreeHash,
            Toolchain: null, Analyzer: "rust-syn", Created: DateTimeOffset.UtcNow);
        store.InsertBasis(basis);

        var result = new RustSynProvider().Import(store, basis, dumpPath);

        Assert.Equal(1, result.EntityCount);
        Assert.Equal(1, result.DroppedDuplicateCount);
        Assert.Single(store.GetEntities(basis.Id));
    }

    [Fact]
    public void RustSynProviderCountsOnlyTheColliderAmongMultipleEntitiesNotEveryEntity()
    {
        using var tempDir = new TempDirectory();
        string dumpPath = Path.Combine(tempDir.Path, "dump.json");
        // Three candidates, one (kind, symbolPath) collision between the first two; the third is
        // distinct. Exercises that the count is precisely "how many were dropped", not e.g. "how
        // many entities share a symbolPath with something" (which would over-count to 2).
        File.WriteAllText(dumpPath, """
            {
              "provider": "rust-map-dump@0.3.0",
              "root": ".",
              "entities": [
                {
                  "kind": "module", "name": "smoke", "symbolPath": "smoke",
                  "file": "alpha/tests/smoke.rs", "startLine": 1, "endLine": 1,
                  "contentHash": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                  "parentSymbolPath": null, "isTest": true
                },
                {
                  "kind": "module", "name": "smoke", "symbolPath": "smoke",
                  "file": "beta/tests/smoke.rs", "startLine": 1, "endLine": 1,
                  "contentHash": "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
                  "parentSymbolPath": null, "isTest": true
                },
                {
                  "kind": "fn", "name": "unrelated", "symbolPath": "gamma::unrelated",
                  "file": "gamma/src/lib.rs", "startLine": 1, "endLine": 1,
                  "contentHash": "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
                  "parentSymbolPath": null
                }
              ]
            }
            """);

        using var dbDir = new TempDirectory();
        using var store = new MapStore(Path.Combine(dbDir.Path, "kpmap.db"));
        const string fakeTreeHash = "deadbeefdeadbeefdeadbeefdeadbeefdeadbeefdeadbeefdeadbeefdeadbeef";
        var basis = new Basis(
            Id: EntityIdCalculator.ComputeBasisId(BasisSide.Source, "base", fakeTreeHash),
            Side: BasisSide.Source, Label: "base", Root: ".", TreeHash: fakeTreeHash,
            Toolchain: null, Analyzer: "rust-syn", Created: DateTimeOffset.UtcNow);
        store.InsertBasis(basis);

        var result = new RustSynProvider().Import(store, basis, dumpPath);

        Assert.Equal(2, result.EntityCount); // the winning "smoke" + the unrelated "gamma::unrelated"
        Assert.Equal(1, result.DroppedDuplicateCount);
    }

    [Fact]
    public void RustSynProviderReportsZeroDroppedDuplicatesOnTheOrdinaryCommittedSampleDump()
    {
        string sampleDumpPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample-rust-dump.json");
        Assert.True(File.Exists(sampleDumpPath), $"Expected fixture at '{sampleDumpPath}'.");

        using var dbDir = new TempDirectory();
        using var store = new MapStore(Path.Combine(dbDir.Path, "kpmap.db"));
        const string fakeTreeHash = "deadbeefdeadbeefdeadbeefdeadbeefdeadbeefdeadbeefdeadbeefdeadbeef";
        var basis = new Basis(
            Id: EntityIdCalculator.ComputeBasisId(BasisSide.Source, "base", fakeTreeHash),
            Side: BasisSide.Source, Label: "base", Root: "fixtures/slice-zero/rust", TreeHash: fakeTreeHash,
            Toolchain: null, Analyzer: "rust-syn", Created: DateTimeOffset.UtcNow);
        store.InsertBasis(basis);

        var result = new RustSynProvider().Import(store, basis, sampleDumpPath);

        Assert.Equal(0, result.DroppedDuplicateCount);
    }
}
