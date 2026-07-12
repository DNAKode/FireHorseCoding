using System.Text.Json;
using Gneiss.Cell;
using KodePorter.Core.Hashing;

namespace KodePorter.Core.Gneiss;

/// <summary>Accept or reject, per <c>kp decide --verdict accept|reject</c> (CONTRACT.md §9).</summary>
public enum KpVerdict
{
    Accept,
    Reject,
}

/// <summary>
/// KodePorter's binding to a workspace's gneiss.db ledger (CONTRACT.md §5): declares the kp.*
/// predicates and the kp-current context at init, and exposes promotion/proposal/decision
/// operations plus the current-belief view. Consumes Gneiss exclusively through Gneiss.Cell's
/// public facade (charter §4.2 sidecar boundary) — no Gneiss internals (its C# `internal` types),
/// and no side-channel database connections, are ever referenced.
/// </summary>
public sealed class GneissBinding : IDisposable
{
    public const string ContextName = "kp-current";

    public const string PredBehavior = "kp.behavior";
    public const string PredEvidenceAnchor = "kp.evidence.anchor";
    public const string PredCorrespondence = "kp.correspondence";
    public const string PredVerification = "kp.verification";
    public const string PredStale = "kp.stale";

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    private readonly GneissLedger _ledger;

    private GneissBinding(GneissLedger ledger)
    {
        _ledger = ledger;
    }

    public static string LedgerPath(string workspaceDir) => Path.Combine(workspaceDir, "gneiss.db");

    /// <summary>
    /// Opens the workspace's ledger, creating it (and declaring the kp.* predicates + kp-current
    /// context) if it does not already exist.
    /// </summary>
    public static GneissBinding Initialize(string workspaceDir)
    {
        string path = LedgerPath(workspaceDir);
        bool isNew = !File.Exists(path);
        var ledger = isNew ? GneissLedger.Create(path) : GneissLedger.Open(path);
        var binding = new GneissBinding(ledger);
        if (isNew)
            binding.DeclarePredicatesAndContext();
        return binding;
    }

    /// <summary>Opens an existing workspace ledger. Throws if it has not been initialized.</summary>
    public static GneissBinding Open(string workspaceDir)
    {
        string path = LedgerPath(workspaceDir);
        return new GneissBinding(GneissLedger.Open(path));
    }

    private void DeclarePredicatesAndContext()
    {
        var env = SystemEnvelope("kp init: declare kp.* predicates and the kp-current context");
        foreach (string pred in new[] { PredBehavior, PredEvidenceAnchor, PredCorrespondence, PredVerification, PredStale })
        {
            _ledger.DeclarePredicate(env, new PredicateDecl(pred, Comparator: "exact", StopRung: 6));
        }
        _ledger.DeclareContext(env, new ContextDecl(ContextName, Admit: "decided-only"));
    }

    private static TxEnvelope SystemEnvelope(string reason) => new("kodeporter", reason, DateTimeOffset.UtcNow);

    // ---- Subjects (CONTRACT.md §5) ---------------------------------------------------------

    public static string UnitSubject(string unitId) => $"unit:{unitId}";

    /// <summary>
    /// Per-claim subject for kp.behavior. Each behavior claim is an independently disputable
    /// judgment (the granularity rule), so each gets its own subject and therefore its own
    /// Gneiss claim key. A shared unit-level subject would make all of a unit's behavior claims
    /// one claim key, turning them into conflicting values where accepting a later one silently
    /// defeats an earlier one at strainer rung 6 — found during M1 story integration, 2026-07-10.
    /// </summary>
    public static string BehaviorSubject(string unitId, string claimId) => $"behavior:{unitId}:{claimId}";

    public static string BehaviorSubjectPrefix(string unitId) => $"behavior:{unitId}:";

    public static string CorrespondenceSubject(string corrId) => $"corr:{corrId}";

    public static string VerificationSubject(string unitId, string criterion) => $"verify:{unitId}:{criterion}";

    public static string AnchorSubject(string symbolPath, string basisLabel) =>
        $"anchor:{Sha256Util.HexOfUtf8($"{symbolPath}|{basisLabel}")}";

    // ---- Promotion / proposal ---------------------------------------------------------------

    /// <summary>
    /// Promotes a mechanically observed anchor as a Gneiss FACT (kp.evidence.anchor; no decision
    /// needed — facts are admitted unconditionally). Returns the resulting assertion's aid, for
    /// use as a justification input on later claims.
    /// </summary>
    public string PromoteAnchor(AnchorEvidenceValue anchor, string actor, string reason)
    {
        string subject = AnchorSubject(anchor.SymbolPath, anchor.BasisLabel);
        var na = new NewAssertion(subject, PredEvidenceAnchor, GValue.Json(JsonSerializer.Serialize(anchor, JsonOpts)), Proposed: false);
        return AppendSingleAndGetAid(new TxEnvelope(actor, reason, DateTimeOffset.UtcNow), na);
    }

    /// <summary>
    /// Proposes a kp.behavior claim (Proposed = true; generation is proposal, charter §6.7).
    /// Evidence anchor aids become justification inputs. Returns the resulting assertion's aid.
    /// </summary>
    public string ProposeBehaviorClaim(string unitId, string claimId, string sentence, IReadOnlyList<string> evidenceAnchorAids, string actor, string reason)
    {
        string subject = BehaviorSubject(unitId, claimId);
        var justs = evidenceAnchorAids.Select(aid => new JustRef(aid, null, "evidence")).ToList();
        var na = new NewAssertion(subject, PredBehavior, GValue.Text(sentence), Proposed: true,
            Justifications: justs.Count > 0 ? justs : null);
        return AppendSingleAndGetAid(new TxEnvelope(actor, reason, DateTimeOffset.UtcNow), na);
    }

    /// <summary>Proposes a kp.correspondence claim (Proposed = true). Returns the resulting assertion's aid.</summary>
    public string ProposeCorrespondenceClaim(string corrId, CorrespondenceClaimValue value, IReadOnlyList<string>? evidenceAids, string actor, string reason)
    {
        string subject = CorrespondenceSubject(corrId);
        var justs = (evidenceAids ?? []).Select(aid => new JustRef(aid, null, "evidence")).ToList();
        var na = new NewAssertion(subject, PredCorrespondence, GValue.Json(JsonSerializer.Serialize(value, JsonOpts)), Proposed: true,
            Justifications: justs.Count > 0 ? justs : null);
        return AppendSingleAndGetAid(new TxEnvelope(actor, reason, DateTimeOffset.UtcNow), na);
    }

    /// <summary>Proposes a kp.verification claim (Proposed = true; a fail verdict is proposed too — it is evidence,
    /// contested-visible, never auto-accepted). Returns the resulting assertion's aid.</summary>
    public string ProposeVerificationClaim(string unitId, string criterion, VerificationClaimValue value, IReadOnlyList<string>? evidenceAids, string actor, string reason)
    {
        string subject = VerificationSubject(unitId, criterion);
        var justs = (evidenceAids ?? []).Select(aid => new JustRef(aid, null, "evidence")).ToList();
        var na = new NewAssertion(subject, PredVerification, GValue.Json(JsonSerializer.Serialize(value, JsonOpts)), Proposed: true,
            Justifications: justs.Count > 0 ? justs : null);
        return AppendSingleAndGetAid(new TxEnvelope(actor, reason, DateTimeOffset.UtcNow), na);
    }

    /// <summary>
    /// Records a kp.stale FACT (CONTRACT.md §7) against a corr:/unit:/verify: subject. Facts are
    /// admitted unconditionally, so staleness is visible in the current view the moment it is
    /// asserted — no decision required.
    /// </summary>
    public string AssertStale(string subject, StaleValue value, string actor, string reason)
    {
        var na = new NewAssertion(subject, PredStale, GValue.Json(JsonSerializer.Serialize(value, JsonOpts)), Proposed: false);
        return AppendSingleAndGetAid(new TxEnvelope(actor, reason, DateTimeOffset.UtcNow), na);
    }

    // ---- Decisions ----------------------------------------------------------------------------

    /// <summary>
    /// Accepts a proposed claim on behalf of the policy actor "policy:&lt;name&gt;@&lt;version&gt;"
    /// (CONTRACT.md §5) — the caller (e.g. the verification harness) is responsible for checking
    /// beforehand that policy.yaml allows the claim class AND the mechanical evidence is green
    /// (<see cref="Domain.PolicyEngine"/>); this method only records the decision.
    /// </summary>
    public void PolicyAutoAccept(string claimAid, string policyName, string policyVersion, string reason)
    {
        string actor = $"policy:{policyName}@{policyVersion}";
        var env = new TxEnvelope(actor, reason, DateTimeOffset.UtcNow);
        _ledger.Append(env, new IAppendItem[] { new NewDecision(DecisionKind.Accepts, TargetAid: claimAid) });
    }

    /// <summary>
    /// Records a human decision on a proposed claim (<c>kp decide</c>, CONTRACT.md §5/§9). A
    /// reason is required (K-A3 receipts discipline).
    /// </summary>
    public void HumanDecide(string claimAid, KpVerdict verdict, string reason, string actor = "govert")
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("HumanDecide requires a non-empty reason.", nameof(reason));

        var kind = verdict switch
        {
            KpVerdict.Accept => DecisionKind.Accepts,
            KpVerdict.Reject => DecisionKind.Rejects,
            _ => throw new ArgumentOutOfRangeException(nameof(verdict), verdict, "Unknown verdict."),
        };
        var env = new TxEnvelope(actor, reason, DateTimeOffset.UtcNow);
        _ledger.Append(env, new IAppendItem[] { new NewDecision(kind, TargetAid: claimAid) });
    }

    // ---- Views --------------------------------------------------------------------------------

    /// <summary>The full current belief view (CONTRACT.md §5: Ask("kp-current", ...), never the yaml alone).</summary>
    public BeliefView CurrentView() => _ledger.Ask(ContextName, new Question());

    /// <summary>The current belief view scoped to one subject.</summary>
    public BeliefView AskClaim(string subject) => _ledger.Ask(ContextName, new Question(Subject: subject));

    public Explanation Why(string aid) => _ledger.Why(ContextName, aid);

    /// <summary>Passthrough to the ledger's canonical JSONL export (tests/tooling; CONTRACT.md §2).</summary>
    public IReadOnlyList<string> ExportLedgerJsonl() => _ledger.ExportLedgerJsonl();

    /// <summary>Fetches a single assertion by aid (Gneiss.Cell facade v0.1, CONTRACT-V01.md §3);
    /// null if no assertion with that aid exists.</summary>
    public AssertionInfo? GetAssertion(string aid) => _ledger.GetAssertion(aid);

    // ---- Notes (K-A8 two-tier capture, CONTRACT-M15.md §3) ---------------------------------------

    /// <summary>Records a free-form note in the Gneiss note inbox (`kp note`). Returns the note's id.</summary>
    public string Note(string text, string actor, string reason) =>
        _ledger.Note(new TxEnvelope(actor, reason, DateTimeOffset.UtcNow), text);

    /// <summary>
    /// Lists every note in the workspace's note inbox, oldest first (`kp notes`). Passthrough to the
    /// Gneiss.Cell facade's <see cref="GneissLedger.ListNotes"/> (CONTRACT-V01.md §6) — the raw,
    /// read-only second connection this used before that method existed (a documented `// DIVERGENCE:`)
    /// is gone, so the sidecar boundary is once again unbroken.
    /// </summary>
    public IReadOnlyList<NoteInfo> ListNotes() => _ledger.ListNotes();

    // ---- Aid resolution -------------------------------------------------------------------------

    // CONTRACT-V01.md §1: Append now returns an AppendResult carrying each item's content-derived
    // aid in item order, so a single-item Append's aid is simply the first (only) entry — no more
    // scanning ExportLedgerJsonl() to recover it (the DIVERGENCE this replaced).
    private string AppendSingleAndGetAid(TxEnvelope env, NewAssertion na) =>
        _ledger.Append(env, new IAppendItem[] { na }).Aids[0];

    public void Dispose() => _ledger.Dispose();
}
