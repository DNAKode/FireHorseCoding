using KodePorter.Core.Model;
using KodePorter.Core.Providers;
using KodePorter.Core.Store;
using KodePorter.Core.Tests.Support;

namespace KodePorter.Core.Tests;

/// <summary>CONTRACT.md §10 test 1: the regeneration drill.</summary>
public class RegenerationDrillTests
{
    [Fact]
    public void ImportingTheSameCSharpTreeTwiceIntoFreshDbsProducesIdenticalOrderedDumps()
    {
        using var sourceDir = new TempDirectory();
        CSharpFixture.WriteSource(sourceDir.Path, "HeaderParser.cs", HeaderParserSource.V1);

        var created = new DateTimeOffset(2026, 7, 10, 0, 0, 0, TimeSpan.Zero);

        var dump1 = ImportIntoFreshDb(sourceDir.Path, created);
        var dump2 = ImportIntoFreshDb(sourceDir.Path, created);

        Assert.NotEmpty(dump1);
        Assert.Equal(dump1, dump2);
    }

    private static List<Entity> ImportIntoFreshDb(string sourceRoot, DateTimeOffset created)
    {
        using var dbDir = new TempDirectory();
        using var store = new MapStore(Path.Combine(dbDir.Path, "kpmap.db"));
        var basis = BasisPinner.Pin(store, BasisSide.Target, sourceRoot, "base", created: created);
        new CSharpRoslynProvider().Import(store, basis);

        // The exact drill query from CONTRACT.md §2: `SELECT * ORDER BY id, basis_id`.
        return store.DumpAllEntities().ToList();
    }
}
