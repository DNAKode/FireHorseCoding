# Candidate E — The Relations: The Page Is The Program

*Phase M, Maxwell hunt. The [S]-spine candidate. Thesis: the kernel is a fixed relational schema (6 relations) plus a stratified Datalog¬ program (~15 rules). No solver, no fixpoint search — a fold over the ledger in transaction order. Codd's own move: pick the schema, write the rules, let the algebra fall out. This page is the **reference semantics** the other four candidates must match — executable on any Datalog engine, transcribable to SQL views.*

Tags: **[S]** settled/load-bearing · **[F]** flexible/policy-shaped · **[M]** maybe/unproven. Nothing [M] is load-bearing.

---

## 1. THE ONE PAGE

**SCHEMA** (6 relations — the append-only base; everything else is derived) **[S]**
```
tx(TxId, WallClock, Actor, Reason, Batch)                                       -- write envelope; TxId is the total order
assrt(Aid, TxId, Subj, Pred, Val, VFrom, VTo, Status, Src, Meth, Conf, CKey)    -- immutable claim; Status∈{fact,proposed}; CKey=claim key
just(Aid, InAid, RuleVer, Role)                                                 -- support edge: Aid grounded in InAid and/or RuleVer
dec(Aid, Kind, TgtAid, TgtCKey, ByAid)                                          -- decision (an assrt too); Kind∈{accepts,rejects,retracts,supersedes,invalidates_src,redacts}
cov(Region, Scope, State, SealAid)                                              -- coverage map; State∈{full,sealed,archived,purged,lost,suspect}
seal(SealAid, Region, WinnerCKey, WinnerVal, DefeatedCKey)                      -- certified frontier: accepted winners + defeat outcomes for a sealed region (kb/25 §3)
```
Context `C` is a fact tuple, not a parameter: `ctx(C, DataCut, DefCut, ConfPolicy, PrecPolicy, ClosurePolicy, AdmitPolicy, StopRung)` — all pins are columns, all read by guarded subgoals (§4). Policies are rows in `assrt` (predicate = ontology stance), pinned by `DefCut`. **[S]**

**RULES** (numbered; `C` free in every head = "per context"; `t.TxId ≤ C.DataCut` written `tx_vis(t,C)`) **[S]** unless tagged
```
                                                                            -- ── EVIDENCE VISIBILITY ──
R1  visible(A,C)      :- assrt(A,Tx,…), tx_vis(Tx,C).                         -- A is on the table if its tx is within the data cutoff
                                                                            -- ── DEFEAT (over raw evidence + total preorder; never over acceptance) ──
R2  defeated(A,C)     :- dec(D,retracts,A,_,_),      effective(D,C).          -- retraction decision in force
R3  defeated(A,C)     :- dec(D,supersedes,A,_,_),    effective(D,C).          -- supersession decision in force
R4  defeated(A,C)     :- envsrc(A,S), invalidated(S,A.VFrom,C).               -- source/method invalidated over A's valid time
R5  defeated(A,C)     :- conflict(A,B,C), prefers(C.ConfPolicy,B,A).          -- lost a conflict under the context's strainer pipeline
                                                                            -- ── DECISIONS ARE ASSERTIONS; I6 STRATIFIES THEM ──
R6  effective(D,C)    :- visible(D,C), not defeated(D,C).                     -- a decision fires unless itself defeated (targets are strictly earlier — I6)
                                                                            -- ── ADMISSION ──
R7  admitted(A,C)     :- assrt(A,…,Status=fact,…).                            -- facts are admitted outright
R8  admitted(A,C)     :- assrt(A,…,Status=proposed,…), dec(D,accepts,A,_,_), effective(D,C).  -- hypothesis accepted by decision
R9  admitted(A,C)     :- assrt(A,…,Status=proposed,…,Conf), allows_unreviewed(C.AdmitPolicy), Conf ≥ threshold(C.AdmitPolicy).  -- [F] threshold auto-admit (badged)
                                                                            -- ── ACCEPTANCE (the least-fixed-point head) ──
R10 accepted(A,C)     :- visible(A,C), admitted(A,C), not defeated(A,C).      -- the belief view: on the table, admitted, undefeated
                                                                            -- ── GRADE (kb/25 §4) ──
R11 grade(A,grounded,C):- accepted(A,C), region_of(A,R), cov(R,_,full,_).     -- recomputable from surviving raw evidence
R12 grade(A,sealed,C) :- accepted(A,C), region_of(A,R), cov(R,_,sealed,S), certifies(S,A). -- recomputable only via a seal
R13 accepted(A,C)     :- seal(S,R,CK,V,_), region_in_cut(R,C), claim(A,CK,V), not superseded_after_seal(A,C). -- [S] sealed winners re-enter belief, outranking raw survivors (anti-attrition, §3)
                                                                            -- ── COVERAGE-LICENSED NEGATION (I8) ──
R14 absent_closed(K,C):- not some_accepted(K,C), closed(K,C.ClosurePolicy), region_of(K,R), covered_for_closure(R,C). -- [S] confident "no" ONLY under positive coverage
                                                                            -- ── FEDERATION (imported relations + watermark facts) ──
R15 visible(A,C)      :- imported(A,FromLedger,W), watermark(C,FromLedger,Wc), W ≤ Wc.  -- [F] a remote ledger is a source; its rows visible up to the pinned watermark
```
Outputs, all first-class: **accepted**, **defeated(with defeater)**, **contested** (R5 conflict where `C.ConfPolicy` stops before its total rung — emitted, never resolved by search), plus **typed missingness** (R14 + the absence enum). **[S]**

**EVALUATION DISCIPLINE** (three lines — this is the whole engine) **[S]**
1. **Strata = transaction order.** Process tx oldest-first. By I6 every `dec` targets strictly-lower `TxId`, so `effective(D)` for D at tx *n* is settled before anything at tx *n* it could defeat — the program is **locally stratified through time** (Dedalus/Statelog; kb/11 §3). Negation (`not defeated`, `not some_accepted`) never closes a cycle. **This is the page's load-bearing lemma.**
2. **Total tiebreak.** Each conflict policy terminates in a total order (…, higher Conf, then later TxId). `contested` is an allowed *output*; nondeterminism is not. Ledger sequence is the ultimate tiebreaker → unique perfect model.
3. **Contested is output, never search.** No stable-model machinery, no WFS solver in the serving path. A fold, not a search. Full recompute per context is the semantic oracle; every optimization must equal it byte-for-byte.

---

## 2. Needs generated, not bolted

| Need | How it falls out of the page — no new machinery |
|---|---|
| **Recording** | The base *is* the six append-only relations (`tx`,`assrt`,`just`,`dec`,`cov`,`seal`). Writing = INSERT. I1/I2 = no UPDATE/DELETE grant. **[S]** |
| **Deterministic views** | R1–R15 have a unique perfect model per `ctx` row (§3 Thm 1). "Same prefix + same context = same view, forever." **[S]** |
| **Contexts / what-if** | Context is a fact tuple; the 2×2 (kb/21) is just `(DataCut, DefCut)` columns. WhatIf = insert hypothetical `assrt`/`dec`/policy rows tagged to a sandbox `ctx`, evaluate, read the delta. Guarded rules (§4) keep it stratified. **[S]** for pins; **[F]** for what-if promotion policy |
| **Forgetting + degradation** | `cov` + `seal` are relations the rules consult. Seal → purge → record = INSERT `seal`, delete raw `assrt` rows in region, INSERT `cov(…,purged/sealed,…)`. R11–R13 recompute grades; R14 retreats `absent_closed`→`unknown` when coverage drops. **[S]** |
| **Federation** | `imported` + `watermark` are relations; R15 makes a remote ledger a source with a pinned frontier. No consensus, no global order (I3′ per-ledger). **[F]** |
| **Consumed-set / labels** | Each derived tuple carries the **set of base tuples used** as a provenance annotation on `just` (why-provenance, kb/11 §6). `why(A)` = transitive walk of `just` to observations/decisions/rule-versions/seals. The consumed-set is what the label *is* (kb/19 §2). **[S]** as edges; **[M]** as a formal semiring (§6) |

The six needs are *reads of the schema*, not features stapled on. That is the candidate's whole claim.

---

## 3. Theorem sketches

**T1 — Determinism (cite, don't prove). [S]** The program R1–R15 is a Datalog¬ program that is **locally stratified through time** by I6 (every `dec` targets strictly-lower `TxId`; the "targets" graph is a DAG; strata = tx order). A locally stratified program has a **unique perfect model**, computable bottom-up in polynomial time by semi-naive evaluation (Przymusinski 1988; Van Gelder). Add versioned total-order policies + ledger-sequence tiebreak (Grosof courteous-LP determinism) ⇒ `f(ledger_prefix, ctx)` is a **function**. This is stratified-Datalog uniqueness specialized to our schema — kb/05 Thm 1, provable now.

**T2 — Rung equivalence (the formulation's freebie). [S]** For a stratified Datalog¬ program, **semi-naive ≡ naive evaluation** — both compute the identical perfect model (standard result). So L0 (full recompute) ≡ any semi-naive materialization: the optimizer's license is *free*, inherited from the formalism rather than argued. **What it does NOT give you [S]:** per-key incremental with early cutoff. Semi-naive is whole-relation delta iteration; it does not know that a new tx touching `(Subj,Pred)` need only recompute that key's slice. L1 per-key and DRed-style incremental (kb/22 §6) are *additional* claims requiring dependency-edge machinery and their own L0-differential proof — the formalism does not hand them to you.

**T3 — Monotone degradation (the hard one). [S] with a stated hypothesis.** Claim: reducing coverage (`full`→`sealed`→`purged`/`lost`) moves every answer only *downward* in the grade order (`grounded`→`sealed`→`attested`) or widens typed unknowns — never silently flips an accepted value. **This holds iff seals satisfy the accepted-frontier + defeat-outcomes condition (kb/25 §3):** a `seal(S,R,WinnerCKey,WinnerVal,DefeatedCKey)` must record, for its region R, (a) the **accepted-value frontier** — every claim key that won under the region's policy, with value — and (b) enough **defeat outcomes** — every `DefeatedCKey` that lost — that a surviving loser cannot win *by attrition* after the winner's raw evidence is purged. R13 then makes the seal **outrank raw survivors** for the sealed scope (`accepted` head, with `not superseded_after_seal`). Without hypothesis (b) the theorem is false: purge the winner, and R5's `conflict` no longer fires, so a stale loser silently flips to `accepted` — exactly the resurrection-by-attrition failure. The amnesia drill (kb/25 §8) exists to catch violations of (b). **[M] flag:** the *sufficiency* of (a)+(b) for all policy shapes (e.g. specificity-clipping across a region boundary) is unproven — the honest open edge of this candidate.

**T4 — No-surprises / consistency for what-if (if expressible). [F]/[M].** WhatIf = evaluate R1–R15 over `base ∪ Δ` where Δ = hypothetical `assrt`/`dec`/policy rows under sandbox context `C'`. The consumed-set of a what-if answer is the read-set (kb/19 §2 = Worlds' read-set). *No-surprises* (Worlds): re-reading a computed what-if value returns the same value ⇒ holds trivially, R1–R15 are deterministic per `ctx`. *Consistency* (Worlds' commit check): promotion of Δ to base is safe iff the read-set is unchanged in base at commit time — expressible as **"the consumed-set of every promoted tuple re-derives identically over the advanced base."** This is a **read-set equality check**, computable from `just` annotations — Datalog can *state* the check but the abort/merge *decision* on failure is policy (kb/19 §3: Worlds-abort vs PIE-merge), not a rule. See §6 probe: sprout/commit is delta-relations + read-set check; promotion-on-conflict is the one piece that reaches past Datalog into a governance choice.

---

## 4. Metacircularity check

Policies and contexts are **facts in the same relations the rules consult** — `assrt` rows whose predicate is a policy stance, pinned by `DefCut`. The kernel describes itself (kb/20 §4): a decision is an `assrt` about an `assrt`; an ontology/policy definition is an `assrt` about a predicate-entity; a context is an entity with `assrt` parameters. One schema, every meta-level.

The danger: a rule reading policy facts could read a policy row *later than the pin*, reopening a negation cycle. The guard pattern — **`definition_cutoff` as a subgoal** on every policy read:
```
prefers(Policy,B,A) :- policy_row(Policy,Rank,PredScope,DefTx),   -- the strainer rule, stored as data
                       ctx(C,_,DefCut,…), DefTx ≤ DefCut,          -- ← the guard: only policy rows within the definition cutoff
                       ranks_below(A,B,Rank,PredScope).
```
Because `DefTx ≤ DefCut` restricts policy rows to a fixed transaction-time prefix, and I6 keeps *evidence* decisions targeting the past, **reading policy-as-data adds strata (aboutness depth) but never a negation cycle** (kb/11 §3: "a decision hierarchy just adds strata"). The program stays locally stratified with policies, contexts, and the ontology all living inside the same six relations it queries. Metacircular *and* stratified — by the same I6/DefCut construction that stratifies evidence. **[S]**

---

## 5. The week test — build plan

This candidate should win the week outright: **the page compiles to SQL views essentially as written** — no engine to build, only rules to transcribe. Prior art is a live differential oracle (Clingo offline; kb/11 §4).

- **Day 1 — Schema.** Create the 6 tables in SQLite (kb/23 §2 is already this). Load the wrong-silo example (kb/22 §7). INSERT-only grants. **[S]**
- **Day 2 — Rules R1–R8 as views.** `visible`, `defeated`(R2–R5 stubbed on a hand-listed `conflict`), `effective`, `admitted`(R7–R8). Reproduce accepted/defeated for the three contexts of kb/22 §7. **[S]**
- **Day 3 — R9–R10 + total tiebreak.** Threshold admission; `accepted`; conflict-strainer pipeline with ledger-sequence tiebreak; `contested` output. Pass the kb/22 §7 acceptance table byte-for-byte. **[S]**
- **Day 4 — R11–R14: grades + coverage + I8.** `cov`/`seal` tables; grade views; R13 seal re-entry; R14 coverage-licensed negation. **[S]**
- **Day 5 — Amnesia drill.** Seal-and-purge a region; assert T3 monotone degradation (no silent flips; `absent_closed`→`unknown` where coverage falls). This is the theorem's empirical shadow. **[S]**
- **Day 6 — Metacircular + federation.** Move one policy into `assrt`, add the `DefCut` guard (§4); add `imported`/`watermark` + R15. Show a policy change is a new pinned version, not an edit. **[S]**
- **Day 7 — What-if + differential.** Δ-relation evaluation (§3 T4); read-set check from `just`. Diff the whole engine against the Clingo spec-oracle over a sampled ledger (kb/11 §4). **[F]** on promotion policy.

By day 3 the candidate has passed the canonical acceptance test; days 4–7 add the harder rungs. No other candidate ships a runnable belief view on day 2.

---

## 6. Self-criticism (ruthless)

Bare Datalog strains in four named places; the honest question is whether the escapes stay on one page.

- **Value computation (blend math, scoring). [F]→escape.** R1–R15 decide *which* assertions are accepted; they do **not** compute a `massEstimate` from a fill level and a shape. Derived *values* are produced by **rules-as-opaque-functions** — a `RuleVer` in `just` names a computation the page treats as a black box (`expression` value kind, kb/23 §3). Datalog carries the *dependency edges*; the arithmetic lives outside. This is honest (the belief engine was never meant to do physics) but it means **the page is a belief calculus, not a value calculus** — a real scope line, not a hollowing one, *provided* the opaque functions are pure and versioned.
- **Aggregation / argmax-per-key. [M]→the sharpest strain.** The conflict strainer "highest confidence wins, then latest tx" is an **argmax per (Subj,Pred,valid-overlap)** — and argmax is *not* expressible in pure stratified Datalog¬. It needs an aggregation extension (`agg`/`min`/`max` over a group, as in Soufflé, LogicBlox, or SQL's `ROW_NUMBER() OVER (PARTITION BY … ORDER BY …)`). **Verdict:** stay one page by adopting **stratified-Datalog-with-aggregation** (aggregates on lower strata only — standard, deterministic, still no solver) and admitting it in the schema line. If transcribed to SQL this is free (window functions); on a pure Datalog engine it is one declared extension. *This is the candidate's most honest wound:* the seductive "just Datalog" story is really "Datalog + monotone aggregation," and pretending otherwise is the failure mode.
- **Consumed-set annotation — honest Datalog or bolted semiring? [M].** Carrying the *set of base tuples used* on each derived tuple is why-provenance (kb/11 §6) — the free, honest version (a set-valued annotation = the Why(X) semiring, which is idempotent and needs no polynomials). It is **honest Datalog** *as long as* we only ever ask "which inputs" (set union/intersection). The moment anyone wants *how many ways* or *probability*, that is the N[X] polynomial semiring — a **bolted-on** structure the page does not carry and should not pretend to. Position: annotate with sets, refuse polynomials, say so out loud. **[S]** for sets; **[M]** if probabilistic scoring ever becomes real.
- **Quantities / intervals. [F].** Valid-time *clipping* (specificity supersession, R3) is interval arithmetic — `conflict` and effective-interval computation need `overlaps`/`during`/interval-difference, which pure Datalog lacks. Practically these are built-in comparison predicates (range types in SQL, interval library on a Datalog engine); conceptually they are another **opaque-function** concession. Small, but real: the page assumes an interval algebra it does not itself define.

**What the page pushes out to "rules as opaque functions":** (1) derived-value arithmetic, (2) interval overlap/difference, (3) confidence scoring. **Does this hollow the candidate?** No — *if and only if* those functions are pure, versioned, and appear in `just` (so `why()` stays total and determinism survives). The candidate's spine — *which claims are believed, under which context, at which grade* — is fully on the page in honest stratified Datalog(+agg). The arithmetic was never the kernel; it was always a stance. The candidate is hollowed only if someone needs **defeat defined over acceptance** (kb/11 Claim 2 threat 1) or **solver-search for incomparable belief views** — both of which I6 + total-tiebreak + `contested`-as-output structurally forbid. That forbidding is the whole point.

---

*Every load-bearing element is [S]: the six relations, R1–R15's spine, the tx-order stratification lemma, the seal anti-attrition condition, the metacircular DefCut guard. The [M] elements — T3 sufficiency across all policy shapes, argmax-as-aggregation-extension, semiring honesty, what-if promotion — are named, not buried, and none carries the load alone.*
