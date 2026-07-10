# The History Mine: Synthesis and Phase M Inputs

*2026-07-05. The yield of the three-shaft primary-source mine ([19A](19A-SURVEY-AUGMENTATION.md) augmentation, [19B](19B-SURVEY-PARC-VPRI.md) PARC–VPRI, [19C](19C-SURVEY-NEAR-MISSES.md) near-misses), compressed into what changes the corpus and what feeds Phase M. Kay's invitation, honored: we did the reading.*

## 1. The lineage, now documented rather than claimed

Every Gneiss organ ran somewhere, on hardware of its decade — never all together:

| Organ | Ran as | When |
|---|---|---|
| Source/actor envelope on assertion-sized units | Engelbart's notch-coded kernel cards ("including a code for myself") | mid-1950s practice, 1962 print |
| Justification edges; the argument as a DAG | "Antecedent links… track down the essential basis upon which a given statement rests" | 1962 (prose) |
| Append-only, permanently citable group memory; IDs minted pre-publication; mail-as-citation | The NLS Journal | 1970, production |
| Declared views over one structure, *carried in the reference* | Viewspecs ("the web kept the address and threw away the lens") | 1968, live on stage |
| Contexts as ordered immutable layers; sealing = layer-closure with contract checks; merges as first-class objects with rationale | PIE | 1980–81 |
| Log + materialized view + replay + condense | Smalltalk .changes/image ("recover changes" after every crash) | 1976–, production continuously |
| `view = f(snapshot, ordered messages)`, bit-identical replicas, dumb sequencer | Croquet/TeaTime → Multisynq (Reed's pseudo-time, 1978) | 2003–, alive 2026 |
| What-if sprout/commit with read-set consistency check | Worlds (Warth/Ohshima/Kaehler/Kay) | 2008/2011 |
| Intent → automatic re-satisfaction, drift as a computed scalar | Sketchpad's error-subroutine constraints | 1963 |
| Free-form capture, rule-computed structure, views with write-back, sticky human overrides | Lotus Agenda | 1988, 200k civilians |
| Intensional ("virtual") structures over extensional decay | Halasz's Seven Issues #3 | 1988, stated as requirement |

The refrain of all eleven surveys holds at full strength: **the pieces exist; the composition doesn't.** And the mine upgraded the lineage sentence itself (19A's verdict): Gneiss doesn't just grow the Journal a type system — it *mechanizes Section III of Engelbart's 1962 report with the semantics he left informal*, and it claims the lot Licklider staked in 1968: **"We are stressing the modeling function, not the switching function."** History built the switch; the shared, revisable, externalized model is still vacant.

## 2. The one artifact that keeps reappearing

Three surveys independently surfaced the same object under three names: **Worlds' read-set** (what a speculative world consulted — the thing checked at commit), **the verifying trace** (survey 13's build-system rebuilder — what a view consulted, hashed), and **`observedGeneration`** (survey 18's k8s status — which spec version an answer evaluated). One artifact: *the record of what a computation consumed, carried with its result.* It is what makes commits safe, rebuilds incremental, staleness detectable, and labels honest. **Phase M should treat the consumed-set as a first-class citizen of the algebra** — possibly the sixth object the Maxwell page needs, or better: the thing the label *is*.

## 3. Phase M protocol (adopted from the mine)

- **Method:** the extracted STEPS procedure (19B, 11 steps) — needs → mathematical centers → T-shirt sizing → throwaways to find the arches → runnable maths → metacircular bottom → distill, don't proliferate → validate by reimplementation *and* mechanized theorems.
- **Social form:** Kay's 1972 one-page bet — definition-first, a named implementer, a deadline, a wager. The kernel is a cultural artifact before it is a technical one.
- **Budget:** kernel = **one T-shirt**. Meaning-code and optimization-code counted separately from day one (the siren's-song guard — STEPS' single biggest stated regret).
- **Theorems ready-named:** determinism-to-the-bit (TeaTime's checklist: synthetic time only, nondeterminism quarantined at the I/O boundary, FPU/hash-order discipline — "even the bugs are the same"); *no surprises* and *consistency* for what-if contexts (Worlds, already stated machine-checkably); monotone degradation (ours); layer-closure-with-validation (PIE's sealing).
- **Open policy question Worlds sharpened:** what-if promotion when the base has moved — Worlds gives abort-atomically-and-keep-inspectable; PIE gives merge-as-new-layer-with-recorded-rationale. Gneiss supports both; a context must also declare whether it *tracks* base advances or pins to a prefix. (D-item folded into D26's orbit.)

## 4. New obligations the mine imposed (gaps and laws)

1. **Structure search (Halasz #1) — no current answer.** Graph patterns over the ledger ("circular chains of supports links" = circular-justification detection, reasoning hygiene). Plus his one-language principle: query = view-definition = filter, one notation. → Language requirements (D32).
2. **The collaboration social layer (Halasz #6) — the biggest hole.** Append-only dissolves locking, but awareness (notify-at-intent-to-update), convention-setting, and mutual intelligibility of ledger entries are undesigned. NoteCards users hand-built convention cards; ours will too unless it's designed. → D31.
3. **The conservation law of capture (19C's synthesis).** Intent rot and review-bandwidth insolvency are *one conserved quantity*: gIBIS moved the cost from writing to encoding; Gneiss moves it from encoding to reviewing. Grudin's audit — for every required human act, name who pays and who gains — becomes standing method on every capture-side feature. Risks register updated in spirit (32/25's two risks merge).
4. **Sketchpad's clock (same-session payoff rule).** Declared intent survives when the engine repays it in seconds, dies when archival. Every realizes-edge must produce a same-session return — a drift check, a generated view, a caught inconsistency. If it doesn't pay rent this session, it's a gIBIS node, not a Sketchpad constraint.
5. **The protonode (gIBIS).** "Not yet structured" as a dignified first-class state — typed missingness applied to *structure itself*. Capture must be free-form-first (Agenda's lesson), typing deferred and machine-proposed.
6. **The schema is never the menu (gIBIS §5.5).** Decisions must always admit prose that transcends the hypothesis set, or the interesting decisions happen off-ledger — which is how intent rot actually starts.
7. **A0's survival condition, finalized (19A).** Engelbart's discipline demanded new motor skills and a new machine; a survivable A0 demands only new *recording habits on incumbent tools*. No chord keysets, no priesthood, no second tool.
8. **Distillation is a system function — triple-confirmed.** c2's ThreadMode drowned volunteers; CODIAK's integration step was specified and never automated; PLATO had testimony and no front page. Three independent deaths from the same missing organ, which is the belief engine. This is now the single best-evidenced claim in the corpus.
9. **why() is the learnability organ, not a luxury (Agenda).** Rules-compute-your-structure products die when users can't predict the rules. Justification edges are what Agenda was missing between 200,000 copies and the mainstream.
10. **Trails: build the mechanism, budget the labor.** A trail = ordered, annotated sequence of labeled replayable views — native to Gneiss, impossible on the web ("trails do not fade" is a bitemporal requirement). But Bush budgeted a *profession*; expect trails in onboarding, incident retrospectives, and audit narratives, not as ambient behavior. The "trail blazer with a budget" joins the distillation-debt workflow.

## 5. Corrections to standing claims

- **Lineage statement** (06/41): upgrade per §1 — mechanizing Engelbart 1962 §III; claiming Licklider's modeling function. The "Journal + type system" line remains true but partial.
- **Smalltalk claim K1** confirmed *stronger than posed*, with the caveat that matters: condensing was **silent total forgetting** — Gneiss's recorded, monotone-degrading forgetting is the genuine advance over its own ancestor. Same shape as the `oldid` finding (17): the ancestors kept breaking exactly the promises Gneiss's ceremony keeps.
- **TeaTime caveat:** its ordered stream is ephemeral coordination (durable artifact = snapshot); Gneiss adds the permanent queryable ledger. Borrow the determinism discipline, not the two-phase commit.
- **Versioning humility (Halasz '91):** he *demoted* versioning after users ranked it low — market evidence that bitemporality is not self-selling; it must be sold through the corrections/restatement pain it removes, never as a feature checkbox.

## 6. Where this leaves the plan

Phase H is complete: eleven surveys, all primary-source-grounded where it counted. Phase M is unblocked and now has: a method (the STEPS procedure), a social protocol (the one-page bet), a candidate sixth element (the consumed-set, §2), named theorems with prior art, a budget discipline, and two pre-announced failure modes (siren's song, DSL sprawl). Phase S (the Model paper) inherits the documented lineage table (§1) as its related-work spine. Phase C inherits three sharpened targets: the conservation law (§4.3), the collaboration hole (§4.2), and the adoption economics that killed every ancestor.
