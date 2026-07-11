using System.Security.Cryptography;
using System.Text;
using Gneiss.Cell;

namespace GovernanceLedger.Lens;

/// <summary>
/// Builds a <see cref="LensModel"/> from a ledger's export content plus two live facade calls —
/// <c>Ask</c> (decided-only status per card, CONTRACT-M15.md section 7) and <c>Why</c> (the
/// expandable decision trail). Deterministic given the same ledger state: two exports of the same
/// ledger produce the same model, hence byte-identical LENS.html.
/// </summary>
internal static class LensBuilder
{
    public static LensModel Build(GneissLedger ledger, LedgerExport export, BeliefView beliefs, string jsonl)
    {
        var txById = export.Tx.ToDictionary(t => t.Id, t => t);
        var decByAid = export.Dec.ToDictionary(d => d.Aid, d => d, StringComparer.Ordinal);
        var acceptedByAid = beliefs.Accepted.ToDictionary(e => e.Aid, e => e, StringComparer.Ordinal);
        var defeatedByAid = beliefs.Defeated.ToDictionary(e => e.Aid, e => e, StringComparer.Ordinal);

        var timeline = export.Tx
            .OrderBy(t => t.Id)
            .Select(t => new LensTxRow(t.Id, t.Wall, t.Actor, t.Reason, KindFor(t.Id, export)))
            .ToList();

        var cards = new List<LensCard>();
        foreach (var a in export.Assrt.Where(a => a.Pred == LedgerPaths.PredGovDecision).OrderBy(a => a.Tx).ThenBy(a => a.Aid, StringComparer.Ordinal))
        {
            var tx = txById[a.Tx];
            string id = a.Subj.StartsWith("decision:", StringComparison.Ordinal) ? a.Subj["decision:".Length..] : a.Subj;

            string status;
            string? defeatedByAid_ = null;
            if (acceptedByAid.ContainsKey(a.Aid))
            {
                status = "accepted";
            }
            else if (defeatedByAid.TryGetValue(a.Aid, out var defeatedEntry))
            {
                status = "defeated";
                defeatedByAid_ = defeatedEntry.DefeatedBy;
            }
            else
            {
                var explanation = ledger.Why(LedgerPaths.GovContextName, a.Aid);
                status = explanation.Status;
                defeatedByAid_ = explanation.DefeatedBy;
            }

            LensSupersession? supersededBy = null;
            if (defeatedByAid_ is not null)
            {
                var info = ledger.GetAssertion(defeatedByAid_);
                if (info is not null && txById.TryGetValue(info.Tx, out var superTx))
                {
                    supersededBy = new LensSupersession(defeatedByAid_, superTx.Actor, superTx.Reason, superTx.Wall, superTx.Id);
                }
            }

            var trail = new List<LensTrailEntry>();
            var why = ledger.Why(LedgerPaths.GovContextName, a.Aid);
            foreach (var decisionAid in why.Decisions)
            {
                if (!decByAid.TryGetValue(decisionAid, out var dec))
                    continue;
                if (!txById.TryGetValue(dec.Tx, out var decTx))
                    continue;
                trail.Add(new LensTrailEntry(decisionAid, dec.DecisionKind, decTx.Actor, decTx.Reason, decTx.Wall));
            }

            cards.Add(new LensCard(
                Id: id,
                Subject: a.Subj,
                ValueText: a.Val,
                Actor: tx.Actor,
                Method: a.Meth ?? "",
                Source: a.Src,
                Reason: tx.Reason,
                ValidFrom: DateOnlyPart(a.VFrom),
                Wall: tx.Wall,
                Status: status,
                SupersededBy: supersededBy,
                Trail: trail));
        }

        string sha256 = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(jsonl)));

        return new LensModel(timeline, cards, sha256);
    }

    private static string? KindFor(long txId, LedgerExport export)
    {
        var dec = export.Dec.FirstOrDefault(d => d.Tx == txId);
        if (dec is not null)
            return dec.DecisionKind;
        var assrt = export.Assrt.FirstOrDefault(a => a.Tx == txId);
        return assrt?.Pred switch
        {
            "gneiss.predicate" or "gneiss.context" => "bootstrap",
            LedgerPaths.PredGovDecision => "decision",
            null => null,
            var other => other,
        };
    }

    private static string DateOnlyPart(string? iso) => iso is { Length: >= 10 } ? iso[..10] : (iso ?? "");
}
