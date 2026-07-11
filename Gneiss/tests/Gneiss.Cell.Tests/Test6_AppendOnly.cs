using Gneiss.Cell;
using Microsoft.Data.Sqlite;

namespace Gneiss.Cell.Tests;

/// <summary>
/// CONTRACT.md section 6, test 6: a direct SQL UPDATE on `assrt` via a second connection is aborted by
/// the append-only trigger.
/// </summary>
public sealed class Test6_AppendOnly
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-07-01T00:00:00Z");

    [Fact]
    public void Direct_Update_On_Assrt_Is_Rejected_By_Trigger()
    {
        using var path = new TempFile();
        string aid;
        using (var l = GneissLedger.Create(path.Path))
        {
            var tx = l.Append(TestHelpers.Env("x", "seed", T0), new IAppendItem[] { new NewAssertion("Thing", "p", GValue.Text("v1")) }).Tx.Value;
            aid = TestHelpers.FindAid(l, tx, "Thing", "p");
        } // close so a second connection is not blocked by the writer lock.

        using var side = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path.Path, Mode = SqliteOpenMode.ReadWrite }.ToString());
        side.Open();
        using var cmd = side.CreateCommand();
        cmd.CommandText = "UPDATE assrt SET val = 'v2' WHERE aid = $aid";
        cmd.Parameters.AddWithValue("$aid", aid);

        var ex = Assert.Throws<SqliteException>(() => cmd.ExecuteNonQuery());
        Assert.Contains("append-only", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Direct_Delete_On_Tx_Is_Rejected_By_Trigger()
    {
        using var path = new TempFile();
        using (var l = GneissLedger.Create(path.Path))
        {
            l.Append(TestHelpers.Env("x", "seed", T0), new IAppendItem[] { new NewAssertion("Thing", "p", GValue.Text("v1")) });
        }

        using var side = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path.Path, Mode = SqliteOpenMode.ReadWrite }.ToString());
        side.Open();
        using var cmd = side.CreateCommand();
        cmd.CommandText = "DELETE FROM tx WHERE id = 1";

        var ex = Assert.Throws<SqliteException>(() => cmd.ExecuteNonQuery());
        Assert.Contains("append-only", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
