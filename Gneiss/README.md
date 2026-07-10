# Gneiss

**A meta-architecture for long-lived operational knowledge.**

**In plain language: Gneiss is a system for keeping important knowledge accountable over time: what was claimed, what is currently believed, why, under which rules, and what changed.** It is intended to be embedded as a library and schema or run as a non-interfering sidecar beside existing repositories, databases, and tools.

This repository currently contains the design rather than the software. It explores a *style of system design* in which:

> Everything important is an assertion.
> Assertions have time, source, method, support, and status.
> Current truth is a belief view over the assertion history.
> Storage is optimized per modality.
> Reports declare their evaluation context.

The name is deliberate: gneiss is a metamorphic bedrock — formed under pressure from older material, banded but coherent, and something you can build on for a very long time.

## Status

**Charter established; design and pre-implementation phase.** [CHARTER.md](CHARTER.md) is the governing statement of purpose, conceptual architecture, invariants, scope, and evolution discipline. [ROADMAP.md](ROADMAP.md) is the first development roadmap (2026-07-10): declared envelope, v0 architecture, epics, and gated milestones to "usable or falsified." The critical review behind both is [CHARTER-REVIEW.md](../CHARTER-REVIEW.md). The seed document is [KNOWLEDGE_MODEL_BRAINSTORM.md](KNOWLEDGE_MODEL_BRAINSTORM.md). The `kb/` directory develops that seed into an idea-and-knowledge-base: surveys of prior art, a proposed minimal kernel, scoping onions, prototype designs, and a ranked agenda of decisions to make.

The charter commits the design center, not a technology stack or final physical schema. The wider corpus remains evidence and argument that can be challenged as implementation teaches us more.

The first intended realization is [KodePorter](../KodePorter/docs/brainstorming-and-project-framing.md), a project-scale map and control system for creating and continuously preserving software ports. KodePorter supplies the demanding domain vocabulary and workflow; Gneiss supplies the reusable epistemic substrate. The boundary and first build are worked through in [kb/37-KODEPORTER-REALIZATION.md](kb/37-KODEPORTER-REALIZATION.md).

## Reading order

**Start with [CHARTER.md](CHARTER.md).** It is the concise governing document from which scope, design, and development should proceed. Then read [THE-STORY-SO-FAR.md](THE-STORY-SO-FAR.md) for the end-of-day-one synthesis and [kb/00-INDEX.md](kb/00-INDEX.md) for the annotated exploration. The short path through the supporting corpus:

1. [kb/01-PROBLEM-FRAME.md](kb/01-PROBLEM-FRAME.md) — the thesis, sharpened: the two-plane model and the Gneiss Contract.
2. [kb/20-KERNEL.md](kb/20-KERNEL.md) — a proposed five-primitive kernel (smaller than the seed's twelve).
3. [kb/37-KODEPORTER-REALIZATION.md](kb/37-KODEPORTER-REALIZATION.md) — the first domain realization, the Gneiss/KodePorter boundary, and the clearest statement of the archival blocker.
4. [kb/30-SCOPE-ONION.md](kb/30-SCOPE-ONION.md) — how much Gneiss to apply, where, and when to stop.
5. [kb/31-PROTOTYPES.md](kb/31-PROTOTYPES.md) — concrete prototype designs with sequencing.
6. [kb/40-DISCUSSION-AGENDA.md](kb/40-DISCUSSION-AGENDA.md) — the decisions that actually need making.

## Context

Motivating systems: AIMS (industrial monitoring: silos, sensors, derived mass), Smoothscrape / CompSeek (competition data: scraped registrations, OCR identities, event–video matching, human-confirmed links), and future ERP-adjacent and bulk-materials work.

**This is a conceptual investigation, not a stack decision.** Concrete SQL/C# fragments in the corpus are notation and existence proofs; platform choices come far down the line. The policy — and the substrate contract that replaces platform naming — is [kb/03-NOTATION.md](kb/03-NOTATION.md).
