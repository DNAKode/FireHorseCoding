// Gneiss.Cell — public surface, per CONTRACT.md section 2.
// Exactly 20 public types (the kb/32 solo-maintainer tripwire, enforced by a reflection test).

namespace Gneiss.Cell;

/// <summary>Opaque handle to a transaction id assigned by <see cref="GneissLedger.Append"/>.</summary>
public readonly record struct TxId(long Value);

/// <summary>The write envelope carried by every ledger-mutating call.</summary>
public sealed record TxEnvelope(string Actor, string Reason, DateTimeOffset Wall, string? Batch = null);

/// <summary>Marker interface for items passed to <see cref="GneissLedger.Append"/>.</summary>
public interface IAppendItem
{
}

/// <summary>A new base assertion to append.</summary>
public sealed record NewAssertion(
    string Subject,
    string Predicate,
    GValue Value,
    DateTimeOffset? ValidFrom = null,
    DateTimeOffset? ValidTo = null,
    bool Proposed = false,
    string? Source = null,
    string? Method = null,
    int? ConfidenceBp = null,
    IReadOnlyList<JustRef>? Justifications = null) : IAppendItem;

/// <summary>A new decision to append. The decision is itself an assertion (same aid).</summary>
public sealed record NewDecision(
    DecisionKind Kind,
    string? TargetAid = null,
    string? TargetClaimKey = null) : IAppendItem;

/// <summary>A justification edge: this new assertion is (partly) grounded in <see cref="InputAid"/> and/or rule <see cref="RuleVersion"/>.</summary>
public sealed record JustRef(string? InputAid, string? RuleVersion, string? Role = null);

/// <summary>Decision kinds recognized by v0. (dec.kind CHECK constraint mirrors this.)</summary>
public enum DecisionKind
{
    Accepts,
    Rejects,
    Retracts,
    Supersedes,
}

/// <summary>A typed scalar value. Use the factory methods to construct.</summary>
public sealed record GValue(string Kind, string Canonical)
{
    public static GValue Text(string s) => new("text", s);
    public static GValue Number(decimal d) => new("number", d.ToString(System.Globalization.CultureInfo.InvariantCulture));
    public static GValue Bool(bool b) => new("bool", b ? "true" : "false");
    public static GValue Entity(string id) => new("entity", id);
    public static GValue Json(string canonical) => new("json", canonical);
}

/// <summary>Declares comparator/strainer policy for a predicate. Sugar over an assertion with pred='gneiss.predicate'.</summary>
public sealed record PredicateDecl(
    string Name,
    string Comparator = "exact",
    decimal? TolAbs = null,
    decimal? TolRel = null,
    int StopRung = 6,
    bool InstantSampled = false,
    IReadOnlyList<string>? SourcePrecedence = null);

/// <summary>Declares a named context. Sugar over an assertion with pred='gneiss.context'.</summary>
public sealed record ContextDecl(
    string Name,
    long? DataCut = null,
    long? DefCut = null,
    string Admit = "decided-only",
    int? AdmitThresholdBp = null,
    string ConfPolicy = "standard-v1");

/// <summary>A belief-view query. Null everything = ask-all.</summary>
public sealed record Question(string? Subject = null, string? Predicate = null, string? ClaimKey = null);

/// <summary>The result of <see cref="GneissLedger.Ask"/>.</summary>
public sealed record BeliefView(
    Label Label,
    IReadOnlyList<BeliefEntry> Accepted,
    IReadOnlyList<BeliefEntry> Defeated,
    IReadOnlyList<ContestedGroup> Contested,
    TypedMissing? Missing);

/// <summary>One belief-view row.</summary>
public sealed record BeliefEntry(
    string Aid,
    string Subject,
    string Predicate,
    GValue Value,
    string ClaimKey,
    bool AutoAdmitted,
    bool StaleViaJustification,
    string? DefeatedBy,
    string? DefeatReason);

/// <summary>An unresolved conflict group — a first-class output, never an error.</summary>
public sealed record ContestedGroup(string ClaimKey, IReadOnlyList<string> Aids, int StoppedAtRung);

/// <summary>Typed-missing marker. v0: always "unknown" (closure/absent_closed deferred to the attic).</summary>
public sealed record TypedMissing(string Kind);

/// <summary>The label is the consumed-set of the evaluation (THE-PAGE §(e)).</summary>
public sealed record Label(
    string ContextName,
    string ContextHash,
    long DataCut,
    long DefCut,
    IReadOnlyList<string> ConsumedAids,
    string ResultHash,
    string ReceiptId);

/// <summary>The result of <see cref="GneissLedger.Why"/> — a justification tree.</summary>
public sealed record Explanation(
    string Aid,
    string Status,
    string? DefeatedBy,
    IReadOnlyList<Explanation> Inputs,
    IReadOnlyList<string> RuleVersions,
    IReadOnlyList<string> Decisions);

/// <summary>The result of <see cref="GneissLedger.CheckStale"/>.</summary>
public sealed record StaleReport(bool Stale, IReadOnlyList<string> Causes);

/// <summary>Errors raised by Gneiss.Cell. <see cref="Code"/> is a short machine-readable tag.</summary>
public sealed class GneissException : Exception
{
    public string Code { get; }

    public GneissException(string code, string message) : base(message)
    {
        Code = code;
    }
}
