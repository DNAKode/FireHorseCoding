using System.Text;
using Gneiss.Cell;
using GovernanceLedger.Lens;

namespace GovernanceLedger;

/// <summary>`export --dir` (CONTRACT-M15.md section 7): writes the canonical
/// `ledger-export.jsonl` (the committed durable artifact — LF, UTF-8 no BOM) and regenerates
/// `LENS.html` from it.</summary>
internal static class ExportOp
{
    private static readonly UTF8Encoding NoBom = new(encoderShouldEmitUTF8Identifier: false);

    public static void Run(string dir)
    {
        string dbPath = LedgerPaths.DbPath(dir);
        if (!File.Exists(dbPath))
            throw new CliDomainException($"No ledger at '{dbPath}'. Run 'seed --dir {dir}' first.");

        using var ledger = GneissLedger.Open(dbPath);
        WriteExportAndLens(ledger, dir);
    }

    /// <summary>Shared by `export` and `rebuild` (which re-exports immediately after replaying the
    /// jsonl, to prove the round trip is byte-identical).</summary>
    public static void WriteExportAndLens(GneissLedger ledger, string dir)
    {
        Directory.CreateDirectory(dir);

        IReadOnlyList<string> lines = ledger.ExportLedgerJsonl();
        string jsonl = lines.Count == 0 ? "" : string.Join('\n', lines) + "\n";
        File.WriteAllText(LedgerPaths.ExportPath(dir), jsonl, NoBom);

        var export = LedgerExport.Parse(lines);
        var beliefs = ledger.Ask(LedgerPaths.GovContextName, new Question());
        var model = LensBuilder.Build(ledger, export, beliefs, jsonl);
        string html = LensHtmlRenderer.Render(model);
        File.WriteAllText(LedgerPaths.LensPath(dir), html, NoBom);
    }
}
