# Gneiss.Cell — Facade v0.1 Addendum

Extends [CONTRACT.md](CONTRACT.md) per THE-PAGE findings F5 and AMENDMENTS 2026-07-11. The v0
semantics are unchanged; this is API surface only. Existing tests stay green (update signatures,
never weaken assertions).

## 1. `Append` returns per-item aids

```csharp
public sealed record AppendResult(TxId Tx, IReadOnlyList<string> Aids);  // Aids in item order (decisions included)
public AppendResult Append(TxEnvelope env, IReadOnlyList<IAppendItem> items);
```
Aids are the content-derived assertion ids, one per item, in the order given (a `NewDecision`
contributes its decision-assertion's aid). `DeclarePredicate`/`DeclareContext` keep their void
signatures.

## 2. Deterministic receipt ids

`Receipt.Id` (and `Label.ReceiptId`) becomes content-derived:
`sha256(ctxHash | dataCut | defCut | canonical(question) | resultHash)` — lowercase hex. Repeated
identical asks produce the same id; the receipt row upserts (INSERT OR REPLACE) rather than
accumulating duplicates. `CheckStale` is unaffected. Add a test: two identical Asks → same
ReceiptId, one receipt row; a data-changing append then a re-Ask → different ReceiptId.

## 3. Fetch by aid

```csharp
public sealed record AssertionInfo(string Aid, long Tx, string Subject, string Predicate,
    GValue Value, string ClaimKey, string Status, string? Source, string? Method, int? ConfidenceBp);
public AssertionInfo? GetAssertion(string aid);
```

## 4. Public-surface budget

The 20-type budget rises to **22** (AppendResult, AssertionInfo) — a conscious, recorded
adjustment (this addendum is the record); update `PublicSurfaceTests` to 22 with a comment
pointing here. Nothing else becomes public.

## 5. Consumer note

`KodePorter.Core.Gneiss.GneissBinding` currently recovers aids by scanning `ExportLedgerJsonl()`
(documented `// DIVERGENCE:`). A follow-up KodePorter change (separate agent) removes those scans
in favor of `AppendResult`/`GetAssertion`; do not modify KodePorter from this contract's scope.
