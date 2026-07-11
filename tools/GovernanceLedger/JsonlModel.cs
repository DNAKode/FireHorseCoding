using System.Text.Json;

namespace GovernanceLedger;

// Public (not internal): this is the plain data shape of the committed ledger-export.jsonl, with
// no implementation-sensitive surface — GovernanceLedger.Tests parses real export files with it
// directly rather than re-deriving a second copy of the parsing logic (mirrors how KodePorter.Core
// keeps its model types public so KodePorter.Core.Tests can assert on them directly).
public sealed record TxRecord(long Id, string Wall, string Actor, string Reason, string? Batch);

public sealed record AssrtRecord(
    string Aid, long Tx, string Subj, string Pred, string Val, string ValKind,
    string? VFrom, string? VTo, string Status, string? Src, string? Meth, int? Conf, string CKey);

public sealed record DecRecord(string Aid, long Tx, string DecisionKind, string? TgtAid, string? TgtCKey);

public sealed record JustRecord(string Aid, string? InputAid, string? RuleVer, string? Role);

/// <summary>The parsed shape of `ledger-export.jsonl` (four record kinds, exactly as
/// <c>GneissLedger.ExportLedgerJsonl</c> writes them — one JSON object per line, grouped by kind,
/// each group internally sorted per CONTRACT.md section 5). Parsing uses System.Text.Json (BCL,
/// not a package) since this is content Gneiss.Cell itself wrote in a known fixed shape — decoding
/// is not the hash-sensitive direction (mirrors Gneiss.Cell's own DeclarationCodec discipline).</summary>
public sealed record LedgerExport(
    IReadOnlyList<TxRecord> Tx,
    IReadOnlyList<AssrtRecord> Assrt,
    IReadOnlyList<DecRecord> Dec,
    IReadOnlyList<JustRecord> Just)
{
    public static LedgerExport Parse(IEnumerable<string> lines)
    {
        var tx = new List<TxRecord>();
        var assrt = new List<AssrtRecord>();
        var dec = new List<DecRecord>();
        var just = new List<JustRecord>();

        foreach (var line in lines)
        {
            if (line.Length == 0)
                continue;
            using var doc = JsonDocument.Parse(line);
            var r = doc.RootElement;
            string kind = r.GetProperty("kind").GetString()!;
            switch (kind)
            {
                case "tx":
                    tx.Add(new TxRecord(
                        Id: r.GetProperty("id").GetInt64(),
                        Wall: r.GetProperty("wall").GetString()!,
                        Actor: r.GetProperty("actor").GetString()!,
                        Reason: r.GetProperty("reason").GetString()!,
                        Batch: NullableString(r, "batch")));
                    break;
                case "assrt":
                    assrt.Add(new AssrtRecord(
                        Aid: r.GetProperty("aid").GetString()!,
                        Tx: r.GetProperty("tx").GetInt64(),
                        Subj: r.GetProperty("subj").GetString()!,
                        Pred: r.GetProperty("pred").GetString()!,
                        Val: r.GetProperty("val").GetString()!,
                        ValKind: r.GetProperty("valkind").GetString()!,
                        VFrom: NullableString(r, "vfrom"),
                        VTo: NullableString(r, "vto"),
                        Status: r.GetProperty("status").GetString()!,
                        Src: NullableString(r, "src"),
                        Meth: NullableString(r, "meth"),
                        Conf: NullableInt(r, "conf"),
                        CKey: r.GetProperty("ckey").GetString()!));
                    break;
                case "dec":
                    dec.Add(new DecRecord(
                        Aid: r.GetProperty("aid").GetString()!,
                        Tx: r.GetProperty("tx").GetInt64(),
                        DecisionKind: r.GetProperty("decisionKind").GetString()!,
                        TgtAid: NullableString(r, "tgtAid"),
                        TgtCKey: NullableString(r, "tgtCKey")));
                    break;
                case "just":
                    just.Add(new JustRecord(
                        Aid: r.GetProperty("aid").GetString()!,
                        InputAid: NullableString(r, "inputAid"),
                        RuleVer: NullableString(r, "ruleVer"),
                        Role: NullableString(r, "role")));
                    break;
                default:
                    throw new InvalidOperationException($"Unknown ledger-export record kind '{kind}'.");
            }
        }

        return new LedgerExport(tx, assrt, dec, just);
    }

    private static string? NullableString(JsonElement r, string prop)
    {
        var el = r.GetProperty(prop);
        return el.ValueKind == JsonValueKind.Null ? null : el.GetString();
    }

    private static int? NullableInt(JsonElement r, string prop)
    {
        var el = r.GetProperty(prop);
        return el.ValueKind == JsonValueKind.Null ? null : el.GetInt32();
    }
}
