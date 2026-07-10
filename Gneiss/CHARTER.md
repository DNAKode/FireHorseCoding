# Gneiss Charter

**Status:** Founding architectural charter  
**Purpose:** Preserve the conceptual integrity of Gneiss while its theory, product shape, and implementations evolve.  
**Applies to:** The Gneiss protocol, reference model, libraries, services, schemas, tools, user experience, and domain integrations.  

---

## 1. Definition and charter authority

> **Gneiss is a system for keeping important knowledge accountable over time: what was claimed, what is currently believed, why, under which rules, and what changed.**

Gneiss is intended to be embeddable. It may be delivered as a protocol, schema, library, local service, sidecar, command-line tool, or a combination of these. Those are product forms, not its definition.

This charter is the governing statement of what Gneiss is for, what must remain true of it, and how it may change. The exploratory corpus in `kb/` supplies history, alternatives, research, and detailed design arguments. The [candidate Maxwell page](kb/maxwell/THE-PAGE-v0.md) supplies a compact reference semantics. Neither should be allowed to obscure the simpler purpose stated here.

When an implementation choice, feature proposal, or elegant theory conflicts with this charter, the proposal must change or the charter must be amended explicitly. Conceptual drift must never arrive disguised as an implementation detail.

## 2. Purpose

Important systems accumulate more than data. They accumulate observations, interpretations, rules, corrections, exceptions, forecasts, decisions, intentions, and unresolved disagreements. These are created by people, instruments, software, models, and agents. They arrive at different times, refer to different periods, use changing definitions, and carry different authority.

Ordinary systems usually flatten this history into current fields, mutable documents, dashboards, or prose. The resulting answer may be useful, but it is difficult to determine:

- what was actually observed;
- what was inferred or decided;
- which definitions and policies were applied;
- which evidence was ignored or defeated;
- what changed since an earlier answer;
- what is unknown rather than absent;
- whether the result can still be reproduced;
- and who or what should be held accountable for it.

Gneiss exists to make those questions structural rather than heroic. It should let an operational system, research programme, or software tool retain a durable epistemic record while continuing to use the databases, files, models, instruments, and workflows appropriate to its domain.

The intended outcome is not a universal database of truth. It is a dependable way for a system to state its current view while keeping that view connected to its basis and open to principled revision.

## 3. Design center

The center of Gneiss is the separation between **recorded testimony** and **current belief**.

Recorded testimony is what sources asserted, observed, proposed, decided, or declared. It is historical and attributable. Current belief is a deterministic interpretation of the available testimony under a declared evaluation context.

The central contract is:

```text
current view = evaluate(recorded history, declared context)
```

The record and the view are both first-class, but they are not the same thing. The record preserves what happened. The view answers the present question. A different context, policy version, time cutoff, authority rule, or evidence horizon may produce a different legitimate view over the same record.

Everything else in Gneiss—provenance, correction, time travel, explanation, typed missingness, what-if worlds, archival grades, and federation—exists to keep that separation useful and honest.

## 4. Constitutional principles

The following principles are the conceptual constitution of Gneiss. An implementation need not expose their internal names, but it must preserve their meaning.

### 4.1 Testimony is not truth

An assertion records that a source made a claim using a method at a time. Recording it does not make it accepted. A sensor reading, analyst conclusion, model proposal, imported fact, and executive decision may all enter the record without becoming equally authoritative or even mutually compatible.

Gneiss stores claims and derives views. It does not silently promote ingestion into truth.

### 4.2 Correction appends; it does not rewrite history

Material knowledge is not edited in place. A later statement may retract, supersede, reject, qualify, or invalidate an earlier one, but the earlier statement remains addressable within its permitted retention boundary.

This is not a fetish for immutability. It is the mechanism that makes “what changed?” answerable.

### 4.3 Every current answer is contextual

An answer is evaluated under a named, versioned context that declares at least the relevant data horizon, definition horizon, conflict policy, admission policy, closure assumptions, and acceptable evidence standing.

No report or API response should imply that its result is timeless or policy-free. Sensible defaults may make contexts unobtrusive, but defaults must still be inspectable and pinnable.

### 4.4 Every material answer carries a receipt

A Gneiss answer carries a label identifying the context and the material inputs consumed in producing it: evidence, decisions, rules, coverage declarations, remote watermarks, and retention standing.

This receipt is the foundation for `why`, reproducibility, staleness detection, incremental recomputation, safe what-if commit, and honest comparison between answers. It is not optional tracing bolted on after evaluation.

### 4.5 Missingness and disagreement are results

Unknown, not observed, not applicable, not yet observable, withheld, lost, outside coverage, contradicted, and unsupported are different states. Gneiss must not collapse them into a null, an empty string, zero, or a convenient winner.

Likewise, contested evidence is a legitimate output when policy does not license a unique answer. Honest abstention is a feature.

### 4.6 Decisions and policies take the witness stand

A decision is recorded testimony with an actor, reason, scope, and effective time. A policy or rule is a versioned declaration, not invisible application code. Changes in policy must be distinguishable from changes in evidence.

Human and machine decisions use the same structural machinery. Authority and regenerability matter; biological identity does not grant a special data type.

### 4.7 Storage follows modality

Gneiss is not a demand that all data become assertions or graph triples. Dense telemetry, documents, source repositories, images, binaries, model outputs, and event streams remain in stores suited to them. Gneiss records stable references, descriptors, important claims, and the relations needed to understand and govern them.

The rule is: keep bulk evidence in its organ; keep its epistemic consequences and durable coordinates in the record.

### 4.8 The world and the record are imperfect

External systems mutate, disappear, lie, lose history, and disagree. Local stores can be corrupted or deliberately purged. Federation does not create a global clock or an infallible authority.

Gneiss must represent evidence coverage, source validity, import watermarks, retention loss, and uncertainty explicitly. “Replay” means replay of the surviving record under a pinned context, not resurrection of the past world.

### 4.9 Warrant kind is separate from evidence survival

A domain may distinguish verified, validated, estimated, reviewed, differential, observed, or production-attested claims. These describe how a claim is warranted. Gneiss separately describes whether its basis remains grounded in raw evidence, survives only through an adequate seal, or is merely attested after loss.

These axes must not be flattened into one confidence number. A claim can be experimentally validated and later survive only in sealed form.

### 4.10 Intent and realization remain connected

Systems contain both statements about what is and statements about what should be. Intentions, obligations, targets, designs, and plans must remain distinguishable from observations while being linkable to the artifacts and behaviors that realize them.

Gneiss should support the question “what for?” alongside “why do we believe this?” without turning values or goals into accidental facts.

### 4.11 Full recomputation is the semantic oracle

The authoritative meaning of a view is a deterministic evaluation over a pinned record and context. Incremental indexes, caches, projections, streams, and distributed services are replaceable acceleration strategies. They must agree with full recomputation for the same inputs.

This preserves the model when the implementation changes.

### 4.12 Theory serves use

Gneiss may draw on temporal databases, truth maintenance, Datalog, provenance semirings, abstract interpretation, event sourcing, accounting, wikis, and knowledge representation. No formalism earns a place merely by being elegant.

Theory belongs in the product when it makes an important answer safer, clearer, smaller, or easier to reproduce. The interface should expose useful questions and actions, not require users to learn the research lineage.

## 5. Conceptual architecture

Gneiss consists conceptually of five cooperating parts.

### 5.1 Record plane

The record plane accepts immutable, attributable transactions. It contains assertions and the relations that explain, qualify, defeat, or connect them. Its job is faithful memory within declared retention limits.

The record plane does not decide a universal current truth at write time. It preserves enough structure for later evaluation under different contexts.

### 5.2 Interpretation plane

The interpretation plane evaluates the record under a context. It admits claims, applies decisions and conflict policies, follows justifications, respects time and coverage, and emits accepted, defeated, contested, or typed-missing results.

Evaluation is a deterministic, stratified fold rather than an open-ended search. A model may propose assertions or rules, but model inference is not hidden inside the definition of current belief.

### 5.3 Evidence organs

Evidence organs are the systems where domain material lives: operational databases, object stores, Git repositories, instruments, document stores, test runners, scientific solvers, and external services. Gneiss binds them through stable identifiers, content hashes, version descriptors, observations, and import receipts.

An organ may be authoritative for a mechanical fact without being the authority on its interpretation.

### 5.4 Domain layer

Applications define domain entities, predicates, warrant types, rules, and views. AIMS may speak of silos, portions, sensors, and reconciliations. FrankenSim speaks of physical claims, regimes, certifiers, and falsifiers. KodePorter speaks of migration units, correspondences, behavioral contracts, and divergences.

Gneiss supplies the grammar of accountability. It must not swallow the vocabulary of its applications.

### 5.5 Presentation and action layer

Humans and agents interact through current views, revealable explanations, diffs, review queues, alerts, plans, and domain tools. Every important displayed conclusion should offer a short path to its status, source, method, context, history, and consequences.

The front page should show the useful current answer. The record must remain one or two gestures away.

## 6. Core model and language

The smallest stable conceptual spine is:

- **Entity:** a stable identity about which statements are made;
- **Assertion:** an immutable claim by a source, with predicate, value, valid time, method, and status;
- **Transaction:** the attributable envelope that records one atomic act;
- **Justification:** a typed dependency linking a claim to evidence, decisions, rules, or other claims;
- **Context:** a versioned declaration of how and as-of-when a view is evaluated.

Decisions, coverage declarations, seals, policies, intents, obligations, hypotheses, forecasts, and imports are stances expressed using that spine. Implementations may use optimized relational forms for them, but should resist multiplying kernel primitives whenever an explicit stance will do.

The user-facing verb grammar should remain small:

- `record` testimony;
- `decide` how a claim or claim-key is treated;
- `declare` a policy, definition, context, predicate, or closure assumption;
- `ask` for a current view under a context;
- `why` to reveal the basis and defeating alternatives;
- `sprout` a what-if world and `commit` it only if its read basis remains valid;
- `seal` an evidence region under a declared future-query contract;
- `purge` only through a seal or record honest loss;
- `import` a pinned prefix or observation from another ledger or organ.

The candidate relational schema and belief rules in `kb/maxwell/THE-PAGE-v0.md` are the initial reference semantics. They may be simplified or corrected through implementation, but replacement must preserve or explicitly amend the constitutional behavior above.

## 7. Product form

Gneiss should become a coherent product family rather than a single mandatory deployment shape:

The ambition is analogous in shape—not yet in achievement—to the relational programme: define a logical model, a small common language of operations, a reference meaning, and interchangeable physical implementations. Gneiss should let applications depend on an accountability contract without depending on one storage engine, just as relational applications need not define themselves by a particular file layout. The analogy is a design programme and a discipline against platform capture, not a claim that the model is already complete.

1. **Protocol and conformance model** — canonical meanings, identifiers, labels, and serialized interchange forms.
2. **Reference evaluator** — a deliberately small, readable implementation of the belief fold and explanation behavior.
3. **Schema and migration kit** — portable logical schema with bindings for selected storage systems.
4. **Embeddable library** — the preferred A2 form for applications that want Gneiss inside their process or data boundary.
5. **Restartable sidecar** — for languages, environments, or adoption paths where embedding is undesirable.
6. **CLI and agent tools** — record, ask, why, diff, inspect, validate, seal, and run conformance drills.
7. **Domain extension kit** — guidance and types for adding a vocabulary without modifying the kernel.
8. **Diagnostic and amnesia test suite** — executable tests for determinism, replay, staleness, retention degradation, and context pinning.

“Tool, library, and shared schema” is therefore a reasonable implementation description, but not the identity sentence. The protocol and behavioral contract must survive any one library or database.

## 8. Scope and non-goals

Gneiss is intended for knowledge that is important enough that source, timing, policy, disagreement, or revision matter. Not every field, log entry, or intermediate computation belongs in it.

Gneiss is not:

- a universal ontology of the world;
- a replacement for relational databases, object stores, Git, data lakes, or document systems;
- a generic graph database sold through a philosophical vocabulary;
- an LLM memory transcript or vector store;
- an autonomous truth oracle;
- an argument that every datum requires human review;
- a confidence-scoring engine that turns uncertainty into one number;
- a distributed consensus system or global transaction coordinator;
- an all-or-nothing platform migration;
- or a reason to duplicate domain tools that already produce authoritative evidence.

The default adoption target is an embeddable library or sidecar used by one real system. Cross-system identity, federation, and platform-scale operation must be earned by demonstrated needs.

## 9. Adoption layers

Gneiss should support incremental adoption with explicit stop points:

- **A0 — discipline:** existing forms and workflows capture source, reason, and correction intent more explicitly.
- **A1 — local ledger:** one application records important assertions and decisions and can answer the witness-stand questions.
- **A2 — embedded system:** the application derives operational views from the ledger and uses labels, contexts, and explanations in normal work. This is the primary target.
- **A3 — shared sidecar:** several systems publish and consume claims through shared identities where reconciliation is genuinely valuable.
- **A4 — institutional substrate:** multiple domains rely on a governed protocol and federated epistemic infrastructure.

Progression is not maturity theater. A system should stop at the lowest layer that pays for itself. Periodic reconciliation may be better than federation; a normal audited table may be better than Gneiss for simple facts.

## 10. Canonical realizations

### 10.1 KodePorter: first co-developed realization

[KodePorter](../KodePorter/CHARTER.md) is the first system that should be deliberately built on Gneiss. It is demanding enough to exercise changing evidence, many-to-many mappings, agent proposals, human decisions, executable verification, context-sensitive equivalence, stale dependency cones, and long-lived project memory.

KodePorter owns the porting domain. Gneiss owns the reusable accountability substrate. Their first shared deliverable is one small migration unit carried end to end through mapping, claim, evidence, proposal, decision, labeled view, source change, staleness, and correction.

KodePorter is both customer and pressure test. A concept discovered there enters Gneiss only if it recurs beyond porting or is required to preserve a constitutional invariant.

### 10.2 FrankenSim: sibling evidence system

FrankenSim provides a contemporary example of a domain that treats warranted claims as the product. Its verified, validated, and estimated colors; regime demotion; falsifier pairing; tombstones; `NoClaim`; and portable evidence packages are useful patterns.

Gneiss should learn from them without universalizing physical-modeling semantics. In particular, domain warrant types must remain distinct from Gneiss's evidence-survival standing.

### 10.3 Other motivating systems

AIMS, competition-data systems, ERP-adjacent work, and fluid-material tracking remain important tests of time, identity, provenance, derived quantities, uncertain observations, and cross-system reconciliation. They are validation domains, not reasons to put their nouns in the kernel.

## 11. Archival honesty and the ADQ boundary

Evidence cannot always remain forever. Cost, privacy, licensing, regulation, and operational practicality may require compaction, redaction, archival, or purge. Gneiss must support this without pretending that lossy memory is full memory.

Two hazards must remain separate:

1. **Resurrection by attrition:** deleting the evidence for an accepted winner must not silently allow an old defeated alternative to become current.
2. **Invented reinterpretation:** a later context must not ask a lossy summary a question that required distinctions no longer present.

A seal is therefore not merely a summary. It is a certified substitute for a declared family of future queries. It must preserve accepted results, relevant defeated alternatives and defeat reasons, applicable rules, coverage boundaries, and the basis needed for those queries.

There is no universal finite summary guaranteed to support every future interpretation. Outside a seal's declared contract, the result must weaken to typed insufficiency, attestation, or `NoClaim`.

The governing sentence is:

> **After evidence is discarded, Gneiss must never answer as though it had re-read that evidence.**

This seal-content adequacy problem is a named open design obligation, not a theorem already supplied by the kernel.

## 12. User and agent experience

For every material answer, a conforming Gneiss experience should make the following questions answerable with little ceremony:

1. What is the current answer?
2. As of what data and definitions?
3. Who or what asserted the important inputs?
4. By which method?
5. Why was this evidence accepted over alternatives?
6. What is missing, disputed, stale, or outside coverage?
7. What changed from the previous answer?
8. What intention, obligation, or decision does this answer serve?

The system should lead with the answer and progressively reveal the machinery. It should use ordinary domain language in primary views, provide diffs before raw histories, and turn typed missingness into useful next actions.

Agents use the same interfaces. They receive bounded context, explicit authority, applicable rules, and labeled answers. They submit attributable proposals and evidence rather than silently mutating project belief. Replaceable models should be able to work over durable state without owning it.

## 13. Operational qualities

### Declared operating envelope

Write and read rates, latency expectations, context count, recursion, retention, evidence volume, distribution, validity horizons, and federation are assumptions about a deployment, not properties of Gneiss in the abstract. Important architectural choices should cite a declared, versioned envelope and include a signal for when drift defeats the choice. “Single node,” “human cadence,” and similar defaults must never be presented as truths about an unbuilt or differently deployed system.

### Determinism and reproducibility

Pinned inputs must produce byte-stable or semantically canonical results. Floating-point behavior, hashing, ordering, serialization, and external method versions must be controlled where they affect identity or decisions.

### Security and privacy

Append-only does not mean universally visible or immortal. Authorization, encryption, redaction receipts, secret isolation, retention mandates, and audit boundaries are part of the design. Redaction must leave an honest structural trace without retaining prohibited content.

### Performance

Correct full recomputation comes first. Per-key folds, dependency indexes, materialized views, incremental engines, and distributed processing may be added against measured envelopes. Each optimization must have a conformance oracle and a declared invalidation strategy.

### Federation

Federated ledgers remain fallible sources to one another. Imports use explicit watermarks and provenance. Cross-ledger identity and order are beliefs, not axioms. Gneiss does not promise consensus unless a separate consensus system supplies it.

### Portability

The conceptual model, protocol, and conformance fixtures must remain platform-neutral. SQL, C#, Rust, Datalog, or a particular database may provide bindings and reference implementations, but none defines Gneiss by itself.

## 14. Preserving conceptual integrity

Brooks's warning applies directly: a system assembled from individually reasonable ideas can lose its conceptual integrity. Gneiss therefore needs an explicit design authority and change discipline.

### 14.1 One design center

The testimony/belief separation and labeled evaluation contract are the center. New features must explain how they strengthen that center. “Knowledge management,” “AI memory,” or “graph capability” is too broad a justification.

### 14.2 Layered authority

Design authority descends in this order:

1. this charter;
2. constitutional invariants and conformance tests;
3. reference semantics and protocol;
4. domain contracts;
5. implementation architecture;
6. optimizations and user-interface choices.

A lower layer may not silently redefine a higher one.

### 14.3 A named conceptual steward

During the formative phase, each release or milestone should have one named design steward responsible for the coherence of the whole, even when many people or agents contribute. The steward does not decide privately: reasons, alternatives, dissent, and consequences are recorded. The role exists to prevent committee-shaped incoherence, not to suppress criticism.

As the project matures, this may become a small architecture council, but final responsibility for each accepted conceptual change must remain identifiable.

### 14.4 Amendment discipline

A constitutional change requires:

- the problem demonstrated in a real domain;
- the invariant or charter passage affected;
- at least one rejected simpler alternative;
- compatibility and migration consequences;
- new or changed conformance fixtures;
- and a dated decision explaining why the added concept belongs in Gneiss rather than a domain layer.

Experimental ideas may remain extensions until they earn promotion. Vocabulary changes are schema changes and deserve the same care as code changes. The [glossary](kb/02-GLOSSARY.md) is the supporting vocabulary registry: one term should carry one load-bearing meaning, and convenient synonyms must not conceal different concepts or collapse distinctions established by the charter.

### 14.5 Executable constitution

The project should maintain a small golden corpus covering:

- competing claims under several contexts;
- correction without erasure;
- definition-time versus data-time changes;
- typed missingness and coverage;
- agent proposal versus accepted decision;
- consumed-set labels and `why`;
- what-if commit over a moved base;
- federation watermarks;
- adequate and inadequate seals;
- and full-recompute versus incremental equivalence.

These examples are more authoritative than explanatory prose about implementation behavior.

### 14.6 Complexity budget

Every kernel concept has a permanent cost in storage, APIs, teaching, migration, and reasoning. Prefer a domain stance, policy, or library extension over a new primitive. Prefer one strong operation over several overlapping ones. Remove mechanisms that the canonical realizations do not use.

## 15. Development programme

The first implementation should be a cell, not a platform.

### Phase 1 — one-week reference cell

Implement the smallest ledger, context, deterministic belief view, explanation receipt, correction path, and deliberately narrow seal needed by one KodePorter migration unit. Full recomputation is sufficient. The result should be reimplementable from the reference specification by a competent graduate student in roughly a week.

### Phase 2 — KodePorter vertical slice

Use the cell to support one source/target mapping and behavioral contract with precise evidence, an agent proposal, a human or policy decision, a source delta, staleness, and correction. Judge Gneiss by whether KodePorter becomes clearer and safer, not by the number of generic features implemented.

### Phase 3 — second-domain test

Apply the same substrate to a materially different domain, likely a narrow FrankenSim evidence package or AIMS observation/reconciliation flow. Anything that only KodePorter needs stays in KodePorter.

### Phase 4 — hardening

Only after two domains agree on the spine should the project stabilize the protocol, add incremental maintenance, publish bindings, and test alternate storage implementations.

### Phase 5 — earned expansion

Federation, shared identities, richer intent models, high-rate streams, and institutional governance enter only against demonstrated envelopes and stop rules.

## 16. Success, failure, and kill criteria

Gneiss succeeds when a real system can answer important operational questions with less ambiguity and less repeated investigation, while users can inspect why the answer exists and reproduce or revise it under explicit contexts.

Evidence of success includes:

- domain applications become simpler because they stop hand-rolling provenance and revision;
- later agents can continue work without reconstructing old reasoning from transcripts;
- policy changes and evidence changes produce distinguishable view changes;
- stale conclusions are detected from consumed dependencies;
- disagreement and insufficiency are visible without blocking useful work;
- the reference evaluator remains small and alternative implementations conform;
- archival loss weakens answers honestly rather than changing them silently;
- a second domain reuses the substrate without adopting KodePorter vocabulary.

The approach should be reconsidered or killed if:

- applications must force most ordinary data into a generic assertion model;
- users cannot understand current answers without studying the ledger internals;
- domain teams routinely bypass the decision and provenance workflow;
- the belief view cannot be made deterministic and testable;
- labels are too expensive or imprecise to support staleness and explanation;
- ordinary audited database patterns solve the target cases more simply;
- the kernel grows mainly to absorb one application's domain model;
- or the system becomes a second, poorly synchronized source of truth.

## 17. Open questions protected by this charter

The charter intentionally does not settle:

- the first implementation language or database;
- whether the primary distribution is embedded, sidecar, or both;
- the final physical schema;
- the most useful user interface;
- the optimal incremental-maintenance engine;
- the complete strainer and typed-value comparison language;
- the general solution to seal adequacy;
- the final federation model;
- or the long-term governance organization.

These are design questions to be answered by prototypes and domains. Their answers may change while the definition, design center, and constitutional principles remain stable.

## 18. Founding commitment

Gneiss will preserve testimony without confusing it with truth, produce current views without hiding their rules, and carry enough of each answer's basis that people and machines can inspect, challenge, reproduce, and revise it.

It will begin small, beside real systems. It will keep domain vocabulary in the domain. It will treat uncertainty, disagreement, loss, and changing policy as normal conditions rather than exceptional failures. Its fixed point will not be a particular schema or technology, but an accountable way of changing what a system believes.

That is the component we intend to build.
