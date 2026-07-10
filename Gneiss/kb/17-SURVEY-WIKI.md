# Survey: Wiki Principles & the Machinery-Reveal Tradition

*Research-agent survey for Gneiss, 2026-07-05. Commissioned for the presentation philosophy ([34-PRESENTATION.md](34-PRESENTATION.md)); claims P1–P4 adjudicated. Verdicts absorbed into 34 §8. The c2 principles page was fetched directly (via its JSON backend); MeatballWiki fetched live.*

## TL;DR — top 5 takeaways for Gneiss

1. **The wiki mapping is real, but wikis conflate what Gneiss separates.** A wiki page is simultaneously storage, testimony, belief, and presentation. Gneiss's ledger/view split means the *rendered view* is the wiki page and the ledger is what wikis never had. Adopt the wiki's presentation grammar (front page, citations, history-below, edit-near-view); do not adopt its storage model, which is why every deep wiki failure (lost history, unattributable claims, unreproducible old pages) is a failure Gneiss already fixes.
2. **The most successful machinery-reveal ever shipped is the spreadsheet formula bar — and it proves visibility ≠ correctness.** Hundreds of millions of users inspect live computation daily, and spreadsheets still ship catastrophic errors at scale. The reveal layer must be *checkable* (deterministic replay, "Say it again"), not merely visible. WordPerfect's Reveal Codes worked because the reveal *was* the document, complete and editable — aim for that fidelity, not View Source's decayed honesty.
3. **Soft security is an ingestion philosophy, not a security architecture.** MeatballWiki's insight — visibility + reversibility beats prevention — maps cleanly onto proposals-cheap/decisions-gated. But it presupposes reversible damage and human-rate attack. The accept decision is Gneiss's irreversibility boundary (especially where views drive actuation); gate hard exactly there, stay soft everywhere upstream.
4. **c2 died of review-bandwidth insolvency, not spam per se.** Proposal cost fell to zero (bots); review cost stayed human-constant; the maintainer was one volunteer. Any platform where LLM agents propose at machine rate hits the same wall orders of magnitude faster. Decision bandwidth is the scarce resource — budget it, price proposals, and triage machine hypotheses with machine pre-review.
5. **A permalink-to-oldid is a *broken promise* of Gneiss's labeled answer** — Wikipedia's oldid pins the wikitext but renders it with *today's* templates, so old pages silently change meaning. Gneiss's label (context version + evidence high-water mark) is the honest version of the same gesture. Market the difference: "our permalinks actually replay."

## 1. c2 WikiDesignPrinciples (fetched)

Fetched via c2's JSON backend (`c2.com/wiki/remodel/pages/WikiDesignPrinciples`; the wiki.c2.com front-end is JS-only). Ward Cunningham prefaces the list by noting it is "only a reconstruction from memory of intentions I held at the beginning," and that additional principles "like server robustness, have been forced upon me."

The primary principles, with Ward's glosses (paraphrased tightly). **Note: the list begins with Simple, which summaries often omit:**

- **Simple** — easier to use than abuse; a wiki that reinvents HTML markup "has lost the path."
- **Open** — any reader finding a page incomplete or poorly organized can edit it as they see fit.
- **Incremental** — pages can cite pages that have not been written yet.
- **Organic** — structure and content are open to editing and evolution.
- **Mundane** — a small number of (irregular) text conventions gives access to the most useful markup.
- **Universal** — editing and organizing use the same mechanisms as writing; every writer is automatically an editor and organizer.
- **Overt** — the formatted output suggests the input required to reproduce it.
- **Unified** — page names come from a flat space; no extra context needed to interpret them.
- **Precise** — titles precise enough (noun phrases) to avoid most name clashes.
- **Tolerant** — interpretable behavior, even if undesirable, is preferred to error messages.
- **Observable** — activity within the site can be watched and reviewed by any visitor.
- **Convergent** — duplication discouraged/removed by finding and citing similar content.

Secondary principles attributed to other wiki authors: **Trust** ("the most important thing in a wiki… trust the people, trust the process, enable trust-building"), **Fun**, **Sharing**; plus comment-level additions. A footnote argues Unified and Precise might fold into Convergent.

"The simplest online database that could possibly work" is Cunningham's famous description of wiki, but it is **not on this page** (widely quoted from a ~2003 interview; wiki.org did not respond to fetch — provenance from background knowledge, labeled).

Three principles are directly load-bearing for Gneiss presentation: **Overt** is the fourth-wall principle stated in 1995 — output should suggest how to reproduce it. **Observable** is RecentChanges-as-right. **Incremental** legitimizes displaying holes — a link to a nonexistent page is an invitation, not an error. **Tolerant** endorses returning a graded answer over refusing to answer.

**Verdict for Gneiss: adopt** — Overt, Observable, Incremental, Tolerant, Universal transfer nearly verbatim; Simple/Mundane transfer as UI restraint. Unified/Precise are naming policy, less relevant. Convergent becomes a rule concern (duplicate-assertion detection), not a presentation one.

## 2. WikiWikiWeb: history, culture, decline

Launched 25 March 1995 as the discussion companion to the Portland Pattern Repository. The lineage runs from Christopher Alexander's pattern language (via the Hillside Group), and the wiki descends from a HyperCard stack Cunningham built in the late 1980s. The pattern-language DNA matters: pages were meant to *converge on distilled, reusable knowledge*, not to be conversation.

Cultural machinery relevant to Gneiss:

- **ThreadMode vs DocumentMode**: signed dialogue accretes (ThreadMode); periodically someone *refactors* it into unsigned distilled prose (DocumentMode). This is a two-layer epistemic architecture: contested testimony below, current belief above — with a human labor step converting one to the other. c2's chronic failure was that refactoring labor never scaled; ThreadMode bloat won.
- **RecentChanges** as the community's pulse — ambient awareness substituted for access control.
- **Dangling links** (on c2, a trailing "?"): a named-but-unwritten page is a visible, clickable invitation. Generative absence.
- **Refactoring culture**: editing others' words, condensing, deleting — legitimate and expected.

**Decline and freeze (verified):** wiki spam from the mid-2000s, community drift as Wikipedia and topical wikis siphoned energy, escalating vandalism economics against a single volunteer maintainer. December 2014: an attack including threats of automated vandalism forced read-only mode; 1 February 2015: relaunched as a single-page app over federated-wiki-style JSON, effectively a frozen archive since. What preserved it: one person's ownership of the domain and data, plus the JSON conversion. (Causal sequencing is interpretation, labeled; the 2014–15 events are documented.)

**What the failure modes predict for operational platforms:** (a) distillation (ThreadMode→DocumentMode) must be a *system function*, not volunteer labor — in Gneiss, rules compute the belief view, which is exactly the mechanization of refactoring; (b) observability without authorization has a finite lifespan once attention becomes adversarial; (c) a system whose integrity depends on one maintainer's spare attention freezes when attack cost drops below defense cost.

**Verdict for Gneiss: adapt** — take ThreadMode/DocumentMode as the contested-layer/front-page split (mechanized by rules), RecentChanges as scoped feeds, dangling links as typed-missingness UI; reject the labor model and the absent authorization.

## 3. MeatballWiki: soft security

Fetched from meatballwiki.org (alive in 2026 as a restored archive — restoration provenance unverified). Sunir Shah's community-of-communities wiki (founded 2000) was where wiki governance theory was written.

**SoftSecurity** (page fetched): "protect the system and its users from harm, in gentle and unobtrusive ways" — the opposite of HardSecurity. It works "architecturally in defense" to limit damage and "socially in offense" to encourage good behavior. It follows from **AssumeGoodFaith** and the **PrincipleOfFirstTrust**, and decomposes into: **PeerReview**, **ForgiveAndForget** (mistakes need not be permanent), **LimitDamage**, **FairProcess** (transparency + voice), NonViolence. Architectural devices include **AuditTrail**, RubberRoom (a sandbox where mistakes are more forgiven), FrontLawn, MessageBox. Crucially: "When SoftSecurity becomes unilaterally enforced, it fails" — it is a collective practice, not a feature.

Two sharp observations for Gneiss:

1. Soft security's preconditions are **visibility** and **reversibility**. An append-only ledger with justification edges is the strongest visibility+reversibility substrate ever proposed for this philosophy — better than wikis, which actually *forgot* history.
2. **ForgiveAndForget directly contradicts append-only.** UseModWiki's KeptPages expired old revisions after ~2 weeks; forgetting was a *feature* — vandalism and shame faded. Gneiss forgives (supersession) but never forgets. Mostly a strength, but it imports obligations wikis dodged: redaction law, permanent records of personal mistakes, vandalism forever *in the ledger* even when absent from every view. Gneiss needs typed tombstones/redactions that are themselves ledger events (it has them — the point is they are load-bearing here too).

**Verdict for Gneiss: adopt (the analysis), adapt (the prescriptions)** — soft security as ingestion philosophy, hard security at decision/actuation boundaries; replace ForgiveAndForget with "forgive-and-remember," and treat that substitution as a first-class design consequence.

## 4. Wikipedia mechanics (not editorial morals)

- **Page history / diff / permalink-to-oldid.** Every revision addressable, any two diffable. **Assessment:** oldid pins the *wikitext only* — templates, modules, and images transclude at their *current* versions, so an old revision renders differently over time; the permalink promises reproducibility and quietly breaks it. Gneiss's label is the kept version of this promise. The single most instructive Wikipedia mechanic: users demonstrably want version-pinned citations; the mechanism just isn't airtight.
- **Talk pages**: the contested layer, one click from the article — dissent adjacent to, never inside, the presented truth. Structure is weak (freeform); Gneiss's typed hypotheses/decisions are the strong form.
- **Watchlists**: per-page change subscription. Notably *not* dependency-aware — you cannot watch "everything this article's claims depend on." A justification-cone subscription is a genuine extension, not a copy.
- **Citations & [citation needed]**: inline doubt rendered to *readers*. Epistemic UI at half-a-billion-readers scale and the strongest precedent for grade badges — with the difference that Gneiss's grade is *computed*, not hand-flagged.
- **Templates/infoboxes**: structured claims embedded in prose; the reconciliation pain between infobox values and article text is a live demo of why one substrate should feed both.
- **Protection levels & pending changes**: authority-gated acceptance where edits *exist but aren't presented* until a reviewer accepts — precisely hypothesis-awaiting-decision, deployed at scale since ~2010.
- **Revert/rollback**: reverts append a new revision; nothing is deleted (except rare oversight/suppression — itself logged). Wikipedia is closer to append-only than folklore suggests.
- **Wikidata's statement model as UI**: claims each carrying **references**, **qualifiers**, and **rank** (preferred/normal/deprecated), all *displayed* and editable. Deprecated-but-retained = superseded-not-deleted. The closest existing public UI to rendering a Gneiss assertion envelope, and it works for lay editors.
- **"Verifiability, not truth"**: yes, the evidence/belief split as editorial policy — Wikipedia refuses to assert truth and asserts only attributability. But Wikipedia collapses the layers in presentation: the article *reads* as truth with footnotes. Gneiss keeps the layers distinct and lets the grade say which layer you're getting. (The famous phrasing was demoted from the policy lead after a 2012 RfC — from memory, labeled.)

**Verdict for Gneiss: adopt the mechanics wholesale** — history/diff/permalink (done right), talk-as-typed-contest-layer, citation-needed-as-grade-badge, pending-changes-as-decision-queue, Wikidata rank/qualifier/reference display as the assertion-envelope UI pattern.

## 5. Federated Wiki (Smallest Federated Wiki, 2011+)

Every page is JSON: a story of items plus a **journal** of every action — provenance ships *inside the page*. **Forking is first-class** (forks record origin; the halo shows lineage); the **neighborhood** is the set of sites you're reading across; the result is a **"chorus of voices"** rather than Wikipedia's consensus engine — divergent copies coexist.

Status mid-2026: alive and maintained (core `fedwiki/wiki` npm package updated June 2026) but permanently niche. Lessons: (a) single-writer sites + forking eliminates most vandalism *and* most collaboration energy — the consensus engine, for all its violence, is what generates convergent value; (b) provenance-in-the-page is technically cheap and genuinely useful; (c) readers found the multi-column multi-version reading model cognitively expensive — a chorus with no designated lead voice confuses non-enthusiasts. Caveat: the journal is owner-mutable — provenance *testimony*, not tamper-evident record.

**Verdict for Gneiss: adapt** — the journal validates carrying provenance with the artifact (Gneiss adds the integrity guarantees fedwiki lacks); forking validates cheap what-if contexts. UX teaching: the chorus must be one layer *down* — operational users need a designated front-page context.

## 6. Xanadu

Nelson's project (begun 1960): **transclusion** (quotation by reference — the quote *is* the original, seen in new context), **two-way unbreakable links**, permanent versioned storage, provenance-preserving quotation. The web won because one-way best-effort links require no coordination and tolerate rot; Xanadu's demands (global addressability of immutable content, both endpoints cooperating) were unbuildable across an open, adversarial internet.

**The recoverable pieces inside a closed platform are almost all of it.** Where the ledger controls both ends of every link: justification edges *are* two-way links; append-only assertions *are* permanent versioned storage; a labeled answer *is* a stable address that cannot rot; a derived value citing testimony spans *is* provenance-preserving transclusion. Xanadu failed as an internet; it is nearly a specification for a Gneiss presentation layer. Unrecoverable: links across the platform boundary — external evidence must be snapshotted as testimony, with the envelope recording that it is a copy.

**Verdict for Gneiss: adopt (internally)** — transclusion and two-way links as the native presentation idiom; the platform boundary is the honesty line where Xanadu guarantees end and envelopes must say so.

## 7. Modern tools-for-thought (brief)

TiddlyWiki (micro-content + transclusion, alive 2026) proved sub-page-granularity content units viable. Roam ignited the backlink wave then declined; Obsidian is the durable winner; Notion took synced-blocks (transclusion) mainstream. **What demonstrably proved out:** backlinks (zero-effort, everywhere now); block/paragraph-level identity; constrained transclusion. What didn't: graph views (decorative), heavy transclusion as primary authoring (power-user only). Presentation lesson: reference-level granularity below "page" is expected — give assertions and view-fragments stable IDs that surfaces can embed.

**Verdict for Gneiss: adapt** — backlinks and block-level identity are table stakes; full transclusion as an expert layer.

## 8. The machinery-reveal traditions

- **WordPerfect Reveal Codes** (Alt+F3): the document shown as its underlying code stream, inline. Loved because when formatting misbehaved, the *cause was findable and directly deletable*. Trustworthy for three reasons: **complete** (nothing existed outside the codes), **live** (the reveal was the document), **actionable** (you fixed it in the reveal layer). The standard for a Gneiss reveal pane — not a static explanation.
- **Formula bar + Excel formula auditing** (trace precedents/dependents, Evaluate Formula): the most successful machinery-reveal in end-user computing — and the honest downside: decades of research (Panko) find errors in most consequential spreadsheets; Reinhart–Rogoff and the UK's 2020 Test-and-Trace XLS truncation happened *in the most inspectable medium ever deployed*. Visible-but-unvalidated machinery breeds misplaced confidence. Gneiss's differentiator: the revealed machinery is *checked* — deterministic rules, "Say it again," typed absences instead of silent truncation.
- **View Source**: bootstrapped a generation, then decayed — minified bundles made source visible but unreadable; DevTools (curated, structured) replaced it. Lesson: a reveal layer rots unless curated at human altitude; raw internals ≠ machinery.
- **Computational notebooks**: Jupyter made computation-visible documents mainstream, but hidden kernel state and out-of-order execution mean visible code often doesn't produce visible output (only ~a quarter of public notebooks re-execute; far fewer reproduce — Pimentel et al. 2019, from memory). Observable fixed it with reactive dataflow. Lesson: the fourth wall is only worth breaking if what's behind it deterministically produces what's in front of it.
- **Bret Victor** ("Ladder of Abstraction," "Learnable Programming"): trust comes from *showing the data* and letting users move continuously between concrete values and abstractions — never simulating the machine in their head. `why(value)` should not print a proof tree; it should let the user step down the ladder — value → rule application with actual bindings → testimony — concrete data visible at each rung.

**Verdict for Gneiss: adopt** Reveal-Codes completeness + formula-bar ubiquity + Victor's ladder as the design brief; design *against* the spreadsheet's false-confidence failure and the notebook's stale-state failure (both are arguments for determinism and grading, which Gneiss has).

## The four claims evaluated

### P1 — Wiki organs ↔ Gneiss machinery. **Verdict: substantially correct; about two-thirds of the correspondences are deep.**

Deep: edit↔append; history↔transaction log; citation↔justification edge; talk↔contested layer; protection/pending-changes↔authority-gated acceptance; RecentChanges↔scoped feed; Wikidata rank↔epistemic grade (*displayed*). Deep with correction: page↔belief view holds only if you note the wiki page is *also* the ledger — wikis have no view/ledger distinction, and every wiki pathology (edit wars, lost provenance, KeptPages amnesia) traces to that missing distinction. Surface-but-fixable: permalink↔labeled answer (oldid doesn't pin transclusions); diff↔context diff (wiki diffs are text-only along one axis; bitemporal and cross-context diffs have no wiki precedent); [citation needed]↔grade badge (identical gesture, upgraded semantics: hand-placed doubt → computed grade); watchlist↔cone subscription (wikis are dependency-blind — the cone is strictly stronger); red link↔typed missingness (wikis have one absence type; copy the *invitation affordance*, upgrade the typology); fork↔what-if context (fedwiki only). **Extensions the claim missed: ThreadMode→DocumentMode refactoring ↔ rules computing belief views from testimony — the deepest correspondence of all: Gneiss mechanizes the labor c2 couldn't sustain.** Also: edit summary ↔ assertion envelope; infobox-vs-prose reconciliation ↔ one substrate feeding structured and narrative surfaces.

### P2 — Soft security ↔ proposals-cheap/decisions-gated. **Verdict: sound as far as it goes; the boundary needs drawing.**

The mapping is faithful: AssumeGoodFaith/FirstTrust = accept any agent's proposals into the ledger; PeerReview = the decision queue; LimitDamage = hypotheses don't enter belief views until accepted; AuditTrail = the ledger. Gneiss even repairs soft security's weakest member — ForgiveAndForget becomes forgive-and-*remember*. Where hard security is genuinely required: (1) **actuation** — once a belief view drives a physical or financial action, reversibility is gone; the accept decision for anything in an actuation cone needs hard authorization; (2) **identity** — "on whose word" is worthless without authenticated, non-repudiable actors; (3) **confidentiality** — Observable must be scoped by the authority lattice ("observable-to-whom" is a typed question); (4) **resource protection** — ledger ingestion needs hard rate/quota control (see P4). Soft security remains correct for the proposal layer precisely *because* the ledger maximizes its two preconditions.

### P3 — Fedwiki journal + forking ↔ federation + what-if contexts. **Verdict: fair, with two corrections.**

The journal genuinely anticipates provenance-with-the-artifact; "chorus of voices" is a real precedent for multiple contexts made visible. Corrections: (1) fedwiki's divergence is *terminal* — no machinery for comparing, reconciling, or governing the voices; Gneiss contexts are versioned, diffable, and governed — the difference between a chorus and a cacophony; (2) fedwiki's journal is owner-mutable testimony, not tamper-evident record. UX lesson: one designated operational context on top; chorus one deliberate click down.

### P4 — c2's decline as attention/spam economics. **Verdict: correct but incomplete — the deeper lesson is review-bandwidth insolvency plus distillation labor.**

What killed/froze c2, in order: topic drift diluted shared context; ThreadMode accumulated faster than volunteers refactored; Wikipedia siphoned the community; spam and automated vandalism pushed defense cost past one volunteer's willingness. The generalization: **an open system stays alive while review supply ≥ proposal demand; bots broke c2 by driving proposal cost to zero while review cost stayed human.** For platforms where LLM agents propose at machine rate, this is the central design problem, arriving on day one rather than year nine. Mitigations: decision bandwidth as a metered, budgeted resource; proposal admission pricing (rate limits, quotas, reputation/stake per agent identity); machine *pre*-review (rules that triage, deduplicate, and batch before any human sees them); aggregation of similar proposals into one decision; authority-lattice delegation downward (authorized agents deciding low-stakes classes) with the audit trail intact. Gneiss-specific twist: c2 could eventually *forget* spam; Gneiss cannot — junk accepted into the ledger is permanent, so ingestion-level hard controls protect not just attention but the ledger's signal-to-noise forever.

## The mapping table, verified and extended

| Wiki/tradition mechanism | Gneiss machinery | Concrete presentation affordance | Depth |
|---|---|---|---|
| Wiki page (DocumentMode) | Belief view of an entity | Simple front page per entity/concern | Deep (view only — wikis lack the ledger split) |
| Edit | Append | "Suggest correction" always visible; never in-place mutate | Deep |
| Page history | Transaction log | "History" tab on every surface | Deep |
| Permalink to oldid | Labeled answer | Copyable label on every answer; label replays exactly | Deep — Gneiss fixes oldid's transclusion leak |
| Diff | Time/context diff | Two-slider diff: tx time, valid time, context | Extension beyond any wiki |
| Citation / footnote | Justification edge | Inline superscript → why() panel | Deep |
| [citation needed] | Weak epistemic grade | Computed grade badge rendered inline | Gesture deep, semantics upgraded |
| Red/dangling link | Typed missingness | Absence as typed, clickable invitation | Deep as affordance; Gneiss types it |
| Talk page | Hypothesis/contested layer | "Disputed" drawer per value | Deep; Gneiss adds structure |
| ThreadMode→DocumentMode refactoring | Rules computing belief views | The front page *is* the refactoring, recomputed deterministically | **Deepest — mechanizes c2's unscalable labor** |
| Revert/rollback | Supersession | "Revert" appends, visible in history | Deep |
| Watchlist | Justification-cone subscription | "Watch this value and everything it depends on" | Extension — wikis are dependency-blind |
| Protection / pending changes | Authority lattice / decision gating | Edit affordances shaped by caller's authority; pending queue | Deep |
| Edit summary | Assertion envelope | Mandatory structured "because…" on every action | Deep |
| Infobox / template | Structured slice of belief view | Panel and prose fed by the same assertions | Deep |
| Wikidata rank + qualifiers + references | Grade + envelope, displayed | Preferred bold, superseded struck-but-present | Deep — the existing UI to copy |
| RecentChanges | Scoped transaction feed | Per-team/per-cone pulse surface | Deep |
| Fedwiki journal | Per-artifact provenance | Exported/printed views carry their label + lineage | Deep in shape; Gneiss adds tamper-evidence |
| Fedwiki fork / chorus | What-if context / overlay | "Fork this view" → sandboxed context; side-by-side compare | Fair |
| Xanadu transclusion + two-way links | Live view refs + reverse traversal | Quote-by-reference; every quote knows its uses | Adopt internally |
| Reveal Codes | Full inference/override/journal drill-down | Complete, live, *actionable* machinery pane | Design brief |
| Formula bar / trace precedents | why(value) | Click any number → rule, bindings, testimony arrows | Design brief |
| Palantir-style Actions | Typed proposals routed by authority | "This is wrong because… / This changed" adjacent to every value | Adopt |

## Failure modes to design against

1. **Review-bandwidth insolvency** (killed c2): machine-rate proposals vs human-rate decisions. Budget decision bandwidth; price admission; pre-review by rules; aggregate duplicates.
2. **Distillation debt** (ThreadMode bloat): if the contested layer grows faster than it resolves, the front page rots. Track open-hypothesis age as a first-class health metric on every surface.
3. **False confidence from visible machinery** (spreadsheets): a beautiful why() panel over an unvalidated rule is worse than none. Pair every reveal with its conformance status; badges must be earned.
4. **Reveal-layer rot** (View Source → minified junk): keep the machinery layer at human altitude; raw ledger dumps are DevTools, not Reveal Codes — offer both, default to curated.
5. **Broken reproducibility promises** (oldid's template leak, Jupyter's hidden state): if a label doesn't replay exactly, users learn to distrust all labels. "Say it again" must be routinely demonstrated in the UI.
6. **Chorus without a lead voice** (fedwiki UX): one designated operational context on top; forks one deliberate click down.
7. **Permanent memory as liability** (anti-ForgiveAndForget): typed redaction/tombstones and personal-data handling planned now; "What have you forgotten?" needs an answer that is neither "nothing, ever" nor a silent hole.
8. **Observable without authorization** (c2's missing organ): scope every feed and history by the authority lattice from day one.
9. **Single-maintainer integrity** (froze c2): governance and custody of the ledger must survive any individual's attention.

## Sources

Fetched directly: c2 WikiDesignPrinciples (JSON backend): http://c2.com/wiki/remodel/pages/WikiDesignPrinciples · MeatballWiki SoftSecurity: https://meatballwiki.org/wiki/SoftSecurity

Search-verified: WikiWikiWeb history & 2014–15 freeze: https://en.wikipedia.org/wiki/WikiWikiWeb · https://news.ycombinator.com/item?id=12715560 · Federated Wiki: https://en.wikipedia.org/wiki/Federated_Wiki · https://github.com/fedwiki (core updated June 2026) · https://github.com/wardcunningham/smallest-federated-wiki · https://blog.jonudell.net/2014/12/29/individual-voices-in-the-federated-wiki-chorus/ · Roam/Obsidian trajectory (third-party estimates): https://medium.com/@theo-james/roam-research-vs-obsidian-has-roam-died-aba7bb456b4d

From background knowledge, labeled where used: "simplest online database" provenance; WP:V 2012 RfC; KeptPages/UseModWiki expiry; Panko spreadsheet-error research; 2020 UK Test-and-Trace XLS incident; Pimentel et al. 2019 notebook figures; Xanadu chronology; Reveal Codes behavior. Not verified: meatballwiki.org restoration provenance; c2 decline causal ordering (marked as interpretation).
