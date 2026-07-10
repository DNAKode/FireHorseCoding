using Gneiss.Cell;

namespace Gneiss.Cell.Tests;

/// <summary>
/// CONTRACT.md section 6, test 9: ask, then append a decision retracting a consumed aid -> CheckStale
/// true with a cause; an unrelated append leaves a receipt fresh.
/// </summary>
public sealed class Test9_ReceiptStaleness
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-07-01T00:00:00Z");

    [Fact]
    public void Retracting_A_Consumed_Aid_Makes_The_Receipt_Stale()
    {
        using var path = new TempFile();
        using var l = GneissLedger.Create(path.Path);

        var tx = l.Append(TestHelpers.Env("x", "seed", T0), new IAppendItem[] { new NewAssertion("Y", "p", GValue.Number(1m)) }).Value;
        var aid = TestHelpers.FindAid(l, tx, "Y", "p");

        l.DeclareContext(TestHelpers.Env("x", "ctx", T0), new ContextDecl("St"));
        var view = l.Ask("St", new Question(Subject: "Y", Predicate: "p"));
        var receiptId = view.Label.ReceiptId;

        var before = l.CheckStale(receiptId);
        Assert.False(before.Stale);
        Assert.Empty(before.Causes);

        l.Append(TestHelpers.Env("x", "retract", T0), new IAppendItem[] { new NewDecision(DecisionKind.Retracts, TargetAid: aid) });

        var after = l.CheckStale(receiptId);
        Assert.True(after.Stale);
        Assert.NotEmpty(after.Causes);
    }

    [Fact]
    public void Unrelated_Append_Leaves_Receipt_Fresh()
    {
        using var path = new TempFile();
        using var l = GneissLedger.Create(path.Path);

        l.Append(TestHelpers.Env("x", "seed", T0), new IAppendItem[] { new NewAssertion("Y2", "p", GValue.Number(1m)) });

        l.DeclareContext(TestHelpers.Env("x", "ctx", T0), new ContextDecl("St2"));
        var view = l.Ask("St2", new Question(Subject: "Y2", Predicate: "p"));
        var receiptId = view.Label.ReceiptId;

        l.Append(TestHelpers.Env("x", "unrelated", T0), new IAppendItem[] { new NewAssertion("Unrelated", "q", GValue.Text("z")) });

        var after = l.CheckStale(receiptId);
        Assert.False(after.Stale);
        Assert.Empty(after.Causes);
    }
}
