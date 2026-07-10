# Gneiss Roadmap v0

**Status:** First development roadmap (2026-07-10 check-in; revised same day after steward comments — the Lens epic, the autonomy dial, the showcase discipline). Companion to [CHARTER.md](CHARTER.md); paired with [KodePorter/ROADMAP.md](../KodePorter/ROADMAP.md); assumes the amendments proposed in [CHARTER-REVIEW.md](../CHARTER-REVIEW.md) (G-A1…G-A9).
**Ambition tags** (adopted from FrankenSim per kb/36 steal #1): everything here is **[S]** unless marked; the E8 spikes are **[F]**; nothing **[M]** sits on the critical path. Demoted vocabulary and deferred mechanisms live in the **attic register** (`kb/ATTIC.md`, to be created at M0) with rationale and promotion triggers — parked, not deleted.
**Discipline:** every technology named here is a falsification convenience under [kb/03-NOTATION.md](kb/03-NOTATION.md), not a platform decision. Every effort figure is a guess and labeled as one. Every gate has a kill or descope branch that is a legitimate outcome, not a failure to be ashamed of.
**Standing policy adopted from the review:** from this document forward, a document of substance must either be executable (spec with oracle, fixture, drill) or record the reasoning of a decision that gated an artifact. This roadmap gates artifacts; the next substantial Gneiss prose should be written by someone who has run the cell.

---

## 1. Destination: what "usable or falsified" means

This roadmap ends at one of three exits, targeted within roughly 20 part-time weeks:

**Exit U (usable):** the reference cell runs; KodePorter's Slice Zero (all three sub-gates S1–S3) runs on it end to end; the conformance kit passes; the witness-stand questions are answerable against a live ledger by someone who did not build it; and the **keep-earning memo** concludes honestly that the cell provides things claim-keyed native tables would not have, at a complexity cost the operator willingly re-pays. Gneiss is then "clearly on the way to becoming usable": Phase 3 (second domain) and Phase 4 (hardening) proceed per the charter.

**Exit P (pattern book):** the cell runs but the keep-earning memo concludes A1-native patterns deliver most of the value at a fraction of the cost. Gneiss ships as discipline + schema patterns + the witness-stand test; KodePorter continues on native tables shaped by that discipline. Per [kb/32-RISKS.md](kb/32-RISKS.md) §4.3 this outcome is celebrated as a finding.

**Exit K (kill):** one of the frailty signals in §8 fires and survives a week of honest attempts to design around it. The post-mortem enters `kb/` and the ideas remain available to future work.

The roadmap's job is to force one of these exits — not to remain, indefinitely and comfortably, "pre-implementation."

---

## 2. Declared operating envelope for v0 (per charter §13; amendment G-A2)

All v0 design decisions cite this envelope. It is versioned data; drift beyond it defeats the choices that cite it.

| Parameter | v0 declaration |
|---|---|
| Ledgers | one per project; single writer (file-lock discipline); no federation |
| Write rate | human-cadence decisions (tens/day); agent-rate proposals and notes (hundreds/day, thousands/day burst) |
| Ledger size | ≤ ~10⁶ assertions before any archival pressure is entertained |
| Read latency | `ask` over ≤ ~10⁵ visible assertions completes at interactive latency (≤ ~2 s) via **full L0 recompute**; no incremental machinery until this is measured broken |
| Contexts | ≤ ~10 named contexts in live use |
| Recursion | meta-assertion depth ≤ 3 in practice (monitored as a smell per [kb/20-KERNEL.md](kb/20-KERNEL.md) §4) |
| Durability | single-node SQLite-class; backup = file copy; corruption drill deferred to Phase 4 |
| Retention | no purge pressure expected in v0 except the deliberate amnesia drill |
| Consumers | one human operator + agent fleet; authority is a policy dial (G-A9) — fully-delegated operation (agents decide under declared policy, zero human minutes) and human-gated classes are both v0 postures, exercised side by side in the fixtures |

---

## 3. Architecture for the first product cycle

Five artifacts, in dependency order. The spec is the property; the engine is an existence proof (protocol posture, D29).

```
┌────────────────────────────────────────────────────────────────────┐
│  THE-PAGE v0 (frozen spec)          kb/maxwell/THE-PAGE-v0.md      │
│  6 relations · verbs · R1–R15 · LABEL ≝ consumed set · ADQ box     │
└──────────────┬─────────────────────────────────────────────────────┘
               │ implements (divergences recorded as findings)
┌──────────────▼─────────────────────────────────────────────────────┐
│  REFERENCE CELL (Gneiss.Cell)                                      │
│  ┌─────────────┐  ┌──────────────┐  ┌─────────────┐  ┌──────────┐  │
│  │ Ledger      │  │ Belief fold  │  │ Label/why   │  │ Coverage │  │
│  │ append path │→ │ R1–R10 (+I6) │→ │ consumed    │  │ + narrow │  │
│  │ (SQLite,    │  │ strainers v0 │  │ sets,       │  │ seal,    │  │
│  │ INSERT-only)│  │ pure, L0     │  │ receipts    │  │ R11–R14  │  │
│  └─────────────┘  └──────────────┘  └─────────────┘  └──────────┘  │
└──────┬──────────────────────┬──────────────────────────────────────┘
       │ in-proc facade       │ CLI (JSON in/out)
┌──────▼──────────┐   ┌───────▼────────┐   ┌───────────────────────┐
│ Gneiss.Facade   │   │  gn  CLI       │   │ CONFORMANCE KIT       │
│ (what KodePorter│   │ record decide  │   │ golden corpus fixtures│
│ links against)  │   │ declare ask    │◄──│ + drills (rebuild,    │
│ ≤ ~20 pub types │   │ why note seal  │   │ replay, amnesia,      │
└─────────────────┘   │ purge drill    │   │ determinism)          │
                      └────────────────┘   │ speaks CLI protocol → │
                                           │ any implementation    │
                                           └───────────────────────┘
```

**What is deliberately absent:** incremental maintenance beyond L0; `sprout/commit`; `import`/federation; any *server-based* UI (the Lens below is regenerated static HTML); a query DSL (named contexts + facade calls are the query surface — [kb/32-RISKS.md](kb/32-RISKS.md) inner-platform tripwire); general seals (only the narrow declared-family seal exists).

**The sixth artifact — the Lens (added on steward direction; there is no other way to judge value early enough).** A static, self-contained HTML rendering over the ledger — transaction timeline, belief view per context, `why()` trees, label/receipt inspection, and diffs between two (context, time) coordinates — regenerated by `gn lens` after any append. Dated snapshots accumulate in the workspace as a longitudinal gallery, so the visualization *keeps track*: progress and regress stay visible gate over gate. No server, no framework — at this envelope, regenerated static pages are the honest form, and they are diffable and committable like everything else. The Lens is a first-class deliverable from M1, not presentation polish; it is also where the FrankenSim showcase discipline lands, since a Lens page over a golden ledger *is* the demo.

### 3.1 Design decisions for v0 (defeasible; each cites the envelope)

- **D-R1 — Substrate: C# / .NET, SQLite.** In-house depth (the KodePorter C# side needs Roslyn anyway), agents fluent in it, one-file store fits the single-writer envelope. Expires per kb/03: the conformance kit, not this binding, is the durable artifact.
- **D-R2 — Append-only enforced structurally:** the storage layer exposes INSERT-only operations; SQLite triggers reject UPDATE/DELETE on base relations as defense in depth. Redaction (the sole exception, charter §13) is deferred to Phase 4 with its protocol already specified in [kb/23-STORAGE.md](kb/23-STORAGE.md).
- **D-R3 — Determinism discipline from the first commit:** canonical serialization (UTF-8 NFC, invariant culture, canonical JSON field order), `decimal`/integer arithmetic wherever values participate in comparisons or tiebreaks (no binary floating point on decision paths), SHA-256 for content hashes, and a fixed total tiebreak ending in TxId per THE-PAGE §(d). T1's caveat is handled by construction, not by later cleanup.
- **D-R4 — Value types and strainers v0 (containing "the hidden 40%"):** typed scalars {string, bool, int64, decimal, entity-ref, content-hash, half-open valid interval} plus the verbatim source string (OMOP pattern). Strainers v0, each a tiny versioned pure function cited by `RuleVer`: exact equality; numeric tolerance (absolute + relative); case/whitespace-normalized string equality; interval clipping for supersession. **Nothing else enters v0.** New strainers require a fixture demonstrating need — this line is where EAV/comparability sprawl is stopped.
- **D-R5 — L0 is the only engine.** Every `ask` is a full deterministic recompute; receipts make later incrementality honest (T2) but no L1/L2 code is written until the envelope's latency line is *measured* broken by a real fixture.
- **D-R6 — Claim keys are registered, not improvised:** `CKey = hash(subject-key, predicate, valid-slice)` with a per-predicate declaration of how subject keys are formed (the Senzing/P1 lesson: the overlay is only as durable as its key). The registry is itself declared assertions.
- **D-R7 — Contexts per THE-PAGE with the kb/50 flag honored:** `ctx` is a materialized view over `declare`d assertions, not a seventh base relation; `bootCtx` is fixed in code, tiny, non-self-referencing; `DefCut` pins policy versions to a strictly earlier prefix.
- **D-R8 — The narrow seal is a contract object:** a seal declares an enumerated query family (predicate × subject-scope × context-family), carries the accepted-value frontier plus the defeat record for that family (the ADQ checklist as code), and everything outside the family answers with typed insufficiency. General seal adequacy remains the charter's named open obligation; v0 does not attempt it.
- **D-R9 — No `sprout/commit`, no `import` (G-A1).** Agent what-ifs are `proposed` assertions plus decisions. Worlds return when a domain demonstrates need.
- **D-R10 — Two-tier capture (review idea #1):** a `note` verb appends zero-ceremony candidate testimony to an inbox stance; triage promotes selected notes to claims with proper envelopes. Capture friction is spent at review time, not at discovery time.
- **D-R11 — Authority is a dial, declared as data, with zero human minutes supported from day one (G-A9):** actors are entities; declared policies say who or what may decide which claim classes. v0 ships both postures and the fixtures exercise both: **delegated** — an agent or standing policy accepts when mechanical evidence criteria are met, no human in the loop, with sampled audit as the honesty check — and **gated** — policy routes named classes to a human. Neither Gneiss nor its domains imposes procedure; ceremony exists only where a policy put it, and any mandatory human step discovered in a flow is logged as a defect. The fuller delegation lattice ([kb/26-DECIDERS.md](kb/26-DECIDERS.md)) arrives with the second decider *type*; the dial itself is v0.

---

## 4. Epics

Effort figures are focused-day guesses for one experienced developer directing agents; part-time calendar mapping in §5.

### E1 — Ledger and belief fold (the cell's heart) — ~8–12 d
Schema for the six relations; write path with envelope and I6 enforcement (decisions target strictly earlier transactions); R1–R10 as the order-fixed left fold; strainers v0; three-context evaluation.
**First fixtures:** the wrong-silo story ([kb/22-BELIEF-ENGINE.md](kb/22-BELIEF-ENGINE.md) §7) verbatim under `AuditAsOf` / `CurrentOperational` / `Backtest`; property tests recycled from [kb/31-PROTOTYPES.md](kb/31-PROTOTYPES.md) P0 (determinism/referential transparency; ledger-monotone-belief-nonmonotone; decision survival across hypothesis wipe-and-regenerate via claim keys; cutoff coherence; I6 rejection at append).
**Exit:** fixtures byte-stable across two machines; the fold is demonstrably a fold.
**Kill signal (charter §16):** any wanted policy forces iteration-to-fixpoint or search inside evaluation — the stratification story is wrong; stop and reconsider before writing more code.

### E2 — Labels and `why()` — ~4–6 d
Thread the consumed set through every accept/defeat/missingness branch (THE-PAGE's day-4 warning: mechanical but pervasive); LABEL per §(e) (context version, high-water TxId, consumed tuples ∪ coverage regions, grade); `why()` as the justification walk rendered as a tree; replay-under-label.
**Exit:** every `ask` carries a label; replay under a stored label is byte-identical; `why()` reaches testimony, decisions, rule versions in ≤ 2 gestures (presentation conformance, [kb/34-PRESENTATION.md](kb/34-PRESENTATION.md)).

### E2b — The Lens (visual ledger, from first light) — ~4–6 d initial, increments at every milestone
`gn lens` renders the workspace to static HTML: the transaction timeline; the belief view per named context with accepted / defeated / contested / typed-missing states visibly distinct; `why()` as a collapsible tree; the label inspector (what did this answer consume); and the diff view between any two (context, time) coordinates. Every generation is dated and kept — the longitudinal gallery. A golden-ledger playback mode renders a stored acceptance run as a browsable story, which makes each gate's demo and its conformance evidence the same bytes.
**Exit (initial):** the steward answers the seven witness-stand questions on the wrong-silo fixture *by eye*, in the Lens, without the CLI. Each later milestone adds its marquee view (E3's correction diff, E4's amnesia degradation, E5's Gneiss-on-Gneiss history).

### E3 — Correction, supersession, staleness — ~4–6 d
Decision kinds (retract/supersede/accept/reject/invalidate-source); interval clipping; correction-without-erasure; the receipt-diff staleness check (which stored answers consumed tuples that later transactions changed — the L2-lite cone, computed from labels, no incremental engine).
**Exit fixture:** correct one earlier claim; old view reproducible under its label; new view differs with reasons; `stale(answer)` lists exactly the affected receipts.

### E4 — Coverage, narrow seal, amnesia drill — ~5–7 d
Coverage map states; `seal` with the D-R8 contract checklist; `purge` only under seal (else honest `lost`); grades (grounded/sealed) via R11–R13 including `not superseded_after_seal`; `absent_closed` under declared closure (R14/I8); the amnesia drill harness (randomized seal-and-purge inside the contract → assert T3: grades only descend, no value flips, closure retreats where coverage fell).
**Exit:** drill green over the fixture corpus; outside-contract queries return typed insufficiency, never invented answers.
**Note:** the two standing THE-PAGE flags (G-A8) are discharged here — `ctx` as view, and an explicit test that R13/R14's negations stratify through tx order.

### E5 — Conformance kit and golden corpus — ~5–7 d
Fixtures as data files + a runner that speaks only the CLI/JSON protocol (so a second implementation can conform without sharing code); drills packaged: **rebuild** (drop projections, regenerate), **replay** (byte-match under stored labels), **amnesia** (E4), **determinism** (two evaluators / two machines). Golden corpus v0 = charter §14.5 minus what-if and federation (moved to Phase 4 per G-A1): competing claims under contexts; correction without erasure; definition-time vs data-time change; typed missingness and coverage; agent proposal vs accepted decision; consumed-set labels and why; adequate and inadequate seals; full-recompute self-equivalence.
**Optional, cheap, recommended:** load the corpus's own decision history (D1–D35 with supersessions and corrections) as a non-synthetic fixture — Gneiss-on-Gneiss as data, not as a project.
**Exit:** kit runs green against the reference cell from a clean clone in one command.

### E6 — Embedding surface for KodePorter — ~4–6 d
`Gneiss.Facade` (record/decide/declare/ask/why/note/seal/purge; ≤ ~20 public types — the [kb/32](kb/32-RISKS.md) solo-maintainer tripwire made a build check); `gn` CLI over it with JSON I/O; single-writer file locking; error taxonomy (append rejected / context unknown / outside seal contract / etc. as typed results, not exceptions-as-strings).
**Exit:** KodePorter S1 (see its roadmap §6) records and asks through the facade without touching cell internals; facade v0 is then **frozen until M3** (coordination contract, §9).

### E7 — External probes (the generality check) — ~5–7 d
The P-1 retrofit memos, restored per G-A6: map Smoothscrape's link/overlay tables and one AIMS config/reading slice onto the v0 vocabulary on paper; record where the mapping needs contortions (each contortion is a kernel finding). Select the Phase-3 second domain from the memos' evidence. Draft (not necessarily publish — D10 is Govert's) the witness-stand essay as the A0 adoption probe.
**Exit:** two memos with pass/contort verdicts; a dated Phase-3 domain decision; essay draft in `kb/`.

### E8 — Hardening spikes (gated on G3 = continue) — ~8–12 d, each timeboxed
(a) **Second implementation from the page** (review idea #6): a fresh agent, given THE-PAGE and the conformance kit only — no corpus, no reference source — builds a cell; divergences are spec bugs; page v0→v1 amendments queued with the formative-phase ceremony. 3–5 d cap.
(b) **Mechanization micro-spike** (D22, shrunk): machine-check T1 (determinism) and the R13/R14 stratification lemma only; ADQ explicitly out of scope beyond restating the obligation. 5 d cap, findings-only deliverable.
(c) **Alternate binding note:** a two-day Marten-or-Postgres sketch *only if* M4 scale pressure (FrankenTui) demands it; otherwise skipped.

---

## 5. Milestones and gates (joint spine with KodePorter)

Calendar figures assume part-time attention (~2–3 focused days/week) with agents amplifying fixture-writing, test scaffolding, and spikes; the steward's review bandwidth is the honest constraint and is itself measured (§7). Per the FrankenSim showcase discipline, **each gate lands with a named marquee demo, a golden ledger (the replayable acceptance run behind the demo), a Lens snapshot, and an auto-generated lab-notebook report** — selling the project is a first-class output, and the demo and the conformance run are the same bytes. The Lens adds roughly a week across M1–M2; the windows below absorb it (they were guesses and remain guesses).

| Milestone | Target | Content (Gneiss side) | Gate |
|---|---|---|---|
| **M0 — Alignment** | week 0–1 | Amendments G-A1…G-A8 accepted/rejected; envelope declared as data; D-R1…D-R11 locked; joint **Slice Zero** fixture repo seeded (`fixtures/slice-zero/`, owned jointly — see KodePorter K1) | **G0:** code starts. The only failure mode is another document. |
| **M1 — First light** | weeks 1–4 | E1 + E2 + E2b (Lens v0); run THE-PAGE's own 7-day build plan honestly and record where it lied (the Kay bet, executed: page frozen, divergences are findings). **Marquee: "The wrong silo, witnessed"** — the kb/22 story live in the Lens: three contexts, three legitimate answers, `why()` on screen | **G1:** fold determinism cross-machine; claim-key survival demo; no search smuggled into evaluation; the Lens renders the fixtures. *Kill branch:* E1's kill signal fires → Exit K deliberation. |
| **M2 — Slice Zero** | weeks 4–10 | E3 + E4 + E6; KodePorter runs S1→S2→S3 on the cell — the charters' **first shared deliverable**. **Marquee: "Correct without erasing"** — a correction lands, the old view still replays, the amnesia drill degrades honestly, all watched in the Lens | **G2:** all three sub-gates green; correction and staleness stories demonstrable by a non-author following the CLI help alone. *Kill signals:* labels too expensive/imprecise to support staleness and explanation; domain team (i.e., KodePorter work) routinely bypasses decisions — both from charter §16. |
| **M3 — Keep-earning** | weeks 10–13 | E5 + E7; **the keep-earning memo** (G-A7): honest comparison against a hand-rolled A1-native alternative for the same slice (what did contexts, labels, uniform corrections buy; at what cost); Gray-log review; witness-stand essay decision. **Marquee: "The ledger reads itself"** — the corpus's own decision history (D1–D35) loaded and browsable in the Lens | **G3:** continue → M4; **or Exit P** (pattern book — celebrated); **or Exit K**. This is the roadmap's center of gravity. |
| **M4 — Reality test** | weeks 13–20 | E8 spikes; support KodePorter M4 (FrankenTui bootstrap — the first real scale pressure: only claims enter the ledger while ~10⁵–10⁶ map entities stay in the organ store; measure that boundary holding); begin Phase-3 second-domain slice per E7 selection. **Marquee: "A stranger rebuilds it"** — the second implementation's conformance diff, rendered | **G4:** protocol posture review — freeze page v1 + conformance kit v1; decide library extraction (A2 packaging), publication posture (D10), and Phase-4 scope. |

**Sequencing note:** nothing in M1–M2 waits on KodePorter's cartographer; nothing in KodePorter's K1–K2 waits on the cell. The projects genuinely converge only at S1 (their step 6 needs `record`/`ask`), which is why E6's facade freeze is scheduled before it.

---

## 6. Verification of the roadmap itself

The charter's success and kill criteria become *scheduled observations*, not vibes:

| Charter criterion (§16) | Where it is measured |
|---|---|
| "stale conclusions are detected from consumed dependencies" | E3 exit fixture; S3 |
| "policy changes vs evidence changes produce distinguishable view changes" | golden corpus fixture (definition-time vs data-time), E5 |
| "archival loss weakens answers honestly rather than changing them silently" | amnesia drill, E4, run in CI forever |
| "users can inspect why the answer exists" | non-author walkthrough at G2 (explain-to-non-author drill: a person or naive agent not involved in the build interprets a labeled answer + `why()` correctly in one sitting) |
| "reference evaluator remains small" | line-count + public-type budget checks in CI (kb/32 tripwire) |
| "alternative implementations conform" | E8(a) at M4 |
| "domain applications become simpler because they stop hand-rolling provenance" | the keep-earning memo, M3 |
| "ordinary audited database patterns solve the target cases more simply" | the same memo's A1-native comparison arm |

---

## 7. Metrics and economics instrumentation (from day one)

Cheap, honest, and append-only themselves — most of these are one-line log entries in the working journal, promoted weekly:

- **Human minutes per accepted claim / decision** (the review's economics-bear number 1, as corrected): **zero is a valid, supported operating point** — under delegated policy the expected number *is* zero, and any mandatory human step discovered in a flow is logged as a defect, not accepted as a cost. Where policy deliberately routes decisions to the steward: single-digit minutes each, with three-band triage and statutes proposed the moment it trends worse. The measure is value added, never compliance.
- **The visual record:** dated Lens snapshots per milestone — the longitudinal gallery is how early value (or its absence) is judged by eye, per steward direction. A gallery that stops changing is itself a signal.
- **Capture cost:** median seconds from "thing learned" to "note recorded" (D-R10 makes the mechanism cheap; this measures whether it is *used*).
- **Queue health:** inbox depth × age; rubber-stamp rate via random sample re-review (kb/32 decision-fatigue mitigation).
- **`ask` latency and ledger size** against the envelope table; first breach triggers the L1 conversation, not before.
- **Re-investigation probes** (jointly with KodePorter): periodically re-ask a previously answered question in a fresh agent session without ledger access; the time delta is the value side of the keep-earning ledger.
- **Charter-debt register entries per week** (G-A3): a rising count is fine early and damning late.

---

## 8. Frailty signals (schedulable kill criteria)

Each is checked at the named gate; any of them surviving one honest week of redesign effort triggers Exit K deliberation:

1. **Evaluation needs search** (E1/G1) — stratification story wrong at the root.
2. **Claim keys churn in practice** (G2; also KodePorter S1) — decision survival was the founding use case; without it the substrate is sand.
3. **Label cost or imprecision defeats staleness/explanation** (G2) — the receipts are the product; if they can't be afforded, nothing above them stands.
4. **The strainer line cannot hold** (any gate) — if v0's four strainers keep proving insufficient for the *slice's own* needs, the "belief calculus, not value calculus" boundary is leaking and the hidden 40% is actually the product.
5. **Correction/`why` unintelligible to a non-author** (G2) — charter kill: users must not need ledger internals.
6. **The keep-earning memo fails** (G3) — Exit P, by design.
7. **KodePorter work bypasses the decision workflow** (G2–M4) — ceremony without use is fiction; either the capture path is too expensive (fix D-R10 ergonomics) or the value is absent (Exit P/K).

---

## 9. Coordination contract with KodePorter

1. **Shared fixture:** `fixtures/slice-zero/` (tiny Rust crate + C# port + scripted delta sequence + ground-truth answer key) is jointly owned; both conformance suites reference it; disagreements about its ground truth are escalations, not local edits.
2. **Interface freeze:** `Gneiss.Facade` v0 freezes at E6 exit (M2 entry) until G3; KodePorter builds against the frozen surface; changes before G3 require a charter-debt entry plus joint sign-off.
3. **Vocabulary binding:** a two-column mapping table (KodePorter noun ↔ Gneiss stance/verb: VerificationRun ↔ observation-with-method; Decision/Review ↔ `decide`; warrant type ↔ domain predicate, never grade) lives beside the fixtures and is executable where possible (fixture assertions use both vocabularies).
4. **Concept promotion protocol** (charter §10.1 made procedural): anything KodePorter wants added to the kernel enters as a KodePorter-local stance first; promotion to Gneiss requires the second-domain recurrence or an invariant-protection argument, decided at G3/G4 with the formative-phase ceremony.
5. **Joint gates:** G2 and G3 are joint go/no-go meetings for both projects; each project's kill/descope is decided in view of the other's evidence.
6. **Shared showcase discipline:** both projects land each joint gate with named marquee demos, golden ledgers, and lab-notebook reports; the Lens and the Atlas snapshot the same gate so the two systems tell one story, and the gallery of snapshots is the running pitch.

---

## 10. Risks to this plan (not to the concept)

- **Solo review bandwidth is the true schedule.** Mitigation: agents author fixtures, tests, spikes, and drafts; the steward's irreducible work is decisions — which is precisely the workload the metrics in §7 watch. If steward-minutes explode, that is not just schedule risk; it is *evidence about the design* (see frailty signal 7).
- **The synthetic slice flatters the model.** Slice Zero is built by the same minds that built the charters. Mitigation: the FrankenTui read-only cartography probe (KodePorter K2b) runs in parallel and feeds real mess into fixture design; E7's P-1 memos test vocabulary against systems that predate Gneiss.
- **Scope creep re-enters through the strainers or the facade.** Mitigations are structural: the D-R4 fixture-required rule; the facade type budget in CI.
- **The week test is failed silently.** THE-PAGE's day-by-day plan is run *as written* at M1 and its misses are recorded as findings — the bet is only worth placing if losing it teaches in public.
- **Another season of documents.** The standing policy at the top of this file, plus G0's framing: the only way to fail M0 is to write more prose.

---

## 11. Charter amendment dependencies

This roadmap assumes G-A1 (verb spine), G-A2 (envelope), G-A3 (formative ceremony + debt register), G-A6 (P-track disposition), G-A7 (keep-earning memo), and G-A9 (zero-mandatory-ceremony parity — implemented by D-R11) are accepted. If G-A1 is declined, add a `sprout/commit` epic (~5–8 d) between E4 and E5 and extend M2 by two weeks. If G-A7 is declined, G3 loses its instrument and this roadmap's center of gravity moves to M4 — not recommended: it would defer the honesty check past most of the spend.
