using Gneiss.Cell;

namespace Gneiss.Cell.Tests;

/// <summary>
/// CONTRACT.md section 6, test 3: propose sameAs hypotheses with ckeys; accepts by ckey; append NEW
/// proposed assertions with same ckeys, different aids/method (the "matcher rebuild"); view accepts
/// the new ones by the old decision; a rejects-by-ckey keeps rejecting regenerated hypotheses.
/// </summary>
public sealed class Test3_DecisionSurvival
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-07-01T00:00:00Z");

    [Fact]
    public void AcceptsByCKey_Survives_Matcher_Rebuild()
    {
        using var path = new TempFile();
        using var l = GneissLedger.Create(path.Path);

        var txH1 = l.Append(TestHelpers.Env("matcher", "propose link v1", T0), new IAppendItem[]
        {
            new NewAssertion("EntityX", "sameAs", GValue.Entity("EntityY"), Proposed: true, Method: "matcherV1", ConfidenceBp: 8000),
        }).Tx.Value;
        var ckey = TestHelpers.FindCKey(l, txH1, "EntityX", "sameAs");
        var aidH1 = TestHelpers.FindAid(l, txH1, "EntityX", "sameAs");

        l.Append(TestHelpers.Env("reviewer", "accept the link", T0), new IAppendItem[]
        {
            new NewDecision(DecisionKind.Accepts, TargetClaimKey: ckey),
        });

        l.DeclareContext(TestHelpers.Env("s", "ctx", T0), new ContextDecl("Survival"));

        var view1 = l.Ask("Survival", new Question(ClaimKey: ckey));
        Assert.Single(view1.Accepted);
        Assert.Equal(aidH1, view1.Accepted[0].Aid);

        // matcher rebuild: same ckey (same subj/pred/vfrom/vto), new method, new aid.
        var txH2 = l.Append(TestHelpers.Env("matcher", "propose link v2", T0), new IAppendItem[]
        {
            new NewAssertion("EntityX", "sameAs", GValue.Entity("EntityY"), Proposed: true, Method: "matcherV2", ConfidenceBp: 7000),
        }).Tx.Value;
        var aidH2 = TestHelpers.FindAid(l, txH2, "EntityX", "sameAs");
        Assert.NotEqual(aidH1, aidH2);
        Assert.Equal(ckey, TestHelpers.FindCKey(l, txH2, "EntityX", "sameAs"));

        var view2 = l.Ask("Survival", new Question(ClaimKey: ckey));
        Assert.Equal(2, view2.Accepted.Count);
        Assert.Contains(view2.Accepted, e => e.Aid == aidH1);
        Assert.Contains(view2.Accepted, e => e.Aid == aidH2);
    }

    [Fact]
    public void RejectsByCKey_Keeps_Rejecting_Regenerated_Hypotheses()
    {
        using var path = new TempFile();
        using var l = GneissLedger.Create(path.Path);

        var txH3 = l.Append(TestHelpers.Env("matcher", "propose link v1", T0), new IAppendItem[]
        {
            new NewAssertion("EntityZ", "sameAs", GValue.Entity("EntityW"), Proposed: true, Method: "matcherV1", ConfidenceBp: 9000),
        }).Tx.Value;
        var ckey = TestHelpers.FindCKey(l, txH3, "EntityZ", "sameAs");
        var aidH3 = TestHelpers.FindAid(l, txH3, "EntityZ", "sameAs");

        l.Append(TestHelpers.Env("reviewer", "reject the link", T0), new IAppendItem[]
        {
            new NewDecision(DecisionKind.Rejects, TargetClaimKey: ckey),
        });

        l.DeclareContext(TestHelpers.Env("s", "ctx", T0), new ContextDecl("RejSurvival", Admit: "threshold", AdmitThresholdBp: 5000));

        var view1 = l.Ask("RejSurvival", new Question(ClaimKey: ckey));
        Assert.Empty(view1.Accepted);
        Assert.Single(view1.Defeated);
        Assert.Equal(aidH3, view1.Defeated[0].Aid);
        Assert.Equal("decision:rejects", view1.Defeated[0].DefeatReason);
        Assert.True(view1.Defeated[0].AutoAdmitted);

        var txH4 = l.Append(TestHelpers.Env("matcher", "propose link v2", T0), new IAppendItem[]
        {
            new NewAssertion("EntityZ", "sameAs", GValue.Entity("EntityW"), Proposed: true, Method: "matcherV2", ConfidenceBp: 9500),
        }).Tx.Value;
        var aidH4 = TestHelpers.FindAid(l, txH4, "EntityZ", "sameAs");

        var view2 = l.Ask("RejSurvival", new Question(ClaimKey: ckey));
        Assert.Empty(view2.Accepted);
        Assert.Equal(2, view2.Defeated.Count);
        Assert.Contains(view2.Defeated, e => e.Aid == aidH3 && e.DefeatReason == "decision:rejects");
        Assert.Contains(view2.Defeated, e => e.Aid == aidH4 && e.DefeatReason == "decision:rejects");
    }
}
