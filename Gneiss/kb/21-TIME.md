# Time: Two Axes and a 2×2, Not Five Axes

*Answers seed §6. Position: only valid time and transaction time are stored axes. Ontology time, rule time, and report time are not axes — they are context pins, and they come for free once definitions live in the ledger.*

## 1. The reduction

Seed §6 lists five times: valid, transaction, ontology, rule, report. Storing five axes per assertion would be crushing. The reduction:

- **Valid time** — stored on the assertion. The interval the claim is about. Half-open `[from, to)`; instants are degenerate intervals; open-ended futures allowed (`to = ∞`).
- **Transaction time** — stored on the transaction. When the ledger learned it. Total order by transaction id; wall clock is attached metadata, not the ordering key (clock skew must never reorder belief).
- **Ontology / rule / report "time"** — *not stored per assertion at all.* Ontology definitions, rule versions, and report definitions are themselves assertions in the ledger. Therefore "which ontology version" is just "which transaction-time cutoff to apply *to the definition subset of the ledger*." They are pins in the evaluation context, not axes in the data.

So an evaluation context carries (at minimum) **two cutoffs**:

```
data_cutoff        which evidence transactions are visible
definition_cutoff  which ontology/rule/report-definition transactions are visible
```

Usually equal. Deliberately unequal in exactly the interesting cases.

## 2. The 2×2 that replaces the mode list

Seed §7 lists report modes (faithful, current-analytical, restated, audit, simulation) as a flat menu. The two cutoffs generate them:

| | definitions **then** | definitions **now** |
|---|---|---|
| **data then** | **Faithful replay / audit** — what the system actually said, byte-for-byte | **Backtest** — what today's rules would have concluded on evidence available then |
| **data now** | **Reconciliation** — old report shape over corrected inputs (rare, forensic) | **Current best belief / restated** — today's everything |

Every seed mode lands in a cell (simulation = any cell + hypothetical overrides). This is a real simplification: instead of enumerating report modes, reports declare two pins plus policies. The backtest cell is a quiet gift — it is exactly the ML/matching-rule evaluation loop ("would matcher v3 have found this link in 2024?") and the feature-store "point-in-time correct training set" pattern, obtained without new machinery.

## 3. Practical decisions (positions taken)

**Interval convention.** Half-open `[from, to)` everywhere, UTC in storage. Site-local calendars (shift boundaries, "yesterday" for a report) are *context concerns*, resolved at evaluation, never baked into stored valid times.

**Granularity per predicate.** `fillLevel` is instant-sampled; `shape` is interval-valid; `capacity` changes at commissioning events. Granularity and interpolation stance (step-hold vs linear vs none) are predicate metadata — ontology assertions — because a provider cannot answer `value_at(t)` without them.

**Correcting valid time.** The wrong-interval case ("shape V7 actually applied from March, not January") is supersession: append a new assertion with the corrected interval, decision links it. Belief views compute effective intervals by *clipping* — the ledger never rewrites intervals. Clipping rules (later-supersedes, most-specific-wins) belong to the conflict policy.

**Transaction time for imports — the knowledge horizon.** Bulk-imported history gets transaction time = import time. "As known on 2024-03-01" is therefore an approximation for anything imported later. Two honest mitigations: (a) record `source_recorded_at` as an evidence attribute when the legacy system has it, and let audit contexts optionally order by it *with an explicit flag*; (b) publish each system's **knowledge horizon** — the transaction time before which as-known-then is best-effort. Never fake transaction times; that forfeits I3 and every determinism guarantee downstream.

**Bitemporal storage shape.** The classic pattern suffices: assertions carry `(valid_from, valid_to)`; the transaction carries `tx_id`; defeat is computed, not stored — an assertion does not have a "tx_to" column, because *whether it is still believed is context-dependent* (this is where Gneiss deliberately departs from textbook bitemporal tables and from SQL:2011 system versioning, which hard-code a single belief timeline). A materialized projection for the default context may well add a physical `superseded_by_tx` column as an optimization — in the view plane, where it belongs.

**Ordering within a transaction.** Assertions within one transaction are unordered; if order matters, that is two transactions. Keeps replay semantics trivial.

**Allen relations.** Useful as query vocabulary (`overlaps`, `during`, `meets`) in provider APIs; no need for them in the kernel.

## 4. What each classic question compiles to

| Question (seed §6) | Compilation |
|---|---|
| What did we believe on June 20? | `f(ledger[..tx_at(Jun20 23:59)], ctx{defs: same cutoff})` |
| What do we now believe about June 20? | `f(ledger[..now], ctx{...})`, filter valid_time ∩ Jun20 |
| What would the old report have shown? | report def as of definition_cutoff, data_cutoff = then |
| What does today's report show about then? | defs now, data now, valid-time window = then |
| What would today's rules conclude on evidence available then? | defs now, data then (backtest cell) |
| What would be restated if we allowed later corrections? | diff(current cell, faithful cell) — restatement *is* this diff |

That last line matters for UX: a **restatement report** is not a new mode; it is the diff between two cells of the 2×2, with justifications explaining each difference ("changed because correction C1 retracted A1"). This is what an auditor actually wants to read.

## 5. Open questions for discussion

1. Do we ever need a *third* stored axis — "decision time" distinct from transaction time — for workflows where decisions are made offline and entered later? (Current position: no; `decided_at` is an attribute on the decision assertion, and belief ordering stays by transaction id. Revisit if audit requirements demand ordering by decision time.)
2. Should `source_recorded_at` be lifted into the envelope (alongside source/method) rather than being an optional attribute? Leaning yes for imported-history-heavy systems like Smoothscrape.
3. Valid-time timezone edge cases for daily aggregates (silo reports at site-local midnight) — context responsibility, but needs a worked example before we freeze provider APIs.
