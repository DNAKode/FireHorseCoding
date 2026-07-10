# Charter Review: Gneiss and KodePorter

**Status:** Point-in-time critical review (2026-07-10 check-in; revised same day after steward comments — the scale-down floor, the zero-ceremony correction, visualization-first, and the FrankenSim showcase discipline: see §2.2, §2.3, §7, §8). Input to the next charter revision, not standing law.
**Scope:** [Gneiss/CHARTER.md](Gneiss/CHARTER.md) and [KodePorter/CHARTER.md](KodePorter/CHARTER.md), read against the full supporting corpus (`Gneiss/kb/`, [THE-STORY-SO-FAR](Gneiss/THE-STORY-SO-FAR.md), [THE-PAGE-v0](Gneiss/kb/maxwell/THE-PAGE-v0.md), the KodePorter [framing notes](KodePorter/docs/brainstorming-and-project-framing.md), and [kb/37](Gneiss/kb/37-KODEPORTER-REALIZATION.md)).
**Method:** verdict first; then adversarial passes from four hostile stances; then specific too-tight / too-loose findings, each with a proposed amendment; then counter-proposals considered and mostly declined; then value-accretive ideas. Companion documents [Gneiss/ROADMAP.md](Gneiss/ROADMAP.md) and [KodePorter/ROADMAP.md](KodePorter/ROADMAP.md) assume the amendments below are accepted unless noted.

---

## 1. Verdict in brief

**Yes, this can work — and the charters are unusually good instruments for making it work.** They contain things most charters never do: kill criteria, non-goals with teeth, complexity budgets, amendment ceremony, an explicit granularity rule, and a design center each ("testimony vs. belief under declared context"; "the living port map") that is genuinely load-bearing rather than decorative. The grade-vs-warrant separation, the sidecar-first posture, and the "index finely, claim selectively, cite precisely" rule are correct and should not be touched.

The dangers are not conceptual. They are three, and all three are execution-shaped:

1. **Execution starvation.** The corpus's own synthesis says it plainly: *"the corpus is gorgeous and unfalsified — the most dangerous state it could be in."* That was written on 2026-07-05. Five days later there are two more beautiful documents (the charters) and still zero running artifacts. P-1 — two days, paper only, designed in the project's first hour — remains unrun. The Kay bet remains unplaced. The charters do not cause this failure mode, but they do not stop it either: their phase language has no dates, no budgets, and no forcing function. This review and the two roadmaps are, ironically, three more documents; they must be the last ones before code.
2. **Self-referential validation.** Gneiss's first customer is KodePorter — a system that does not exist, whose own first build depends on Gneiss. Both charters were written by the same mind-set in the same week; KodePorter was *selected* because it exercises Gneiss. That is legitimate co-development, but it means the pair can pass each other's tests without ever touching an unsympathetic reality. The genuinely external anchors available — FrankenTui/FrankenTui.NET (real repos, real mess), Smoothscrape/AIMS (real systems, real correction pain), TypeScript→Go (a real port by strangers) — appear in the charters as later phases or benchmarks. They need to be pulled forward as instruments, not rewards.
3. **Unpriced economics.** Both charters *name* the binding constraints — review-bandwidth insolvency (the documented cause of death of c2, flagged as "the day-one problem for agent-rate platforms"), map-maintenance cost vs. re-investigation cost (KodePorter's own first kill criterion) — and neither *budgets* them. A kill criterion that reads "maintaining the map costs more than the repeated investigation it prevents" is undecidable unless someone is measuring both sides from day one. Nothing in either charter requires that measurement to exist.

Everything below is in service of those three findings.

---

## 2. Adversarial passes

### 2.1 The skeptical buyer (against Gneiss's value)

*"The composite is unclaimed"* — the prior-art surveys' central finding — has a second reading the corpus does not entertain seriously enough: **the composite is unclaimed because nobody's pain has ever justified its ceremony.** Datomic, XTDB, Data Vault, event sourcing, bitemporal SQL:2011 all exist, all are adopted piecemeal, and every organization that needed provenance-plus-revision built exactly the fragment it needed and stopped. Fifty years of near-misses can be read as lineage — or as a market verdict rendered eleven times.

The corpus's rebuttal is the "two economies" argument: agent-rate testimony simultaneously makes the substrate affordable (agents do the structuring gIBIS demanded of exhausted humans) and necessary (stochastic contributors at machine rate need adjudicable conflict). This is the load-bearing why-now claim, and it is **currently evidence-free within the project**. No experiment run so far distinguishes "Gneiss is what agent-era systems need" from "Gneiss is what this particular architect finds beautiful."

What would count as evidence, cheaply: (a) the witness-stand test applied to one real system (P-1, two days, paper); (b) a fresh agent measurably outperforming its baseline when given a claim ledger instead of transcripts (the orientation benchmark, defined in the KodePorter roadmap); (c) one person who is not Govert adopting A0 discipline and reporting it paid. The roadmaps schedule (a) and (b). (c) should be sought opportunistically — publishing the witness-stand essay is the cheap probe.

The skeptical buyer's residual point stands and should be pinned to the wall: **Gneiss succeeds as infrastructure only if a system that uses it becomes visibly better than the same system without it, at a cost its operator willingly re-pays.** The A2-vs-A1 "keep-earning memo" (kb/31's P2 gate) is the honest instrument for this, and it has quietly fallen out of the charter's development programme. It must come back (amendment G-A7).

### 2.2 The YAGNI engineer (against KodePorter's value)

The hostile framing: *"You are proposing forty nouns, five graphs, and a governance process in order to port a parser. I would use a `PORTING.md`, a test suite, and a coding agent, and I would be done before your dossier template stabilizes."*

This deserves a straight answer, because for small ports the YAGNI engineer is **right**. KodePorter's value claim only activates when at least one of these holds: the port is large enough that no session holds it; the source keeps moving; multiple agents/people rotate through; or acceptance requires defensible evidence rather than vibes. The charter says this in scattered places but never states the **activation threshold** plainly, and the first implementation (a deliberately small slice) is precisely the case where the YAGNI alternative wins. That is fine for a mechanism test — but it means the slice proves *machinery*, not *value*, and the charter's own success language ("easier to understand, safer to continue, cheaper to preserve than a collection of coding-agent sessions") can only be tested at FrankenTui scale or beyond. The roadmap therefore treats the slice as a mechanism gate and the FrankenTui bootstrap as the first value gate, with an explicitly defined baseline competitor: **a well-run agent workflow with repo access, a docs folder, and tests — not chaos.** Beating chaos is not the bar; chaos is not what competent teams have.

**Steward response, adopted (2026-07-10):** the YAGNI floor is not an objection to absorb but a *product boundary to claim*. The minimal conformant KodePorter port is declared to be: two repository addresses, a `PORTING.md`, and optionally some transcripts — nothing more. KodePorter must be able to **adopt** that floor as-is (ingest it as testimony), enrich it only where enrichment pays, and **export** back to it at any time, so every richer representation degrades gracefully to a form any tool can read. This inverts the lock-in economics of the agent-tooling market: every coding-agent system accretes proprietary memory to hold its users; KodePorter's durable representation is deliberately agent-system-neutral, so it **ports the port** — intentions, correspondences, decisions, evidence — between Claude Code, Codex, Cursor, and whatever replaces them. The moat is not memory; it is the portable, verifiable form of the memory. (Amendment K-A9; KodePorter roadmap §3.0.)

Second YAGNI point, sharper: by late 2026 every serious coding-agent harness ships some durable project memory (memory files, plan documents, task systems). If "durable memory across sessions" is the pitch, the moat erodes monthly. What the agent ecosystem does **not** ship — and is structurally unlikely to — is *typed correspondences with declared equivalence criteria, claim-directed verification, and acceptance gates whose evidence is re-runnable*. KodePorter's differentiation is the **verification-and-acceptance spine, not the memory**. The roadmap weights epics accordingly (differential harness and decision workflow before dossier polish), and the charter's product framing should tilt the same way.

### 2.3 The economics bear (against both)

Three numbers decide these projects, and none appears in either charter:

1. **Minutes of human judgment per accepted claim — with zero as the design point.** Every *mandatory* decision, review, or ceremony consumes the scarcest resource in a solo-operator system, and (steward correction, 2026-07-10) neither system is entitled to impose any: pointing a coding agent at KodePorter must be able to cost **zero human minutes** — agents decide under delegated policy, mechanical evidence gates acceptance, and humans receive only what policy routes to them. Human judgment is a dial, not a toll; neither Gneiss nor KodePorter imposes procedural demands — they have to add value. The machinery already exists in the corpus — statutes over verdicts, three-band triage, per-method budgets, authority delegation (kb/26) — but the charters treat it as available options rather than as the default posture. If the vertical slice *requires* 30 human decisions, that is a defect in the policy layer, not a cost of doing business. (Amendments K-A10, G-A9.)
2. **Cost of capture at the moment of discovery.** The Sketchpad lesson from the history mine — *"intent must pay rent same-session"* — is the single most predictive variable for whether the map gets maintained. If recording a correspondence or divergence takes more than seconds beyond the work the porter was already doing, the map decays into the very status documents FrankenTui already has. Neither charter states this as a design constraint. It should be constitutional for KodePorter (amendment K-A8).
3. **Cost of *not* capturing.** Re-investigation cost is claimed to be large and is never measured. Without a measured baseline, the M3 economics memo will be written from impressions — i.e., it will pass.

The bear's conclusion: instrument or die guessing. Both roadmaps make the instrumentation an epic-level deliverable, not a virtue.

### 2.4 The historian (their own lineage, turned on them)

Every ancestor in the mine died of one of three causes — capture economics, adoption economics, or silent forgetting — and the corpus says "we have named all three, which makes us without excuse." The historian notes a fourth cause the mine documents but the risk register does not name: **the founder's medium became the product.** gIBIS, Compendium, and the rationale-capture tradition died specifically because the people best at the method loved producing its artifacts more than its consumers loved consuming them. Forty kb documents in six days is virtuosic — and it is also exactly the failure signature of that tradition. The defense is not abstinence but *conversion pressure*: every future document of substance should either be executable (fixtures, drills, specs with oracles) or be the recorded reasoning of a decision that gated code. The roadmaps adopt this as policy ("documents earn their place by gating artifacts").

One more historical rhyme worth honoring: ACID arrived from outside Codd's frame ("the Gray warning"). The story-so-far predicts Gneiss's Gray will arrive from testimony economics, privacy law, or the social layer. Keep the standing "Gray log" — it costs nothing and primes recognition (the roadmaps carry it).

---

## 3. Too tight, too early

Findings where the charters commit prematurely. Each carries a proposed amendment; identifiers (G-A*, K-A*) are consolidated in §7.

Steward gloss adopted for all of them, and for §5 symmetrically: *nothing is set in stone*. Demotion is parking, not deletion — every noun, verb, or mechanism moved off the v0 spine lands in an explicit **attic register** with its rationale and a promotion trigger, revisited at every gate. And the too-loose findings are tightened incrementally at gates as evidence arrives, rather than blocking the start. The charters remain living documents; the roadmaps schedule when each question gets its answer.

### 3.1 Gneiss: ten constitutional verbs before one has run (§6)

The charter places `sprout/commit` (what-if worlds) and `import` (federation watermarks) inside "the smallest stable conceptual spine," and the golden corpus (§14.5) demands what-if-commit-over-a-moved-base fixtures. These are the two most speculative mechanisms on THE PAGE — `sprout/commit` is tagged the hardest theorem to mechanize (T4), federation is explicitly `[F]` off-spine — and **nothing in the KodePorter vertical slice needs either**: agent proposals are `proposed`-status assertions plus decisions, no world-forking required. Freezing all ten verbs constitutionally before implementation invites building to the constitution instead of to the customer.
**→ G-A1:** keep all ten verbs as chartered *vocabulary*, but designate `record / decide / declare / ask / why / seal / purge` as the v0 conformance spine; `sprout / commit / import` become Phase-4 verbs, conformance-optional until a domain demonstrates need. Move the what-if and federation fixtures to the Phase-4 corpus.

### 3.2 Gneiss: mature-protocol amendment ceremony applied to an unborn protocol (§14.4)

The amendment discipline (demonstrated problem in a real domain, rejected simpler alternative, migration consequences, new conformance fixtures, dated decision) is right for a protocol with adopters. Applied now, with zero users and zero code, it has one predictable effect: implementation will contradict the charter weekly, the ceremony will be too heavy to invoke weekly, and deviation will accumulate *silently* — the exact sin ("unwitnessed change") the whole system forbids. A constitution nobody can afford to amend is routed around, not obeyed.
**→ G-A3:** add formative-phase amendment rules — steward decides, reason recorded in a dated log, affected fixtures updated; full ceremony reserved for bedrock (B1/B2) and for §4 principles — in force until the first external adopter or Phase 4, whichever comes first. Add a **charter-debt register**: known places where implementation deviates pending amendment, so drift is witnessed. *(This is their own ceremony principle — change is permitted, unwitnessed change is not — applied to the charter itself.)*

### 3.3 KodePorter: a forty-noun spine before one migration unit has lived (§7)

The domain model declares ~34 first-class nouns as "the initial KodePorter spine," and §18's discipline (changes must "preserve distinctions") ratchets them in place. Four separate flavors of difference (Adaptation, Exception, Divergence, KnownDeviation) plus three flavors of meaning-decomposition (Capability, BehavioralContract, SemanticUnit) is prose-crisp and use-untested; vocabulary of this width, chartered before contact, is how domain models become taxonomies that users file things into rather than tools they think with. The charter's own §18.6 warns that every noun has permanent teaching cost.
**→ K-A1:** partition §7 into a **core spine** (~14 nouns: PortProject, SourceBasis/TargetBasis, Artifact, CodeEntity, MigrationUnit, Dossier, Correspondence, PortPolicy, EquivalenceCriterion, Divergence, Obligation, VerificationPlan/Run, SourceDelta, ImpactCone, Decision) and **candidate vocabulary** (everything else), with candidates confirmed, collapsed, or discarded by the slice and the FrankenTui bootstrap. Concretely: fold Adaptation/Exception/KnownDeviation into `Divergence.kind ∈ {adaptation, exception, intended, observed, unresolved}` with a lifecycle, and BehavioralContract into a Dossier section, promoting them back to first-class only when flattening demonstrably loses a distinction someone needed. StaleMapping is a state, PortHealth is a view, TransformationStrategy is an enum field — say so.

### 3.4 KodePorter: the thirteen-step slice as a single all-or-nothing gate (§15)

The first implementation is called "thin" but exercises every Gneiss mechanism and every KodePorter mechanism in one demo: two analyzer imports, dossier, typed correspondences, differential verification, agent proposal, decision, labeled views, source delta, impact cone, correction, view reproduction, and an amnesia drill. As a *target* it is exactly right (nothing can hide behind infrastructure progress). As a *gate* it means nothing is checkable until everything works, which defeats the point of a small cell.
**→ K-A2:** restage the same thirteen steps as three cumulative sub-gates, each with kill signals: **S1 Mapped & Claimed** (steps 1–6: pin, import, unit, dossier, correspondences, evidence with a re-runnable differential run), **S2 Proposed & Decided** (steps 7–9: proposal, decision, labeled views with `why`), **S3 Changed & Honest** (steps 10–13: delta, staleness cone, correction-without-erasure, view reproduction, narrow-seal amnesia drill). The roadmaps use this staging.

### 3.5 Both: the "week" that hides a quarter

Gneiss Phase 1 promises a one-week reference cell "needed by one KodePorter migration unit." The Gneiss cell alone may honor the week (THE PAGE's own build plan warns the strainers and label plumbing eat two of the seven days). But the KodePorter shell around it — two language providers, pinned bases, a differential harness, proposal tooling — is several times that, and the charter pairing lets each project book the other's cost as "not mine." No amendment needed; the roadmaps simply budget them separately and honestly (Gneiss cell ≈ 2 focused weeks; the full slice ≈ 6–9 focused weeks part-time).

---

## 4. Too loose

### 4.1 Gneiss: no declared envelope for its own first build (§13 vs §15)

The charter demands that every architectural choice cite a declared, versioned operating envelope — then presents a five-phase development programme with no envelope. By its own rule this is nonconforming, and it matters practically: phantom scale requirements ("but what about machine-rate telemetry?") are exactly what creep into unenveloped designs.
**→ G-A2:** declare the v0 envelope in the charter's development programme: single ledger, single writer, human-cadence decisions (tens/day), agent-rate proposals (hundreds/day burst), ledger ≤ ~10⁶ assertions, full L0 recompute acceptable at interactive latency for views under ~10⁵ visible assertions, SQLite-class durability, no federation. Choices citing it; drift-signals named.

### 4.2 Gneiss: "important knowledge" has no chartered admission test (§8)

The scope sentence — knowledge "important enough that source, timing, policy, disagreement, or revision matter" — is a vibe until a domain operationalizes it. KodePorter did (the claim-promotion rule, §8); Gneiss's charter should require that move of every domain rather than leaving it to each adopter's taste, because the two failure modes it prevents (everything-is-a-claim EAV creep; nothing-is-a-claim ceremony avoidance) are both fatal and both documented in the risk register.
**→ G-A5:** every domain realization must declare, as adoption preconditions: (a) its claim-promotion rule, (b) its review-bandwidth budget (expected human decisions per period, queue-depth alarm, statute-over-verdict posture). Add both to the A2 definition in §9.

### 4.3 Both: review-bandwidth named as lethal, budgeted nowhere

D25/c2's death-by-review-insolvency is cited across the corpus as *the* agent-era hazard, and KodePorter §13 lists autonomy levels with per-work-item gates — but no charter text says what happens when the queue outruns the human, or what the design budget is. For a solo operator commanding an agent fleet, this is the binding constraint from day one.
**→ K-A4:** make review economics a first-class port-health dimension with a declared budget and a stop rule (queue depth × age triggers admission-threshold tightening and statute proposals — the mechanism already sketched in kb/32's hypothesis-spam mitigations, promoted from risk-note to charter).

### 4.4 KodePorter: verification economics unstated (§11, §15 step 6)

"Attach … a differential or otherwise independent verification run" is one clause describing the hardest engineering in the entire slice. A real differential harness — input corpus capture, dual runners, canonical serialization, comparison under a declared criterion, error-semantics mapping across languages — is a first-class artifact whose cost rivals the rest of the slice combined. The charter's test-laundering threat analysis is excellent; its resourcing implication is absent.
**→ addressed in roadmap** (harness is its own epic, K4, scoped to pure-function units first) **plus K-A3:** evidence supporting an acceptance decision must be *mechanically re-derivable* (the system can re-run it) or be explicitly marked attested-only and be insufficient for acceptance under default policy; agent-cited evidence coordinates (file, range, commit) are mechanically verified to exist and to contain what the claim says. This also operationalizes adversarial testimony — the corpus's named blind spot — in its most likely first form: a model that fabricates or misquotes evidence.

### 4.5 KodePorter: negative knowledge is not chartered (§7, §10.4)

Porting work constantly produces closure-shaped findings — "no other callers of X at basis B," "this API surface is not used by the target," "we looked for a lock discipline and there is none" — and these are precisely what agents silently re-derive and what impact analysis needs (a delta invalidates the closure, not just the mapping). Gneiss has the machinery (typed missingness, closure declarations, I8); KodePorter's charter never asks for it.
**→ K-A5:** add investigated-absence (closure declarations over declared scopes, at pinned bases) to the dossier's chartered outcomes and to the impact-cone inputs.

### 4.6 Gneiss: the steward is unnamed and the P-track's status is unstated

§14.3 requires a named steward per milestone; nobody is named (it is obviously Govert — say so, with the recording obligations). And the charter's development programme silently supersedes the kb/31 two-track prototype programme (P-1/P1 vs P0–P5) without saying which parts survive; a reader of the corpus cannot tell whether Smoothscrape P1 is retired, deferred, or forgotten.
**→ G-A4:** name the formative-phase steward. **→ G-A6:** state that the charter's programme absorbs the P-track: P-1 memos survive as Phase-3 domain-selection instruments (and as the cheapest generality kill-test); P1 remains a value-track option if Smoothscrape independently warrants it; P0/P2–P5 are superseded by the Phase-1/2 cell.

### 4.7 Gneiss: the keep-earning gate fell out of the charter

kb/31's P2 gate — the honest memo asking whether the kernel beats A1-native tables — is the project's most important self-honesty instrument, and §15/§16 do not schedule it.
**→ G-A7:** require a dated **keep-earning memo** at the Phase-2→3 boundary: what did Gneiss provide that claim-keyed native tables would not have, at what complexity cost, with the pattern-book descope (kb/32 §4.3) as a first-class, celebrated outcome if the memo says so.

---

## 5. Cross-charter coherence findings

1. **The litmus test overstates separability.** KodePorter §4.1: remove Gneiss and "the remaining nouns and workflows still describe a crisp porting product … with weaker memory, provenance, revision, and accountability." But memory, provenance, revision, and accountability are the *product's lead features* (§2: "durable memory that survives models, sessions, branches, and team changes"). What actually remains without Gneiss is a code-cartography and workflow tool — crisp, but not the product the charter sells. This is acceptable for co-developed systems and does not need fixing; it needs *admitting*, because it affects sequencing: KodePorter cannot meaningfully precede the Gneiss cell, so the Gneiss cell is on the critical path of everything. The roadmaps sequence accordingly.
2. **Grade vs. warrant is handled well** — the cleanest boundary in the pair (Gneiss §4.9, KodePorter §11, kb/37 §7). Leave untouched; encode it in the shared fixtures early so it stays clean under implementation pressure.
3. **Shared conformance fixtures are gestured at, not specified.** Both charters invoke shared canonical examples (Gneiss §10.1, KodePorter §4.3/§18.5) with no owner or location. The roadmaps establish a single jointly-owned fixture: **Slice Zero**, a deliberately tiny Rust crate + C# port + scripted source-delta sequence + ground-truth answer key, living at `fixtures/slice-zero/`, referenced by both projects' conformance suites. One fixture, two charters kept honest — divergence in understanding surfaces as fixture disagreement.
4. **Vocabulary collision risk is real but manageable:** KodePorter `Decision/Review` vs Gneiss `decide`, `VerificationRun` vs `observation`, KodePorter warrants vs Gneiss grades. The glossary discipline exists (Gneiss §14.4); extend it with a two-column mapping table in the shared fixtures so the binding is explicit rather than idiomatic.

---

## 6. Counter-proposals considered

**CP-A: Build KodePorter first, without Gneiss, on plain tables; extract Gneiss later from what it actually needed.** The strongest rival plan — it is how most good substrates are born (extraction, not construction), and it directly attacks the self-referential-validation risk. **Declined, narrowly**, for two reasons: (1) the extraction experiment has effectively been run — Smoothscrape's overlay tables, FrankenSim's fs-ledger, and the corpus's own prose-ledger are three independent hand-rolled fragments; the third-reinvention signal that justifies a shared library (kb/30's A2 entry rule) has already fired. (2) The Gneiss cell is genuinely small (a week-scale artifact by design); deferring it saves little and forfeits the co-development pressure that is the point. **But CP-A survives as the descope path:** if the M3 keep-earning memo says the cell isn't paying, KodePorter continues on its native tables and Gneiss ships as a pattern book. That exit must stay cheap, which is one more reason the sidecar boundary (KodePorter never imports Gneiss types into its domain model) must be enforced from the first commit.

**CP-B: Go brownfield-first — anchor Phase 1 on FrankenTui instead of a synthetic slice.** Attractive because FrankenTui is where the value claim lives and it is real. **Declined:** brownfield front-loads the two hardest open problems (identity across refactors; correspondence inference at scale) before the model has stabilized on easy ground — a classic way to drown a kernel in its hardest case. **Adopted instead:** FrankenTui *read-only cartography* runs early as a parallel probe (no Gneiss dependency, pure data-gathering), and the FrankenTui orientation benchmark is the M4 value gate with teeth (defined quantitatively in the KodePorter roadmap §7). The synthetic slice gets a hard timebox so it cannot become a comfortable residence.

**CP-C: Restore Smoothscrape/AIMS as the first Gneiss proving ground (the original P-track).** Half-adopted: the P-1 *paper probes* (two days each, zero code) return as scheduled M3 instruments — they are the cheapest external falsifier of the vocabulary and they select the Phase-3 second domain. Full P1 (claim-keyed links in production Smoothscrape) is not restored as a gate: it is real value but it validates only rings B1–B2, exercises none of the staleness/agent/seal machinery KodePorter needs, and would split solo attention. It remains chartered as an option if Smoothscrape's own needs pull it.

**CP-D: Ship Gneiss as spec + pattern book only; never build the engine.** The corpus itself holds this door open (kb/32 kill outcome 3). Kept as a *live, celebrated* exit at M3 — but not adopted now, because the two-economies claim (the whole strategic case) is only testable with a running cell under agent load, and because KodePorter's differentiation (re-runnable evidence, labeled views) needs the machinery, not the discipline alone.

---

## 7. Consolidated amendment list

**Gneiss charter:**
- **G-A1** (§6, §14.5): v0 conformance spine = `record/decide/declare/ask/why/seal/purge`; `sprout/commit/import` become Phase-4 verbs; move what-if and federation fixtures to the Phase-4 corpus.
- **G-A2** (§15): declare the v0 operating envelope (single writer, human-cadence decisions, agent-rate proposals, ≤10⁶ assertions, L0-recompute latency targets, no federation) per the charter's own §13 rule.
- **G-A3** (§14.4): formative-phase amendment rules + a charter-debt register; full ceremony reserved for bedrock and §4 principles until first external adopter or Phase 4.
- **G-A4** (§14.3): name the formative-phase steward (Govert), with the recording obligations.
- **G-A5** (§8, §9): A2 adoption preconditions: a declared claim-promotion rule and a declared review-bandwidth budget per domain.
- **G-A6** (§15): state the kb/31 P-track's disposition: P-1 memos retained as Phase-3 selection instruments; P1 optional value-track; P0/P2–P5 superseded.
- **G-A7** (§15–16): schedule the keep-earning memo at the Phase-2→3 boundary; pattern-book descope is a first-class outcome.
- **G-A8** (§6): reference the two standing THE-PAGE flags from kb/50 §4 (ctx is a materialized view over declared assertions, not a seventh base relation; R13/R14 negation strata need explicit verification) as implementation obligations, so they cannot be lost.
- **G-A9** (§12): zero-mandatory-ceremony parity: a conforming Gneiss experience must support fully-delegated operation — agents record, propose, and (under declared, revocable policy) decide with no human in the loop. Any human-only gate is a *policy* choice, never a structural requirement; imposed procedure without a policy basis is nonconforming.

**KodePorter charter:**
- **K-A1** (§7): partition into core spine (~14 nouns) and candidate vocabulary; collapse the four difference-flavors into typed `Divergence`; BehavioralContract folds into Dossier initially; promote back only on demonstrated loss.
- **K-A2** (§15): restage the thirteen steps as sub-gates S1/S2/S3 with kill signals per stage.
- **K-A3** (§11): acceptance-grade evidence must be mechanically re-derivable (or is attested-only and insufficient by default); agent-cited evidence coordinates mechanically verified. (Adversarial testimony, first defense.)
- **K-A4** (§12–13): review economics as a health dimension with declared budget and stop rule.
- **K-A5** (§7, §10.4): investigated-absence / closure declarations chartered as dossier outcomes and impact-cone inputs.
- **K-A6** (§14): promote "inspectable repository artifacts" from component #9 to a design principle: the durable layer of the map must be exportable as diffable, in-repo, human-readable artifacts; reading a dossier must require no KodePorter runtime.
- **K-A7** (§16): define the FrankenTui orientation benchmark quantitatively (metric, baseline, honesty rule) — see roadmap §7 for the definition to adopt.
- **K-A8** (§13, §18): the same-session rent rule as a design constraint: capture rides on work already being done; standalone ceremony is a tracked smell.
- **K-A9** (§5, §14): the representation ladder with a declared floor: **KP-0** (two repository addresses + `PORTING.md` + optional transcripts) is a *conformant KodePorter port*. Every richer tier must ingest the floor, pay for its own enrichment, and export back down without losing the floor's content. The durable representation is agent-system-neutral by principle: KodePorter ports the port between coding-agent systems, and leaving KodePorter is always free.
- **K-A10** (§13): the autonomy dial with zero as a supported setting: acceptance may be fully delegated to policy backed by mechanical evidence (re-runnable verification per K-A3); the review queue receives only what policy routes to humans; sampled audit is the standing honesty check; imposed procedure is a tracked defect.

---

## 8. Value-accretive ideas worth incorporating

Ranked roughly by information-per-cost. The roadmaps schedule 1–6, 10, and 11; 7–9 are cheap opportunistic bets.

1. **Two-tier capture: journal cheaply, promote deliberately.** The granularity rule ("claim selectively") must not mean friction at capture time, or agents and humans will route around the ledger (a chartered kill criterion). Give every agent/human a one-line, zero-ceremony `note` verb that appends candidate testimony to an inbox; promotion to claims happens in batch, later, by triage. Selectivity moves from the moment of discovery (where it taxes) to review (where it belongs). *(Adopted: both roadmaps.)*
2. **The orientation benchmark as the product's first proof.** Define now, run at M4: a fresh agent with map+ledger access must answer the charter's six engineer-questions about FrankenTui faster and more correctly than the same agent with repos, docs, and transcripts alone. This converts "durable memory that survives sessions" from slogan to measurement — and its failure mode is as informative as its success. *(Adopted: KodePorter roadmap §7; charter amendment K-A7.)*
3. **The map's durable layer as in-repo, diffable artifacts — and git as the federation transport.** Dossiers, correspondences, decisions, and policies export canonically into the repos they describe (`.kodeporter/`), so Git supplies distribution, review-by-PR, merge, and history; the map is readable on GitHub with no runtime. A further spike-worthy alignment: Gneiss federation semantics (single-sequencer ledgers, watermark imports, no global clock) map startlingly well onto git's model — one ledger per clone, import-with-watermark ≈ fetch/merge. Not a commitment; one honest spike at M3. *(Adopted: K-A6 + roadmap spike.)*
4. **The differential harness as a standalone product seed.** Pin inputs, run both implementations, compare under a declared equivalence criterion, emit a re-runnable VerificationRun. Teams porting anything want this even without the map — it is also the component that generates the evidence stream making the map worth having. Build it early as its own artifact with its own README; it is the most exportable thing in the programme. *(Adopted: KodePorter epic K4.)*
5. **The witness-stand essay as a zero-code adoption probe.** kb/04's seven questions are a portable audit instrument for *any* data system. Publishing "can your system take the stand?" costs a day, tests whether the framing resonates with strangers (the missing external signal from §2.1), and creates A0 adopters at zero engineering cost. *(Scheduled as an M3 option; publication is Govert's call — D10.)*
6. **The second-implementation proof, agent-executed.** Kay/D29: you are bedrock when strangers implement your questions. With agents, the "independent reimplementation from THE PAGE alone" test costs a few days: hand the frozen page to a fresh agent with no corpus access, let it build, diff the belief views against the reference on the golden corpus. Divergences are spec bugs — the cheapest spec-debugging available. *(Adopted: Gneiss roadmap E8/M4.)*
7. **Strainers as versioned, shared micro-packages.** The confessed "hidden 40%" (comparators, tolerances, interval clipping, key normalization) becomes an asset if each strainer is a tiny pure versioned package with golden tests, cited by `RuleVer` in justifications. Domains then share and audit comparability logic instead of re-hiding it in application code.
8. **Provenance anchors in generated code.** Stable claim-key anchors in target-code comments (`// kp:unit=… claim=…`), verified by CI against the map (anchors must resolve; orphans flagged). Makes the correspondence bidirectionally navigable *in the artifact developers actually touch* and survives file moves. Comment-rot risk is real: spike, don't commit.
9. **Gneiss-on-Gneiss as a standing fixture, not a project.** The corpus's own decision history (D1–D35, supersessions, corrections) is real revision-rich data. Once the cell runs, load it as a conformance fixture: the project's own history takes the witness stand. Cheap, self-similar, and it keeps the fixture corpus honest with non-synthetic data. *(Adopted: Gneiss roadmap E5, optional.)*
10. **Visualization from the first milestone — the Lens and the Atlas.** (Steward requirement, 2026-07-10: "there is no other way for me to judge value early enough.") Gneiss ships a **Lens** — static, regenerated HTML over the ledger: transaction timeline, belief view per context, `why()` trees, label inspection, view diffs — from M1. KodePorter ships a **Port Atlas** — both maps rendered, correspondences drawn, health dashboard with time series, impact-cone replay — from its first cartographer output. Both keep dated longitudinal snapshots: the visualizations *keep track*, so value (or its absence) accretes visibly, gate over gate. *(Adopted: new epics E-L and K-V in the roadmaps; both on the M1 critical path.)*
11. **The FrankenSim showcase discipline: the demo and the conformance run are the same bytes.** From the FrankenSim plan (kb/36 + the pinned plan §11.5, §13.3, §15–16), adopted on steward direction — selling the project is a first-class output artifact, not an afterthought: **named marquee demos** as milestone exits (FrankenSim: "topology optimization on a raw SDF — no mesh in the loop"; ours must be as nameable); **golden ledgers** — every gate lands with a replayable ledger of its own acceptance run, so the demo is evidence; **automatic lab notebooks** — every verification run and study emits a human-readable, reproducible report as a side effect ("reproducibility should be a side effect, not a virtue"); flagship stories carrying a "what breaks first" clause; ambition tags [S]/[F]/[M] with nothing [M] on the critical path; and targets stated so they can be failed. *(Adopted: both roadmaps' milestone tables now name their marquee demos and land golden ledgers.)*

---

## 9. What we deliberately leave alone

The design centers (testimony/belief separation; the living port map). The constitutional principles of both charters — nothing in this review touches §4 (Gneiss) or §6 (KodePorter) substance. The five-primitive kernel and THE-PAGE-v0 as frozen reference semantics pending the bet. The granularity rule. Grade-vs-warrant. Sidecar-first. The kill criteria (both lists are good; the roadmaps make them *schedulable* rather than aspirational). The Codd-programme ambition — properly guarded by the positioning steer ("theory is bedrock, not product"), it is a direction, not a deliverable, and needs no trimming now.

The verdict, restated: **the charters are fit to build on once ~19 amendments land, and the binding risk is not in the documents at all — it is that the next six weeks produce more documents instead of a running cell.** The roadmaps are designed to make that failure impossible to commit silently — and, per the steward's direction, to make the value visible early enough to judge: from M1 onward there is always something to *watch*, and every gate ships its own demonstration.
