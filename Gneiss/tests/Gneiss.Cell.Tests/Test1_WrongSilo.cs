using Gneiss.Cell;

namespace Gneiss.Cell.Tests;

/// <summary>
/// CONTRACT.md section 6, test 1: the kb/22-BELIEF-ENGINE.md section 7 wrong-silo table, reproduced
/// cell by cell. This is the acceptance test for the whole engine.
/// </summary>
public sealed class Test1_WrongSilo
{
    private static readonly DateTimeOffset Jun20_1000 = DateTimeOffset.Parse("2026-06-20T10:00:00Z");
    private static readonly DateTimeOffset Jun20_1003 = DateTimeOffset.Parse("2026-06-20T10:03:00Z");
    private static readonly DateTimeOffset Jun20_1030 = DateTimeOffset.Parse("2026-06-20T10:30:00Z");
    private static readonly DateTimeOffset Jun22_0910 = DateTimeOffset.Parse("2026-06-22T09:10:00Z");
    private static readonly DateTimeOffset Jun22_0911 = DateTimeOffset.Parse("2026-06-22T09:11:00Z");

    private sealed class Ledger : IDisposable
    {
        public required GneissLedger L { get; init; }
        public required string AidA1 { get; init; }
        public required string AidA2 { get; init; }
        public required string AidD1 { get; init; }
        public required string AidA3 { get; init; }
        public required long TxA2 { get; init; }
        public void Dispose() => L.Dispose();
    }

    private static Ledger BuildLedger(string path)
    {
        var l = GneissLedger.Create(path);

        // tx1 — A1: fillLevel(Silo17) = 4.2m @ [Jun20 10:00], src=manual_laser, status=fact
        var txA1 = l.Append(
            TestHelpers.Env("surveyor", "routine fill reading", Jun20_1003),
            new IAppendItem[]
            {
                new NewAssertion("Silo17", "fillLevel", GValue.Number(4.2m), ValidFrom: Jun20_1000, Source: "manual_laser"),
            }).Value;
        var aidA1 = TestHelpers.FindAid(l, txA1, "Silo17", "fillLevel");

        // tx2 — A2: massEstimate(Silo17) = 12.4t @ [Jun20 10:00], just={A1, ShapeV7, DensityV3, FormulaV5}
        var txA2 = l.Append(
            TestHelpers.Env("estimator", "derive mass from fill", Jun20_1030),
            new IAppendItem[]
            {
                new NewAssertion("Silo17", "massEstimate", GValue.Number(12.4m), ValidFrom: Jun20_1000,
                    Justifications: new[]
                    {
                        new JustRef(aidA1, "ShapeV7", "input"),
                        new JustRef(null, "DensityV3", "rule"),
                        new JustRef(null, "FormulaV5", "rule"),
                    }),
            }).Value;
        var aidA2 = TestHelpers.FindAid(l, txA2, "Silo17", "massEstimate");

        // tx3 — D1: retracts A1, reason="wrong silo selected"
        var txD1 = l.Append(
            TestHelpers.Env("auditor", "wrong silo selected", Jun22_0910),
            new IAppendItem[] { new NewDecision(DecisionKind.Retracts, TargetAid: aidA1) }).Value;
        var aidD1 = TestHelpers.FindAid(l, txD1, aidA1, "gneiss.decision");

        // tx4 — A3: fillLevel(Silo18) = 4.2m @ [Jun20 10:00], src=correction
        var txA3 = l.Append(
            TestHelpers.Env("auditor", "corrected silo assignment", Jun22_0911),
            new IAppendItem[]
            {
                new NewAssertion("Silo18", "fillLevel", GValue.Number(4.2m), ValidFrom: Jun20_1000, Source: "correction"),
            }).Value;
        var aidA3 = TestHelpers.FindAid(l, txA3, "Silo18", "fillLevel");

        l.DeclareContext(TestHelpers.Env("steward", "declare audit context", Jun22_0911), new ContextDecl("AuditJun21", DataCut: txA2, DefCut: txA2));
        l.DeclareContext(TestHelpers.Env("steward", "declare current context", Jun22_0911), new ContextDecl("Current", DataCut: null, DefCut: null));
        l.DeclareContext(TestHelpers.Env("steward", "declare backtest context", Jun22_0911), new ContextDecl("Backtest", DataCut: txA2, DefCut: null));

        return new Ledger { L = l, AidA1 = aidA1, AidA2 = aidA2, AidD1 = aidD1, AidA3 = aidA3, TxA2 = txA2 };
    }

    [Fact]
    public void Audit_A1_accepted()
    {
        using var path = new TempFile();
        using var s = BuildLedger(path.Path);
        var view = s.L.Ask("AuditJun21", new Question(Subject: "Silo17", Predicate: "fillLevel"));
        Assert.Single(view.Accepted);
        Assert.Equal(s.AidA1, view.Accepted[0].Aid);
        Assert.Empty(view.Defeated);
        Assert.Null(view.Missing);
    }

    [Fact]
    public void Audit_A2_accepted()
    {
        using var path = new TempFile();
        using var s = BuildLedger(path.Path);
        var view = s.L.Ask("AuditJun21", new Question(Subject: "Silo17", Predicate: "massEstimate"));
        Assert.Single(view.Accepted);
        Assert.Equal(s.AidA2, view.Accepted[0].Aid);
        Assert.False(view.Accepted[0].StaleViaJustification);
    }

    [Fact]
    public void Audit_A3_not_visible()
    {
        using var path = new TempFile();
        using var s = BuildLedger(path.Path);
        var view = s.L.Ask("AuditJun21", new Question(Subject: "Silo18", Predicate: "fillLevel"));
        Assert.Empty(view.Accepted);
        Assert.Empty(view.Defeated);
        Assert.NotNull(view.Missing);
        Assert.Equal("unknown", view.Missing!.Kind);
    }

    [Fact]
    public void Current_A1_defeated_by_D1()
    {
        using var path = new TempFile();
        using var s = BuildLedger(path.Path);
        var view = s.L.Ask("Current", new Question(Subject: "Silo17", Predicate: "fillLevel"));
        Assert.Empty(view.Accepted);
        Assert.Single(view.Defeated);
        Assert.Equal(s.AidA1, view.Defeated[0].Aid);
        Assert.Equal(s.AidD1, view.Defeated[0].DefeatedBy);
        Assert.Equal("decision:retracts", view.Defeated[0].DefeatReason);
    }

    [Fact]
    public void Current_A2_accepted_but_stale_and_why_cites_D1()
    {
        using var path = new TempFile();
        using var s = BuildLedger(path.Path);
        var view = s.L.Ask("Current", new Question(Subject: "Silo17", Predicate: "massEstimate"));
        Assert.Single(view.Accepted);
        Assert.Equal(s.AidA2, view.Accepted[0].Aid);
        Assert.True(view.Accepted[0].StaleViaJustification);

        var why = s.L.Why("Current", s.AidA2);
        Assert.Equal("accepted", why.Status);
        var a1Input = Assert.Single(why.Inputs, i => i.Aid == s.AidA1);
        Assert.Equal("defeated", a1Input.Status);
        Assert.Equal(s.AidD1, a1Input.DefeatedBy);
        Assert.Contains(s.AidD1, a1Input.Decisions);
    }

    [Fact]
    public void Current_A3_accepted()
    {
        using var path = new TempFile();
        using var s = BuildLedger(path.Path);
        var view = s.L.Ask("Current", new Question(Subject: "Silo18", Predicate: "fillLevel"));
        Assert.Single(view.Accepted);
        Assert.Equal(s.AidA3, view.Accepted[0].Aid);
        Assert.Null(view.Missing);
    }

    [Fact]
    public void Backtest_A1_accepted_D1_not_visible()
    {
        using var path = new TempFile();
        using var s = BuildLedger(path.Path);
        var view = s.L.Ask("Backtest", new Question(Subject: "Silo17", Predicate: "fillLevel"));
        Assert.Single(view.Accepted);
        Assert.Equal(s.AidA1, view.Accepted[0].Aid);
        Assert.Empty(view.Defeated);
    }

    [Fact]
    public void Backtest_A2_accepted()
    {
        using var path = new TempFile();
        using var s = BuildLedger(path.Path);
        var view = s.L.Ask("Backtest", new Question(Subject: "Silo17", Predicate: "massEstimate"));
        Assert.Single(view.Accepted);
        Assert.Equal(s.AidA2, view.Accepted[0].Aid);
        Assert.False(view.Accepted[0].StaleViaJustification);
    }

    [Fact]
    public void Backtest_A3_not_visible()
    {
        using var path = new TempFile();
        using var s = BuildLedger(path.Path);
        var view = s.L.Ask("Backtest", new Question(Subject: "Silo18", Predicate: "fillLevel"));
        Assert.Empty(view.Accepted);
        Assert.Empty(view.Defeated);
        Assert.NotNull(view.Missing);
    }
}
