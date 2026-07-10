# Survey: The Near-Misses (Memex, Sketchpad, NoteCards, IBIS, Agenda)

*Primary-source mine for Gneiss, 2026-07-05 (Phase H, miner 3 of 3). Sources marked [FETCHED] were retrieved and read in the original this session; everything else labeled. Claims N1–N4 adjudicated. Quotes verbatim from fetched texts.*

## TL;DR — top 5 takeaways for Gneiss

1. **Capture economics is the recurring killer, and it has a precise mechanism.** gIBIS wrote its own autopsy in 1988: "the cognitive overhead required to segment the 'muck' into discrete thoughts, identify their types, label them, and link them is prohibitive." Grudin supplied the political economy: the people who do the capture work are not the people who benefit. Every structured-capture system in this survey died of this; every survivor cheapened capture, professionalized it, or made it pay back instantly.
2. **Halasz's Seven Issues (1988) really does read as a partial requirements review of Gneiss** — virtual structures are belief views almost verbatim ("defined intentionally... by specifying a description of their components... recomputed" each access), and his versioning section anticipates addressable deltas and layered contexts. But Gneiss currently has no answer to structure search (graph-pattern queries like "find circular chains of supports links") and only a thin answer to collaboration-as-social-process.
3. **ADR vs gIBIS is the controlled experiment for the Intent stance (D26).** Same goal (durable decision rationale), opposite capture design. ADR's deltas — one page, prose not typed graphs, in-repo, written at decision time, immutable-with-supersession — are empirically the survival envelope. Note what Nygard *dropped*: the argument graph itself. ADRs survived by discarding exactly what IBIS held sacred.
4. **Sketchpad shows when intent capture is *not* overhead: when the engine repays it in seconds.** A declared constraint immediately fixes your drawing and keeps re-satisfying it forever ("manual change of a critical part will automatically result in appropriate changes to related parts"). Intent capture survives where realization is automatic and same-session, dies where it is archival. Gneiss's realizes-edges must pay rent (drift alarms, generated checks), not just accumulate.
5. **Bush's trails are the presentation-layer organ nobody shipped**, because trails require durable, non-fading targets — which requires exactly the bitemporal, pinnable substrate the web lacks and Gneiss has. But trails also cost curation labor; Bush knew it and invented a *profession* to pay for it. Mechanism buildable ≠ trails will get built.

---

## Bush, "As We May Think" (1945) [FETCHED]

**Mechanics.** A microfilm desk: "slanting translucent screens," keyboard, levers; input by dry photography. Every item carries "a number of blank code spaces"; to associate two items, "the user taps a single key, and the items are permanently joined." Bush's target was the filing cabinet: data "filed alphabetically or numerically... traced down from subclass to subclass," versus the mind, which "operates by association." His proposal: "Selection by association, rather than by indexing, may yet be mechanized."

**Trails — the part hypertext dropped.** A trail is not a link. It is a *named, durable, shareable, first-class artifact*: "When the user is building a trail, he names it, inserts the name in his code book, and taps it out on his keyboard." He annotates ("Occasionally he inserts a comment of his own, either linking it into the main trail or joining it by a side trail"). Trails persist — "his trails do not fade" — and are transferable: he "photographs the whole trail out, and passes it to his friend for insertion in his own memex." Two economic institutions follow: "a new profession of trail blazers, those who find delight in the task of establishing useful trails through the enormous mass of the common record," and "wholly new forms of encyclopedias... ready-made with a mesh of associative trails running through them."

**What got lost when hypertext took links and dropped trails:** (a) the *path* as an addressable object distinct from its nodes; (b) authorship and provenance of the association itself; (c) durability (web links rot; Bush specified non-fading); (d) the labor market for curation. The web kept only the cheapest fragment — the anonymous, unidirectional, embedded link — because it was the only fragment with near-zero capture cost.

> **What they knew that we forgot:** the association is itself a document — named, owned, annotated, shareable, durable.

**Verdict for Gneiss:** trails map to curated sequences over labeled views, and the ledger's non-fading substrate is the missing precondition. See claim N4 for the caveat.

## Sutherland's Sketchpad (1963 thesis) [FETCHED — UCAM-CL-TR-574 electronic edition, full text extracted]

**Constraints as declared intent.** "The major feature which distinguishes a Sketchpad drawing from a paper and pencil drawing is the user's ability to specify... mathematical conditions on already drawn parts of his drawing which will be automatically satisfied by the computer." Constraint types are generic blocks; each is *defined by an error subroutine* — "The computed error is a scalar which the constraint satisfaction routine will attempt to reduce to zero." Satisfaction: a one-pass ordering method (maze-solving propagation of "free" variables) with fallback to relaxation (iterative least-mean-squares, monotone error decrease). There are even one-way ("for reference only") constraints — 1963 dataflow — with Sutherland warning about their instability (A←2B, B←2A "would grow without bound").

**Intent-realization framing, in Sutherland's own words:** "Construction of a drawing with Sketchpad is itself a model of the design process... the geometric constraints applied to the points and lines of the drawing model the design constraints which limit the values of design variables." And the payoff loop: "Since a drawing stored in the computer may contain explicit representation of design conditions in its constraints, manual change of a critical part will automatically result in appropriate changes to related parts." That is intent → realizes-edge → automatic re-satisfaction, running on a TX-2.

**Master/instance:** "If some change to the basic transistor symbol is made, this change appears at once in all transistor symbols without further effort. Most important of all, the computer 'knows' that a 'transistor' is intended at that place in the circuit." Identity-with-propagation plus *machine-legible intent*.

**Recursive merging:** merging two dependent things forces merger of their dependencies, and "all constraints which applied to any of the four original end points now apply to the appropriate one of the remaining pair" — constraint-preserving entity resolution. Gneiss's entity-merge under claim keys must solve exactly this: merges carry justifications and decisions forward.

**Ring structure:** doubly-linked circular registration of every relationship — chosen for consistency maintenance over storage economy; the changes needed "to keep the ring structure consistent led to useful facilities such as recursive merging."

> **What they knew that we forgot:** declared intent is worth capturing when an engine re-satisfies it automatically and visibly. The capture *is* the work, not overhead on the work.

**Verdict for Gneiss:** the existence proof for the Intent stance — and the bar for it. If realizes-edges don't trigger recomputation someone can watch, they are gIBIS nodes, not Sketchpad constraints.

## ThingLab and the constraint lineage (brief; recalled, not fetched)

Borning's ThingLab (PARC, 1977–79; TOPLAS 1981) generalized Sketchpad inside Smalltalk: user-defined constraint classes, multi-way constraints, compiled satisfaction plans. Lineage: ThingLab II, DeltaBlue/SkyBlue, Garnet/Amulet, then Cassowary (~2001). Where constraints live today: **spreadsheets** (one-way dataflow constraints — the most successful constraint system ever shipped), **layout engines** (Apple Auto Layout is literally Cassowary), and **solvers** (SAT/SMT/MiniZinc) as batch specialists. General-purpose constraint *environments* died: debugging over-/under-constrained systems was opaque. The survivors share Sketchpad's property — instant, visible re-satisfaction — and add a second: users never author "constraints," they author formulas and layouts. The formalism hid inside a cheaper authoring surface.

**Verdict for Gneiss:** alignment/drift computation should feel like a spreadsheet recalc, not a solver invocation; and "why is this over-constrained" (conflicting intents) needs a first-class answer — why(value) for failures, not just values.

## NoteCards and Halasz's "Seven Issues" (1988) [FETCHED — full CACM text; plus the 1991 "Seven Issues: Revisited" transcript, FETCHED]

NoteCards (Halasz, Trigg, Moran; Xerox PARC): typed cards, typed directional links, browsers (computed structural diagrams), fileboxes (mandatory hierarchical filing). The seven issues, precisely:

1. **Search and query.** "Navigational access by itself is not sufficient... search and query needs to be elevated to a primary access mechanism on par with navigation." Two classes: *content search* and *structure search* — graph patterns, e.g. "a circular structure containing a node that is indirectly linked to itself via an unbroken sequence of 'supports' links. This query could be used, for example, to find circular arguments." Queries double as interface filters.
2. **Composites.** The node/link model "lacks a composition mechanism." Inclusion (part-whole, operations propagate) must be semantically distinct from reference. Open questions: multi-membership, links to a node *as it exists within a composite*, version propagation from part to whole.
3. **Virtual structures.** The "premature organization" problem: segmentation, titling, and filing "all require the user to have such knowledge up front," so "users' conceptual structures have a tendency to change faster than their corresponding NoteCards structures." Fix: structures "defined intentionally... by specifying a description of their components," recomputed on access, adapted from relational views, with a *non-differentiation principle*: "Any operation possible on a base hypermedia entity should apply to virtual structures as well." Virtual links may have intensional destinations: "the node containing the currently strongest evidence that supports ClaimX" — legal even before any evidence exists.
4. **Computation in/over networks.** Hypermedia is passive; "unlike expert systems... hypermedia systems do not include inference engines that actively derive new information and enter it into the network." He proposes merging frame-system machinery — "truth maintenance, inference engines, and rule-based reasoning" — into hypermedia.
5. **Versioning.** NoteCards had none ("altered in place when modified"). He praises PIE's two-level model: per-entity version histories plus **layers** (coordinated change-sets) composed into **contexts**, including private-context collaboration over a shared base. Both versions *and deltas* "should be addressable within the system, that is, they should be possible hits for a search."
6. **Collaborative work.** Two halves: mechanics (long transactions, soft locks, *notification at intent-to-update time*) and social process — "mutual intelligibility," support for *procedural* activities (users hand-maintained history cards and convention-discussion cards), and a "rhetoric of hypermedia."
7. **Extensibility/tailorability.** "Each new NoteCards user is faced with a significant database design task"; the programmer's interface succeeded for programmers and left everyone else stranded.

In 1991 [FETCHED] Halasz compressed issues 1–3 into "Ending the Tyranny of the Link," kept computation, collaboration, tailorability, added open systems / very-large hypertexts — and **demoted versioning** ("not that I don't think it's important..."), noting even software-engineering users ranked it low.

> **What they knew that we forgot:** extensional structure decays as understanding evolves; only intensional (computed) structure tracks a moving mind. And they knew it in production, from watching users pile cards in a single filebox to dodge premature filing.

**Verdict for Gneiss:** the closest thing to a prior requirements document that exists. Scorecard under claim N1.

## The IBIS lineage → ADRs

**Rittel's IBIS** (Kunz & Rittel 1970; read here via Conklin & Begeman's account [FETCHED]): design for "wicked" problems is "fundamentally a conversation among the stakeholders" — Issues, Positions, Arguments, nine legal link types. No stopping rule; resolution is a marked Position.

**gIBIS** (Conklin & Begeman, ToIS 1988) [FETCHED]: colored graphical browser, typed nodes/links over a relational server, collaborative over LAN, explicitly aimed at "the capture of design history: the decisions, rejected options, and trade-off analysis — in short, the rationale behind the design itself." Its §5.4 ("The Dangers of Premature Segmentation") is the classic failure autopsy, in the authors' own words: users demanded a "protonode" for unstructured thought; the early design phase "must be allowed to proceed in a vague, contradictory, and incomplete form for as long as necessary"; and "in the moment of struggling to solve the problem, the cognitive overhead required to segment the 'muck' into discrete thoughts, identify their types, label them, and link them is prohibitive." §5.5 adds the resolution problem: real resolutions "transcend the fixed options which were originally perceived to be available" — breakthroughs don't fit the schema that was supposed to capture them. They shipped "Other" node/link types as an admitted "escape mechanism."

**Grudin, "Why CSCW Applications Fail" (1988)** [FETCHED]: "A factor contributing to the application's failure is the disparity between those who will benefit from an application and those who must do additional work to support it." The calendar parable: automatic meeting scheduling benefits the manager; subordinates "would have to maintain electronic calendars that they would not otherwise use." His prescription: "the best solution is to try to insure that everyone benefits directly from using the application." Design rationale is this asymmetry stretched across *time*: the capturer is present-you under deadline; the beneficiary is future-someone-else.

**QOC** (MacLean et al. 1991; recalled): deliberately *post-hoc* design-space reconstruction — academically influential, practically rare, because reconstruction is analyst-hours. **Compendium** (Buckingham Shum, Conklin et al.; recalled): IBIS survived *only* by professionalizing capture — a trained facilitator dialogue-maps the meeting live. That is Bush's trail blazer, salaried. Shipman & Marshall's "Formality Considered Harmful" (1999; recalled) generalized the capture problem to all formal structure and prescribed incremental formalization.

**The survivor: ADRs (Nygard, 2011)** [FETCHED]. Format: Title / Context ("the forces at play, including technological, political, social, and project local... value-neutral") / Decision ("full sentences, with active voice. 'We will...'") / Status (proposed, accepted, deprecated, superseded) / Consequences (all of them, "positive, negative, and neutral"). "One or two pages long." Stored "in the project repository under doc/arch/adr-NNN.md," numbered "sequentially and monotonically. Numbers will not be reused." And the append-only clause: "If a decision is reversed, we will keep the old one around, but mark it as superseded... It's still relevant to know that it was the decision, but is no longer the decision." Motivation is Grudin-shaped: the reader is your own team next quarter, collapsing the who-benefits asymmetry. Mainstreamed via ThoughtWorks Radar (~2016; recalled), adr.github.io tooling, cloud-vendor guidance.

> **What they knew that we forgot (gIBIS edition):** the schema you impose at capture time is a tax paid at the moment of *lowest* cognitive surplus. And: resolutions outgrow the option set — the decision record must allow prose that transcends the captured alternatives.

**Verdict for Gneiss:** see claim N2; this pair of autopsies is the empirical core of the whole survey.

## Lotus Agenda (1988–1992)

*(CACM paper NOT fetched — ACM DL blocked; model from secondary sources plus recall, labeled.)*

The model: **Items** are free-form text entered with zero pre-structure. **Categories** form a hierarchy; items are assigned manually or *automatically* — by text match, learned association, and semantic interpreters (the When category parses "Tuesday 3pm"). Assignment rules had conditions and actions. **Views** are saved queries rendered as grids — and *editing in a view writes back*. Recalled, moderate confidence: manual assignments were sticky — a human override survived automatic re-categorization, a genuine claim-key ancestor.

**Death:** ~200,000 copies at ~$195; Lotus declined the Windows port and shipped the paper-metaphor Organizer. Kapor's post-mortem points at the on-ramp: "hard to learn"; the first screen "didn't connect to any familiar image." The concept was retried as Chandler (2002–2008), which also failed (*Dreaming in Code*; recalled) — evidence the difficulty wasn't only Lotus's business priorities.

> **What they knew that we forgot:** capture must be free-form *first*, with categorization computed *afterwards* and revisable — items/categories/rules/views in 1988 is entities/assertions/rules/belief-views with the provenance, bitemporality, and justifications stripped out.

**Verdict for Gneiss:** closest mass-market ancestor of the data model; also a warning that rules-compute-your-structure products die when users can't predict what the rules will do — why(value) is not a luxury, it is the learnability fix Agenda lacked.

## FRESS / van Dam (brief) [Hypertext '87 keynote transcript FETCHED]

HES (1967–68, with Nelson) then FRESS (1968–): bidirectional links *with explainers*, keywords on every element usable for trails/filtered views, display independence — and "I will claim that, to the best of my knowledge, FRESS was the first system to have an undo," plus his 1987 injunction: "The most important feature in any system built today has to be indefinite undo and redo." Note the fates: undo (cheap, instantly self-rewarding, single-user) became universal; bidirectional links (require global maintenance someone else pays for) died on the web. Same economics, opposite outcomes.

## PLATO Notes / group memory (brief; recalled — thinkofit.com unreachable)

Woolley's PLATO Notes (1973) then Group Notes (1976): persistent, threaded public discussion with responses, access control; by the late '70s a campus-scale community memory; Ray Ozzie carried the design into Lotus Notes. What PLATO had: durable threaded testimony under identity. What nobody added: synthesis. Threads accumulate; no front page of truth ever condenses them — the original, still-unpaid **distillation debt**. Wikis solved distillation and dropped threaded testimony; PLATO had testimony and no distillation; Gneiss's stance (front page of truth, history below) is explicitly the union.

## Trellis and formal hypertext (very brief)

Stotts & Furuta's Trellis (1989): Petri-net hypertext — browsing semantics as a formal executable model. Marshall's Aquanet (typed relational schemas) found users refusing schema commitment, which led to VIKI and spatial hypertext: infer structure from cheap spatial gesture — incremental formalization again. Fate of all: proceedings, not products. Formal semantics without capture economics produces literature.

---

## The claims evaluated

### N1 — "Seven Issues is a 40-year-old requirements review of Gneiss"

**Verdict: substantially true, with two real gaps and one overstated mapping.**

| # | Halasz issue (1988) | Gneiss organ | Score | Notes |
|---|---|---|---|---|
| 1 | Search & query — content **and structure search**; queries as pervasive filters; one language for query = view-definition = filter | The Language | **Partial** | Structure search (graph patterns, e.g. circular chains of supports-links — "find circular arguments") has **no announced Gneiss answer**; over justification edges it would be a reasoning-hygiene tool (circular-justification detection). The one-language requirement should be adopted verbatim. |
| 2 | Composites — inclusion ≠ reference; part→whole version propagation; links-to-node-in-context | Clusters/lots | **Partial** | Grouping exists; the semantic distinction and propagation policy need explicit answers. |
| 3 | Virtual structures — intensional definition, recomputed on access, **non-differentiation** with base entities, intensional link destinations | Belief views | **Strong** | Near-verbatim match. Residual: non-differentiation implies acting *through* a view (the view-update problem); edit-as-testify is a principled answer — state it as such. |
| 4 | Computation in/over networks — engines that "derive new information and enter it into the network," truth maintenance | Belief engine + machine hypotheses | **Strong** | Gneiss is the hybrid he sketches, plus a decision gate he never considered — which is where review-bandwidth insolvency lives. |
| 5 | Versioning — per-entity histories, **addressable deltas** ("possible hits for a search"), PIE layers/contexts | Bitemporal ledger + contexts | **Strong** | Assertions-as-first-class = addressable deltas. Gneiss exceeds the spec (dual time axes). Piquant: Halasz demoted versioning in '91 for market reasons; Gneiss makes it the foundation. |
| 6 | Collaboration — long transactions, soft locks, notify-at-intent-to-update, mutual intelligibility, procedural conventions | Actor envelopes; append-only coexistence | **Weak/partial** | Append-only dissolves the locking half elegantly. The *social* half — awareness, convention-setting, shared rhetoric — is presently **no answer**. |
| 7 | Extensibility — without leaving every user "a significant database design task" | Stances, schema evolution | **Partial** | The ontology-design burden on *non-programmers* is inherited by every Gneiss deployment. No announced answer beyond shipped stances. |

Overstated: "composites = clusters/lots" is the shakiest equation. Where Gneiss has no answer: **structure search (1)** and **social collaboration (6)**, plus non-programmer tailoring (7).

### N2 — "DR died of capture overhead; ADRs survived by cheapening; the prior evidence for intent rot and the D26 design constraints"

**Verdict: true, with two sharpenings.** The evidence chain is unusually clean: gIBIS §5.4 names prohibitive per-thought overhead; Grudin names the incentive asymmetry; QOC concedes live capture is infeasible; Compendium survives only by hiring a professional. ADR's deltas each attack a specific failure: one page (volume cap), prose headings (no typing tax — *no graph*), written by the decider at decision time (asymmetry collapsed to "future us"), in-repo (no second tool; a realizes-edge by colocation), numbered and immutable with supersession (append-only decision management — the claim-key pattern in markdown).

Sharpening 1: **ADRs succeeded partly by *abandoning* the rationale graph.** The lesson for D26 is not "structured intent capture is now viable"; it is "only the *decision* earns human keystrokes; structure must be reconstructed by machines or not at all." Which is Gneiss's architecture — agents draft the typed structure, humans decide — but that moves the cost from capture to review: **the intent-rot risk and the review-bandwidth-insolvency risk are one conserved quantity moving between ledger columns.**

Sharpening 2 (guess, flagged): part of ADR's success is environmental — git ubiquity, PR review as a free capture ritual, ThoughtWorks legitimization — not purely format design.

### N3 — "Agenda was entity/assertion/rule/view for civilians in 1988, and died of platform/business, not concept"

**Verdict: mapping true; cause-of-death half true.** The mapping holds (with sticky manual overrides prefiguring decisions-survive-recomputation). Cause of death: proximately corporate, but "not concept" overreaches — the learnability wall was real, opaque auto-assignment eroded trust, and the concept failed *again* with full funding (Chandler). Honest reading: viable but *unteachable as shipped*; its rules lacked explainability. Gneiss's justification edges are the specific missing part.

### N4 — "Trails are the never-shipped presentation organ; Gneiss makes them buildable at last"

**Verdict: fair on mechanism, romantic on adoption.** Fair: "trails do not fade" is a *bitemporal* requirement — a trail must pin each stop to an evaluation context; Gneiss is, unusually, a substrate where that is native (a trail = an ordered, annotated sequence of labeled views, each replayable). Romantic: Bush specified the *economics* too — trail blazing as a paid profession. The web's history says unpaid curation-at-capture doesn't happen at scale. Expect trails where there is a facilitator-equivalent — onboarding docs, incident retrospectives, audit narratives — not as ambient behavior.

---

## What Gneiss should steal

| From | Steal | Into |
|---|---|---|
| Memex | Trail as first-class object: name, owner, annotations, side-trails, export | Curated sequences of labeled views; context-pinned |
| Memex | "Trail blazer" as a *role with budget*, not a hope | Distillation-debt workflow: assign curation, don't await it |
| Sketchpad | Same-session payoff for declared intent; error-metric constraints (drift as scalar per intent) | D26: every realizes-edge computes visible alignment/drift on change |
| Sketchpad | Constraint-preserving merge | Entity resolution: justifications and decisions survive merges via claim keys |
| ThingLab lineage | Hide the formalism inside a cheaper authoring surface | Rule authoring UX; spreadsheet-grade recompute latency |
| Halasz #1 | Structure search; one language for query = view-definition = filter | Graph patterns over justification edges (circular-justification detector) |
| Halasz #2 | Inclusion vs reference; part→whole version-propagation policy | Clusters/lots spec |
| Halasz #5/PIE | Deltas addressable and searchable | "Search for the change that…" in the Language |
| Halasz #6 | Notify at intent-to-update; procedural/convention discourse | Collaboration layer (currently the biggest hole) |
| gIBIS | The protonode: zero-structure capture with deferred typing, dignified in the schema | Typed missingness for *structure itself* — "not yet structured" as a first-class state |
| gIBIS §5.5 | Resolutions may transcend captured options | Decision objects always admit free prose beyond the hypothesis menu |
| Grudin | Benefit audit: for each required human act, who pays and who gains | Risk-register method for every capture-side feature |
| ADR | One page; decider writes; decision time; in-repo; supersession | D26 format constraints, verbatim |
| Agenda | Free-form first, categorize by rule afterwards; sticky overrides | Already aligned — add the missing organ, why(this-categorization) |
| FRESS | Undo economics: cheap, instant, self-rewarding beats globally-correct-but-costly | Prioritize reversibility of testimony over completeness of capture |
| PLATO/wiki | Testimony threads *and* a distilling front page — never one without the other | Presentation philosophy (PLATO is the cautionary half) |

## The recurring killer: capture economics across all of these

Line every system up by *when structure is paid for* and the graveyard sorts itself:

- **Structure demanded at capture time, paid by the thinker, benefit deferred to others:** gIBIS, NoteCards fileboxes, Aquanet schemas, QOC, Agenda's category gardening (partially). All dead or niche. The mechanism is compound: Conklin's *cognitive* overhead × Grudin's *incentive* asymmetry. Either alone is survivable; the product is fatal.
- **Structure paid by a professional:** Compendium's facilitator, Bush's trail blazers, database designers. Works; scales linearly with payroll.
- **Structure cheapened to near-zero:** the web's link (killed the trail), the wiki edit (killed structured KM), the ADR page (killed the rationale graph), undo. Survivors all discard machine-readable structure to survive — the field's repeated deal with the devil.
- **Structure computed after cheap capture:** Agenda's rules, spatial hypertext, Sketchpad's satisfied constraints. The only quadrant that keeps both the structure and the user — and, until agents, it was compute-starved.

Gneiss's wager is that agent-rate computation finally makes the fourth quadrant rich: ledger entries stay as cheap as testimony; machines propose the typed structure; humans spend keystrokes only on decisions, which claim keys make durable. Three warnings from the record. **First**, the conservation law: gIBIS moved overhead from writing to encoding; Gneiss moves it from encoding to reviewing. Grudin's audit must be rerun on the review queue — if agents propose at machine rate and humans ratify at human rate, the disparity returns wearing the opposite uniform (treat review-bandwidth insolvency and intent rot as the *same* risk, not two). **Second**, Sketchpad's clock: intent capture survived in 1963 because payoff arrived in seconds. Any capture act whose benefit is purely archival will rot; every declared intent needs a same-session return (a drift check, a generated view, a caught inconsistency). **Third**, gIBIS §5.5: the schema must never become the menu. Decisions must always admit prose that outruns the hypothesis set, or the interesting decisions will happen off-ledger — which is how intent rot actually starts.

## Sources

**Fetched and read this session (primary):** Bush, "As We May Think" (Atlantic 1945, w3.org mirror) · Sutherland, *Sketchpad* (UCAM-CL-TR-574, full text extracted) · Halasz, "Seven Issues" (CACM 31(7) 1988, full read) · Halasz, "'Seven Issues': Revisited" (HT'91 plenary transcript, eastgate.com) · Conklin & Begeman, "gIBIS" (ToIS 6(4) 1988; §§2, 5.4–5.6 close-read) · Grudin, "Why CSCW Applications Fail" (CSCW '88) · Nygard, "Documenting Architecture Decisions" (2011) · van Dam Hypertext '87 keynote (cs.brown.edu transcript).

**Secondary / not obtained:** Kaplan et al., "Agenda" (CACM 1990 — ACM DL blocked; reconstructed from secondary) · Woolley PLATO history (site unreachable; recalled) · ThingLab, QOC, Compendium, Trellis, Aquanet/VIKI, Shipman & Marshall, Kunz & Rittel, *Dreaming in Code*, ThoughtWorks Radar — recalled, flagged where load-bearing.
