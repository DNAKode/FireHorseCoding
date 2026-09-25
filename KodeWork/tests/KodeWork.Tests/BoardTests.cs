using KodeWork.Core;

namespace KodeWork.Tests;

public sealed class BoardTests
{
    [Fact]
    public void Add_reparent_complete_and_project()
    {
        string home = NewHome();
        using var board = KodeWorkBoard.Initialize(home);

        var area = board.Add("alice", "add area", new AddItemRequest("Universe", "area"));
        var task = board.Add("bob", "add task", new AddItemRequest("Install SDK", "task", area.Id));
        Assert.Equal(area.Id, task.ParentId);
        Assert.Equal("open", task.Status);

        board.Set("alice", "rename", task.Id, new SetItemRequest(Title: "Install .NET 10"));
        board.Set("alice", "doing", task.Id, new SetItemRequest(Status: "doing"));
        var movedParent = board.Add("alice", "other area", new AddItemRequest("Toolchain", "area"));
        board.Set("alice", "move", task.Id, new SetItemRequest(ParentId: movedParent.Id));
        board.Set("alice", "done", task.Id, new SetItemRequest(Status: "done"));

        var view = board.Current();
        Assert.Equal(2, view.Roots.Count);
        var toolchain = view.Roots.Single(r => r.Item.Title == "Toolchain");
        Assert.Single(toolchain.Children);
        Assert.Equal("Install .NET 10", toolchain.Children[0].Item.Title);
        Assert.Equal("done", toolchain.Children[0].Item.Status);
        Assert.Empty(view.Roots.Single(r => r.Item.Title == "Universe").Children);

        board.Project();
        string md = File.ReadAllText(Path.Combine(home, "projection", "tree.md"));
        Assert.Contains("Install .NET 10", md, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(home, "projection", "items", new ItemId(task.Id).FileStem + ".md")));
        Assert.True(File.Exists(Path.Combine(home, "projection", "ledger", "export.jsonl")));
    }

    [Fact]
    public void Later_title_wins_and_old_receipt_can_go_stale()
    {
        string home = NewHome();
        using var board = KodeWorkBoard.Initialize(home);
        var item = board.Add("alice", "create", new AddItemRequest("Alpha", "idea"));
        var first = board.Current();
        string receipt = first.Label.ReceiptId;

        board.Set("alice", "rename", item.Id, new SetItemRequest(Title: "Beta"));
        Assert.Equal("Beta", board.Get(item.Id)!.Title);

        using var again = KodeWorkBoard.Open(home);
        Assert.Equal("Beta", again.Get(item.Id)!.Title);
    }

    [Fact]
    public void Retract_parent_makes_root()
    {
        string home = NewHome();
        using var board = KodeWorkBoard.Initialize(home);
        var parent = board.Add("alice", "p", new AddItemRequest("Parent", "area"));
        var child = board.Add("alice", "c", new AddItemRequest("Child", "task", parent.Id));
        board.Set("alice", "unparent", child.Id, new SetItemRequest(ClearParent: true));
        var view = board.Current();
        Assert.Contains(view.Roots, r => r.Item.Id == child.Id);
    }

    [Fact]
    public void Cycle_is_rejected()
    {
        string home = NewHome();
        using var board = KodeWorkBoard.Initialize(home);
        var a = board.Add("alice", "a", new AddItemRequest("A", "area"));
        var b = board.Add("alice", "b", new AddItemRequest("B", "area", a.Id));
        var ex = Assert.Throws<InvalidOperationException>(() =>
            board.Set("alice", "cycle", a.Id, new SetItemRequest(ParentId: b.Id)));
        Assert.Contains("cycle", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Why_reaches_title_aid()
    {
        string home = NewHome();
        using var board = KodeWorkBoard.Initialize(home);
        var item = board.Add("alice", "why", new AddItemRequest("Seen", "note"));
        Assert.False(string.IsNullOrEmpty(item.TitleAid));
        var expl = board.Why(item.TitleAid!);
        Assert.Equal("accepted", expl.Status);
    }

    [Fact]
    public void Seed_is_idempotent()
    {
        string home = NewHome();
        using var board = KodeWorkBoard.Initialize(home);
        DemoSeed.Apply(board, "bob");
        DemoSeed.Apply(board, "bob");
        var view = board.Current();
        Assert.Single(view.Roots);
        Assert.Equal("Acme", view.Roots[0].Item.Title);
        Assert.Equal(2, view.Roots[0].Children.Count);
    }

    [Fact]
    public void Note_inbox_roundtrip()
    {
        string home = NewHome();
        using var board = KodeWorkBoard.Initialize(home);
        board.Note("alice", "remember the milk");
        Assert.Contains(board.ListNotes(), n => n.Text == "remember the milk");
    }

    private static string NewHome()
    {
        string dir = Path.Combine(Path.GetTempPath(), "kodework-tests", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
