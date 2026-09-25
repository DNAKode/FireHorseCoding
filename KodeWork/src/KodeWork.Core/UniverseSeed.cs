namespace KodeWork.Core;

/// <summary>First real nodes for the Koderbot universe board. Idempotent by title under a parent.</summary>
public static class UniverseSeed
{
    public static void Apply(KodeWorkBoard board, string actor)
    {
        var view = board.Current();
        if (view.Roots.Any(r => r.Item.Title == "Koderbot universe"))
        {
            return;
        }

        var uni = board.Add(actor, "seed: universe", new AddItemRequest("Koderbot universe", "area",
            Body: "Standing overlay for hosts, connectors, and work — above per-project trackers."));
        var hosts = board.Add(actor, "seed: hosts", new AddItemRequest("Hosts", "area", uni.Id));
        board.Add(actor, "seed: dna-koderbot", new AddItemRequest("dna-koderbot", "vpc", hosts.Id,
            Status: "running",
            Body: "OpenClaw host. Public name koderbot.dnakode.com. Ubuntu 25.10."));
        var connectors = board.Add(actor, "seed: connectors", new AddItemRequest("Connectors", "area", uni.Id));
        board.Add(actor, "seed: outlook", new AddItemRequest("Outlook / Microsoft Graph", "project", connectors.Id,
            Status: "doing",
            Body: "Plugin at ~/.openclaw/extensions/microsoft-graph. Outbound sendMail worked 2026-03-12."));
        var work = board.Add(actor, "seed: work", new AddItemRequest("Work", "area", uni.Id));
        board.Add(actor, "seed: kodework", new AddItemRequest("KodeWork", "project", work.Id,
            Status: "doing",
            Body: "This board. Gneiss.Cell domain. Page at /kodework."));
        board.Add(actor, "seed: compseek", new AddItemRequest("CompSeek", "project", work.Id,
            Status: "open",
            Body: "Competition-data system. Also a Gneiss motivating domain."));
        board.Add(actor, "seed: firehorse", new AddItemRequest("FireHorse / Gneiss", "project", work.Id,
            Status: "doing",
            Body: "Public repo DNAKode/FireHorseCoding. KodeWork is the minimal Gneiss example."));
    }
}
