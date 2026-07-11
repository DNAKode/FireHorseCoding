using KodePorter.Core.Gneiss;
using KodePorter.Core.Tests.Support;

namespace KodePorter.Core.Tests;

/// <summary>CONTRACT.md §10 test 5: promotion + the autonomy dial.</summary>
public class GneissBindingTests
{
    [Fact]
    public void ProposedBehaviorClaimIsInvisibleInDecidedOnlyViewUntilDecided()
    {
        using var dir = new TempDirectory();
        using var binding = GneissBinding.Initialize(dir.Path);

        string anchorAid = binding.PromoteAnchor(
            new AnchorEvidenceValue("headscan::parse", "d1", "aaaa", "src/lib.rs", 8, 40),
            actor: "kodeporter", reason: "map import");

        string claimAid = binding.ProposeBehaviorClaim(
            "unit-parse", "B1", "Parses a header line into tokens.", [anchorAid],
            actor: "kodeporter", reason: "generated from anchors");

        var view = binding.AskClaim(GneissBinding.BehaviorSubject("unit-parse", "B1"));
        Assert.Empty(view.Accepted);
        Assert.Empty(view.Defeated);

        binding.HumanDecide(claimAid, KpVerdict.Accept, reason: "looks right", actor: "govert");

        var afterDecide = binding.AskClaim(GneissBinding.BehaviorSubject("unit-parse", "B1"));
        var accepted = Assert.Single(afterDecide.Accepted);
        Assert.Equal("govert", GetDecisionActor(binding, claimAid));
        Assert.Equal(claimAid, accepted.Aid);
    }

    [Fact]
    public void VerificationClaimGreenAndPolicyAllowsAutoAcceptsWithPolicyActorAndZeroHumanDecisions()
    {
        using var dir = new TempDirectory();
        using var binding = GneissBinding.Initialize(dir.Path);

        var value = new VerificationClaimValue("pass", "corpushash", "sourcebasis", "targetbasis", 3, [], "runs/verify-x.json");
        string claimAid = binding.ProposeVerificationClaim("unit-parse", "io-agreement-v1", value, evidenceAids: null,
            actor: "kodeporter", reason: "verify run");

        // Before any decision: invisible in the decided-only view.
        var before = binding.AskClaim(GneissBinding.VerificationSubject("unit-parse", "io-agreement-v1"));
        Assert.Empty(before.Accepted);

        binding.PolicyAutoAccept(claimAid, "kp-default", "1", reason: "verification green, policy allows kpVerification");

        var after = binding.AskClaim(GneissBinding.VerificationSubject("unit-parse", "io-agreement-v1"));
        var accepted = Assert.Single(after.Accepted);
        Assert.Equal(claimAid, accepted.Aid);
        Assert.Equal("policy:kp-default@1", GetDecisionActor(binding, claimAid));
    }

    [Fact]
    public void FailedVerificationClaimIsRecordedButNeverAutoAcceptedAsPass()
    {
        using var dir = new TempDirectory();
        using var binding = GneissBinding.Initialize(dir.Path);

        var value = new VerificationClaimValue("fail", "corpushash", "sourcebasis", "targetbasis", 3, ["case-2"], "runs/verify-x.json");
        string claimAid = binding.ProposeVerificationClaim("unit-parse", "io-agreement-v1", value, evidenceAids: null,
            actor: "kodeporter", reason: "verify run");

        // A fail verdict is never auto-accepted — no PolicyAutoAccept call happens for it.
        var view = binding.AskClaim(GneissBinding.VerificationSubject("unit-parse", "io-agreement-v1"));
        Assert.Empty(view.Accepted);
        Assert.Empty(view.Defeated);

        // Explicitly proving the claim exists in the raw ledger (recorded, just not admitted).
        var explanation = binding.Why(claimAid);
        Assert.Equal("proposed-unadmitted", explanation.Status);
    }

    [Fact]
    public void HumanDecideRejectDefeatsAnAlreadyAdmittedClaim()
    {
        // A reject on a *proposed, never-accepted* claim leaves it NotAdmitted (it was never a
        // visible candidate to defeat — Gneiss's own admission rule, kb/22 §2 step 4). Defeat is
        // observable once the target was admitted in the first place; anchors are FACTS, admitted
        // unconditionally, so rejecting one is the clean way to exercise a genuine defeat here.
        using var dir = new TempDirectory();
        using var binding = GneissBinding.Initialize(dir.Path);

        string anchorAid = binding.PromoteAnchor(
            new AnchorEvidenceValue("headscan::parse", "d1", "aaaa", "src/lib.rs", 8, 40),
            actor: "kodeporter", reason: "map import");

        var beforeReject = binding.AskClaim(GneissBinding.AnchorSubject("headscan::parse", "d1"));
        Assert.Single(beforeReject.Accepted);

        binding.HumanDecide(anchorAid, KpVerdict.Reject, reason: "mis-imported", actor: "govert");

        var view = binding.AskClaim(GneissBinding.AnchorSubject("headscan::parse", "d1"));
        Assert.Empty(view.Accepted);
        var defeated = Assert.Single(view.Defeated);
        Assert.Equal(anchorAid, defeated.Aid);
    }

    [Fact]
    public void HumanDecideRejectOnAnUndecidedProposedClaimLeavesItNotAdmittedNotDefeated()
    {
        // Documents the (correct, non-obvious) Gneiss admission rule directly: rejecting a
        // proposed claim that was never accepted does not produce a "Defeated" entry — it simply
        // stays out of the decided-only view (NotAdmitted), same as before the reject.
        using var dir = new TempDirectory();
        using var binding = GneissBinding.Initialize(dir.Path);

        string claimAid = binding.ProposeBehaviorClaim("unit-x", "B1", "sentence", [], actor: "kodeporter", reason: "generated");
        binding.HumanDecide(claimAid, KpVerdict.Reject, reason: "wrong", actor: "govert");

        var view = binding.AskClaim(GneissBinding.BehaviorSubject("unit-x", "B1"));
        Assert.Empty(view.Accepted);
        Assert.Empty(view.Defeated);

        var explanation = binding.Why(claimAid);
        Assert.Equal("proposed-unadmitted", explanation.Status);
    }

    [Fact]
    public void HumanDecideRequiresNonEmptyReason()
    {
        using var dir = new TempDirectory();
        using var binding = GneissBinding.Initialize(dir.Path);
        string claimAid = binding.ProposeBehaviorClaim("unit-x", "B1", "sentence", [], actor: "kodeporter", reason: "generated");

        Assert.Throws<ArgumentException>(() => binding.HumanDecide(claimAid, KpVerdict.Accept, reason: "", actor: "govert"));
    }

    /// <summary>
    /// Gneiss facade v0.1 equivalence (CONTRACT-V01.md; CONTRACT-M15.md §4): the aid a proposal
    /// method returns (now sourced directly from Append's AppendResult, no more export-scan
    /// recovery) round-trips through GetAssertion to the same subject/predicate/value the caller
    /// asked for.
    /// </summary>
    [Fact]
    public void ProposedClaimAidFromAppendResultMatchesGetAssertionRoundTrip()
    {
        using var dir = new TempDirectory();
        using var binding = GneissBinding.Initialize(dir.Path);

        string claimAid = binding.ProposeBehaviorClaim(
            "unit-parse", "B1", "Parses a header line into tokens.", [],
            actor: "kodeporter", reason: "generated from anchors");

        var info = binding.GetAssertion(claimAid);
        Assert.NotNull(info);
        Assert.Equal(claimAid, info!.Aid);
        Assert.Equal(GneissBinding.BehaviorSubject("unit-parse", "B1"), info.Subject);
        Assert.Equal(GneissBinding.PredBehavior, info.Predicate);
        Assert.Equal("text", info.Value.Kind);
        Assert.Equal("Parses a header line into tokens.", info.Value.Canonical);
        Assert.Equal("proposed", info.Status);

        // A second, unrelated proposal gets a different aid, and GetAssertion of an aid that was
        // never returned by Append is null (never confused with a different assertion).
        string anchorAid = binding.PromoteAnchor(
            new AnchorEvidenceValue("headscan::parse", "d1", "aaaa", "src/lib.rs", 8, 40),
            actor: "kodeporter", reason: "map import");
        Assert.NotEqual(claimAid, anchorAid);
        Assert.Null(binding.GetAssertion("not-a-real-aid"));
    }

    /// <summary>CONTRACT-M15.md §3: `Note`/`ListNotes` — the two-tier capture inbox. Notes are
    /// listed oldest first and start unpromoted.</summary>
    [Fact]
    public void NoteIsRecordedAndListedUnpromoted()
    {
        using var dir = new TempDirectory();
        using var binding = GneissBinding.Initialize(dir.Path);

        Assert.Empty(binding.ListNotes());

        string id1 = binding.Note("first observation", "govert", "kp note");
        string id2 = binding.Note("second observation", "fable", "kp note");

        var notes = binding.ListNotes();
        Assert.Equal(2, notes.Count);
        Assert.Equal([id1, id2], notes.Select(n => n.Id));
        Assert.Equal("first observation", notes[0].Text);
        Assert.Equal("govert", notes[0].Actor);
        Assert.Null(notes[0].PromotedAid);
        Assert.Equal("second observation", notes[1].Text);
        Assert.Equal("fable", notes[1].Actor);
    }

    [Fact]
    public void InitializeIsIdempotentAndOpenReusesTheSameLedger()
    {
        using var dir = new TempDirectory();
        using (var first = GneissBinding.Initialize(dir.Path))
        {
            first.PromoteAnchor(new AnchorEvidenceValue("a", "b", "c", "f", 1, 2), "kodeporter", "seed");
        }

        using var reopened = GneissBinding.Initialize(dir.Path);
        var view = reopened.AskClaim(GneissBinding.AnchorSubject("a", "b"));
        Assert.Single(view.Accepted); // facts are admitted unconditionally
    }

    /// <summary>
    /// The actor who decided a claim, cross-checked directly from the ledger export (a decision's
    /// assrt row carries the tx; that tx row carries the actor — CONTRACT.md §1/§2).
    /// </summary>
    private static string GetDecisionActor(GneissBinding binding, string claimAid)
    {
        var explanation = binding.Why(claimAid);
        string decisionAid = Assert.Single(explanation.Decisions);

        var lines = binding.ExportLedgerJsonl().ToList();
        long? decisionTx = null;
        foreach (var line in lines)
        {
            if (line.Contains("\"kind\":\"assrt\"") && line.Contains($"\"aid\":\"{decisionAid}\""))
                decisionTx = ExtractTx(line);
        }
        Assert.NotNull(decisionTx);

        foreach (var line in lines)
        {
            if (line.Contains("\"kind\":\"tx\"") && line.Contains($"\"id\":{decisionTx}"))
                return ExtractActor(line);
        }
        throw new InvalidOperationException("tx row not found for decision.");
    }

    private static long ExtractTx(string assrtJsonLine)
    {
        const string marker = "\"tx\":";
        int idx = assrtJsonLine.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        int end = idx;
        while (end < assrtJsonLine.Length && char.IsDigit(assrtJsonLine[end])) end++;
        return long.Parse(assrtJsonLine[idx..end]);
    }

    private static string ExtractActor(string txJsonLine)
    {
        const string marker = "\"actor\":\"";
        int idx = txJsonLine.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        int end = txJsonLine.IndexOf('"', idx);
        return txJsonLine[idx..end];
    }
}
