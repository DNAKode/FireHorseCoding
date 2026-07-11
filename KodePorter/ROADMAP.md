# KodePorter Roadmap v0

**Status:** First development roadmap (2026-07-10 check-in; revised same day after steward comments — the representation ladder and anti-lock-in principle, the autonomy dial, the Atlas epic, the showcase discipline). Companion to [CHARTER.md](CHARTER.md); paired with [Gneiss/ROADMAP.md](../Gneiss/ROADMAP.md); assumes the amendments proposed in [CHARTER-REVIEW.md](../CHARTER-REVIEW.md) (K-A1…K-A10). Background: the [framing notes](docs/brainstorming-and-project-framing.md) and [Gneiss kb/37](../Gneiss/kb/37-KODEPORTER-REALIZATION.md).
**Ambition tags** (FrankenSim discipline): everything here is **[S]** unless marked; nothing **[M]** sits on the critical path. Vocabulary demoted by K-A1 lives in the shared **attic register** with promotion triggers — parked, not deleted; nothing is set in stone.
**Discipline:** technology choices are falsification conveniences; effort figures are guesses and labeled as such; every gate has a descope or kill branch that is a legitimate outcome. The standing documents-gate-artifacts policy from the review applies here too.

---

## 1. Destination: what "usable or falsified" means

Three exits, targeted within roughly 20 part-time weeks (jointly scheduled with Gneiss):

**Exit U (usable trajectory):** Slice Zero runs end to end through the charter's thirteen steps (staged as sub-gates S1–S3); the FrankenTui **orientation benchmark** (§7) shows a fresh agent with the map materially outperforming its baseline; one source delta produces a bounded, explainable impact cone with a review queue a solo operator clears within budget. KodePorter is then demonstrably on the way to its charter promise — "easier to understand, safer to continue, cheaper to preserve than a collection of coding-agent sessions, issue tickets, status documents, and ad hoc scripts."

**Exit D (descoped product):** the full map does not pay, but components do. The honest salvage set, pre-declared so descoping is a decision rather than a rout: the **differential verification harness** (standalone value for any port), **dossier-lite** (structured investigation notes with claim promotion), the **cartographer + Atlas** (read-only maps you can see), and the **KP-0 ladder verbs** (`kp adopt` / `kp export` — the PORTING.md tier survives any descope because it costs nearly nothing). These ship; the correspondence/preservation superstructure is shelved with a post-mortem.

**Exit K (kill):** a frailty signal in §8 fires and survives honest redesign — most plausibly identity churn (correspondences cannot survive ordinary repository evolution) or economics (the map costs more than the re-investigation it prevents, measured, twice).

A deliberate asymmetry with Gneiss: Gneiss's M2 gate is *mechanism* ("does the cell work"); KodePorter's real gate is M4 *value* ("does the map beat a competent baseline"). The vertical slice proves machinery; only FrankenTui-scale reality proves worth. This roadmap does not let the slice's success be mistaken for the product's.

---

## 2. Declared envelope and the two PortProjects

Per the Gneiss envelope discipline (charter §19 "Scale" made concrete for v0):

| Parameter | v0 declaration |
|---|---|
| PortProjects | exactly two: **Slice Zero** (synthetic, tiny, jointly owned fixture) and **FrankenTui → FrankenTui.NET** (real, brownfield, read-only until M4 explicitly opens remediation) |
| Language pair | Rust → C# only; the reverse direction and other pairs wait for G4 |
| Map scale | FrankenTui-class: ~10³ files, ~10⁴–10⁵ symbols per side; map regeneration ≤ minutes; map queries interactive |
| Claim scale | 10²–10³ durable claims per project (the granularity rule doing its job); claims live in Gneiss, map data does not |
| Deltas | single-commit to small-batch advances at human-review cadence; no continuous CI ingestion in v0 |
| Actors | one human steward + agent fleet; authority is a policy dial (K-A10) — fully-delegated acceptance (mechanical evidence gates, zero human minutes) and human-gated classes run side by side in v0; autonomy above "implementer-on-a-branch" (multi-agent coordination, migration operator) is out of v0 scope |
| Repository safety | discovery is read-only, enforced by running providers against detached checkouts; the only writable surfaces are Slice Zero's target branch (proposals) and the port workspace |

**Where the port state lives (K-D4):** a **port workspace** — its own directory/repo, not inside either subject repository — holding the map store, the Gneiss ledger file, and the exported `.kodeporter/` artifacts. Embedding artifacts into a target repo is a later, opt-in choice; for FrankenTui discovery it is prohibited by the read-only rule.

---

## 3. Architecture for the first product cycle

### 3.0 The representation ladder and the anti-lock-in principle (steward direction, 2026-07-10; amendment K-A9)

KodePorter scales **down** as deliberately as it scales up. The YAGNI engineer's floor is not the objection — it is tier zero of the product. Each tier is a *conformant* port project:

| Tier | Representation | What KodePorter adds |
|---|---|---|
| **KP-0 — the floor** | two repository addresses + `PORTING.md` (+ optional transcripts) | nothing yet — this already *is* a KodePorter port. `kp adopt` ingests it: repos pinned, `PORTING.md` parsed into candidate units, policies, and claims *as testimony* (not truth), transcripts indexed as evidence |
| **KP-1 — structured files** | + in-repo diffable artifacts: dossiers, correspondences, policies as md/yaml | typed, reviewable, durable memory; readable on any Git host with no runtime |
| **KP-2 — the workspace** | + map store, Gneiss ledger, differential harness, Atlas | verification-linked claims, labeled views, `why()`, staleness, health |
| **KP-3 — the monitor** | + continuous preservation | source deltas become bounded work |

Two rules keep the ladder honest. **Scale-down conformance:** every feature degrades gracefully to the tier below, and `kp export` always emits the floor — a fresh, current `PORTING.md` any tool or agent can read — so *leaving KodePorter is always free*. **Agent-system neutrality:** the durable representation never depends on one coding-agent harness. Every agent ecosystem accretes proprietary memory (memory files, plan docs, transcripts) to hold its users; KodePorter is the counter-move — it **ports the port** — intentions, correspondences, decisions, evidence — *between* Claude Code, Codex, Cursor, and their successors, ingesting their memory and transcripts as testimony and never writing anything only one of them can read. The moat is not memory; it is the portable, verifiable form of memory.

A port may live its whole life at KP-0 and be well served: that is the scale-down promise, and it is also the adoption wedge — `kp adopt` on an existing PORTING.md-style port is the cheapest first contact any user (starting with FrankenTui) can have with the product.

### 3.1 Components

The charter's nine components (§14), scoped to what v0 actually builds. The port workspace is the unit of deployment; everything is local-first.

```
        SUBJECT REPOS (read-only)                 PORT WORKSPACE
┌──────────────┐  ┌──────────────┐   ┌─────────────────────────────────────┐
│ Rust source  │  │ C# target    │   │ ┌─────────────────────────────────┐ │
│ (pinned      │  │ (pinned      │   │ │ MAP STORE (SQLite, regenerable) │ │
│  commits)    │  │  commits)    │   │ │ entities · references ·         │ │
└──────┬───────┘  └──────┬───────┘   │ │ containment · candidate links   │ │
       │                 │           │ └────────────┬────────────────────┘ │
┌──────▼───────┐  ┌──────▼───────┐   │              │                      │
│ rust-analyzer│  │ Roslyn       │   │ ┌────────────▼────────────────────┐ │
│ (SCIP index) │  │ (SCIP index/ │──▶│ │ DOMAIN STATE (exported to       │ │
└──────────────┘  │  roscli)     │   │ │ .kodeporter/ as diffable md/    │ │
   CARTOGRAPHER   └──────────────┘   │ │ yaml: units, dossiers, corres-  │ │
   (K2): pinned imports, stable IDs  │ │ pondences, policies, criteria)  │ │
                                     │ └────────────┬────────────────────┘ │
┌──────────────────────────────┐     │              │ claims/decisions/    │
│ DIFFERENTIAL HARNESS (K4)    │     │              │ evidence/labels      │
│ JSONL case corpus → run both │────▶│ ┌────────────▼────────────────────┐ │
│ sides → compare under        │     │ │ GNEISS LEDGER (via Gneiss.      │ │
│ declared criterion →         │     │ │ Facade, frozen v0): testimony,  │ │
│ re-runnable VerificationRun  │     │ │ decisions, contexts, why(),     │ │
└──────────────────────────────┘     │ │ labels, staleness receipts      │ │
┌──────────────────────────────┐     │ └─────────────────────────────────┘ │
│ AGENT TOOLS + kp CLI (K5)    │     │  PORT MONITOR v0 (K6): delta →     │
│ get-context · propose ·      │────▶│  impact cone → stale marks →       │
│ note · attach-evidence       │     │  bounded work items → review queue │
└──────────────────────────────┘     └─────────────────────────────────────┘
                       HUMAN VIEWS — the PORT ATLAS (K-V) + K7 semantics:
                       maps · correspondences · dossier · health time series ·
                       impact-cone replay · queue  (regenerated static HTML)
```

**Deliberately absent in v0:** overlay graphs beyond containment + resolved references (no call/dataflow/ownership graphs until a fixture needs one); TransformationRule replay machinery (K9 pilots the first, narrow class); bidirectional anything; IDE integration; any autonomy above implementer-on-a-branch; any *server-based* UI (the Atlas is regenerated static HTML — at this envelope, honest and diffable).

### 3.2 Design decisions for v0 (defeasible; each cites the envelope)

- **K-D1 — Substrate:** C#/.NET for KodePorter itself; SQLite for the map store, a separate file from the Gneiss ledger — the boundary between regenerable organ data and durable testimony is physical, not conventional.
- **K-D2 — Providers via SCIP first:** both rust-analyzer (`rust-analyzer scip`) and Sourcegraph's `scip-dotnet` emit the SCIP code-intelligence format, making one neutral, pinned, regenerable index the cartographer's input for both languages. This is a 2-day validation spike inside K2, with fallbacks pre-named (direct LSP queries; Roslyn walked natively — in-house RoslynSkills/`roscli` tooling is an accelerant here; rust-analyzer's LSIF export). A pinned **basis** = (repo, commit, toolchain version, analyzer version, index content hash).
- **K-D3 — Identity as testimony, not magic:** v0 entity identity = immutable coordinates (repo, commit, path, range, symbol path, content hash). **Semantic continuity across commits is recorded as continuity assertions** — claims with evidence (git rename detection, identical symbol path, identical content hash, agent judgment) — reviewable and defeasible like any claim. The map never silently pretends two snapshots contain "the same" entity; it asserts it, with grounds. This turns the charter's hardest open question (§21, stable identity) into normal Gneiss traffic and gives the kill criterion ("identity cannot survive ordinary evolution") a measurable form: continuity-assertion churn per benign commit.
- **K-D4 — Physical split of the three granularities** (charter §8): analyzer indexes are cached files, disposable; the map store is regenerable from pinned bases (a **regeneration drill** — delete, rebuild, byte-compare — is CI, mirroring Gneiss's rebuild drill); domain state exports to `.kodeporter/` as diffable, human-readable artifacts (K-A6); durable judgments live in the Gneiss ledger. Anything that cannot say which of the four it is does not get stored.
- **K-D5 — Dossier = markdown + structured front matter.** The human layer is prose readable on any Git host with no runtime; the front matter binds unit id, scope coordinates, policy references, claim ids, obligation ids. One file per migration unit; renames survive via unit id.
- **K-D6 — Correspondence types v0, maximum six:** `implements` (unit-level: target region realizes source unit), `maps-to` (entity-level structural), `adapts` (systematic transformation, with adaptation note), `diverges` (typed per amended `Divergence.kind` ∈ {adaptation, exception, intended, observed, unresolved}), `covers` (verification → claim), `continues` (identity continuity, K-D3). New types require a fixture demonstrating an inexpressible statement.
- **K-D7 — Equivalence criteria v0, exactly three:** `io-agreement` (differential agreement on a declared input corpus, with declared canonicalization), `api-shape` (signature-level compatibility under the mapping policy), `error-semantics` (declared error-class mapping, e.g. Rust `Err(ParseError::…)` ↔ C# exception types — which is itself an Adaptation, pleasingly). Each criterion is a versioned object with parameters; claims cite criterion + version.
- **K-D8 — The harness protocol is the narrow waist:** each side exposes a tiny CLI adapter (`stdin` JSONL cases → canonical JSON results); the harness runs both at pinned bases, compares under a criterion, and emits a **VerificationRun** = (harness version, corpus hash, both basis pins, criterion id+version, per-case results, verdict) — recorded as Gneiss testimony and re-runnable from its own record by one command (K-A3's mechanical re-derivability, implemented).
- **K-D9 — Proposal bundles and the anti-laundering rule:** an agent proposal = (code patch and/or map delta) + declared basis + instructions provenance + claimed evidence. Acceptance requires at least one evidence item **independent of the proposal's own generation** — a differential run against a source-derived corpus, a verified source citation, or human review. Generated tests alone are, by policy, insufficient for acceptance (charter §11's threat, operationalized). Agent-cited coordinates are mechanically verified to exist and match before a decision is offered. Acceptance itself is policy (K-A10): a declared PortPolicy may **auto-accept** when the mechanical criteria are green — independent evidence present, coordinates verified, obligations closed — which is the zero-human-minutes path; named classes (security-sensitive regions, intentional divergences, policy changes) may be routed to a human instead. The review queue receives only what policy routes to it, and sampled audit of auto-accepted claims is the standing honesty check. Neither KodePorter nor Gneiss imposes procedure; ceremony exists only where a policy put it.
- **K-D10 — Two-tier capture, shared with Gneiss (D-R10):** `kp note` appends zero-ceremony observations from any work session; triage promotes to dossier content or claims. Dossiers are updated *in the session that learned the thing* (K-A8's same-session rent), never as a separate documentation phase.
- **K-D11 — Impact cone v0 is deterministic and conservative:** changed files/symbols between bases → map edges (entity → unit → correspondence → claims, tests, obligations) → cone. Over-approximation accepted; **cone precision is measured** (cone size vs. actually-revalidated-and-changed) because "source-delta impact remains too noisy to guide action" is a chartered kill criterion and needs a number.
- **K-D12 — Port health v0 = six dimensions, no composite:** mapped / implemented / verified / stale / unknown / review-queue-health (K-A4). Roll-up functions are versioned policy documents; every rolled number drills to its obligations and evidence; "unknown" and "unimplemented" never merge (charter §12).

---

## 4. Epics

Focused-day guesses; R1 runs parallel and agent-heavy.

### K1 — Slice Zero fixture (jointly owned with Gneiss) — ~3–5 d
A deliberately tiny Rust crate with genuine semantic hazards: a header/config parser (echoing the charter's canonical example) with error cases, an ordering guarantee, one numeric-tolerance behavior, one platform difference. Plus: a C# target skeleton, a JSONL input corpus (valid, malformed, adversarial), a scripted source-delta sequence (a benign refactor, a behavior change, a signature change), and a ground-truth answer key (what corresponds, what diverges, what each delta affects). Lives at `fixtures/slice-zero/`; both projects' conformance suites cite it; ground-truth edits are escalations.
**Exit:** both sides build; corpus runs green on source; the delta script replays.

### K2 — Cartographer v0 — ~8–12 d (includes the SCIP spike)
Pinned structural import (containment, symbols, resolved references) for both languages into the map store; stable coordinates per K-D3; `kp map` CLI queries; the regeneration drill in CI.
**Explicit non-goals:** call/dataflow/ownership overlays; cross-language linking (that is correspondence work, K3, and it is *judgment*, not indexing).
**K2b — FrankenTui read-only probe (~2–3 d inside K2):** run the cartographer over FrankenTui and FrankenTui.NET at pinned commits. Pure reconnaissance: scale numbers, mess catalog (generated files, test topologies, dirty state), identity-scheme stress notes. No Gneiss dependency, no correspondence inference yet. This keeps the synthetic slice honest and feeds M4 planning early.
**Exit:** regeneration drill green; map queries answer the Slice Zero ground-truth structural questions; probe report filed.

### K-V — The Port Atlas (visualization from first light; steward requirement) — ~5–8 d initial, increments at every milestone
Static, self-contained HTML regenerated by `kp atlas`: the two structural maps side by side, correspondences drawn between them with their types and statuses visible; dossier rendering; the six-dimension health dashboard with **time series across bases** so the trend, not just the snapshot, is on screen; and — the marquee view — **impact-cone replay**: advance the basis and watch the affected region light up with its stale claims and generated work items listed. Dated snapshots accumulate as the longitudinal gallery; the Atlas *keeps track*, because there is no other way for the steward to judge value early enough. Golden-ledger playback renders a gate's acceptance run as a browsable story — the demo and the conformance run are the same bytes.
**Increments:** M1 — both maps + candidate links visible (Slice Zero, then the FrankenTui probe's real-scale map). M2 — dossier, decision trail, cone replay. M3 — health time series + queue economics. M4 — the bootstrap dossier and orientation-benchmark results, rendered as the showcase.
**Exit (initial):** a stranger opens one HTML file and understands what is mapped, what is claimed, and what is stale on Slice Zero, without running anything.

### K3 — Units, dossiers, correspondences, policy, and the ladder verbs — ~6–10 d
MigrationUnit selection (`kp unit new` over map regions); dossier per K-D5 with behavioral-contract section (K-A1: contract folds into dossier for now); correspondence types per K-D6; PortPolicy v0 + the three criteria objects; claim promotion through the frozen Gneiss facade per the [kb/37 §4.3](../Gneiss/kb/37-KODEPORTER-REALIZATION.md) rule; `.kodeporter/` export round-trip. Plus the KP-0 ladder verbs (K-A9): **`kp adopt`** — pin the repos, parse an existing `PORTING.md` into candidate units, policies, and claims *as testimony*, index transcripts as evidence — and **`kp export`** — emit a fresh, current `PORTING.md` from the workspace, so the floor is always reachable and leaving is always free.
**Exit = sub-gate S1 precondition:** Slice Zero's parser unit has a dossier, typed correspondences, one intended divergence, and promoted claims with precise evidence coordinates — all readable as plain files.

### K4 — Differential verification harness — ~6–10 d (the moat epic)
K-D8 in full: adapters for both Slice Zero sides; corpus tooling (seed from source tests/examples; property-based generation deferred); criterion evaluation (`io-agreement`, `error-semantics`); VerificationRun recording and one-command re-run; failure rendering that names the *claim* affected, not just the case.
Built as a separable artifact with its own README — it is the most exportable component in the programme and the seed of Exit D.
**Exit:** a `covers` correspondence links a green (and deliberately, once, a red) VerificationRun to the parser-behavior claim; re-run reproduces byte-identically at pinned bases; and every run emits an **automatic lab-notebook report** (inputs, criterion, per-case results, the exact command to reproduce) — reproducibility as a side effect, not a virtue.

### K5 — Proposal and decision workflow — ~4–6 d
Proposal bundles per K-D9; agent tools (`kp context <unit>` returning bounded dossier+policy+map slice, `kp propose`, `kp attach-evidence`, `kp note`); decision CLI with obligations check and the anti-laundering gate; provenance links (generated-by, instructed-by, basis); merge assist onto the target branch after acceptance.
**Exit = sub-gate S2:** an agent proposes a bounded implementation delta for Slice Zero; the steward rejects once (recorded, with reason) and accepts once; then the same flow runs **fully delegated** — a second proposal auto-accepted by policy on green mechanical evidence, zero human minutes, sampled-audit entry created. The dossier view shows both accepted states with `why()`, consumed-set label, and *which policy* accepted each.

### K6 — Delta, impact, staleness — ~5–8 d
Advance the source basis along the scripted sequence; symbol/file diff → K-D11 cone; stale marks flow to claims via Gneiss receipts and to map objects via edges; bounded work items generated; review queue with age/depth metrics (K-A4).
**Exit = sub-gate S3 (with E4's amnesia drill from the Gneiss side):** the behavior-change commit produces a cone that includes the affected unit and excludes the untouched one; a prior claim is corrected without erasure; old and new views both reproduce; the narrow-seal drill returns typed insufficiency outside its contract. Cone precision measured against ground truth.

### K7 — Port health semantics — ~3–5 d
Six-dimension health per K-D12 with drill-down; roll-up functions as versioned policy documents; traceability queries; CLI output for scripts. The Atlas (K-V) is the presentation surface — K7 supplies the labeled numbers it renders.
**Exit:** the charter §15 demo questions are answerable from the Atlas alone by a non-author; every health number carries its label and drills to its obligations.

### K8 — FrankenTui brownfield bootstrap — ~10–15 d (M4 core)
The charter §16 benchmark, staged: full cartography both sides (scaling K2); candidate correspondence inference as **hypotheses** (path/name/symbol/test-topology heuristics + agent proposals — all `proposed`, none silently accepted); depth classification on a sampled subset (faithful / adapted / scaffold / divergent / unknown) with evidence; existing status documents and the historical test baseline (2,815 passing / 134 failing headless tests) ingested as testimony to reconcile, not truth; bootstrap dossier with honest baselines and a ranked recovery plan. Strictly read-only throughout.
**Exit:** bootstrap dossier delivered; **orientation benchmark run (§7)**; economics data captured; a proposed "small, safe next porting wave" identified but not executed (that is post-G4 work).

### K9 — Continuous preservation pilot — ~8–12 d (starts only on a green G4 trajectory)
One real upstream FrankenTui delta (or the extended Slice Zero sequence if upstream is quiet): cone → deterministic replay for the narrow rule class that v0 supports (moved/renamed files, mechanical signature propagation) → bounded synthesis work items for the rest → verification re-runs → health update. Measures the charter's central promise: **source change becomes bounded work, not archaeology.**
**Exit:** steward review minutes per delta and cone precision published; the "preservation is cheaper than re-porting" claim gets its first real number.

### R1 — TypeScript→Go observational mining (parallel, agent-executed) — ~3–5 d cap
The framing §20.4 study, run against the public typescript-go repository and reports using the §21 extraction template: their de facto correspondence model, parity tracking, intentional-divergence handling, preservation workflow. Output: a nouns-and-practices report scored against the K-A1 candidate vocabulary — each candidate noun is confirmed (they needed it), renamed (they had a better word), or unsupported.
**Exit:** report in `docs/`; K-A1 confirmations queued for G3.

---

## 5. Milestones and gates (joint spine with Gneiss)

Per the FrankenSim showcase discipline, **each gate lands with a named marquee demo, a golden ledger (its replayable acceptance run), an Atlas snapshot, and a lab-notebook report** — selling the project is a first-class output artifact, and the demo and the conformance run are the same bytes. The Atlas adds roughly a week across M1–M2; the windows absorb it (they were guesses and remain guesses).

| Milestone | Target | KodePorter content | Gate |
|---|---|---|---|
| **M0 — Alignment** | wk 0–1 | Amendments K-A1…K-A8 decided; envelope §2 declared; K-D1…K-D12 locked; K1 seeded | **G0:** code starts |
| **M1 — First light** | wks 1–4 | K2 (with SCIP spike verdict and K2b probe); K-V Atlas v0; K3 begun; R1 launched. **Marquee: "See the port"** — both Slice Zero maps rendered with candidate links, and FrankenTui's real-scale map on the same screen | **G1 (joint):** Gneiss fold green **and** cartographer regeneration drill green; IDs stable across a no-op recommit; the Atlas renders both maps. *Kill watch:* SCIP fallback also failing → provider strategy rethink before proceeding |
| **M2 — Slice Zero** | wks 4–10 | K3, K4, K5, K6 → sub-gates **S1** (mapped & claimed), **S2** (proposed & decided), **S3** (changed & honest) — the charters' first shared deliverable, staged per K-A2. **Marquee: "Watch the cone light up"** — the source advances and the Atlas replays the impact, stale claims listed, work items born | **G2 (joint):** all three sub-gates green with kill signals evaluated per stage (see §8); the zero-human-minutes path demonstrated alongside the gated one |
| **M3 — Keep-earning** | wks 10–13 | K7; map-maintenance economics data into the joint keep-earning memo; R1 report; K-A1 vocabulary confirmations (attic promotions/retirements). **Marquee: "The queue pays rent"** — health and review-economics time series in the Atlas | **G3 (joint):** continue → M4; or Exit D (harness + dossier-lite + cartographer/Atlas + ladder verbs ship; superstructure shelved); or Exit K |
| **M4 — Reality test** | wks 13–20 | K8 (bootstrap + orientation benchmark), then K9 pilot. **Marquee: "The stranger orients"** — the benchmark results rendered in the Atlas beside the bootstrap dossier: the pitch and the measurement are the same page | **G4:** the value verdict. Usable trajectory → charter Phase continuation (second language pair, rule library growth, autonomy levels). Benchmark loss → Exit D with the published numbers as the post-mortem's spine |

---

## 6. Mapping to the charter's thirteen steps

For traceability: S1 = steps 1–6 (pin, import, unit, dossier, correspondences/strategy/questions/divergence, evidence incl. differential run) via K2+K3+K4. S2 = steps 7–9 (proposal, decision, labeled dossier/health views) via K5 + K7/K-V. S3 = steps 10–13 (source advance, cone and staleness, correction with history, reproduced views, amnesia drill) via K6 + Gneiss E3/E4. Nothing in the charter's list is dropped; only the gating is staged.

---

## 7. The orientation benchmark (K-A7's definition, adopted here)

The measurement that converts "durable memory that survives models, sessions, branches, and team changes" from slogan into evidence. Run at M4 on FrankenTui; dry-run at M2 on Slice Zero to debug the protocol.

- **Task set:** ~12 questions with ground truth from K8's dossier work — the charter §15 six (what are we porting; what must stay the same; why is this unit complete/incomplete; which source/rules/tests/decisions produced that answer; what went stale; what do we know we don't know) plus concrete location/status tasks ("where is behavior X implemented in the target, what verifies it, what diverges intentionally and why").
- **Arms:** same model, same tool access to repos, same budget caps. **A (baseline):** repos + existing docs, status documents, and transcripts. **B:** repos + the KodePorter workspace (map queries, dossiers, ledger `ask`/`why`) — with the baseline's materials also available, since the claim is *addition* of value.
- **Metrics:** correctness against ground truth (rubric-scored); wall-clock and tokens to answer; **unsupported-claim rate** (confident answers contradicted by ground truth — the measure of whether the map reduces confabulation, not just latency).
- **Bar for Exit U:** B materially better — as a working line, ≥1.5× faster at no worse correctness, or clearly higher correctness with fewer unsupported claims at similar cost. **Honesty rule:** results are published in the corpus regardless of outcome; if A wins, that headline opens the economics memo.

**Economics instrumentation (continuous, from M0):** every working session logs minutes on capture / promotion / review vs. an estimate of investigation avoided (calibrated by periodic re-investigation probes: re-ask an old question in a fresh session without the workspace and time it). Review-queue depth, age, decisions/week, and rubber-stamp sample audits per K-A4. **Human minutes per accepted claim has zero as its supported design point (K-A10):** under delegated policy the number *is* zero, and any imposed ceremony discovered in a flow is logged as a defect, not accepted as a cost — what is measured is value added (orientation speedup, avoided re-investigation), never compliance. The M3 memo and G4 verdict cite these numbers, not impressions.

---

## 8. Frailty signals (schedulable kill criteria)

| Signal | Where checked | Charter anchor |
|---|---|---|
| Continuity assertions churn on benign commits (identity fails ordinary evolution) | G1, S3, K8 | §20 "file and symbol identity cannot survive ordinary repository evolution" |
| Cone noise: stale marks ≫ truly affected; precision below a usable floor after tuning | S3, K9 | §20 "source-delta impact remains too noisy to guide action" |
| Map maintenance minutes exceed measured re-investigation savings, twice | M3, M4 memos | §20 kill #1 |
| Dossiers write-only (telemetry: agents/humans never *read* them during work) | M3 | §20 "documentation chores detached from executable work" |
| Harness cost explodes beyond pure-function units with no reuse path | K4, K8 | §11 feasibility of independent verification |
| Agents bypass the workspace because bounded context is too thin | K5 onward | §20 "bounded interfaces remove too much useful context" |
| Correspondence inference produces confidence theater (hypothesis spam overwhelming review) | K8 | §20; Gneiss kb/32 hypothesis-spam economics |
| Delegated acceptance rots: sampled audits find a rising wrong-acceptance rate (evidence gates too weak or the dial mis-set) | S2 onward, K8 | §11 test-laundering threat; K-A10's audit obligation |

Any signal surviving a week of honest redesign → G-gate deliberation with Exit D as the default landing, Exit K if the failure is foundational (identity, economics).

---

## 9. Coordination contract with Gneiss (mirror of its §9)

Shared Slice Zero fixture with escalation-only ground-truth edits; `Gneiss.Facade` v0 frozen from M2 entry to G3, changes via charter-debt entry + joint sign-off; the executable vocabulary mapping table (VerificationRun ↔ observation-with-method; Decision ↔ `decide`; KodePorter warrants {structural, differential, observational, reviewed, operationally-attested} ↔ domain predicates, never Gneiss grades); concept-promotion protocol (porting concepts enter Gneiss only via second-domain recurrence or invariant protection, decided at G3/G4); G2 and G3 are joint gates. Shared showcase discipline: both projects land each joint gate with named marquee demos, golden ledgers, and lab-notebook reports; the Atlas and the Lens snapshot the same gate so the two systems tell one story, and the accumulated gallery is the running pitch.

**Division of the first build's labor, stated to prevent the "week that hides a quarter":** Gneiss owns the cell (~2 focused weeks); KodePorter owns everything else in the slice (~5–8 focused weeks: providers, harness, workflow). Neither books its cost to the other.

---

## 10. Risks to this plan

- **Provider brittleness** (rust-analyzer/SCIP/scip-dotnet version drift, index gaps): mitigated by the pinned-basis discipline (analyzer version is part of the basis), the K2 spike with named fallbacks, and treating index gaps as typed coverage facts (`outside coverage`), not silent holes — the Gneiss vocabulary earning its keep at the infrastructure layer.
- **The harness eats the schedule:** K4 is scoped to pure-function units and one criterion pair; anything more waits for demonstrated need. If even that scope blows its budget ×2, that is a §8 signal, not a reason to push harder silently.
- **Brownfield shock at K8:** the K2b probe exists precisely to move this shock to week 3, when the model is still cheap to change.
- **Steward bandwidth:** the same constraint and the same instrumentation as Gneiss's plan; here it is doubly load-bearing because review economics *is* the product hypothesis. If the review queue drowns the steward on Slice Zero — a toy — the design has answered early, and cheaply.
- **Sunk-cost drift at G3/G4:** the exits are pre-declared with their salvage sets so descoping is an execution of the plan, not an admission against it.

---

## 10.5 Post-M1 replan (2026-07-11, steward-directed)

The M1 drive landed S1 plus the S2/S3 mechanics ahead of schedule (see
[showcase/m1/NOTEBOOK.md](../showcase/m1/NOTEBOOK.md)); this section re-points the next two
increments per the steward's positioning steer, extended 2026-07-11 to the full service
realization (charter §14): KodePorter is **two layers, one product** — the representation layer
(the map with the typed imperfections of the mapping itself) and the skills & guidance layer
(porting subtleties + agent-management knowledge, seeded by
[guidance/PLAYBOOK.md](guidance/PLAYBOOK.md)) — delivered as an installable kit + knowledge site,
with a flagship corpus gathering porting and agent-coding signal, everything managed in Gneiss at
three tiers (per-port, KodePorter meta-ledger, FireHorseCoding governance ledger), open source
(MIT).

### M1.5 — The imperfection vocabulary (next increment)

Extend the map schema so it can describe its own epistemic state, per layer, as small closed sets,
each rendered as a first-class visual state in the Atlas:

| Layer | Typed states to add |
|---|---|
| Cartography | per-entity resolution grade (clean / degraded-resolution / provider-gap) — the "39 diagnostics" become addressable facts |
| Identity | continuity-unknown (candidate same-entity links across bases, unconfirmed) |
| Correspondence | provenance grade: candidate (inferred, unreviewed) / asserted / verified; conflicting-correspondence detection; refinement links between coarse (unit) and fine (entity) correspondences |
| Understanding | mapped-but-thin vs dossiered (typed, not inferred from empty prose) |
| Absence | source-without-target: not-yet-ported / deliberately-dropped / unknown; target-only: intentional / unexplained |
| Evidence | independence typing on VerificationRuns (independently-derived vs implementation-coupled); corpus-coverage declaration; stale-basis |
| Staleness | measured cone precision carried in the advance report and health |

Also in scope: `kp note` (the two-tier capture verb, chartered K-A8, currently unwired), health
`unknown` filtered by kind/test-ness, Gneiss facade v0.1 (per-item aids from `Append`,
deterministic receipt ids, fetch-by-aid), the THE-PAGE findings annex for the grounded pairwise
conflict semantics and consumed-set closure (**ratified by the steward 2026-07-11**, with the
constitutional wording tightened to "unique, deterministic, monotone evaluation — no choice
points, no nonmonotone revision loops"; the annex records it), and **bootstrapping the
FireHorseCoding governance ledger** — the meta-meta tier: this redirection and the amendments log
become its first recorded decisions, with AMENDMENTS.md continuing as its human-readable export
(D28 made real).

Seals (E4) gain a product path from the one-shot decision: a completed one-shot port **seals its
map** under a declared query contract — the deliverable is the port plus its receipt, and
reopening the seal upgrades to a tracked port. E4 remains scheduled at M2/S3 and now has a
customer.

### M2′ — FrankenTui: probe, then the iterative learning loop (steward-directed shape)

Both repositories are local (`C:\Work\FrankenTui.Net`; Rust upstream vendored at
`.external/frankentui`). Sequence:

**Probe (read-only, K2b):** cartography of both sides at pinned commits; scale and mess report;
imperfection-vocabulary stress test (a brownfield map is *mostly* imperfection states — the
fixture flattered us by being clean).

**Bootstrap:** candidate correspondences inferred by low-cost agents (entering as `candidate`
grade, never silently asserted), depth classification on samples, absence typing, honest test
baseline (inherited failures separated from regressions).

**The learning loop** — iterate, with the map as both memory and fitness function:

1. **Direct:** standing queries over the map select bounded work items — the stale queue, the
   candidate-review queue, the thin queue, the unverified queue, the absence-unknown queue.
2. **Work:** very low-cost agents execute bounded items (verify a candidate link, deepen a
   dossier, port a gap, repair a test) with `kp context`-style bounded input.
3. **Check:** *independent* low-cost checkers verify — evidence recorded with its independence
   type; nothing implementation-coupled can satisfy an acceptance gate alone.
4. **Record:** everything flows through kp into the ledger — proposals, decisions (policy
   auto-accept by default, sampled audit), stale marks. No knowledge lives in prompts.
5. **Measure:** per-iteration health snapshot + test-baseline delta + Atlas snapshot into the
   longitudinal gallery. An iteration *improves* if verified coverage rises without new
   regressions or stale debt; it *regresses* if the deltas go negative. The gallery becomes the
   loop's fitness chart.
6. **Learn:** each work item records its **method** (agent tier, prompt template, direction
   policy) in the Gneiss method envelope; acceptance, rework, and regression rates accumulate
   per method as ordinary ledger data. Direction policy prefers methods with earned skill and
   prunes ones that regress — "which iterations improve" is a query, not an impression.
   *(This is the lessons-become-schema rule applied to the loop itself: the learning lives in
   the map's method-skill records, not in orchestrator memory.)*

**Rails:** upstream stays read-only; target work on branches only; no destructive git; inherited
failures never count as regressions; economics instrumentation (tokens per accepted verified
delta, human minutes — expected ~0 — rework rate) wired before the loop starts, so the M3
keep-earning memo cites numbers.

**Kill-watch (live numbers, per chartered criteria):** cone precision at brownfield scale;
candidate-queue depth vs review capacity (confidence-theater tripwire); map-maintenance cost vs
re-investigation saved.

**After FrankenTui: the flagship corpus track.** Per the steward's corpus posture — many
flagships, some shallow and some deep, increasing variety over time, many maintained deeply — the
corpus is the service's signal-gathering instrument (porting subtleties AND agent-coding signal
in the low-cost/high-compliance sense). Each flagship feeds the KodePorter meta-ledger
(method-skill records accumulate ACROSS flagships, so guidance earns its versions from evidence).
"Published as consumable artifacts" is explicitly deferred until there is a real decision to
make.

## 11. Charter amendment dependencies

Assumes K-A1 (else the schema work for the full §7 vocabulary adds ~2–3 weeks and teaches less), K-A2 (else M2 is a single all-or-nothing gate), K-A3 (the anti-laundering gate is load-bearing in K5), K-A6 (in-repo artifacts are K-D4/K-D5), K-A7 (the benchmark definition above), K-A8 (same-session capture as design constraint), K-A9 (the representation ladder is §3.0; `kp adopt`/`kp export` are v0 verbs in K3), and K-A10 (the autonomy dial is K-D9/K5; zero human minutes is a supported operating point, not an aspiration). If K-A5 (investigated-absence) is declined, S3's cone loses closure invalidation and the roadmap's staleness story weakens accordingly — not recommended.
