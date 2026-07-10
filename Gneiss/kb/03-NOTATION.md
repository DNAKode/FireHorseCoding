# Notation, Concreteness, and Platform Neutrality

*Added 2026-07-04 from discussion. Govert's correction: the docs fuss about .NET and SQL as if a tech stack were being targeted. It is not. This is a conceptual investigation; implementation platform choices come far down the line. This document states the policy that governs the rest of the corpus.*

## 1. Three layers of commitment

| Layer | What it is | Status in this corpus |
|---|---|---|
| **Conceptual model** | The kernel algebra (entities, assertions, transactions, justifications, contexts), the contract `view = f(evidence, coverage, context)` with monotone degradation, invariants and bedrock, stances, policies-as-data, the belief fold, typed missingness, the onions | **The actual subject.** Must be statable without naming any technology. If a claim cannot be phrased without naming a database, it is not yet a conceptual claim. |
| **Bindings** | How the model lands on a *class* of substrate: a relational binding, a document binding, an event-log binding | Illustrated concretely (e.g., [23-STORAGE.md](23-STORAGE.md) is essentially the relational binding); all choices still open. A binding is an existence proof, not a selection. |
| **Implementations** | A particular codebase on particular technology | **Deferred.** Exists only inside prototypes, where tech picks are conveniences for falsification speed, discarded without ceremony. |

## 2. Rules for the corpus

1. **SQL, C#, and Datalog fragments are notation.** They appear because worked examples keep abstractions honest (seed Risk 1), and a schema sketch is a precise way to say "these fields, these keys." They carry zero targeting force. Any fragment could be rewritten in another notation without changing the claim it illustrates.
2. **Platform-contingent facts are evidence, not decisions.** The surveys (10–14) were deliberately run with a .NET/SQL lens — a concreteness device, chosen because the motivating systems happen to live there. Findings like "no .NET incremental-Datalog library exists" or "Marten ships the projection pattern" are *feasibility data about one candidate binding*. They inform nothing until an implementation layer opens.
3. **No conceptual claim may depend on a platform feature.** Where a design doc leaned on one (e.g., SQL Server temporal tables as tamper-evidence), read it as "some integrity mechanism, of which X is one instance."
4. **Prototype tech picks expire with the prototype.** P0's belief fold could be C#, F#, Python, or pseudocode with a test harness; what survives P0 is the semantics and the property suite, not the code.

## 3. The constructive replacement: the substrate contract

The platform-facing face of the bedrock ([27-EVOLUTION.md](27-EVOLUTION.md)) is not a technology name but a requirements list. **Any substrate hosting a Gneiss ledger must provide:**

- **S1 — Durable ordered append**: a per-ledger totally ordered, durable append operation (the physical carrier of transaction order, I3′).
- **S2 — Non-mutation enforcement or detection**: either the substrate can forbid in-place change of recorded testimony, or it supports cheap integrity detection (content hashing / periodic roots) so silent mutation becomes detected mutation (B1, I9).
- **S3 — Derived-view computation**: some means of computing and storing recomputable projections from the ledger (the view plane) — anything from batch jobs to incremental engines qualifies (I4′).
- **S4 — Content addressing**: stable hashes for documents, media, and seals, so attestation can outlive payloads ([25-IMPERFECTION.md](25-IMPERFECTION.md) §5b).
- **S5 — Reference stability**: identifiers that can be held across the substrate's own upgrades and migrations (entities must outlive storage generations).

A relational database satisfies S1–S5 comfortably — which is *why* the relational binding reads so naturally, and the honest content of the old "SQL spine" language. So do log stores, document stores with append discipline, or files plus an index. The contract is the requirement; everything else is shopping.

A corollary worth stating because it closes a loop with [27-EVOLUTION.md](27-EVOLUTION.md): **replatforming is itself expected evolution.** A substrate migration is a ceremony (export testimony, verify roots, re-import with source coordinates preserved, record the epoch) — the model must survive its own re-hosting, and a conceptual layer contaminated with platform assumptions would fail exactly that test.

## 4. What was reworded where

- [README](../README.md): "implementation substrate" line replaced with a concepts-first statement pointing here.
- [01-PROBLEM-FRAME.md](01-PROBLEM-FRAME.md) §4: conformance example de-branded.
- [23-STORAGE.md](23-STORAGE.md): preamble now names it the *relational binding*, SQL as notation.
- [30-SCOPE-ONION.md](30-SCOPE-ONION.md) A2: "kernel library in the adopting systems' host ecosystem" (packages named for illustration).
- [31-PROTOTYPES.md](31-PROTOTYPES.md): preamble marks all tech picks as falsification conveniences.
- [40-DISCUSSION-AGENDA.md](40-DISCUSSION-AGENDA.md) D7: rewritten from "which language" to "deferred by design; substrate contract governs."
- Survey docs left untouched (their lens is disclosed here and in the index): their platform observations remain useful *as data*.
