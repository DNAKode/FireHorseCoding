# THE PAGE (v0) — Gneiss's Candidate Maxwell Equations

*Phase M, Wave 2 unification. Composed 2026-07-05 from the five candidates (kb/maxwell/CANDIDATE-A..E) per kb/50 §3. **E is the normative core** (schema + stratified rules); **D supplies the surface and the metacircular bottom** (verb grammar + `bootCtx`, and the label-IS-consumed-set definition); **A the engine shape + one theorem** (order-fixed left fold; rung equivalence by fold-fusion); **B the proof frame** (three orders, α/γ sealing, Cousot degradation — corollaries, not machinery); **C is salvage only** (federation `contested` diagnostic, commit-as-descent remark, the I8 rhyme). Tags: **[S]** settled/load-bearing · **[F]** frontier (needed, not yet proven) · **[M]** speculative. Nothing [M] is load-bearing. Bare absolutes only at ceremony boundaries (§CEREMONY).*

---

## THE ONE PAGE

**(a) SCHEMA** — 6 append-only relations; everything else is derived. **[S]**
```
tx(TxId, Wall, Actor, Reason, Batch)                                    -- write envelope; TxId totally orders one ledger (I3′)
assrt(Aid, TxId, Subj, Pred, Val, VFrom, VTo, Status, Src, Meth, Conf, CKey)  -- immutable claim; Status∈{fact,proposed}; CKey = claim-key
just(Aid, InAid, RuleVer, Role)                                         -- support edge: Aid grounded in InAid and/or a versioned rule
dec(Aid, Kind, TgtAid, TgtCKey, ByAid)                                  -- decision (itself an assrt); Kind∈{accepts,rejects,retracts,supersedes,invalidates_src,redacts}
cov(Region, Scope, State, SealAid)                                      -- coverage map; State∈{full,sealed,archived,purged,lost,suspect}
seal(SealAid, Region, WinnerCKey, WinnerVal, DefeatedCKey)             -- certified frontier: accepted winners + defeat outcomes (kb/25 §3)
ctx(C, DataCut, DefCut, ConfPolicy, PrecPolicy, ClosurePolicy, AdmitPolicy, StopRung)  -- a context is a fact row; every pin is a column
```
**Claim-key** `CKey = (Subj,Pred,valid-slice)` OR a target Aid. **Envelope** = `(TxId,Actor,Reason,Wall)`. Policies are `assrt` rows (predicate = stance), pinned by `DefCut`. **[S]**

**(b) VERB GRAMMAR** — the interface layer; `record/decide/declare/seal/purge/sprout/commit/import` build the ledger, `ask` is the belief engine (D). **[S]**
```
record  e claim         = L ⊕ tx(e, assrt(claim))            declare e name defn  = L ⊕ tx(e, defn-as-assrt)   -- policy|context|predicate|closure|rule
decide  e tgt verdict   = require tgt.tx < now(L); L ⊕ tx(e, dec(tgt,verdict))    -- I6: targets strictly-lower tx ⇒ acyclic
seal    e region summary= require covers(summary.frontier,region); L ⊕ tx(e, seal(region,summary))
purge   e region receipt= require sealed(region,L); L ⊕ tx(e, cov(region,purged,receipt))   -- else honest state is `lost`
sprout  ctx             = world(base=L, ctx, reads=∅, writes=[])          commit world = if base⊨world.reads then base⊕world.writes else ABORT(world inspectable)
import  e Lf mark       = L ⊕ [tx(e, assrt(a, Src=Lf, W=mark)) | a ∈ prefix(Lf,mark)]      ask q ctxRef = render(q, believe(L, resolveCtx(ctxRef,L)))
```

**(c) RULES** — E's core, extended with grade + coverage-guarded negation; `C` free in every head ("per context"). **[S]** unless tagged
```
R1  visible(A,C)       :- assrt(A,Tx,…), Tx ≤ C.DataCut.                        -- on the table iff its tx is within the data cutoff
R2  defeated(A,C)      :- dec(D,retracts,A,_,_),   effective(D,C).              -- retraction in force
R3  defeated(A,C)      :- dec(D,supersedes,A,_,_), effective(D,C).              -- supersession in force
R4  defeated(A,C)      :- envsrc(A,S), invalidated(S,A.VFrom,C).                -- source/method invalidated over A's valid-time
R5  defeated(A,C)      :- conflict(A,B,C), prefers(C.ConfPolicy,B,A).           -- lost a conflict under the strainer pipeline
R6  effective(D,C)     :- visible(D,C), not defeated(D,C).                      -- a decision fires unless itself defeated (targets earlier — I6)
R7  admitted(A,C)      :- assrt(A,…,fact,…).                                    -- facts admitted outright
R8  admitted(A,C)      :- assrt(A,…,proposed,…), dec(D,accepts,A,_,_), effective(D,C).  -- hypothesis accepted by decision
R9  admitted(A,C)      :- assrt(A,…,proposed,…,Conf), allows_unreviewed(C.AdmitPolicy), Conf ≥ threshold(C.AdmitPolicy).  -- [F] auto-admit, badged
R10 accepted(A,C)      :- visible(A,C), admitted(A,C), not defeated(A,C).       -- THE belief view: visible, admitted, undefeated
R11 grade(A,grounded,C):- accepted(A,C), region_of(A,R), cov(R,_,full,_).       -- recomputable from surviving raw
R12 grade(A,sealed,C)  :- accepted(A,C), region_of(A,R), cov(R,_,sealed,S), certifies(S,A). -- recomputable only via a seal
R13 accepted(A,C)      :- seal(S,R,CK,V,_), region_in_cut(R,C), claim(A,CK,V), not superseded_after_seal(A,C).  -- sealed winners outrank raw survivors (anti-attrition)
R14 absent_closed(K,C) :- not some_accepted(K,C), closed(K,C.ClosurePolicy), region_of(K,R), covered_for_closure(R,C).  -- I8: confident "no" ONLY under positive coverage
R15 visible(A,C)       :- imported(A,Lf,W), watermark(C,Lf,Wc), W ≤ Wc.         -- [F] remote ledger is a source, visible to its pinned watermark
```
Outputs, all first-class: **accepted**, **defeated**(+defeater), **contested** (R5 conflict where `C.ConfPolicy` stops before its total rung), **typed-missing** (R14 + absence enum). **[S]**

**(d) EVALUATION DISCIPLINE** — the whole engine in four lines. **[S]**
```
1  believe(L,C) = foldl(step, ∅, orderByTx(visible(L,C)))   -- an order-fixed LEFT fold in tx order (A: "monoid" is a polite fiction — order is load-bearing)
2  Strata = tx order. By I6 every dec targets strictly-lower TxId ⇒ the targets graph is a DAG ⇒ locally stratified through time (I6, THE load-bearing lemma) ⇒ belief is a fold, not a search.
3  contested is an OUTPUT, never search: each ConfPolicy terminates in a total tiebreak (…,Conf, then TxId) or STOPs → contested. Unique perfect model. Full recompute is the oracle; every optimization equals it byte-for-byte.
4  The one declared aggregation extension: the conflict strainer is argmax-per-CKey — NOT pure Datalog¬ (E's confession). Adopt stratified-Datalog-with-aggregation (lower strata only; free as SQL `ROW_NUMBER() OVER (PARTITION BY CKey ORDER BY …)`).
```

**(e) LABEL** — *the label IS the consumed-set of the evaluation* (D's move — a definition, not an instrument). **[S]**
```
LABEL(answer) ≝ ⟨ C.version(=DefCut), high-water TxId, consumed = {base tuples the fold read} ∪ {coverage regions} ∪ {watermarks}, grade ⟩
```
This one artifact is simultaneously Worlds' read-set, the verifying-trace, and `observedGeneration` (kb/19 §2). Every answer carries it; every missingness path emits it. `why(A)` = walk `consumed` via `just` to observations/decisions/rule-versions/seals.

**(f) bootCtx** — the metacircular bottom (D). **[S]**
```
resolveCtx(ref,L) = materialize(ref.name, believe(defnSpace(L) ↾ ref.DefCut, bootCtx))   -- a context is itself a declared term, folded by the same evaluator
bootCtx = a FIXED, tiny, non-self-referencing base context (open-world, decided-only admission, tx-tiebreak) that is NOT declared in the ledger.
```
It bottoms out the policy regress: user contexts are folded metacircularly, but the interpreter-of-contexts terminates in one primitive context that cannot refer to itself. "Lisp's `eval` in a base Lisp you already trust." `DefCut` pins the policy *version* to a strictly-earlier prefix, so no feedback loop between an answer and the policy that computed it.

---

## THE ADQ BOX — the named open problem of the kernel  **[F, load-bearing → stated as OBLIGATION, not theorem]**

> **The seal-content adequacy condition (ADQ) is an obligation the kernel IMPOSES, not a theorem it PROVIDES.**
>
> Monotone degradation (no silent value flip under purge) holds **iff** every seal captures, for its region: (a) the **accepted-value frontier** — each winning CKey with value — and (b) enough of the **defeat record** that every surviving raw loser stays defeated. Under ADQ, R13 makes the seal outrank raw survivors and degradation is Cousot-sound (B's T3). Without ADQ, purge the winner and R5's `conflict` no longer fires — a stale loser wins *by attrition*, a silent flip: exactly the sin the whole system forbids.
>
> In B's frame, ADQ is the **completeness half of the α/γ pair restricted to the defeat frontier** — resurrection-safety is NOT free from soundness. **Four independent hits name this as THE hardest object on the page: A (doubt 4, "an obligation, not a theorem"), B (ADQ, "a real design obligation on seal content"), E (T3 hypothesis (b), [M] sufficiency-across-policy-shapes unproven), kb/25 §3 ("the single trickiest piece of the whole imperfection design").** The amnesia drill (kb/25 §8) is the test that this content is sufficient. This is not softened, not solved, and is the object the mechanization spike (D22) should attack first alongside determinism.

---

## THEOREM BLOCK  *(claim · owner · proof difficulty)*

- **T1 Determinism / unique perfect model.** `believe` is a `foldl` = a function; R1–R15 are locally stratified through time (I6) ⇒ unique perfect model, poly-time semi-naive (Przymusinski/Van Gelder). Owner: **E + D** (fold purity). *Easy* [S]; caveat — needs float/hash-order discipline for bit-identity (A T1).
- **T2 Rung equivalence.** L0 recompute ≡ any incremental rung, by **fold-fusion** `foldl f e (xs⊕[x]) = f (foldl f e xs) x` (Bird–Meertens; = semi-naive ≡ naive). Owner: **A** (crown jewel), E's freebie. *Easy* for the statement; *medium* once `step` touches many keys/tx (per-*affected*-key, not per-written-key — A's cost caveat) [S].
- **T3 Monotone degradation.** Forgetting moves belief only DOWN the information order (weaker grade / wider typed-unknown), never sideways — Cousot soundness of α, **GIVEN ADQ**. Owner: **B** (α/γ one-liner). *Framing easy; real content is ADQ* [F]. The one theorem gated on the open problem.
- **T4 No-surprises + consistency (sprout/commit).** A sprout never asserts what its base did not license; `commit` succeeds iff the read-set re-derives identically over the advanced base, else atomic abort (Worlds). C's descent remark: commit is safe **iff the sprout and the moved base glue on the read-set** — the crisp two-piece statement. Owner: **Worlds + C**. *Easy to state, medium to mechanize* — needs the LABEL per-address precise [S/F].
- **T5 Metacircular termination.** `resolveCtx` folds a context that is itself a declared term; terminates by **definition-cutoff pinning** (strictly-earlier prefix) **+ I6 stratification + bootCtx** (non-self-referencing base). Owner: **D**. *Easy given bootCtx sufficiency* — which is [M], unproven (D concealment 5), and non-load-bearing (a wrong bootCtx needs a different base context, not a different kernel).

---

## EXCLUSIONS  *(what the page deliberately does NOT do)*

- **Value computation / interval arithmetic / confidence scoring are opaque functions.** A `RuleVer` in `just` names a pure, versioned black box; the page carries dependency edges, not the arithmetic. **This is a belief calculus, not a value calculus** (E's scope line) — honest iff those functions are pure, versioned, and in `just`.
- **The strainer / typed-value comparability layer is the hidden 40%** (D's confession): "same CKey, incompatible values" presumes numeric tolerance / enum equality / interval clipping (specificity) — six named algorithms behind six words. One line of the page, ~40% of the build.
- **Per-affected-key dirtying cone** (A's doubt 3): range-scoped defeaters (a closure, a source invalidation over a valid-time range) dirty a cone, not one key; L1 is per-*affected*-key. Derived-value staleness through `just` edges (L2) is a second pass, not this fold.
- **Nothing on distribution beyond watermark imports.** No consensus, no global clock, no multi-writer on one ledger (I3′). Cross-ledger order is a watermark belief, never an axiom.

---

## THE WEEK-TEST BUILD PLAN  *(grad student, one week, from this page — E's day-by-day, with D's warning)*

- **Day 1 — Schema + write cases.** 6 tables in SQLite; INSERT-only grants; `record/decide/declare/import` as `L⊕tx(…)`; load the wrong-silo example (kb/22 §7). **[S]**
- **Day 2 — R1–R10 as views.** `visible/defeated/effective/admitted/accepted`; ship a **runnable belief view** and reproduce accepted/defeated for three contexts. No other candidate ships belief by day 2. **[S]**
- **Days 2–3 warning (D):** the conflict strainers eat a day — `defeatedBy`/`loses`/specificity-clipping/recency are small algorithms compressed into one line (the hidden 40%). Budget for it.
- **Day 4 — R11–R14: grades + coverage + I8 + R13 seal re-entry.** Build a *deliberately ADQ-adequate* seal (winners + defeat frontier). **[S]**
- **Day 4 warning (D):** LABEL plumbing threads `consumed` through every accept/defeat/missingness branch — mechanical but pervasive, ~a day. **[S]**
- **Day 5 — Amnesia drill (kb/25 §8): the day the candidate lives or dies.** Random seal-and-purge; assert T3 (grade only descends, no value flips, `absent_closed`→`unknown` where coverage falls). **[S]**
- **Day 6 — Metacircular + federation.** Move one policy into `assrt` with the `DefCut` guard; add `imported/watermark` + R15; show a policy change is a new pinned version, not an edit. **[S]**
- **Day 7 — What-if + differential.** Δ-relation sprout/commit with the read-set check from `just`; diff the engine against a Clingo spec-oracle (kb/11 §4). Promotion-on-conflict is a governance choice, not a rule. **[F]**

---

## DIVERGENCES FROM THE CANDIDATES  *(overrides of the settled composition, with cause)*

1. **LABEL promoted from decoration to definition (§(e)).** A models it as Writer-monad decoration, B refuses it "a seat as a sixth order" and calls it receipts. The mandate follows D: the label *is* the consumed-set. I state it as `≝`, a definition on the page, per the mandate's explicit instruction ("make that a definition, not an instrument bolted after"). Cause: D placed it most natively and the checkpoint (kb/50 §4) confirms the consumed-set resists being *structure* in A and B — so it is honest to define it as the evaluation's read-set, not to derive it from an order or a monad. B's "it is not itself an order" is preserved as true and non-contradictory: a definition need not be an order.
2. **B demoted from page machinery to a theorem block (§THEOREM).** Per B's own verdict ("the orders are the correctness argument, not the machine") and the mandate. The five orders and the two Galois pairs appear *only* as owners of T3/T4, never as carriers on the page. Cause: the page must be the executable kernel (C1); order vocabulary is proof scaffolding a grad student needs to *know* the code is right, not to *write* it.
3. **C reduced to three lines and a diagnostic, zero sheaf vocabulary on the page.** `contested` appears as an ordinary rule output (R5), not H¹; the federation `contested` diagnostic and the commit-as-descent remark live in T4's gloss and the exclusions frame; the I8 rhyme is noted once (R14's "positive coverage" = C's "global sections need covers"). Cause: C's own refutation clause — the total order kills gluing on the spine; the sheaf is a lens for two corners, not the kernel.

**Possible over-discard (dissent, per mandate §7):** the composition drops C's **cellular-sheaf federation diagnostic** (the pairwise-disagreement 1-cochain over the federation nerve) below even a frontier mention on the page — it survives only as a word in T4. C rated it a genuine, implementable, operator-useful tool for the *one* real non-chain cover. I judge the demotion correct for a ≤66-line page (federation is [F], off-spine, and the diagnostic is a build-time tool, not a kernel law), but I record that the unification spends nothing on the single corner where C's machinery was actually earned — if federation becomes load-bearing before the mechanization spike, that diagnostic is the first thing to promote back. Similarly, A's observation that **two contexts share no work** (the ATMS-label refusal) is correct and excluded by design, but it is the honest cost the page never prices; a real multi-context deployment will feel it.
