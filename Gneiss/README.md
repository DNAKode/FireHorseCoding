# Gneiss

**A meta-architecture for long-lived operational knowledge.**

Gneiss is not (yet) a piece of software. It is an exploration of a *style of system design* in which:

> Everything important is an assertion.
> Assertions have time, source, method, support, and status.
> Current truth is a belief view over the assertion history.
> Storage is optimized per modality.
> Reports declare their evaluation context.

The name is deliberate: gneiss is a metamorphic bedrock — formed under pressure from older material, banded but coherent, and something you can build on for a very long time.

## Status

**Discussion phase.** The seed document is [KNOWLEDGE_MODEL_BRAINSTORM.md](KNOWLEDGE_MODEL_BRAINSTORM.md). The `kb/` directory develops that seed into an idea-and-knowledge-base: surveys of prior art, a proposed minimal kernel, scoping onions, prototype designs, and a ranked agenda of decisions to make. Positions are taken throughout — deliberately, so there is something concrete to disagree with.

Nothing here is committed. The point of this corpus is to make the next conversation sharper.

## Reading order

**Start with [THE-STORY-SO-FAR.md](THE-STORY-SO-FAR.md)** — the end-of-day-one synthesis, written from full context: the demotion operator, the session-as-first-ledger, the five deepest objects, the institutional vision, and the instructions to future sessions. Then [kb/00-INDEX.md](kb/00-INDEX.md), which annotates every document. The short path through the corpus:

1. [kb/01-PROBLEM-FRAME.md](kb/01-PROBLEM-FRAME.md) — the thesis, sharpened: the two-plane model and the Gneiss Contract.
2. [kb/20-KERNEL.md](kb/20-KERNEL.md) — a proposed five-primitive kernel (smaller than the seed's twelve).
3. [kb/30-SCOPE-ONION.md](kb/30-SCOPE-ONION.md) — how much Gneiss to apply, where, and when to stop.
4. [kb/31-PROTOTYPES.md](kb/31-PROTOTYPES.md) — concrete prototype designs with sequencing.
5. [kb/40-DISCUSSION-AGENDA.md](kb/40-DISCUSSION-AGENDA.md) — the decisions that actually need making.

## Context

Motivating systems: AIMS (industrial monitoring: silos, sensors, derived mass), Smoothscrape / CompSeek (competition data: scraped registrations, OCR identities, event–video matching, human-confirmed links), and future ERP-adjacent and bulk-materials work.

**This is a conceptual investigation, not a stack decision.** Concrete SQL/C# fragments in the corpus are notation and existence proofs; platform choices come far down the line. The policy — and the substrate contract that replaces platform naming — is [kb/03-NOTATION.md](kb/03-NOTATION.md).
