# KodePorter  
## Brainstorming and Project-Framing Notes

**Status:** Supporting project-framing record  
**Purpose:** Preserve the substantive ideas from the initial discussions and the exploration behind the governing [KodePorter Charter](../CHARTER.md).

This is an edited synthesis of the original KodePorter discussions. It deliberately keeps the ambitious ideas visible while identifying a practical first build. It is not a requirements specification; [CHARTER.md](../CHARTER.md) now governs project scope, conceptual architecture, and co-evolution with Gneiss.

---

## 1. The starting intuition

KodePorter begins with a simple observation:

> Porting a substantial software system is not mainly a matter of translating source code from one language into another. It is a programme of discovery, interpretation, decision-making, reconstruction, and verification.

Modern coding models make it possible to imagine code migrations that would previously have been prohibitively expensive. They can read unfamiliar code, generate translations, write tests, explain dependencies, and operate across large repositories. But a large port still cannot be reduced to asking a model to convert files one at a time.

The difficult parts are usually elsewhere:

- understanding what the system actually does;
- recovering architectural intent that is only partially represented in the code;
- distinguishing accidental implementation details from required behaviour;
- deciding which properties must be preserved and which should be redesigned;
- sequencing changes so that the system remains understandable and testable;
- maintaining traceability between the old system and the new one;
- dealing with gaps, ambiguity, obsolete assumptions, and undocumented behaviour;
- accumulating confidence that the new system is genuinely equivalent where it needs to be;
- and preserving the knowledge developed during the migration so that the work does not dissolve into thousands of disconnected model interactions.

The project is therefore potentially much broader than a code translator. It may be better understood as a **workspace, knowledge system, and operating method for software-porting projects**.

---

## 2. A provisional project thesis

A useful working thesis is:

> KodePorter should maintain a persistent, hierarchical map of a port: the source and target structures, their correspondences, the rules and adaptations that connect them, the evidence that supports them, and the state of the port as both codebases evolve.

This suggests a system that supports at least six intertwined activities:

1. **Map the source repository and its available semantic structure**
2. **Define the direction, policies, constraints, and desired fidelity of the port**
3. **Express correspondences and rules at several levels of abstraction**
4. **Bootstrap and improve the target implementation**
5. **Propagate later source changes and preserve the port over time**
6. **Measure, verify, diagnose, and explain port health**

The key product may not be the generated code alone. It may be the continually evolving body of structured knowledge that connects:

- source artefacts;
- structural hierarchies and overlay graphs;
- recovered behaviour;
- architectural concepts;
- migration decisions;
- correspondence rules, adaptations, and known divergences;
- generated target artefacts;
- tests and runtime evidence;
- synchronisation state and deltas;
- rolled-up port-health metrics;
- open questions;
- and confidence assessments.

That body of knowledge becomes the memory and control surface of the porting project.

### 2.1 The big ambition and the practical first move

The ambitious form of KodePorter is a long-lived, agent-ready control system for software that exists in more than one implementation world. It should make large ports replayable, queryable, explainable, progressively trustworthy, and maintainable as their source systems change. The ported code is an important product, but the compounding asset is the reusable map, rule library, and accumulated porting knowledge.

The first useful build need not wait for complete programme comprehension. Its first job can be to create a durable **repository and semantic map** from source trees, build metadata, compiler and analyser services, tests, and agent observations. Deeper behavioural understanding can be added where the port demands it. Mapping and porting should be mutually reinforcing activities, not two phases separated by an unrealistic gate.

### 2.2 Scope discipline within the ambition

KodePorter will be the first real system built on **Gneiss**, the sibling project in this repository. Gneiss is the reusable substrate for maintaining claims, evidence, decisions, provenance, revision, evaluation contexts, and reproducible current views. That substrate is not the KodePorter product. KodePorter should foreground a domain-specific vocabulary and persistent representation for ports: structural objects, correspondences, adaptations, divergences, policies, transformations, verification, deltas, and health.

It is also not primarily an IDE, a proprietary agent runtime, a universal intermediate representation, or a one-shot transpiler. Existing coding agents, compilers, analysers, and build tools should remain replaceable participants behind stable tools and interfaces. The orchestration should live as much as possible in the versioned domain model and work state rather than in model-specific prompts.

### 2.3 KodePorter on Gneiss

The initial architecture should be a non-interfering sidecar. Git remains authoritative for code; language analysers, builds, and test runners remain authoritative for their mechanical outputs; KodePorter owns the porting model and workflow; Gneiss keeps the important claims, evidence, decisions, corrections, and answer receipts behind them.

This changes the implementation boundary, not the ambition:

- KodePorter should not build a second generic ledger, belief engine, provenance model, context system, or revision protocol.
- KodePorter should concentrate on the structural pyramid, port policies, migration units, behavioural dossiers, correspondences, adaptations, divergences, delta impact, continuous preservation, and port-health views.
- KodePorter objects keep stable domain identities; important statements about them use Gneiss's epistemic machinery.
- Porting language should remain visible to users. An engineer should meet “migration unit,” “behavioural contract,” “correspondence,” and “intentional divergence” before needing to understand Gneiss internals.

The granularity rule is:

> **Index finely, claim selectively, and cite precisely. Record a claim at the granularity of a challengeable, reusable judgment; cite its evidence at the finest granularity needed to justify it.**

KodePorter may index every symbol and a compiler may point to a single line, but neither fact requires one durable epistemic assertion per line. A line becomes an evidence address for a behavioural, mapping, risk, or decision claim when it matters. The full boundary, first vertical slice, and archival implications are described in [Gneiss's KodePorter realization note](../../Gneiss/kb/37-KODEPORTER-REALIZATION.md).

---

## 3. Why this feels timely

There have always been tools for source translation, compiler construction, compatibility layers, binary translation, automated refactoring, schema migration, and legacy-system modernisation. What appears newly possible is the combination of:

- code models with broad language and framework knowledge;
- long-context analysis across repositories;
- agentic execution of build, test, and inspection tasks;
- automated creation of explanatory artefacts;
- cheap iteration over multiple candidate implementations;
- and a model-assisted ability to work with incomplete or messy systems.

This changes the economic boundary of what can be attempted. Projects that once required large specialist teams may become feasible for much smaller teams.

However, the availability of powerful code generation also creates a danger: it can make a migration look easier than it is. A system may produce a large quantity of plausible target code while quietly losing important behaviours, constraints, operational properties, or architectural meaning.

KodePorter should therefore not be built around the assumption that generation is the scarce resource. Generation may become abundant. The scarce resources may instead be:

- correct understanding;
- good decomposition;
- explicit decisions;
- high-quality evidence;
- disciplined verification;
- and project-wide coherence.

---

## 4. What counts as “porting”?

The project should use a deliberately broad notion of porting.

A port may involve one or more of the following:

- changing programming language;
- changing runtime or operating system;
- moving from desktop to web;
- moving from monolith to services, or the reverse;
- replacing a framework;
- changing processor architecture;
- moving from proprietary to open-source infrastructure;
- replacing an unavailable library or platform service;
- adopting, auditing, and completing an existing partial port whose history and quality are uneven;
- modernising a mainframe or minicomputer application;
- reimplementing undocumented behaviour;
- reconstructing a system from source plus binaries plus observed behaviour;
- preserving a protocol while replacing its implementation;
- moving from an obsolete build and deployment environment;
- extracting a reusable core from a platform-specific application;
- or redesigning a system while preserving selected external contracts.

These are not all the same problem. A central research task is to determine which concepts are common across them and which require specialised methods.

---

## 5. Porting is not one transformation

A simplistic model is:

```text
Source code → Target code
```

A more realistic model is closer to:

```text
Source artefacts
    ↓
Recovered structure, behaviour, and constraints
    ↓
Explicit migration decisions
    ↓
Target design
    ↓
Many staged transformations
    ↓
Tests, comparisons, and operational evidence
    ↓
Accepted target system
```

Even this is too linear. Real migrations move backwards and forwards:

- a failed target implementation reveals a misunderstood source behaviour;
- a newly discovered dependency changes the migration sequence;
- a test exposes an undocumented business rule;
- a performance problem forces a target-design change;
- an apparent bug is discovered to be behaviour on which users depend;
- a component thought to be independent turns out to rely on shared state;
- a generated translation is syntactically correct but architecturally wrong.

The natural representation may therefore be a **graph of claims, artefacts, transformations, evidence, and dependencies**, rather than a sequence of converted files.

---

## 6. The central object: a porting knowledge model

KodePorter may need an explicit model of the porting project.

This model would not merely mirror a source-code abstract syntax tree. It would connect several structural hierarchies and overlay graphs, with knowledge that can begin coarse and become more precise through porting work.

### 6.1 Source artefacts

Examples:

- repositories;
- projects and modules;
- source files;
- generated code;
- configuration;
- schemas;
- build scripts;
- deployment scripts;
- runtime dependencies;
- external services;
- binaries;
- documentation;
- issue history;
- tests;
- sample data;
- operational logs;
- user workflows.

### 6.2 Recovered concepts

Examples:

- components;
- responsibilities;
- domain entities;
- state machines;
- business rules;
- protocols;
- persistence assumptions;
- concurrency models;
- lifecycle rules;
- error semantics;
- security boundaries;
- performance expectations;
- user-visible behaviours;
- undocumented conventions.

### 6.3 Target concepts

Examples:

- target modules;
- target interfaces;
- new service boundaries;
- replacement libraries;
- target data models;
- compatibility layers;
- new deployment units;
- revised operational assumptions.

### 6.4 Mapping and transformation objects

Examples:

- “source component A becomes target component B”;
- “these five source types collapse into one target abstraction”;
- “this source behaviour is intentionally not preserved”;
- “this module is temporarily wrapped rather than rewritten”;
- “this API remains compatible until migration stage four”;
- “this generated implementation is provisional”;
- “this target test verifies these source observations.”

### 6.5 Evidence and confidence

Each important statement should ideally be connected to evidence.

Possible evidence includes:

- source-code references;
- documentation;
- tests;
- runtime traces;
- sample inputs and outputs;
- binary observation;
- user confirmation;
- issue reports;
- model analysis;
- comparison runs;
- benchmarks;
- or successful operation in a staging environment.

The system should distinguish between:

- known facts;
- strong inferences;
- tentative hypotheses;
- migration decisions;
- unresolved questions;
- and disproven assumptions.

This is important because large ports are full of uncertain knowledge. A useful tool should not flatten uncertainty into false certainty.

### 6.6 A pyramid of connected graphs

The initial discussion repeatedly returned to a **hierarchy, pyramid, or pyramid of graphs**. This should be a central design idea rather than a visualisation added later.

One useful vertical hierarchy runs from coarse to fine:

1. portfolios, organisations, and repository groups;
2. repositories, products, and deployable systems;
3. projects, crates, packages, services, and assemblies;
4. modules, namespaces, files, and types;
5. functions, methods, fields, tests, and declarations;
6. expressions, statements, semantic events, and assertions;
7. runtime, library, numeric, memory, ownership, and platform assumptions.

Several graphs can overlay that structural hierarchy:

- containment and build structure;
- call and control flow;
- type and data dependency;
- ownership, lifetime, concurrency, and resource flow;
- source-to-target correspondence;
- transformation and adaptation rules;
- verification and evidence;
- work status, review state, and risk.

These graphs are separate but connected. No single universal graph should be forced to represent everything. Rules declare the graph and level on which they operate. Refinement links connect a coarse rule to its more detailed cases. Metrics and health can roll upward while preserving the reasons behind the aggregate.

This gives KodePorter a multi-resolution language. A project may assert strong API-level equivalence while allowing implementation-level divergence. A package rule can apply uniformly to its descendants, with explicit exceptions. Coarse mappings can remain stable while fine details churn. Agents can be assigned a bounded layer and return structured deltas to the shared map.

The lowest useful unit should remain an empirical question. A candidate is a **semantic event or assertion in explicit execution context**, rather than a bare line of source text. Some important semantics, such as ownership or type constraints, do not correspond to an executable statement. Whatever atom is chosen must compose into the larger structural hierarchies and support context that can change equivalence across languages or runtimes.

### 6.7 Map first, deepen understanding where needed

KodePorter should not require a complete recovered architecture or behavioural model before useful work can begin. A repository map can be built progressively from relatively cheap and authoritative signals:

- source layout and version-control history;
- build graphs and package metadata;
- compiler and language-server symbols;
- resolved types and references;
- call, dependency, and test graphs;
- runtime traces where available;
- and agent-proposed concepts or correspondences.

This map is useful even while many nodes remain structurally known but semantically thin. Porting work then creates focused questions, mappings, rules, tests, and observations that enrich the relevant parts. The system should represent different depths of understanding without treating incompleteness as failure.

Existing language tools should be treated as semantic providers, not reimplemented. For an initial Rust and C# pairing, rust-analyzer and Roslyn can provide canonical language-specific structure behind a stable KodePorter interface. Experiments should compare text-only generation, analyser-assisted generation, structured target edits, and hybrid generate-then-round-trip workflows.

### 6.8 Theoretical vocabulary to preserve

The original discussion identified several bodies of theory that may give KodePorter precise language without dictating a heavyweight implementation:

- **Operational semantics and small-step versus big-step descriptions** distinguish internal execution detail from externally observable behaviour.
- **Simulation, bisimulation, and observational equivalence** describe different strengths of claims that one implementation preserves another. Equivalence should be typed, scoped, and selected as a policy, not asserted as a universal binary property.
- **Bidirectional transformations and lenses** frame synchronisation as change propagation between artefacts through an explicit correspondence model. KodePorter can support partial, asymmetric correspondences with gaps and obligations rather than promising perfect round trips.
- **Refinement theory and abstract interpretation** give language for relating fine and coarse representations, sound approximation, and the pyramid's refinement links.
- **Term rewriting and compiler transformation passes** suggest reusable rules and staged transformations, while KodePorter deliberately retains high-level intent that compilers often discard.
- **Institution theory and category-theoretic ideas** may later help describe mappings without privileging one language as fundamental, but should be treated as conceptual foundations rather than an implementation prerequisite.
- **Model-driven engineering and triple-graph-style correspondence models** are relevant to maintaining several representations and an explicit relationship among them.

A useful composition from the discussion is: a lens-like mechanism proposes how a change should propagate; a selected simulation, refinement, or equivalence criterion determines whether the resulting pair is acceptable; and the hierarchical graphs allow both mechanisms and acceptance criteria to differ by level.

### 6.9 Bootstrapping from an existing partial port

KodePorter must support a brownfield or **partial-port bootstrap** mode. Real projects may arrive with years of target code, incomplete mappings, abandoned approaches, copied tests, local redesigns, stale status documents, and a mixture of faithful, shallow, generated, and handwritten implementations. Starting over would destroy useful work and repeat old mistakes.

Bootstrap mode should:

1. establish the source and target repositories, branches, basis commits, build systems, and relevant history;
2. map both repositories independently before assuming any correspondence;
3. infer candidate links from paths, names, symbols, comments, tests, commit history, documentation, analyser data, and observed behaviour;
4. import existing ledgers and status claims as hypotheses to reconcile, not unquestioned truth;
5. classify target entities as faithful, adapted, intentionally divergent, shallow or shortcut, stale, target-only, missing, duplicated, conflicting, or unknown;
6. measure depth at the symbol, contract, test, and behaviour levels rather than treating file existence or line-count ratios as completion;
7. establish an honest build and test baseline, separating inherited failures from regressions introduced by new work;
8. identify competing implementations, API generations, deleted source surfaces, weakened tests, and uncommitted work before agents modify anything;
9. preserve existing target improvements and human decisions while turning unexplained differences into explicit obligations;
10. select a new synchronisation basis and begin continuous port monitoring from that known state.

The output is a **bootstrap dossier**: source and target inventories, candidate and confirmed correspondences, depth assessments, divergence and conflict ledgers, verification baselines, unresolved decisions, provenance gaps, and a ranked recovery plan.

This mode requires conservative repository handling. Discovery must be read-only. Existing dirty state must be inventoried and protected. Autonomous workers must not use destructive Git operations, weaken assertions merely to make tests pass, or overwrite a partial target wholesale. Verified checkpoints should be created at bounded milestones once the user chooses to begin remediation.

---

## 7. Tentative links and progressive confirmation

One potentially powerful idea is to allow the system to create **tentative relationships** before they are fully verified.

Examples:

- “This target class probably corresponds to these three source modules.”
- “This database column appears to encode the same concept as this enum.”
- “This error condition is likely equivalent to this exception path.”
- “This source callback may be serving as an implicit transaction boundary.”
- “This test seems to specify a business rule rather than an implementation detail.”

Such links could begin with a confidence level and accumulate supporting or contradicting evidence.

Confirmation might come from:

- a human reviewer;
- another model pass;
- successful test execution;
- runtime comparison;
- static analysis;
- a trace from the old system;
- or later work elsewhere in the repository.

This creates a more realistic knowledge process than requiring every relationship to be either absent or asserted as fact.

It also suggests that KodePorter could share ideas with:

- knowledge graphs;
- software traceability systems;
- nonmonotonic reasoning;
- provenance systems;
- issue trackers;
- scientific notebooks;
- and intelligence-analysis tools.

The relevant question is not whether KodePorter should literally become one of these systems, but what representational ideas it can borrow from them.

---

## 8. The migration unit is probably not the file

Files are convenient containers, but they may be poor units of planning.

A meaningful migration unit might instead be:

- a user-visible capability;
- a protocol;
- a domain concept;
- a state transition;
- a persistence boundary;
- a call graph slice;
- a vertical feature;
- an API contract;
- a subsystem;
- a buildable component;
- or a testable behavioural cluster.

The system may need to support multiple overlapping decompositions at once.

For example, the same source method may participate in:

- a business capability;
- a persistence workflow;
- a security boundary;
- and a performance-critical path.

A file-by-file conversion obscures these relationships. A graph-oriented project model could make them explicit.

---

## 9. Behaviour before implementation

One major theme should be the recovery of **behavioural contracts**.

For each migration unit, the system should help answer:

- What inputs are accepted?
- What outputs are produced?
- What state is read or modified?
- What ordering assumptions exist?
- What errors are raised?
- Which side effects occur?
- What timing or performance constraints matter?
- Which edge cases are relied upon?
- Which external systems are involved?
- What is observable to callers or users?
- Which behaviours are accidental and which are contractual?

This could lead to a “behaviour dossier” for each significant unit.

Such a dossier may combine:

- prose explanation;
- source references;
- generated tests;
- captured examples;
- traces;
- invariants;
- known deviations;
- and confidence notes.

The dossier would guide both the target implementation and its verification.

---

## 10. Preserve, emulate, replace, or redesign

A large port is made up of different transformation strategies. KodePorter should make these strategies explicit rather than treating everything as a rewrite.

Possible strategies include:

- **Mechanical translation**  
  Preserve structure closely and translate syntax or APIs.

- **Semantic reimplementation**  
  Recreate behaviour using target-native concepts.

- **Wrapping**  
  Keep a source component temporarily and expose it through a new interface.

- **Compatibility emulation**  
  Implement a source runtime, protocol, or API surface on the target platform.

- **Strangler migration**  
  Replace portions incrementally while old and new systems coexist.

- **Data-first migration**  
  Stabilise and translate schemas and persistence semantics before application logic.

- **Test-first reconstruction**  
  Recover behaviour into executable tests, then build a new implementation.

- **Architecture-preserving port**  
  Keep the conceptual structure while changing technology.

- **Architecture-changing modernisation**  
  Intentionally redesign structure, accepting that mapping becomes many-to-many.

- **Selective abandonment**  
  Drop obsolete or unused behaviour, with evidence and explicit approval.

A useful system should help select, combine, and revise these strategies.

---

## 11. Target-native versus source-shaped output

One of the central tensions is whether the target should resemble the source.

A source-shaped translation has advantages:

- easier traceability;
- simpler comparison;
- reduced initial semantic risk;
- possibly faster automation.

But it may produce poor target code:

- unnatural APIs;
- inappropriate threading models;
- obsolete patterns;
- excessive compatibility scaffolding;
- weak maintainability;
- or performance problems.

A target-native rewrite may be cleaner, but it increases interpretive risk.

KodePorter may need to support staged convergence:

1. establish behavioural equivalence;
2. preserve strong traceability;
3. then progressively refactor toward target-native design;
4. while retaining tests and links back to source intent.

This could be a core design principle: **separate the problem of preserving behaviour from the problem of improving architecture**, even when both happen within the same overall programme.

---

## 12. Verification as a first-class project

Verification cannot be a final phase added after generation. It must be interleaved with discovery and implementation.

Possible verification modes include:

- unit tests;
- golden-master comparisons;
- differential execution of old and new systems;
- property-based testing;
- trace comparison;
- database-state comparison;
- protocol replay;
- fuzzing;
- performance benchmarking;
- static equivalence checks;
- user-journey tests;
- visual comparison;
- manual expert review;
- shadow production execution;
- staged rollout evidence.

The system should record not just whether a test passed, but what claim the test supports.

For example:

```text
Claim:
Target parser preserves the source parser’s treatment of malformed header X.

Evidence:
- 47 captured source-system examples
- differential test suite
- one known intentional deviation
- reviewer confirmation
```

This turns test results into part of a broader evidence structure.

---

## 13. Continuous port preservation and project memory

The first port is a bootstrap. The enduring product is the ability to preserve and improve the port as the source evolves.

A **porting monitor** should be able to:

1. observe a source change;
2. project the delta onto the structural and correspondence graphs;
3. identify affected target nodes, rules, tests, and obligations;
4. apply deterministic rules where possible;
5. assign bounded work to agents where synthesis is needed;
6. validate the proposed target changes;
7. update the map, evidence, and port-health state;
8. escalate only decisions that need human judgement.

This is continuous port maintenance rather than continuous blind regeneration. One-way preservation is the initial goal: a Rust source may continuously feed a C# port, while a separate C# source may feed a Rust port. Editing both sides of the same correspondence is a more difficult later possibility and should not be assumed by the first design.

The hierarchical map should provide diagnostic port health at every level. It should distinguish at least:

- mapped coverage;
- implementation coverage;
- build and test coverage;
- behavioural verification coverage;
- quality or maturity of each correspondence;
- stale mappings after source deltas;
- unresolved obligations and human decisions;
- and confidence or trust, with its basis.

These dimensions can roll up from semantic units to types, packages, repositories, and whole ports without collapsing into one theatrical percentage.

Large migrations often rediscover the same facts repeatedly.

A developer investigates an obscure behaviour, makes a local decision, and moves on. Months later another developer or model encounters the same area without access to that reasoning.

KodePorter should act as persistent memory for:

- what was learned;
- how it was learned;
- which alternatives were considered;
- why a decision was made;
- what remains uncertain;
- which tests support it;
- what later evidence changed the conclusion.

This may be one of the strongest reasons for the project to exist independently of any particular coding model.

Models will change. Individual agent sessions will be transient. The project’s accumulated understanding must remain durable, inspectable, and model-independent.

---

## 14. A possible working environment

A future KodePorter workspace might contain several connected views.

### 14.1 System map

A navigable map of:

- source and target structural hierarchies;
- compiler and analyser entities;
- build, call, type, data, ownership, and test graphs;
- source-to-target correspondences at several resolutions;
- domain concepts where they have been recovered;
- transformation rules, adaptations, exceptions, and divergences;
- runtime and platform assumptions;
- migration and synchronisation status;
- and rolled-up port health.

### 14.2 Conversion dossiers

A dossier for each migration unit containing:

- source scope;
- recovered purpose;
- behavioural contract;
- dependencies;
- target plan;
- chosen strategy;
- generated artefacts;
- tests;
- open questions;
- evidence;
- confidence;
- review status.

### 14.3 Traceability graph

A graph connecting:

- source code;
- target code;
- tests;
- decisions;
- requirements;
- runtime evidence;
- issues;
- reviewers.

### 14.4 Migration plan

A dependency-aware sequence of work showing:

- prerequisites;
- blockers;
- temporary compatibility layers;
- parallelisable tracks;
- risk;
- estimated uncertainty;
- acceptance gates.

### 14.5 Agent workspace

A controlled environment in which models can:

- investigate a question;
- propose mappings;
- generate tests;
- implement target code;
- run builds;
- compare behaviour;
- and submit evidence-backed changes.

Agents should normally behave as bounded, replaceable workers over the shared map. They query applicable rules, request semantic context, propose correspondence or code deltas, and return verification results through tools. They should not own project memory or hide the process inside a long prompt. Every accepted delta should be attributable and replayable against versioned inputs and rules.

### 14.6 Review and decision queue

Humans should be able to focus on decisions that genuinely require judgement:

- ambiguous behaviour;
- architecture choices;
- intentional incompatibilities;
- risk acceptance;
- security-sensitive changes;
- difficult evidence conflicts.

---

## 15. Human and model roles

The project should avoid both extremes:

- treating the model as an autocomplete tool; and
- pretending the model can autonomously own an entire migration without supervision.

A more useful framing is a team with different kinds of agency.

Models may be especially strong at:

- repository exploration;
- cross-language explanation;
- pattern detection;
- draft translation;
- test generation;
- repeated comparison;
- documentation synthesis;
- proposing candidate mappings;
- identifying likely gaps.

Humans remain important for:

- defining acceptable outcomes;
- interpreting business meaning;
- resolving ambiguous intent;
- choosing architecture;
- evaluating risk;
- deciding what not to preserve;
- and accepting evidence.

KodePorter should make this division of labour explicit and support hand-offs between human judgement and automated investigation.

---

## 16. Questions about autonomy

A major design question is how autonomous a KodePorter agent should be.

Possible levels include:

1. **Analyst**  
   Reads and explains, but does not modify.

2. **Planner**  
   Proposes migration units, dependencies, and strategies.

3. **Implementer**  
   Produces target code and tests in a controlled branch.

4. **Investigator**  
   Runs experiments against source and target systems.

5. **Coordinator**  
   Selects work, delegates to sub-agents, and updates the project model.

6. **Migration operator**  
   Executes an approved plan through multiple stages.

The system may need policies determining:

- which actions require approval;
- what environments agents may access;
- how evidence is attached;
- how failed attempts are retained;
- how speculative conclusions are labelled;
- and what must be reviewed before merge or deployment.

---

## 17. A port as a set of claims

Another useful conceptual framing is to treat a migration as a structured set of claims.

Examples:

- “The target implementation preserves behaviour B.”
- “Component X has no callers outside subsystem Y.”
- “Schema field A can be removed.”
- “This source race condition is accidental and should not be reproduced.”
- “The new service boundary preserves transactional semantics.”
- “All uses of platform API P have been replaced.”
- “The new application can process historical dataset D without material deviation.”

Every claim can have:

- status;
- owner;
- supporting evidence;
- contradicting evidence;
- confidence;
- affected artefacts;
- review history;
- and acceptance criteria.

This may provide a more rigorous basis for progress than “percentage of files converted.”

---

## 18. Measuring progress

Traditional progress measures can be misleading.

Weak measures include:

- lines translated;
- files converted;
- commits produced;
- percentage of code generated.

More meaningful measures might include:

- percentage of identified capabilities with behavioural dossiers;
- percentage of critical claims supported by executable evidence;
- source-to-target traceability coverage;
- known unresolved ambiguities;
- differential-test coverage;
- number of source dependencies eliminated;
- migration units accepted;
- compatibility scaffolding remaining;
- production traffic handled by the target;
- confidence by subsystem;
- risk retired.

Progress may need to be shown in several dimensions rather than as a single number.

---

## 19. Important distinctions the project should preserve

### 19.1 Fact versus decision

“This code currently behaves this way” is different from “the new system should behave this way.”

### 19.2 Behaviour versus mechanism

A lock, callback, message queue, or database trigger may be a mechanism. The behaviour it creates may be what matters.

### 19.3 Required compatibility versus incidental similarity

Some external contracts must remain exact. Internal structure may not need to.

### 19.4 Unknown versus unimplemented

A missing target feature may be understood but unfinished, or it may still be poorly understood. These require different responses.

### 19.5 Generated versus accepted

Model output should not become equivalent to project truth merely because it compiles.

### 19.6 Passing tests versus sufficient evidence

A test suite can pass while important behaviours remain untested or misunderstood.

### 19.7 Temporary versus permanent architecture

Compatibility layers often begin as temporary and quietly become permanent. The system should make intended lifetime visible.

---

## 20. Historical research programme

Before defining the product too narrowly, we should study how major porting and modernisation programmes have actually been conducted.

The most valuable sources may not be product marketing or short migration blogs. We should look for:

- research papers;
- programme reports;
- technical retrospectives;
- government and defence reports;
- doctoral theses;
- conference proceedings;
- postmortems;
- standards;
- archived project documentation;
- and interviews with engineers who led long-running migrations.

Areas worth investigating include:

### 20.1 Mainframe and COBOL modernisation

These projects are likely to have developed mature ideas about:

- programme understanding;
- data semantics;
- dependency recovery;
- automated translation limits;
- coexistence strategies;
- regression testing;
- operational cutover;
- and institutional knowledge loss.

We should pay particular attention to projects that moved:

- COBOL to C, C++, Java, or modern managed runtimes;
- mainframe workloads to distributed systems;
- legacy databases to relational or cloud platforms;
- batch systems to online services.

### 20.2 Defence and government systems

Large government and defence organisations have repeatedly faced:

- very long-lived systems;
- obsolete hardware;
- safety and mission constraints;
- sparse documentation;
- security requirements;
- formal acceptance processes;
- and multi-year migration programmes.

Reports from these projects may contain exactly the kind of conceptual vocabulary and process knowledge that KodePorter needs.

Our discussion specifically raised the possibility that US Navy or related programmes may have documented major language or platform transitions. This should become a focused research thread rather than a vague example.

### 20.3 Automated language translation

We should study systems that attempted:

- source-to-source translation;
- compiler-assisted migration;
- binary translation;
- API substitution;
- architecture lifting;
- or semantics-preserving rewrite.

The key question is not merely whether the translation worked, but what surrounding machinery was required:

- annotations;
- manual intervention;
- test harnesses;
- intermediate representations;
- equivalence checks;
- staging;
- and post-translation cleanup.

### 20.4 Large open-source rewrites

Open-source projects may offer unusually rich evidence because design discussions, commits, issues, and test evolution are public.

Useful cases may include:

- major browser-engine changes;
- compiler rewrites;
- database-engine migrations;
- operating-system subsystem rewrites;
- language-runtime changes;
- major Python-to-Rust, C-to-Rust, Java-to-Kotlin, or JavaScript-to-TypeScript transitions;
- framework replacements;
- and long-running compatibility projects.

#### TypeScript to Go: TypeScript 7 / Project Corsa

The native TypeScript port is a priority case study because it is recent, public, large, and unusually close to the KodePorter problem. Microsoft is porting the TypeScript compiler and language-service toolset from the existing TypeScript/JavaScript implementation to Go while preserving compatibility, documenting intentional changes, running both lines during transition, and using performance and feature-parity evidence to judge progress.

Primary sources:

- [A 10x Faster TypeScript](https://devblogs.microsoft.com/typescript/typescript-native-port/) describes the motivation, parallel TypeScript 6 and native TypeScript 7 lines, and the intention to keep them closely aligned during adoption.
- [microsoft/typescript-go](https://github.com/microsoft/typescript-go) exposes the evolving implementation, feature-status matrix, tests, issues, intentional changes, and project history.
- [Announcing TypeScript Native Previews](https://devblogs.microsoft.com/typescript/announcing-typescript-native-previews/) discusses parity work, deliberate non-1:1 areas such as the language service, testing, and real-project performance.

Questions to study include:

- Which source structures were deliberately preserved, and which were redesigned for Go?
- How was work decomposed and tracked across compiler phases and language-service features?
- What served as the correspondence model, whether explicit or implicit?
- How were exact compatibility, intentional divergence, performance, and feature parity distinguished?
- How were the old and new implementations kept aligned while both continued to matter?
- What tools, tests, generated assets, and manual practices acted as the real porting infrastructure?
- Which project artefacts could have been represented more directly by a KodePorter pyramid of graphs?

#### FrankenTui to FrankenTui.NET: partial-port bootstrap

[FrankenTui.NET](https://github.com/govert/FrankenTui.Net) is a local, medium-sized Rust-to-C# port that should become KodePorter's first brownfield benchmark. It is valuable precisely because it did not begin with KodePorter: several agents and workflows produced a partially completed target, extensive tests and planning documents, uneven implementation depth, and a substantial transcript trail.

The historical material exposes recurring bootstrap problems:

- a good charter and coarse crate-to-project mapping existed before an honest fine-grained correspondence map;
- file presence and optimistic status language sometimes overstated semantic depth;
- some target files were faithful ports, while others were scaffolds, abbreviated implementations, duplicate generations, or locally redesigned surfaces;
- tests were both executable specifications and vulnerable to accidental weakening;
- an historical baseline recorded 2,815 passing and 134 failing headless tests, making inherited failures a first-class concern;
- upstream-sync, divergence, gap-register, snapshot, replay, and comparison documents evolved separately rather than as one queryable port model;
- autonomous work demonstrated both useful Scout/Port/Verify/Build loops and the danger of destructive repository actions.

The KodePorter experiment should ingest the current Rust source, the current C# target, both histories, compiler/analyser structures, tests, mapping and status documents, and selected archived agent transcripts. It should then produce a reconstructed correspondence graph and bootstrap dossier without changing either repository. The result can be compared against the manually maintained ledgers and used to choose a small, safe next porting wave.

### 20.5 Safety-critical and regulated software

These fields may offer valuable methods for:

- traceability;
- requirements linkage;
- evidence management;
- independent verification;
- change control;
- and staged acceptance.

KodePorter probably should not imitate heavyweight certification processes by default, but it may borrow useful ideas in lighter form.

### 20.6 Software archaeology and programme comprehension

There is likely an important research literature around:

- architecture recovery;
- concept assignment;
- feature location;
- dependency analysis;
- impact analysis;
- dynamic tracing;
- clone detection;
- and knowledge recovery from legacy systems.

This may supply established terminology and prevent us from reinventing known concepts.

---

## 21. What to extract from each case study

For each substantial migration case, the research should capture more than a narrative summary.

A standard extraction template could include:

### Project context

- What was being migrated?
- Why?
- What was the size and age of the system?
- What constraints made the migration difficult?

### Knowledge problem

- How did the team determine what the source system did?
- What documentation existed?
- How were undocumented behaviours found?
- How was domain knowledge recovered?

### Decomposition

- What units of migration were used?
- How were dependencies represented?
- How was sequencing decided?

### Transformation method

- Automated translation?
- Manual rewrite?
- Wrapping?
- Compatibility layer?
- Incremental replacement?
- Hybrid method?

### Existing target or partial-port state

- Did a target implementation already exist when the method began?
- Which source and target commits formed the actual comparison basis?
- Which mappings were documented, inferred, stale, or contradictory?
- How was faithful depth distinguished from a present-but-shallow file?
- Which local target improvements or intentional divergences had to be preserved?
- How were inherited build failures, test failures, duplicate generations, and uncommitted work handled?

### Verification

- What counted as evidence of equivalence?
- Were old and new systems run in parallel?
- How were tests created?
- How were edge cases and performance handled?

### Human organisation

- What roles existed?
- Where was expert judgement required?
- How was knowledge transferred?
- How were decisions recorded?

### Failure modes

- What went wrong?
- Which assumptions proved false?
- Where did automation fail?
- What caused cost or schedule overruns?

### End state

- Was the result source-shaped or target-native?
- How much compatibility scaffolding remained?
- Was the migration considered successful?
- What maintenance consequences followed?

### Implications for KodePorter

- Which concepts should be represented?
- Which workflow should be supported?
- Which kinds of evidence matter?
- Which tempting approaches should be avoided?

---

## 22. Candidate research questions

The next research phase should try to answer questions such as:

### Project model

- What is the best primary unit of a porting project?
- Can one model support language ports, framework migrations, and architectural modernisation?
- Is a graph representation essential, or would a document-and-work-item model be more usable?
- Which entities and relationships recur across real migrations?

### Understanding

- How can a model distinguish domain behaviour from implementation detail?
- How should recovered knowledge be represented when incomplete or contradictory?
- How can static analysis, runtime evidence, documentation, and human testimony be combined?

### Planning

- How should migration units be selected?
- Can dependency-aware sequencing be generated automatically?
- How should temporary bridges and coexistence states be represented?
- How can the system identify high-risk or high-uncertainty areas?

### Transformation

- When is direct translation appropriate?
- When should the system recommend wrapping, emulation, or redesign?
- Can target-native refactoring be separated cleanly from behaviour preservation?
- How should generated artefacts remain linked to their source rationale?

### Verification

- What evidence is sufficient for different classes of component?
- How can differential testing be generated automatically?
- How should known deviations be represented and approved?
- Can confidence be estimated meaningfully without becoming pseudo-precision?

### Human interaction

- What information does a reviewer need to make a good decision?
- Which questions should be escalated to humans?
- How can domain experts contribute without reading source code?
- How should the system present uncertainty?

### Product shape

- Is KodePorter primarily:
  - an IDE;
  - a repository service;
  - a project database;
  - an agent orchestration system;
  - a migration methodology;
  - or a combination?
- What is the smallest useful version?
- What would make it valuable before it can perform large autonomous migrations?

---

## 23. Possible prototype directions

The first prototype should not attempt end-to-end autonomous porting. It should test the core assumptions as a thin KodePorter-on-Gneiss vertical slice, rather than proving isolated infrastructure components separately.

### 23.1 Repository cartographer

Input:

- a source repository;
- build metadata;
- optional documentation.

Output:

- a versioned structural hierarchy from repositories to semantic units;
- build, call, type, dependency, ownership, and test overlays where available;
- stable identities for entities across source revisions;
- a query interface for agents and humans;
- initial health and coverage roll-ups;
- candidate domain concepts and migration units;
- and an uncertainty and investigation queue.

Question tested:

> Can the system create a useful project-level representation that is richer than a normal code index?

This is the preferred first technical component, but not a sufficient demo by itself. The first demo should carry one meaningful migration unit through cartography, a behavioural and correspondence claim, exact evidence, agent proposal, human acceptance, a labeled current view, a later source delta, and explicit staleness or revision. That end-to-end path tests both the KodePorter domain model and the Gneiss substrate without requiring complete programme understanding.

### 23.2 Conversion dossier generator

For a selected subsystem, generate:

- purpose;
- source scope;
- external contracts;
- behavioural hypotheses;
- dependencies;
- risks;
- candidate target designs;
- proposed tests;
- unresolved questions.

Question tested:

> Is a structured dossier a useful unit for human-and-model collaboration?

### 23.3 Source-target traceability graph

Given a small completed port, link:

- source entities;
- target entities;
- tests;
- migration decisions;
- and evidence.

Question tested:

> Can traceability remain understandable when mappings are many-to-many?

### 23.4 Differential behaviour harness

Run old and new implementations on shared inputs and record:

- matching behaviour;
- deviations;
- traces;
- state differences;
- confidence by behaviour class.

Question tested:

> Can verification be turned into a reusable first-class workflow?

### 23.5 Migration planner

Take a system map and target architecture, then propose:

- migration stages;
- prerequisites;
- compatibility layers;
- parallel tracks;
- acceptance gates;
- high-risk points.

Question tested:

> Can explicit dependency and evidence models improve migration planning?

### 23.6 Persistent agent notebook

Allow agents to perform investigations while recording:

- question;
- method;
- evidence;
- conclusion;
- confidence;
- affected artefacts;
- follow-up questions.

Question tested:

> Can project memory materially improve later agent performance and reduce repeated investigation?

### 23.7 Continuous port monitor

Given a small completed port and a sequence of later source commits:

- map each source delta onto affected correspondence nodes;
- identify rules that can replay automatically;
- generate bounded target changes;
- rerun relevant verification;
- update stale, blocked, and healthy regions;
- and produce a human review queue only where needed.

Question tested:

> Can the map make preservation of a port materially cheaper and more reliable than repeatedly re-porting changed code?

---

## 24. A promising smallest useful product

A possible first useful KodePorter product might not generate much target code at all. It can begin as a Gneiss-backed durable map on which later porting and preservation depend.

It could be a tool that:

1. ingests a source repository alone or an existing source-target pair;
2. creates a navigable multi-resolution repository and semantic map;
3. lets users define a target direction;
4. produces conversion dossiers;
5. manages questions, decisions, and evidence;
6. generates targeted tests;
7. tracks source-to-target links as implementation proceeds;
8. detects source deltas and marks affected target regions;
9. reports multi-dimensional port health at every level.

This would already address a real weakness in current model-assisted development: the absence of a durable project-wide representation of what the model has learned and why.

Code generation can then be added inside a structure that makes it safer and more coherent. The map should grow through the act of porting rather than waiting for exhaustive understanding beforehand.

---

## 25. Data model ideas

A first-pass conceptual model might contain entities such as:

- `Artefact`
- `SourceEntity`
- `TargetEntity`
- `Concept`
- `Capability`
- `Behaviour`
- `Contract`
- `Dependency`
- `MigrationUnit`
- `Mapping`
- `Transformation`
- `Decision`
- `Question`
- `Hypothesis`
- `Claim`
- `Evidence`
- `Test`
- `Observation`
- `Deviation`
- `Risk`
- `Review`
- `Milestone`
- `AgentRun`
- `SourceBasis`
- `TargetBasis`
- `BootstrapAssessment`
- `CorrespondenceCandidate`
- `DepthAssessment`
- `InheritedFailure`
- `Conflict`
- `StaleMapping`

Potential relationships include:

- implements;
- depends on;
- calls;
- reads;
- writes;
- corresponds to;
- replaces;
- preserves;
- intentionally differs from;
- supports;
- contradicts;
- verifies;
- was derived from;
- is blocked by;
- requires review by;
- is provisional until.

This list should remain provisional until grounded in case-study research and prototype experience.

---

## 26. Versioning and changing knowledge

The project model must support revision.

During a migration:

- hypotheses become facts;
- accepted facts are later disproved;
- target architecture changes;
- one migration unit splits into three;
- a temporary workaround becomes unnecessary;
- a test is discovered to encode the wrong behaviour;
- a source bug is reclassified as a required compatibility feature.

Therefore, the system should retain history and provenance rather than silently overwriting conclusions.

This suggests:

- versioned claims;
- superseding relationships;
- decision history;
- evidence timestamps;
- model-run provenance;
- and the ability to explain why the current view differs from an earlier one.

---

## 27. Build and runtime integration

A credible KodePorter system must connect to executable reality.

It should eventually be able to interact with:

- source and target builds;
- test runners;
- package managers;
- containers or virtual machines;
- databases;
- debuggers;
- profilers;
- tracing systems;
- deployment environments;
- binary-analysis tools;
- and possibly UI automation.

The knowledge model should not become a detached documentation layer. It should be continuously informed by actual builds, executions, failures, and comparisons.

---

## 28. Security and containment

Autonomous or semi-autonomous migration work may require access to sensitive repositories and executable systems.

Questions include:

- How are secrets isolated?
- What actions can agents perform?
- How are network and filesystem boundaries controlled?
- Can source and target environments be treated as untrusted?
- How are generated dependencies vetted?
- How are model prompts and outputs retained?
- Can sensitive source code be kept within a controlled model environment?
- How are destructive migrations prevented?
- How is every material action audited?

Security should be part of the architecture from the start, especially if the eventual product is used for proprietary legacy systems.

---

## 29. Failure modes to guard against

Likely dangers include:

### Plausible but false understanding

The model produces a coherent explanation that is not actually supported by the system.

### Local correctness, global incoherence

Individual conversions look good but do not fit a consistent target architecture.

### Test laundering

Generated tests merely reproduce the generated implementation rather than independently recovering source behaviour.

### Silent loss of edge cases

Common paths work while unusual but important behaviour disappears.

### Accidental permanence

Temporary wrappers and compatibility layers become the final architecture.

### Translation-shaped technical debt

The target compiles but remains structurally trapped in the source platform.

### Confidence theatre

Numbers and dashboards imply certainty that the evidence does not justify.

### Context fragmentation

Each agent understands its local task but no durable project-wide understanding exists.

### Over-automation

The system continues generating work where the real need is an unresolved human decision.

### Under-specification of intent

The project preserves everything because nobody explicitly decided what should change.

---

## 30. Principles that may guide the project

These are provisional, but they capture the direction of the discussion.

1. **Mapping can begin before understanding is complete.**
2. **Behaviour is more important than syntax.**
3. **Uncertainty should be represented, not hidden.**
4. **Important claims should be connected to evidence.**
5. **The hierarchy and pyramid of graphs are core domain infrastructure.**
6. **The migration unit should be chosen for meaning and verifiability, not convenience.**
7. **Traceability must survive architectural redesign.**
8. **The first port is bootstrap; preservation is the continuing operation.**
9. **Generation is not acceptance.**
10. **Verification is continuous, not final.**
11. **Temporary architecture should be explicitly temporary.**
12. **Human judgement should be concentrated where it adds the most value.**
13. **Project memory must outlive individual model sessions.**
14. **Agents should be replaceable workers over a durable, versioned map.**
15. **The system should support incremental success rather than requiring a heroic big-bang rewrite.**
16. **The target should eventually become native to its new environment.**
17. **The project model should remain connected to actual executable evidence.**

---

## 31. Suggested next research phase

The next phase should be a structured deep-research exercise with three parallel tracks.

### Track A: Historical and industrial evidence

Find and analyse substantial migration programmes, especially:

- mainframe and COBOL modernisation;
- defence and government ports;
- automated language conversions;
- the TypeScript-to-Go native port for TypeScript 7;
- major open-source rewrites;
- safety-critical migrations;
- long-lived compatibility projects.

Goal:

> Recover the concepts, artefacts, workflows, and failure modes that experienced migration teams found important.

### Track B: Relevant research fields

Survey literature on:

- programme comprehension;
- software archaeology;
- architecture recovery;
- feature location;
- traceability;
- knowledge graphs for software engineering;
- automated program transformation;
- semantic equivalence;
- differential testing;
- migration planning;
- and human-AI software engineering.

Goal:

> Identify established methods and vocabulary that should shape KodePorter.

### Track C: Current AI coding systems

Study how current coding agents handle:

- repository-scale understanding;
- long-running tasks;
- persistent memory;
- planning;
- tool execution;
- test generation;
- cross-language translation;
- and multi-agent coordination.

Goal:

> Determine what can already be delegated to models and where a specialised KodePorter layer is still necessary.

---

## 32. Deliverables from the research phase

The research should produce:

1. **A case-study catalogue**  
   A concise but structured set of major migration examples.

2. **A concept glossary**  
   Terms used across legacy modernisation, programme comprehension, verification, and transformation.

3. **A common migration model**  
   Candidate entities, relationships, stages, and evidence types.

4. **A failure-mode catalogue**  
   Repeated causes of unsuccessful or unnecessarily expensive ports.

5. **A design implications document**  
   What the evidence suggests KodePorter should and should not become.

6. **A prototype recommendation**  
   The smallest experiment that will test the most important assumptions.

7. **A benchmark corpus proposal**  
   Candidate small and medium systems that can be ported experimentally with observable ground truth.

---

## 33. Candidate experimental projects

A useful benchmark set should include different migration shapes.

Possible categories:

- the existing FrankenTui Rust-to-C# partial port, used to test read-only brownfield bootstrap and map reconstruction;
- a Rust-to-C# port and a separate C#-to-Rust port, using rust-analyzer and Roslyn as semantic providers;
- a small library translated language-to-language;
- an application moved between UI frameworks;
- a network service moved between runtimes;
- a data-heavy legacy application with schema changes;
- a system with sparse tests but observable behaviour;
- a system with strong tests and architectural change;
- a component requiring a compatibility layer;
- a project where target-native redesign is clearly preferable.

The TypeScript-to-Go port should be studied as an observational benchmark: reconstruct its implicit project map, correspondence structure, parity criteria, intentional divergences, and preservation workflow from the public repository and reports.

The point is not to demonstrate that a model can generate code. It is to test whether the KodePorter method and knowledge model improve:

- correctness;
- coherence;
- reviewability;
- traceability;
- speed of investigation;
- and confidence.

---

## 34. Product questions still open

We have not yet decided:

- whether KodePorter should be local-first, server-based, or hybrid;
- whether its primary interface should be an IDE, browser application, graph view, or document workspace;
- how much formal structure users will tolerate;
- how much of the project model can be generated automatically;
- whether the system should own code changes or integrate with existing coding agents;
- how evidence should be scored;
- whether a graph database is necessary;
- how to handle very large proprietary systems;
- what level of model independence is realistic;
- and where the boundary lies between a methodology and a software product.

These should remain open until research and prototypes provide stronger evidence.

---

## 35. The deeper ambition

The ambitious version of KodePorter is not merely a tool for moving code once.

It is a system for making a complex software transformation:

- legible;
- decomposable;
- collaborative;
- evidence-backed;
- reversible where possible;
- progressively trustworthy;
- and maintainable as all participating systems continue to evolve.

Its characteristic object is a persistent pyramid of connected graphs: structural maps, semantic relationships, correspondences, transformation rules, verification, health, and history at multiple resolutions. Its characteristic operation is continuous port preservation. Its characteristic labour model is replaceable agents consulting and updating that durable representation.

That same underlying machinery might later support more than ports:

- large refactorings;
- framework replacement;
- system consolidation;
- architecture recovery;
- dependency removal;
- security remediation;
- protocol replacement;
- or continuous modernisation.

Porting is a particularly good proving ground because it exposes the full problem: mapping an existing system, recreating it under changed constraints, and keeping the resulting implementation aligned as the source evolves. This may later support a broader form of software knowledge continuity, but KodePorter should first make that ambition concrete in the porting domain.

---

## 36. Working summary

KodePorter should be explored as a **project-scale map and control system for creating and continuously preserving software ports**.

Its possible core is a durable model that connects:

```text
Source and target structural hierarchies
    ↕
Overlay graphs and multi-resolution correspondences
    ↕
Versioned rules, adaptations, and port policies
    ↕
Agent work and target implementations
    ↕
Verification, health, deltas, and continuous preservation
```

The immediate task is not to rush into building a universal translator or to wait for complete deep understanding. It is to build a useful repository map, learn the domain vocabulary needed to express real ports, and test whether that map improves port construction and later change propagation.

Research into major ports, especially TypeScript to Go and well-documented legacy modernisations, should feed the map and data model directly. The smallest useful system should already help a human-and-AI team orient, coordinate, measure, and preserve porting work better than a collection of coding-agent conversations and ad hoc documents.

---

## 37. Immediate next steps

1. Define the minimum KodePorter domain schema—source and target bases, structural entities, migration units, behavioural contracts, correspondences, adaptations, divergences, obligations, and verification—and bind its important statements to Gneiss identities and claims.
2. Select one small Rust-to-C# migration unit and build the complete KodePorter-on-Gneiss vertical slice: pinned source and target, map, dossier, evidence, proposal, review, labeled current view, source delta, staleness, and correction.
3. Settle which dense analyser outputs remain regenerable organ data, which mappings are KodePorter domain state, and which judgments are promoted into the Gneiss ledger.
4. Prototype read-only repository cartography and partial-port bootstrap on the FrankenTui Rust and FrankenTui.NET C# pair after the small slice establishes the model.
5. Turn this note into a focused research brief, with the TypeScript-to-Go port as the first priority case study.
6. Identify ten to twenty additional serious migration cases, favouring detailed reports, repositories, tests, and decision history over promotional summaries.
7. Mine those cases for domain nouns, persistent objects, relationships, rules, invariants, metrics, and preservation practices.
8. Test text-only, analyser-assisted, structured-edit, and hybrid workflows on a small Rust-to-C# port and a separate C#-to-Rust port.
9. Prototype a correspondence graph, port-health roll-up, and continuous port monitor over a sequence of source changes.
10. Use the experiments to refine the project thesis, domain model, and Gneiss boundary without shrinking the long-term ambition.

---

*This document is a brainstorming synthesis, not a settled specification. Its purpose is to preserve the shape of the discussion, make the implicit ideas explicit, and create a strong starting point for research and prototype design.*
