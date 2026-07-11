using Gneiss.Cell;

namespace Gneiss.Cell.Tests;

/// <summary>
/// kb defect 1 (BLOCKER): conflict resolution must be grounded PAIRWISE semantics, not
/// "run the whole connected component through the strainer as one N-way contest". A connected
/// component can be joined by a CHAIN of pairwise edges even though some members never conflict
/// with each other directly (X-Y-Z where X and Z are disjoint) -- those members must never end up
/// Defeated by (or Contested with) an assertion they don't actually conflict with.
/// </summary>
public sealed class Test11_GroundedConflictResolution
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-07-01T00:00:00Z");

    /// <summary>
    /// X=1@[0,10), Y=2@[5,15), Z=1@[12,20): X and Z never overlap and share a value (no edge between
    /// them), yet the old whole-component strainer wrongly defeated BOTH X and Y "by" Z. Grounded
    /// pairwise semantics: X beats Y at rung6 (later tx), Z beats Y at rung6 (later tx); X and Z have
    /// no attackers, so both are Accepted; Y has two accepted attackers, deterministically resolved to
    /// the later (tx, aid) -- Z.
    /// </summary>
    [Fact]
    public void Disjoint_Chain_Ends_Never_Defeat_Each_Other_Transitively()
    {
        using var path = new TempFile();
        using var l = GneissLedger.Create(path.Path);

        // append order fixes tx order: Y first (earliest tx), then X, then Z (latest tx).
        var txY = l.Append(TestHelpers.Env("x", "y", T0), new IAppendItem[]
        {
            new NewAssertion("Chain1", "reading", GValue.Number(2m), ValidFrom: T0.AddHours(5), ValidTo: T0.AddHours(15)),
        }).Tx.Value;
        var txX = l.Append(TestHelpers.Env("x", "x", T0), new IAppendItem[]
        {
            new NewAssertion("Chain1", "reading", GValue.Number(1m), ValidFrom: T0.AddHours(0), ValidTo: T0.AddHours(10)),
        }).Tx.Value;
        var txZ = l.Append(TestHelpers.Env("x", "z", T0), new IAppendItem[]
        {
            new NewAssertion("Chain1", "reading", GValue.Number(1m), ValidFrom: T0.AddHours(12), ValidTo: T0.AddHours(20)),
        }).Tx.Value;

        var aidY = TestHelpers.FindAid(l, txY, "Chain1", "reading");
        var aidX = TestHelpers.FindAid(l, txX, "Chain1", "reading");
        var aidZ = TestHelpers.FindAid(l, txZ, "Chain1", "reading");

        l.DeclareContext(TestHelpers.Env("x", "ctx", T0), new ContextDecl("Chain1Ctx"));
        var view = l.Ask("Chain1Ctx", new Question(Subject: "Chain1", Predicate: "reading"));

        Assert.Equal(2, view.Accepted.Count);
        Assert.Contains(view.Accepted, e => e.Aid == aidX);
        Assert.Contains(view.Accepted, e => e.Aid == aidZ);

        var defeated = Assert.Single(view.Defeated);
        Assert.Equal(aidY, defeated.Aid);
        Assert.Equal(aidZ, defeated.DefeatedBy);
        Assert.Equal("conflict:rung6", defeated.DefeatReason);

        Assert.Empty(view.Contested);
    }

    /// <summary>
    /// A groundedness chain where rung-2 source precedence reverses tx order: A(strong, tx1)@[0,10)
    /// conflicts B(weak, tx2)@[5,15) conflicts C(weak, tx3)@[12,20); A vs C disjoint. A beats B at
    /// rung2 (source precedence); C beats B at rung6 (later tx, since both are "weak"-ranked so rung2
    /// ties them); B is defeated (deterministically attributed to C, the later of its two accepted
    /// attackers); A and C are both Accepted (grounded fixpoint: neither has an attacker until B's own
    /// status is settled, and B never attacks anyone).
    /// </summary>
    [Fact]
    public void SourcePrecedence_And_Recency_Both_Contribute_Without_Chaining_Defeats()
    {
        using var path = new TempFile();
        using var l = GneissLedger.Create(path.Path);

        l.DeclarePredicate(TestHelpers.Env("x", "declare", T0), new PredicateDecl("chain2", SourcePrecedence: new[] { "strong" }));

        var txA = l.Append(TestHelpers.Env("x", "a", T0), new IAppendItem[]
        {
            new NewAssertion("Chain2", "chain2", GValue.Text("a"), ValidFrom: T0.AddHours(0), ValidTo: T0.AddHours(10), Source: "strong"),
        }).Tx.Value;
        var txB = l.Append(TestHelpers.Env("x", "b", T0), new IAppendItem[]
        {
            new NewAssertion("Chain2", "chain2", GValue.Text("b"), ValidFrom: T0.AddHours(5), ValidTo: T0.AddHours(15), Source: "weak"),
        }).Tx.Value;
        var txC = l.Append(TestHelpers.Env("x", "c", T0), new IAppendItem[]
        {
            new NewAssertion("Chain2", "chain2", GValue.Text("c"), ValidFrom: T0.AddHours(12), ValidTo: T0.AddHours(20), Source: "weak"),
        }).Tx.Value;

        var aidA = TestHelpers.FindAid(l, txA, "Chain2", "chain2");
        var aidB = TestHelpers.FindAid(l, txB, "Chain2", "chain2");
        var aidC = TestHelpers.FindAid(l, txC, "Chain2", "chain2");

        l.DeclareContext(TestHelpers.Env("x", "ctx", T0), new ContextDecl("Chain2Ctx"));
        var view = l.Ask("Chain2Ctx", new Question(Subject: "Chain2", Predicate: "chain2"));

        Assert.Equal(2, view.Accepted.Count);
        Assert.Contains(view.Accepted, e => e.Aid == aidA);
        Assert.Contains(view.Accepted, e => e.Aid == aidC);

        var defeated = Assert.Single(view.Defeated);
        Assert.Equal(aidB, defeated.Aid);
        Assert.Equal(aidC, defeated.DefeatedBy);
        Assert.Equal("conflict:rung6", defeated.DefeatReason);

        Assert.Empty(view.Contested);
    }

    /// <summary>
    /// A StopRung=2 unresolved pair inside a 3-node chain surfaces Contested without wrongly defeating
    /// the third node. A(strong,tx1)@[0,10) and B(strong,tx2)@[5,15) tie at rung2 (same source rank) --
    /// unresolved, since StopRung=2 leaves no further rung to break the tie. B(strong) conflicts
    /// C(null-source,tx3)@[12,20): B pairwise beats C at rung2. A,C are disjoint (no edge). Old
    /// whole-component code ran the 3-way strainer together, ranked C last, and defeated it outright
    /// (with no defeater attributed) even though C never conflicts with A. Grounded pairwise semantics:
    /// B's own standing is never settled (permanently entangled with A's unresolved rivalry), so B can
    /// never stand as an ACCEPTED attacker against C -- C is not defeated, it joins the Contested group.
    /// </summary>
    [Fact]
    public void StopRung2_Unresolved_Pair_Does_Not_Defeat_The_Third_Chain_Node()
    {
        using var path = new TempFile();
        using var l = GneissLedger.Create(path.Path);

        l.DeclarePredicate(TestHelpers.Env("x", "declare", T0), new PredicateDecl("chain3", StopRung: 2, SourcePrecedence: new[] { "strong" }));

        var txA = l.Append(TestHelpers.Env("x", "a", T0), new IAppendItem[]
        {
            new NewAssertion("Chain3", "chain3", GValue.Text("a"), ValidFrom: T0.AddHours(0), ValidTo: T0.AddHours(10), Source: "strong"),
        }).Tx.Value;
        var txB = l.Append(TestHelpers.Env("x", "b", T0), new IAppendItem[]
        {
            new NewAssertion("Chain3", "chain3", GValue.Text("b"), ValidFrom: T0.AddHours(5), ValidTo: T0.AddHours(15), Source: "strong"),
        }).Tx.Value;
        var txC = l.Append(TestHelpers.Env("x", "c", T0), new IAppendItem[]
        {
            new NewAssertion("Chain3", "chain3", GValue.Text("c"), ValidFrom: T0.AddHours(12), ValidTo: T0.AddHours(20), Source: null),
        }).Tx.Value;

        var aidA = TestHelpers.FindAid(l, txA, "Chain3", "chain3");
        var aidB = TestHelpers.FindAid(l, txB, "Chain3", "chain3");
        var aidC = TestHelpers.FindAid(l, txC, "Chain3", "chain3");

        l.DeclareContext(TestHelpers.Env("x", "ctx", T0), new ContextDecl("Chain3Ctx"));
        var view = l.Ask("Chain3Ctx", new Question(Subject: "Chain3", Predicate: "chain3"));

        Assert.Empty(view.Accepted);
        // Property: the third node (C) must never be defeated by a claim (B) whose own standing was
        // never actually settled.
        Assert.DoesNotContain(view.Defeated, e => e.Aid == aidC);
        Assert.Empty(view.Defeated);

        var group = Assert.Single(view.Contested);
        Assert.Equal(2, group.StoppedAtRung);
        Assert.Contains(aidA, group.Aids);
        Assert.Contains(aidB, group.Aids);
        Assert.Contains(aidC, group.Aids);
    }
}
