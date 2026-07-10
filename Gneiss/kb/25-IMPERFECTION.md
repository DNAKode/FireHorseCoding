# The Impure Ledger: Forgetting, Faults, and Graceful Degradation

*Added 2026-07-04 from discussion. The objection (Govert): every system that survives five years wants to archive and delete "irrelevant" history — and distributed reality (eventual consistency, bugs, imperfect networks and storage) means the ledger itself will be imperfect. If the semantics demand a perfect, complete, totally-ordered ledger, any violation collapses coherence the way one inconsistent proposition collapses a classical theory. This document keeps the ideals while making the ledger's own imperfections first-class citizens of the model.*

## 1. The move: epistemic self-inclusion

Gneiss's founding discipline is refusing to confuse evidence with truth: the world is partially known, claims carry sources, absence is typed, belief is revisable. The purity contract `view = f(ledger[0..t], context)` quietly violated that discipline at the meta-level: it treated *the record* as perfectly known even while treating the *world* as uncertain.

The fix is the same move, one level up:

> **The ledger is not the complete history. It is evidence about history — including evidence about its own gaps, summaries, and injuries.**

The ledger stops being an axiom and becomes a witness whose wounds are themselves testified. Because the kernel is closed under self-description (assertions about assertions, [20-KERNEL.md](20-KERNEL.md) §4), this costs no new machinery: coverage claims, seals, and epoch records are ordinary assertions. The ideal contract survives as the *limit case* — full coverage — the way ideal gases survive inside real thermodynamics.

## 2. Coverage: the ledger's map of itself

Every ledger maintains a **coverage map**: assertions whose subject is a region (transaction range × scope, kept deliberately coarse — region-level, never per-assertion, or this becomes the metadata creep we swore off) and whose value is a coverage state:

| State | Meaning |
|---|---|
| `full` | raw evidence present and integrity-checked |
| `sealed(H)` | region summarized into certified checkpoint H; raw evidence may or may not still exist |
| `archived(uri)` | raw evidence moved offline; retrievable with effort |
| `purged` | raw evidence destroyed *after* sealing (the legal path for bulk forgetting) |
| `lost` | evidence destroyed or never captured, **without** a seal (restores, crashes, bugs) — recorded ignorance |
| `suspect` | integrity check failed or a defect decision covers this region; treated as contested |

The knowledge horizon ([21-TIME.md](21-TIME.md)) is revealed as a special case: "coverage before tx T is `lost` for as-known-then purposes." Closure declarations ([24-CONTEXTS.md](24-CONTEXTS.md) §4) get a companion rule that falls straight out:

> **Invariant I8 (proposed): negative conclusions require positive coverage.** `absent_closed` may be derived only where coverage is `full` (or `sealed` and the seal explicitly certifies the closure). Forgetting can widen "unknown"; it can never manufacture a confident "no".

## 3. Forgetting with ceremony: seal → purge → record

Accounting solved graceful amnesia centuries ago: close the period, carry forward opening balances, archive the detail, destroy boxes per retention schedule — keeping summaries and audit certificates forever. The Gneiss equivalent:

1. **Seal.** A checkpoint is computed over the region (per belief-relevant scope): period aggregates, final accepted values, counts, and a Merkle root of the raw records. The seal is a *certified derived assertion* — the "opening balance" — whose justification says "closing of region R" rather than reaching raw observations. Seals are the graduation pattern (cached → published → certified, seed §19.6) applied to history itself.
2. **Purge.** Raw evidence in the region is destroyed (or archived). Allowed only when a seal covers the region — otherwise the honest state is `lost`, not `purged`.
3. **Record.** The coverage map is updated by ordinary appended assertions: what was forgotten, when, by whom, under which retention policy version.

Provenance then never lies — `why()` walks down to a seal and stops, honestly: *"derived from CheckpointH (sealing txs 0–184,220 of 2019, certified 2020-01-15; raw evidence purged 2025-01-15 under RetentionPolicy v3)."*

The stance in one sentence: **Gneiss never forbade being wrong — it forbids being silently wrong. Likewise it does not forbid forgetting — it forbids silent forgetting.**

**The hard part of seal design — resurrection by attrition.** Monotone degradation has one subtle enemy: suppose assertion W won a conflict against L under policy, W's region gets purged, and L's region survives. A naive recompute over surviving evidence would let L win *by attrition* — a silent flip, exactly what I4′ forbids. Therefore a seal must capture not merely aggregates but the **accepted-value frontier and the defeat outcomes** for its region: winners, and enough of the defeat record that surviving losers stay defeated. Equivalently: the belief fold must treat seals as evidence that *outranks* raw survivors for the sealed scope. This is the single trickiest piece of the whole imperfection design — the amnesia drill (§8) exists chiefly to catch violations of it, and seal content design is a named sub-problem of D13.

## 4. The graded contract: purity replaced by monotone degradation

The contract becomes:

> `view = f(surviving evidence, coverage map, context)`
> — where the coverage map is part of the evidence, and `f` is **monotone under coverage loss**: reducing coverage may move answers to weaker epistemic grades or widen typed unknowns, but may never silently change an accepted value.

Every belief, and every report run, carries an **epistemic grade**:

| Grade | Meaning | What survives |
|---|---|---|
| `grounded` | recomputable from surviving raw evidence | everything |
| `sealed` | recomputable from certified checkpoints | reproducibility via seals; raw re-interrogation gone |
| `attested` | the claim, its hash, and its coordinates (context version, high-water tx) survive; inputs do not | proof that we said X; not the ability to re-derive X |
| `orphaned` | attestation exists but its coverage is `suspect`/`lost` | a flag for review; treated as contested |

`attested` deserves rehabilitation: it is exactly what paper-era audit provided, and it was sufficient for centuries. A 2019 report whose inputs were purged in 2025 is not a broken promise — it is an as-reported artifact with a certificate, clearly badged.

**Proposed invariant amendments** (offered for discussion, not yet applied to [20-KERNEL.md](20-KERNEL.md)):

- **I1′** Append-only, with forgetting as recorded, certified state transitions (seal → purge → coverage assertion). Silent mutation remains forbidden; redaction (23 §7) becomes a special case of this protocol.
- **I3′** Total transaction order is a **per-ledger** property (one sequencer per ledger). Cross-ledger order is evidence (watermarks), never axiom.
- **I4′** Views are pure functions of (surviving evidence, coverage, context), monotone under coverage loss; every view output carries its grade.
- **I8** Negative conclusions require positive coverage (§2 above).
- **I9** Every ledger maintains its own coverage map, epoch record, and periodic integrity roots; the amnesia and restore drills (§8) are part of the definition of done.

## 5. Distribution: ledgers that regard each other as fallible sources

The wrong instinct is a distributed ledger with global total order — consensus protocols, coordination, and a fiction of a single global "now". The Gneiss-native answer mirrors its storage stance (no universal store; per-modality engines under one contract):

> **Don't build a distributed ledger. Build many small ledgers that treat each other as fallible sources.**

- **Per-ledger order is real and cheap**: one sequencer (one database identity column) per ledger. Multi-site AIMS, scraper farms, offline collectors each keep a local evidence ledger — possibly tiny and ephemeral.
- **Federation is import**, and import is already solved machinery: assertions arriving from another ledger carry source coordinates and `source_recorded_at`; the receiving ledger assigns its own transaction time. A remote ledger is a *source* with reliability, watermarks, and the full defeasibility treatment.
- **Convergence is a belief, not a property**: "central incorporates collector B through B-tx 987" is a watermark assertion — revocable, monitorable, exactly the closure-declaration pattern. Replica disagreement is contested evidence resolved by policy, not a violated invariant.
- **What is given up**: the single global as-known-then instant. What is gained: the honest version — "as known to ledger A at its tx N, incorporating B through W" — which is what was actually true all along. Queries against federated views surface their watermark vector.

Two mechanical rules make this robust against real networks:

- **Idempotent append**: assertion ids are deterministic hashes of (content, source coordinates, batch key). Retries, replays, and double-imports coalesce instead of duplicating. At-least-once delivery + idempotent append = effective exactly-once, the standard event-sourcing result.
- **Epochs**: a restore-from-backup starts a new ledger epoch with a recorded assertion — "epoch E2 restored from E1@tx N; E1 txs N..M `lost`." Forks are recorded, never denied. Views spanning the fork carry the degradation.

## 5b. The imperfect outside: external sources and organ stores

*(Added same day, from discussion: Smoothcomp pages, video streams, and sensor feeds are not perfectly re-acquirable either — the imperfection runs through our abstractions of external systems too.)*

**There is no replay of the world.** Every read of an external system is an *observation* — timestamped, unrepeatable. Re-scraping a Smoothcomp page in 2027 does not recover the 2019 evidence; it produces *new* evidence about the page's 2027 state, from a witness that mutates in place and keeps no history of its own. Corollary that reframes the retention economics: **raw captures are always observation-grade — the sole surviving witness of an external system's past state.** The "re-acquirable in principle" discount in §7 was wrong and is corrected below. A second corollary worth saying out loud: for every mutable external system it touches, the Gneiss capture layer *is that system's only bitemporal record* — Gneiss is the memory the outside world doesn't keep. That is a feature to advertise, and a responsibility that raises the capture layer's retention priority.

External sources therefore get the same graded treatment as everything else: acquisition windows and availability are recorded per source; a source that redesigns, paywalls, or deletes its archive gets a coverage-style assertion ("SourceX historical pages unavailable from 2026-03"); expected-loss-rates can be declared per source class so retention policy reads them.

**Organs rot too.** The per-modality stores (blob storage for media, TS stores for telemetry) are not eternal append-only abstractions: files corrupt, retention jobs expire samples, migrations drop precision. Two consequences: (a) the coverage map extends to organ regions — a series descriptor or media reference carries coverage state like any ledger region; (b) **content hashes captured at ingest are cheap attestations that outlive the payload**: when the video file is gone, the ledger still proves what its bytes were, and every derived assertion that cited it keeps a verifiable (if no longer recomputable) grade — `attested`, exactly per §4. Hash-at-capture should be mandatory for media and recommended for series segment seals.

## 6. Faults become data: detection, quarantine, blast radius

- **Detection**: periodic Merkle roots over transaction ranges, stored as assertions (optionally cross-deposited on another ledger). Integrity checking is then a scan; a failed range flips coverage to `suspect`. This is ~30 lines of hashing, not blockchain theater — its entire purpose is converting *silent* corruption into *recorded, bounded* ignorance.
- **Quarantine**: "code version V wrote garbage between t1 and t2" is method invalidation — already in the model. A defect is a *method-entity with a bad period*; one decision defeats the whole cone.
- **Blast radius is a query.** The reason coherence cannot collapse the way an inconsistent classical theory collapses: ex falso quodlibet requires global inference, and the belief engine deliberately has none. Acceptance is a per-key fold; derivation flows only through recorded justification edges. Any injury's consequences are exactly: (its justification cone) ∪ (conclusions whose closure claims overlap the damaged coverage region) — a computable, displayable set. "How wrong could we be, given this?" is a report, not a crisis. Brittleness lives in systems whose correctness claims are global and implicit; every Gneiss answer carries the coordinates of its own validity (context version, watermarks, grade), so failure degrades per answer, never globally.

## 7. Retention economics: the problem half-dissolves by stance

What actually bloats a five-year Gneiss ledger, examined per stance:

| Stance | Growth | Retention verdict |
|---|---|---|
| Dense telemetry | huge — **but never in the ledger** (descriptors only) | archival is the TS-store's native business; update descriptors |
| Hypotheses | the bulk of ledger growth | **purge freely** — regenerable machine output, and decisions already survive without the tokens because they target claim keys ([23-STORAGE.md](23-STORAGE.md) §5). Retroactive vindication of decision D3. |
| Derived / cached values | large | recomputable while inputs live; purge on policy; seal `published`/`certified` ones first |
| Imported evidence (scrapes, captures of external systems) | moderate | **corrected (§5b): observation-grade, not re-acquirable.** Re-acquisition is a new observation of a mutated witness, never recovery. Captures are the sole surviving record of the external past — keep longest, seal before any purge, hash at ingest. |
| Raw observations (manual readings, sparse measurements) | small–moderate | **irreplaceable — keep longest**; seal into period summaries before any purge. The one genuine loss purging inflicts: you can never run a better 2029 matcher over 2019 raw evidence you destroyed. That trade is made *explicitly*, per predicate, in a retention policy. |
| Identities, ontology/policy versions, seals, **non-regenerable adjudications** | small | **permanent core — never purge.** Corrected per [26-DECIDERS.md](26-DECIDERS.md): the permanent tier is defined by *non-regenerability*, not by the Decision stance — human case-specific judgment stays forever; machine-generated verdicts at volume are derived-grade and purgeable after their outcomes are sealed. The core stays small *by this rule*, not by assuming decisions are human. |

The forever-core is small; the bulk is regenerable by construction. Retention schedules are declared policy versions in the ledger (per predicate × stance), like every other policy — an ops accident becomes a governed act.

## 8. Drills: forgetting must be rehearsed

If forgetting is designed but never exercised, year five implements it rudely. Into the P0/P2 property suites ([31-PROTOTYPES.md](31-PROTOTYPES.md)) from day one:

- **Amnesia drill**: randomly seal-and-purge regions of a test ledger → assert monotone degradation (answers only weaken in grade or widen typed unknowns; no silent flips); assert decisions survive hypothesis purges; assert `absent_closed` retreats to `unknown` where coverage fell below the I8 bar; assert report-run grades transition `grounded → sealed → attested` correctly.
- **Restore drill**: fork an epoch mid-history → assert the fork is recorded, watermarks honest, spanning views degraded and badged.
- **Corruption drill**: flip bits in a region → assert detection flags `suspect`, quarantine bounds the blast radius to the computed cone.

## 9. New decisions for the agenda

- **D11 — Coverage granularity.** Position: contiguous (tx-range × scope) regions, coarse; per-assertion coverage is forbidden complexity. Counter: mixed-scope purges may force finer grain.
- **D12 — Distribution stance.** Position: single sequencer per ledger + federation-as-sources; refuse consensus protocols outright. Counter: is there any real Gneiss deployment that genuinely needs multi-writer on *one* ledger? (Suspected answer: no — collectors are natural single-writers.)
- **D13 — Retention schedule defaults** per the §7 table; per-predicate overrides as ontology assertions. Needs Govert's read on re-scrapeability of Smoothscrape sources (does `imported evidence` really get the "re-acquirable" discount?).
- **D14 — Grade UX.** How loud are `sealed`/`attested` badges? Position: as loud as `restated` — same family of honesty. Counter: badge fatigue (see decision-fatigue risk, [32-RISKS.md](32-RISKS.md)).

## 10. The one-paragraph summary

The ledger joins the rest of the model as evidence rather than axiom: its completeness is a scoped, defeasible claim (coverage map); forgetting is a certified transition (seal → purge → record), never a silent one; distribution is many honestly-ordered small ledgers treating each other as fallible sources with watermark beliefs; faults are detected, quarantined, and bounded by the justification graph rather than exploding; and purity is replaced by the property that actually matters and can actually be tested — **monotone degradation: losing history can weaken what we claim to know, but can never silently change it.** The gneiss metaphor was apter than intended: real bedrock has faults, and you build on it by mapping them, not by pretending it is ideal granite.
