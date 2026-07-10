# The Bedrock, Operationally: The Witness-Stand Test

*Added 2026-07-04 from discussion. Govert: before the adversarial round, restate the "truly fixed" premise into something with operational meaning. This document is that restatement — the bedrock as operations, obligations, questions, and drills, so that "is this system Gneiss?" is decidable by inspection and test rather than by sympathy. It is also the designated hard target for the deferred adversarial round: attacks aim here.*

## 1. The compression

Abstract bedrock ([27-EVOLUTION.md](27-EVOLUTION.md)): **B1** testimony is never silently destroyed or altered; **B2** every interpretation is derivable from surviving testimony plus a declared context, degrading monotonically under loss; **B3** (candidate) the system describes itself.

Operational bedrock: **a Gneiss system can take the witness stand.** It survives cross-examination — seven questions it must never be unable to answer, a restricted write interface, and standing drills. One sentence:

> **It records on the record, answers with its reasons, and forgets only with receipts.**

## 2. The seven questions

Each question: what answering requires · what nonconformance looks like · the minimum viable compliance (MVC) in a plain system with no Gneiss machinery — because if the bedrock required machinery it would be product, not bedrock.

**Q1 — What do you claim?**
Every surfaced answer is *labeled*: context (name + version), evidence coordinates (high-water mark / watermark vector), epistemic grade.
*Nonconformance:* any answer without a label — a dashboard number that cannot say what it was computed from.
*MVC:* report footers and API basis fields carrying definitions-version + data-cutoff timestamp.

**Q2 — Why do you claim it?**
A justification walk from the answer to testimony or to a seal. Absences answer with their typed kind (`unknown`, `not_observed`, `rejected`, `absent_closed`, …), never bare nothing.
*Nonconformance:* dead-end values; NULLs that conflate "no" with "don't know."
*MVC:* derived tables keep references to their input rows; absence columns are enums, not NULL.

**Q3 — On whose word, since when?**
Attribution for every piece of testimony: source, method, actor, and position in the record (the per-ledger order).
*Nonconformance:* rows of unknown origin; imports indistinguishable from measurements.
*MVC:* source/method/load-batch columns on every imported and derived row (the Data Vault discipline).

**Q4 — What did you claim before, and why did it change?**
The past stays addressable; corrections are data targeting earlier claims; the difference between any two (time, context) coordinates is producible *with reasons*.
*Nonconformance:* UPDATE-in-place on facts; a restated report that cannot say what changed or why.
*MVC:* correction tables + no-UPDATE convention on fact tables; retained copies of issued reports.

**Q5 — What have you forgotten?**
Enumerable amnesia: a receipt for every destruction (what, by hash/coordinates; when; by whom; under what authority; what seal replaced it), the coverage map, the knowledge horizon.
*Nonconformance:* silent retention jobs; "we archived that at some point"; gaps discovered rather than declared.
*MVC:* a deletions-log table + a written retention schedule + summaries kept where detail was dropped.

**Q6 — By what rules did you conclude this, and can I see them as they were?** *(B3's operational content)*
The policies, ontology, and context definitions used in any answer are retrievable at the cited versions, and their own change history is itself on the record (who changed the rule, when, and what the previous rule would have said).
*Nonconformance:* logic living only in code with no versioned trace from answer to rule; "the algorithm changed sometime last year."
*MVC:* versioned config/policy tables with effective dates; at minimum, the released-code version stamped on every output.

**Q7 — Say it again.**
Replay under the same label reproduces the answer byte-for-byte — or the system states honestly which grade of reproducibility remains: `grounded` (from raw evidence), `sealed` (from certified checkpoints), `attested` (hash and coordinates survive; inputs do not).
*Nonconformance:* irreproducible numbers presented without qualification; "it said 12.4 at the time, we can't tell you why anymore" *without that being a typed, expected state*.
*MVC:* regeneration scripts for reports from base tables; permanent run records with output hashes where stakes warrant.

## 3. The write interface (B1 as verbs)

The only primitive is **append-with-envelope**. Every other permitted verb is a stance on append:

| Verb | What it appends | Special obligation |
|---|---|---|
| `record` | testimony with envelope (source, method, actor, position) | — |
| `assert-about` | claims targeting earlier records (corrections, decisions, invalidations) | may target only earlier positions |
| `declare` | policies, contexts, ontology — as versioned data | citable by version forever |
| `derive` | view-plane artifacts | always labeled (Q1); never mistaken for testimony |
| `seal` | a certified summary of a region | prerequisite for purge; carries integrity root |
| `purge` | a destruction receipt | destroys payloads only under a covering seal |
| `redact` | a destruction receipt under legal authority | destroys payload, preserves skeleton and justification structure |

**Prohibitions** (checkable in code review, meaningful at A0):
in-place update or delete of testimony · surfacing unlabeled answers · destruction without receipt · applying a policy that cannot be cited by version · bare NULL for semantic absence.

## 4. The standing drills (the premise as test suite)

The bedrock is not documentation; it is the permanent test suite of any conformant system:

| Drill | Action | Property enforced |
|---|---|---|
| Rebuild | delete all derived stores; regenerate from testimony + contexts | views are cattle (B2) |
| Replay | pick any past labeled answer; recompute under its label | byte-match, or honest grade (Q7) |
| Amnesia | seal-and-purge random regions | monotone degradation — answers weaken, never flip; decisions survive hypothesis purges; `absent_closed` retreats where coverage fell |
| Restore | fork an epoch mid-history | the fork is recorded, watermarks honest, spanning views badged |
| Corruption | flip bits in a region | detected (integrity roots), quarantined (`suspect`), blast radius bounded to the computed cone |
| Determinism | two independent evaluators, same testimony + context | identical belief views (the L0-oracle differential test) |

## 5. Mapping back to the abstract bedrock

| Commitment | Discharged by |
|---|---|
| B1 (no silent destruction/alteration) | write verbs + prohibitions; Q3, Q4, Q5; corruption + restore drills |
| B2 (interpretations derivable, degrading monotonically) | Q1, Q2, Q7; rebuild + replay + amnesia + determinism drills |
| B3 (self-description) | Q6 — and its operational cheapness is the argument for promoting B3 from candidate to bedrock (D15) |

## 6. Uses

- **Conformance audit of existing systems**: probe P-1 ([31-PROTOTYPES.md](31-PROTOTYPES.md)) becomes literal — put Smoothscrape (then AIMS) on the witness stand, ask the seven questions, record per-question pass/gap/cost-to-fix. The output is a gap list, not an impression.
- **A0 definition sharpened**: the discipline layer of the scope onion ([30-SCOPE-ONION.md](30-SCOPE-ONION.md)) *is* MVC on all seven questions plus the prohibitions. A team can adopt the bedrock this quarter with plain tables.
- **The adversarial round's target** (deferred, per discussion): hostile counsel attacks this specification — the questions, the verbs, the drills — looking for the objection that breaks it: adversarial testimony, redaction-vs-provenance, ceremony economics, or something we haven't imagined. A breach that survives contact with this document is a real breach.
