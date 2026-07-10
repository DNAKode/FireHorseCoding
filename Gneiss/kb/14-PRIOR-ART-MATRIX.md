# Prior-Art Matrix: Where Gneiss Is Redundant and Where It Is Novel

*Synthesis of surveys [10](10-SURVEY-TEMPORAL-DATA.md), [11](11-SURVEY-KR-BELIEF.md), [12](12-SURVEY-INDUSTRY.md), [13](13-SURVEY-INCREMENTAL.md). The question this document answers: **should we adopt an existing engine instead of building a kernel — and if not, what exactly is the novel composite?***

## The requirements

| # | Requirement |
|---|---|
| R1 | Bitemporality (valid time + transaction time) |
| R2 | Retraction/correction without deletion (append-only history) |
| R3 | Defeasible hypotheses + human accept/reject decisions as first-class durable data that survive rebuilds |
| R4 | Ontology/schema versioning with historically faithful interpretation |
| R5 | Explicit evaluation contexts for queries/reports (cutoffs + precedence + missingness + restatement as one declared object) |
| R6 | Heterogeneous per-modality storage under one semantic layer |
| R7 | Provenance/justification of derived values |
| R8 | Incremental materialized belief views |
| R9 | Composes with a conventional SQL spine rather than replacing it |

## The matrix

Legend: ● cover · ◐ partial · — none. (Condensed from the surveys; see them for nuance.)

| System / pattern | R1 | R2 | R3 | R4 | R5 | R6 | R7 | R8 | R9 |
|---|---|---|---|---|---|---|---|---|---|
| XTDB v2 | ● | ● | — | — | ◐ | ◐ | — | — | — |
| Datomic | ◐ | ● | ◐ | ◐ | ◐ | — | ◐ | — | — |
| SQL Server temporal tables | ◐ | ◐ | — | — | — | — | — | — | ● |
| MariaDB bitemporal | ● | ◐ | — | — | — | — | — | — | ● |
| PostgreSQL 18 + extensions | ◐/● | ◐ | — | — | — | — | — | ◐ | ● |
| TerminusDB / Dolt | ◐ | ●/◐ | — | ◐ | — | — | — | — | —/◐ |
| Marten (event sourcing) | ◐ | ● | — | ◐ | — | ◐ | ◐ | ● | ● |
| KurrentDB | ◐ | ● | — | ◐ | — | — | ◐ | ◐ | ◐ |
| Palantir Foundry | ◐ | ◐ | ● | ● | ◐ | ● | ◐ | ● | ◐ |
| MDM/ER (Senzing, Zingg Ent.) | — | ◐ | ●/◐ | — | — | — | ◐ | ◐ | ◐ |
| Feature stores (Feast et al.) | ◐ | — | — | — | ◐ | ◐ | — | ● | ◐ |
| Semantic layers (MetricFlow, Cube) | — | — | — | ◐ | ◐ | ● | — | ◐ | ● |
| OMOP / FHIR | ◐ | ◐ | — | ◐ | — | ● | ● | — | ● |
| ERP effective dating (Workday) | ● | ◐ | — | — | ◐ | — | — | — | ● |
| Accounting practice | ◐ | ● | — | — | ◐ | n/a | ◐ | n/a | n/a |
| Anchor Modeling / Data Vault | ◐ | ◐ | — | ●/— | — | — | ◐ | — | ● |
| DBSP/Feldera, Materialize | — | ◐ | — | — | — | — | — | ● | ◐ |

## What the matrix says

**1. Every row has real coverage somewhere; no row covers the composite.** The four surveys reached the same verdict independently:

- *Temporal survey:* "No surveyed system offers bitemporal, append-only, provenance-carrying semantics **as a layer over** SQL Server/SQLite/Postgres — which is precisely the slot Gneiss targets." (R1+R9 together is the composition gap.)
- *Temporal + industry surveys:* **R3 and R5 are universal gaps.** The hypothesis lifecycle with durable human adjudication exists only as fragments (Foundry edit overlays, Senzing TRUSTED_ID, Zingg Enterprise constraints); the multi-axis evaluation context exists nowhere — every "as-of" in industry is time-only.
- *Industry survey:* "Nobody versions semantics at query time" — R4-at-query-time is an open wound across semantic layers, feature stores, and OMOP alike.

**2. The novelty is the composition, not the parts — confirming seed §23.** Each mechanism has proven prior art to copy: reified transactions (Datomic), bitemporal columns and query taxonomy (Snodgrass/XTDB), edit overlays (Foundry/Senzing), as-of joins (feature stores), projection rebuilds (Marten), dirty-set recompute (build systems), stratified acceptance (Datalog literature), missingness vocabulary (HL7), correction lexicon (accounting). Gneiss's contribution is one contract that makes these compose: **assertions + decisions + contexts over an append-only ledger, with everything else derived.**

**3. Buy-vs-build verdict.** Nothing to buy *as the kernel*. Three adoption decisions worth taking seriously instead:

- **Marten as substrate** for Postgres-based systems: it already ships the ledger-ish store + projection machinery + rebuild discipline (R2, R8, R9). The question is whether Gneiss's assertion schema fits Marten's event-stream shape or fights it (assertions are finer-grained than aggregate streams). → Prototype question, see [31-PROTOTYPES.md](31-PROTOTYPES.md) P2.
- **XTDB as a reference implementation** to test semantics against (its bitemporal SQL is the closest executable model of R1/R2), not as a component.
- **Feldera as the L4 escape hatch** if incremental view maintenance ever outgrows per-key recompute — per the incremental survey, start at L1 with L2's tables designed in, keep L0 as the differential-testing oracle.

**4. The strongest cross-survey convergences** (independent sources agreeing is the best evidence this corpus produced):

| Convergence | Sources |
|---|---|
| Human decisions must be keyed, append-only data replayed over rebuilt machine output — and *the overlay is only as durable as its key* | Foundry, Zingg, Senzing, OMOP (survey 12); Smoothscrape's own overlay pattern (seed §10) |
| Derived state is disposable; evidence is forever ("views are cattle, evidence is pedigree") | All four surveys, unanimously |
| Machine-generated cluster/entity IDs must not be promised stable; decisions anchor to record-pair/claim level | Senzing/Jonas (survey 12); kernel §5 cluster graduation; belief-engine open question 2 |
| Belief must be a deterministic fold with an enforced total order, never a solver search | KR survey (Dedalus/courteous LP); incremental survey (fold-per-key); belief engine I6 |
| The preference ordering IS the revision operator; store it as versioned data | AGM takeaway (survey 11); survivorship rules (survey 12); context policy versioning ([24-CONTEXTS.md](24-CONTEXTS.md)) |
| Stamp every output with the definition-version hash that produced it — cheap, and it leapfrogs the whole semantic-layer category | Survey 12 takeaway 4/6; report runs in [24-CONTEXTS.md](24-CONTEXTS.md) |

**5. Corrections the surveys forced on the design docs** (kept honest):

- The ATMS≈contexts analogy has a genuine monotonicity mismatch — downgraded to "design lens" in [22-BELIEF-ENGINE.md](22-BELIEF-ENGINE.md) §3.
- Stratification must be *enforced at write time* (rank column + check), not assumed — already invariant I6, now with literature backing (Dedalus, courteous LP) and a named failure mode ("defeat defined via acceptance" — forbidden rule shape).
- Missingness taxonomy should adopt HL7's asked-and-unknown vs never-asked split and the `absent_closed` concept (closure-derived absence) — incorporated in [24-CONTEXTS.md](24-CONTEXTS.md).
- Closure declarations should themselves be defeasible assertions with (scope, predicate, complete-through) — the watermark pattern — incorporated in [24-CONTEXTS.md](24-CONTEXTS.md).
