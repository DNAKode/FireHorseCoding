---
title: "KodePorter Brainstorming Document Generation"
source: "https://chatgpt.com/g/g-p-6a50c665d51881918afa04b73ac72c4f-kodeporter/c/6a50c725-a9a8-83ea-82b1-004286c2ba38"
captured: "2026-07-10"
turns: 6
---

# KodePorter Brainstorming Document Generation

Verbatim visible transcript captured from the ChatGPT KodePorter project. Speech transcription quirks are preserved.

<!-- turn 1: user -->
## Turn 1 - User

Create a brainstorming notes document from our discussion session that will frame the codeporter project and the ideas we had around it, areas we would like to explore, the context in which it sits. Synthesize our discussion into a coherent document that can be the starting point for further research into how the project should go. Yeah, that kind of thing.

<!-- end turn 1 -->

<!-- turn 2: assistant -->
## Turn 2 - ChatGPT

I’ll turn the session into a structured project-framing document: vision, problem space, key ideas, research questions, candidate evidence sources, and a next-phase research agenda.

<!-- end turn 2 -->

<!-- turn 3: user -->
## Turn 3 - User

Not worried about fonts and stuff, just markdown

<!-- end turn 3 -->

<!-- turn 4: assistant -->
## Turn 4 - ChatGPT

I’m keeping it as an exploratory framing note rather than a requirements document. The historical-research strand—especially large mainframe, COBOL, defence, and other long-running migration programmes—will be treated as a source of design principles, not just case studies.

<!-- end turn 4 -->

<!-- turn 5: user -->
## Turn 5 - User

Show it easy to read

<!-- end turn 5 -->

<!-- turn 6: assistant -->
## Turn 6 - ChatGPT

Here is the document in a cleaner reading form. I’ve kept it exploratory rather than turning it into a premature specification. It is based on the fuller brainstorming capture. 

KodePorter brainstorming notes

Edit
KodePorter
Brainstorming and project-framing notes

Status: Working ideas document
Purpose: Frame the project, preserve the main ideas from the discussion, and provide a foundation for research, prototyping, and further design work.

1. The starting idea

KodePorter begins with a simple observation:

Porting a substantial software system is not mainly a matter of translating source code from one language into another. It is a programme of discovery, interpretation, decision-making, reconstruction, and verification.

Modern coding models make software migration newly tractable. They can read unfamiliar code, explain dependencies, generate translations, write tests, investigate failures, and work across large repositories.

But a serious migration still cannot be reduced to converting files one by one.

The difficult parts are usually:

understanding what the source system actually does;

recovering intent that is only partly represented in the code;

distinguishing required behaviour from accidental implementation detail;

deciding what must be preserved and what should be redesigned;

finding meaningful units of migration;

sequencing the work;

maintaining coherence across thousands of changes;

verifying that the target system is genuinely correct;

and retaining the knowledge developed during the project.

KodePorter may therefore be much broader than a code translator.

It may be better understood as a workspace, knowledge system, and operating method for large software-porting projects.

2. A provisional thesis

A useful working thesis is:

KodePorter should help a human-and-AI team conduct an entire software migration as an evidence-backed, inspectable, incrementally verified transformation.

This implies five closely connected activities:

Understand the source system.

Design the target system.

Map source concepts and behaviours to target concepts.

Carry out the transformation.

Verify and explain the result.

The important product may not be the generated target code alone.

It may be the evolving body of structured knowledge connecting:

source artefacts;

recovered architecture and behaviour;

domain concepts;

migration decisions;

target artefacts;

tests and runtime observations;

unanswered questions;

risks;

deviations;

and confidence assessments.

That body of knowledge becomes the memory and control surface of the migration.

3. Why this project feels timely

Software migration is not a new field. There have long been:

source-to-source translators;

compilers and intermediate representations;

compatibility layers;

automated refactoring tools;

binary translators;

database migration tools;

architecture-recovery systems;

and large legacy-modernisation practices.

What is new is the combination of:

models with broad knowledge of languages and frameworks;

repository-scale code understanding;

agentic build, test, and inspection workflows;

cheap generation of alternative implementations;

natural-language explanations;

and an ability to work with incomplete, inconsistent, or poorly documented systems.

This changes the economics of migration. Work that once required a large specialist team may become feasible for a much smaller team.

It also introduces a danger.

A model can produce a great deal of plausible target code while quietly losing important behaviour, operational constraints, edge cases, performance properties, or architectural meaning.

KodePorter should therefore not assume that code generation is the scarce resource.

Generation may become abundant. The scarce resources may instead be:

correct understanding;

good decomposition;

explicit decisions;

reliable evidence;

disciplined verification;

and project-wide coherence.

4. What counts as porting?

The project should use a broad definition.

A port may involve:

changing programming language;

changing runtime or operating system;

replacing a framework;

moving from desktop to web;

moving between processor architectures;

replacing proprietary infrastructure;

modernising a mainframe or minicomputer application;

migrating schemas and persistence models;

reconstructing undocumented behaviour;

preserving a protocol while replacing its implementation;

moving away from obsolete build and deployment systems;

extracting a reusable core from a platform-specific application;

or substantially redesigning a system while preserving selected external contracts.

These are not identical problems.

An important research question is which concepts recur across all of them and which require specialised methods.

5. Porting is not a single transformation

The naive model is:

Source code → Target code

A more realistic model is:

Source artefacts
    ↓
Recovered structure, behaviour, and constraints
    ↓
Explicit migration decisions
    ↓
Target design
    ↓
A sequence of transformations
    ↓
Tests, comparisons, and operational evidence
    ↓
Accepted target system

Even this is too linear.

Real migrations repeatedly move backwards and forwards:

a failed implementation exposes a misunderstood source behaviour;

a newly discovered dependency changes the migration sequence;

a test reveals an undocumented business rule;

a performance problem forces a target-design change;

an apparent source bug turns out to be behaviour on which users depend;

a supposedly independent component turns out to share hidden state;

a translation compiles but is architecturally wrong.

The natural representation of a port may therefore be a graph of artefacts, concepts, claims, decisions, transformations, dependencies, and evidence.

6. The central object: a porting knowledge model

KodePorter may need an explicit representation of the porting project.

It should not merely mirror the source-code syntax tree. It should represent several layers of understanding.

Source artefacts

These might include:

repositories;

projects and modules;

source and generated files;

schemas;

configuration;

build and deployment scripts;

documentation;

tests;

binaries;

sample data;

logs;

issue history;

external services;

and user workflows.

Recovered concepts

The system may identify:

components and responsibilities;

domain entities;

business rules;

state machines;

protocols;

lifecycle assumptions;

persistence semantics;

concurrency models;

error behaviour;

security boundaries;

performance expectations;

and undocumented conventions.

Target concepts

The target side may contain:

target modules;

replacement interfaces;

new service boundaries;

revised data models;

compatibility layers;

replacement libraries;

new deployment units;

and altered operational assumptions.

Mappings and transformations

The system should represent statements such as:

source component A becomes target component B;

several source types collapse into one target abstraction;

this behaviour is intentionally not preserved;

this module is temporarily wrapped rather than rewritten;

this interface remains compatible until migration stage four;

this implementation is provisional;

this target test verifies these particular source observations.

Evidence and confidence

Important statements should be attached to evidence.

Evidence might include:

source references;

documentation;

existing tests;

generated tests;

runtime traces;

captured inputs and outputs;

user confirmation;

issue reports;

static analysis;

comparison runs;

benchmarks;

and staging or production observations.

The system should distinguish among:

known facts;

strong inferences;

tentative hypotheses;

migration decisions;

unresolved questions;

and disproven assumptions.

Large ports contain a great deal of uncertain knowledge. KodePorter should represent that uncertainty rather than hiding it behind confident-looking prose.

7. Tentative links and progressive confirmation

One promising idea is to allow relationships to exist before they are fully verified.

Examples:

This target class probably corresponds to these three source modules.

This database column appears to encode the same concept as this enum.

This callback may be acting as an implicit transaction boundary.

This test seems to express a business rule rather than an implementation detail.

This exception path is probably equivalent to this source error condition.

Such links could begin as tentative and gain or lose support over time.

Confirmation might come from:

human review;

further model analysis;

static analysis;

successful test execution;

runtime comparison;

source-system traces;

or evidence discovered elsewhere in the repository.

This resembles ideas from:

knowledge graphs;

provenance systems;

scientific notebooks;

nonmonotonic reasoning;

intelligence analysis;

and software traceability.

The goal is not necessarily to reproduce any one of those systems. The goal is to borrow useful ways of representing provisional knowledge.

8. The migration unit is probably not the file

Files are convenient containers but may be poor units for planning and verification.

A meaningful migration unit might instead be:

a user-visible capability;

a domain concept;

an API contract;

a protocol;

a persistence boundary;

a state transition;

a call-graph slice;

a vertical feature;

a buildable subsystem;

or a testable cluster of behaviour.

The same source method may participate in several overlapping structures:

a business capability;

a security boundary;

a persistence workflow;

and a performance-critical path.

KodePorter may therefore need to support several simultaneous views of the system rather than imposing one universal decomposition.

9. Behaviour before implementation

For each meaningful migration unit, the system should help recover a behavioural contract.

Questions might include:

What inputs are accepted?

What outputs are produced?

What state is read or changed?

Which side effects occur?

What ordering assumptions exist?

How are errors represented?

Which external systems are involved?

What timing or performance properties matter?

Which edge cases are relied upon?

What is visible to callers or users?

Which behaviours are contractual?

Which are merely consequences of the current implementation?

This could produce a conversion dossier or behaviour dossier for each migration unit.

A dossier could combine:

a prose explanation;

relevant source references;

captured examples;

tests;

traces;

invariants;

target-design notes;

known deviations;

open questions;

and confidence assessments.

The dossier would guide both implementation and review.

10. Different things require different migration strategies

KodePorter should not treat every component as a rewrite.

Possible strategies include:

Mechanical translation

Preserve the source structure closely while translating syntax and APIs.

Semantic reimplementation

Recreate the required behaviour using target-native concepts.

Wrapping

Retain a source component temporarily and expose it through a new interface.

Compatibility emulation

Implement a source API, protocol, or runtime surface on the target platform.

Incremental replacement

Gradually divert behaviour from the old system to the new one.

Data-first migration

Stabilise schemas and persistence semantics before application logic.

Test-first reconstruction

Recover behaviour into executable tests and then implement against those tests.

Architecture-preserving port

Keep the system’s conceptual shape while changing its technology.

Architecture-changing modernisation

Redesign the structure, accepting that source-to-target mappings will be many-to-many.

Selective abandonment

Drop obsolete or unused behaviour, but do so explicitly and with evidence.

Real projects will probably combine several of these approaches.

11. Source-shaped versus target-native output

A central tension is whether the target should resemble the source.

A source-shaped translation provides:

easier traceability;

simpler comparisons;

lower initial semantic risk;

and faster automated conversion.

But it may also produce:

unnatural target APIs;

inappropriate threading or lifecycle models;

excessive compatibility scaffolding;

poor maintainability;

and code that remains mentally trapped in the old platform.

A target-native rewrite may be cleaner but introduces more interpretive risk.

A sensible strategy may be staged convergence:

recover and establish the required behaviour;

preserve strong source-to-target traceability;

create an initially conservative implementation;

then move progressively toward a target-native design;

while retaining behavioural evidence and decision history.

This suggests an important principle:

Behaviour preservation and architectural improvement are related, but they should not be treated as the same problem.

12. Verification must be a first-class activity

Verification cannot be added only after code generation.

It must be part of discovery, planning, implementation, and acceptance.

Possible methods include:

unit tests;

golden-master testing;

differential execution of old and new systems;

property-based testing;

trace comparison;

database-state comparison;

protocol replay;

fuzzing;

static checks;

performance benchmarks;

visual comparison;

user-journey testing;

shadow production execution;

and manual expert review.

The system should record not only whether a test passed, but what claim the test supports.

For example:

Claim:
The target parser preserves the source parser’s treatment
of malformed header X.

Evidence:
- 47 captured source-system examples
- differential test suite
- one approved intentional deviation
- reviewer confirmation

This turns tests into part of a wider evidence structure rather than treating them as isolated green or red indicators.

13. A port can be viewed as a set of claims

Another useful framing is to treat the migration as a structured set of claims.

Examples:

The target implementation preserves behaviour B.

Component X has no callers outside subsystem Y.

Database field A can be removed.

This source race condition is accidental and should not be reproduced.

The new service boundary preserves the original transaction semantics.

All uses of platform API P have been replaced.

The target can process historical dataset D without material deviation.

Each claim could have:

a status;

an owner;

supporting evidence;

contradicting evidence;

confidence;

affected artefacts;

review history;

and acceptance criteria.

This provides a more meaningful account of progress than “percentage of files converted.”

14. Project memory may be one of the core products

Large migrations repeatedly lose and rediscover knowledge.

Someone investigates an obscure behaviour, reaches a conclusion, makes a local change, and moves on. Months later, another person or model encounters the same area without access to the original reasoning.

KodePorter should preserve:

what was learned;

how it was learned;

which alternatives were considered;

why a decision was made;

what remains uncertain;

which tests support the conclusion;

and what later evidence changed it.

This project memory must outlive:

individual prompts;

agent sessions;

particular models;

temporary branches;

and individual team members.

That may be one of the strongest reasons for KodePorter to exist as a specialised system.

15. A possible working environment

A future KodePorter workspace might include several connected views.

System map

A navigable map of:

source components;

domain concepts;

dependencies;

runtime boundaries;

target components;

and migration state.

Conversion dossiers

For each migration unit:

source scope;

recovered purpose;

behavioural contract;

dependencies;

chosen strategy;

target plan;

generated artefacts;

tests;

questions;

evidence;

confidence;

and review status.

Traceability graph

Connections among:

source code;

target code;

tests;

behaviours;

decisions;

issues;

runtime observations;

and reviewers.

Migration plan

A dependency-aware plan showing:

prerequisites;

blockers;

temporary compatibility layers;

parallelisable work;

risk;

uncertainty;

and acceptance gates.

Agent workspace

A controlled environment where models can:

investigate questions;

propose mappings;

write tests;

implement target code;

run builds;

compare behaviour;

and submit evidence-backed changes.

Human decision queue

A place for decisions that genuinely require judgement:

ambiguous behaviour;

architecture choices;

intentional incompatibilities;

difficult evidence conflicts;

security-sensitive changes;

and risk acceptance.

16. Human and model roles

KodePorter should avoid two extremes:

treating the model merely as autocomplete;

or pretending that a model can safely own an entire migration without supervision.

Models may be particularly useful for:

repository exploration;

cross-language explanation;

pattern detection;

draft translation;

test generation;

repeated comparison;

documentation synthesis;

identifying likely gaps;

and proposing candidate mappings.

Humans remain important for:

defining acceptable outcomes;

interpreting business meaning;

resolving ambiguous intent;

choosing architecture;

evaluating risk;

deciding what should not be preserved;

and accepting evidence.

The system should help hand work backwards and forwards between automated investigation and human judgement.

17. Measuring progress

Weak measures include:

lines translated;

files converted;

commits produced;

and percentage of code generated.

More meaningful measures might include:

capabilities with completed behavioural dossiers;

critical claims supported by evidence;

source-to-target traceability coverage;

unresolved ambiguities;

differential-test coverage;

dependencies eliminated;

migration units accepted;

compatibility scaffolding remaining;

production traffic handled by the target;

and confidence by subsystem.

Progress may need to be shown across several dimensions rather than collapsed into one percentage.

18. Important distinctions

KodePorter should preserve several distinctions that ordinary project tools often blur.

Fact versus decision

“This code currently behaves this way” is different from “the new system should behave this way.”

Behaviour versus mechanism

A lock, callback, trigger, or queue may be an implementation mechanism. The behaviour it creates may be what matters.

Unknown versus unfinished

A missing target feature may be fully understood but not implemented, or it may remain conceptually unclear.

Generated versus accepted

Model output does not become project truth merely because it compiles.

Passing tests versus sufficient evidence

A test suite can pass while important behaviours remain misunderstood or untested.

Temporary versus permanent architecture

Compatibility layers often begin as temporary and quietly become permanent.

Compatibility versus similarity

Some interfaces must remain exactly compatible. Internal source-like structure may be unnecessary.

19. Historical research programme

Before defining the product too narrowly, we should study major migration programmes that have already dealt with these problems.

The most useful sources are likely to be:

research papers;

technical reports;

government and defence reports;

doctoral theses;

conference proceedings;

programme retrospectives;

archived project documentation;

and serious engineering postmortems.

Marketing summaries will be less useful unless they lead to more detailed sources.

Mainframe and COBOL modernisation

These projects are likely to contain mature thinking about:

programme understanding;

dependency recovery;

data semantics;

translation limits;

coexistence;

regression testing;

operational cutover;

and the loss of institutional knowledge.

Particular attention should be paid to projects moving:

COBOL to C, C++, Java, or managed runtimes;

mainframe workloads to distributed systems;

batch systems to online systems;

and legacy databases to newer persistence models.

Defence and government systems

Defence organisations and government agencies have repeatedly faced:

very long-lived systems;

obsolete hardware;

scarce expertise;

safety and mission constraints;

sparse documentation;

formal acceptance processes;

security requirements;

and multi-year transitions.

Our discussion raised the possibility that the US Navy and related organisations may have documented major platform or language migrations. This should become a specific research thread.

Automated language translation

We should investigate projects involving:

source-to-source translation;

compiler-assisted migration;

binary translation;

automated API replacement;

architecture lifting;

and semantics-preserving transformation.

The important question is not merely whether translation worked.

We should determine what supporting machinery was required:

intermediate representations;

annotations;

manual intervention;

test harnesses;

equivalence checks;

staging;

and post-translation restructuring.

Large open-source rewrites

Open-source migrations may provide unusually rich evidence because commits, issues, design discussions, and tests are publicly visible.

Useful examples may include:

compiler rewrites;

browser-engine migrations;

operating-system subsystems;

database-engine changes;

runtime migrations;

C or C++ to Rust transitions;

Java to Kotlin transitions;

JavaScript to TypeScript transitions;

and major framework replacements.

Safety-critical and regulated software

These fields may provide useful methods for:

requirements traceability;

evidence management;

change control;

independent verification;

and staged acceptance.

KodePorter should probably not adopt heavyweight certification processes by default, but it may be able to borrow lighter versions of their best ideas.

Programme comprehension and software archaeology

There is likely an important existing research literature around:

architecture recovery;

feature location;

concept assignment;

dependency analysis;

impact analysis;

dynamic tracing;

clone detection;

and legacy-system understanding.

This literature may provide established vocabulary and prevent the project from reinventing known methods.

20. What to extract from each case study

Each case study should be analysed using a common template.

Context

What was migrated?

Why was the migration undertaken?

How large and old was the system?

Which constraints made it difficult?

Knowledge recovery

How did the team learn what the system did?

What documentation existed?

How were undocumented behaviours found?

How was domain knowledge recovered?

Decomposition

Which units of migration were used?

How were dependencies represented?

How was sequencing chosen?

Transformation method

Automated translation?

Manual rewrite?

Wrapping?

Emulation?

Incremental replacement?

A hybrid approach?

Verification

What counted as evidence of equivalence?

Were the systems run in parallel?

How were tests created?

How were edge cases and performance handled?

Organisation

Which roles existed?

Where was expert judgement required?

How were decisions and knowledge recorded?

Failure modes

What went wrong?

Which assumptions proved false?

Where did automation fail?

What caused overruns or technical debt?

End state

Was the result source-shaped or target-native?

How much compatibility scaffolding remained?

What maintenance consequences followed?

Implications for KodePorter

Which concepts should the system represent?

Which workflows should it support?

Which kinds of evidence matter?

Which tempting approaches should be avoided?

21. Candidate research questions
Project representation

What is the best primary unit of a porting project?

Can one model support language ports, framework migrations, and architectural modernisation?

Is a graph representation essential?

Which entities and relationships recur across successful migrations?

Understanding

How can models distinguish domain behaviour from implementation detail?

How should contradictory or incomplete knowledge be represented?

How can static analysis, runtime observation, documentation, and human testimony be combined?

Planning

How should migration units be selected?

Can dependency-aware sequencing be produced automatically?

How should temporary bridges and coexistence states be represented?

Can the system identify areas of unusually high risk or uncertainty?

Transformation

When is direct translation appropriate?

When should wrapping, emulation, or redesign be preferred?

Can behaviour preservation be separated from target-native refactoring?

How should generated code remain connected to its source rationale?

Verification

What evidence is sufficient for different kinds of component?

How can differential tests be generated?

How should intentional deviations be represented and approved?

Can confidence be estimated without producing false precision?

Human interaction

Which questions should be escalated to humans?

What information does a reviewer need?

How can domain experts contribute without reading source code?

How should uncertainty be presented?

Product form

Is KodePorter primarily:

an IDE;

a repository service;

a project database;

an agent-orchestration system;

a migration methodology;

or some combination of these?

22. Possible prototype directions

The first prototype should probably not attempt a complete autonomous migration.

It should test the central assumptions.

Repository cartographer

Input:

a repository;

build metadata;

optional documentation.

Output:

component map;

dependency map;

candidate concepts;

suggested migration units;

investigation questions;

uncertainty indicators.

This tests whether KodePorter can create a representation richer than a conventional code index.

Conversion dossier generator

For a selected subsystem, generate:

purpose;

source scope;

dependencies;

behavioural hypotheses;

external contracts;

risks;

candidate target designs;

proposed tests;

open questions.

This tests whether the dossier is a useful unit for human-and-model collaboration.

Source-target traceability graph

Given a small completed port, connect:

source entities;

target entities;

tests;

behaviours;

decisions;

and evidence.

This tests whether traceability remains understandable when mappings are not one-to-one.

Differential behaviour harness

Run old and new implementations against shared inputs and record:

matches;

deviations;

traces;

state differences;

and supported claims.

This tests whether verification can become a reusable workflow rather than a one-off script.

Migration planner

Given a system map and target direction, propose:

stages;

prerequisites;

compatibility layers;

parallel tracks;

risk points;

and acceptance gates.

This tests whether an explicit dependency and evidence model improves planning.

Persistent agent notebook

Record each investigation as:

question;

method;

evidence;

conclusion;

confidence;

affected artefacts;

and follow-up work.

This tests whether durable project memory reduces repeated investigation and improves later model performance.

23. A possible smallest useful product

The first genuinely useful KodePorter product may generate relatively little target code.

It might:

ingest a repository;

create a navigable architectural and behavioural map;

let the user define a target direction;

generate conversion dossiers;

manage questions, claims, evidence, and decisions;

generate targeted tests;

track source-to-target mappings as the implementation proceeds.

This would already solve a weakness in current model-assisted software work: the lack of durable, project-wide understanding.

Code generation could then operate inside a structure that makes it more coherent and trustworthy.

24. A provisional conceptual model

Possible entities include:

Artefact

SourceEntity

TargetEntity

Concept

Capability

Behaviour

Contract

Dependency

MigrationUnit

Mapping

Transformation

Decision

Question

Hypothesis

Claim

Evidence

Test

Observation

Deviation

Risk

Review

Milestone

AgentRun

Possible relationships include:

implements;

depends on;

calls;

reads;

writes;

corresponds to;

replaces;

preserves;

intentionally differs from;

supports;

contradicts;

verifies;

was derived from;

is blocked by;

requires review;

is provisional until.

This model should remain provisional until it has been tested against real migration case studies.

25. Knowledge must be revisable

During a migration:

hypotheses become facts;

apparent facts are disproved;

target architecture changes;

migration units split or merge;

temporary workarounds become unnecessary;

tests are discovered to specify the wrong behaviour;

and source bugs are reclassified as required compatibility behaviour.

The system should therefore retain history and provenance rather than silently overwriting conclusions.

This suggests:

versioned claims;

superseding relationships;

decision history;

evidence timestamps;

agent-run provenance;

and explanations of why the present view differs from an earlier one.

26. Connection to executable reality

KodePorter cannot become a detached documentation system.

It should eventually integrate with:

source and target builds;

test runners;

package managers;

containers and virtual machines;

databases;

debuggers;

profilers;

tracing systems;

deployment environments;

binary-analysis tools;

and UI automation.

The knowledge model should be continuously updated by actual builds, executions, comparisons, and failures.

27. Likely failure modes
Plausible but false understanding

The model creates a coherent explanation that is not supported by the actual system.

Local correctness without global coherence

Individual conversions look reasonable but do not fit a consistent target architecture.

Test laundering

Generated tests merely reproduce the assumptions of the generated implementation.

Silent loss of edge cases

Normal paths work while unusual but important behaviours disappear.

Accidental permanence

Temporary wrappers and compatibility layers become the final architecture.

Translation-shaped technical debt

The target compiles but remains structurally trapped in the source platform.

Confidence theatre

Numbers and dashboards imply more certainty than the evidence justifies.

Context fragmentation

Each agent understands its local task, but no durable project-wide understanding exists.

Over-automation

The system keeps generating code when the real blocker is an unresolved human decision.

Failure to define intent

Everything is preserved because nobody explicitly decided what should change.

28. Provisional design principles

Understanding precedes transformation.

Behaviour matters more than syntax.

Uncertainty should be represented rather than hidden.

Important claims should be connected to evidence.

Migration units should be chosen for meaning and verifiability.

Traceability must survive architectural redesign.

Generation is not acceptance.

Verification is continuous rather than final.

Temporary architecture should be visibly temporary.

Human attention should be concentrated where judgement matters.

Project memory must outlive individual model sessions.

The system should support incremental success rather than heroic rewrites.

The target should eventually become native to its new environment.

The project model must remain connected to executable reality.

29. Suggested research phase

The next phase should consist of three parallel research tracks.

Track A: Historical and industrial evidence

Study:

mainframe and COBOL modernisation;

defence and government ports;

automated language conversions;

major open-source rewrites;

safety-critical migrations;

and long-running compatibility projects.

Goal: Recover the concepts, workflows, artefacts, and failure modes that experienced migration teams found important.

Track B: Relevant research fields

Survey:

programme comprehension;

software archaeology;

architecture recovery;

feature location;

traceability;

knowledge representation for software engineering;

automated program transformation;

semantic equivalence;

differential testing;

migration planning;

and human-AI software engineering.

Goal: Identify established ideas and vocabulary that should shape KodePorter.

Track C: Current AI coding systems

Study how present coding agents handle:

repository-scale understanding;

long-running tasks;

persistent memory;

planning;

tool use;

testing;

cross-language work;

and multi-agent coordination.

Goal: Determine what coding agents can already do and what specialised KodePorter layer remains necessary.

30. Expected research outputs

The research phase should produce:

A structured catalogue of major migration case studies.

A glossary of relevant concepts.

A candidate common model of migration projects.

A catalogue of repeated failure modes.

A design-implications document.

A recommendation for the first prototype.

A proposal for a benchmark set of experimental ports.

31. Candidate experimental projects

A useful experimental set should cover several migration shapes:

a small language-to-language library port;

an application moved between UI frameworks;

a service moved between runtimes;

a data-heavy application involving schema changes;

a project with weak tests but observable behaviour;

a project with strong tests and major architectural change;

a project requiring a temporary compatibility layer;

and a project where a target-native redesign is clearly preferable.

The experiment should not merely ask whether a model can produce code.

It should ask whether the KodePorter approach improves:

correctness;

coherence;

reviewability;

traceability;

investigation speed;

and justified confidence.

32. Open product questions

We have not yet decided:

whether KodePorter should be local-first, server-based, or hybrid;

whether its main interface should be an IDE, browser workspace, graph, or document system;

how much structure users will tolerate;

how much of the knowledge model can be generated automatically;

whether KodePorter should directly modify code or coordinate existing coding agents;

whether a graph database is actually necessary;

how confidence and evidence should be represented;

how proprietary systems should be handled securely;

how independent the system should be from any particular model provider;

and where the boundary lies between a methodology and a software product.

These questions should remain open until the case studies and prototypes provide stronger evidence.

33. The deeper ambition

The ambitious version of KodePorter is not merely a tool for moving code.

It is a system for making a complex software transformation:

legible;

decomposable;

collaborative;

evidence-backed;

incrementally verifiable;

and progressively trustworthy.

The same underlying machinery could later support:

large refactorings;

architecture recovery;

framework replacement;

dependency removal;

system consolidation;

security remediation;

protocol replacement;

and continuous modernisation.

Porting is an especially good proving ground because it exposes the whole problem:

Can we understand an existing system deeply enough to recreate it under changed technical constraints without losing what matters?

34. Working summary

KodePorter should be explored as a project-scale cognition and control system for software migration.

Its core may be a durable representation connecting:

Source reality
    ↕
Recovered understanding
    ↕
Migration decisions
    ↕
Target implementation
    ↕
Executable evidence

The immediate task is not to build a universal translator.

The immediate task is to learn how serious migration programmes have:

represented the work;

recovered lost knowledge;

managed uncertainty;

decomposed systems;

sequenced transformations;

combined automated and manual methods;

and established confidence in the result.

From that research, we can identify the smallest useful system that helps a human-and-AI team conduct a port more coherently than a collection of coding-agent sessions, issue tickets, and disconnected documents.

35. Immediate next steps

Turn this framing note into a focused deep-research brief.

Identify ten to twenty substantial migration case studies.

Prioritise detailed technical reports over promotional summaries.

Investigate mainframe, COBOL, defence, and government migration programmes.

Survey programme comprehension, architecture recovery, traceability, and equivalence research.

Develop a first candidate schema for claims, evidence, mappings, decisions, and migration units.

Select a small real system for an experimental port.

Prototype the conversion-dossier and project-memory ideas before attempting broad automation.

This is a brainstorming synthesis rather than a settled specification. Its purpose is to preserve the shape of the discussion, make the implicit ideas explicit, and provide a strong starting point for research and prototype design.

KodePorter_Brainstorming_Notes.md

<!-- end turn 6 -->
