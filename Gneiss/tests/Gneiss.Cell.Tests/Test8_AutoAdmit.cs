using Gneiss.Cell;

namespace Gneiss.Cell.Tests;

/// <summary>
/// CONTRACT.md section 6, test 8: a threshold context admits a conf 9000bp proposed assertion with
/// AutoAdmitted = true (kb/22 §8 Q1: always badged); a decided-only context leaves it out of Accepted.
/// </summary>
public sealed class Test8_AutoAdmit
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-07-01T00:00:00Z");

    [Fact]
    public void Threshold_Context_AutoAdmits_And_Badges_HighConfidence_Proposal()
    {
        using var path = new TempFile();
        using var l = GneissLedger.Create(path.Path);

        var tx = l.Append(TestHelpers.Env("agent", "propose risk flag", T0), new IAppendItem[]
        {
            new NewAssertion("X", "risk", GValue.Text("high"), Proposed: true, ConfidenceBp: 9000),
        }).Value;
        var aid = TestHelpers.FindAid(l, tx, "X", "risk");

        l.DeclareContext(TestHelpers.Env("s", "ctx1", T0), new ContextDecl("Threshold", Admit: "threshold", AdmitThresholdBp: 8000));
        l.DeclareContext(TestHelpers.Env("s", "ctx2", T0), new ContextDecl("Decided", Admit: "decided-only"));

        var thresholdView = l.Ask("Threshold", new Question(Subject: "X", Predicate: "risk"));
        Assert.Single(thresholdView.Accepted);
        Assert.Equal(aid, thresholdView.Accepted[0].Aid);
        Assert.True(thresholdView.Accepted[0].AutoAdmitted);

        var decidedView = l.Ask("Decided", new Question(Subject: "X", Predicate: "risk"));
        Assert.Empty(decidedView.Accepted);
        Assert.Empty(decidedView.Defeated);
        Assert.Null(decidedView.Missing); // it IS visible, just not admitted -- not a zero-match case.
    }
}
