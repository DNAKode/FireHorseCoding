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

## 6. List notes (v0.1 addendum, 2026-07-12)

```csharp
public sealed record NoteInfo(string Id, string Wall, string Actor, string Text, string? PromotedAid);
public IReadOnlyList<NoteInfo> ListNotes();  // every note, oldest first (insertion order)
```

`Note(...)` (CONTRACT-M15.md §3) appends to the `note` table but had no read side, and notes are
not part of `ExportLedgerJsonl()` (which covers only tx/assrt/dec/just — CONTRACT.md §2), so a
consumer had no sanctioned way to read them back. `ListNotes()` closes that gap: it returns every
note in the ledger, oldest first — ordered by rowid (insertion order), not by `wall`, so two notes
recorded within the same wall-clock tick still list in append order. `PromotedAid` is null until a
note is promoted into an assertion.

The public-type budget rises from **22** to **23** (NoteInfo) — a conscious, recorded adjustment
(this addendum is the record); update `PublicSurfaceTests` to 23 with a comment pointing here.
Nothing else becomes public.

### Consumer note

`KodePorter.Core.Gneiss.GneissBinding.ListNotes()` previously opened a second, read-only
`SqliteConnection` directly to the ledger file and read the `note` table by hand (documented
`// DIVERGENCE:`), the last side-channel around this facade. It now calls `ListNotes()` and returns
`NoteInfo` directly; the raw connection and the divergence are gone.
