namespace KodeWork.Core;

/// <summary>
/// Optional demo board for empty ledgers and tests. Not an organization's real inventory.
/// Instance data belongs in the private <c>KODEWORK_DATA</c> repo, not in this source tree.
/// </summary>
public static class DemoSeed
{
    public const string RootTitle = "Acme";

    public static void Apply(KodeWorkBoard board, string actor)
    {
        var view = board.Current();
        if (view.Roots.Count > 0)
        {
            return;
        }

        var root = board.Add(actor, "demo: root", new AddItemRequest(RootTitle, "area",
            Body: "Example overlay. Replace this with your own board; do not commit real hosts here."));
        var sites = board.Add(actor, "demo: sites", new AddItemRequest("Sites", "area", root.Id));
        board.Add(actor, "demo: lab", new AddItemRequest("acme-lab", "vpc", sites.Id,
            Status: "running",
            Body: "A fictional lab VM. Forks should delete the demo and start from an empty tree."));
        var work = board.Add(actor, "demo: work", new AddItemRequest("Work", "area", root.Id));
        board.Add(actor, "demo: site", new AddItemRequest("Website", "project", work.Id,
            Status: "doing",
            Body: "Stand-in project so the outline is not empty."));
        board.Add(actor, "demo: idea", new AddItemRequest("Ship the outline", "idea", work.Id));
    }
}
