using Gneiss.Cell;
using Microsoft.Data.Sqlite;

namespace Gneiss.Cell.Tests;

/// <summary>
/// CONTRACT.md section 6, test 10: delete all `receipt` rows; re-Ask under the same declared contexts
/// -> same ResultHashes. Views are cattle, not pets: the receipt table is a rebuildable projection.
/// </summary>
public sealed class Test10_RebuildDrill
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-07-01T00:00:00Z");

    [Fact]
    public void Deleting_All_Receipts_And_Re_Asking_Reproduces_The_Same_Hashes()
    {
        using var path = new TempFile();
        using var l = GneissLedger.Create(path.Path);

        l.Append(TestHelpers.Env("x", "r1", T0), new IAppendItem[] { new NewAssertion("Thing", "p", GValue.Number(1m)) });
        l.Append(TestHelpers.Env("x", "r2", T0), new IAppendItem[] { new NewAssertion("Thing", "q", GValue.Number(2m)) });
        l.DeclareContext(TestHelpers.Env("x", "ctx", T0), new ContextDecl("Rebuild"));

        var before1 = l.Ask("Rebuild", new Question(Subject: "Thing", Predicate: "p")).Label.ResultHash;
        var before2 = l.Ask("Rebuild", new Question(Subject: "Thing", Predicate: "q")).Label.ResultHash;

        using (var side = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path.Path, Mode = SqliteOpenMode.ReadWrite }.ToString()))
        {
            side.Open();
            using var cmd = side.CreateCommand();
            cmd.CommandText = "DELETE FROM receipt";
            var deleted = cmd.ExecuteNonQuery();
            Assert.True(deleted > 0);
        }

        var after1 = l.Ask("Rebuild", new Question(Subject: "Thing", Predicate: "p")).Label.ResultHash;
        var after2 = l.Ask("Rebuild", new Question(Subject: "Thing", Predicate: "q")).Label.ResultHash;

        Assert.Equal(before1, after1);
        Assert.Equal(before2, after2);
    }
}
