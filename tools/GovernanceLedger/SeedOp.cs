using Gneiss.Cell;

namespace GovernanceLedger;

/// <summary>`seed --dir governance [--wall <iso8601>]` (CONTRACT-M15.md section 7): creates
/// `ledger.db` from scratch and records the seed history (SeedTable), then exports. `--wall`, if
/// given, overrides every seed transaction's wall clock uniformly (useful for tests that want one
/// fixed instant); omitted, each entry uses its own fixed historical date (never
/// DateTimeOffset.UtcNow either way).</summary>
internal static class SeedOp
{
    public static void Run(string dir, DateTimeOffset? wallOverride)
    {
        Directory.CreateDirectory(dir);
        string dbPath = LedgerPaths.DbPath(dir);
        if (File.Exists(dbPath))
            throw new CliDomainException($"Ledger already exists at '{dbPath}'. Delete it (it is gitignored/derived) to re-seed, or use 'rebuild' to recreate it from ledger-export.jsonl.");

        using var ledger = GneissLedger.Create(dbPath);
        SeedRunner.Run(ledger, wallOverride);
        ExportOp.WriteExportAndLens(ledger, dir);
    }
}
