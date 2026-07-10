# The Problem Frame, Sharpened

*Develops §1–§5 of the seed document into a claim precise enough to argue with.*

## 1. The thesis restated

The seed document's fixed-point realization:

> The durable core of the system is not a universal storage format.
> It is a universal interpretation contract over assertions, evidence, time, provenance, and belief.

This document proposes a sharper form of that contract, a name for its two halves, and an argument for why *now* is the right time to invest in it.

## 2. The two-plane model

Every Gneiss-style system has exactly two planes:

**The Ledger plane** — append-only, transactional, small vocabulary. The only thing that is ever *written*. It records evidence: observations, imports, configuration changes, machine-generated hypotheses, human decisions, rule and ontology definitions, corrections. A correction does not modify the ledger; it appends new knowledge *about* earlier entries.

**The View plane** — everything else. Current-state tables, materialized projections, report outputs, search indexes, caches, dashboards, even "the current ontology." All of it is *derived* and *disposable*.

The relationship between the planes is the single invariant everything else hangs off:

> **The Gneiss Contract.** Every view is a pure function of a ledger prefix and an evaluation context:
> `view = f(ledger[0..t], context)`
> Same prefix, same context → same view. Forever.

Corollaries, each of which is a feature users actually ask for:

- **Rebuildability.** You can delete the entire view plane and reconstruct it. (This is the generalization of Smoothscrape's "overlay survives rebuild" pattern — decisions live in the ledger, so re-running the matcher can never destroy them.)
- **Time travel.** "What did we believe on June 20?" is just `f(ledger[0..t_june20], ctx)`.
- **Auditability.** A report run that records its `(t, context)` pair is reproducible byte-for-byte.
- **What-if.** A hypothetical is just a different context (or a ledger branch), not a copy of the database.
- **Safe concurrency of interpretation.** Two teams can hold different beliefs (different contexts) over the same evidence without forking the data.

The seed document's "evidence is monotonic, belief is nonmonotonic" (§5) is exactly this: the ledger only grows; `f` is free to accept less than it accepted yesterday.

## 3. The oldest prior art: double-entry bookkeeping

The strongest *mental model* for Gneiss is not a database technology. It is accounting, which solved nonmonotonic belief over monotonic evidence roughly seven centuries ago:

| Accounting | Gneiss |
|---|---|
| Journal (never erased) | Ledger of assertions |
| Journal entry | Transaction |
| Adjusting / reversing entry | Correction, retraction, supersession |
| Trial balance, statements | Belief views, projections |
| Closed period | Knowledge cutoff |
| As-reported vs as-restated financials | Audit context vs restated context |
| Materiality policy | Conflict / missingness policy |
| Auditor's trace to source documents | Provenance / justification graph |

This analogy earns its keep twice. First, it is design guidance: when unsure how a correction should behave, ask what an accountant would do (answer: post a new entry, never erase; disclose restatements; keep the audit trail). Second, it is *user language* — the seed's Risk 5 ("users confused by multiple truths") is mitigated by vocabulary users' finance departments already accept: *as reported then*, *restated*, *adjusted*.

Where the analogy breaks: accounting has one predicate (monetary amount in an account), a closed world, and no machine-generated hypotheses. Gneiss needs open-world predicates, typed missingness, and a defeasibility layer. But the *discipline* transfers whole.

## 4. Why "meta-architecture"

Gneiss is deliberately not "a database" or "a platform." It is three separable things, adoptable at different depths (see [30-SCOPE-ONION.md](30-SCOPE-ONION.md)):

1. **A contract and vocabulary** — the two-plane model, the kernel concepts, the missingness taxonomy. Adoptable with zero shared code, in any existing system, as design discipline.
2. **A kernel library** — a small implementation of the ledger, belief function, and property providers that individual systems embed.
3. **Optional shared services** — review workbenches, report compilers, cross-system identity. Only if and when multiple systems at level 2 need them.

Calling it a meta-architecture is a commitment: the test of Gneiss is not "did we build the engine" but "do systems designed under this contract stay explainable, correctable, and restatable for a decade." A system can be 100% Gneiss-conformant while being an ordinary web application over an ordinary relational database — any stack — if its schema honors the contract.

## 5. Why now — three converging pressures

**(a) The pattern keeps recurring.** AIMS, CompSeek, Smoothscrape have independently evolved fragments of this: overlay tables, correction flows, source-change histories, "prefer confirmed" read paths. Each fragment was invented ad hoc. The third reinvention is the signal to extract the pattern.

**(b) Corrections and restatement are where operational systems actually bleed.** Happy-path CRUD is a solved problem. The expensive incidents are: the reading attributed to the wrong silo, the calibration that was wrong for a month, the report a customer already saw that now needs restating, the identity match that merged two people. Systems without an assertion/decision/context model handle these with heroics and tribal knowledge. That is precisely the un-differentiated heavy lifting a kernel should absorb.

**(c) LLM agents need exactly this substrate.** This is the genuinely new motivation, absent from the seed document. Agentic systems reading and writing operational data need:

- *grounded reads*: claims with source, method, confidence, and justification attached — a belief view is the natural retrieval surface for an agent, better than raw tables or a vector store;
- *safe writes*: agents should never write facts; they should write **hypotheses** (with support) and, at most, propose decisions. Gneiss's hypothesis/decision layer is a ready-made agent write-path with human oversight built into the model rather than bolted on;
- *revision*: agents are wrong at some rate; a substrate where every acceptance is reversible-by-append makes that rate survivable;
- *reproducibility*: an agent's answer citing `(ledger prefix, context)` can be re-derived and audited.

A Gneiss-style layer turns "AI in operational systems" from terrifying to governed. This may end up the strongest commercial argument for the whole endeavor.

## 6. What Gneiss is NOT

- **Not a universal storage format.** Dense telemetry stays in time-series storage; media stays in blob storage; hot operational tables stay relational. The ledger holds *semantics*, not bulk (see [23-STORAGE.md](23-STORAGE.md)).
- **Not an ontology standard.** No commitment to RDF/OWL. The ontology is data in the ledger, versioned like everything else.
- **Not event sourcing rebranded.** Event sourcing replays *state*; Gneiss replays *belief under a context*. The context parameter — and the defeasibility layer under it — is the difference (event sourcing has no native notion of "this event is now disbelieved but retained").
- **Not a rewrite mandate.** The scope onion exists so existing systems adopt rings, not the whole model.
- **Not a research project in nonmonotonic logic.** We take the 20% of truth-maintenance theory that pays rent operationally (see [11-SURVEY-KR-BELIEF.md](11-SURVEY-KR-BELIEF.md)) and refuse the rest.

## 7. Success criteria for the discussion phase

This corpus has done its job when we can answer, with conviction:

1. Is the five-primitive kernel ([20-KERNEL.md](20-KERNEL.md)) right, or does the seed's larger kernel earn its extra concepts?
2. Two time axes plus context pins ([21-TIME.md](21-TIME.md)) — or does ontology time need to be a real axis?
3. Which ring of the scope onion does each existing system enter at, and what is the first concrete retrofit?
4. Which prototype runs first, and what would make us *abandon* the idea (kill criteria in [32-RISKS.md](32-RISKS.md))?
5. Does anything in the prior-art surveys (10–14) mean we should adopt an existing engine instead of building a kernel?
