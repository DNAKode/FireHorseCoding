# KodePorter: the First Gneiss Realization

*Added 2026-07-10 after reading the complete KodePorter discussion trail and canonical framing document. This is a design decision and a test plan, not a claim that either system has been implemented.*

## 1. The two plain-language sentences

> **Gneiss is a system for keeping important knowledge accountable over time: what was claimed, what is currently believed, why, under which rules, and what changed.**

> **KodePorter keeps a software port alive: it records what must correspond, what may differ, how changes should propagate, and what evidence is required before the target is trusted.**

The first sentence should remain true for AIMS, Palantir-shaped systems, a research project built around FrankenSim, or a future domain we have not imagined. The second should make sense to an engineer planning a Rust-to-C# port without first learning the Gneiss vocabulary.

That is the boundary.

## 2. Decision

KodePorter should be the first real system built on Gneiss and the canonical example used to explain it.

Initially, it should use Gneiss as a **non-interfering sidecar**:

- Git remains the store for source and target code.
- rust-analyzer, Roslyn, build systems, and test runners remain authorities for their own mechanical observations.
- KodePorter owns the porting model, workflows, and views.
- Gneiss records the important claims, evidence, decisions, revisions, contexts, and answer labels behind that model.

This posture makes the integration removable and inspectable. Once the boundary has survived real work, parts of the KodePorter model may use Gneiss as bedrock rather than merely publishing to it.

The litmus test is simple:

> If the Gneiss machinery were removed, the remaining KodePorter nouns and workflows should still describe a crisp porting product, although one with weaker memory and accountability.

If all that remains is “Gneiss for code,” the domain model has disappeared and the separation has failed.

## 3. What belongs where

### Gneiss supplies the reusable epistemic substrate

- append-only testimony and transaction provenance;
- claims, justifications, decisions, correction, supersession, and retraction;
- declared evaluation contexts and versioned policies;
- deterministic current views over the recorded history;
- typed missingness, contest, and honest refusal;
- evidence coverage, retention state, and epistemic standing;
- consumed-set labels and `why` explanations;
- authority, delegation, and human/agent parity;
- reproducible reports and comparisons across contexts or times.

These concepts should not be redesigned inside every application.

### KodePorter supplies the porting domain

- source and target bases, repositories, commits, and build worlds;
- code entities and the structural, call, type, data, ownership, and test graphs over them;
- capabilities, behavioural contracts, and semantic events;
- migration units and conversion dossiers;
- many-to-many correspondences between source and target;
- port policies, equivalence criteria, transformation rules, adaptations, and exceptions;
- intentional divergences, compatibility obligations, and temporary bridges;
- verification plans, differential runs, and acceptance gates;
- source-delta impact, stale mappings, continuous preservation, and port health;
- porting-specific maps, work queues, reviews, and agent tools.

These are the concepts users should meet first in KodePorter. “Claim,” “evidence,” and “context” should appear when they clarify a porting question, not as an abstract preamble.

### The binding between them

A KodePorter object has a stable domain identity. Important statements about it are recorded through Gneiss.

Examples:

| KodePorter statement | Gneiss role |
|---|---|
| `ParserBehaviour/MalformedHeader` is a required contract | claim with a port-policy context |
| Rust parser region corresponds to C# parser region | correspondence claim |
| the implementations intentionally differ in error type | decision plus divergence object |
| differential run 417 matched 47 captured cases | observation supporting the preservation claim |
| reviewer accepted the deviation for target version 2 | decision with actor, reason, and time |
| source commit `abc` changed a consumed symbol | new observation that makes dependent mappings stale |
| current port health for the parser is “verified, one approved deviation” | derived KodePorter view carrying a Gneiss label |

Gneiss should not need to understand what a parser, crate, migration unit, or differential test is. KodePorter should not need to invent what supersession, coverage, or a reproducible answer means.

## 4. Granularity: index finely, claim selectively, cite precisely

The KodePorter discussion asked for a hierarchy that can reach statements and execution assumptions. That does **not** imply one enduring Gneiss assertion per line of code.

Three granularities must be kept distinct.

### 4.1 Artifact and analysis granularity

Git, compiler services, analyser indexes, traces, and test logs may operate at line, token, symbol, event, or byte level. KodePorter can index these densely. Gneiss records the versioned descriptor or import that says which artifact and analysis basis was used; it need not copy the entire organ store into the ledger.

### 4.2 Port-map granularity

KodePorter may retain a dense graph of code entities and candidate correspondences. Much of this can be generated deterministically and regenerated from a pinned repository/analyser basis. It is domain state, not automatically a collection of human-significant judgments.

### 4.3 Epistemic claim granularity

Create an enduring claim when a statement has an independent reason to live and change. In practice, promote it when one or more of these are true:

- it changes a migration decision, an agent instruction, a risk, or an acceptance gate;
- it can be disputed, corrected, or superseded independently;
- several downstream mappings or decisions rely on it;
- it crosses a source/target, component, runtime, or organisational boundary;
- it records uncertainty, an exception, or an intentional divergence;
- it compresses a meaningful investigation or body of executable evidence;
- a reviewer will reasonably ask “why do we believe this?” later.

The compact rule is:

> **Record claims at the granularity of a challengeable, reusable judgment. Cite evidence at the finest granularity needed to justify it.**

A single line may therefore be an exact evidence address without becoming a claim. If that line is the only place where deterministic disposal, numeric rounding, or transaction ordering is established, it may justify a project-level behavioural claim. The claim is about the behaviour; the line is its witness.

Stable evidence coordinates should combine immutable and semantic anchors where possible: repository plus commit, path and range, symbol identity, content hash, build or test-run identifier, and relevant runtime context. Immutable coordinates preserve what was seen; semantic coordinates help KodePorter find the corresponding thing after a refactor.

## 5. How the KodePorter scope changes

The ambition does not shrink. The implementation boundary becomes sharper.

### Remove from KodePorter's foundation work

KodePorter should not first build its own generic:

- append-only knowledge ledger;
- provenance ontology;
- belief/revision engine;
- context and policy versioning scheme;
- generic human-versus-agent authority model;
- archival truth-maintenance mechanism;
- universal explanation and answer-label format.

It should exercise and shape those facilities in Gneiss.

### Move to the foreground

KodePorter's first design work should concentrate on:

1. the structural pyramid and overlay graphs;
2. stable source and target entity identity;
3. port policies and typed equivalence criteria;
4. migration units and behavioural dossiers;
5. correspondence, adaptation, and divergence objects;
6. source-delta impact and continuous port preservation;
7. port-health views based on explicit obligations and evidence;
8. the human and agent workflows that create, test, accept, and revise those objects.

This makes KodePorter more visibly about ports, even though it gains stronger evidence and provenance underneath.

## 6. The canonical first vertical slice

The first prototype should no longer be a repository cartographer in isolation. It should be a thin, end-to-end **KodePorter-on-Gneiss** slice:

1. Pin a small Rust source and C# target to explicit commits and analysis versions.
2. Import their structural maps without modifying either repository.
3. Select one meaningful migration unit, not an arbitrary file.
4. Record its behavioural contract, correspondence, strategy, open questions, and any intended divergence.
5. Attach exact source references, analyser observations, and a differential test run as evidence.
6. Let an agent propose an implementation or mapping update; keep proposal distinct from acceptance.
7. Accept or reject it through an explicit review decision.
8. Show the current dossier and port-health result with `why`, context, and consumed-set label.
9. Advance the source by one commit and show precisely what becomes stale and why.
10. Correct one earlier claim without editing history, then reproduce both the old and current views.

The demo succeeds if an engineer can answer:

- What are we porting here?
- What must remain the same, and what may differ?
- Why do we think this part is complete?
- Which exact source, rules, tests, and decisions produced that answer?
- What changed when the source advanced?
- What do we now know we do not know?

After this small controlled slice, the FrankenTui/FrankenTui.NET partial port remains a good brownfield stress test for reconstructing a map from imperfect history.

## 7. What KodePorter should teach Gneiss

KodePorter is not only a consumer. It is the first serious pressure test of the substrate.

It should force concrete answers to:

- how domain schemas extend the kernel without becoming generic EAV;
- how dense, regenerable organ data relates to sparse, durable testimony;
- how stable identities survive changing repositories and refactors;
- how claims and evidence work at several overlapping resolutions;
- how a consumed set names code regions, analyser results, rules, and test executions;
- how derived health rolls up without becoming confidence theatre;
- how source changes invalidate only the affected cone;
- how a domain expresses its own warrant vocabulary without overloading Gneiss's retention grade.

FrankenSim is useful here as a sibling realization. Its `verified` / `validated` / `estimated` colors describe **how a physical claim is warranted**. Gneiss's `grounded` / `sealed` / `attested` standing describes **what evidential substrate remains available**. KodePorter will need its own domain warrant types—perhaps static, differential, observational, reviewed, or production-attested—kept orthogonal to the survival state of the evidence.

## 8. The archival confounder, in KodePorter language

The Maxwell page's ADQ problem becomes clearer in a port.

Suppose the current view says:

> The C# parser preserves the required Rust parser behaviour.

That answer may have consumed source code, 47 captured examples, a differential test, a port policy, one defeated alternative mapping, and an approved deviation. Later, the raw source capture and test detail are purged, leaving a compact seal saying only “parser equivalent.” A surviving old alternative may then appear undefeated, or a new policy may ask about malformed Unicode that the seal never summarized.

There is no universal finite summary guaranteed to answer every future porting question.

The plain statement of the blocker is:

> **After evidence is discarded, Gneiss must never answer as though it had re-read that evidence. An archive is safe only for the questions its seal was designed to support; outside that declared contract, the honest result is “unsupported from the surviving record.”**

This separates two requirements that the phrase “maintain consistency” had blurred:

1. **No resurrection by attrition.** Purging a winning claim's evidence must not silently allow an old defeated alternative to become the winner.
2. **No invented reinterpretation.** A later context must not apply a new question or policy to a lossy summary that lacks the distinctions needed to answer it.

For the first KodePorter realization, avoid pretending this is solved:

- keep source and target commits and important executable artifacts in Git or content-addressed storage;
- make evidence packages portable and hashed;
- if a migration unit is sealed, retain winners, defeated alternatives and their defeat reasons, applicable policy versions, coverage boundaries, and exact evidence references;
- declare the family of future queries the seal supports;
- return typed insufficiency or `NoClaim` outside that family.

This does not solve optimal archival summarization. It turns a mysterious global consistency problem into a domain-testable contract.

## 9. The Maxwell money shot, restated

The current page can be introduced without beginning with its six relations:

> **Gneiss never overwrites what was said. It computes the current answer from the recorded testimony and declared rules, and every answer carries a receipt for exactly what it used.**

The schema and fold are the mechanism behind that sentence. The archival confounder is the limit on the receipt: once some inputs are deliberately forgotten, the system must either preserve a sufficient, query-scoped substitute or weaken/refuse the answer.

## 10. Immediate design decisions for the build

Before implementation, the two projects should jointly settle only the choices needed by the vertical slice:

1. the minimum KodePorter domain types and their stable identifiers;
2. which dense analyzer outputs stay outside Gneiss and how a pinned import describes them;
3. the claim-promotion rule and evidence-coordinate format;
4. the first equivalence/acceptance policy for one migration unit;
5. the shape of a dossier view and its answer label;
6. the invalidation rule from a changed source entity to affected claims;
7. a deliberately narrow seal contract for the amnesia test.

That is enough to grow the first cell and test the candidate Maxwell kernel against a real domain without letting either project's abstraction swallow the other.
