# Gneiss.Cell — Contract (v0, this increment)

Implements E1 (ledger + belief fold) + E2 (labels + `why()`) + E3-lite (staleness) from
[ROADMAP.md](../../ROADMAP.md), scoped per [AMENDMENTS.md](../../../AMENDMENTS.md). Reference
semantics: [THE-PAGE-v0](../../kb/maxwell/THE-PAGE-v0.md); rule spec:
[kb/22-BELIEF-ENGINE.md](../../kb/22-BELIEF-ENGINE.md) §1–§5 and §7 (the wrong-silo table is the
acceptance test, cell by cell). Deviations from this contract are findings — record them in code
comments tagged `// DIVERGENCE:` and surface them, don't silently patch.

## 1. Storage (SQLite via Microsoft.Data.Sqlite)

One file per ledger. Tables (STRICT), append-only enforced by triggers raising `ABORT` on
UPDATE/DELETE for the six base relations:

```sql
CREATE TABLE tx    (id INTEGER PRIMARY KEY AUTOINCREMENT, wall TEXT NOT NULL, actor TEXT NOT NULL,
                    reason TEXT NOT NULL, batch TEXT) STRICT;
CREATE TABLE assrt (aid TEXT PRIMARY KEY, tx INTEGER NOT NULL REFERENCES tx(id),
                    subj TEXT NOT NULL, pred TEXT NOT NULL,
                    val TEXT NOT NULL, valkind TEXT NOT NULL CHECK(valkind IN ('text','number','bool','entity','json')),
                    vfrom TEXT, vto TEXT,                    -- half-open [vfrom, vto), ISO-8601 UTC, NULL = open
                    status TEXT NOT NULL CHECK(status IN ('fact','proposed')),
                    src TEXT, meth TEXT, conf INTEGER,       -- confidence in basis points 0..10000, NULL = none
                    ckey TEXT NOT NULL) STRICT;
CREATE TABLE just  (aid TEXT NOT NULL, input_aid TEXT, rule_ver TEXT, role TEXT) STRICT;
CREATE TABLE dec   (aid TEXT PRIMARY KEY,                    -- the decision IS an assertion (same aid)
                    kind TEXT NOT NULL CHECK(kind IN ('accepts','rejects','retracts','supersedes')),
                    tgt_aid TEXT, tgt_ckey TEXT) STRICT;     -- exactly one of tgt_aid / tgt_ckey
CREATE TABLE cov   (region TEXT, scope TEXT, state TEXT, seal_aid TEXT) STRICT;  -- v0: present, unused
CREATE TABLE seal  (seal_aid TEXT, region TEXT, winner_ckey TEXT, winner_val TEXT, defeated_ckey TEXT) STRICT; -- v0: unused
-- view-plane (rebuildable; UPDATE/DELETE allowed):
CREATE TABLE receipt(id TEXT PRIMARY KEY, question TEXT NOT NULL, ctx_name TEXT NOT NULL,
                     ctx_hash TEXT NOT NULL, data_cut INTEGER NOT NULL, def_cut INTEGER NOT NULL,
                     consumed TEXT NOT NULL, result_hash TEXT NOT NULL, created_wall TEXT NOT NULL);
CREATE TABLE note  (id TEXT PRIMARY KEY, wall TEXT NOT NULL, actor TEXT NOT NULL, text TEXT NOT NULL,
                     promoted_aid TEXT);                      -- two-tier capture inbox (D-R10)
```

- `aid` = lowercase hex SHA-256 of canonical content `(tx-scoped ordinal | subj | pred | val | vfrom | vto | status | src | meth)` — or a GUID; MUST be deterministic given Append inputs + assigned TxId. Prefer content-derived.
- **Claim key** `ckey` = SHA-256 hex of `subj + "|" + pred + "|" + (vfrom ?? "") + "|" + (vto ?? "")` -- the (Subj, Pred, valid-slice) form from THE-PAGE. Computed by the library, never by callers.
- Decisions: `Append` writes both the `assrt` row (pred = `gneiss.decision`, subj = target ckey or aid, val = canonical JSON of the decision) and the `dec` row in one transaction.
- **I6 enforcement at append:** a decision with `tgt_aid` MUST target an assertion with strictly
  lower `tx` → else `GneissException` and the whole Append aborts. `tgt_ckey` decisions are exempt
  (they re-attach to past AND future assertions with that ckey — decision-survival by construction;
  see kb/22 §8 Q2 and D3).
- All writes in one `Append` = one SQLite transaction = one `tx` row.

## 2. Public API (≤ 20 public types — CI budget; keep internal everything else)

```csharp
public sealed class GneissLedger : IDisposable
{
    public static GneissLedger Create(string path);   // fails if exists
    public static GneissLedger Open(string path);
    public long HighWater { get; }
    public TxId Append(TxEnvelope env, IReadOnlyList<IAppendItem> items);
    public void DeclarePredicate(TxEnvelope env, PredicateDecl decl);  // sugar → assertion pred='gneiss.predicate', subj=decl.Name
    public void DeclareContext(TxEnvelope env, ContextDecl decl);      // sugar → assertion pred='gneiss.context', subj=decl.Name
    public BeliefView Ask(string contextName, Question q);             // writes a receipt row; view carries Label
    public Explanation Why(string contextName, string aid);
    public StaleReport CheckStale(string receiptId);
    public string Note(TxEnvelope env, string text);                   // inbox; returns note id
    public IReadOnlyList<string> ExportLedgerJsonl();                  // tx+assrt+dec+just as canonical JSONL (for golden ledgers)
}
public readonly record struct TxId(long Value);
public sealed record TxEnvelope(string Actor, string Reason, DateTimeOffset Wall, string? Batch = null);
public interface IAppendItem { }
public sealed record NewAssertion(string Subject, string Predicate, GValue Value,
    DateTimeOffset? ValidFrom = null, DateTimeOffset? ValidTo = null, bool Proposed = false,
    string? Source = null, string? Method = null, int? ConfidenceBp = null,
    IReadOnlyList<JustRef>? Justifications = null) : IAppendItem;
public sealed record NewDecision(DecisionKind Kind, string? TargetAid = null, string? TargetClaimKey = null) : IAppendItem;
public sealed record JustRef(string? InputAid, string? RuleVersion, string? Role = null);
public enum DecisionKind { Accepts, Rejects, Retracts, Supersedes }
public sealed record GValue(string Kind, string Canonical);  // factories: Text(s), Number(decimal), Bool(b), Entity(id), Json(canonical)
public sealed record PredicateDecl(string Name, string Comparator = "exact",  // exact | numberTol | stringNorm
    decimal? TolAbs = null, decimal? TolRel = null, int StopRung = 6, bool InstantSampled = false,
    IReadOnlyList<string>? SourcePrecedence = null);          // rung 2, highest first
public sealed record ContextDecl(string Name, long? DataCut = null, long? DefCut = null,
    string Admit = "decided-only", int? AdmitThresholdBp = null, string ConfPolicy = "standard-v1");
public sealed record Question(string? Subject = null, string? Predicate = null, string? ClaimKey = null); // null everything = ask-all
public sealed record BeliefView(Label Label, IReadOnlyList<BeliefEntry> Accepted,
    IReadOnlyList<BeliefEntry> Defeated, IReadOnlyList<ContestedGroup> Contested, TypedMissing? Missing);
public sealed record BeliefEntry(string Aid, string Subject, string Predicate, GValue Value,
    string ClaimKey, bool AutoAdmitted, bool StaleViaJustification, string? DefeatedBy, string? DefeatReason);
public sealed record ContestedGroup(string ClaimKey, IReadOnlyList<string> Aids, int StoppedAtRung);
public sealed record TypedMissing(string Kind);               // v0: always "unknown"
public sealed record Label(string ContextName, string ContextHash, long DataCut, long DefCut,
    IReadOnlyList<string> ConsumedAids, string ResultHash, string ReceiptId);
public sealed record Explanation(string Aid, string Status, string? DefeatedBy,
    IReadOnlyList<Explanation> Inputs, IReadOnlyList<string> RuleVersions, IReadOnlyList<string> Decisions);
public sealed record StaleReport(bool Stale, IReadOnlyList<string> Causes);
public sealed class GneissException : Exception { public string Code { get; } }
```

## 3. Evaluation semantics (the fold — L0, full recompute per Ask)

Per kb/22 §1–§2, THE-PAGE R1–R10. Deterministic order-fixed left fold in `tx` order (ties within a
tx by `aid`). No search, no fixpoint iteration — if a policy seems to need one, STOP (that is the
E1 kill signal; raise it, do not code around it).

1. **Resolve context**: `bootCtx` is FIXED IN CODE (dataCut = HighWater-at-ask, defCut = same,
   admit = decided-only, confPolicy = standard-v1). Named contexts: read `gneiss.context`
   declarations with tx ≤ (declared defCut or ask-time highwater); latest declaration per name
   ≤ defCut wins. Predicate declarations similarly pinned by defCut. `ctxHash` = SHA-256 of the
   resolved context's canonical JSON. Null DataCut in a declaration = highwater at ask time (the
   *resolved* value is recorded in the Label — labels always pin).
2. **Visible** = assertions with tx ≤ dataCut.
3. **Decision effectiveness**: process visible decisions in DESCENDING tx order (by I6, targets of
   aid-decisions point strictly backward; ckey-decisions targeting decisions must reference the
   decision's own subject-ckey — supported but rare). D is effective iff admitted(D) and no
   already-known-effective decision defeats D (kind rejects/retracts/supersedes with tgt = D.aid or
   D.ckey). admitted(D): status fact, or proposed + effective accepts targeting it.
4. **Admission** (non-decision): fact → admitted. proposed → effective `accepts` targeting its aid
   or ckey → admitted; else if ctx.Admit = threshold and conf ≥ threshold → admitted with
   `AutoAdmitted = true` (ALWAYS badged — kb/22 §8 Q1).
5. **Defeat by decision**: effective retracts/supersedes/rejects targeting aid or ckey → defeated
   (record defeater aid + kind as reason).
6. **Conflicts** (kb/22 §4): group remaining candidates by (subj, pred) with overlapping valid
   intervals (half-open overlap; NULL vto = +∞) AND incompatible values under the predicate's
   comparator (exact: canonical inequality; numberTol: |a-b| > tolAbs and relative > tolRel;
   stringNorm: trim+casefold inequality). Strainer pipeline, stopping at the predicate's StopRung:
   rung 2 source precedence (declared list; unlisted sources rank below listed; equal rank → next
   rung) · rung 3 specificity (only when one interval strictly contains the other: narrower wins)
   · rung 4 recency (only if predicate InstantSampled: later vfrom wins) · rung 5 confidence
   (higher bp; null = lowest) · rung 6 later tx wins (total). If StopRung reached without a unique
   winner → the whole group is **Contested** (an output, never an error). Losers → Defeated with
   reason `conflict:rung<N>`.
7. **Stale via justification** (kb/22 §5, in-view): an accepted assertion any of whose transitive
   `just.input_aid` inputs is defeated-or-contested in this context gets `StaleViaJustification =
   true` (it stays accepted — staleness is a flag inviting recomputation, not a defeat).
8. **Typed missing**: a Question naming (Subject and/or Predicate/ClaimKey) that matches zero
   visible assertions → `Missing = unknown` (closure/`absent_closed` deferred — attic).
9. **Label**: ConsumedAids = sorted aids of every assertion/decision examined for this question's
   scope (for scoped questions: all visible rows matching scope + all decisions targeting them +
   the context/predicate declaration aids; for ask-all: all visible). ResultHash = SHA-256 of the
   canonical JSON of (Accepted, Defeated, Contested, Missing) serialized deterministically.
   A `receipt` row is written on every Ask.
10. **CheckStale(receiptId)**: true iff any tx > receipt.data_cut wrote an assertion whose ckey ∈
    {ckeys of consumed aids} or a decision targeting any consumed aid/ckey, or a later
    context/predicate declaration re-declares a consumed declaration name. Causes list what.

## 4. `Why(ctx, aid)`

Status (accepted/defeated/contested/proposed-unadmitted/not-visible) + defeater if any + recursive
`Inputs` via `just` edges (cycle-safe; depth cap 32) + distinct rule_vers + decisions that targeted
this aid/ckey. Deterministic ordering (tx, aid).

## 5. Determinism discipline (D-R3 — non-negotiable)

- Canonical JSON everywhere: UTF-8, no whitespace, object keys in fixed schema-defined order (NOT
  alphabetical-per-object at runtime — fixed by the serializer code), numbers as invariant strings,
  `decimal` only on comparison paths (comparators parse `val` to `decimal`; f64 never touches a
  decision).
- All hashes SHA-256 lowercase hex. All timestamps ISO-8601 UTC (`yyyy-MM-ddTHH:mm:ss.fffffffZ`).
- Every enumeration that reaches output or hashing is explicitly ordered by (tx, aid) or sorted.
- Double-run test: same ledger file, same ask, twice (and after close/reopen) → identical
  ResultHash.

## 6. Required tests (xunit, in Gneiss.Cell.Tests)

1. **Wrong-silo table (kb/22 §7) — cell by cell.** Build the four-transaction ledger exactly as
   written (A1 fill fact; A2 mass with justifications to A1 + rule versions ShapeV7/DensityV3/
   FormulaV5; D1 retracts A1; A3 fill Silo18 with just → A1). Declare contexts `AuditJun21`
   (dataCut = tx2, defCut = tx2), `Current` (dataCut = null→latest), `Backtest` (defCut latest,
   dataCut = tx2). Assert: Audit — A1 accepted, A2 accepted, A3 not visible; Current — A1 defeated
   by D1, A2 accepted-but-StaleViaJustification (why cites D1), A3 accepted; Backtest — A1
   accepted, A2 accepted (D1 not visible: NOTE dataCut tx2 excludes D1). Ask(Silo17 fill) in
   Current: entry defeated; Ask(Silo18 fill) in Audit: Missing = unknown.
2. **Determinism:** double-run + reopen → identical ResultHash; ExportLedgerJsonl stable.
3. **Decision survival:** propose `sameAs` hypotheses with ckeys; `accepts` by ckey; append NEW
   proposed assertions with same ckeys, different aids/method (the "matcher rebuild"); view accepts
   the new ones by the old decision; a `rejects`-by-ckey keeps rejecting regenerated hypotheses.
4. **Cutoff coherence:** Ask(ctx with dataCut = t) over full ledger ≡ Ask(latest) over a copy
   truncated at t (build two ledgers, compare ResultHash).
5. **I6:** decision by tgt_aid targeting same-tx or later aid → GneissException; ledger unchanged.
6. **Append-only:** direct SQL UPDATE on assrt via a second connection → SQLite abort (trigger).
7. **Strainers:** source-precedence win; confidence win; StopRung=2 tie → Contested surfaced;
   numberTol comparator treats 4.20000000001 vs 4.2 as compatible (no conflict) under declared tol.
8. **Auto-admit badge:** threshold context admits conf 9000bp proposed, `AutoAdmitted = true`;
   decided-only context leaves it out of Accepted.
9. **Receipt staleness:** ask, then append a decision retracting a consumed aid → CheckStale true
   with cause; unrelated append → false.
10. **Rebuild drill:** delete all `receipt` rows; re-Ask under same declared contexts → same
    ResultHashes (views are cattle).
