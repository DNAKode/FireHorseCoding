# Survey: Intentional Programming & the Intent-Capture Traditions

*Research-agent survey for Gneiss, 2026-07-05. Commissioned for the Intent stance design ([35-INTENT.md](35-INTENT.md)); claims I1–I5 adjudicated. Verdicts absorbed into 35 §10. Web-verified where noted; guesses labeled.*

## TL;DR — top 5 takeaways for Gneiss

1. **Nobody has shipped the realizes-relation as a persistent, drift-detecting, queryable record.** IP stored intent beautifully and assumed drift was definitionally impossible (regenerate everything); MDA died of exactly the drift it couldn't track; DOORS records the relation but nothing executes it; Kubernetes shipped the *loop* but its "intent ledger" (etcd/git) is last-writer-wins, not append-only with provenance. The organ Gneiss is designing genuinely does not exist assembled. Every tradition contributes one piece.
2. **Copy Kubernetes ergonomics almost literally.** Spec beside status on the same object; status computed, never hand-edited; `observedGeneration` stamped on status so staleness is detectable. `observedGeneration` *is* Gneiss's answer-label (context version + evidence high-water mark), independently evolved and proven at planetary scale. That convergence is strong evidence the design is right.
3. **IP's fatal wound was the all-or-nothing substrate plus secrecy, not the idea.** The intentional tree required abandoning text, diff, merge, grep, and every existing tool at once, and Intentional Software spent 15 years saying "trust us" without a public live system. Gneiss must remain a side-car: ledger *beside* artifacts, text projections first-class, something small live early.
4. **Trace links survive only when a consumer fails loudly.** DO-178C traceability stays alive because certification gates on it; Argo/Flux drift status stays alive because sync alarms fire; DOORS matrices maintained for audit rot within months. Realizes-edges must be asserted in the same act as the work (edit-as-testify) and consumed by machinery that breaks visibly when they dangle.
5. **Intension-over-extension is the robustness move, but only with time, provenance, and identity attached.** Excel's dynamic arrays prove the move works for millions of users and also show its ceiling: the formula carries scope-intent but has no history, no `why(value)`, no identity, no grade. Steal one concrete thing: `#SPILL!` — an explicit, named failure mode for when intensional realization collides with manually-owned extensional space.

## 1. Charles Simonyi's Intentional Programming

**The MSR years (≈1994–2001).** Simonyi's September 1995 technical report, *The Death of Computer Languages, the Birth of Intentional Programming* (MSR TR-95-52), framed the thesis: programming languages are an accident of text encoding; the durable artifact should be the programmer's *intentions*, stored as a graph. The IP system stored source as an **intentional tree**: nodes with immutable **identities**, where names are display properties and all references are graph pointers — rename is trivial, "grep" is a graph query. **Projection** rendered one tree in multiple notations; semantics came from **transformations** (Simonyi called reduction methods "enzymes") rewriting high-level intentions down to executable code. The OOPSLA 2006 *Intentional Software* paper/demo (with Christerson and Clifford) is the canonical public artifact.

**Why Microsoft dropped it.** Around 2000–2001, amid the C#/.NET push, Microsoft declined to productize. Hal Berenson's candid version: everyone found it interesting; nobody could find a near-term home. Simonyi negotiated the rights out.

**Intentional Software (2002–2017).** With Gregor Kiczales as co-founder, built the **Domain Workbench**. Verified flagship use: **Capgemini's Pension Workbench** for the Dutch pension domain (JAOO 2007; InfoQ, Fowler) — actuaries edited pension rules in real mathematical notation, interleaved with prose and **FIT-style live test tables showing red/green as formulas changed**, all projections of one tree. Note that detail: *live example tables beside the intent* — an in-editor alignment check between intent and realization. Fowler's contemporary assessment: "no system designed using the Intentional Domain Workbench has yet gone live"; the posture "boiled down to saying trust us." **[guess]** Whether a Capgemini pension system reached sustained production is publicly undocumented.

**The end.** Microsoft acquired Intentional Software April 18, 2017 (~100 staff), folded into the Office org. No product traceable to the Domain Workbench has surfaced since.

**Why it failed — the honest synthesis:** (1) all-or-nothing substrate: adopting the tree meant abandoning text, diff, merge, review, and every editor at once; (2) closed ecosystem and secrecy: no incremental adoption path, no public artifact; (3) the value needed domain-expert co-authoring, which needed deployed successes, which needed the value — bootstrap deadlock; (4) **drift was assumed away**: realization was always regenerated from intent, so the system had no concept of, or machinery for, a realization diverging — which broke at every boundary with the world it didn't own.

**Verdict for Gneiss: adapt.** The identity/projection/store tenets are already Gneiss DNA and are validated. The delivery model is the anti-pattern: never require the world to move into your substrate; ledger the intent beside the world's artifacts.

## 2. Language workbenches & projectional editing

Fowler's June 2005 *Language Workbenches* essay coined the term, set the promise (cheap DSLs, domain experts as co-authors), and flagged the caveats that came true: tool lock-in, vendor risk, and the diff/merge/storage problem of non-text sources.

**JetBrains MPS in 2026: alive.** MPS 2025.3 shipped; 2026.1 EAP/RC current as of June 2026 (with "accessibility improvements for AI coding agents"). Real deployments, verified: the **Dutch Tax and Customs Administration** uses MPS for **ALEF/RegelSpraak**, a controlled natural language in which tax-law rules are specified and executed — the strongest living example of law-as-ledgered-intent anywhere. **mbeddr** survives as a community/itemis-maintained platform **[guess: maintenance mode]**. Healthcare: Voelter et al.'s insulin-titration DSL behind Voluntis's Insulia **[unverified this pass]**.

**Projectional editing ergonomics.** Berger, Völter et al. (FSE 2016): basic editing reaches text parity; complex edits require understanding the AST; error rates rise until users internalize the tree. Partial fixes: **grammar cells** (SLE 2016) make structured editors feel like text. Honest reading: twenty years in, projectional editing is viable where an institution pays the training tax, and nowhere else. Parser-based cousins (Xtext→Langium, Spoofax, Racket LOP) survived better precisely by staying inside the text ecosystem.

**Verdict for Gneiss: adapt.** ALEF proves domain-expert-authored intent at national scale. The ergonomics literature is a direct warning for edit-as-testify surfaces: structured input must feel like ordinary editing or experts route around it.

## 3. Model-driven architecture/engineering — the cautionary sibling

OMG's MDA (2001): platform-independent models → platform-specific models → code, with round-trip engineering. It failed in the mainstream because round-tripping is intent-recovery from realization — lossy and unreliable; the moment generated code was hand-edited (always), there were two sources of truth and no arbiter. "The code is the truth, the model rots" became folklore because nothing *broke* when the model rotted: no consumer, no gate, no drift signal.

Where MDE quietly won: **Simulink/Stateflow** codegen in automotive/aerospace, **SCADE** (qualified generators for DO-178), **AUTOSAR**, railway interlocking. The common condition: the model is executable, generation is one-directional, and hand-editing the output is *forbidden* — realizes is total and machine-maintained, so drift cannot enter.

**Verdict for Gneiss: adopt the lesson, ignore the stack.** One-directional derivation with hand-patching forbidden is Gneiss's existing rule for belief views. The MDA corpse is the strongest argument that an explicit, drift-detecting realizes-edge is the load-bearing organ, not a nice-to-have.

## 4. Subtext / Jonathan Edwards (brief)

Subtext (OOPSLA 2005; schematic tables 2007) pursued "no syntax": programs as directly-manipulated structure, live values visible inside the logic, all conditional paths displayed orthogonally. Edwards remains active (Denicek with Petricek, UIST 2025). The teaching: **reveal meaning, not text** — show values, branches taken, justification *in situ*. The cautionary half: two decades of brilliant demos with no migration path stayed demos.

**Verdict for Gneiss: adapt (the presentation instinct only).**

## 5. Excel dynamic arrays as intent capture

**Verified timeline.** Dynamic arrays announced September 2018, GA through 2019–2020: **spilling**, `FILTER`/`SORT`/`UNIQUE`/`SEQUENCE`/`SORTBY`, `A1#` (references *whatever the spill currently is*). `LET` (2020) names intermediate intent. `LAMBDA` (2021) makes the formula language Turing-complete — driven by MSR's **Calc Intelligence** (Peyton Jones, Gordon, Sarkar et al.); *Elastic Sheet-Defined Functions* (JFP 2020) is the academic core: mechanized intension-recovery from an extensional example. `GROUPBY`/`PIVOTBY` (2023+), `TRIMRANGE` (2024–25) are mainstream in M365 by mid-2026.

**The frame holds.** `A1:A17` is extensional — frozen at authoring, silently wrong at row 18. `FILTER(Table[amt], Table[region]="EU")` is intensional — the reference *is* the scope-intent, realized at every calc. Error classes removed: stale ranges, fill-down drift, off-by-one edges, CSE mysteries. Not fixed: wrong intent (the formula computes exactly the wrong thing, faster); **no provenance** (a spilled cell cannot answer `why(value)`); **no history** (recalc destroys the previous extension — no bitemporal record of what the range *was*); **no identity or envelope** on the intent itself. And one gem: **`#SPILL!`** — when an intensional realization collides with occupied cells, the engine refuses loudly rather than overwriting or silently truncating.

**Verdict for Gneiss: adopt the framing; steal `#SPILL!`.** The mass-market proof that moving intent into the reference removes whole error classes — the statutes-over-verdicts argument with 750 million users of evidence.

## 6. Kubernetes-style declarative reconciliation

Every object carries **`spec`** (declared intent) and **`status`** (observed/computed, controller-written, *never* hand-edited — enforced by the status subresource's separate write path). **Controllers** run level-triggered reconciliation: observe, diff, act; drift detection is continuous and ambient. `kubectl diff` previews intent changes against live state. **GitOps** (Flux, Argo CD) makes git the intent ledger — versioned desired state, `Synced`/`OutOfSync` as first-class dashboard property, drift alerts, auto-heal. Terraform: `plan` = explicit intent-vs-recorded-state diff; its state file is a recorded *belief* about realizations, and its pathologies (drift, contested single writer) are instructive.

**Why the ergonomics work:** (1) spec and status co-located — alignment is a glance, not a join; (2) "status is computed, never edited" — you cannot repair reality by editing the record of it; (3) **`status.observedGeneration`** — status declares *which spec version it evaluated*, so staleness is detectable — precisely Gneiss's answer-label; (4) `status.conditions[]` (`type/status/reason/message/lastTransitionTime`) — graded, timestamped, justified belief statements as a standard shape.

**Where loops go wrong:** fighting controllers (two owners of one field — mitigated by server-side apply's per-field ownership); flapping without hysteresis; orphaned specs no controller watches (intent silently pending forever); stale status read as fresh by clients ignoring `observedGeneration`.

**Verdict for Gneiss: adopt — the proven ergonomic template for the Intent stance.**

## 7. Requirements traceability — the old-school realizes-relation

DOORS-era trace matrices (requirements → design → code → test, with **suspect-link** flags when an upstream item changes) are the literal ancestor of `realizes(artifact, intent)`. They rot because links are hand-made, retro-fitted at audit time, and — decisively — *nothing executes them*: a wrong link breaks no build, so decay is invisible and unpunished. The matrix is a report about a relation, not the relation.

DO-178C keeps traceability alive **by force**: DAL A requires full bidirectional traceability, certification authorities audit it, and verification automation must itself be qualified (DO-330). Even there: costly maintenance, and without a locked baseline nobody can prove which requirement version was verified — the label problem again.

**Transferable lessons:** links created *in the act of work*, not reconstructed later (= edit-as-testify); the suspect-link flag is a proven drift-detection primitive; traceability survives only where a consumer gates on it.

**Verdict for Gneiss: adapt.** Take suspect-links and baseline-stamping; reject the standalone-matrix form. Realizes-edges are trace links a machine actually reads.

## 8. Specs-as-source in the LLM era (2024–2026)

Verified: **GitHub Spec Kit** (open-sourced September 2025): Spec → Plan → Tasks → Implement, each phase a markdown artifact feeding an agent; ~90k stars, v0.11.0 (June 2026), 30+ agent integrations, plus a "constitution" file of standing project principles (a standing-policy cousin). Around it: Amazon Kiro **[not re-verified]**, Tessl **[unverified]**, and a wave of "intent programming" claims.

**How real:** real as a *prompting discipline* — front-loading intent measurably improves agent output; specs in git form a crude intent ledger. Not real as *regeneration*: nobody regenerates a nontrivial system from spec on each change; code gets patched without back-propagation, and the spec silently stops describing the system. **This is the MDA round-trip failure wearing new clothes**, with two twists: the generator is cheap and tolerant of informal specs, but also *nondeterministic*, so even a maintained spec doesn't pin the realization — only tests bridge the gap. No identity, no realizes-edge, no drift detector, no label saying which spec version the code reflects. **The wave has rediscovered the intent artifact and not yet rediscovered the reconciliation loop.**

**Verdict for Gneiss: adopt the moment, supply the missing half.** Market timing: everyone is suddenly writing intent artifacts with no machinery to ledger them or detect drift. Gneiss's Intent stance is precisely the organ Spec Kit lacks.

## 9. Intent-based networking (one paragraph)

Co-opted by networking marketing ~2017 (Cisco, Apstra, Forward Networks); by 2026 largely dissolved into "AI-driven operations" positioning. The real residue: **network verification** (Batfish, Forward) — configs checked against declared policy-intent — and Apstra's drift alarms over a single-source-of-truth graph: structurally the same spec/status/reconcile pattern, independently re-derived. Nothing new beyond confirmation the pattern generalizes — and a warning about how fast "intent" degrades into a marketing word. **Verdict: ignore (mine the vocabulary caution only).**

## The five claims evaluated

**I1 — IP's tree/identities anticipates Gneiss entities + ledger.** *Fair, with one scope correction.* IP's identities were intra-artifact (code-graph nodes); Gneiss's are world-entities. The mechanism and payoff map cleanly. What IP predicts: the cost is the **interoperability moat** — the world's tooling assumes text; a structured source of truth must project losslessly into text-diffable form or die reimplementing the ecosystem. Gneiss is structurally better positioned (append-only rows are interop-friendly; Gneiss sits beside artifacts) — but only if text/export projections stay first-class. **Fair mapping; heed the moat.**

**I2 — IP projection ≈ Gneiss belief views + presentation.** *Surface-plus, not deep.* Shared: the surface is never the store; many notations over one substrate. Different and decisive: IP projections are lossless, bidirectional, and *must all agree* — two projections disagreeing means the system is broken. Gneiss belief views are lossy, computed, context-parameterized, and *licensed to disagree* (contexts, grades); you don't edit a view, you testify. The genuinely deep echo is narrower: **edit-as-testify is IP's edit-the-projection channel** — surface edits routed to the canonical store through a defined pathway. **Adopt the discipline, don't claim the equivalence.**

**I3 — IP's missing organ was a ledgered, drift-detecting realizes-relation.** *Historically fair, with one credit owed.* IP had versioned intent and generative transformations, but no persistent record that a deployed realization corresponded to tree-version X, no status object, no drift concept — because in IP's cosmology drift was impossible: realization was always freshly regenerated. That assumption failed at every boundary the workbench didn't own. The credit: **Capgemini's Pension Workbench had live FIT-style test tables red/greening beside the formulas** — a real, in-editor intent-vs-realization alignment check. It was ephemeral, in-tool, untimestamped: **a reveal without a ledger.** **The claim stands; steal the live-table UI as the reveal-surface for realizes-status.**

**I4 — Copy the Kubernetes spec/status split.** *Sound.* The only intent-reconciliation ergonomics with planetary-scale evidence, and `observedGeneration` independently converges on Gneiss's answer-label. Four guards: **fighting controllers** → per-facet authority over who computes an alignment status (the actor envelope supports server-side-apply-style ownership naturally); **flapping** → hysteresis/rate-damping on drift-grade transitions; **orphaned intent** → alignment defaults to *unknown-by-absence-of-watcher*, never silently green — every intent entity needs a declared reconciler or an explicit "unwatched" grade (Gneiss's own coverage doctrine applied to itself); **stale status trusted** → mandatory label checks; refuse to present status whose evaluated intent-version lags. Also: hand-edited status is unsayable — human judgments about alignment enter as *testimony about alignment* with a source envelope, never as edits to the computed view. **Adopt, with the four guards.**

**I5 — Dynamic arrays = statutes-over-verdicts = intension over extension.** *Fair at the core; breaks in three places and under-sells Gneiss.* Core holds: `A1#`/`FILTER`, a standing policy, and a belief view are the same move — the reference carries scope-intent, the engine re-realizes it. Breaks: (1) **no time** — recalc annihilates the previous extension; Gneiss keeps every realization; (2) **no provenance** — a spilled value has no `why()`; (3) **no identity/envelope/grade** — the formula is anonymous intent. The stealable extra: **`#SPILL!` semantics** — when intensional realization collides with manually-owned space, fail loudly with a named error, never overwrite, never silently truncate; Gneiss needs exactly this defined behavior where rule-derived values collide with direct testimony. **Fair; use as the accessible explanation of the philosophy, with the three-break honesty.**

## What Gneiss should steal

| Mechanism | Source | Gneiss use |
|---|---|---|
| Identity-not-names; rename as display-only | IP intentional tree | Already core; keep lossless text projections exportable to dodge the interop moat |
| Surface-is-never-the-store discipline | IP / MPS projection | Presentation contract; edit-as-testify as the sanctioned back-channel |
| Live red/green example tables beside intent | Intentional Pension Workbench / FIT | Render realizes-status with test-case evidence inline at the reveal layer |
| Grammar-cells lesson: structure that feels like text | MPS ergonomics research | Edit-as-testify input surfaces; don't make experts learn the tree |
| One-directional derivation, hand-patching forbidden | MDE where it worked (SCADE/Simulink) | Belief views and derived artifacts never edited in place |
| Suspect-link flag on upstream change | DOORS/RM tooling | Auto-mark realizes-edges "suspect" when the intent entity gains a new version |
| Links asserted at work-time, gated by machinery | DO-178C | Realizes edges recorded in the same testimony as the change; checks fail loudly on dangling/suspect edges |
| Spec/status co-location | Kubernetes | Intent entities always render declared intent beside computed alignment |
| `observedGeneration` on status | Kubernetes | Alignment status carries evaluated intent-version + evidence watermark (the label) |
| `conditions[]` shape | Kubernetes | Standard shape for graded, justified, timestamped alignment beliefs |
| Per-field ownership (server-side apply) | Kubernetes | Actor-scoped authority over which agent computes/attests which alignment facets |
| Git-as-intent-ledger + Synced/OutOfSync UX | Flux / Argo CD | Drift as a first-class, dashboarded epistemic grade |
| Plan-before-apply diff | Terraform / kubectl diff | "What would change under this intent/context version" preview queries |
| `#SPILL!` collision semantics | Excel dynamic arrays | Named, loud failure when derived realization collides with testimony-owned values |
| Values-and-branches visible in the logic | Subtext | `why(value)` inline; evaluation paths inspectable at the reveal layer |
| Spec templates + constitution + checklists | GitHub Spec Kit | Intent-authoring scaffolds; Gneiss supplies the ledger and loop they lack |
| Controlled natural language for expert-authored rules | DTCA ALEF/RegelSpraak | Existence proof for statutes authored by domain experts at national scale |

## Failure modes to design against

- **All-or-nothing substrate** (killed IP): value must arrive on one artifact pair first, not after total adoption.
- **Trust-us opacity** (killed Intentional Software commercially): something small must be live and citable early.
- **The interop moat** (projectional editing's 20-year lesson): if standard diff/review/search can't see the intent store, adoption stalls at institutional-mandate niches.
- **Model rot via hand-patched realizations** (MDA): a derived artifact edited outside the loop with no detection makes the intent record a lie nothing exposes.
- **Traceability as audit artifact** (DOORS): edges maintained *for* a review rot; edges consumed by machinery that fails loudly survive.
- **Fighting controllers / flapping** (k8s): require ownership and hysteresis on alignment computation.
- **Orphaned intent** (k8s): unwatched intents default to *unknown/unwatched*, never green.
- **Stale status trusted as fresh**: label-checking non-optional in presentation.
- **Regeneration myth / spec drift** (LLM-era SDD, MDA redux): intent artifacts without a drift detector reproduce the failure, faster and nondeterministically.
- **Provenance-free intension** (Excel): intensional references without identity, history, or justification cap out at single-user robustness.
- **Term dilution** (intent-based networking): "intent" is already a marketing word; keep claims operational — an intent is an entity, a realizes-edge is a ledger row, drift is a computed grade with a label.

## Sources

- Simonyi, MSR TR-95-52: https://www.microsoft.com/en-us/research/wp-content/uploads/2016/02/tr-95-52.doc
- Intentional programming / Intentional Software: https://en.wikipedia.org/wiki/Intentional_programming · https://en.wikipedia.org/wiki/Intentional_Software
- Microsoft acquisition (2017-04-18): https://blogs.microsoft.com/blog/2017/04/18/microsoft-acquire-intentional-software-expand-future-productivity-capabilities/ · https://techcrunch.com/2017/04/18/microsoft-acquires-intentional-software-and-brings-old-friend-back-into-fold/
- Hal Berenson post-mortem: https://hal2020.com/2017/04/18/does-intentional-finally-have-clear-intent/
- Fowler: https://martinfowler.com/bliki/IntentionalSoftware.html · https://martinfowler.com/articles/languageWorkbench.html
- InfoQ JAOO 2007 (Capgemini pension workbench): https://www.infoq.com/news/2007/09/intentional-at-jaoo/
- c2 critique: https://wiki.c2.com/?CritiqueOfIntentionalProgramming=
- JetBrains MPS: https://www.jetbrains.com/mps/ · https://blog.jetbrains.com/mps/2026/06/the-mps-2026-1-rc1/ · DTCA/ALEF: https://resources.jetbrains.com/storage/products/mps/docs/MPSQuest_DTO_Case_Study.pdf · mbeddr: https://jetbrains.github.io/MPS-extensions/
- Projectional editing: https://voelter.de/data/pub/fse2016-projEditing.pdf · Grammar cells: https://www.mathematik.uni-marburg.de/~seba/publications/grammar-cells.pdf
- Subtext: https://www.subtext-lang.org/OOPSLA05.pdf · https://alarmingdevelopment.org/
- Elastic SDFs (JFP 2020): https://www.microsoft.com/en-us/research/uploads/prod/2018/11/ElasticSDFs.pdf · Calc Intelligence: https://www.microsoft.com/en-us/research/project/calc-intelligence/
- Excel GROUPBY/PIVOTBY: https://techcommunity.microsoft.com/blog/excelblog/new-aggregation-functions-groupby-and-pivotby/3965765
- GitHub Spec Kit: https://github.com/github/spec-kit · https://developer.microsoft.com/blog/spec-driven-development-spec-kit
- DO-178C traceability: https://www.parasoft.com/learning-center/do-178c/requirements-traceability/ · https://www.jamasoftware.com/requirements-management-guide/requirements-traceability/traceability-matrix-101/
- IBN reality check: https://www.networkworld.com/article/963821/what-is-intent-based-networking.html
- Kubernetes API conventions (spec/status, conditions, observedGeneration): https://github.com/kubernetes/community/blob/master/contributors/devel/sig-architecture/api-conventions.md · Flux: https://fluxcd.io/ · Argo CD: https://argo-cd.readthedocs.io/ *(standard docs, not re-fetched)*
- Voelter et al., SoSyM 2019 (Voluntis/MPS healthcare) *(not independently verified)*
