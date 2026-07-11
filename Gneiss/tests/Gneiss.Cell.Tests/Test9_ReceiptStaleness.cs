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

        var tx = l.Append(TestHelpers.Env("x", "seed", T0), new IAppendItem[] { new NewAssertion("Y", "p", GValue.Number(1m)) }).Tx.Value;
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

    /// <summary>
    /// kb defect 2 (MAJOR): the consumed set must close transitively over the decision graph. A fact;
    /// D1 retracts A; D2 retracts D1 (a decision targeting a decision). Before the fix, Ask's consumed
    /// set recorded only the one-hop decision (D1), never D2 -- so a later decision that flips D1's
    /// effectiveness by defeating D2 (not A, not D1 directly) was invisible to CheckStale.
    /// </summary>
    [Fact]
    public void DecisionOnDecision_Chain_Is_Closed_Into_The_Consumed_Set_And_CheckStale_Sees_It()
    {
        using var path = new TempFile();
        using var l = GneissLedger.Create(path.Path);

        var txA = l.Append(TestHelpers.Env("x", "seed", T0), new IAppendItem[] { new NewAssertion("Chain9", "p", GValue.Number(1m)) }).Tx.Value;
        var aidA = TestHelpers.FindAid(l, txA, "Chain9", "p");

        var txD1 = l.Append(TestHelpers.Env("x", "d1", T0), new IAppendItem[] { new NewDecision(DecisionKind.Retracts, TargetAid: aidA) }).Tx.Value;
        var aidD1 = TestHelpers.FindAid(l, txD1, aidA, "gneiss.decision");

        var txD2 = l.Append(TestHelpers.Env("x", "d2", T0), new IAppendItem[] { new NewDecision(DecisionKind.Retracts, TargetAid: aidD1) }).Tx.Value;
        var aidD2 = TestHelpers.FindAid(l, txD2, aidD1, "gneiss.decision");

        l.DeclareContext(TestHelpers.Env("x", "ctx", T0), new ContextDecl("Chain9Ctx"));
        var view = l.Ask("Chain9Ctx", new Question(Subject: "Chain9", Predicate: "p"));

        // D1 is defeated by D2 (D2 retracts D1), so D1 is ineffective and A survives, Accepted.
        var accepted = Assert.Single(view.Accepted);
        Assert.Equal(aidA, accepted.Aid);

        // the fix: both hops of the decision chain are consumed, not just the one-hop D1.
        Assert.Contains(aidD1, view.Label.ConsumedAids);
        Assert.Contains(aidD2, view.Label.ConsumedAids);

        var receiptId = view.Label.ReceiptId;
        var before = l.CheckStale(receiptId);
        Assert.False(before.Stale);
        Assert.Empty(before.Causes);

        // D3 rejects D2 -- this flips D1 back to effective, which flips A's answer -- but D3 targets D2,
        // not A and not D1, so only a transitively-closed consumed set can see it.
        l.Append(TestHelpers.Env("x", "d3", T0), new IAppendItem[] { new NewDecision(DecisionKind.Rejects, TargetAid: aidD2) });

        var after = l.CheckStale(receiptId);
        Assert.True(after.Stale);
        Assert.NotEmpty(after.Causes);
    }

    /// <summary>Control for the decision-on-decision closure: a decision elsewhere in the ledger that
    /// never touches the D1/D2 chain must not mark the chain's receipt stale.</summary>
    [Fact]
    public void Unrelated_Decision_Elsewhere_Does_Not_Mark_The_Chain_Receipt_Stale()
    {
        using var path = new TempFile();
        using var l = GneissLedger.Create(path.Path);

        var txA = l.Append(TestHelpers.Env("x", "seed", T0), new IAppendItem[] { new NewAssertion("Chain9b", "p", GValue.Number(1m)) }).Tx.Value;
        var aidA = TestHelpers.FindAid(l, txA, "Chain9b", "p");

        var txD1 = l.Append(TestHelpers.Env("x", "d1", T0), new IAppendItem[] { new NewDecision(DecisionKind.Retracts, TargetAid: aidA) }).Tx.Value;
        var aidD1 = TestHelpers.FindAid(l, txD1, aidA, "gneiss.decision");

        l.Append(TestHelpers.Env("x", "d2", T0), new IAppendItem[] { new NewDecision(DecisionKind.Retracts, TargetAid: aidD1) });

        l.DeclareContext(TestHelpers.Env("x", "ctx", T0), new ContextDecl("Chain9bCtx"));
        var view = l.Ask("Chain9bCtx", new Question(Subject: "Chain9b", Predicate: "p"));
        var receiptId = view.Label.ReceiptId;

        var txElsewhere = l.Append(TestHelpers.Env("x", "elsewhere", T0), new IAppendItem[] { new NewAssertion("Elsewhere9", "q", GValue.Text("z")) }).Tx.Value;
        var aidElsewhere = TestHelpers.FindAid(l, txElsewhere, "Elsewhere9", "q");
        l.Append(TestHelpers.Env("x", "unrelated-decision", T0), new IAppendItem[] { new NewDecision(DecisionKind.Retracts, TargetAid: aidElsewhere) });

        var after = l.CheckStale(receiptId);
        Assert.False(after.Stale);
        Assert.Empty(after.Causes);
    }

    /// <summary>
    /// The belief-fold half of kb defect 2's scenario (not the consumed-set half): D3 rejects D2, D2
    /// retracts D1, D1 retracts A, asserted in four separate transactions -> A ends Defeated, because
    /// D1 is effective (D2, which would defeat D1, is itself defeated by the effective D3).
    /// </summary>
    [Fact]
    public void Rejecting_The_Chain_Head_Makes_The_Original_Retraction_Effective_Again()
    {
        using var path = new TempFile();
        using var l = GneissLedger.Create(path.Path);

        var txA = l.Append(TestHelpers.Env("x", "seed", T0), new IAppendItem[] { new NewAssertion("Chain9c", "p", GValue.Number(1m)) }).Tx.Value;
        var aidA = TestHelpers.FindAid(l, txA, "Chain9c", "p");

        var txD1 = l.Append(TestHelpers.Env("x", "d1", T0), new IAppendItem[] { new NewDecision(DecisionKind.Retracts, TargetAid: aidA) }).Tx.Value;
        var aidD1 = TestHelpers.FindAid(l, txD1, aidA, "gneiss.decision");

        var txD2 = l.Append(TestHelpers.Env("x", "d2", T0), new IAppendItem[] { new NewDecision(DecisionKind.Retracts, TargetAid: aidD1) }).Tx.Value;
        var aidD2 = TestHelpers.FindAid(l, txD2, aidD1, "gneiss.decision");

        l.Append(TestHelpers.Env("x", "d3", T0), new IAppendItem[] { new NewDecision(DecisionKind.Rejects, TargetAid: aidD2) });

        l.DeclareContext(TestHelpers.Env("x", "ctx", T0), new ContextDecl("Chain9cCtx"));
        var view = l.Ask("Chain9cCtx", new Question(Subject: "Chain9c", Predicate: "p"));

        Assert.Empty(view.Accepted);
        var defeated = Assert.Single(view.Defeated);
        Assert.Equal(aidA, defeated.Aid);
        Assert.Equal(aidD1, defeated.DefeatedBy);
        Assert.Equal("decision:retracts", defeated.DefeatReason);
    }
}
