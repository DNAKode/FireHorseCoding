using KodePorter.Core.Model;
using KodePorter.Core.Providers;
using KodePorter.Core.Store;
using KodePorter.Core.Tests.Support;
using static KodePorter.Core.Tests.Support.EntityAssertions;

namespace KodePorter.Core.Tests;

/// <summary>CONTRACT.md §10 test 2: fixed source string -> exact expected entities.</summary>
public class CSharpRoslynProviderTests
{
    [Fact]
    public void ImportsExpectedEntitiesWithKindsSymbolPathsSpansAndParentLinks()
    {
        using var sourceDir = new TempDirectory();
        CSharpFixture.WriteSource(sourceDir.Path, "HeaderParser.cs", HeaderParserSource.V1);

        using var dbDir = new TempDirectory();
        using var store = new MapStore(Path.Combine(dbDir.Path, "kpmap.db"));
        var basis = BasisPinner.Pin(store, BasisSide.Target, sourceDir.Path, "base");

        var result = new CSharpRoslynProvider().Import(store, basis);

        Assert.Equal(0, result.ErrorDiagnosticCount);
        Assert.Equal(8, result.EntityCount);

        var entities = store.GetEntities(basis.Id).ToDictionary(e => e.SymbolPath);
        Assert.Equal(8, entities.Count);

        var ns = AssertEntity(entities, "HeadScan", "namespace", "HeadScan", 1, 20, parent: null);
        var cls = AssertEntity(entities, "HeadScan.HeaderParser", "class", "HeaderParser", 3, 13, parent: ns);
        AssertEntity(entities, "HeadScan.HeaderParser.MaxLength", "field", "MaxLength", 5, 5, parent: cls);
        AssertEntity(entities, "HeadScan.HeaderParser.Name", "property", "Name", 7, 7, parent: cls);
        var method = AssertEntity(entities, "HeadScan.HeaderParser.Parse(string)", "method", "Parse", 9, 12, parent: cls);
        var enumEntity = AssertEntity(entities, "HeadScan.ParseErrorCode", "enum", "ParseErrorCode", 15, 19, parent: ns);
        AssertEntity(entities, "HeadScan.ParseErrorCode.MissingColon", "enummember", "MissingColon", 17, 17, parent: enumEntity);
        AssertEntity(entities, "HeadScan.ParseErrorCode.BadKey", "enummember", "BadKey", 18, 18, parent: enumEntity);

        // Entity id is content-addressed and independent of span/hash.
        Assert.Equal(EntityIdCalculator.ComputeEntityId(BasisSide.Target, "method", "HeadScan.HeaderParser.Parse(string)"), method.Id);
        Assert.Matches("^[0-9a-f]{64}$", method.Id);
        Assert.Matches("^[0-9a-f]{64}$", method.ContentHash);
    }

    [Fact]
    public void ContentHashDriftIsScopedToTheChangedSpanAndItsAncestors()
    {
        using var sourceDirV1 = new TempDirectory();
        CSharpFixture.WriteSource(sourceDirV1.Path, "HeaderParser.cs", HeaderParserSource.V1);
        using var sourceDirV2 = new TempDirectory();
        CSharpFixture.WriteSource(sourceDirV2.Path, "HeaderParser.cs", HeaderParserSource.V2);

        using var dbDir = new TempDirectory();
        using var store = new MapStore(Path.Combine(dbDir.Path, "kpmap.db"));

        var basisV1 = BasisPinner.Pin(store, BasisSide.Target, sourceDirV1.Path, "v1");
        new CSharpRoslynProvider().Import(store, basisV1);
        var entitiesV1 = store.GetEntities(basisV1.Id).ToDictionary(e => e.SymbolPath);

        var basisV2 = BasisPinner.Pin(store, BasisSide.Target, sourceDirV2.Path, "v2");
        new CSharpRoslynProvider().Import(store, basisV2);
        var entitiesV2 = store.GetEntities(basisV2.Id).ToDictionary(e => e.SymbolPath);

        // The two bases differ in tree_hash (different file bytes) but every entity id
        // (side|kind|symbolPath) is stable across them (K-D3).
        Assert.NotEqual(basisV1.Id, basisV2.Id);
        Assert.Equal(entitiesV1.Keys.OrderBy(k => k), entitiesV2.Keys.OrderBy(k => k));
        foreach (var symbolPath in entitiesV1.Keys)
            Assert.Equal(entitiesV1[symbolPath].Id, entitiesV2[symbolPath].Id);

        // The method body changed -> the method's own hash, and every ancestor whose span
        // textually contains that method (the class, the namespace), drift.
        Assert.NotEqual(
            entitiesV1["HeadScan.HeaderParser.Parse(string)"].ContentHash,
            entitiesV2["HeadScan.HeaderParser.Parse(string)"].ContentHash);
        Assert.NotEqual(
            entitiesV1["HeadScan.HeaderParser"].ContentHash,
            entitiesV2["HeadScan.HeaderParser"].ContentHash);
        Assert.NotEqual(
            entitiesV1["HeadScan"].ContentHash,
            entitiesV2["HeadScan"].ContentHash);

        // Unrelated siblings — outside the method's own span and not its ancestor — do not
        // cry wolf: same content, same hash.
        Assert.Equal(
            entitiesV1["HeadScan.HeaderParser.MaxLength"].ContentHash,
            entitiesV2["HeadScan.HeaderParser.MaxLength"].ContentHash);
        Assert.Equal(
            entitiesV1["HeadScan.HeaderParser.Name"].ContentHash,
            entitiesV2["HeadScan.HeaderParser.Name"].ContentHash);
        Assert.Equal(
            entitiesV1["HeadScan.ParseErrorCode"].ContentHash,
            entitiesV2["HeadScan.ParseErrorCode"].ContentHash);
        Assert.Equal(
            entitiesV1["HeadScan.ParseErrorCode.MissingColon"].ContentHash,
            entitiesV2["HeadScan.ParseErrorCode.MissingColon"].ContentHash);
        Assert.Equal(
            entitiesV1["HeadScan.ParseErrorCode.BadKey"].ContentHash,
            entitiesV2["HeadScan.ParseErrorCode.BadKey"].ContentHash);
    }
}
