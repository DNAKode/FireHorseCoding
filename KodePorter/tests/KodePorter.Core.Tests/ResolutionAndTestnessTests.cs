using KodePorter.Core.Model;
using KodePorter.Core.Providers;
using KodePorter.Core.Store;
using KodePorter.Core.Tests.Support;

namespace KodePorter.Core.Tests;

/// <summary>CONTRACT-M15.md §1.1: entity resolution grade + is_test, both providers.</summary>
public class ResolutionAndTestnessTests
{
    [Fact]
    public void CSharpEntitiesInAFileWithAnErrorDiagnosticAreDegradedAndOthersStayClean()
    {
        using var sourceDir = new TempDirectory();
        CSharpFixture.WriteSource(sourceDir.Path, "Good.cs", """
            namespace Sample
            {
                public sealed class Good
                {
                    public void Ok() { }
                }
            }
            """);
        // References an undefined type -> at least one Error-severity diagnostic in this file only.
        CSharpFixture.WriteSource(sourceDir.Path, "Bad.cs", """
            namespace Sample
            {
                public sealed class Bad
                {
                    public NoSuchType Broken() => default;
                }
            }
            """);

        using var dbDir = new TempDirectory();
        using var store = new MapStore(Path.Combine(dbDir.Path, "kpmap.db"));
        var basis = BasisPinner.Pin(store, BasisSide.Target, sourceDir.Path, "base");

        var result = new CSharpRoslynProvider().Import(store, basis);
        Assert.True(result.ErrorDiagnosticCount >= 1);

        var entities = store.GetEntities(basis.Id).ToDictionary(e => e.SymbolPath);

        Assert.Equal("clean", entities["Sample.Good"].Resolution);
        Assert.Equal("clean", entities["Sample.Good.Ok()"].Resolution);
        Assert.Equal("degraded", entities["Sample.Bad"].Resolution);
        Assert.Equal("degraded", entities["Sample.Bad.Broken()"].Resolution);
    }

    [Fact]
    public void CSharpEntitiesAreMarkedTestByFilePathSegmentOrByContainingTypeNameEndingInTests()
    {
        using var sourceDir = new TempDirectory();
        // File-path heuristic: under a "tests/" segment.
        CSharpFixture.WriteSource(sourceDir.Path, "tests/SomeSpec.cs", """
            namespace Sample
            {
                public sealed class SomeSpec
                {
                    public void CheckSomething() { }
                }
            }
            """);
        // Type-name heuristic: containing type ends with "Tests", file NOT under tests/.
        CSharpFixture.WriteSource(sourceDir.Path, "FooTests.cs", """
            namespace Sample
            {
                public sealed class FooTests
                {
                    public void CheckFoo() { }
                }
            }
            """);
        // Neither heuristic applies.
        CSharpFixture.WriteSource(sourceDir.Path, "Foo.cs", """
            namespace Sample
            {
                public sealed class Foo
                {
                    public void Run() { }
                }
            }
            """);

        using var dbDir = new TempDirectory();
        using var store = new MapStore(Path.Combine(dbDir.Path, "kpmap.db"));
        var basis = BasisPinner.Pin(store, BasisSide.Target, sourceDir.Path, "base");
        new CSharpRoslynProvider().Import(store, basis);

        var entities = store.GetEntities(basis.Id).ToDictionary(e => e.SymbolPath);

        Assert.True(entities["Sample.SomeSpec"].IsTest);
        Assert.True(entities["Sample.SomeSpec.CheckSomething()"].IsTest);

        Assert.True(entities["Sample.FooTests"].IsTest);
        Assert.True(entities["Sample.FooTests.CheckFoo()"].IsTest);

        Assert.False(entities["Sample.Foo"].IsTest);
        Assert.False(entities["Sample.Foo.Run()"].IsTest);
    }

    [Fact]
    public void RustDumpEntitiesPassThroughExplicitResolutionAndIsTestAndDefaultToCleanFalseWhenAbsent()
    {
        using var tempDir = new TempDirectory();
        string dumpPath = Path.Combine(tempDir.Path, "dump.json");
        File.WriteAllText(dumpPath, """
            {
              "provider": "rust-syn@0.1.0",
              "root": "fixtures/slice-zero/rust",
              "entities": [
                {
                  "kind": "fn", "name": "clean_fn", "symbolPath": "krate::clean_fn",
                  "file": "src/lib.rs", "startLine": 1, "endLine": 2,
                  "contentHash": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                  "parentSymbolPath": null
                },
                {
                  "kind": "fn", "name": "gapped_fn", "symbolPath": "krate::gapped_fn",
                  "file": "src/broken.rs", "startLine": 1, "endLine": 1,
                  "contentHash": "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
                  "parentSymbolPath": null, "resolution": "gap"
                },
                {
                  "kind": "fn", "name": "test_it", "symbolPath": "krate::tests::test_it",
                  "file": "src/lib.rs", "startLine": 4, "endLine": 5,
                  "contentHash": "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
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
            Side: BasisSide.Source, Label: "base", Root: "fixtures/slice-zero/rust",
            TreeHash: fakeTreeHash, Toolchain: null, Analyzer: "rust-syn", Created: DateTimeOffset.UtcNow);
        store.InsertBasis(basis);

        new RustSynProvider().Import(store, basis, dumpPath);

        var entities = store.GetEntities(basis.Id).ToDictionary(e => e.SymbolPath);

        Assert.Equal("clean", entities["krate::clean_fn"].Resolution);
        Assert.False(entities["krate::clean_fn"].IsTest);

        Assert.Equal("gap", entities["krate::gapped_fn"].Resolution);
        Assert.False(entities["krate::gapped_fn"].IsTest); // isTest absent -> default false, even though resolution was given

        Assert.Equal("clean", entities["krate::tests::test_it"].Resolution); // resolution absent -> default clean
        Assert.True(entities["krate::tests::test_it"].IsTest);
    }

    [Fact]
    public void RustDumpRejectsAnUnknownResolutionValue()
    {
        using var tempDir = new TempDirectory();
        string dumpPath = Path.Combine(tempDir.Path, "dump.json");
        File.WriteAllText(dumpPath, """
            {
              "provider": "rust-syn@0.1.0",
              "root": ".",
              "entities": [
                {
                  "kind": "fn", "name": "f", "symbolPath": "krate::f",
                  "file": "src/lib.rs", "startLine": 1, "endLine": 1,
                  "contentHash": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                  "parentSymbolPath": null, "resolution": "bogus"
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

        Assert.Throws<InvalidDataException>(() => new RustSynProvider().Import(store, basis, dumpPath));
    }
}
