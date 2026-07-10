# Candidate A — THE FOLD

*Phase M, Maxwell hunt. Formulation A of five. Center of gravity: operational/algebraic.
Thesis: **a belief view is a catamorphism (fold) over the ledger monoid, parameterized by a
context; every other operation is a law of that fold.** Prior art: event-sourcing fold+snapshot
(Marten/Equinox), TeaTime `view = f(snapshot, ordered messages)`, Bird–Meertens fold-fusion,
Worlds sprout/commit. Tags: [S] solid · [F] frontier · [M] moonshot. Nothing [M] is load-bearing.*

---

## 1. THE ONE PAGE

**Carriers.** `[S]`
```
Tx        a transaction: (tid, actor, reason, {assertions}, wall)         -- tid total-orders
Ledger    L : [Tx]         the free monoid (Tx*, ++, [])   -- append is  L ++ [tx]
Key       k = (subject, predicate, valid-slice)  OR  a target assertion-id  -- the claim-key
Cell      per-key state:  { visible[], winner|contested|∅, defeated{a→defeater} }
S         View state:  Map Key Cell     -- the fold's carrier (a commutative-merge map)
Ctx       C : policies-as-data (data_cutoff, def_cutoff, precedence, conflict,
              admission, closure, missingness) — itself ledger content, pinned by tid
Trace     R : consumed-set = { addresses read } ∪ { coverage regions } ∪ { watermarks }
```

**RECORD** — append (the monoid operation). `[S]`
`record(L, tx) = L ++ [tx]`  with idempotent id `tid = hash(content, source, batch)` (kb/25 §5).

**VIEW** — the fold under a context.  `[S]` the whole kernel is this line:
```
view(L, C)  =  foldl (stepC) ∅ (visible(L, C))          -- ∅ = empty Map
visible(L,C) = [ tx ∈ L | tx.tid ≤ C.data_cutoff ]      -- prefix selection
stepC : S → Tx → S      -- per-key: apply each assertion/decision in tx to its Cell,
   admit(status,C) · defeat(decisions,C) · resolve(conflict_policy C)   (the 6 rules, kb/22 §1)
```
Well-defined left-to-right because **I6** makes decisions target strictly-lower tid ⇒ the target
graph is acyclic ⇒ evaluation stratifies by tid ⇒ *belief is a fold, not a search* (kb/22 §2). `[S]`
Output of each `Cell`: accepted / defeated(+defeater) / contested — all first-class (kb/22 §1).

**LABEL** — the consumed-set every answer carries.  `[F]`
Run the fold in the **Writer monad** over trace-monoid `R`: `stepC` also emits which addresses,
coverage regions, and watermarks it read. `view⁺(L,C) = (S, R)`. `R` **is** the label — the one
artifact that is simultaneously Worlds' read-set, the verifying-trace, and `observedGeneration`
(kb/19 §2). Answers carry `(C.version, high-water tid, R, grade)`.

**SPROUT / COMMIT** — what-if.  `[S] sprout / [F] commit`
```
sprout(L,H)   = a fold over the hypothetically-extended prefix  L ++ H     (H = hypo txs)
whatif(L,H,C) = view⁺(L ++ H, C)                                            -- falls straight out
commit(child→L): let R = child's consumed-set;  admit iff  read-values(R, L_now) = read-values(R, L_at_sprout)
                 else abort atomically, child stays inspectable            -- Worlds' rule (kb/19B §4)
```

**SEAL / PURGE** — forgetting.  `[S] seal / [F] purge-faithfulness`
A seal is a **fold checkpoint = homomorphic image** of a prefix. Fold-fusion:
`view(L1 ++ L2, C) = resume( seal(L1,C), L2 )`. So the carrier after `L1` *is* a valid seal.
```
seal(L1,C)  = (S₁ = view(L1,C), merkle(L1))     -- accepted-value frontier + defeat outcomes + root
purge(L1)   = replace raw L1 by seal; fold resumes from S₁; coverage[L1] := sealed(H)   (kb/25 §3)
```
Faithfulness law (anti-resurrection): a purge is legal only if `resume(seal(L1),L2) = view(L1++L2)`
for all `L2` — i.e. the seal outranks surviving raw losers (kb/25 §3). `grade ∈ {grounded,sealed,attested}`.

**IMPORT** — federation.  `[S]`
Each ledger is its own monoid with its own sequencer (**I3′**). Import = fold over an interleaved,
watermark-labeled stream: `import(L, Bstream) = L ++ [ asImportedEvidence(b, watermark(B)) | b∈Bstream ]`.
A remote ledger is just a *source* with reliability + watermark; watermarks join `R`. Convergence is a
belief (a closure/watermark assertion), never a global order (kb/25 §5).

---

## 2. NEEDS: GENERATED vs BOLTED

| Need | Verdict | How it emerges |
|---|---|---|
| Recording | **generated** `[S]` | `record` = the monoid's `++`. Nothing added. |
| Deterministic views | **generated** `[S]` | `view` = `foldl` over a totally-ordered prefix; same (prefix,C) ⇒ same S by construction of `foldl`. |
| Contexts / what-if | **mostly generated** `[F]` | `sprout`/`whatif` = fold over `L ++ H` — free. The **commit consistency check is a bolted-on procedure** (compare-and-abort); it is *cheap* only because Label already carries the read-set. |
| Forgetting + monotone degradation | **generated w/ a proof obligation** `[F]` | seal = fold checkpoint (fusion). Degradation = seal faithfulness. The obligation "seal captures defeat outcomes" is real machinery the fold does not give you for free — it constrains seal *content*, not the fold. |
| Federation | **generated** `[S]` | merge of monoids → one labeled stream → same fold. Watermarks are ordinary evidence. |
| Consumed-set / labels | **generated** `[F]` | Writer-monad instrumentation of the *same* fold. The label is `R`; it needs per-key precision to be useful (see §6). |

Honest count: **4 generated, 2 generated-with-a-bolt** (commit rule; seal-content obligation).

---

## 3. THEOREM SKETCHES (each a lemma of the fold)

**T1 — Determinism.** `[S]` For fixed `(visible(L,C), C)`, `foldl stepC ∅` returns one `S`.
*Proof:* `foldl` is a function; `stepC` is a function (conflict policy totalizes to a tiebreak or
emits `contested`, kb/22 §2). Nondeterminism is quarantined at the I/O boundary (TeaTime discipline).
**Difficulty: easy.** Caveat: needs float/hash-order discipline to be *bit*-identical (kb/19B §3).

**T2 — Rung equivalence (the crown jewel).** `[S→F]`
`L0` (recompute `view(L,C)`) `≡` `L1` (incremental per-key `stepC` on each new tx). *Proof:* this is
**fold-fusion / catamorphism uniqueness** (Bird–Meertens): `foldl f e (xs ++ [x]) = f (foldl f e xs) x`.
L1 *is* the right-hand side; L0 the left. They are equal by the definition of `foldl`. **Difficulty:
easy for the exact statement; medium** once `stepC` touches many keys per tx (must show `stepC`'s
per-key writes commute with the `Map` merge — true because keys are disjoint targets). This is the
rung-equivalence theorem the corpus wanted, delivered as a one-line fold law.

**T3 — Monotone degradation under seal-respecting purge.** `[F]`
For a purge legal under §1's faithfulness law, `view` moves answers only to weaker grades or wider
typed-unknowns; never a silent value flip (kb/25 §4). *Proof obligation, stated as hypothesis:*
> **Resurrection-by-attrition hypothesis:** if the seal `S₁` records, per key, the winner *and*
> enough defeat outcomes that every surviving raw loser stays defeated, then `resume(S₁,L2)`
> equals `view(L1++L2)` on accepted values and only widens unknowns where coverage fell below I8.

*Proof:* reduces to seal faithfulness (§1) + I8 (`absent_closed` needs positive coverage).
**Difficulty: medium-hard** — the whole risk lives in "enough defeat outcomes"; the amnesia drill
(kb/25 §8) is the test that this content is sufficient. This is honestly the shakiest theorem.

**T4 — No-surprises + consistency for what-if.** `[S]` (inherited verbatim from Worlds, kb/19B §4)
*No surprises:* once `R` memoizes a read in the child fold, later parent changes to that address are
invisible in the child. *Consistency:* `commit` succeeds iff every address in `R` has the same value
in `L_now` as at sprout; else atomic abort. *Proof:* both are properties of the read-set `R`, already
stated machine-checkably by Warth et al. **Difficulty: easy to state, medium to mechanize** (needs
`R` to be per-address precise — same precondition as Label).

---

## 4. METACIRCULARITY CHECK  `[S]`

Yes. Contexts and policies are **ledger content** (assertions about context-entities and
policy-entities, kb/24 §1–2). The fold reads them from the same prefix it folds: `C` is recovered by
`view(L, C₀)` restricted to the policy subset, then used as the parameter of the main fold —
`def_cutoff` is exactly "which tid-prefix of the definition subset to fold first." So the fold
**interprets its own governing contexts**: one `stepC`, applied first to policy-keys to build `C`,
then to evidence-keys under `C`. Coverage maps, seals, watermarks are likewise ordinary assertions
the fold reads (kb/25 §1). The regress stops for the same reason as the kernel's (one schema at every
meta-level, I5): the fold over meta-assertions uses the *same* `stepC`. Two-pass, not two engines.

---

## 5. THE WEEK TEST  `[S]`

**Built in 5 days from this page:** `Tx`/`Ledger`/`Cell`/`S` types; `record` (append + idempotent
hash); `stepC` implementing the six acceptance rules with a totalizing conflict pipeline; `view` as
`foldl`; the Writer-monad Label producing `R`; `whatif` (fold over `L++H`); `seal`/`resume` with the
fusion equality; a property test asserting **T2** (L0≡L1) byte-for-byte on random ledgers; the kb/22 §7
wrong-silo three-context table as the acceptance oracle.

**Where they get stuck:** (a) **seal content** — deciding exactly what defeat outcomes `S₁` must carry
so T3 holds; the page states the obligation but not the schema (D13). (b) **claim-key identity** —
whether a decision targets an assertion-id or a `(type,subject,object,predicate)` key across rebuilds
(kb/22 open-Q2); this decides whether decisions survive purge by construction. (c) **commit semantics
under a moved base** — abort-atomically (Worlds) vs merge-as-layer (PIE); the page picks Worlds but
the choice is a context flag (kb/19 §3). (d) making Label per-*address* precise rather than per-tx.

---

## 6. SELF-CRITICISM (ruthless)

- **The monoid is a polite fiction where it matters most.** `Tx*` is a free monoid, but `stepC` is
  **not** a monoid homomorphism into a *commutative* structure — order matters (I6, decisions target
  the past). So the clean "free monoid ⇒ everything folds" story is really "free monoid + a
  left-fold that is only associative because tid-order is fixed." Reordering breaks it. The algebra
  is `foldl`, not `foldMap`; I cannot parallelize the fold across the tid axis without the seal
  machinery. **This is the weakest joint.** `[F]`
- **Per-key locality strains the fold.** Conflict resolution is per-`(subject,predicate,slice)`, so
  the carrier is a `Map`, and cross-key interactions (a closure declaration defeating many keys; a
  source invalidation with a valid-time range) make `stepC` touch an unbounded key-set per tx. T2's
  "keys commute" holds, but the *cost* model of L1 (touch only the new key) is a lie for range-scoped
  decisions — they dirty a cone. Honest: L1 is per-*affected*-key, not per-*written*-key. `[F]`
- **Commit is bolted on.** Sprout falls out; commit does not — it is a compare-and-abort procedure
  riding on Label. If Label is imprecise, commit is unsound. Worlds needed a VM primitive; we need a
  precise `R`. Not free. `[F]`
- **Seal faithfulness is an obligation, not a theorem.** T3 is the one place the formulation *assumes*
  a design (seal captures defeat outcomes) rather than *deriving* it. If that content is wrong,
  monotone degradation fails silently — the exact sin the whole system forbids. `[F]`
- **Explicit exclusions:** (1) structure/graph search over the ledger (Halasz #1, kb/19 §4.1) — the
  fold answers per-key queries, not "find circular support chains"; that is a separate traversal.
  (2) The collaboration/awareness social layer (kb/19 §4.2) — invisible to the fold. (3) Derived-value
  dirty-propagation through justification edges (L2, kb/22 §6) — the fold gives per-key acceptance,
  not the transitive staleness cone; that is a second pass over the justification DAG, not this fold.
  (4) Rich decision workflow state machines (kb/20 §7 falsifier) — if decisions need more than
  target+kind, "decision = assertion folded by stepC" becomes convention.
- **Where context-dependence strains it:** two contexts = two folds over the same prefix. Nothing
  shares work between them; the formulation has no story for "compute many contexts cheaply" (that is
  ATMS's exponential label, which kb/22 §3 explicitly refuses). Fine philosophically, costly in
  practice. `[F]`

---

## 7. TAG LEDGER

Load-bearing on the page: RECORD `[S]`, VIEW/fold `[S]`, I6-stratification `[S]`, T2 fold-fusion `[S]`,
metacircular two-pass `[S]`, federation-as-fold `[S]`. Frontier (needed, not yet proven): LABEL
precision `[F]`, COMMIT rule `[F]`, seal faithfulness / T3 `[F]`, L1 cost model `[F]`. **Moonshot [M]:**
none load-bearing — the only `[M]`-adjacent idea (share work across contexts) is explicitly *excluded*,
not relied on.
