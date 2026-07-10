using System.Text.Json;
using Gneiss.Cell;

namespace Gneiss.Cell.Tests;

internal static class TestHelpers
{
    private static readonly string Root = Path.Combine(Path.GetTempPath(), "gneiss-cell-tests");

    internal static string NewLedgerPath()
    {
        Directory.CreateDirectory(Root);
        return Path.Combine(Root, Guid.NewGuid().ToString("n") + ".db");
    }

    internal static TxEnvelope Env(string actor, string reason, DateTimeOffset wall, string? batch = null) =>
        new(actor, reason, wall, batch);

    /// <summary>
    /// Finds the aid of an assertion written at a given tx, by subject+predicate, by reading back
    /// through the public <see cref="GneissLedger.ExportLedgerJsonl"/> surface. Aid computation is a
    /// deterministic internal detail (CONTRACT.md section 1); tests discover ids this way rather than
    /// depending on the hash formula.
    /// </summary>
    internal static string FindAid(GneissLedger ledger, long tx, string subj, string pred)
    {
        foreach (var line in ledger.ExportLedgerJsonl())
        {
            using var doc = JsonDocument.Parse(line);
            var r = doc.RootElement;
            if (r.GetProperty("kind").GetString() != "assrt")
            {
                continue;
            }
            if (r.GetProperty("tx").GetInt64() == tx &&
                r.GetProperty("subj").GetString() == subj &&
                r.GetProperty("pred").GetString() == pred)
            {
                return r.GetProperty("aid").GetString()!;
            }
        }
        throw new InvalidOperationException($"No assrt row found at tx={tx} subj={subj} pred={pred}.");
    }

    /// <summary>Finds the ckey of an assertion written at a given tx, by subject+predicate.</summary>
    internal static string FindCKey(GneissLedger ledger, long tx, string subj, string pred)
    {
        foreach (var line in ledger.ExportLedgerJsonl())
        {
            using var doc = JsonDocument.Parse(line);
            var r = doc.RootElement;
            if (r.GetProperty("kind").GetString() != "assrt")
            {
                continue;
            }
            if (r.GetProperty("tx").GetInt64() == tx &&
                r.GetProperty("subj").GetString() == subj &&
                r.GetProperty("pred").GetString() == pred)
            {
                return r.GetProperty("ckey").GetString()!;
            }
        }
        throw new InvalidOperationException($"No assrt row found at tx={tx} subj={subj} pred={pred}.");
    }

    /// <summary>
    /// Replicates CONTRACT.md section 1's aid formula (SHA-256 of "tx:ordinal|subj|pred|val|vfrom|vto|status|src|meth")
    /// so a test can construct a decision that deliberately targets an aid from the SAME transaction —
    /// the only reachable way to exercise the "same-tx" branch of I6 through the public API (a decision
    /// cannot legally target a not-yet-existing future aid). This couples the test to the aid formula,
    /// which is the point: I6's determinism guarantee (CONTRACT.md section 1) is what makes this legal.
    /// </summary>
    internal static string PredictAid(long tx, int ordinal, string subj, string pred, string val, string? vfrom, string? vto, string status, string? src, string? meth)
    {
        var content = $"{tx}:{ordinal}|{subj}|{pred}|{val}|{vfrom ?? string.Empty}|{vto ?? string.Empty}|{status}|{src ?? string.Empty}|{meth ?? string.Empty}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }
}

/// <summary>Reserves a unique ledger path and deletes the file (if any) on Dispose.</summary>
internal sealed class TempFile : IDisposable
{
    internal string Path { get; } = TestHelpers.NewLedgerPath();

    public void Dispose()
    {
        try
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
        catch
        {
            // best-effort cleanup
        }
    }
}
