# The Evolvable Spine: Bedrock, Ceremony, and Consumer Contracts

*Added 2026-07-04 from discussion. Govert's question — the actual fixed-point question: is there a spine that stays coherent while itself evolving (within constraints), or is that going too far, so that a small immutable bedrock must stand everything up? And separately: every code component that talks to an entity system becomes a constraint on its evolution — can a model of evolution, which code is implemented against, relax those constraints? ("The code knows a future system might not have that sensor.")*

## 1. Three strata of permanence

The answer proposed here: both, stratified. The thing that must be immutable is far smaller than the kernel; the kernel itself is already an evolvable layer.

**Bedrock** — unamendable; changing these means it is a different system, not a new version. Two commitments (possibly three):

> **B1.** Testimony is never silently destroyed or altered. All change is by append or by recorded, certified ceremony (seal → purge → record; redaction protocol).
> **B2.** Every interpretation is derivable from surviving testimony plus a declared context, degrading monotonically under loss.
> **B3** (candidate)**.** Self-description: the system's own definitions, policies, and amendments live inside it, subject to B1 and B2.

That is the whole bedrock. Note what is *not* in it: no five primitives, no bitemporality, no claim keys — those are spine.

**Spine** — the current kernel: the five primitives, the envelope fields, invariants I1–I9. Evolvable, under ceremony: amendments are proposed, discussed, adopted as recorded acts; changes are additive by strong default (the Anchor Modeling lesson: never repurpose, only add); kernel versions are themselves ledger data, so a definition cutoff pins not just the ontology but *which kernel semantics* interpret a region of history. The I1→I1′ amendments proposed in [25-IMPERFECTION.md](25-IMPERFECTION.md) are the worked example — the corpus is currently *performing* spine evolution by ceremony, which is the best demonstration that the concept is coherent.

**Flesh** — ontology, predicates, stances, policies, contexts. Evolves freely as data under machinery that already exists.

## 2. The fixed point, relocated

The seed asked for a "fixed-point model." The honest resolution of Govert's question:

> **What is fixed is not the content but the manner of change.** The spine is a fixed point of the *evolution operator*: applying legitimate change to the system yields a system with the same change-discipline. The amendment ceremony is the constitution.

The regress ("who amends the amendment procedure?") bottoms out in bedrock B1/B2 — which are small enough to defend as *definitional*: a system that silently rewrites testimony or whose interpretations are not derivable is simply not Gneiss, the way a ledger with erasures is simply not double-entry bookkeeping. This is "the small bedrock to stand everything on" *and* "a spine that coherently evolves" — the choice posed was false once permanence is stratified.

## 3. Consumer contracts: constraint tracking as data

The operational half of the question: code constrains evolution, invisibly, until something breaks. Make the constraints data:

- Every consumer (report, service, integration, agent, dashboard) declares its dependencies as assertions: `requires(ReportDailyMass_v7, predicate=massEstimate, unit=tonne, presence=required)`; `reads(SiloDashboard, fillLevel, presence=absence_tolerant)`.
- **Impact analysis becomes a query**: "who constrains retiring this sensor's predicate?" — the exact "tracking relationships and constraining change only when needed" Govert named as the useful first step. Where no contract binds, change is free *and provably so*.
- **CI/compile gate**: contracts checked against the target ontology version (the report compiler's predicate-existence check, generalized to every consumer). A migration plan is then: the list of binding contracts + per-contract resolution (update consumer, add backfill rule, declare tolerated absence).

## 4. Code that expects absence

"The code knows a future system might not have that sensor" is not aspiration — it is what the provider API already forces: `BeliefValue = value | typed-missingness`, never a nullable scalar ([31-PROTOTYPES.md](31-PROTOTYPES.md) P3). A consumer of Gneiss data handles `not_configured`, `retired`, `not_yet_introduced` as *values in the domain*, at compile time, on day one. Absence stops being an exceptional path that rots; it is the ordinary path, exercised constantly. This — more than any process — is what relaxes evolution constraints: code written against typed missingness is *already* written for the system's future shapes.

Two supporting mechanisms:

- **Predicate lifecycle** as ontology assertions with valid time: `proposed → active → deprecated(sunset) → retired`. Belief views return `retired` missingness after sunset; absence-tolerant consumers keep working; `required` consumers show up in the impact query the day deprecation is proposed, not the day retirement breaks them.
- **Volatility classes** as predicate metadata: hardware-bound predicates (sensors, series bindings) are declared volatile — churn is *expected*; identity-bound predicates are stable-class. Contract-strictness defaults follow the class: requiring a volatile predicate as `presence=required` is a lint warning. This is "a model of evolution that a system is implemented against": the expectation of future change is itself declared data that tooling reads.

## 5. What this buys, and its cost

Bought: schema evolution stops being fearful because old data stays interpretable under old definitions (definition cutoff), impact is queryable before change, and absence is typed. Migration = new predicates + recorded backfill rules, not ALTER + prayer.

Cost, honestly: contract declarations are one more thing to keep truthful (stale contracts are worse than none — they veto changes nobody depends on). Mitigations: derive contracts from observed reads where possible (a projection of actual query telemetry beats a manifest nobody updates); expire unrefreshed contracts. At A0/A1 the "ceremony" is nothing heavier than a pull-request review — the stratification must not platform-ify small systems.

## 6. Agenda

**D15** — where exactly is the bedrock/spine boundary? (Is B3 bedrock or spine? Is per-ledger total order bedrock?) **D18** interacts: if world-model stances are flesh, the spine must never mention "sensor" or "silo" — audit the kernel docs for smuggled domain assumptions.
