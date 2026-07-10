# Glossary

*Fixed vocabulary for the Gneiss discussion. Where the seed document and this corpus differ, this file wins. Terms marked ◆ are kernel primitives (argued in [20-KERNEL.md](20-KERNEL.md)); everything else is defined in terms of them.*

## The two planes

| Term | Definition |
|---|---|
| **Ledger** | The append-only record of transactions. The only thing ever written. Physically: a set of tables, not necessarily one. |
| **View plane** | Everything derived: projections, current tables, report outputs, caches, indexes. Disposable and recomputable. |
| **Gneiss Contract** | Every view is a pure function of (ledger prefix, evaluation context). |

## Kernel

| Term | Definition |
|---|---|
| **Entity** ◆ | A stable identity that can be the subject or object of assertions. Nothing more — entities have no fields of their own. |
| **Assertion** ◆ | An immutable claim: subject entity, predicate, value, valid-time interval, plus the envelope of the transaction that recorded it. Assertions are addressable (they can themselves be subjects of later assertions). |
| **Transaction** ◆ | The write envelope: transaction time (system-ordered), actor, reason, batch. Assertions exist only inside transactions. |
| **Justification** ◆ | A support edge: this assertion is grounded in these other assertions and/or this rule version. The structure the belief engine and provenance queries traverse. |
| **Evaluation context** ◆ | A named, versioned parameter set that turns the ledger into a belief view: data cutoff, definition cutoff, source precedence, conflict policy, closure/missingness policy, restatement policy, hypothesis admission policy. |

## Stances (roles, not types)

A *stance* is a role an entity or assertion plays; stances need no new kernel machinery.

| Term | Definition |
|---|---|
| **Predicate** | An entity described by assertions (unit, value type, applies-to, history kind, introduced-in-version). |
| **Source** | An entity referenced by an assertion's envelope: where the claim came from (sensor, operator, import, scraper, model). |
| **Method** | An entity: how the claim was produced (radar_model_v3, name_similarity_v2, manual entry). |
| **Hypothesis** | An assertion in `proposed` status — admitted to belief only per the context's admission policy, usually requiring a decision. |
| **Decision** | An assertion whose subject is another assertion (or hypothesis): accept, reject, supersede, invalidate-source. Carries actor and reason via its transaction. |
| **Rule** | An entity whose versioned definition (a value document) computes derived assertions; referenced by justifications. |
| **Report** | An entity whose definition is a query + context binding; a **report run** records (context version, ledger high-water mark, output hash). |

## Change vocabulary (all are appended assertions/decisions — nothing edits the past)

| Term | Definition |
|---|---|
| **Retraction** | Decision: target assertion should no longer be accepted as true. Evidence remains. |
| **Supersession** | Decision: target assertion is replaced by a named better assertion. |
| **Revision** | New assertion giving a new value for the same (subject, predicate, valid time); belief view prefers it by policy. |
| **Reinterpretation** | New assertion mapping old evidence to a different predicate or subject (the wrong-silo case). |
| **Source invalidation** | Decision: a source or method is unreliable for a time range; defeats matching assertions in belief views. |
| **Redaction** | The one destructive act: value payload destroyed for legal/privacy reasons; assertion skeleton and justification structure remain; recorded by a decision. Exceptional and audited. |

## Belief

| Term | Definition |
|---|---|
| **Belief view** | The set of accepted assertions (plus typed missingness and unresolved conflicts) computed from a ledger prefix under a context. |
| **Accepted / Defeated** | An assertion's status within one belief view. Not properties of the assertion itself — the same assertion can be accepted in one context and defeated in another. |
| **Defeater** | Whatever removes acceptance: retraction, supersession, source invalidation, precedence loss, closure policy. |
| **Projection** | A materialized belief view (e.g., `CurrentSiloState`). Cache key: (ledger high-water mark, context version). |
| **Provider** | An implementation of the property-history interface (`value_at`, `history_between`) over some physical store, resolving a predicate under a context. |

## Time

| Term | Definition |
|---|---|
| **Valid time** | The interval the assertion is *about* (half-open, `[from, to)`). |
| **Transaction time** | When the ledger recorded it. Total order by transaction id; wall clock attached. |
| **Data cutoff** | Context parameter: ignore transactions after this point ("as known then"). |
| **Definition cutoff** | Context parameter: which ontology/rule/report definitions to use (they live in the ledger too, so this is also a transaction-time pin). |
| **Knowledge horizon** | The earliest transaction time at which as-known-then queries are honest — data imported from pre-Gneiss systems has import-time transaction times; before the horizon, "as known then" is an approximation. |

## Missingness (belief views return these, never bare NULL)

`unknown` · `not_applicable` · `not_observed` · `not_configured` · `not_yet_introduced` (predicate didn't exist in the pinned ontology) · `not_available_under_this_context` · `rejected` · `retracted` · `redacted` · `defaulted` · `backfilled` — semantics in [24-CONTEXTS.md](24-CONTEXTS.md).

## User-facing language (Risk 5 mitigation)

| System state | Say |
|---|---|
| Audit context result | "as reported at the time" |
| Current operational context result | "current best belief" |
| Restated context result | "restated" |
| Conflict, no policy winner | "unresolved — needs review" |
| Hypothesis pending decision | "suggested, awaiting confirmation" |
| Defeated by decision | "rejected" / "retracted" (as applicable) |
