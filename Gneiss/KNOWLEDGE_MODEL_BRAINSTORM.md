# Toward a Fixed-Point Model of Operational Knowledge

## A discussion and brainstorming document

## 1. Motivation

The recurring problem is that real operational systems do not merely store data. They store a changing model of the world.

In systems like AIMS, CompSeek, Smoothscrape, industrial monitoring, ERP, and Palantir-style ontology platforms, we repeatedly meet the same pattern:

* There are entities: stores, silos, sensors, people, videos, events, matches, products, reports.
* There are properties of those entities: names, shapes, capacities, locations, measurements, statuses, identities, relationships.
* Some properties are stable and column-like.
* Some are structured and evolving.
* Some are dense time series.
* Some are sparse configuration changes.
* Some are derived.
* Some are tentative.
* Some are corrected later.
* Some are believed only under a particular interpretation context.
* Some are not known to be true, but are plausible hypotheses awaiting confirmation.

Traditional database design gives us tables, rows, columns, constraints, indexes, and transactions. These are extremely useful, but they are not by themselves a complete conceptual model for long-lived operational knowledge.

At the other extreme, a universal “everything is an entity with a JSON blob” design gives flexibility, but loses too much: queryability, integrity, understandable schemas, reporting, indexing, tool support, and operational discipline.

The fixed-point realization seems to be somewhere else:

> The durable core of the system is not a universal storage format.
> It is a universal interpretation contract over assertions, evidence, time, provenance, and belief.

The aim of this document is to frame that conceptual area and sketch the beginning of a model that could support prototype systems and further research.

---

## 2. The central tension

Consider a `Store` or `Silo` entity.

It may have ordinary properties:

```text
StoreId
Name
Description
Location
StoreType
```

It may also have structured configuration:

```text
Shape:
  type: cylinder
  diameter: 3.2m
  height: 8.5m
```

It may have dense time series:

```text
FillLevel(t)
Temperature(t)
Weight(t)
Vibration(t)
```

It may have sparse configuration history:

```text
Capacity changed on 2025-03-01
Shape model changed on 2025-07-12
Sensor source changed on 2026-01-18
```

It may have derived quantities:

```text
MassEstimate(t) = f(FillLevel(t), ShapeVersion(t), ProductDensity(t))
```

It may have corrections:

```text
The manual reading entered on Monday was for the wrong silo.
The sensor calibration used last week was wrong.
The shape model used before April should be superseded.
```

It may have uncertain or tentative links:

```text
Sensor42 probably measures Silo17.
This video probably corresponds to Event123.
This OCR name probably corresponds to Person456.
```

The question is: where should all of this live?

Should `Name` be a SQL column?
Should `Shape` be JSON?
Should `FillLevel` be a time-series table?
Should `Sensor42 measures Silo17` be a relation table?
Should a tentative link be a row with `status='suggested'`?
Should every property become a rich metadata object?
Should the whole entity become a versioned JSON graph?
Should databases become coarse object stores?

The answer is probably: no single representation should be forced to do all jobs.

But the system should still expose a coherent model.

---

## 3. The first principle: separate storage from semantics

The same semantic property may have different physical representations.

For example:

```text
Silo17.fillLevel
```

may be sourced historically from:

```text
2024: manual laser readings
2025: intermittent operator-entered measurements
2026: online radar sensor
2027: fused radar + camera model
```

The storage may change. The measurement method may change. The quality may change. The sampling rate may change.

But the semantic question remains stable:

```text
What was the fill level of Silo17 at time T, under interpretation context C?
```

That suggests the ontology or semantic layer should not say:

```text
fillLevel is stored in table X
```

It should say:

```text
fillLevel is a time-varying property of Silo,
resolved by a history provider under an evaluation context.
```

The provider can then read from:

* SQL row history
* dense time-series storage
* event logs
* versioned JSON configuration documents
* derived calculation pipelines
* external systems
* cached projections
* manual override tables

The semantic layer owns the meaning.
The storage layer owns performance and physical representation.

---

## 4. The second principle: all important claims are assertions

A useful fixed point is:

> Everything important is an assertion.

A sensor reading is an assertion:

```text
Silo17 fillLevel = 4.2m at 2026-06-20 10:00
source = RadarSensor42
method = radar_model_v3
```

A manual entry is an assertion:

```text
Silo17 fillLevel = 4.3m at 2026-06-20 10:05
source = OperatorEntry
operator = Alice
method = handheld_laser
```

A configuration change is an assertion:

```text
Silo17 shape = Cylinder(diameter=3.2m, height=8.5m)
valid from 2025-03-01
source = engineering_config
```

A derived value is an assertion:

```text
Silo17 massEstimate = 12.4t at 2026-06-20 10:00
derived from fillLevel, shape, density, formula version 5
```

A tentative identity link is an assertion:

```text
OCRPersonCluster91 sameAs SmoothcompPerson456
confidence = 0.87
method = name_country_club_similarity_v2
status = proposed
```

A human confirmation is also an assertion:

```text
User Bob accepted Hypothesis H123 at transaction time T
```

A correction is an assertion:

```text
Assertion A17 is retracted because it referred to the wrong silo
```

An ontology definition is an assertion:

```text
fillLevel is a property of Silo
unit = metre
introduced in ontology version 42
```

A report definition is an assertion:

```text
Daily Silo Mass Report version 7 uses massEstimate
with restatement_policy = current_restated
```

This does not mean all assertions are stored in one table. It means they participate in one conceptual model.

---

## 5. The third principle: evidence is monotonic, belief is nonmonotonic

This may be the most important conceptual split.

The evidence store should be append-only or nearly append-only.

Once the system observed something, imported something, calculated something, or received a user decision, that event should remain part of the trace.

But the system’s current belief can change.

Example:

```text
At 10:03:
  Manual reading says fillLevel = 4.2m at 10:00.

Current belief:
  fillLevel = 4.2m.

At 12:30:
  Operator says the reading was for the wrong silo.

Evidence store:
  still contains the original manual reading
  plus the correction event.

Current belief:
  no longer accepts the original reading for Silo17.
```

This is a nonmonotonic belief layer over a monotonic evidence layer.

That is exactly the older knowledge-base and nonmonotonic logic problem in practical form.

A classical monotonic system says:

```text
Adding new facts only adds conclusions.
```

But real operational systems say:

```text
Adding new facts can invalidate previous conclusions.
```

A tentative link may be accepted today and rejected tomorrow.
A derived mass may be recomputed after a calibration correction.
A report may be restated after a source is deprecated.
A property may be introduced into the ontology after the historical period being analyzed.

So the system needs to distinguish:

```text
What evidence exists?
What conclusions are currently accepted?
Why are they accepted?
What would change if this evidence or rule were retracted?
```

This is where truth-maintenance, belief revision, justification tracking, and provenance become relevant again.

---

## 6. The fourth principle: time has several axes

A single timestamp is not enough.

At minimum, the model needs to distinguish:

```text
valid_time
  The time the assertion is about.

transaction_time
  The time the system recorded or changed belief about the assertion.

ontology_time
  The ontology/schema/model version used to interpret the assertion.

rule_time
  The calculation or inference rule version used.

report_time
  The report definition or report execution time.
```

Example:

```text
valid_time:
  2026-06-20 10:00
  The time the silo level reading is about.

transaction_time:
  2026-06-22 09:10
  The time a correction was entered.

ontology_time:
  Ontology version 42
  The version in which fillLevel has current semantics.

rule_time:
  Mass formula version 5
  The formula used to derive mass from fill level.

report_time:
  Report version 7, run on 2026-06-23
```

This allows precise questions:

```text
What did we believe on 2026-06-20?
What do we now believe about 2026-06-20?
What would the old report have shown then?
What does today’s report show about then?
What would today’s rules have concluded using only evidence available then?
What would be restated if we allowed later corrections?
```

Without explicit time axes, these questions collapse into ambiguity.

---

## 7. The fifth principle: reports need an evaluation context

Reports are not just queries. They are semantic programs.

A report should not merely say:

```sql
SELECT mass FROM SiloMass WHERE date = yesterday
```

It should have an explicit evaluation contract:

```text
Report: Daily Silo Mass
entity_time_window: yesterday
ontology_mode: current
knowledge_cutoff: now
ruleset_version: current
source_policy: best_available
restatement_policy: allowed
missingness_policy: explicit
conflict_policy: prefer_confirmed_then_highest_confidence
```

An audit-style report might instead say:

```text
Report: Daily Silo Mass Audit
entity_time_window: 2026-06-20
ontology_mode: as_of_2026-06-20
knowledge_cutoff: 2026-06-20 23:59
ruleset_version: as_of_report_time
restatement_policy: forbidden
source_policy: sources_available_then
```

These are different reports, even if they appear to ask for “the same data.”

This directly addresses the original concern:

> What if a report asks about a time before a property existed?

The answer should not be hidden inside ad hoc null handling.

The report context should determine the behavior:

```text
historical faithful mode:
  use the ontology and report definition as they existed then

current analytical mode:
  use today’s ontology over old data, with explicit missingness behavior

restated history mode:
  use today’s ontology and approved backfills/migrations

audit mode:
  use only evidence known at the transaction cutoff

simulation mode:
  use a hypothetical ontology/ruleset/source policy
```

---

## 8. Dense time series and sparse configuration are both histories

Configuration history and sensor time series are not entirely different kinds of thing.

Both are histories of time-varying properties.

The difference is operational:

```text
Sensor fill level:
  dense, regular or semi-regular, high volume, compressible

Configuration shape:
  sparse, irregular, long-lived, semantically rich

User actions:
  sparse, causally important, audit-sensitive

Derived mass:
  dense or sparse, recomputable, provenance-heavy

Video/media:
  large binary object with timestamps and extracted annotations
```

The unified abstraction is:

```text
history_of(subject, predicate, time_range, evaluation_context)
```

But the storage engines can differ.

A property history may be implemented by:

```text
DenseTimeSeriesProvider
SparseAssertionProvider
ConfigurationDocumentProvider
EventLogProvider
DerivedCalculationProvider
MediaReferenceProvider
ExternalApiProvider
MaterializedProjectionProvider
```

The goal is not one physical storage format.

The goal is a common semantic interface.

---

## 9. SQL still matters

The movement toward assertions, JSON documents, ontology catalogs, and time-series providers can make SQL feel like it is being unwound.

But databases still play essential roles.

SQL is good at:

```text
stable identity
foreign keys
uniqueness
integrity
joins
constraints
indexes
current projections
audit ledgers
transaction boundaries
query planning
human inspectability
tooling
```

So the database should not become merely a coarse blob store.

A reasonable pattern is:

```text
Core relational spine:
  Entity
  EntityType
  Predicate
  Source
  Method
  Assertion
  Rule
  Report
  Version
  Decision

Operational tables:
  Store
  Silo
  Sensor
  Site
  Device
  Series

Versioned documents:
  StoreShape
  SensorCalibration
  ReportDefinition
  FormulaPlan

Dense stores:
  TimeSeriesSamples
  CompressedTelemetry
  MediaStore
  PointCloudStore

Projections:
  CurrentStore
  CurrentSiloState
  CurrentAcceptedLinks
  CurrentMassEstimate
```

The relational model remains the spine.
The richer stores become organs attached to the spine.
The ontology/evaluation layer becomes the nervous system.

---

## 10. Tentative links as defeasible assertions

The Smoothscrape / CompSeek “tentative link → human confirmation → preserved overlay” pattern is a concrete example of the general model.

The current pattern is:

```text
source records
  scraped registrations
  OCR names
  video metadata

candidate links
  machine-generated possible identity/event/video matches

scores
  confidence values and methods

human decisions
  confirm/reject

overlay tables
  preserve human decisions across rebuilds

read paths
  prefer confirmed links and ignore rejected links
```

This can be reframed more generally:

```text
Evidence:
  immutable source records

Hypothesis:
  a possible link or interpretation

Support:
  method, score, evidence edges

Decision:
  human or system accept/reject/override

Belief view:
  currently accepted links under a policy
```

A tentative link is not a weak row in a relation table.

It is a defeasible assertion:

```text
Hypothesis H:
  same_identity(OCRPerson91, RegisteredPerson456)

Support:
  name similarity
  same country
  same club
  same event
  score = 0.87

Decision:
  pending / accepted / rejected / superseded

Current belief:
  accepted only if policy allows it
```

The same pattern applies to industrial data:

```text
Hypothesis H:
  Sensor42 measures Silo17

Support:
  installation record
  data correlation
  wiring plan
  manual confirmation

Decision:
  accepted by engineer

Current belief:
  use Sensor42 as fillLevel provider for Silo17
```

And to configuration:

```text
Hypothesis H:
  ShapeModelV7 correctly describes Silo17 from 2025-03-01

Support:
  engineering drawing
  commissioning measurement
  capacity test

Decision:
  accepted, later superseded
```

This suggests a reusable abstraction:

```text
ConfirmableHypothesis
DefeasibleAssertion
AssertionSupport
AssertionDecision
BeliefView
```

---

## 11. Corrections, retractions, and supersession

A correction should not delete the past.

It should append new knowledge about the past.

Example:

```text
A1:
  Silo17 fillLevel = 4.2m
  valid_time = 2026-06-20 10:00
  transaction_time = 2026-06-20 10:03
  source = manual_laser

C1:
  retracts A1
  transaction_time = 2026-06-22 09:10
  reason = wrong silo selected

A2:
  Silo18 fillLevel = 4.2m
  valid_time = 2026-06-20 10:00
  transaction_time = 2026-06-22 09:11
  source = correction
  derived_from = A1
```

The system can then answer:

```text
What did we believe on June 20?
  A1 applied to Silo17.

What do we now believe about June 20?
  A1 is retracted for Silo17.
  A2 applies to Silo18.

What changed?
  Correction C1 reclassified the reading.
```

Useful correction concepts:

```text
retraction:
  assertion should no longer be used as true

supersession:
  assertion is replaced by a better assertion

revision:
  assertion remains conceptually valid but its value changes

reinterpretation:
  assertion is still evidence, but maps to a different semantic predicate

source invalidation:
  a source or method is marked unreliable for a period

policy change:
  the same evidence is interpreted differently under new rules
```

---

## 12. The role of metadata

The danger is infinite remetadata: every property has metadata, whose metadata has metadata, whose metadata has metadata.

The way to reach a fixed point is to define a small kernel.

Possible kernel:

```text
Entity
Predicate
Assertion
Value
Source
Method
Time
Version
Support
Decision
Provenance
Context
```

Everything else can be expressed using these concepts.

A property definition is an assertion about a predicate.

```text
Predicate fillLevel:
  applies_to = Silo
  unit = metre
  value_type = decimal
  history_kind = dense_time_series
```

A report definition is an assertion about a report entity.

```text
Report DailyMass:
  uses predicate = massEstimate
  default evaluation context = current_restated
```

A source policy is an assertion about precedence.

```text
For fillLevel:
  prefer confirmed online radar
  then manual reading
  then interpolated estimate
```

An ontology version is a set of accepted metadata assertions.

The fixed point is not achieved by avoiding metadata about metadata.
It is achieved by ensuring the metadata uses the same small kernel.

---

## 13. Open world, closed world, and local closed worlds

A normal database often acts closed-world:

```text
If there is no row, then the fact is false.
```

A knowledge base often acts open-world:

```text
If there is no assertion, then we do not know.
```

Operational systems need both, but scoped.

Examples:

```text
Billing ledger:
  closed-world.
  If no credit transaction exists, the user has no credit.

Identity matching:
  open-world.
  If no link exists, we do not know whether two records identify the same person.

Reviewed identity matching:
  locally closed-world.
  If a candidate was reviewed and rejected, treat it as false in that context.

Sensor installation:
  mixed.
  If a sensor is not configured, it may be not installed, unknown, pending, or removed.

Configuration:
  locally closed after commissioning.
  Before commissioning, missing means unknown.
  After commissioning, missing may mean not applicable.
```

So the model needs explicit missingness and closure policies:

```text
unknown
not_applicable
not_observed
not_configured
not_yet_introduced
not_available_under_this_ontology
rejected
retracted
defaulted
derived
backfilled
```

This is essential for historical reports.

---

## 14. Belief views

The system should distinguish raw assertions from accepted beliefs.

A belief view is the accepted extension of the knowledge base under a given context.

```text
BeliefView(context):
  input:
    evidence assertions
    hypotheses
    decisions
    rules
    ontology version
    transaction cutoff
    conflict policy
    source precedence policy

  output:
    accepted facts
    rejected facts
    unresolved conflicts
    provenance graph
```

This can produce views such as:

```text
CurrentOperationalBelief
AuditBeliefAsOfDate
RestatedHistoricalBelief
ExperimentalModelBelief
WhatIfBelief
```

For example:

```text
CurrentOperationalBelief:
  use latest accepted ontology
  include all corrections
  prefer confirmed sensor sources
  allow restatement

AuditBeliefAsOf2026-06-20:
  ontology as of 2026-06-20
  evidence recorded by 2026-06-20 23:59
  no later corrections
  no later source invalidations

ExperimentalBelief:
  use new matching rules
  compare accepted links with current production links
```

This gives a principled way to run reports, dashboards, alerts, and AI agents.

---

## 15. Derivation and provenance

A derived value should know its dependencies.

Example:

```text
MassEstimate(Silo17, 2026-06-20 10:00) = 12.4t
```

should be traceable to:

```text
fillLevel:
  RadarSensor42 sample at 10:00

shape:
  ShapeModelV7

density:
  ProductDensityConfigV3

formula:
  MassFormulaV5

calibration:
  RadarCalibrationC19

ontology:
  OntologyVersion42

evaluation context:
  current_restated
```

Then the system can answer:

```text
Why is this value 12.4t?
Which reports depend on ShapeModelV7?
Which derived values change if CalibrationC19 is retracted?
Which historical report outputs are stale?
What changed between report run A and report run B?
```

This is the bridge between database provenance, truth maintenance, and operational reporting.

---

## 16. Current state as a projection

The “current world” should be a projection, not the only truth.

Current tables are still useful:

```text
CurrentSiloState
CurrentStoreShape
CurrentAcceptedPersonLinks
CurrentEventVideoLinks
CurrentSensorAssignments
```

But they should be understood as materialized belief views.

They are derived from the assertion/evidence/history layer under a named context.

This allows efficient operational queries without giving up history.

```text
CurrentSiloState:
  context = CurrentOperationalBelief
  refreshed after new assertions, corrections, or rule changes
```

Applications can use current projections most of the time.

But when needed, the system can go back to the full trace.

---

## 17. Possible conceptual architecture

A layered architecture might look like this:

```text
1. Source layer
   Raw imports, sensor data, OCR, scraped records, user input, external APIs.

2. Evidence layer
   Immutable or append-only source assertions.

3. Normalization layer
   Canonicalized names, units, timestamps, IDs, parsed values.

4. Hypothesis layer
   Candidate links, inferred relationships, proposed classifications.

5. Decision layer
   Human confirmations, rejections, overrides, corrections.

6. Rule and ontology layer
   Predicate definitions, entity types, derivation rules, source policies.

7. Belief engine
   Computes accepted facts under evaluation context.

8. Projection layer
   Current tables, materialized views, report caches, search indexes.

9. Application layer
   Dashboards, reports, workflows, AI agents, review UIs.
```

The main discipline is that lower layers are mostly append-only, and upper layers are recomputable.

---

## 18. Possible schema sketch

This is not a final schema, but a vocabulary for prototyping.

```text
Entity
  EntityId
  EntityTypeId
  StableKey
  CreatedTransactionId

Predicate
  PredicateId
  Name
  ValueType
  AppliesToEntityTypeId
  HistoryKind
  Unit
  IntroducedInOntologyVersionId

Assertion
  AssertionId
  SubjectEntityId
  PredicateId
  ValueRef
  ValidTimeStart
  ValidTimeEnd
  TransactionId
  SourceId
  MethodId
  AssertionKind
  Confidence
  Status

AssertionSupport
  AssertionId
  SupportingAssertionId
  SupportKind
  Weight

Hypothesis
  HypothesisId
  HypothesisType
  SubjectEntityId
  ObjectEntityId
  PredicateId
  ProposedByMethodId
  Confidence
  CreatedTransactionId

HypothesisSupport
  HypothesisId
  EvidenceAssertionId
  SupportKind
  Weight

Decision
  DecisionId
  TargetKind
  TargetId
  DecisionKind
  DecidedBy
  TransactionId
  Reason

Retraction
  RetractionId
  TargetAssertionId
  TransactionId
  Reason
  ReplacementAssertionId

RuleVersion
  RuleId
  VersionId
  DefinitionRef
  ValidFromTransactionId

OntologyVersion
  OntologyVersionId
  ParentVersionId
  CreatedTransactionId
  Description

EvaluationContext
  ContextId
  OntologyVersionId
  RuleSetVersionId
  KnowledgeCutoffTransactionId
  SourcePolicyId
  MissingnessPolicyId
  ConflictPolicyId
  RestatementPolicyId
```

The `ValueRef` could point to:

```text
inline scalar
JSON document
time-series segment
media reference
external object
derived expression
```

The schema should not force all values into one shape.

---

## 19. Questions to explore

### 19.1 What is the minimal kernel?

Can the whole model be built from:

```text
Entity
Predicate
Assertion
Time
Source
Method
Support
Decision
Context
```

Or are additional primitives required?

Candidates:

```text
Event
Hypothesis
Rule
Version
Projection
ReportRun
```

The research task is to identify which are genuinely primitive and which are derived conveniences.

---

### 19.2 Is a link just an assertion?

A link such as:

```text
PersonA sameAs PersonB
Sensor42 measures Silo17
VideoX correspondsTo EventY
```

could be modeled as an assertion.

But links often have special behavior:

```text
bidirectionality
transitivity
merge/split behavior
confidence
review workflow
identity clustering
conflict resolution
```

Question:

```text
Should links be first-class objects, or just assertions over two entities?
```

Possible answer:

```text
A link assertion is enough for simple relationships.
An identity cluster or merge decision may need to become an entity in its own right.
```

---

### 19.3 What is an entity?

An entity may be:

```text
physical object
logical object
source record
person
sensor
report
rule
ontology version
hypothesis
assertion
```

If everything can be an entity, does the concept become too broad?

Possible discipline:

```text
Entity = stable identity that can be the subject or object of assertions.
```

That may be enough.

---

### 19.4 What should remain as SQL columns?

A practical rule:

```text
Use SQL columns for stable identity, common query surfaces, constraints, operational joins, and current projections.

Use documents for evolving structured configuration.

Use dense stores for high-volume samples.

Use assertions to connect them semantically.
```

But the boundary will need real examples.

Examples to test:

```text
Store.Name
Store.Description
Store.Shape
Silo.Capacity
Silo.FillLevel
Sensor.Calibration
Video.Duration
Person.DisplayName
Event.StartDate
```

For each, ask:

```text
Is it identity-like?
Is it frequently queried?
Does it need constraints?
Does it evolve structurally?
Does it need history?
Is it dense or sparse?
Is it derived?
Is it source-dependent?
```

---

### 19.5 How should source changes be represented?

Example:

```text
Before 2026:
  fillLevel from manual readings

After 2026:
  fillLevel from RadarSensor42

Later:
  RadarSensor42 replaced by RadarSensor88
```

Should this be:

```text
configuration history?
source policy?
property provider history?
sensor assignment assertions?
all of the above?
```

Likely model:

```text
Sensor42 measures Silo17 fillLevel
valid_time = 2026-01-01 to 2026-07-01

Sensor88 measures Silo17 fillLevel
valid_time = 2026-07-01 onward

fillLevel property provider resolves according to these assertions.
```

---

### 19.6 How should derived values be cached?

Derived values may be expensive.

Should derived assertions be stored as facts?

Possible distinction:

```text
observation assertion:
  cannot be recomputed from inside the system

derived assertion:
  can be recomputed from dependencies

published assertion:
  result was used externally or reported officially

cached assertion:
  performance artifact, discardable

certified assertion:
  derived and approved
```

A mass estimate may start as cached, but become published if used in a report or invoice.

---

### 19.7 What does “delete” mean?

In this model, deletion is rare.

Possible delete-like operations:

```text
hide from current belief
retract
supersede
mark erroneous
mark duplicate
merge
split
archive
redact
physically purge for legal/privacy reasons
```

The system needs vocabulary for these.

A dangerous operation like physical deletion should be exceptional and audited.

---

### 19.8 How do reports survive ontology evolution?

A report should either be pinned or floating.

```text
Pinned report:
  bound to ontology version and rule version.

Floating report:
  always uses current ontology, but must declare missingness and restatement behavior.

Restated report:
  uses current ontology and approved backfills.

Audit report:
  uses transaction cutoff and historical ontology.
```

A report compiler could check:

```text
Does every referenced predicate exist in the chosen ontology?
What happens before the predicate was introduced?
Is a backfill rule available?
Is missingness acceptable?
Did the meaning of the predicate change?
```

This makes reporting more like compiling against a semantic interface.

---

## 20. Prototype directions

### Prototype A: Confirmable links

Generalize the Smoothscrape overlay pattern.

Core tables:

```text
link_hypothesis
link_hypothesis_support
link_decision
link_belief_view
```

Use cases:

```text
person identity link
event-video link
match-livestream link
sensor-silo assignment
```

Goal:

```text
Show that the same review/confirmation machinery works across domains.
```

---

### Prototype B: Bitemporal assertions

Build a minimal bitemporal assertion store.

Support:

```text
valid_time
transaction_time
retraction
supersession
current belief view
as-known-then query
current-restated query
```

Use cases:

```text
manual fill-level readings
corrections
configuration changes
mass estimates
```

Goal:

```text
Make the difference between “what we believed then” and “what we now believe about then” concrete.
```

---

### Prototype C: Time-varying property provider

Define an interface:

```text
value_at(subject, predicate, time, context)
history_between(subject, predicate, start, end, context)
```

Implement providers:

```text
SQL sparse history
JSON configuration history
dense synthetic time series
derived mass calculation
```

Goal:

```text
Show that dense measurements and sparse configuration can share one semantic interface without sharing one storage engine.
```

---

### Prototype D: Report evaluation context

Create two reports over the same data:

```text
Audit report:
  as-known-then

Restated report:
  current-restated
```

Then introduce:

```text
a correction
a source change
a new property
a formula change
```

Show how outputs differ and why.

Goal:

```text
Make report semantics explicit and visible.
```

---

### Prototype E: Provenance graph

For one derived value, display:

```text
value
source measurements
configuration versions
rules
ontology version
decisions
corrections
report run
```

Goal:

```text
Show explainability and dependency tracking.
```

---

## 21. Design risks

### Risk 1: Too abstract

A universal assertion model can become intellectually pleasing but practically unusable.

Mitigation:

```text
Always prototype against concrete examples:
  Store shape
  Silo fill level
  Smoothcomp person identity
  Event-video link
  Mass report
```

---

### Risk 2: Too much metadata

Every field could become a metadata object.

Mitigation:

```text
Use the rich model only where history, uncertainty, provenance, correction, or semantic evolution matter.

Keep ordinary operational columns where they are useful.
```

---

### Risk 3: Query complexity

Historical semantic queries can become hard to write.

Mitigation:

```text
Expose named belief views and property providers.
Most application code should not manually join assertion tables.
```

---

### Risk 4: Performance

A pure assertion engine may be too slow.

Mitigation:

```text
Use materialized projections.
Use dense stores for dense data.
Use SQL indexes for current state.
Use caches for derived values.
```

---

### Risk 5: Ambiguous truth

Users may be confused if the system says there are multiple truths.

Mitigation:

```text
Use explicit language:
  as reported then
  current best belief
  restated
  unresolved
  rejected
  retracted
```

---

## 22. A possible slogan

The architecture is not:

```text
Everything is a table.
```

Nor:

```text
Everything is JSON.
```

Nor:

```text
Everything is time series.
```

Nor:

```text
Everything is a graph.
```

A better slogan is:

```text
Everything important is an assertion.
Assertions have time, source, method, support, and status.
Current truth is a belief view over the assertion history.
Storage is optimized per modality.
Reports declare their evaluation context.
```

---

## 23. The research question

The deepest research question is:

> Can we design a small, elegant kernel for operational knowledge that supports dense sensor histories, sparse configuration histories, tentative links, corrections, ontology evolution, report restatement, human confirmation, and provenance — without collapsing into either rigid relational schemas or opaque universal blobs?

That kernel probably needs:

```text
stable identity
assertions
bitemporal time
provenance
defeasible hypotheses
decisions
ontology/rule versions
evaluation contexts
materialized belief views
```

The main challenge is not inventing each piece. Many of the pieces exist in older knowledge representation, temporal databases, event sourcing, provenance systems, truth-maintenance systems, semantic layers, and modern ontology platforms.

The challenge is finding the right practical composition.

---

## 24. Working hypothesis

The promising direction is:

```text
Use SQL as the integrity spine.
Use specialized stores for dense or large data.
Use versioned documents for evolving structured configuration.
Use assertions as the semantic glue.
Use nonmonotonic belief views for current truth.
Use explicit evaluation contexts for reports and AI.
```

This gives a system that can grow over time.

It allows:

```text
simple tables where useful
rich objects where necessary
time series where appropriate
human review where uncertainty exists
corrections without deletion
reports with explicit semantics
ontology evolution without breaking history
```

The result is not one database design.

It is a style of system design.

A system built this way treats the operational world as a historically evolving, partially known, revisable knowledge base — while still using ordinary databases, indexes, documents, and time-series stores where they are strongest.

That feels like the fixed point worth exploring.
