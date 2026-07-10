using System.Text.Json;

namespace KodePorter.Core.Atlas;

/// <summary>One raw <c>assrt</c> row as read back from <c>GneissBinding.ExportLedgerJsonl()</c>.</summary>
internal readonly record struct LedgerAssrt(string Aid, long Tx, string Subject, string Predicate, string Value, string ValueKind);

/// <summary>
/// An in-memory index over one ledger export (CONTRACT.md §2's canonical JSONL), built once per
/// Atlas generation so claim rows and decision actors can be resolved without re-parsing. Purely
/// a read over already-exported, deterministic content — introduces no additional ledger writes
/// (unlike <c>GneissLedger.Ask</c>, which appends a receipt row on every call).
/// </summary>
internal sealed class LedgerIndex
{
    public required IReadOnlyDictionary<string, LedgerAssrt> AssrtByAid { get; init; }
    public required IReadOnlyDictionary<long, string> ActorByTx { get; init; }
    public required IReadOnlyList<LedgerAssrt> AssrtsInTxOrder { get; init; }
    public required IReadOnlyDictionary<string, IReadOnlyList<string>> JustInputsByAid { get; init; }

    public static LedgerIndex Build(IReadOnlyList<string> exportLines)
    {
        var assrtByAid = new Dictionary<string, LedgerAssrt>(StringComparer.Ordinal);
        var actorByTx = new Dictionary<long, string>();
        var ordered = new List<LedgerAssrt>();
        var justInputs = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (string line in exportLines)
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            string kind = root.GetProperty("kind").GetString()!;
            switch (kind)
            {
                case "tx":
                    actorByTx[root.GetProperty("id").GetInt64()] = root.GetProperty("actor").GetString()!;
                    break;
                case "assrt":
                    var a = new LedgerAssrt(
                        root.GetProperty("aid").GetString()!,
                        root.GetProperty("tx").GetInt64(),
                        root.GetProperty("subj").GetString()!,
                        root.GetProperty("pred").GetString()!,
                        root.GetProperty("val").GetString()!,
                        root.GetProperty("valkind").GetString()!);
                    assrtByAid[a.Aid] = a;
                    ordered.Add(a);
                    break;
                case "just":
                    // The Gneiss export serializes just rows with camelCase field names
                    // ("inputAid"); accept the snake_case table-column spelling defensively too.
                    if ((root.TryGetProperty("inputAid", out var inputEl) || root.TryGetProperty("input_aid", out inputEl))
                        && inputEl.ValueKind == JsonValueKind.String)
                    {
                        string owner = root.GetProperty("aid").GetString()!;
                        if (!justInputs.TryGetValue(owner, out var list))
                            justInputs[owner] = list = [];
                        list.Add(inputEl.GetString()!);
                    }
                    break;
            }
        }

        ordered = ordered.OrderBy(a => a.Tx).ThenBy(a => a.Aid, StringComparer.Ordinal).ToList();

        return new LedgerIndex
        {
            AssrtByAid = assrtByAid,
            ActorByTx = actorByTx,
            AssrtsInTxOrder = ordered,
            JustInputsByAid = justInputs.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<string>)kv.Value, StringComparer.Ordinal),
        };
    }

    /// <summary>The actor that wrote the transaction containing <paramref name="aid"/>, or null if unknown.</summary>
    public string? ActorFor(string aid) =>
        AssrtByAid.TryGetValue(aid, out var a) && ActorByTx.TryGetValue(a.Tx, out var actor) ? actor : null;
}
