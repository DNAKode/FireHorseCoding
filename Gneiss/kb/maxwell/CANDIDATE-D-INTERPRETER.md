# Candidate D — The Metacircular Interpreter

*Phase M, one of five competing Maxwell-page formulations. The LISP-page move: the kernel is a tiny term language plus an evaluator written so the evaluator can interpret its own configuration. McCarthy's `eval`/`apply`, but for testimony and belief instead of computation. Prior art: Lisp 1.5 eval/apply; Kay's 1972 one-page bet; Smalltalk-72's six sentences; kb/22's six rules as the evaluator's case analysis.*

**The thesis in one breath [S]:** Gneiss's verbs are the forms of a term language; `record`/`decide`/`declare`/`seal`/`purge`/`sprout`/`commit`/`import` build the ledger, and `ask` *is* the belief engine — it evaluates a query by folding the ledger under a context. The metacircular payoff: the context and policies `ask` folds *with* are themselves `declare`d terms in the same ledger, read by the same evaluator. The page below is the interpreter; the category theory (tagged [F]) is optional and stays off the page.

---

## 1. THE ONE PAGE

Grammar (the forms are the verbs). `⟨…⟩` = tuple; `[…]` = list; `{…}` = labelled set; `|` = alternation.

```
term  ::= (record  env claim)          -- append evidence/hypothesis; env carries source,method,actor,reason
        | (decide  env target verdict) -- append a decision; verdict ∈ {accepts,rejects,retracts,supersedes(by),invalidate(src,range)}
        | (declare env name defn)       -- append a definition: predicate | policy | context | closure | report | rule
        | (seal    env region summary)  -- certify a region; summary = ⟨frontier, defeats, root⟩
        | (purge   env region receipt)  -- destroy raw of a SEALED region; leaves receipt
        | (import  env ledger mark)      -- fold a foreign ledger's prefix as a source, at watermark mark
        | (sprout  ctx)                  -- open a speculative world over ctx, recording its read-set
        | (commit  world)                -- fold world's writes into base iff read-set still holds
        | (ask     query ctxRef)         -- evaluate: what | why | at t | under ctx | say-it-again
claim   ::= ⟨subject, predicate, value, valid, born⟩   -- born ∈ {fact, proposed}
answer  ::= ⟨grade, value|missing, LABEL⟩              -- graded, typed, labelled (never bare NULL)
LABEL   ::= ⟨ctxVersion, hiWater, consumed:{tx…}, watermarks, coverageGrade⟩   -- the consumed-set IS the label
```

The evaluator (functional; pattern-matching; platform-free). `L` = ledger (append-only sequence). `⊕` = append.

```
eval(term, L) = case term of                                             -- writes: pure ledger transducers
  (record  e c)        -> L ⊕ tx(e, assert(c))                            -- I1 append-only; id = hash(content,e) (idempotent)
  (decide  e t v)      -> require t.tx < now(L);  L ⊕ tx(e, decision(t,v))-- I6: targets strictly-lower tx ⇒ acyclic
  (declare e n d)      -> L ⊕ tx(e, defn(n,d))                            -- policies/contexts/predicates are just terms
  (seal    e r s)      -> require covers(s.frontier, r);  L ⊕ tx(e, sealAssertion(r,s))
  (purge   e r rc)     -> require sealed(r, L);  L ⊕ tx(e, coverage(r,purged,rc))   -- else honest state is 'lost'
  (import  e Lf m)     -> L ⊕ [ tx(e, assertFrom(a, src=Lf, mark=m)) | a ∈ prefix(Lf, m) ]  -- foreign order = evidence
  (sprout  ctx)        -> world(base=L, ctx=ctx, reads=∅, writes=[])      -- speculative; consumed-set recorded live
  (commit  w)          -> if fold(w.base ⊕ nothing, w.reads.ctx) ⊨ w.reads.frontier
                         then w.base ⊕ w.writes else ABORT(inspectable=w)  -- Worlds' read-set check

eval((ask q ctxRef), L) =                                                 -- READ: the evaluator IS the belief engine
  let ctx = resolveCtx(ctxRef, L)                                         -- ★ metacircular: ctx is itself a declared term
      B   = believe(L, ctx)                                              -- the fold (below); returns {accepted, defeated, contested}
  in  render(q, B, ctx.missingness)                                       -- what/why/at/under/say-it-again over B

resolveCtx(ref, L) =                                                      -- ★ read the context by folding definition-space
  let dc  = ref.definition_cutoff                                        -- pins the policy VERSION (see Thm CIRC)
      raw = believe(defnSpace(L) ↾ dc, bootCtx)                          -- bootCtx: fixed, tiny, cannot self-reference
  in  materialize(ref.name, raw)                                          -- context = accepted belief over its own defn terms

believe(L, ctx) =                                                         -- THE FOLD (kb/22 §1, six rules, stratified by tx)
  foldl(step, ∅, orderByTx(L ↾ ctx.data_cutoff))                        -- oldest-first ⇒ decisions settle before targets
  where step(state, tx) =
    for a in tx.items:                                                    -- one pass; no search, no solver (I6 gives this)
      vis      = a.tx ≤ ctx.data_cutoff
      adm      = a.born=fact
               ∨ (a.born=proposed ∧ decidedAccept(a, state, ctx))
               ∨ (a.born=proposed ∧ ctx.admission.auto ∧ conf(a) ≥ ctx.admission.θ ∧ badge(a,'auto'))
      def      = defeatedBy(a, state, ctx)                               -- {retract,supersede,srcInvalid,conflictLoss}
      grade    = coverageGrade(a, ctx)                                    -- grounded|sealed|attested|orphaned (kb/25 §4)
      state    = if vis ∧ adm ∧ ¬def then accept(state, a, grade, consumed=a.tx ∪ def.witnesses)
                 else record_defeat(state, a, reason=def) ⊎ record_contested(state, a)
    sealsOutrankRaw(state)                                                -- kb/25 §3: seal frontier defeats surviving losers

defeatedBy(a, state, ctx) =                                               -- conflict = same ⟨subj,pred,valid∩⟩, incompatible
  firstHit([ decisionRetracts(a,state), decisionSupersedes(a,state),
             sourceInvalidated(a,state,ctx),
             loses(a, rivals(a,state), ctx.conflict_policy) ])           -- strainer pipeline ↓ (kb/22 §4)
  where strain = [ decisionWins, sourcePrecedence, specificity,
                   recency, confidence, tiebreakByTx | STOP→contested ]  -- per-predicate stop rung
```

*Every input file's demanded behaviour is here: append-only record; the acceptance fold with visible/admitted/defeated stratified by tx order (I6); typed missingness in `render`; labels carrying the consumed-set; seal/purge with receipts and coverage grades; sprout/commit with the read-set check; import with watermarks — and `resolveCtx`, which reads the context by the same fold, is the metacircular bottom.*

---

## 2. Needs generated, not bolted [S]

The six needs are not features added to the page — they are what particular `eval` cases *do*, or corollaries of the fold.

1. **Append-only immutable record.** `record`/`decide`/`declare` are the *only* write cases and each is `L ⊕ tx(…)`. There is no `update` or `delete` form to write. Immutability is grammatical, not enforced.
2. **Context-relative belief.** `ask` takes `ctxRef` as an argument and every branch reads it; there is no belief without a context because `believe` has no other closure over policy. The Gneiss Contract `view = f(evidence, coverage, context)` is literally `eval((ask …), L)`'s signature.
3. **Defeasible truth maintenance.** `defeatedBy`'s four clauses are the defeaters (kb/22); acceptance is the least fixed point of the fold, and retraction/re-assertion restores belief because the fold is recomputed, not mutated (AGM postulate as a corollary, not an operator).
4. **Typed missingness.** `render` never emits bare NULL: an absence resolves to a member of the taxonomy (kb/24 §4) chosen by *which fold outcome produced it* — `contested` from the STOP rung, `retracted`/`rejected` from `defeatedBy`, `absent_closed` only when a closure term is accepted AND coverage is `full` (I8), else `unknown`.
5. **Forgetting with ceremony.** `seal`/`purge`/coverage are three forms; `purge` *requires* a prior `seal` (else the honest coverage is `lost`), and `coverageGrade` degrades answers to `sealed`/`attested`. Monotone degradation is a property of the fold (Thm below), not a subsystem.
6. **Federation / distribution.** `import` folds a foreign prefix *as a source* at a watermark, assigning local tx order; `LABEL.watermarks` carries the vector. No consensus, no global clock — a remote ledger is just an argument to `import`.

---

## 3. Theorem sketches [S] (one distinctive [F])

- **Determinism (evaluator purity) [S].** Each `eval` case is a pure function of `(term, L)`; `believe` is a `foldl` over a tx-total-ordered sequence with no wall-clock or hash-iteration dependence. Therefore `ask` is a function of `(L↾cutoffs, ctx)` — same prefix + same context = same graded answer, forever. Nondeterminism is quarantined at the I/O boundary (`now(L)`, external `import`), exactly TeaTime's discipline.
- **Rung equivalence [S].** L0 (recompute `believe` per query), L1 (per-key refold), L2 (dirty-set via justifications) compute the same accepted set because each is a restriction of the *same fold* to a sub-domain; any rung validates against L0 byte-for-byte — incrementality bugs are detectable by construction (kb/22 §6).
- **Monotone degradation [S].** Reducing coverage only changes `coverageGrade` (weaker grade) or removes raw the seal already outranks (`sealsOutrankRaw`); it never changes an accepted *value*, because the seal frontier captures winners-and-defeats so no loser wins by attrition (kb/25 §3). Answers only weaken in grade or widen typed unknowns.
- **No-surprises / consistency for sprout/commit [S].** `sprout` records the read-set (the consumed-set of everything the speculative world folded); `commit` re-folds the base and checks `⊨ frontier`. If the base moved under a consumed tx, the check fails and `commit` ABORTs leaving the world inspectable (Worlds' guarantee, machine-checkable). "No surprises": nothing the world read can have silently changed at commit.
- **★ The metacircular fixed point [S] (distinctive).** `ask` folds a context that is *itself* a `declare`d term folded by the same evaluator. Why this terminates and is safe:
  - **Version-pinning by `definition_cutoff`.** `resolveCtx` reads the context from `defnSpace(L) ↾ ctx.definition_cutoff` — a *strictly earlier prefix* of the ledger. The policy version a context configures `ask` with is frozen at a tx boundary; evaluating `ask` cannot change which policy terms are in scope, so there is no feedback loop between "the answer" and "the policy that computed it."
  - **Stratification by I6.** Definition terms are ordinary assertions; a decision refining a policy targets strictly-lower tx (I6), so `defnSpace` is itself acyclic and folds oldest-first. The context's own definition is settled before any query that uses it.
  - **The safe restriction (the load-bearing constraint).** `resolveCtx` folds `defnSpace(L)` under a **fixed `bootCtx`** — a tiny, closed context (open-world, decided-only admission, tx-tiebreak, no self-reference) that is *not* itself declared in the ledger. This breaks the regress: user contexts are interpreted metacircularly, but the interpreter of contexts bottoms out in one primitive context that cannot refer to itself. This is exactly Lisp's `eval` being written in a base Lisp you already trust; here the base is `bootCtx`. Without this restriction a context could set its own `definition_cutoff` to `latest` and fold itself — the fixed point would be ill-founded. `bootCtx` + earlier-prefix pinning together make the fixed point *least and unique*.
- **[F] Adjunction / graded monad (off the page).** `testify ⊣ believe` reads as an adjunction (free evidence / forgetful belief); grades `{grounded,sealed,attested,orphaned}` form a graded monad indexing `ask`'s return. Stated only to show the page *could* carry more structure; nothing above depends on it, and it earns no line on the one page.

---

## 4. Metacircularity check — worked micro-example [S]

The centerpiece: a context declared *as a term*, then used by `ask`, folded by the same evaluator that the context configures.

```
tx1 (declare env "billing_conflict"  policy:[decisionWins, sourcePrecedence, specificity, STOP→contested])
tx2 (declare env "Billing"           context:{data:latest, defs:≤tx1, conflict→"billing_conflict", admission:decided-only})
tx3 (record  env  ⟨Silo17, fill, 4.2m, [Jun20], fact⟩  src=manual_laser)
tx4 (record  env  ⟨Silo17, fill, 4.9m, [Jun20], fact⟩  src=operator_typed)      -- conflict with tx3, same subj/pred/valid
tx5 (ask (what Silo17 fill @Jun20) "Billing")
```

Evaluating tx5:
1. `resolveCtx("Billing", L)` folds `defnSpace(L) ↾ tx2.definition_cutoff (=tx1)` under `bootCtx`. That fold accepts tx1 and tx2 as definition assertions and *materializes* the context: `conflict_policy = billing_conflict = [decisionWins, sourcePrecedence, specificity, STOP]`. **The policy that `ask` will strain conflicts with was just computed by `ask`'s own fold** — over an earlier prefix, under `bootCtx`. That is the metacircular step, concretely.
2. `believe(L, ctx)` folds evidence ≤ `data_cutoff`. At tx4 a conflict fires: rivals = {tx3, tx4}. `billing_conflict` strains: no decision (skip), source precedence — `manual_laser > operator_typed` per the pinned precedence policy → tx3 wins? No: precedence is declared to *stop* here only if it separates; suppose it doesn't rank these two → next rung specificity (equal intervals) → **STOP → contested**.
3. `render`: `⟨grade=grounded, missing=contested, LABEL=⟨ctxV=tx2, hiWater=tx5, consumed={tx1,tx2,tx3,tx4}, watermarks=∅, coverage=full⟩⟩`. UX surface: *"contested — needs review."* The consumed-set includes **the two definition txs** — the answer's label proves which policy version adjudicated it.

Now change one line — `declare` a **new** context `"Ops"` whose `conflict_policy` adds `confidence` before STOP. Re-ask under `"Ops"`: the *same evidence* now resolves (operator_typed carries lower confidence → tx3 wins), grade `grounded`, value `4.2m`. **No evidence changed; a declared term changed the fold.** That is the metacircular payoff made operational: semantics is data the evaluator reads, not code the evaluator is.

---

## 5. The week test [S]

*Claim: this candidate wins the week — an implementer can build the interpreter from this page in a week — and here is the honest accounting of where the week actually goes.*

- **Day 1 (easy, the arch is real).** The write cases and `foldl` skeleton are a day. `record`/`decide`/`declare`/`import` are `L ⊕ tx(…)`; the six-rule `step` transcribes kb/22 §1 almost verbatim. This part is genuinely one page and genuinely a week's opening morning.
- **Days 2–3 (the real cost: the conflict-policy strainers).** `defeatedBy`/`loses`/the strainer pipeline is where the week goes. "Same ⟨subj,pred,valid∩⟩ incompatible" needs a value-comparability notion (numeric tolerance? enum equality? interval clipping for specificity?). Each strainer rung is a small algorithm; `specificity` needs interval clipping, `recency` needs per-predicate sample semantics. This is not hidden by the page so much as *compressed* into `strain = […]`, and honesty requires saying: **the strainer pipeline is 40% of the implementation effort and one line of the page.**
- **Day 4 (label plumbing).** Threading `consumed` through every `accept`/`defeat` and out into `LABEL` touches every branch. It is mechanical but pervasive — the kind of plumbing that is trivial to design and tedious to get complete (every missingness path must also emit a label). Real, but not deep.
- **Day 5 (seal/commit checks).** `sealsOutrankRaw` and `commit`'s read-set check are subtle but small — each is a predicate over recorded frontiers. The amnesia drill (kb/25 §8) is the acceptance test.
- **Honest verdict.** The page is a true one-page kernel and the fold is a week. But two of those days are the conflict strainers and the label plumbing — the machinery the grammar's tidiness quietly compresses. The week is winnable; it is not *uniformly easy*, and anyone quoting "one page" without pointing at the strainers is selling.

---

## 6. Self-criticism (ruthless) [S]

The danger of an interpreter page is that `eval`'s elegance hides machinery in `…` and in innocent-looking helper names. Audit of my own concealments:

- **Concealment 1 — `assert(c)` and value typing.** `claim = ⟨subject, predicate, value, valid, born⟩` says nothing about what `value` *is*. Comparability (`incompatible`, `conf ≥ θ`, tolerance) presumes a typed-value layer (kb/23) that the page hides entirely. **Named, load-bearing, absent from the page.** This is the single biggest concealment.
- **Concealment 2 — the conflict-policy pipeline.** `strain = [decisionWins, sourcePrecedence, …]` is six named algorithms behind six words. `specificity` alone is supersession-clipping. The page shows the *shape* of the pipeline and hides every strainer's body. (§5 owns the cost; here I own that it is *concealed*, not merely deferred.)
- **Concealment 3 — claim keys / decision targeting.** `decidedAccept(a, …)` and `decisionRetracts(a, …)` presume a way to match a decision to its target across rebuilds. kb/22 §8 flags this as "the single trickiest identity question": decisions target a *claim key* (type,subj,obj,pred), not just an assertion id, so re-proposed hypotheses re-attach. My `target` field pretends this is settled. **Concealed and genuinely unsettled.**
- **Concealment 4 — `covers` / seal content.** `require covers(s.frontier, r)` hides kb/25 §3's hardest sub-problem: a seal must capture the accepted-value frontier *and* enough defeat record that surviving losers stay defeated. One `require` conceals the anti-attrition design.
- **Concealment 5 — `resolveCtx` bootstrap.** The metacircular star hides that `bootCtx` must be *exactly* expressive enough to fold definition terms and *no more*. If a policy needs machinery `bootCtx` lacks, the regress reopens. I believe it is closable; I have not proven `bootCtx`'s sufficiency.

**The deeper honesty — does the term-language framing add anything over Candidate E's bare Datalog rules?** Partly, and I must not overclaim. The six belief rules (kb/22 §1) are *already written in Datalog-with-negation*; Candidate E can present them as-is and the fold is the stratified evaluation of those rules. What the interpreter framing genuinely adds: (a) it puts **writes and reads in one grammar** — Datalog rules describe belief but not `record`/`seal`/`import`, so E needs a separate write story; (b) it makes the **metacircular step syntactically visible** — `resolveCtx` calling `believe` is the whole point, and in a bare-rules presentation the fact that the context is folded by the same rules is true but *invisible*. What it risks: **syntax masquerading as insight.** If the grammar is just a skin over E's rules plus a write-log, then the "term language" is packaging, and the honest kernel is E's rules + an append primitive. My defense is the metacircular bottom (§3 ★, §4): the LISP-page claim is not "we have verbs" but "the interpreter of contexts is the belief engine, and it bottoms out in one primitive context." That claim is *load-bearing and distinctive*; the verb grammar around it is, I concede, partly ergonomic sugar. A reviewer who finds the metacircular bottom unconvincing should prefer Candidate E, and would be right to.

---

## 7. Tags

Every element above is tagged inline: [S] = standing/grounded in the corpus (kb/20–25, 19); [F] = optional deeper formal structure (the adjunction / graded monad in §3, off the page); [M] = my speculation. **Nothing tagged [M] is load-bearing** — the one [M]-adjacent claim (that `bootCtx`'s sufficiency is closable, Concealment 5) is explicitly flagged as unproven and the page does not rest on it; if it fails, the metacircular bottom needs a different base context, not a different kernel.

*Bare absolutes ("only", "never", "always") appear only at ceremony boundaries — append-only (I1), decisions-target-the-past (I6), purge-requires-seal, negative-conclusions-require-coverage (I8) — where the corpus already commits to them.*
