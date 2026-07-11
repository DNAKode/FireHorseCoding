using System.Text;
using GovernanceLedger;
using GovernanceLedger.Tests.Support;
using Microsoft.Data.Sqlite;

namespace GovernanceLedger.Tests;

/// <summary>
/// The required round-trip suite (CONTRACT-M15.md section 7): seed→export→rebuild round-trip
/// (rebuilt db re-exports byte-identically), the seed content shape (9 entries incl. the real
/// Gneiss supersedes decision), and determinism (no DateTimeOffset.UtcNow anywhere in seed
/// content — two independently seeded ledgers at different real wall-clock moments still produce
/// byte-identical exports).
/// </summary>
public class SeedExportRebuildTests
{
    private static void RunOk(string[] args)
    {
        var err = new StringWriter();
        int exit = GovLedgerApp.Run(args, new StringWriter(), err);
        Assert.True(exit == 0, $"govledger {string.Join(' ', args)} failed ({exit}): {err}");
    }

    [Fact]
    public void SeedProducesNineEntriesInclOneRealSupersedesDecision()
    {
        using var dir = new TempDirectory();
        RunOk(["seed", "--dir", dir.Path]);

        var lines = File.ReadAllLines(Path.Combine(dir.Path, "ledger-export.jsonl"));
        var export = LedgerExport.Parse(lines);

        var govDecisionAssertions = export.Assrt.Where(a => a.Pred == "gov.decision").ToList();
        Assert.Equal(8, govDecisionAssertions.Count);

        var decisions = export.Dec.ToList();
        Assert.Single(decisions);
        Assert.Equal("supersedes", decisions[0].DecisionKind);

        // The supersedes decision must target a real assertion aid recorded earlier in the ledger
        // (the "post-M1-positioning-map-is-product" content assertion) — not a fabricated/self aid.
        var target = govDecisionAssertions.Single(a => a.Subj == "decision:post-M1-positioning-map-is-product");
        Assert.Equal(target.Aid, decisions[0].TgtAid);
        var supersedesTx = export.Tx.Single(t => t.Id == decisions[0].Tx);
        Assert.True(supersedesTx.Id > target.Tx, "the supersedes decision must be recorded strictly after its target (I6).");
    }

    [Fact]
    public void ExportFileIsLfOnlyUtf8NoBom()
    {
        using var dir = new TempDirectory();
        RunOk(["seed", "--dir", dir.Path]);

        byte[] bytes = File.ReadAllBytes(Path.Combine(dir.Path, "ledger-export.jsonl"));
        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF, "must not have a UTF-8 BOM");

        string text = Encoding.UTF8.GetString(bytes);
        Assert.DoesNotContain('\r', text);
        Assert.EndsWith("\n", text);
    }

    [Fact]
    public void SeedExportRebuild_ReExportIsByteIdentical()
    {
        using var dir = new TempDirectory();
        RunOk(["seed", "--dir", dir.Path]);

        byte[] jsonlBefore = File.ReadAllBytes(Path.Combine(dir.Path, "ledger-export.jsonl"));
        byte[] lensBefore = File.ReadAllBytes(Path.Combine(dir.Path, "LENS.html"));

        RunOk(["rebuild", "--dir", dir.Path]);

        byte[] jsonlAfter = File.ReadAllBytes(Path.Combine(dir.Path, "ledger-export.jsonl"));
        byte[] lensAfter = File.ReadAllBytes(Path.Combine(dir.Path, "LENS.html"));

        Assert.Equal(jsonlBefore, jsonlAfter);
        Assert.Equal(lensBefore, lensAfter);
    }

    [Fact]
    public void RebuildWorksFromExportAloneWithNoPriorDb()
    {
        // Simulates a fresh clone: only the committed jsonl exists, ledger.db (gitignored) does not.
        using var dir = new TempDirectory();
        RunOk(["seed", "--dir", dir.Path]);
        byte[] jsonlBefore = File.ReadAllBytes(Path.Combine(dir.Path, "ledger-export.jsonl"));
        byte[] lensBefore = File.ReadAllBytes(Path.Combine(dir.Path, "LENS.html"));

        // Microsoft.Data.Sqlite pools connections; clear the pool so this same-process delete
        // (simulating "no local ledger.db, only the committed jsonl") doesn't race a handle the
        // prior seed call's disposed connection left pooled.
        SqliteConnection.ClearAllPools();
        File.Delete(Path.Combine(dir.Path, "ledger.db"));
        RunOk(["rebuild", "--dir", dir.Path]);

        Assert.True(File.Exists(Path.Combine(dir.Path, "ledger.db")));
        Assert.Equal(jsonlBefore, File.ReadAllBytes(Path.Combine(dir.Path, "ledger-export.jsonl")));
        Assert.Equal(lensBefore, File.ReadAllBytes(Path.Combine(dir.Path, "LENS.html")));
    }

    [Fact]
    public void RepeatedExportOfUnchangedLedgerIsByteIdentical()
    {
        using var dir = new TempDirectory();
        RunOk(["seed", "--dir", dir.Path]);
        byte[] jsonl1 = File.ReadAllBytes(Path.Combine(dir.Path, "ledger-export.jsonl"));
        byte[] lens1 = File.ReadAllBytes(Path.Combine(dir.Path, "LENS.html"));

        RunOk(["export", "--dir", dir.Path]);
        byte[] jsonl2 = File.ReadAllBytes(Path.Combine(dir.Path, "ledger-export.jsonl"));
        byte[] lens2 = File.ReadAllBytes(Path.Combine(dir.Path, "LENS.html"));

        Assert.Equal(jsonl1, jsonl2);
        Assert.Equal(lens1, lens2);
    }

    [Fact]
    public void SeedContentNeverUsesLiveWallClock_TwoSeedsAtDifferentRealMomentsMatchByteForByte()
    {
        using var dirA = new TempDirectory();
        using var dirB = new TempDirectory();

        RunOk(["seed", "--dir", dirA.Path]);
        Thread.Sleep(50); // a real, differing wall-clock moment between the two seed runs
        RunOk(["seed", "--dir", dirB.Path]);

        byte[] jsonlA = File.ReadAllBytes(Path.Combine(dirA.Path, "ledger-export.jsonl"));
        byte[] jsonlB = File.ReadAllBytes(Path.Combine(dirB.Path, "ledger-export.jsonl"));
        Assert.Equal(jsonlA, jsonlB);

        byte[] lensA = File.ReadAllBytes(Path.Combine(dirA.Path, "LENS.html"));
        byte[] lensB = File.ReadAllBytes(Path.Combine(dirB.Path, "LENS.html"));
        Assert.Equal(lensA, lensB);
    }

    [Fact]
    public void SeedWallOverrideAppliesToEveryTransaction()
    {
        using var dir = new TempDirectory();
        RunOk(["seed", "--dir", dir.Path, "--wall", "2030-01-01T00:00:00.0000000Z"]);

        var export = LedgerExport.Parse(File.ReadAllLines(Path.Combine(dir.Path, "ledger-export.jsonl")));
        Assert.All(export.Tx, t => Assert.Equal("2030-01-01T00:00:00.0000000Z", t.Wall));
        // ValidFrom stays the historical AMENDMENTS.md date regardless of --wall.
        var charters = export.Assrt.Single(a => a.Subj == "decision:charters-established");
        Assert.Equal("2026-07-10T00:00:00.0000000Z", charters.VFrom);
    }
}
