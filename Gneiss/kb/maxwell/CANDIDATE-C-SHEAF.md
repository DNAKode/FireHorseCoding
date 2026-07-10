# Candidate C — The Sheaf

*Phase M, one of five competing Maxwell-page formulations. Thesis: Gneiss's essence is* local belief, globally reconciled *— so the kernel is a computational (pre)sheaf: a finite site of (prefix, context) observation-points, belief views as sections over it, commit as descent, `contested`/federation-conflict as a gluing failure. **This candidate carries an explicit mandate to refute itself if the machinery is not earned.** §6 and the final verdict discharge that mandate — spoiler: it partially collapses. Tags: [S] solid / [F] frontier / [M] moonshot; nothing [M] is load-bearing.*

Anchors: PIE (contexts as ordered layers) · Worlds (sprout/commit + read-set check) · FrankenSim Bet 11 (cellular sheaf, watertightness = H⁰, leak = H¹ — and it tags this [F/M], off critical path) · cellular sheaves (Ghrist/Hansen: "finite linear algebra over an adjacency complex").

---

## 1. THE ONE PAGE — computational sheaf language [S except where marked]

A **site** is a finite set of *observation points* `p = (prefix, context)` — a ledger prefix `L≤t` (transactions up to tx `t`) paired with an evaluation context `c`. This is the base. It is *finite and explicit*: the points that exist are the ones some report run, agent read, or what-if world actually named. No infinite Grothendieck fantasy — a lookup table of points.

A **View** (the sheaf `𝔅`) assigns to each point `p` its **belief section** `𝔅(p)` = the accepted-assertion map computed by the belief fold: for each claim-key `k` (subject, predicate, valid-slice), a value tagged `accepted | contested | typed-missing`, with defeater and grade. `𝔅(p)` is a concrete finite map `k ↦ (value, status, grade, why-handle)`. [S]

**Restriction** `ρ: 𝔅(p) → 𝔅(q)` exists when `q ≤ p` in the *refinement order* on points: smaller prefix (`t_q ≤ t_p`) and/or coarser context (fewer decisions/policies in force). Restriction is a **total function you can run**: recompute the fold at `q`, or equivalently *replay the answer* — drop claims whose supporting txs exceed `t_q`, re-run the conflict strainer under `c_q`'s policy. Functoriality (`ρ_{p→r} = ρ_{q→r}∘ρ_{p→q}`) is just "cutoff coherence": `Compute(L, ctx{cutoff=t}) == Compute(L≤t, ctx{latest})` — already a P0 property test (§31 P0.5). [S]

**Covers.** A family `{p_i → p}` covers `p` when the pieces *jointly determine* `p`'s belief: every claim decided at `p` is decided at some `p_i`, and the `p_i` agree on overlaps. Two covers matter, and only two:
- **Prefix/segment cover** — `p` split by tx-range into consecutive segments. On one ledger this cover is *degenerate*: the total order I3 makes segments a chain, so "gluing" is a left fold. [S] *(This is the collapse; see §6.)*
- **Federation cover** — `p` = a federated view built from sibling ledgers `A, B, …`; the covering family is `{import(A), import(B), …}` and there is **no global order** across them (I3′). Here the cover is genuinely a family, not a chain. [S]

**Gluing (descent) as equation-checking.** Given sections `s_i` on a cover `{p_i}`, a global section `s` exists iff they agree on every overlap: for each shared claim-key `k`, `s_i(k) = s_j(k)` after restriction to the overlap point `p_i ∧ p_j`. This is **run the strainer on the pair and check for a policy-winner**:
```
glue({s_i}) :  for each claim-key k touched by ≥2 pieces:
                 vals ← { s_i(k) : k ∈ dom(s_i) }
                 if strainer(vals, c) yields a unique winner  → s(k) = winner        (glued)
                 else                                          → s(k) = contested(k)  (H¹ ≠ 0)
```
The section is the accepted frontier; the **obstruction** is the set of `contested` keys. Gluing is *solving-by-checking*, not abstract existence. [S]

**The six kernel operations, in this vocabulary:**
- **Record** (append assertion in a tx): extend one point `p` to `p⁺` with `t⁺ = t+1`; `𝔅(p⁺)` differs from `𝔅(p)` only on keys the new tx touches (locality of the fold). [S]
- **View**: name a point `p = (prefix, context)` and read the section `𝔅(p)`. A projection is a *cached stalk*; cache key `(high-water tx, context version)` is literally the point's coordinates. [S]
- **Sprout + Commit** (Worlds): sprout = a new context `c'` extending `c` with hypothetical overrides → a fresh point `p'` above `p`; its **read-set is exactly the sub-presheaf it restricted from** `p`. Commit = *descent back down*: check the read-set section is unchanged at current base `p_now` (`ρ` of `p_now` agrees with what `p'` read), then append `p'`'s new assertions. Commit is a gluing check between the sprouted section and the moved base. [S]
- **Seal + Purge** (imperfection §3): seal = replace a sub-cover (a tx-range segment) by a single **certified section** — a stalk whose value is the accepted frontier + defeat outcomes, whose restriction maps *outrank raw survivors*. Purge = delete the covered raw points; the sealed stalk still restricts correctly. Monotone degradation = restriction can only *coarsen* the section (weaken grade / widen typed-missing), never silently change a glued value. [S]
- **Import / Federate**: add a covering leg `import(B) → p`. B's assertions enter as evidence with source coordinates + watermark; the federation cover's gluing is where cross-ledger `contested` (= H¹) lives. Watermark = which of B's points are currently in the cover. [S]
- **Label** (the consumed-set, §19 MINE-SYNTHESIS §2): the *stalk-with-provenance*. A label records which points a section restricted from — Worlds' read-set = verifying-trace = `observedGeneration`. In sheaf terms the label **is** the section together with the minimal sub-cover that determines it. [S]

That is the page: a finite site of `(prefix, context)` points; `𝔅` a computable presheaf of belief maps; restriction = re-fold/replay; two covers (chain-degenerate prefix, genuine federation); gluing = run-the-strainer-and-check; the six ops as extend / read / sprout-descend / seal / federate / trace.

---

## 2. Needs generated, not bolted [S]

The Maxwell test: each need should *fall out* of the presheaf, not be added.

1. **Rebuildability / determinism** — `𝔅` is a function of the point alone; recomputing a stalk is deterministic; `glue` over the (chain) prefix cover is the fold. Delete every cached stalk, recompute from points. *Generated: it is the definition of a presheaf-of-values.*
2. **Time-travel / as-of** — a smaller prefix is a point *below* `p`; `ρ` to it is as-of query. "What did we believe on Jun 20" = read `𝔅((L≤Jun20, c))`. *Generated: restriction along the prefix order.*
3. **Auditability / provenance** — the **label** (§Label) is the consumed sub-cover; `why()` walks it. A report run's coordinates `(def, ctx, high-water)` are the point's address. *Generated: sections carry their determining sub-cover.*
4. **What-if** — sprout = a point above `p` in a fresh context; the world is a *slice* of the site over that context. Zero data copy. *Generated: contexts index the fibers of the site.*
5. **Safe concurrency of interpretation** — two teams = two contexts = two points over the same prefix; they never collide because they are *different fibers*, glued only if/when a federation or commit cover forces overlap. *Generated: the context axis of the base.*
6. **Graceful degradation / federation** — seal coarsens stalks monotonically; federation is the non-chain cover; `contested` is the honest H¹. *Generated: covers + the coarsening property of restriction.*

Honest note: needs **1, 2, 4, 5** are generated *equally well by any monotone-cutoff order theory* (Candidate B). Only **3 (label-as-subcover)** and **6 (federation H¹)** use anything the sheaf framing adds beyond a poset. Flagged for §6.

---

## 3. Theorem sketches [S unless marked]

- **Determinism.** `𝔅(p)` depends only on `p`. Proof: the fold is a pure function of (visible txs, pinned policy versions), both determined by `p`; ties broken by a total order (confidence, then tx-id). ∎ *(= P0.1; corollary of "presheaf of values.")*
- **Rung equivalence** (incrementality ladder L0–L3). Any incremental scheme is a functor computing the *same* section; validity = "agrees with the recompute stalk on the nose." Proof: restriction is single-valued, so any two routes to `𝔅(p)` (full recompute vs per-key vs IVM) must equal it. ∎ *(Sheaf gives the crisp statement: incremental engines are natural transformations that must equal identity on stalks; the content is just single-valuedness — a poset gives it too.)*
- **Monotone degradation.** Seal+purge factors through restriction to a coarser cover; restriction can only move a key to a *weaker grade or wider typed-missing*, never flip a glued winner. Proof obligation lands on **seal content**: the certified stalk must carry the defeat frontier so surviving losers stay defeated (the "resurrection by attrition" hazard, imperfection §3). Sheaf reframing makes the obligation precise (*seals must outrank raw survivors in the restriction maps*) but does not discharge it — it is a design constraint, not a corollary. [S]
- **No-surprises / consistency** (Worlds → descent). *Read-set consistency becomes a descent condition — this is the candidate's cleanest genuine win.* A sprouted world `p'` commits safely iff its read-set section equals the restriction of the current base `p_now` to that read-set: `ρ_{p_now → readset} = (what p' read)`. Precisely: commit is valid iff `{s_{p'}, s_{p_now}}` **glue** on their overlap. "No surprises" (Worlds' theorem: reading in a world never sees a base change you didn't make) = the read-set is a *sub-presheaf preserved by restriction*. This is a real, machine-checkable descent statement, and it is exactly the artifact §19 flagged as the recurring one. [S]
- **DISTINCTIVE — contested / federation-conflict as computable H¹.** Build the **cellular sheaf** (à la Ghrist/Hansen, exactly FrankenSim Bet 11): 0-cells = participating ledgers/points in a federation cover; 1-cells = overlaps (shared claim-keys between two ledgers); stalks = the local belief maps; restriction maps = the strainer's projection to the shared key. The **coboundary** `δ` sends `(s_A, s_B, …)` to the per-overlap disagreement `s_A(k) − s_B(k)` (in a value space where "−" means "strainer found no unique winner"). Then:
  - `H⁰ = ker δ` = globally consistent federated beliefs (the glued section).
  - `H¹ = coker δ` (obstructions modulo what coboundaries can fix) = **the irreducible `contested` set**: conflicts no policy resolves and no re-import repairs.

  *Precise claim:* the set of `contested` claim-keys in a federated view is representable as a cocycle whose class in `H¹` is nonzero exactly when the disagreement cannot be discharged by any single ledger's decision. **Computability rating at Gneiss scale: YES for the diagnostic, NO/DECORATIVE for the cohomology.** The *check* — "do these ledgers agree on shared keys, and if not, which keys/edges" — is O(shared-keys × ledgers), trivially computable, and IS what an operator needs (same argument FrankenSim gives: "the offending interface cells attached is exactly the diagnostic an agent needs"). But the belief value space is **not a vector space** (values are typed, ordered by a policy strainer, not added), so `H¹` is not literally linear cohomology — it is a *set-valued / lattice-valued* obstruction. You get the **shape** of H¹ (a per-edge disagreement 1-cochain, a "is there a global section?" question) but not the linear-algebra payoff (dimension counts, harmonic representatives, spectral methods). The honest statement: **`contested` is a computable H⁰/H¹ *analogy* — a genuine gluing obstruction over the federation nerve — but the cohomology is over a lattice, not a field, so 90% of the sheaf-Laplacian toolkit does not apply.** [F]

---

## 4. Metacircularity check [S]

The site is self-describing without new machinery. Points `(prefix, context)` are built from kernel objects: a prefix is a tx-id (Transaction), a context is an Entity described by Assertions (kernel §2). So the base of the site *lives in the ledger it indexes* — the fixed point (kernel §4) holds at the site level: **the presheaf's own base category is a belief view over context-entities.** A context version is a point; describing it is appending assertions about a context-entity, i.e. moving to a neighbouring point. Gneiss-on-Gneiss (D28): the corpus's own decisions are a ledger, and the five candidate documents are five contexts (points) over it — this document is literally a section `𝔅((corpus, C-sheaf))`. Passes. *No sheaf-specific machinery is needed for self-description; the kernel already closes under it. This is neutral evidence — it neither supports nor refutes the sheaf framing over the order-theoretic one.*

---

## 5. The week test — brutal [S]

*Can a graduate student reimplement the kernel in a week from this page? Sheaf machinery has a notorious transcription cost — Kay's "T-shirt size" bar is unforgiving here.*

- **What survives a week (the fold + covers-as-lookups):** if the student *ignores the sheaf words* and implements "belief map per (prefix, context), recompute-or-cache, and a pairwise strainer-check for federation," they finish in a week. This is P0 + a federation merge. **≈4/5 achievable.**
- **What blows the week (the sheaf words taken seriously):** if the student tries to build a base category with a coverage (Grothendieck topology), verify the sheaf axioms, or implement cellular-sheaf cohomology with a coboundary operator, they spend the week learning `H¹` and ship nothing. FrankenSim — a fearless, agent-swarm-sized plan — tags *its* sheaf component [F/M] and puts it in Phase 6 (weeks 56–68), off the critical path. That is the single most damning external data point for this candidate: **the one team that actually plans to build a computational sheaf treats it as a late, flagged, non-spine luxury.**
- **Verdict on the page:** the *runnable* content is 60 lines of fold + strainer; the *sheaf vocabulary* is a lens laid over it that costs a week if mistaken for the implementation. A one-page kernel must not have a failure mode where reading it literally sends you down a cohomology rabbit hole. **This page has that failure mode.** Mitigated only by §1's discipline ("finite lookup table of points," "solving-by-checking, not abstract existence") — but discipline that must be *repeatedly asserted against the vocabulary's own pull* is a smell.

---

## 6. Self-criticism and the refutation clause [S]

**The mandate: judge, do not defend. Verdict: the sheaf story does NOT earn the machinery on the spine. It collapses to Candidate-B order theory on a single ledger, and is decoration over the fold — with two genuine surviving lenses at the corners.**

The collapse, precisely:

1. **The total order kills gluing on the spine.** Gneiss's deepest engineering decision is I3/I6: *belief is a fold, not a search*, over a per-ledger **total order**. But gluing is the whole point of a sheaf — reconciling pieces with *no global order*. On a single ledger the prefix cover is a **chain**, and descent over a chain is a left fold. A sheaf over a totally-ordered poset is not using any sheaf structure a poset lacks: it is order theory wearing a fancy hat. Candidate B (order theory / the fold over `(≤_tx × ⊑_context)`) says everything §1–§5 says with *no* site, *no* coverage, *no* restriction-functor ceremony — and it passes the week test cleanly. **Nine-tenths of this page is Candidate B.**
2. **The base category is trivial.** A real Grothendieck site earns its keep when the coverage is a rich topology (opens of a space, étale maps). Ours is a finite poset of `(prefix, context)` points with at most two cover shapes, one of which is a chain. That is not a site that needs sheaf theory; it is a *lookup table with a partial order*.
3. **The values are not abelian.** Genuine sheaf cohomology needs a field (or at least an abelian group) in the stalks to make `δ`, `ker`, `coker` linear. Belief values are typed, lattice-ordered, resolved by a *non-linear policy strainer*. So even where H¹ is invoked (§3 distinctive), it is a lattice-valued obstruction — the *diagnostic* is real, the *cohomology* is analogy. Calling `contested` "H¹" is 20% insight, 80% branding.
4. **The I8 pun — real or decorative?** My mandate asked whether "coverage map" (which ledger regions survive) ≟ Grothendieck "coverage" (which families license gluing/negative conclusions), with I8 ("negative conclusions require positive coverage") as a candidate sheaf condition. **Verdict: a genuine structural rhyme, not an identity.** I8 really is a *covering condition*: `absent_closed` (a confident "no") may be glued only where the covering family is `full`/`sealed` — i.e. a global section (a negative conclusion) exists only over a legitimate cover. That is exactly the sheaf discipline "a section over `U` exists only if you have a cover of `U`." The rhyme is **worth keeping as a lens** (it explains *why* I8 is the right law: negative conclusions are global sections, and global sections need covers). But it does not require sheaf *machinery* to state or check — it is one predicate on the coverage map. Real insight, decorative apparatus.

**The salvage list — the 2–3 sheaf ideas that survive as lenses for the corners where the total order breaks:**

1. **Federation as the only real site [S/F].** Cross-ledger reconciliation (I3′: no global order) is where a cover is genuinely a non-chain family and gluing is genuinely non-trivial. Here the sheaf lens *earns its keep*: it names the federated view as a section over the nerve of the ledger cover, and it correctly predicts that watermark-based covers + pairwise overlap-checks are the mechanism. **Keep the sheaf framing scoped to federation only.**
2. **`contested` as a computable gluing obstruction [F].** The FrankenSim-Bet-11 diagnostic — build the pairwise-disagreement 1-cochain over the federation nerve, report the offending edges/keys — is a real, implementable, operator-useful tool. Keep it as the *federation-conflict diagnostic*, honestly labelled as a lattice-valued obstruction (a "does a global section exist, and which edges block it" check), **not** as linear cohomology.
3. **Read-set-commit as descent [S].** Worlds' commit check *is* a two-piece gluing condition (§3 no-surprises). This is the one place on the *spine* (not just the corners) where the sheaf reframing adds clarity: "commit is safe iff the sprout and the moved base glue on the read-set." Keep it as the precise statement of the commit rule — it makes the machine-checkable theorem crisp.

Everything else — the site, the coverage-as-topology, the presheaf ceremony for as-of and what-if — is subsumed by Candidate B's order theory and should be dropped from the Maxwell page. **The sheaf is not the kernel. The fold is the kernel; the sheaf is a lens for its two hardest corners.**

---

## 7. Tag audit [S]

Load-bearing spine claims (§1 six ops, §2 needs, §3 determinism/rung/no-surprises, §4, §6 collapse verdict): all **[S]**. Frontier: federation-as-site, `contested`-as-obstruction diagnostic (§3, §6 salvage) — **[F]**, and explicitly *off the spine*. Nothing **[M]** appears; nothing load-bearing is [F]. The distinctive H¹ theorem is [F] and is **not** required for any need — needs 1–6 are all generated by the [S] fold + covers. Compliant.
