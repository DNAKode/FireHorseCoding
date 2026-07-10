# Discussion Agenda: The Decisions That Actually Need Making

> **The forward plan (agreed 2026-07-05).** Govert: "The crystals will form and start growing when the solution has the right concentration." Four phases:
> **Phase H — the history mine** (running): three primary-source surveys down the rabbit hole Kay opened — 19A the augmentation lineage (Licklider, Engelbart 1962, the NLS Journal/viewspecs/CODIAK, the autopsy), 19B the PARC–VPRI lineage (Smalltalk changes-file-as-ledger, Croquet/TeaTime determinism, the Worlds paper, PIE, and the STEPS *method* extracted as a repeatable procedure), 19C the near-misses (memex trails, Sketchpad constraints, Halasz's Seven Issues scored against Gneiss, IBIS→ADR capture economics, Lotus Agenda).
> **Phase M — Maxwell's equations**: STEPS-ify the corpus — hunt the one-page runnable metacircular kernel and the algebra under it; the shape of the ah-ha; candidate formulations compared; grad-student-reimplementable-in-a-week as the test.
> **Phase S — synthesis**: the bundle — the Model paper as a from-scratch v2 of the seed, absorbing everything.
> **Phase C — challenge**: De Bono's six hats pass + Brenner-style diverse-perspective interrogation + the long-parked adversarial round (targets: the witness stand, D25 economics, intent rot, and "find our Gray"). Local skills exist for this phase (modes-of-reasoning analysis, dueling idea wizards, brenner sessions) when we get there.

*The corpus takes positions so there is something to disagree with. This file ranks the open decisions, states each position and its strongest counter, and names what evidence would settle it. Ordered by how much downstream design each decision moves.*

## D1 — Kernel size: five primitives, or the seed's twelve?

**Position:** {Entity, Assertion, Transaction, Justification, Context}; everything else is a stance ([20-KERNEL.md](20-KERNEL.md)).
**Strongest counter:** Justification could fold into Assertion (`derived_from` list) making it four — or Hypothesis/Decision deserve promotion because the review workflow is the product's heart and "decision = assertion" may prove a fiction maintained by convention (kernel falsification test 4).
**Settles it:** P0's property tests + P1 in production. If decision workflows need state machines beyond assertion-append, promote Decision.

## D2 — Time: two stored axes + context pins, or ontology time as a real axis?

**Position:** valid + transaction time stored; ontology/rule/report versions are definition-cutoff pins because definitions live in the ledger ([21-TIME.md](21-TIME.md)); the 2×2 (data × definitions) generates all report modes.
**Strongest counter:** a third axis ("decision time" ≠ transaction time) for offline decisions entered late; also `source_recorded_at` may deserve envelope status for import-heavy systems.
**Settles it:** P2 demo + one real Smoothscrape import scenario. Current lean: promote `source_recorded_at` to the envelope; refuse a third axis.

## D3 — Decision targeting: assertion ids, claim keys, or both?

**Position:** both, with claim keys the norm for hypotheses ([22-BELIEF-ENGINE.md](22-BELIEF-ENGINE.md) open Q2, [23-STORAGE.md](23-STORAGE.md) §5). The surveys' strongest cross-validation supports this (Senzing/Foundry/Zingg: "the overlay is only as durable as its key").
**Strongest counter:** two targeting modes complicate the belief fold and invite subtle bugs (decision targets claim; a *different* assertion asserts the same claim with wildly different confidence — should one rejection kill both? Position says yes; is that always right?).
**Settles it:** P1 acceptance demo (c) — the rebuild-with-changed-method test — plus a deliberately adversarial case list.

## D4 — Are decisions ever method-scoped?

**Position:** no — rejecting `sameAs(A,B)` rejects the claim regardless of which matcher proposed it (claim keys exclude method).
**Strongest counter:** "reject this OCR-based match" might mean "the OCR evidence is bad," not "these are different people" — the reviewer's intent is ambiguous, and conflating them poisons future better-evidence hypotheses.
**Settles it:** UX design for the review queue: perhaps two verdicts (`reject_claim` vs `reject_evidence`) — which is just two decision predicates, no kernel change. Needs a real reviewer (Govert) to say which they *mean* when they click reject in Smoothscrape today.

## D5 — Belief admission of unreviewed high-confidence hypotheses?

**Position:** per-context admission thresholds, always badged downstream ([22-BELIEF-ENGINE.md](22-BELIEF-ENGINE.md) open Q1).
**Strongest counter:** Smoothscrape practice may show badges get ignored and auto-admitted links get treated as confirmed — in which case admit-only-decided is safer and the queue must simply be kept small.
**Settles it:** three months of P1 telemetry: how often are auto-admitted links later rejected?

## D6 — Substrate for P2+: hand-rolled ledger, or Marten underneath?

**Position:** undecided by design; P2 includes the 2-day Marten sub-experiment. Priors: hand-rolled (assertions are finer than event streams; Marten drags Postgres-only), but Marten's projection/rebuild machinery is exactly right and MIT-licensed ([13-SURVEY-INCREMENTAL.md](13-SURVEY-INCREMENTAL.md)).
**Also in scope:** SQLite for P1/P2 prototypes; Postgres vs SQL Server as first target — likely decided by which host system (Smoothscrape vs AIMS) converges first.

## D7 — Platform and stack?

**Deferred by design** (corrected 2026-07-04; see [03-NOTATION.md](03-NOTATION.md)). The conceptual model must stand without naming a platform; what any substrate must provide is stated as the substrate contract S1–S5. Prototypes pick conveniences for falsification speed and their code expires. A real platform decision belongs in the post-discussion planning pass, informed by which host systems adopt first — and by then it should feel like shopping against S1–S5, not like architecture.

## D8 — How literal should the accounting UX be?

**Position:** adopt the lexicon (as-reported/restated, close, adjust) and *period close as a first-class closure declaration* for reporting-heavy systems (AIMS mass reports); refuse balancing metaphors and journal-entry ceremony ([12-SURVEY-INDUSTRY.md](12-SURVEY-INDUSTRY.md) §7's break-points).
**Open flavor question:** should `AuditAsOf` contexts be creatable by any user (cheap, they're just pins) or a governed act (like closing a period)? Lean: cheap to create, governed to *publish*.

## D9 — The agent interface: how far, how soon?

**Position:** the read side (belief views + provenance handles under a named context) and the write side (evidence + proposals only) should be designed into the A2 library API from day one — it is nearly free given the kernel, and it may be Gneiss's strongest external story ([24-CONTEXTS.md](24-CONTEXTS.md) §7). An MCP server over belief views is the obvious eventual shape (the dbt/Cube 2026 trajectory confirms the market direction).
**Strongest counter:** scope discipline (risk: solo-maintainer). Agent interface as *design constraint* now, *implementation* after P4.

## D10 — Naming and publication posture

Keep "Gneiss"? (Good name: bedrock, banded, metamorphic — formed from prior material under pressure.) And: is this corpus private R&D, or does it become a public pattern-book/essay series at some point? The kill-criteria outcome "ship as a pattern book" ([32-RISKS.md](32-RISKS.md) §4.3) would be *strengthened* by writing-in-public pressure. No position taken — genuinely Govert's call.

---

## Added 2026-07-04, after the imperfection discussion

Govert's objection — archival pressure and distributed imperfection break the pure contract — produced [25-IMPERFECTION.md](25-IMPERFECTION.md): the ledger becomes evidence about itself (coverage maps, seals, epochs), purity is replaced by monotone degradation with epistemic grades, and distribution becomes federation of single-sequencer ledgers treating each other as fallible sources. New decisions **D11** (coverage granularity), **D12** (distribution stance — refuse consensus protocols?), **D13** (retention schedule defaults per stance), **D14** (grade UX loudness) are detailed in that document's §9, along with proposed invariant amendments I1′/I3′/I4′/I8/I9 to [20-KERNEL.md](20-KERNEL.md) — deliberately *proposed*, not applied, pending this discussion. The amnesia/restore/corruption drills are now listed as P0/P2 acceptance requirements in spirit; if adopted, [31-PROTOTYPES.md](31-PROTOTYPES.md) should be amended.

---

## Added 2026-07-04 (second round): parity, evolution, fluid worlds

Three further threads from discussion, each with a document: [26-DECIDERS.md](26-DECIDERS.md) (human/agent parity — regenerability × authority replace the human/machine axis; standing policies over verdict streams), [27-EVOLUTION.md](27-EVOLUTION.md) (bedrock/spine/flesh stratification — the fixed point is the manner of change), [28-FLUID-WORLDS.md](28-FLUID-WORLDS.md) (stuff-and-flow stance library; probe P-1c). The §5b amendment to [25-IMPERFECTION.md](25-IMPERFECTION.md) records that external sources and organ stores are imperfect witnesses too ("there is no replay of the world"), and the retention table was corrected accordingly.

- **D15 — Bedrock/spine boundary.** Is B3 (self-description) bedrock or spine? Is per-ledger total order bedrock? Position: bedrock = B1+B2 only; everything else amendable by ceremony.
- **D16 — Authority lattice + regenerability retention.** Adopt authority-ranked decision precedence and regenerability-based retention tiers ([26-DECIDERS.md](26-DECIDERS.md))? Position: yes to both; the strainer rung reword is a proposed amendment to 22 §4.
- **D17 — Verdicts vs statutes.** When must an agent record per-item decisions rather than propose a standing policy? Position: only for case-specific judgment (override or policy-routed review); machine-rate verdict streams are a design smell with an alarm metric.
- **D18 — World-model stance libraries.** Run probe P-1c (bulk-material cycle) and audit kernel docs for smuggled object-persistence assumptions? Position: yes — P-1c joins P-1 as the second paper gate, and a 15-SURVEY-MATERIAL-FLOW (EPCIS 2.0, hydrocarbon allocation, ISA-88) is queued behind it.
- **D20 — Adopt the Codd program as the structuring ambition?** ([05-CODD-PROGRAM.md](05-CODD-PROGRAM.md)) Four layers: Model paper (v2 of the seed, with theorems) → Language spec (+ compiler-to-SQL over the relational binding) → reference Engine (the P-track re-aimed) → Ecosystem (operational world-model platforms built by others; A4 re-scoped from "not now" to "not ours — ours to make buildable"). Position: yes; the Model paper becomes the next major artifact after the current discussion round closes.
- **D21 — The Language.** Commit to a spec-sketch round; adopt machine-first authorship (agents write, humans audit; spend the elegance budget on the algebra, not the syntax); feature typed-missingness-repairs-NULL as the flagship "stronger underpinnings" claim; naming deferred but collect candidates.
- **D22 — Mechanization spike.** Lean (or similar) proofs of determinism and monotone degradation alongside P0 — machine-checked theorems as a credibility artifact nothing in the surveys possesses, and formalization pressure as the cheapest kernel-bug finder. Position: yes, small spike, agent-driven.
- **D19 — Operating envelope as declared data** ([29-ENVELOPE.md](29-ENVELOPE.md), from Govert's dissection of the survey's "single-node, human-cadence" sentence). Adopt envelope assertions (W/R/T/S/V), engineering justifications citing them with drift-defeat monitoring, and per-(view, envelope) rung selection? Position: yes; scenarios E1–E4 become the standing planning fixture, and "aspiration stated as description" joins the risks list as a named hazard. Related follow-up: a systematic assumption audit of the whole corpus (the survey sentence is unlikely to be the only offender) — candidate task for the next working session.

---

## Questions for Govert (the ones the corpus cannot answer)

1. In Smoothscrape today, when you reject a suggested match, do you mean "different people" or "bad evidence"? (D4 hinges on this.)
2. What did the last three *painful* corrections across AIMS/Smoothscrape actually cost, in hours and trust? (Calibrates how much kernel the pain justifies — the P2 gate's denominator.)
3. Are there real users (customers, operators) who would consume an audit/restated distinction, or is that ring speculative for now? (Sequences B5.)
4. Is there any near-term scenario with *shared* entities across systems, or is A3 safely years away?
5. Which system gets prototype attention first — the answer sequences everything: Smoothscrape (P1 value now) vs AIMS (richer ring coverage)?
6. Appetite check: is the 8–12 week two-track program ([31-PROTOTYPES.md](31-PROTOTYPES.md)) the right size, or should the first commitment be just P-1 + P1 (~3 weeks)?

## Proposed next-session agenda

1. Walk the two-plane model and five-primitive kernel — agree, amend, or shrink (D1).
2. Decide D3/D4 with real Smoothscrape cases on screen.
3. Pick the first prototype commitment (question 6) and the host system (question 5).
4. If committed: green-light the planning-workflow pass for the chosen prototypes (full plan → review rounds → beads).
