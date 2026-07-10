# The Wiki Answer: Presentation Philosophy for the Layer Above

*Added 2026-07-05 from discussion. Govert's question: how do we break the fourth wall — peek into the machinery, present the break in abstraction — to define and build trust in the presented layer? His answer: Wiki. Not Wikipedia's editorial morals; WikiWikiWeb's knowledge-organization ideas, aspiring somewhere between wikiwiki (dissent, change, tolerated inconsistency) and Wikipedia (citations, history) — plus provenance, security, and authorization, which c2 lacked. The wiki is not the bedrock; it is the **presentation philosophy** every operational world-model platform built on Gneiss should exhibit. Survey 17 landed same day ([17-SURVEY-WIKI.md](17-SURVEY-WIKI.md)); verdicts absorbed throughout and summarized in §8.*

## 1. Why the fourth wall needs doors

Every abstraction that cannot be interrogated is eventually distrusted or bypassed. The operational form of that law is **shadow Excel**: when the dashboard can't be questioned, users rebuild it in a spreadsheet — the one tool where the machinery is one click away (the formula bar) — and now the organization runs on unaudited copies. Trust doesn't come from hiding the machinery *or* from exposing all of it; it comes from **graduated disclosure**: each layer of "why should I believe this?" exactly one gesture away, none mandatory.

The design thesis that snaps this together with the bedrock:

> **The witness stand defined what the system must answer ([04-BEDROCK-OPERATIONAL.md](04-BEDROCK-OPERATIONAL.md)). The wiki philosophy defines how the interface asks — on the user's behalf, one gesture per question. The fourth wall gets seven doors.**

| Witness-stand question | Presentation affordance (≤2 gestures from any displayed value) |
|---|---|
| Q1 What do you claim? | The front page itself, with its quiet basis footer (context, high-water mark, grade badge) |
| Q2 Why? | Citations at hand; derivation drill-down ("reveal codes") |
| Q3 On whose word, since when? | Attribution on every citation: source, method, actor, when-learned |
| Q4 What did you claim before, and why the change? | Page history + diff (time-diff *and* context-diff) |
| Q5 What have you forgotten? | Coverage warnings, knowledge-horizon banners on the page basis |
| Q6 By what rules? | The rules have pages too — every policy/formula/context is linkable and browsable |
| Q7 Say it again | Permalink-to-basis: a link that reproduces the page byte-for-byte or states its grade honestly |

**Presentation conformance** = every question has its door. This is the platform-layer twin of the bedrock's conformance test.

## 2. The wiki-ish aspects (Govert's four, elaborated, plus more)

### G1 — The front page is the simple truth
The default surface is the belief view under the default context — a number, a chart, a state board — *presentation-first, machinery-quiet*. The page carries its basis (context version, evidence high-water mark, coverage caveats) the way a wiki page carries its footer: present, unobtrusive, ignorable until needed. No epistemic ceremony on the happy path; the c2 principle is *Mundane*.

### G2 — Citations near at hand
Every displayed fact/figure can produce its citations — internal (assertions, decisions, rule versions) or external (the scraped page, the delivery note, the standard) — possibly multiple, possibly conflicting-and-shown. **"Citation needed" generalizes into the grade/missingness badge family**: weak provenance, unreviewed auto-admitted links, `attested`-only history, and forecast-grade values all render as visible doubt, in the same visual register Wikipedia taught two billion people to read. (The badge vocabulary is presentation-layer; the internal term "grade" — which Govert is still weighing — need not surface at all. See §6.)

### G3 — Reveal codes: the machinery layer below
One level down from any value: not merely *source* drill-down but **inference** drill-down — the derivation tree (inputs, rule version, calibrations), the overrides and decisions that shaped acceptance, the human/agent journal entries with their reasons, the conflicting claims that lost and why. This is `why()` rendered as an explorable surface — WordPerfect's Reveal Codes for beliefs, the spreadsheet's trace-precedents arrows for the knowledge base. The formula-bar test: **value → rule → inputs in two gestures.**

Survey 17 sharpened the design brief to three adjectives from why Reveal Codes was *trusted*: the pane must be **complete** (nothing about the belief exists outside what it can show), **live** (it is the machinery, not a report about it), and **actionable** (the fix — a correction, a challenge — is made *in* the reveal layer). And Bret Victor's amendment: `why()` should not print a proof tree; it lets the user *step down the ladder* — value → rule application with actual bindings → testimony — concrete data visible at each rung.

### G4 — Editing near presentation, and "edit" means testify
"Here is what we see — but this is wrong / this has changed / this will change." The edit affordance sits next to the presentation, and its first job is **intent disambiguation**, because the bedrock offers different verbs for different kinds of "wrong":

| User intent | Bedrock verb |
|---|---|
| "This was recorded wrong" | correction of the past (retraction + replacement, valid time unchanged) |
| "This changed" (the silo's grain type is now different — no mistake) | new assertion, valid-from now; old one closes naturally |
| "I don't believe this inference" | challenge: a decision proposal against the derived claim, or a rival hypothesis |
| "This will happen / must happen by…" | plan / expectation / obligation stance ([33-FUTURE-TENSE.md](33-FUTURE-TENSE.md)) |
| "This shouldn't be visible at all" | redaction request (authority-gated, receipted) |

Every edit is an appended, attributed, *reasoned* assertion (wiki edit summaries proved people will write one honest line), routed by the authority lattice: within delegation → applies directly; beyond it → becomes a proposal in someone's queue. Palantir's Actions are this pattern productized per-object-type; Gneiss generalizes it in the bedrock — **"Edit this page" becomes "testify."**

### G5 — Recent changes, scoped to what you care about
RecentChanges was the wiki's pulse; the Gneiss version is better-targeted: **change feeds scoped to a page's justification cone.** "What changed since you last looked, *among the things this page depends on*": the calibration retraction that restated your chart, the accepted link that merged two competitors, the closure that turned a `presumably_violated` into `violated`. Watchlists = standing subscriptions to cones. This is where corrections stop being silent and start being social.

### G6 — Red links: absence as invitation
The wiki's most underrated invention: a link to a page that doesn't exist yet, rendered as an invitation to create it. Typed missingness renders the same way — `not_observed` is a red link inviting a reading; a pending hypothesis is a red-ish link inviting a decision; `unknown`-where-closure-is-possible invites a completeness declaration. **The review queue is the red-link list of the knowledge base.** Absence stops being blank space and becomes navigable work.

### G7 — The talk page: dissent rendered, not resolved
Every presented value has a "discussion" surface: the rival claims, pending hypotheses, dissenting decisions, and unresolved conflicts behind it. `contested` renders the way Wikipedia renders disputes — *attributed multiplicity* ("Radar says 4.2 m; operator log says 4.3 m; policy declined to choose") rather than false confidence. This is wikiwiki's tolerance of inconsistency, held safely: the *shared* page shows the policy's verdict or the honest dispute; dissent lives on-surface, not in hallway folklore.

### G8 — Diff is a first-class view
Any two states of a page are comparable: two moments (what changed since Tuesday), two contexts (**as-reported vs as-restated — the restatement report *is* a wiki diff with reasons per hunk**), two what-ifs (current rules vs proposed rules). Nobody should ever explain a restatement in prose that the platform could render as a diff.

### G9 — Soft security where knowledge forms, hard security where the domain demands
Meatball's SoftSecurity — make damage visible, attributable, and reversible rather than preventing every act — is exactly the ledger's posture: proposing is cheap and open (to agents too); *acceptance* is gated by authority; everything is attributed; revert is supersession, one click, itself on the record. Hard security stays where it belongs: redaction, authority grants, legal holds, safety-critical writes. c2 had no authorization and died partly of it; Gneiss platforms get the wiki's openness *inside* an authorization perimeter.

### G10 — Fork, don't fight
Federated wiki's answer to edit wars: disagreement forks a page rather than thrashing it. Gneiss's version: a dissenting user or team holds a **what-if context or personal overlay** — their view, clearly badged as theirs, over the same testimony — without touching shared belief. The platform always shows *whose view you are in* (the context badge is federated wiki's site attribution). The "chorus of voices" is multiple evaluation contexts made visible and navigable.

### G11 — Transclusion, never paste
Every number on every dashboard is a **live reference to a belief-view cell**, not a copied value — provenance survives display composition (Xanadu's transclusion, buildable here because the platform owns both ends of every link). Corollary via two-way links: the cell knows which pages display it, so "this correction will restate 3 reports and 2 dashboards" is computable *before* accepting the correction — consumer contracts ([27-EVOLUTION.md](27-EVOLUTION.md)) extended to presentation surfaces. The anti-pattern this kills: the screenshot pasted into a slide, aging silently.

### G12 — Everything is a page
Silos, sensors, people, lots, policies, contexts, rules, decisions, report runs, sources, methods: each an entity, each with a front page, citations, history, talk, and edit affordances. The wiki's uniform page model is the kernel's uniform entity model surfacing in the UI — including the meta level: **the rules have pages** (Q6), a context's page shows its parameters and its own history, a method's page shows its skill record. One navigation idiom for the whole world-model.

## 3. The convergence claim

The wiki — invented in 1995 for humans sharing fallible pattern knowledge — independently evolved: append-ish history, attributed edits with reasons, citations, diffs, revert-not-delete, dispute surfaces, absence-as-invitation, watchable change. That is most of Gneiss's organ inventory, grown in the one large-scale system whose *entire product* is contested, revisable, human-curated knowledge. Convergent evolution is evidence for both sides: **the wiki is folk Gneiss; Gneiss is a wiki whose pages are claims and whose edits are testimony.** And Wikipedia's most famous policy — *verifiability, not truth* — is the evidence/belief split as editorial law.

Survey 17 verified the mapping (~two-thirds of the correspondences deep) and found the two sharpest pieces:

- **The deepest correspondence was one this document missed: ThreadMode→DocumentMode refactoring ↔ the belief engine.** c2's culture accreted signed dialogue (contested testimony) and relied on volunteers to periodically refactor it into distilled prose (current belief). That labor never scaled, and its failure — distillation debt — is a major reason c2 drowned. Gneiss *mechanizes the refactoring*: the front page is DocumentMode computed deterministically from ThreadMode by rules. The one organ wikis needed and never had is the one Gneiss is built around.
- **The corrective: wikis conflate what Gneiss separates.** A wiki page is simultaneously storage, testimony, belief, and presentation; every deep wiki pathology (edit wars, lost provenance, KeptPages amnesia, oldid's broken replay) traces to the missing ledger/view split. So the honest form of the slogan: the wiki is folk Gneiss *minus the ledger* — adopt its presentation grammar, never its storage model.

## 4. What NOT to take

- **From wikiwiki:** ThreadMode sprawl (our talk layer is structured: hypotheses, decisions, conflicts — not chat); anonymous edits (the envelope always attributes); a single flat namespace; wiki rot (our pages are views — they regenerate; only *testimony* ages, and it ages honestly).
- **From Wikipedia:** NPOV as epistemology (we don't aspire to neutrality — we record attributed conflict and let *policy under a context* adjudicate); consensus-as-truth (we have authority lattices and decisions); notability (operational worlds keep everything addressable); deletionism (nothing here is deleted, only superseded or receipted away).
- **From both:** the assumption that all participants are humans at human rate. Agent-rate proposal floods are our named risk (hypothesis spam, [32-RISKS.md](32-RISKS.md)); the wiki's attention economics under machine-rate participation is exactly claim P4 sent to survey 17.

## 5. Trust, stated as a mechanism

Trust in a presented layer = the *verified expectation that interrogation is always available and never punished*. Users don't drill down often; they need to know they could, and that the one time they did, the machinery was really there (the drill-down that dead-ends once destroys the credibility of every badge). Hence presentation conformance (§1) is a *contract*, drilled like the bedrock's: a platform test suite should walk every rendered value to its testimony and its permalink — the UI's own witness stand, exercised mechanically.

Survey 17 added the two hard-won caveats from the machinery-reveal tradition, both now design rules:

- **Visibility ≠ correctness (the spreadsheet's lesson).** The formula bar is the most successful reveal ever shipped, and catastrophic spreadsheet errors ship through it constantly (Panko's error-rate research; Reinhart–Rogoff; the UK's 2020 Test-and-Trace truncation — all in the most inspectable medium ever deployed). Visible-but-unvalidated machinery breeds *misplaced* confidence. Rule: every reveal is paired with its conformance status — grade badges are earned by drills, never decorative; the differentiator is not that the machinery is visible but that it is **checked**.
- **A label that doesn't replay poisons all labels (the oldid/Jupyter lesson).** Wikipedia's permalink-to-oldid pins the wikitext but renders old pages with *today's* templates — a reproducibility promise quietly broken; Jupyter's hidden kernel state means visible code often doesn't produce visible output. Rule: "Say it again" (Q7) must be routinely *demonstrated* in the UI, not merely possible — and where a replay can only reach `sealed` or `attested` grade, the permalink says so instead of pretending. The one-line marketing of the whole architecture: **our permalinks actually replay.**

## 6. Terminology note (flagged, not settled)

Govert is "still getting used to 'grade'." The wiki lens suggests a resolution: the internal term (grade: grounded/sealed/attested) need never surface — users see **badge language** ("verified", "derived", "from archive summary", "record only", "citation needed"). Candidate replacements if the internal term should change too: *standing*, *warrant*, *footing*, *basis*. Parked as part of D24; naming decisions improve with a night's sleep and a worked UI mock.

## 7. Agenda

- **D23** — Adopt presentation conformance (the seven doors, §1) + aspects G1–G12 as the platform-layer contract, sibling to the witness stand? Position: yes; this becomes a core chapter of what "operational world-model platform on Gneiss" means ([05-CODD-PROGRAM.md](05-CODD-PROGRAM.md) §9's benefit-hiding made concrete).
- **D24** — Badge vocabulary + the "grade" naming question (§6). Wants a UI mock more than a debate. Survey input: Wikidata's rank display (preferred bold, deprecated struck-but-present, references and qualifiers visible) is *the existing public UI to copy* — proven with lay editors at scale.
- **D25** — Review-bandwidth economics as a platform-contract chapter (from survey 17's P4, the sharpest warning in the corpus): c2 died of review-bandwidth insolvency — proposal cost fell to zero (bots) while review cost stayed human — and agent-rate platforms hit that wall on day one, not year nine. Position: decision bandwidth is a metered, budgeted resource; proposal admission is priced (rate/quota/reputation per agent identity); machine hypotheses get machine *pre-review* (triage, dedup, batch) before any human sees them; similar proposals aggregate into one decision; low-stakes decision classes delegate down the authority lattice with the audit trail intact. Gneiss-specific twist, **corrected** (Govert caught the survey's "junk is permanent" framing contradicting [25-IMPERFECTION.md](25-IMPERFECTION.md)): junk is *not* permanent — unaccepted hypotheses are the cheapest purge tier in the retention economics, and even accepted junk is removable by defeat → seal → purge. What ingestion controls actually protect is two real things: the **cost asymmetry** (writing junk costs the attacker nothing; defeating, sealing, and purging it costs us ceremony — prevention is O(1), cleanup is not) and the **entanglement damage** (junk that gets accepted and *cited* before cleanup leaves graded scars — attested-grade holes — in every justification cone that touched it; the bytes leave, the weakening stays visible). The only permanent residue of a spam episode is its receipts and cleanup decisions — kilobytes, and arguably a feature: institutional memory of the attack. Corollary worth stating: if junk *were* permanent, a spam flood would be a permanent storage DoS and the architecture would be unshippable — bounded attacker-inflicted cost *requires* the cheap-purge tiers. (Subsumes and sharpens the hypothesis-spam risk in [32-RISKS.md](32-RISKS.md); also adds **distillation debt** — open-hypothesis age as a first-class health metric on every surface.)

## 8. Survey 17: verdicts absorbed

P1 (organ mapping): substantially correct, two-thirds deep; the deepest correspondence was the one we missed (ThreadMode→DocumentMode ↔ belief engine, §3), and the standing correction is that wikis lack the ledger/view split — adopt the presentation grammar, never the storage model. P2 (soft security): sound, with the hard-security boundary now drawn — hard at **actuation cones** (once a view drives physical/financial action, reversibility is gone), **identity** (authenticated actors or the envelope is worthless), **confidentiality** (c2's Observable must be scoped: *observable-to-whom* is a typed question), and **ingestion quotas** (P4); soft everywhere upstream, and ForgiveAndForget becomes **forgive-and-remember** as a first-class design consequence. P3 (fedwiki): fair — the journal anticipates provenance-with-the-artifact and forking anticipates what-if contexts, but fedwiki's divergence is terminal (no reconcile/govern machinery) and its UX teaches that the chorus needs a lead voice: **one designated operational context on top, the chorus one deliberate click down** (G10 amended accordingly). P4: became D25 above. Full detail and the extended mapping table: [17-SURVEY-WIKI.md](17-SURVEY-WIKI.md).
