using Gneiss.Cell;

namespace Gneiss.Cell.Tests;

/// <summary>
/// CONTRACT.md section 6, test 7: source-precedence win; confidence win; StopRung=2 tie -> Contested
/// surfaced; numberTol comparator treats 4.20000000001 vs 4.2 as compatible (no conflict).
/// </summary>
public sealed class Test7_Strainers
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-07-01T00:00:00Z");

    [Fact]
    public void SourcePrecedence_Wins()
    {
        using var path = new TempFile();
        using var l = GneissLedger.Create(path.Path);

        l.DeclarePredicate(TestHelpers.Env("x", "declare", T0), new PredicateDecl("temp", SourcePrecedence: new[] { "radar", "manual" }));

        var txManual = l.Append(TestHelpers.Env("x", "manual reading", T0), new IAppendItem[]
        {
            new NewAssertion("SensorA", "temp", GValue.Text("hot"), Source: "manual"),
        }).Value;
        var txRadar = l.Append(TestHelpers.Env("x", "radar reading", T0), new IAppendItem[]
        {
            new NewAssertion("SensorA", "temp", GValue.Text("cold"), Source: "radar"),
        }).Value;

        var aidManual = TestHelpers.FindAid(l, txManual, "SensorA", "temp");
        var aidRadar = TestHelpers.FindAid(l, txRadar, "SensorA", "temp");

        l.DeclareContext(TestHelpers.Env("x", "ctx", T0), new ContextDecl("SP"));
        var view = l.Ask("SP", new Question(Subject: "SensorA", Predicate: "temp"));

        Assert.Single(view.Accepted);
        Assert.Equal(aidRadar, view.Accepted[0].Aid);
        Assert.Single(view.Defeated);
        Assert.Equal(aidManual, view.Defeated[0].Aid);
        Assert.Equal(aidRadar, view.Defeated[0].DefeatedBy);
        Assert.Equal("conflict:rung2", view.Defeated[0].DefeatReason);
        Assert.Empty(view.Contested);
    }

    [Fact]
    public void Confidence_Wins_When_Source_Precedence_Does_Not_Discriminate()
    {
        using var path = new TempFile();
        using var l = GneissLedger.Create(path.Path);

        var txHigh = l.Append(TestHelpers.Env("x", "r1", T0), new IAppendItem[]
        {
            new NewAssertion("SensorB", "score", GValue.Number(1m), ConfidenceBp: 9000),
        }).Value;
        var txLow = l.Append(TestHelpers.Env("x", "r2", T0), new IAppendItem[]
        {
            new NewAssertion("SensorB", "score", GValue.Number(2m), ConfidenceBp: 5000),
        }).Value;

        var aidHigh = TestHelpers.FindAid(l, txHigh, "SensorB", "score");
        var aidLow = TestHelpers.FindAid(l, txLow, "SensorB", "score");

        l.DeclareContext(TestHelpers.Env("x", "ctx", T0), new ContextDecl("Conf"));
        var view = l.Ask("Conf", new Question(Subject: "SensorB", Predicate: "score"));

        Assert.Single(view.Accepted);
        Assert.Equal(aidHigh, view.Accepted[0].Aid);
        Assert.Single(view.Defeated);
        Assert.Equal(aidLow, view.Defeated[0].Aid);
        Assert.Equal("conflict:rung5", view.Defeated[0].DefeatReason);
    }

    [Fact]
    public void StopRung2_Tie_Surfaces_As_Contested()
    {
        using var path = new TempFile();
        using var l = GneissLedger.Create(path.Path);

        l.DeclarePredicate(TestHelpers.Env("x", "declare", T0), new PredicateDecl("flag", StopRung: 2));

        var tx1 = l.Append(TestHelpers.Env("x", "r1", T0), new IAppendItem[]
        {
            new NewAssertion("SensorC", "flag", GValue.Bool(true)),
        }).Value;
        var tx2 = l.Append(TestHelpers.Env("x", "r2", T0), new IAppendItem[]
        {
            new NewAssertion("SensorC", "flag", GValue.Bool(false)),
        }).Value;

        var aid1 = TestHelpers.FindAid(l, tx1, "SensorC", "flag");
        var aid2 = TestHelpers.FindAid(l, tx2, "SensorC", "flag");

        l.DeclareContext(TestHelpers.Env("x", "ctx", T0), new ContextDecl("StopEarly"));
        var view = l.Ask("StopEarly", new Question(Subject: "SensorC", Predicate: "flag"));

        Assert.Empty(view.Accepted);
        Assert.Empty(view.Defeated);
        var group = Assert.Single(view.Contested);
        Assert.Equal(2, group.StoppedAtRung);
        Assert.Contains(aid1, group.Aids);
        Assert.Contains(aid2, group.Aids);
    }

    [Fact]
    public void NumberTol_Treats_Near_Equal_Values_As_Compatible()
    {
        using var path = new TempFile();
        using var l = GneissLedger.Create(path.Path);

        l.DeclarePredicate(TestHelpers.Env("x", "declare", T0), new PredicateDecl("weight2", Comparator: "numberTol", TolAbs: 0.001m));

        var tx1 = l.Append(TestHelpers.Env("x", "r1", T0), new IAppendItem[]
        {
            new NewAssertion("SensorD", "weight2", GValue.Number(4.2m), Source: "a"),
        }).Value;
        var tx2 = l.Append(TestHelpers.Env("x", "r2", T0), new IAppendItem[]
        {
            new NewAssertion("SensorD", "weight2", GValue.Number(4.20000000001m), Source: "b"),
        }).Value;

        var aid1 = TestHelpers.FindAid(l, tx1, "SensorD", "weight2");
        var aid2 = TestHelpers.FindAid(l, tx2, "SensorD", "weight2");

        l.DeclareContext(TestHelpers.Env("x", "ctx", T0), new ContextDecl("Tol"));
        var view = l.Ask("Tol", new Question(Subject: "SensorD", Predicate: "weight2"));

        Assert.Equal(2, view.Accepted.Count);
        Assert.Contains(view.Accepted, e => e.Aid == aid1);
        Assert.Contains(view.Accepted, e => e.Aid == aid2);
        Assert.Empty(view.Defeated);
        Assert.Empty(view.Contested);
    }
}
