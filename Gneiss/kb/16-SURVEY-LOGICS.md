# Survey: Logics of Belief, Time, Obligation & Intention

*Research-agent survey for Gneiss, 2026-07-05. Commissioned after the future-tense design ([33-FUTURE-TENSE.md](33-FUTURE-TENSE.md)); claims C1–C4 submitted for adversarial checking. Verdicts absorbed into 33 §9. Focus: what is practically load-bearing for (a) the Language's vocabulary for types of consistency and (b) future-directed stances.*

## TL;DR — top 5 takeaways for Gneiss

1. **The single best import is RV-LTL's four-valued verdict lattice** (true / false / presumably-true / presumably-false, over a finite prefix with open future). It is the exact semantics Gneiss expectations need, it is battle-tested in maintained tools (R2U2, MonPoly), and it composes cleanly with Gneiss's cutoff contexts. Adopt it nearly verbatim.
2. **RV theory gets trace-completeness for free; Gneiss must buy it explicitly.** Classical monitorability assumes the monitor sees every event, in order. Gneiss's watermark/coverage requirement is not merely *analogous* to monitorability — it is the explicit form of RV's hidden synchrony assumption. This makes C1 sound, with an amendment (below).
3. **The deontic literature's one operational lesson is representational**: never encode contrary-to-duty obligations as material implications; encode them as separate norms triggered by recorded violation facts, with an explicit obligation typology (achievement / maintenance / punctual, persistent-after-violation or not). Gneiss's "rules + assertions + consequence chains" plan is the literature's own recommended resolution — but only if it adopts the typology.
4. **BDI implementations threw away the logic and kept the lifecycle.** No deployed BDI system ever computed KD45 or BDI-CTL. What survived 30 years of practice: belief base as ground atoms (a database), intention as a data record with commitment conditions, and Cohen & Levesque's persistent-goal drop conditions (achieved / believed-impossible / deadline) as the state machine. That is precisely "stances over a kernel" — strong precedent for the Gneiss design position.
5. **For C4, swap the citation.** Justification logic gives Gneiss good operator *names* (apply, sum, check), but the realization theorem is the wrong theoretical backing for `why()`. The right backing is database provenance semirings (Green–Karvounarakis–Tannen), which were built for exactly ground, token-level, query-shaped justification.

---

## 1. Epistemic/doxastic modal logic (K, KD45, S5)

**Core, practically.** Belief/knowledge as box operators over possible worlds. The axioms are a menu of commitments: **K** (closure under consequence), **T** (factivity — knowledge only), **D** (consistency: never believe both φ and ¬φ), **4** (positive introspection), **5** (negative introspection). KD45 = idealized belief; S5 = idealized knowledge. The semantics is *type-level*: propositions are sets of worlds; there are no tokens, no sources, no time.

**What practical systems took from it:** almost nothing axiomatically. The survivors are (a) vocabulary — *factive vs non-factive*, *introspection*, *consistency* — and (b) epistemic *model checking* of protocols (MCK, MCMAS), which verifies designs offline rather than running a belief store. Every fielded "belief base" (BDI platforms, databases) is a set of ground literals with consistency maintenance — the D axiom operationalized, nothing more.

**Fit with Gneiss:** possible-worlds semantics is the wrong abstraction for a token-level ledger — Gneiss's accepted/defeated/contested are statuses of *assertions under a context*, not truth in accessible worlds. But two pieces of vocabulary earn their keep: the **D axiom as the name of the per-context consistency contract** ("every belief view is D-consistent: acceptance rules never accept φ and ¬φ in the same context"), and **factivity as the axis separating epistemic grades** (observation ≈ factive-graded; derivation/assumption/forecast ≈ non-factive).

**Verdict for Gneiss: ignore the axiomatics, adopt two words — D-consistency as the view invariant, factivity as the grade axis.**

## 2. Temporal logics: LTL, CTL, MTL, intervals

**Core, practically.** LTL: linear time, operators ◇ (eventually), □ (always), U, X. CTL: branching time with path quantifiers — relevant to model checking possible futures of a program, irrelevant to a ledger with one actual timeline. MTL adds metric bounds: ◇≤d φ ("φ within d"). Full MTL over dense time is undecidable for satisfiability (MITL restores decidability), but **monitoring** metric formulas over timestamped traces is cheap and standard — decidability of satisfiability is not Gneiss's problem. Allen's interval algebra gives 13 qualitative relations between intervals; the Halpern–Shoham modal interval logic is mostly undecidable and best avoided.

**The fragments that matter:**
- "K by D" = bounded eventuality ◇≤D K — a **co-safety** property (satisfaction confirmable from a finite prefix; violation confirmable once time provably passes D).
- "Y within window W of X" = the metric **response pattern** □(X → ◇≤W Y).
- Recurring obligations = □ over a response pattern with a recurrence generator.
- **Past-time LTL** deserves special mention: rules whose temporal operators look only backward ("if X happened and no Y since") are *always* monitorable from the prefix — the classic "declarative past" result (Lichtenstein–Pnueli–Zuck). Steering Language users toward past-form rules is a cheap way to keep rules verdict-yielding.

**Verdict for Gneiss: adopt the MTL-with-past fragment as the semantic reference for expectation/deadline syntax; adopt Allen relations as valid-time comparison vocabulary; ignore CTL and Halpern–Shoham.**

## 3. Combined epistemic-temporal logics

**Core, practically.** The interpreted-systems tradition (Fagin, Halpern, Moses, Vardi, *Reasoning About Knowledge*) combines K-operators with time and classifies agents by axioms: **perfect recall**, **no learning**, **synchrony**. Complexity results are brutal (perfect recall + common knowledge pushes into undecidability); no practical system implements these logics as engines.

**Fit with Gneiss:** Gneiss's bitemporal ledger *is* the working realization of "what did we believe at t about t'": transaction time is a perfect-recall axis made physical (append-only ⇒ the system provably remembers every prior epistemic state), and the watermark is synchrony made explicit. There is genuine value in *saying so with the standard words*: it lets one sentence — "Gneiss views satisfy perfect recall and synchrony by construction" — replace a page of explanation for logic-literate readers.

**Verdict for Gneiss: ignore the machinery, adapt the vocabulary — "perfect recall" = append-only TT, "synchrony" = watermark, `believed-at(t) about(t')` as the surface form of as-of slicing.**

## 4. Justification logic (Artemov's LP and successors)

**Core, practically.** Replace the belief box with explicit terms: `t : φ` ("t justifies φ"), with operators **application** (t·s : ψ — modus ponens carrying evidence), **sum** (t+s justifies anything either does — evidence pooling), and **proof checker** (!t : (t : φ) — evidence about evidence). Two headline theorems: **internalization** (whatever the logic proves, it proves with a term — no unjustified beliefs) and **realization** (every S4 theorem can be rewritten with explicit terms replacing boxes).

**Fit with Gneiss:** the operator vocabulary maps beautifully: application ≈ a derivation rule firing; sum ≈ multiple independent supports for one assertion; ! ≈ assertions-about-assertions. Internalization restated is a fine design invariant: *every accepted value can exhibit a justification term the ledger contains.* But note a real mismatch: **LP is monotonic — sum only ever adds support, nothing defeats.** Gneiss's acceptance is defeasible; justification logic has no native account of defeat. And **no operationalized justification-logic system exists** — implementations are proof-theoretic experiments; nothing production-shaped found in 2026.

**Verdict for Gneiss: adopt the operator names (apply / sum / check) and the internalization invariant for the Language's why-algebra; do not lean on the theorems — see C4.**

## 5. Dynamic epistemic logic (public announcement, model update)

**Core, practically.** DEL (Baltag–Moss–Solecki; van Ditmarsch et al.) treats information change as *model surgery*: an announcement [!φ] deletes ¬φ-worlds; action models generalize to private/partial observation. Famous curiosities: Moore sentences ("p, but you don't believe p") that become false when announced; arbitrary-announcement logics that go undecidable.

**Fit with Gneiss:** the federation analogy (import-with-watermark ≈ announcement event) is real but shallow. Two transferable lessons, both cautionary: (1) **announcements are events with preconditions and their order matters** — federation semantics must be defined over the *sequence* of imports, which transaction time already gives; (2) the Moore-sentence effect warns that **self-referential status assertions can invalidate themselves on import** — an imported assertion "ledger B has no assertion about X" can be falsified by the very batch carrying it; closure/coverage claims are exactly this shape, so imports of coverage assertions need care.

**Verdict for Gneiss: ignore as machinery; steal "import = announcement event" as documentation framing and the Moore-sentence caveat for imported coverage claims.**

## 6. BDI: intention theory and implemented platforms

**Core, practically.** Cohen & Levesque (1990): *intention = choice + commitment*, formalized as the **persistent goal** — maintained until the agent believes it achieved or believes it impossible. Rao & Georgeff wrapped B, D, I operators around CTL and named the **commitment strategies**: *blind* (drop only when achieved), *single-minded* (achieved or believed impossible), *open-minded* (…or the goal itself is dropped).

**What implementations kept vs dropped.** PRS, dMARS, JACK, Jason/AgentSpeak, 2APL, Jadex all dropped the modal logic entirely — no theorem prover ever ran inside a deployed BDI agent. What they kept: **belief base = ground atoms with add/delete events; goals/intentions = records on a stack with lifecycle transitions** (Jason: event → relevant plans → applicable plans → intended → executing → suspended → achieved / failed / dropped, with failure raising goal-deletion events that trigger recovery plans — *reparation as event-triggered rules*, same shape as §7). 2020s LLM-agent work (ChatBDI on Jason; the "LLM vs classic MAS" literature) re-imports BDI as the explainable skeleton around an LLM — evidence that *lifecycle-as-data* is the durable residue.

**Operational lessons for Gneiss's intention stance:** (1) C&L's persistent-goal conditions are a ready-made drop-condition spec: an intention assertion carries explicit *achieved-when*, *impossible-when*, *deadline*, *reconsider-when* clauses. (2) Commitment strategies are **named persistence policies** — three well-understood defaults the Language can offer as keywords. (3) BDI's hardest practical problem was *intention reconsideration cost*; in Gneiss this becomes "which cutoff/context re-derives intention status" — cheap, because it is a view computation, not an agent interrupt. A genuine architectural advantage worth stating.

**Verdict for Gneiss: adopt the lifecycle-as-data pattern with C&L drop conditions and commitment strategies as named policies; ignore BDI-CTL axiomatics — even the BDI community never ran them.**

## 7. Deontic logic: obligation, violation, contrary-to-duty

**Core, practically.** Standard deontic logic (SDL) is just KD with O ("obligatory") — and it is famously broken for real norms. The operationally important break is **Chisholm's contrary-to-duty (CTD) paradox**. The lesson is *representational*: a CTD obligation is not a material implication — it is a **separate norm whose triggering condition is a violation fact**. Reparational obligations ("failed X by T ⇒ must Y") are real-world escalation chains, and the fix the field converged on is exactly explicit violation events + secondary norms — Governatori et al.'s reparation chains (O(A) ⊗ O(B): B becomes obligatory upon A's recorded violation), implemented in the Regorous compliance checker with defeasible deontic logic and LegalRuleML.

**The load-bearing typology** (norm-lifecycle / normative-MAS literature, esp. Governatori & Rotolo): obligations differ in how violation is *computed* — **achievement** (do X by D; violated by closure-licensed absence at D), **maintenance** (keep X true throughout [s,e]; violated by any lapse), **punctual**; orthogonally **persistent after violation or not** (must you still do X late?), and **preemptive or not** (does doing X early count?). Norms have lifecycles of their own — in force / effective / applicable, retroactive annulment vs abrogation — which is *legal bitemporality*, mapping directly onto Gneiss's valid/transaction split. **ODRL** (W3C) is the implemented artifact worth aligning to: permission / prohibition / duty with *remedy* and *consequence* chains; its formal semantics remained draft-stage as of 2025 — align vocabulary, don't inherit semantics.

**Verdict for Gneiss: adopt the obligation typology, violation-as-event, and reparation-chain vocabulary (⊗, remedy, consequence); skip deontic logic engines — the implemented state of the art is itself rules + violation facts.**

## 8. Runtime verification / monitoring

**Core, practically.** RV asks: given a *finite prefix* of a trace and a temporal property over *infinite* behaviors, what can be concluded now? **LTL3** (Bauer–Leucker–Schallhart) gives ⊤ / ⊥ / ? (inconclusive); **RV-LTL** refines ? into *presumably true / presumably false*. **Monitorability** (Pnueli–Zaks): a property is monitorable if every prefix can still be extended to a definitive verdict — safety properties yield ⊥-verdicts from prefixes, co-safety yield ⊤-verdicts, and some properties (□◇p) never yield verdicts — a *useful lint*: the Language can statically warn "this expectation can never be definitively violated." Deadline-bounded properties are the friendly case: ◇≤D K becomes decidable at D. **Tools** genuinely maintained in 2026: MonPoly/VeriMon (metric *first-order* temporal logic — events with data, the closest shape to Gneiss assertions; VeriMon is Isabelle-verified), R2U2, TeSSLa, DejaVu, Reelay, NASA's Copilot.

**The transfer, and the catch:** RV verdict semantics is the operational twin of Gneiss expectations — but classical RV assumes a **complete, ordered** trace. The verdict "violated at D" is licensed by an *implicit* premise: nothing was missed. Gneiss's ledger is incomplete and out-of-order by design, so that premise must be purchased explicitly — the coverage/closure watermark. The MonPoly lineage has recent work on out-of-order and delayed events with verdicts held back until watermark-like conditions (*guess: TimelyMon and related Basin/Krstić/Traytel work — direction verified, tool names not*).

**Verdict for Gneiss: adopt — the RV-LTL verdict lattice for expectation statuses, monitorability as a static lint on rule syntax, and MFOTL (MonPoly) as the reference point for first-order-with-data monitoring.**

## 9. Brief: stream reasoning / temporal Datalog

LARS (window operators + ASP semantics + @-operator; Ticker) and DatalogMTL (MeTeoR scales to tens of millions of temporal facts; active through 2025) prove out two syntax decisions: **time-annotated atoms `p@t` as the base fact form** (identical to Gneiss's assertion shape) and **windows as first-class syntax**. Their hard-won lesson: *negation over a window is only sound once the window is closed* — stratified negation + window closure is the datalog-world's rendition of Gneiss's closure-licensed absence. The industrial watermark literature (Flink/Beam) covers the same ground with more operational maturity.

**Verdict for Gneiss: adapt — steal `@t` annotation and first-class window syntax; treat stream-processing watermarks as the primary engineering reference.**

---

## The four claims evaluated

### C1 — Violation-by-silence ≡ co-safety monitorability under closure
**Verdict: sound, with one correction and one amendment.** Correction: the polarity is off. "Evidence K by D" is a *co-safety* property whose *satisfaction* is prefix-detectable on arrival; its **violation** is the *safety-side* verdict, available exactly when (i) time observably exceeds D and (ii) the input is known complete through D. Classical RV gets (ii) as a free, implicit assumption; Gneiss must assert it as data. Sharpened claim: *Gneiss's coverage/closure watermark is the explicit reification of RV's implicit trace-completeness assumption; violation-by-silence is the LTL3 ⊥-verdict for a bounded eventuality, licensed only when that reified assumption holds.* Amendment: adopt the four-valued refinement — between D-passed-without-closure and closure, the honest status is *presumably-violated* (RV-LTL), not inconclusive; operationally valuable for escalation policies.

### C2 — Observation-outranks-anticipation, forecasts never retracted
**Verdict: sound as a default rung; three known failure modes need escape hatches.** (1) *Observation quality*: meteorological data assimilation never lets raw observations lexically trump forecasts — observations pass a background/plausibility check against the forecast first. Gneiss's rung should read "***accepted* observation** outranks anticipation," with a quality-gate acceptance rule upstream; a lexical rung over ungated observations lets one bad sensor defeat a good model. (2) *Out-of-order arrival*: a late observation must outrank retroactively in later-cutoff views while earlier-cutoff views still show the forecast — bitemporality handles this natively; an argument *for* the design. (3) *Forecast successions*: multiple forecasts for the same target need a recency-by-issue-time rung below the observation rung. Never-retract is affirmatively correct for scoring: proper scoring requires grading the forecast *as issued* — **scoring reads at the forecast's transaction time**, an invariant worth writing down.

### C3 — Deontic stances without a deontic engine
**Verdict: sound — this is the literature's own answer — but two traps if the typology is skipped.** Explicit violation facts + reparational rules is precisely how CTD paradoxes are operationally resolved (Governatori's ⊗-chains, Regorous, ODRL remedy/consequence). Chisholm's paradox is an argument *against* embedding SDL, not against Gneiss's plan. The traps: (1) **skipping the obligation typology** — achievement vs maintenance vs punctual have *different violation-detection computations*, and persistence-after-violation must be per-norm policy; a single generic "obligation" record computes wrong verdicts for maintenance norms. (2) **Weak vs strong permission** — "permitted because no prohibition found" is a closure-dependent inference and must say so. Minor: reparation chains can cascade; cap or make termination explicit. Pleasant surprise: legal-temporality (force vs efficacy vs applicability, retroactive annulment) maps directly onto Gneiss bitemporality — norms are assertions with valid/transaction time.

### C4 — Realization theorem as backing for "why() IS the belief modality"
**Verdict: over-claimed as stated; a weaker true claim is available.** Three gaps: (1) realization is a theorem about *theoremhood* — it does not say a belief *store* is equivalent to its justification structure. (2) LP is **monotonic** — no defeat; Gneiss's defeasible acceptance is outside the theorem's scope. (3) Gneiss's assertions are ground and quantifier-free; the hard content of realization never arises, so the theorem does no work even where it applies. The honest version: *Gneiss adopts justification logic's operator vocabulary and its internalization discipline (no acceptance without an exhibitable term), while the actual semantics of why() is provenance-theoretic.* The stronger citation is **provenance semirings** (Green–Karvounarakis–Tannen): why-provenance with application-as-⊗ and alternative-support-as-⊕ was built for exactly this token-level, query-shaped setting and comes with real theorems about query rewriting. Keep JL as inspiration; cite semirings as foundation.

---

## What the Language should steal

| Steal | From | Use in Gneiss |
|---|---|---|
| Four-valued verdicts: true / false / presumably-true / presumably-false | RV-LTL (Bauer–Leucker–Schallhart) | Expectation & obligation status under open future; "presumably-violated" between deadline and closure |
| Monitorability lint | Pnueli–Zaks | Static warning: "this expectation can never reach a definitive verdict" |
| `by(D)`, `within(W) of X`, response pattern □(X→◇≤W Y) | MTL / MLTL | Deadline & window syntax with known semantics |
| Prefer past-form rules (always verdict-yielding) | Past-time LTL, declarative past | Rule-authoring guidance + fast monitoring path |
| Allen's 13 interval relations | Allen interval algebra | Valid-time comparison predicates (during, overlaps, meets…) |
| "D-consistency" as the view invariant; "factive" as grade axis | Doxastic logic (KD45) | One-word names for the acceptance contract |
| "Perfect recall", "synchrony" | Epistemic-temporal (interpreted systems) | Standard names for append-only TT and watermark |
| `apply` / `sum` / `check` operators; internalization invariant | Justification logic (Artemov LP) | why-algebra operator names; "no acceptance without exhibitable term" |
| ⊗/⊕ semantics for why() | Provenance semirings (GKT) | Actual foundation for why-extraction and evidence combination |
| Achievement / maintenance / punctual; preemptive; persistent-after-violation; ⊗ reparation chains | Defeasible deontic logic (Governatori et al.) | Obligation stance schema; correct violation computation per type |
| duty / remedy / consequence vocabulary | ODRL (W3C) | Interop-friendly naming for consequence chains |
| Drop conditions (achieved / impossible / deadline / reconsider); blind / single-minded / open-minded | Cohen & Levesque; Rao & Georgeff | Intention lifecycle fields + named commitment policies |
| `p@t` atoms; first-class windows; negation-needs-closed-window | LARS / DatalogMTL | Surface syntax for windowed expectation rules |

## Sources

- Runtime verification tools list — https://runtime-verification.github.io/tools.html
- Leucker & Schallhart, *A Brief Account of Runtime Verification* — https://www.isp.uni-luebeck.de/sites/default/files/publications/jlap08_1.pdf
- Bauer, Leucker, Schallhart, *Comparing LTL Semantics for Runtime Verification* — https://www.semanticscholar.org/paper/17d419d93c0800c84edbb2b7228ba42bc22b83e2
- Four-valued monitorability of ω-regular languages — https://arxiv.org/pdf/2002.06737
- Aceto et al., *An Operational Guide to Monitorability* — https://arxiv.org/pdf/1902.00435
- R2U2 Playground (2025, TU Wien) — https://repositum.tuwien.at/bitstream/20.500.12708/219547/1/
- VeriMon — https://link.springer.com/chapter/10.1007/978-3-031-17715-6_1
- Reelay (2026) — https://arxiv.org/html/2604.22384
- SEP, *Justification Logic* — https://plato.stanford.edu/entries/logic-justification/
- Artemov, *The Logic of Justification* — https://sartemov.ws.gc.cuny.edu/files/2014/01/RSL2008.pdf
- Brünnler, Goetschi, Kuznets, *A Syntactic Realization Theorem for Justification Logics* — http://www.aiml.net/volumes/volume8/Bruennler-Goetschi-Kuznets.pdf
- ODRL Formal Semantics (W3C draft) — https://w3c.github.io/odrl/formal-semantics/
- Evaluation and Comparison Semantics for ODRL (2025) — https://arxiv.org/html/2509.05139v1
- ODRL grounding in UFO-L (2026) — https://arxiv.org/abs/2606.24344
- Models of the Chisholm set — https://arxiv.org/pdf/1607.02189
- BDI software model — https://en.wikipedia.org/wiki/Belief%E2%80%93desire%E2%80%93intention_software_model
- ChatBDI / LLM+BDI survey (2025) — https://arxiv.org/pdf/2509.02515
- BDI + ATL planning (2025) — https://arxiv.org/pdf/2509.15238
- MeTeoR: Practical Reasoning in DatalogMTL — https://arxiv.org/abs/2201.04596
- DatalogMTL talks (TIME 2024) — https://drops.dagstuhl.de/entities/document/10.4230/LIPIcs.TIME.2024.3
- Incremental maintenance of DatalogMTL materialisations (2025) — https://arxiv.org/html/2511.12169
- ETH Zurich, MonPoly group — https://infsec.ethz.ch/research/projects/mon_enf.html

**Unverified / flagged:** exact tool names for out-of-order first-order monitoring in the MonPoly family (guess: TimelyMon; direction confirmed, name not). Cohen & Levesque (1990), Rao & Georgeff (1991), Pnueli–Zaks (2006), Governatori's ⊗-chains/Regorous, and Green–Karvounarakis–Tannen (PODS 2007) cited from background knowledge — canonical, but page-level claims not re-verified this session. "No operationalized justification-logic system exists" is a strong negative supported by the SEP survey and absence of search hits, not proof.
