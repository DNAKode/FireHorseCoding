using Gneiss.Cell;
using Microsoft.Data.Sqlite;

namespace GovernanceLedger;

/// <summary>
/// `rebuild --dir` (CONTRACT-M15.md section 7): recreates `ledger.db` from the committed
/// `ledger-export.jsonl` — the durable artifact — by replaying each transaction's sole item
/// through <see cref="GneissLedger.Append"/> in tx order. Because SeedRunner/RecordOp never batch
/// more than one item per Append call, every tx's assertion (and its decision row, if any) always
/// had ordinal 0 — so <c>ComputeAid(txId, 0, ...)</c> reproduces the exact original aid given the
/// same txId (which AUTOINCREMENT reassigns identically on a fresh, empty database replayed in tx
/// order) and the same content fields. Justification order doesn't need preserving: the export's
/// `just` rows are re-sorted deterministically on every export (CONTRACT.md section 5), so any
/// insertion order reproduces byte-identical output. This is what makes the re-export below
/// byte-identical to the jsonl just read.
/// </summary>
internal static class RebuildOp
{
    public static void Run(string dir)
    {
        string exportPath = LedgerPaths.ExportPath(dir);
        if (!File.Exists(exportPath))
            throw new CliDomainException($"No export at '{exportPath}'. Run 'seed --dir {dir}' or restore the committed ledger-export.jsonl first.");

        var export = LedgerExport.Parse(File.ReadAllLines(exportPath));

        string dbPath = LedgerPaths.DbPath(dir);
        // Microsoft.Data.Sqlite pools connections by default, so a prior GneissLedger.Dispose()
        // (e.g. an earlier seed/export call in this same process — the CLI's own tests drive
        // several verbs in-process) can still hold the file open. Clear the pool before deleting so
        // a same-process seed-then-rebuild doesn't hit an IOException here; a real, separate CLI
        // invocation never had a pooled handle to begin with, so this is a no-op there.
        SqliteConnection.ClearAllPools();
        if (File.Exists(dbPath))
            File.Delete(dbPath);
        Directory.CreateDirectory(dir);

        var assrtByTx = new Dictionary<long, AssrtRecord>();
        foreach (var a in export.Assrt)
        {
            if (!assrtByTx.TryAdd(a.Tx, a))
                throw new InvalidOperationException($"rebuild: tx {a.Tx} has more than one assrt row; SeedRunner/RecordOp only ever append one item per tx, so the export is not one this tool wrote.");
        }
        var decByTx = new Dictionary<long, DecRecord>();
        foreach (var d in export.Dec)
        {
            if (!decByTx.TryAdd(d.Tx, d))
                throw new InvalidOperationException($"rebuild: tx {d.Tx} has more than one dec row.");
        }
        var justByAid = export.Just
            .GroupBy(j => j.Aid, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

        using var ledger = GneissLedger.Create(dbPath);

        foreach (var tx in export.Tx.OrderBy(t => t.Id))
        {
            if (!assrtByTx.TryGetValue(tx.Id, out var assrt))
                throw new InvalidOperationException($"rebuild: tx {tx.Id} has no assrt row in the export.");

            IAppendItem item;
            if (decByTx.TryGetValue(tx.Id, out var dec))
            {
                if (!string.Equals(dec.Aid, assrt.Aid, StringComparison.Ordinal))
                    throw new InvalidOperationException($"rebuild: tx {tx.Id} dec.aid '{dec.Aid}' does not match assrt.aid '{assrt.Aid}'.");
                item = new NewDecision(ParseDecisionKind(dec.DecisionKind), TargetAid: dec.TgtAid, TargetClaimKey: dec.TgtCKey);
            }
            else
            {
                IReadOnlyList<JustRef>? justs = justByAid.TryGetValue(assrt.Aid, out var js)
                    ? js.Select(j => new JustRef(j.InputAid, j.RuleVer, j.Role)).ToList()
                    : null;
                item = new NewAssertion(
                    Subject: assrt.Subj,
                    Predicate: assrt.Pred,
                    Value: new GValue(assrt.ValKind, assrt.Val),
                    ValidFrom: assrt.VFrom is null ? null : WallClock.Parse(assrt.VFrom),
                    ValidTo: assrt.VTo is null ? null : WallClock.Parse(assrt.VTo),
                    Proposed: assrt.Status == "proposed",
                    Source: assrt.Src,
                    Method: assrt.Meth,
                    ConfidenceBp: assrt.Conf,
                    Justifications: justs);
            }

            var env = new TxEnvelope(tx.Actor, tx.Reason, WallClock.Parse(tx.Wall), tx.Batch);
            var result = ledger.Append(env, [item]);

            if (result.Tx.Value != tx.Id)
                throw new InvalidOperationException($"rebuild: replaying tx {tx.Id} was assigned tx {result.Tx.Value} instead — the export is not a prefix-complete, gap-free replay of a single ledger.");
            if (!string.Equals(result.Aids[0], assrt.Aid, StringComparison.Ordinal))
                throw new InvalidOperationException($"rebuild: tx {tx.Id} reconstructed aid '{result.Aids[0]}' does not match original aid '{assrt.Aid}'.");
        }

        // Re-export from the rebuilt db: this is the "byte-identical re-export" the contract
        // requires — governance/ledger-export.jsonl and LENS.html are overwritten from the fresh
        // ledger.db, and a byte-diff against the pre-rebuild files should be empty.
        ExportOp.WriteExportAndLens(ledger, dir);
    }

    private static DecisionKind ParseDecisionKind(string wire) => wire switch
    {
        "accepts" => DecisionKind.Accepts,
        "rejects" => DecisionKind.Rejects,
        "retracts" => DecisionKind.Retracts,
        "supersedes" => DecisionKind.Supersedes,
        _ => throw new InvalidOperationException($"rebuild: unknown decisionKind '{wire}' in export."),
    };
}
