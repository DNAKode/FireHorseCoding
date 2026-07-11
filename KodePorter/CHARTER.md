# KodePorter Charter

**Status:** Founding architectural charter  
**Purpose:** Define KodePorter's enduring product identity, its relationship with Gneiss, and the rules by which the project may evolve without losing conceptual integrity.  
**Applies to:** The KodePorter domain model, repository analysis, porting workflows, agents, tools, services, user experience, benchmarks, and Gneiss integration.  

---

## 1. Definition and charter authority

> **KodePorter is a map and control system for creating, understanding, verifying, and continuously preserving software ports.**

In plainer operational language:

> **KodePorter keeps a software port alive: it records what must correspond, what may differ, how changes should propagate, and what evidence is required before the target is trusted.**

KodePorter is built on [Gneiss](../Gneiss/CHARTER.md), which supplies the reusable machinery for accountable claims, evidence, decisions, revision, contexts, and labeled current views. KodePorter supplies the porting vocabulary, structures, workflows, and tools. Neither project is a synonym for the other.

This charter governs KodePorter's scope and conceptual architecture. The brainstorming notes and research corpus preserve the exploration that produced it. Future requirements and implementation designs should be derived from this charter and should amend it openly if experience demonstrates that its center is wrong.

## 2. Purpose

Porting a substantial software system is not mainly translation. It is a long-running programme of discovery, interpretation, decision, reconstruction, verification, and maintenance under changed technical constraints.

Modern coding models make code generation abundant. They can translate files, explain unfamiliar code, propose APIs, write tests, and investigate failures. They also make it easy to produce a large body of plausible target code whose behavioral coverage, architectural coherence, provenance, and trustworthiness are unclear.

The scarce resources are therefore not keystrokes. They are:

- an accurate map of both systems;
- a defensible understanding of behavior and constraints;
- explicit choices about preservation and change;
- coherent application of those choices across a large codebase;
- reliable evidence that the target satisfies the intended contract;
- and durable memory that survives models, sessions, branches, and team changes.

KodePorter exists to make those resources persistent and operational. It should let a human-and-agent team bootstrap a port, understand its present state, improve it, and keep it aligned as the source continues to evolve.

## 3. Design center: the living port map

The center of KodePorter is a persistent, multi-resolution **port map** connecting:

```text
source structure and behavior
            ↕
port policies, correspondences, adaptations, and obligations
            ↕
target structure and behavior
            ↕
executable verification, decisions, health, and change impact
```

The map is not only a visualization. It is the project's durable control surface. It says what the port is made of, how the two systems relate, which rules apply, which differences are intended, which questions remain open, and which evidence supports acceptance.

The target code is a vital deliverable, but it is not sufficient as the memory of the port. The map, rule library, dossiers, and evidence make the target explainable, revisable, and maintainable. They are what allow a later source delta to become bounded work rather than a new archaeology project.

KodePorter's characteristic operation is therefore **continuous port preservation**, not one-shot translation.

## 4. Relationship with Gneiss

### 4.1 The boundary

Gneiss supplies:

- immutable, attributable testimony;
- claims, justifications, decisions, correction, supersession, and retraction;
- versioned evaluation contexts and policies;
- deterministic current views;
- evidence coverage, typed missingness, and contest;
- answer labels, consumed sets, and `why` explanations;
- retention standing and archival honesty;
- authority and delegation for people and agents.

KodePorter supplies:

- source and target repository models;
- code and semantic entity identities;
- structural and overlay graphs;
- migration units and behavioral dossiers;
- correspondences, adaptations, divergences, and port policies;
- transformation strategies and reusable porting rules;
- verification obligations and port-specific warrant types;
- source-delta impact, staleness, work planning, and port health;
- porting tools and user workflows.

The litmus test is:

> If Gneiss were removed, the remaining KodePorter nouns and workflows would still describe a crisp porting product, but one with weaker memory, provenance, revision, and accountability.

If KodePorter becomes merely “Gneiss for source code,” its domain model has failed. If it rebuilds a generic provenance ledger and belief engine, the integration has failed.

### 4.2 Sidecar first

The initial integration should be non-interfering:

- Git remains authoritative for source and target artifacts.
- Compiler services and analyzers remain authoritative for language structure and resolution.
- Build systems and test runners remain authoritative for their executions and outputs.
- KodePorter owns the port map and domain workflow.
- Gneiss owns the important claims, evidence references, decisions, contexts, corrections, and derived answer receipts behind that workflow.

KodePorter must be able to inspect an existing port without first rewriting either repository or forcing its build through a proprietary runtime.

### 4.3 Co-development and co-evolution

KodePorter is Gneiss's first demanding customer and reference realization. It may expose missing generic capabilities, but porting concepts enter Gneiss only when they protect a Gneiss invariant or recur in another domain.

Conversely, Gneiss changes must be tested against KodePorter's canonical fixtures. A new context model, label format, identity rule, or seal behavior that makes existing port history uninterpretable requires an explicit migration and compatibility decision.

The two projects should share conformance examples but maintain separate charters, vocabularies, release concerns, and user promises.

## 5. Scope

KodePorter uses a broad but disciplined meaning of “port.” It includes transformations that preserve selected system meaning while changing one or more implementation worlds:

- programming language or language version;
- runtime, operating system, or processor architecture;
- framework, UI technology, or deployment model;
- library, protocol, or proprietary platform dependency;
- persistence schema or data access model;
- desktop, service, web, batch, or distributed form;
- a partial or failed existing port brought under control;
- a source-shaped implementation progressively made target-native;
- or a substantial redesign that preserves declared external contracts.

Not all transformations require the same method or equivalence strength. KodePorter should provide a common domain model while allowing specialized analyzers, strategies, evidence, and policies.

The first directional examples are a Rust source continuously feeding a C# target and, separately, a C# source feeding a Rust target. Bidirectional editing of both sides of one port is a later, harder problem and is not assumed by the first architecture.

## 6. Constitutional principles

### 6.1 A port is an explicit relationship, not a pile of target files

Every accepted target region should be connected to the source behaviors, policies, mappings, decisions, or target-only intentions that explain its place in the port. Traceability may be many-to-many and may cross architectural redesign.

### 6.2 Behavior outranks syntax

Syntactic and structural similarity are useful strategies, not the definition of correctness. A port preserves explicitly selected observable behaviors, contracts, constraints, and operational properties. It may intentionally change mechanisms and architecture.

### 6.3 Preservation intent is typed and scoped

“Equivalent” is never sufficient by itself. Equivalence or refinement claims state their scope and strength: API compatibility, input/output agreement, state-transition preservation, error semantics, performance envelope, protocol conformance, observational equivalence, lockstep behavior, or another declared criterion.

Different parts of one port may use different criteria.

### 6.4 Mapping may begin before understanding is complete

KodePorter should create useful structural maps from repositories, build metadata, analyzers, tests, and history before every behavior is understood. Unknown and tentative regions remain explicit. Porting work deepens the relevant parts of the map.

The system must not require a fictional complete recovered architecture before useful work begins.

### 6.5 The unit of work is chosen for meaning and verification

Files are artifact containers, not the universal migration unit. A migration unit may be a capability, behavioral contract, protocol, state transition, persistence boundary, vertical feature, call-graph slice, type cluster, or buildable subsystem.

Multiple overlapping decompositions are normal.

### 6.6 Correspondence, adaptation, and divergence are different objects

A correspondence says how source and target regions relate. An adaptation explains a systematic transformation required by language, runtime, library, architecture, or policy differences. A divergence records an intentional or unresolved difference. These must not be flattened into an unqualified link.

### 6.7 Generation is proposal, not acceptance

Code produced by a model, rule, transpiler, or human does not become accepted port state merely because it exists, compiles, or passes generated tests. Acceptance is a decision against explicit obligations and evidence.

### 6.8 Verification is continuous and claim-directed

Tests and runs are evidence for named claims. A green suite is not a universal certificate. Discovery, mapping, implementation, review, source synchronization, and production operation all create or challenge verification evidence.

### 6.9 Source-shaped and target-native are independent axes from correctness

An initial conservative port may preserve source structure for safety and traceability. Later work may make it target-native while retaining behavioral evidence and rationale. Architectural improvement and behavior preservation should be coordinated but not confused.

### 6.10 Uncertainty and temporary architecture remain visible

Candidate mappings, inferred behaviors, provisional implementations, compatibility bridges, and known deviations carry explicit status and intended lifetime. Temporary structures do not become permanent merely by surviving several releases.

### 6.11 Agents are replaceable workers over durable state

Agents query the port map, receive bounded work and applicable rules, propose deltas, run tools, and return evidence. They do not own project memory inside prompts. Their actions, assumptions, failures, and proposals remain attributable.

### 6.12 Port health is multidimensional and explainable

No single “percent ported” number can represent structural coverage, implementation depth, verification strength, staleness, risk, and unresolved intent. Roll-ups must preserve their component dimensions and link back to the underlying obligations.

## 7. Domain model

The following vocabulary is the initial KodePorter spine. It is deliberately domain-specific.

### 7.1 Identity and basis

- **PortProject:** the declared source, target, direction, purpose, policies, and lifecycle of one port.
- **SourceBasis / TargetBasis:** pinned repository commits, build configurations, analyzer versions, dependency locks, and environment assumptions used for a view.
- **Artifact:** a repository, file, generated output, schema, binary, document, test corpus, configuration, or build product.
- **CodeEntity:** a language-aware entity such as package, crate, assembly, module, namespace, type, member, declaration, expression, or semantic event.
- **ExternalEntity:** a service, protocol, database, platform facility, or user-observable workflow that participates in behavior.

Stable identity must combine immutable coordinates with semantic continuity. Commit, path, range, symbol, content hash, and analyzer identity serve different purposes and may all be needed.

### 7.2 Meaning and decomposition

- **Capability:** a meaningful thing the system enables for a caller, operator, user, or other system.
- **BehavioralContract:** inputs, outputs, state, effects, ordering, errors, timing, edge cases, external interactions, and observables that matter.
- **SemanticUnit:** the finest behaviorally meaningful event, constraint, or semantic assertion when statement-level context matters; not necessarily an executable source statement.
- **MigrationUnit:** a bounded, plan-able, reviewable, and verifiable portion of the port.
- **Dossier:** the current working view for a migration unit: purpose, scope, contract, dependencies, strategy, mappings, questions, decisions, evidence, risks, target state, and acceptance status.

### 7.3 Relationship and transformation

- **Correspondence:** a typed, scoped relationship between source and target entities, capabilities, behaviors, or structures.
- **PortPolicy:** a versioned declaration of fidelity, naming, structure, compatibility, target-native direction, risk tolerance, and acceptance requirements.
- **EquivalenceCriterion:** the particular preservation or refinement relation required for a claim.
- **TransformationStrategy:** mechanical translation, semantic reimplementation, wrapping, emulation, strangler replacement, data-first migration, test-first reconstruction, architecture preservation, redesign, or selective abandonment.
- **TransformationRule:** a reusable instruction that maps a recurring source pattern or change into target work.
- **Adaptation:** a required systematic departure caused by differences between worlds.
- **Exception:** a local departure from an otherwise applicable rule.
- **Divergence:** an intentional, accidental, disputed, or not-yet-resolved difference between source and target.
- **CompatibilityBridge:** a deliberately temporary coexistence or emulation structure with an exit condition.

### 7.4 Work and assurance

- **Obligation:** something that must be understood, implemented, verified, reviewed, or retired before a declared acceptance gate.
- **Question / Hypothesis:** unresolved or tentative domain knowledge directing investigation.
- **VerificationPlan:** the evidence required to support one or more claims.
- **VerificationRun:** a pinned execution of tests, comparisons, traces, analysis, benchmarks, or review.
- **KnownDeviation:** an observed difference with disposition, scope, and acceptance status.
- **Risk:** a possible failure with affected scope, likelihood or uncertainty, consequence, mitigation, and owner.
- **Review / Decision:** an acceptance, rejection, exception, or architectural choice recorded through Gneiss.
- **AgentRun:** a bounded investigation or implementation attempt with inputs, authority, outputs, evidence, and failures.

### 7.5 Evolution and health

- **SourceDelta:** a change between pinned source bases.
- **ImpactCone:** the mappings, rules, target regions, tests, and obligations potentially affected by a delta.
- **StaleMapping:** a correspondence whose consumed source or rule basis has advanced without revalidation.
- **PortHealth:** a derived, labeled view over coverage, depth, evidence, staleness, divergence, risk, and acceptance.
- **Milestone / AcceptanceGate:** a declared set of obligations and evidence required for a release or migration stage.

This vocabulary should change through case studies and prototypes, but changes must preserve distinctions rather than merely rename a generic graph.

## 8. Granularity and evidence

KodePorter must keep three granularities separate.

### 8.1 Artifact and analyzer granularity

Git, compilers, analyzers, traces, and test systems may work at byte, line, syntax-node, symbol, event, or call-edge resolution. KodePorter may index this material densely. Most of it is regenerable organ data tied to a pinned basis.

### 8.2 Port-map granularity

The map may contain every structural entity and large numbers of generated candidate links. These are necessary for navigation and impact analysis, but they are not all independent judgments requiring permanent epistemic ceremony.

### 8.3 Claim granularity

An enduring Gneiss-backed claim is created when a statement:

- changes a migration decision, agent instruction, risk, or acceptance gate;
- may be disputed, corrected, or superseded independently;
- supports several downstream relationships or decisions;
- crosses a source/target, runtime, component, or organizational boundary;
- records an exception, uncertainty, or intentional divergence;
- compresses a meaningful investigation or body of executable evidence;
- or is likely to provoke a later “why do we believe this?” question.

The governing rule is:

> **Index finely, claim selectively, and cite precisely. Record a claim at the granularity of a challengeable, reusable judgment; cite evidence at the finest granularity needed to justify it.**

A single source line may be the decisive evidence for resource lifetime, rounding, ordering, or error behavior. The claim is about that behavior; the line is its witness. KodePorter should not create one claim per ported line by default.

## 9. Pyramid of connected graphs

KodePorter should support several connected structures rather than forcing one graph to carry every meaning.

The containment hierarchy runs approximately from repository groups through repositories, builds, packages, modules, files, types, members, statements, and semantic events. Overlay graphs include:

- build and package dependency;
- call and control flow;
- type and data flow;
- ownership, lifetime, concurrency, and resource flow;
- capability and behavior participation;
- source-to-target correspondence;
- transformation rules, adaptations, and exceptions;
- verification, evidence, and claim support;
- work, review, risk, and acceptance state.

Rules declare the graph, scope, and level on which they operate. Refinement links connect coarse statements to detailed cases. A package-level mapping may remain stable while member-level details change. Strong API-level equivalence can coexist with implementation-level divergence.

Roll-ups must preserve drill-down. A health total without its obligations and evidence is not a trustworthy map.

## 10. Port lifecycle

KodePorter supports a recurring lifecycle rather than a one-way pipeline.

### 10.1 Declare the port

Record source and target worlds, direction, goals, exclusions, fidelity policies, target-native ambition, constraints, security boundaries, and acceptance strategy. These become versioned project artifacts, not oral setup instructions.

### 10.2 Establish pinned bases

Identify repository commits, dependencies, build configurations, analyzers, test baselines, environments, and relevant external systems. If an existing target is dirty or inconsistent, inventory and protect that state before modification.

### 10.3 Map both systems

Build source and target hierarchies independently from authoritative tools. Add overlays and candidate correspondences without assuming that similar names imply equivalence.

### 10.4 Recover meaning where work demands it

Select migration units, create dossiers, state behavioral hypotheses, identify dependencies and risks, and turn uncertainty into targeted questions. Complete understanding is not a prerequisite; honest local depth is.

### 10.5 Choose strategy and obligations

For each unit, declare the transformation strategy, equivalence criteria, intended divergences, temporary bridges, required evidence, and human decisions.

### 10.6 Implement through proposals

Agents, rules, and people produce bounded code, mapping, test, and documentation deltas. Generated artifacts are linked to the instructions and basis that produced them.

### 10.7 Verify and accept

Builds, static analysis, differential execution, property tests, traces, benchmarks, review, and production observations support named claims. Acceptance closes declared obligations; it does not erase uncertainty outside their scope.

### 10.8 Preserve continuously

When the source advances, compute the impact cone, replay deterministic rules, assign bounded synthesis work, rerun relevant verification, and mark affected regions healthy, stale, blocked, or contested. Human attention is requested where judgment is necessary.

### 10.9 Revise and improve

Corrections append. Policies evolve by version. Temporary architecture is retired deliberately. Target-native refactoring remains connected to the behavior and decisions it preserves.

## 11. Verification and warranted trust

KodePorter should support multiple evidence modes:

- compiler and analyzer facts;
- source references and history;
- existing and generated unit tests;
- differential execution;
- golden-master and snapshot comparison;
- property-based and fuzz testing;
- state, database, protocol, and trace comparison;
- performance and resource benchmarks;
- manual expert review;
- staging, shadow, and production observations.

These are not one total ladder. A static proof, differential test, expert review, and production observation warrant different claims. KodePorter should define a small port-specific vocabulary such as structural, differential, observational, reviewed, and operationally attested, while Gneiss separately records evidence survival and context.

Every acceptance claim should identify:

- the behavior or obligation addressed;
- the source and target bases;
- the equivalence criterion;
- supporting and contradicting evidence;
- known deviations;
- the applicable policy version;
- and the decision or rule that accepted it.

Test laundering is a primary threat. A generated test that merely reproduces assumptions in generated code is weak evidence. Whenever feasible, verification should be independently derived from source behavior, captured examples, external contracts, alternative methods, or separate reviewers.

## 12. Port health and progress

KodePorter reports progress across independent dimensions:

- structural map coverage;
- correspondence coverage;
- implementation presence and depth;
- behavioral-dossier coverage;
- verification coverage and warrant type;
- accepted versus proposed state;
- unresolved questions and decisions;
- source-delta staleness;
- intentional and unexplained divergence;
- temporary compatibility debt;
- risk and critical-path obligations;
- production or operational adoption.

These may roll up by migration unit, capability, package, subsystem, release, and entire port. Roll-up functions are versioned policies and must expose their components.

“Unknown” and “unimplemented” remain separate. A fully understood but unwritten component is different from a present implementation whose behavior is not understood. File presence and line counts must never masquerade as semantic completion.

## 13. Human and agent roles

Models are expected to perform substantial work:

- repository exploration and explanation;
- candidate mapping and pattern detection;
- dossier drafting;
- translation and target-native implementation;
- test generation and repeated comparison;
- source-delta triage;
- gap and inconsistency detection;
- documentation and rationale synthesis.

Humans remain responsible for outcomes where domain meaning, architecture, risk, security, compatibility, or intentional loss requires judgment. Authority may be delegated to policies or agents for bounded classes of decision, but delegation is explicit and revocable.

KodePorter should support autonomy levels from read-only analyst through planner, investigator, implementer, coordinator, and migration operator. Each work item declares:

- allowed repositories and environments;
- permitted actions;
- applicable rules and context;
- required evidence;
- approval and merge gates;
- time, compute, and cost budgets;
- and failure/rollback behavior.

Failed investigations are retained when they teach something. Destructive Git operations, silent test weakening, target overwrite, and unbounded autonomous regeneration are prohibited by default.

## 14. Product form

KodePorter should be assembled as interoperable components around the shared domain model:

1. **Repository cartographer** — pinned structural hierarchies and overlays from language and build providers.
2. **Port map store and query service** — domain entities, correspondences, obligations, deltas, and Gneiss bindings.
3. **Dossier workspace** — the primary unit for investigation, planning, implementation, and review.
4. **Rule and policy library** — versioned, readable, reusable porting knowledge with applicability and exceptions.
5. **Verification adapters** — builds, analyzers, tests, differential harnesses, traces, benchmarks, and runtime evidence.
6. **Continuous port monitor** — source-delta ingestion, impact analysis, rule replay, bounded work creation, and staleness management.
7. **Agent tools** — get applicable rules, inspect entities, propose correspondences, submit deltas, attach evidence, and request decisions.
8. **Human views** — system map, dossier, traceability, health, diff, decision queue, risks, and migration plan.
9. **CLI/API and export formats** — model-independent operation, automation, and inspectable repository artifacts.

KodePorter need not own an IDE or agent runtime. It should integrate with existing tools through stable interfaces. The persistent map and workflow are the product center.

**Positioning: KodePorter as a product and service** *(steward direction 2026-07-11, superseding the same-day narrower note)*:

The full realization is a product/service/tool. Someone wanting to port — C# to Rust, Rust to C#, and pairs beyond — finds the KodePorter site and gets what they need to set up a porting system: **one-shot** (the map serves as scaffolding during the port and is **sealed** at completion — archived under a declared query contract, so the deliverable is the port plus its receipt; reopening a seal upgrades it to a tracked port) or **tracked** (maintainable, long-term, continuously preserved). Delivery is an **installable kit plus knowledge site**: CLI, agent skills, and templates that users run in their own environment with whatever orchestrator they choose; the site is the storefront and knowledge hub — guidance, setup paths, and the flagship showcases with their live Atlases.

KodePorter is **two layers, one product**:

1. **The representation layer** — the map, its typed imperfections (unknown, candidate, thin, stale, conflicting, absent-with-kind), and the tools and operations that keep it honest. This is the system of record any orchestrator works against: it is read for orientation and direction (work queues, bounded context, acceptance gates) and written through proposals, evidence, and decisions. It must remain fully usable by an orchestrator that has never seen the guidance that built it.
2. **The skills & guidance layer** — the service's accumulating knowledge of porting subtleties and of how agents are most effectively managed to perform, maintain, prove, and support porting projects. Written orchestrator-neutral: the anti-lock-in principle applies to guidance as much as to data.

The boundary between them is internal discipline, not a product boundary: guidance never leaks into the representation as prose. Lessons about porting correctly are absorbed into the representation as schema and affordances (a typed property, a standing query, a gate policy) wherever possible; what remains is guidance, versioned and governed like everything else.

**The service learns, on the record.** A growing **flagship corpus** of open-source porting projects — many, of mixed depth and increasing variety, a number of them maintained deeply — exists to gather signal: porting subtleties and agent-coding signal alike (which methods perform, in the low-cost/high-compliance sense). Everything KodePorter builds, learns, is instructed, is corrected, and is aligned over time is managed in **Gneiss at three tiers**: per-port ledgers; the **KodePorter meta-ledger** (porting knowledge, transformation rules, method-skill records — flagships feed it, projects import pinned knowledge from it); and the **FireHorseCoding governance ledger** (the projects' own decisions and redirections — the meta-meta level). Whether flagship ports are *published* as consumable artifacts is deferred until there is a real decision to make.

**Open source:** the full porting stack, knowledge base, and everything related is open source (MIT) for now. Private/closed porting projects are served by the same installable, local-first stack — no hosted dependency, no phone-home.

## 15. First implementation

The first prototype is a thin, complete KodePorter-on-Gneiss cell, not an isolated graph platform.

It should:

1. pin a small Rust source and C# target to explicit commits and analyzer versions;
2. import read-only structural maps;
3. select one meaningful migration unit;
4. create its dossier and behavioral contract;
5. record one or more typed correspondences, a strategy, open questions, and an intended divergence if applicable;
6. attach precise source references and a differential or otherwise independent verification run;
7. let an agent propose a bounded implementation or mapping delta;
8. accept or reject it through an explicit decision;
9. display the current dossier and port health with context, `why`, and consumed-set label;
10. advance the source by one commit and show the affected cone and stale conclusions;
11. correct one earlier claim without erasing history;
12. reproduce the old and new views;
13. run a deliberately narrow archive/amnesia drill and return insufficiency outside the seal's contract.

This is the Alan Kay week test: small enough to reveal the real kernel, complete enough that no project can hide behind infrastructure progress.

The first demo is successful when an engineer can answer:

- What are we porting here?
- What must stay the same, and what may differ?
- Why is this unit considered complete or incomplete?
- Which exact source, rules, tests, and decisions produced that answer?
- What became stale when the source changed?
- What does the system know that it does not know?

## 16. Benchmarks and research

After the small controlled cell, KodePorter should test increasingly difficult shapes. These benchmarks are the beginning of the **flagship corpus** (§14 positioning): a widening set of open-source ports of mixed depth, maintained as the service's signal-gathering instrument and public showcase.

### FrankenTui / FrankenTui.NET

This partial Rust-to-C# port is the first brownfield bootstrap benchmark. KodePorter should reconstruct both maps, infer and review correspondences, reconcile status documents and tests, distinguish faithful depth from scaffolding, preserve existing improvements, and establish an honest new synchronization basis without modifying either repository during discovery.

### TypeScript to Go

The native TypeScript port is a priority observational case. Its public code, tests, parity tracking, intentional changes, parallel implementations, and performance goals should be mined for domain nouns, decomposition, correspondence practice, and preservation methods.

### Controlled language pairs

A small Rust-to-C# port and a separate C#-to-Rust port should compare:

- text-only generation;
- analyzer-assisted generation;
- structured target edits;
- and generate-then-round-trip hybrid workflows.

The experiment asks whether the KodePorter map improves correctness, coherence, reviewability, traceability, investigation speed, justified confidence, and later change propagation—not merely whether a model can emit compiling code.

## 17. Non-goals

KodePorter is not initially:

- a universal transpiler;
- a promise of perfect bidirectional synchronization;
- a replacement for rust-analyzer, Roslyn, compilers, build tools, Git, or test frameworks;
- a universal language-neutral intermediate representation;
- an IDE that must own all coding work;
- a proprietary multi-agent runtime;
- a requirement to model every line as a knowledge claim;
- a graph visualization without operational workflows;
- a dashboard that manufactures one confidence percentage;
- a method for preserving every source bug by default;
- or a license for autonomous agents to rewrite repositories without bounded authority.

KodePorter may later support refactoring, framework replacement, dependency removal, architecture recovery, system consolidation, and continuous modernization. Those extensions must first prove that the porting model generalizes; they should not blur the initial product before it works.

## 18. Preserving conceptual integrity

### 18.1 One design center

Every major feature must strengthen the living port map or the workflows that create, verify, and preserve it. Generic knowledge features belong in Gneiss. Generic agent orchestration belongs in agent infrastructure. Language semantics belong in semantic providers.

### 18.2 Layered authority

Design authority descends in this order:

1. this charter;
2. KodePorter domain invariants and canonical workflows;
3. the domain schema and Gneiss binding contract;
4. provider and tool interfaces;
5. implementation architecture;
6. optimization and presentation choices.

A lower layer may not redefine what a migration unit, correspondence, divergence, acceptance, or health view means.

### 18.3 Named design stewardship

Each milestone should have one named steward responsible for the coherence of the domain model and overall user story. Contributions may come from many people and agents; acceptance remains attributable. Dissent and alternatives are recorded so that coherent direction does not become undocumented authority.

### 18.4 Domain-first change test

A proposed core concept must answer:

- Which real porting problem requires it?
- Why is an existing object, relation, stance, or view insufficient?
- Does it belong in KodePorter, Gneiss, or a language provider?
- How does it affect existing maps and histories?
- What canonical example and conformance test demonstrate it?
- Can a veteran porter understand the noun without learning the implementation?

### 18.5 Executable canonical story

The initial vertical slice becomes a permanent golden fixture. Later releases must continue to reconstruct its old and current views, explain acceptance, detect the source delta, preserve correction history, and handle archive insufficiency honestly.

Brownfield FrankenTui fixtures then protect bootstrap behavior. Controlled source-delta sequences protect continuous preservation.

### 18.6 Complexity budget

Do not create a universal IR, workflow engine, graph platform, or new generic claim type to solve a local case. Prefer provider-specific facts, domain-specific objects, and Gneiss stances at their proper layers. Every new first-class noun must earn its permanent teaching and migration cost.

## 19. Operational qualities

### Repository safety

Discovery is read-only by default. Dirty state, branches, submodules, generated files, baselines, and uncommitted work are inventoried before modification. KodePorter must preserve user work and avoid destructive Git operations.

### Reproducibility

Maps, dossiers, verification, and health views pin repositories, dependencies, analyzers, environments, policies, and evidence. Regeneration against a new basis creates a new view rather than silently changing the old one.

### Security

Porting often involves proprietary, obsolete, or security-sensitive systems. Agent access, secrets, network boundaries, code retention, model-provider exposure, generated dependencies, execution sandboxes, and audit records are explicit policies.

### Scale

The model must eventually handle large repositories and machine-rate analyzer facts without forcing every fact through the durable claim path. Dense indexes may be disposable and recomputable; important judgments remain stable. Incremental impact and query performance are optimized against measured envelopes.

### Portability

The core domain model must not depend on one language pair, model vendor, IDE, graph database, or Gneiss implementation binding. Provider interfaces and canonical exports should make the accumulated map durable.

## 20. Success, failure, and kill criteria

KodePorter succeeds when it makes a serious port easier to understand, safer to continue, and cheaper to preserve than a collection of coding-agent sessions, issue tickets, status documents, and ad hoc scripts.

Evidence of success includes:

- a newcomer or replacement agent can orient from the map and dossiers without repeating foundational investigation;
- source changes produce bounded, explainable impact cones;
- accepted target behavior is linked to explicit criteria and independent evidence;
- many-to-many redesign remains traceable;
- intentional divergence is visible and does not look like an omission;
- port-health views distinguish mapped, implemented, verified, stale, and unknown;
- reusable rules improve later ports without hiding local exceptions;
- the target can become more native without losing behavioral accountability;
- KodePorter-specific workflows remain clear while Gneiss stays largely invisible to ordinary users.

The approach should be reconsidered or killed if:

- maintaining the map costs more than the repeated investigation it prevents;
- users treat dossiers and decisions as documentation chores detached from executable work;
- source-delta impact remains too noisy to guide action;
- generated mappings create confidence theater rather than reviewable structure;
- file and symbol identity cannot survive ordinary repository evolution;
- the domain model collapses into generic nodes and edges with prose labels;
- agents bypass the system because its bounded interfaces remove too much useful context;
- verification cannot be connected to meaningful claims;
- or KodePorter becomes an inferior replacement for existing compilers, IDEs, and workflow tools.

## 21. Open questions protected by this charter

The charter intentionally leaves open:

- the first implementation language and storage technology;
- the final stable identity scheme across refactors;
- the division between regenerable map data and durable Gneiss-backed claims;
- the smallest useful set of equivalence criteria and warrant types;
- the initial Rust and C# provider interfaces;
- whether the primary user experience is browser, IDE, CLI, or a combination;
- the exact rule language for adaptations and continuous propagation;
- how health roll-ups should be calibrated;
- how much behavioral understanding can be generated safely;
- the practical boundary between automatic acceptance and human review;
- and when, if ever, bidirectional synchronization is worth its complexity.

These are prototype questions. They may change without changing KodePorter's definition or design center.

## 22. Founding commitment

KodePorter will treat a port as a living, inspectable relationship between changing systems. It will keep source and target structure, behavior, intent, mappings, decisions, and evidence connected at the resolutions where they matter. It will make uncertainty and divergence explicit, separate generation from acceptance, and use executable evidence to build justified trust.

It will use Gneiss for epistemic accountability without turning porting into generic knowledge management. It will use compilers, analyzers, Git, tests, and agents without trying to replace them. It will begin with one complete migration unit and grow only where real ports demand more.

That is the system we intend to build and preserve.
