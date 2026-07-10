# The Belief Engine

*Develops seed §5, §10, §11, §14. The belief view as a deterministic, stratified computation over the ledger — truth maintenance reduced to what pays rent.*

## 1. The semantics in six rules

A belief view is computed per context. Informally, in Datalog-with-negation style (the real implementation will be SQL views + code, but this is the specification):

```
visible(A, C)    :- asserted(A, Tx), Tx ≤ C.data_cutoff.

defeated(A, C)   :- decision(D, retracts, A),      effective(D, C).
defeated(A, C)   :- decision(D, supersedes, A, _), effective(D, C).
defeated(A, C)   :- envelope_source(A, S), invalidated(S, A.valid_time, C).
defeated(A, C)   :- conflicts(A, B, C), prefers(C.conflict_policy, B, A).

effective(D, C)  :- visible(D, C), not defeated(D, C).      -- decisions are assertions too

admitted(A, C)   :- status(A, fact).
admitted(A, C)   :- status(A, proposed), decision(D, accepts, A), effective(D, C).
admitted(A, C)   :- status(A, proposed), C.admission allows unreviewed,
                    confidence(A) ≥ C.admission.threshold.

accepted(A, C)   :- visible(A, C), admitted(A, C), not defeated(A, C).
```

Outputs are three sets, all first-class: **accepted**, **defeated (with defeater)**, and **unresolved conflicts** (where the policy declines to pick a winner — surfaced, never swallowed).

## 2. Why this terminates and stays deterministic

The dangerous part is `effective(D) :- ... not defeated(D)` — decisions defeating decisions (an appeal, a reversed confirmation). Negation through a cycle would make semantics ambiguous (the classic nonmonotonic swamp). Gneiss avoids the swamp **by construction, not by solver**:

> **Kernel invariant I6:** a decision may only target assertions with strictly lower transaction ids.

The "targets" graph is therefore acyclic, and evaluation stratifies by transaction order: process transactions oldest-first; each decision's effectiveness is settled before anything it targets is finally judged. No stable-model machinery, no well-founded semantics, no solver dependency — a fold over the ledger. This is the single most important engineering decision in the whole design: **belief must be a fold, not a search.**

Two further determinism requirements:

- **Policies are data, versioned.** A context pins exact versions of its conflict/precedence/admission policies (they live in the ledger like everything else). "Same prefix + same context = same view, forever" fails the moment a policy is code that someone edits.
- **Total tiebreak.** Every conflict policy must terminate in a total order (…, then higher confidence, then later transaction id). "Unresolved" is an allowed *output*; nondeterminism is not.

## 3. Relation to the classic machinery (what we take, what we refuse)

| Theory | Take | Refuse |
|---|---|---|
| JTMS (Doyle) | The in/out labeling *is* accepted/defeated; justification edges *are* our Justification primitive. | Dependency-directed backtracking, non-deterministic relabeling — our I6 makes them unnecessary. |
| ATMS (de Kleer) | The deep idea: a node's label says *under which assumption sets it holds*. Gneiss contexts select assumption sets (which decisions/policies/source-validities are in force), so one ledger serves many belief views — that is ATMS's multiple-context trick with contexts made explicit and few. | Computing full labels (exponential). We evaluate per named context, on demand; we never enumerate all environments. **Caveat from the KR survey:** vanilla ATMS labels are *monotone* in assumptions, while Gneiss defeat is nonmonotone (accepting a retraction decision removes beliefs) — so the correspondence is a design lens, not a semantics we could inherit even in principle without NATMS-style out-assumptions. |
| AGM belief revision | The postulates as *test cases* (e.g., revising with already-accepted information changes nothing; retraction then re-assertion restores belief). | Any attempt to implement AGM operators directly; they underdetermine implementations. |
| Stable models / ASP | Nothing at runtime. | Solver-dependent semantics in an operational system. If a policy is so tangled it needs ASP, the policy is wrong. |

(Agent survey [11-SURVEY-KR-BELIEF.md](11-SURVEY-KR-BELIEF.md) checks these claims against the literature.)

## 4. Conflict policies

A conflict exists when two accepted-candidate assertions give incompatible values for the same (subject, predicate, overlapping valid time). Policy is a *pipeline of strainers*, each a named, versioned rule:

```
1. decision wins        (explicitly accepted beats everything)
2. source precedence    (confirmed radar > manual > interpolated — per predicate)
3. specificity          (narrower valid interval clips wider — supersession clipping)
4. recency of evidence  (later valid-time observation for instant-sampled predicates)
5. confidence
6. later transaction id (total tiebreak)  — or —  STOP: emit unresolved
```

Where a policy chooses to stop before rung 6 matters: billing-grade predicates should emit `unresolved` and demand a decision; telemetry-grade predicates can auto-tiebreak. This knob — *how far down the strainer a predicate is allowed to fall* — is a per-predicate ontology assertion.

## 5. Derived values and staleness

Derived assertions (mass estimates) carry justifications: inputs + rule version. Two consequences:

- **Why-query:** `why(A)` walks justifications down to observations, decisions, and rule versions — the seed §15 explainability list, answered by graph walk.
- **Dirty propagation:** when a transaction defeats assertion X, everything justified-by X (transitively) becomes *stale in contexts where X mattered*. Staleness is a view-plane flag, not a ledger fact. Recompute happens lazily (on read) or eagerly (subscription) per projection.

This is "poor-man's provenance" — dependency edges and dirty bits, not provenance semirings. Position: that is the right trade; semiring provenance answers "in exactly which ways does this output depend on inputs," and nobody in our domains has asked that question at a price above dependency-set precision.

## 6. Incrementality ladder (summary; details in [13-SURVEY-INCREMENTAL.md](13-SURVEY-INCREMENTAL.md))

- **L0** Recompute view per query. Correct, fine for prototypes and audit contexts (which are frozen anyway — cache forever).
- **L1** Per-key recompute: a new transaction touching (subject, predicate) recomputes only that key's slice in affected projections. The workhorse for curated-operational envelopes. *(Amended 2026-07-04: "ledger writes are human/import-paced" was an envelope assumption, not a property — rung selection is per (view, envelope); see [29-ENVELOPE.md](29-ENVELOPE.md).)*
- **L2** Dirty-set propagation through justification edges for derived values.
- **L3** Real IVM (DBSP-style) — only if L1/L2 measurably fail.

Start at L0/L1. The Gneiss Contract makes climbing safe: any rung can be validated against full recompute, byte-for-byte — incrementality bugs are *detectable by construction*. (Projection rebuild = event-sourcing replay; blue/green rebuilds apply directly.)

## 7. Worked example: the wrong-silo correction, three contexts

Ledger (abbreviated):

```
tx1 (Jun20 10:03)  A1: fillLevel(Silo17) = 4.2m @ [Jun20 10:00], src=manual_laser, status=fact
tx2 (Jun20 10:30)  A2: massEstimate(Silo17) = 12.4t @ [Jun20 10:00], just={A1, ShapeV7, DensityV3, FormulaV5}
tx3 (Jun22 09:10)  D1: retracts A1, reason="wrong silo selected"
tx4 (Jun22 09:11)  A3: fillLevel(Silo18) = 4.2m @ [Jun20 10:00], src=correction, derived_from=A1
```

| | Audit(Jun21) — data ≤ tx2, defs ≤ tx2 | Current — data ≤ tx4 | Backtest — defs now, data ≤ tx2 |
|---|---|---|---|
| A1 (Silo17 fill) | **accepted** | defeated (by D1) | accepted (D1 not visible) |
| A2 (Silo17 mass) | **accepted** | **stale** → recompute yields `not_observed` for Silo17 mass; justification cites D1 | accepted |
| A3 (Silo18 fill) | not visible | **accepted** | not visible |
| Silo17 report line | 12.4t, "as reported at the time" | missing: `retracted`, with why-link to D1 | 12.4t |
| Silo18 report line | `not_observed` | 12.4t equivalent after re-derivation | `not_observed` |

Every cell is forced by the six rules plus the context pins — nothing ad hoc. This table is the acceptance test for Prototype P2 ([31-PROTOTYPES.md](31-PROTOTYPES.md)).

## 8. Open questions

1. **Hypothesis admission without decisions** (rule 3 of `admitted`): auto-admitting high-confidence unreviewed links is operationally tempting (Smoothscrape does a version of this) and epistemically risky. Per-context threshold feels right; should *auto-admitted* acceptances be visibly badged in every downstream surface? (Position: yes, always.)
2. **Decision scope:** does accepting hypothesis H accept the *assertion* or the *(subject, object) claim*? If the matcher re-proposes the same link with a new method next rebuild, the old decision should re-attach — which argues decisions target a **claim key** (type, subject, object, predicate), not an assertion id. This is the single trickiest identity question in the design; it decides whether P1's "decisions survive rebuild" works by construction or by fragile id-mapping. Current position: decisions may target either an assertion id *or* a claim key; the claim-key form is the norm for link hypotheses. Needs prototyping.
3. **Cross-context writes:** may a decision be scoped to a context ("accepted for operations, not for billing")? Parsimony says no (decisions are global; contexts differ in *policy*, not in which decisions exist). Watch for a real counterexample.
