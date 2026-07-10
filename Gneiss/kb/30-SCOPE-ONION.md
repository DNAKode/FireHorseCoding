# The Scope Onions: How Much Gneiss, Where, and When to Stop

*The user-posed question — "what would suitable scope onion layers look like?" — has two distinct answers, because scope varies along two independent dimensions: how deeply an organization adopts Gneiss machinery (**adoption layers**), and which data within one system gets the rich model (**modeling rings**). Conflating them is how meta-architectures turn into rewrite mandates. Each layer/ring has explicit stop rules.*

## Onion A: Adoption layers (organization/codebase depth)

### A0 — Discipline (vocabulary + design rules, zero shared code)

Gneiss as a checklist applied to ordinary systems:

1. Facts that matter are never UPDATEd; corrections append.
2. Human match/review decisions live in their own keyed tables (claim-keyed, not surrogate-keyed) and are never overwritten by pipelines.
3. Effective-dated configuration gets `[valid_from, valid_to)` columns, half-open, UTC.
4. Every imported/derived row carries source + method + load transaction.
5. Normalized values keep their verbatim source value beside them (OMOP pattern).
6. Reports and exports are stamped with a definition-version hash and a data high-water mark.
7. Semantic absence is typed (at minimum: unknown / not-applicable / rejected), never bare NULL.
8. Destructive deletion is a named, audited, exceptional procedure.

**Value:** most of Gneiss's operational pain-relief, free of any framework. **Stop rule:** stay at A0 for any system that is working and rarely corrected. *A0 is not a consolation prize — it is the fixed point expressed as culture.*

### A1 — Patterns in place (per-system tables, no shared library)

Overlay/decision tables, correction ledgers, and effective-dated config implemented natively inside an existing app's schema. Smoothscrape's confirmed-links overlay is already an A1 fragment. **Enter when** a system has live correction/link-review pain. **Stop rule:** don't build shared code for one consumer.

### A2 — Gneiss library (the recommended target)

A small kernel library embedded per application, in whatever ecosystem the adopting systems live (module names below are illustrative — the boundaries are the conceptual content; see [03-NOTATION.md](03-NOTATION.md)):

```
Gneiss.Kernel      — types (Entity, Assertion, Tx, Justification, Context), belief fold, invariants
Gneiss.Store.Sql   — ledger schema + append path + projection maintenance (SQLite/Postgres/SqlServer)
Gneiss.Providers   — IPropertyHistory + sparse/document/dense-descriptor/derived providers
Gneiss.Review      — hypothesis queues, decision capture, three-band triage plumbing
```

Each application owns its ledger; no shared runtime service; the *contract* is shared, the data is not. **Enter when** two systems have independently needed A1 machinery (the third-reinvention signal — already true across Smoothscrape/AIMS). **Stop rule:** the library must never require an app to abandon its existing tables — Gneiss composes (R9) or it has failed.

### A3 — Federation (shared service, cross-system identity)

A knowledge sidecar owning cross-system entities and links (the same person in CompSeek and Smoothscrape; the same site in AIMS and an ERP). Apps publish assertions to it; it publishes belief views back. **Enter when** ≥2 A2 systems demonstrably need *shared* entities — not before; federated identity is where complexity explodes (Senzing's drifting-ID lesson applies across systems). **Stop rule:** if a periodic export/import reconciliation suffices, A3 is vanity.

### A4 — Platform (ontology manager, review workbench, report compiler, agent gateway)

Foundry-shaped. The industry survey's explicit warning applies: a small team should imitate Foundry's *patterns* (actions-as-only-write-path, proposals, overlays) and never attempt its *infrastructure*. **Enter when** there are external users of the meta-system itself. Realistically: not a current-decade goal, and that is fine.

## Onion B: Modeling rings (within one system, per-predicate)

Which data earns the rich model, ordered by pain-per-effort density. Everything outside the outermost adopted ring stays plain SQL **by design, forever**.

| Ring | Scope | Why this order |
|---|---|---|
| B1 | Confirmable links + human decisions (identity matches, event–video, sensor→silo) | Highest value density: decisions are the most expensive data the system owns (survey 12), and losing them on rebuild is the canonical failure. Smallest surface. |
| B2 | Corrections/retractions on manual + imported facts | The wrong-silo story; where trust is won. Needs bitemporal assertions for a handful of predicates only. |
| B3 | Sparse configuration histories (shapes, calibrations, assignments, capacities) | Effective-dated documents + binding assertions; unlocks honest historical derivation. |
| B4 | Derived values + justification edges | Staleness propagation, `why()`; requires B2+B3 inputs to be worth it. |
| B5 | Report evaluation contexts | Compiling reports against declared contexts; the restatement diff. Payoff grows with B2–B4 coverage. |
| B6 | Dense telemetry binding | Descriptors + semantic masks over native TS stores. Cheap *if* B3 exists (calibrations/assignments are the hard part, and they're B3). |
| B7 | Closure declarations + agent interface | `absent_closed`, completeness watermarks, agent read/write contracts. |

**Ring stop rule (per predicate):** apply the [23-STORAGE.md](23-STORAGE.md) §6 decision procedure; if a predicate lands on "plain column," it stays a plain column even in a ring-7 system.

## Per-system entry map (positions to confirm with Govert)

| System | Today | Entry move | Ring path |
|---|---|---|---|
| **Smoothscrape** | A1 fragment exists (overlay tables) | Retrofit vocabulary (P-1 probe), then rebuild overlay on claim-keys (P1) — decisions survive matcher rebuilds *by construction* | B1 → B2 (scrape corrections) → B7 (closure: "event fully scraped") |
| **CompSeek** | shares Smoothscrape's domain | Reuse P1 tables/library — the "same machinery across domains" proof (seed Prototype A's goal) | B1 → B2 |
| **AIMS** | conventional + historian | Start at B3 (shape/calibration/assignment histories) — industrial config is where its corrections bite; B1 for sensor→silo assignment review | B3 → B4 (mass + why) → B5 (audit vs restated mass reports) → B6 |
| **Future ERP-adjacent** | greenfield | Design at A2 from day one; rings B2+B3+B5 native | — |

The meta-system reading: **Gneiss succeeds at A0/A2 or not at all.** If the discipline plus an embeddable library doesn't pay for itself inside individual systems, no federation layer or platform would have rescued it — it would only have hidden the failure behind infrastructure.
