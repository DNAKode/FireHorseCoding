# KodePorter  
## Brainstorming and Project-Framing Notes

**Status:** Working ideas note  
**Purpose:** Capture the conceptual shape of the project and provide a starting point for deeper research, design discussion, and prototyping.

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

> KodePorter should help a human-and-AI team conduct an entire software migration as an evidence-backed, inspectable, incrementally verified transformation.

This suggests a system that supports at least five intertwined activities:

1. **Understand the source system**
2. **Design the target system**
3. **Map source concepts to target concepts**
4. **Carry out the transformation**
5. **Verify and explain the result**

The key product may not be the generated code alone. It may be the continually evolving body of structured knowledge that connects:

- source artefacts;
- recovered behaviour;
- architectural concepts;
- migration decisions;
- generated target artefacts;
- tests and runtime evidence;
- open questions;
- and confidence assessments.

That body of knowledge becomes the memory and control surface of the porting project.

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

This model would not merely mirror the source-code abstract syntax tree. It would represent several layers of knowledge.

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

## 13. The importance of project memory

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

- source components;
- domain concepts;
- dependencies;
- runtime boundaries;
- target components;
- migration status.

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

The first prototype should probably not attempt end-to-end autonomous porting. It should test the core assumptions.

### 23.1 Repository cartographer

Input:

- a source repository;
- build metadata;
- optional documentation.

Output:

- component map;
- dependency map;
- candidate domain concepts;
- migration-unit suggestions;
- uncertainty and investigation queue.

Question tested:

> Can the system create a useful project-level representation that is richer than a normal code index?

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

---

## 24. A promising smallest useful product

A possible first useful KodePorter product might not generate much target code at all.

It could be a tool that:

1. ingests a repository;
2. creates a navigable architectural and behavioural map;
3. lets users define a target direction;
4. produces conversion dossiers;
5. manages questions, decisions, and evidence;
6. generates targeted tests;
7. tracks source-to-target links as implementation proceeds.

This would already address a real weakness in current model-assisted development: the absence of a durable project-wide representation of what the model has learned and why.

Code generation could then be added inside a structure that makes it safer and more coherent.

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

1. **Understanding precedes transformation.**
2. **Behaviour is more important than syntax.**
3. **Uncertainty should be represented, not hidden.**
4. **Important claims should be connected to evidence.**
5. **The migration unit should be chosen for meaning and verifiability, not convenience.**
6. **Traceability must survive architectural redesign.**
7. **Generation is not acceptance.**
8. **Verification is continuous, not final.**
9. **Temporary architecture should be explicitly temporary.**
10. **Human judgement should be concentrated where it adds the most value.**
11. **Project memory must outlive individual model sessions.**
12. **The system should support incremental success rather than requiring a heroic big-bang rewrite.**
13. **The target should eventually become native to its new environment.**
14. **The project model should remain connected to actual executable evidence.**

---

## 31. Suggested next research phase

The next phase should be a structured deep-research exercise with three parallel tracks.

### Track A: Historical and industrial evidence

Find and analyse substantial migration programmes, especially:

- mainframe and COBOL modernisation;
- defence and government ports;
- automated language conversions;
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

- a small library translated language-to-language;
- an application moved between UI frameworks;
- a network service moved between runtimes;
- a data-heavy legacy application with schema changes;
- a system with sparse tests but observable behaviour;
- a system with strong tests and architectural change;
- a component requiring a compatibility layer;
- a project where target-native redesign is clearly preferable.

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

The ambitious version of KodePorter is not merely a tool for moving code.

It is a system for making a complex software transformation:

- legible;
- decomposable;
- collaborative;
- evidence-backed;
- reversible where possible;
- and progressively trustworthy.

That same underlying machinery might later support more than ports:

- large refactorings;
- framework replacement;
- system consolidation;
- architecture recovery;
- dependency removal;
- security remediation;
- protocol replacement;
- or continuous modernisation.

Porting is a particularly good proving ground because it exposes the full problem: understanding an existing system deeply enough to recreate it under changed constraints.

---

## 36. Working summary

KodePorter should be explored as a **project-scale cognition and control system for software migration**.

Its possible core is a durable model that connects:

```text
Source reality
    ↕
Recovered understanding
    ↕
Migration decisions
    ↕
Target implementation
    ↕
Executable evidence
```

The immediate task is not to rush into building a universal translator. It is to learn how successful and unsuccessful porting programmes have represented the work, controlled uncertainty, sequenced transformation, and established confidence.

From that research, we can identify the smallest useful system that helps a human-and-AI team understand and conduct a port better than a collection of coding-agent conversations and ad hoc documents.

---

## 37. Immediate next steps

1. Turn this note into a focused deep-research brief.
2. Identify ten to twenty serious migration case studies, with emphasis on detailed reports rather than promotional summaries.
3. Investigate the research vocabulary around programme comprehension, architecture recovery, traceability, transformation, and equivalence.
4. Develop a first candidate schema for claims, evidence, mappings, migration units, and decisions.
5. Select one small real codebase for an experimental port.
6. Prototype a conversion dossier and traceability workflow before attempting full automation.
7. Use the prototype to refine the project thesis and determine the likely product boundary.

---

*This document is a brainstorming synthesis, not a settled specification. Its purpose is to preserve the shape of the discussion, make the implicit ideas explicit, and create a strong starting point for research and prototype design.*
