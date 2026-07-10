# Prototype Designs

*Sharpens seed §20 (Prototypes A–E) into a dependency-ordered program with concrete tech, acceptance demos, and effort guesses (labeled as guesses). When direction is agreed, this document is the input to a full planning-workflow pass (plan → review rounds → beads); it is deliberately not that plan yet.*

*Per [03-NOTATION.md](03-NOTATION.md): prototypes are deliberately concrete, and every tech pick below (C#, SQLite, Postgres, Marten) is a **falsification convenience**, not a platform decision. What a prototype is allowed to prove is conceptual — semantics, property suites, mapping fit; its code expires.*

## The program at a glance

```
Track V (value now, in a real system)      Track K (kernel research spike)
──────────────────────────────────        ─────────────────────────────────
P-1  Retrofit probe (paper only)           P0  Gneiss.Kernel in-memory
P1   Confirmable links v2                  P2  Bitemporal assertion store
     (Smoothscrape, claim-keys)            P3  Property providers
                                           P4  Contexts + report compiler
        └────────────► convergence ◄──────┘         P5  Provenance / why()
              (P1 rebased onto P2 substrate if and only if both succeed)
```

Two tracks on purpose: Track V pays rent immediately and de-risks the *domain* fit; Track K de-risks the *kernel* design. Neither blocks the other; convergence is a decision, not an assumption.

DAG: P-1 → P1; P0 → P2 → {P3, P5}; {P2, P3} → P4. Effort guesses assume one experienced developer, part-time attention.

---

## P-1 — Retrofit probe (zero code, ~2 days)

Map one existing schema (proposal: Smoothscrape's link/overlay tables; second candidate: AIMS silo config) onto Gneiss vocabulary in a short markdown memo: which tables are ledger-like, which are projections, where decisions live, what the claim keys would be, which invariants (I1–I7) already hold, which are violated and what violating them has cost historically.

**Goal:** test whether the concepts fit reality before any code. **Kill signal:** if the mapping memo needs contortions ("well, this table is sort of both planes..."), the kernel needs revision — cheapest possible falsification. This is also the direct test of seed Risk 1 (too abstract).

## P0 — Gneiss.Kernel, in-memory (C#, 1–2 weeks)

Records + a pure belief fold + property-based tests. No storage, no I/O.

```csharp
public readonly record struct EntityId(Guid Value);
public readonly record struct TxId(long Value);           // total order = the spine

public sealed record Tx(TxId Id, DateTimeOffset WallClock, EntityId Actor, string Reason);

public sealed record Assertion(
    Guid Id, TxId Tx, EntityId Subject, EntityId Predicate,
    Value Value, ValidInterval Valid, BirthStatus Status,
    EntityId Source, EntityId Method, double? Confidence,
    ClaimKey? Claim);                                     // decisions target Claim or Id

public sealed record Justification(Guid Assertion, Guid? Input, RuleVersionId? Rule);

public sealed record Context(/* pins + policy version refs per 24-CONTEXTS.md */);

public static class Belief
{
    // THE contract: pure, deterministic, fold-shaped (invariant I3/I4/I6 by construction)
    public static BeliefView Compute(IReadOnlyList<Tx> ledgerPrefix, Context ctx);
}
```

Property tests (the AGM postulates recycled as test cases, per the KR survey):

1. **Determinism:** `Compute(prefix, ctx)` is referentially transparent (run twice, hash-equal).
2. **Ledger monotone / belief nonmonotone:** appending never mutates prior prefixes' views; a retraction visibly shrinks acceptance.
3. **Decision survival:** wipe + regenerate all `proposed` assertions with new ids but same claim keys → belief view unchanged.
4. **Retract/re-assert round trip:** retraction then equivalent re-assertion restores acceptance (recovery-flavored sanity).
5. **Cutoff coherence:** `Compute(ledger, ctx{cutoff=t})` == `Compute(ledger[..t], ctx{latest})`.
6. **I6 enforcement:** decision targeting a later tx is rejected at append.

**Goal:** freeze kernel semantics small enough to hold in one head. **Kill signal:** the fold needs iteration-to-fixpoint or search to express a policy anyone actually wants → stratification story is wrong (see survey 11's "defeat via acceptance" trap).

## P1 — Confirmable links v2 (Smoothscrape/CompSeek, SQLite or the existing DB, 1–2 weeks)

Seed Prototype A, upgraded with the survey findings. Tables (native, A1-style — no kernel dependency):

```sql
link_hypothesis (HypothesisId, ClaimKey UNIQUE-per-(kind,run) -- hash(kind, subj_key, obj_key)
                 Kind, SubjectKey, ObjectKey, MethodId, RunId, Confidence, CreatedAt)
link_support    (HypothesisId, EvidenceRef, SupportKind, Weight)
link_decision   (DecisionId, ClaimKey,      -- ← targets the CLAIM, not the hypothesis row
                 Verdict, DecidedBy, DecidedAt, Reason)
link_belief     -- VIEW: latest decision per claim wins; else admission policy
                 -- (three-band: auto-accept ≥ hi, queue in middle, auto-reject ≤ lo)
```

Acceptance demo: (a) run matcher → hypotheses; (b) decide a sample (accept some, reject some); (c) **drop all hypotheses, re-run matcher with changed ids and even a changed method** → decisions re-attach via claim keys, belief view identical; (d) point the same four tables at a second domain (sensor→silo assignment) with zero schema change — the seed's cross-domain goal.

**Goal:** real value in a real system this month; the claim-key design validated under fire. **Kill signal:** claim keys turn out unstable in practice (source record keys churn) — that would be a serious finding, since the whole decision-survival story leans on them.

## P2 — Bitemporal assertion store (Postgres or SQLite, 2–3 weeks, needs P0)

The [23-STORAGE.md](23-STORAGE.md) §2 schema, plus `Compute` from P0 compiled into SQL views for the default contexts. Implements: retraction, supersession with interval clipping, as-known-then, current-restated.

Acceptance demo: **the worked example from [22-BELIEF-ENGINE.md](22-BELIEF-ENGINE.md) §7 executes verbatim** — the wrong-silo story under `AuditAsOf(Jun21)`, `CurrentOperational`, and `Backtest`, producing exactly the table in that document. Differential test: SQL views ≡ P0 in-memory fold on randomized ledgers (the L0-as-oracle discipline from survey 13).

Substrate decision to make here, not before: hand-rolled tables vs Marten-as-ledger. Sub-experiment (2 days): write the same ledger as Marten events + projections; note friction where assertions are finer-grained than streams. → feeds [40-DISCUSSION-AGENDA.md](40-DISCUSSION-AGENDA.md) D6.

## P3 — Property providers (1–2 weeks, needs P2)

```csharp
public interface IPropertyHistory
{
    BeliefValue ValueAt(EntityId subject, EntityId predicate, DateTimeOffset t, Context ctx);
    IAsyncEnumerable<BeliefSample> HistoryBetween(EntityId subject, EntityId predicate,
                                                  DateTimeOffset from, DateTimeOffset to, Context ctx);
}
// BeliefValue = value | typed-missingness (never null), + why() handle
```

Four providers: SparseAssertion (P2), ConfigurationDocument (shape docs + binding assertions), DenseSeries (synthetic fill-level table + descriptor + a calibration-mask decision), Derived (mass = f(fill, shape, density, formula v), recording justifications + verifying trace).

Acceptance demo: one `HistoryBetween(Silo17, massEstimate, June, ctx)` call that transparently spans a manual-reading era, a radar era, and a shape-model change — the seed §3 story — and returns typed missingness (`not_configured`) for the pre-sensor gap. Then retract the calibration → derived values flip to stale → recompute → `why()` cites the new calibration.

## P4 — Contexts + report compiler (2–3 weeks, needs P2+P3)

Report definitions as documents ([24-CONTEXTS.md](24-CONTEXTS.md) §5); compile-time checks (predicate existence at definition cutoff, declared missingness, backfill availability); run recording `(def version, ctx version, high-water tx, output hash)`; the **restatement diff report** (as-reported | as-restated | why columns).

Acceptance demo: seed Prototype D verbatim — audit vs restated over the same data; introduce a correction, a source change, a new predicate, a formula change; the diff report explains all four differences with justification links. Plus: re-running the audit report at any later date is byte-identical (hash check).

## P5 — Provenance / why() (1 week, needs P2, ideally after P3)

Justification-graph walk rendered as an explanation tree (`why(value)`), reverse walk for impact (`what_becomes_stale_if(assertionId)`), and `stale(reportRun)` via verifying traces.

Acceptance demo: seed Prototype E — for one mass estimate, display value ← samples ← calibration ← shape doc ← formula version ← ontology version ← context, with the correction story visible after retraction.

---

## Sequencing recommendation

1. **This month:** P-1 (both memos: Smoothscrape *and* AIMS) + P1 in Smoothscrape. Real value, cheapest falsification, no kernel bet.
2. **In parallel/next:** P0, then P2 with its differential oracle.
3. **Decision gate after P2:** does the kernel earn its keep against the A1-native alternative demonstrated by P1? (Explicit comparison memo: what did P1 lack that P2 provides — multi-context evaluation, uniform corrections, provenance — and at what complexity cost.)
4. **Then** P3 → P4/P5 with AIMS-shaped demo data, since AIMS exercises rings B3–B6 that Smoothscrape doesn't.
5. **Then** the planning-workflow pass: full plan, external review rounds, beads, implementation of Gneiss.Kernel/Store/Providers as an A2 library.

Total through P5, honest guess: 8–12 part-time weeks. The two kill gates (P-1 mapping friction, P2-vs-P1 keep-earning gate) are the seed's Risk 1 and Risk 2 made operational — the program is designed to be abandonable cheaply, which is exactly what makes starting it safe.
