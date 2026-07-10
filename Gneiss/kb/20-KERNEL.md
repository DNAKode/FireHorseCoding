# The Kernel: A Five-Primitive Proposal

*Answers seed §12 and §19.1–19.3. Position taken: the kernel is smaller than the seed's twelve concepts. Five primitives; everything else is a stance.*

## 1. The minimization argument

The seed proposes a kernel of ~12 concepts (Entity, Predicate, Assertion, Value, Source, Method, Time, Version, Support, Decision, Provenance, Context) and asks which are genuinely primitive (§19.1). Test applied here: **a concept is primitive only if the belief function's signature or the ledger's write path cannot be expressed without it.** Everything else is a *stance* — a role played by entities and assertions — and adding it to the kernel would be buying vocabulary at the price of machinery.

Result:

```
KERNEL (5):        Entity, Assertion, Transaction, Justification, Context
STANCES (the rest): Predicate, Source, Method, Value¹, Hypothesis, Decision,
                    Rule, Report, Version², Retraction, ...
```

¹ Value is not a primitive *concept*; it is a field of Assertion with a typed representation problem (see [23-STORAGE.md](23-STORAGE.md)).
² Version is not a concept at all — it is what the ledger's transaction order gives you for free, plus naming.

## 2. The five primitives

### Entity
A stable identity. No fields. Everything you might want to make claims about — silos, people, sensors, predicates, rules, reports, contexts, sources — is an entity. Identity policy (what natural key, who mints, when to split/merge) is *governance per entity type*, recorded as ontology assertions, not kernel structure.

This answers §19.3: yes, "stable identity that can be subject or object of assertions" is enough, and the breadth is a feature — it is exactly what lets the kernel describe itself.

### Assertion
Immutable: `(id, subject, predicate, value, valid_time, status_at_birth)` inside a transaction envelope carrying `(source, method, confidence)`. Two properties do all the heavy lifting:

1. **Immutability** — an assertion is never edited; later knowledge about it is expressed by *other* assertions targeting it.
2. **Addressability** — an assertion has an id and can be the *subject* of later assertions. This is the one-level reification move, and it is what stops the metadata regress (§4 below).

### Transaction
The write envelope: monotonically ordered id (the real transaction time; wall clock is attached data), actor, reason, batch handle. Why primitive rather than "just an assertion attribute": the ledger's total order *is* the system's spine — determinism of belief views, as-known-then queries, and decision acyclicity (see [22-BELIEF-ENGINE.md](22-BELIEF-ENGINE.md)) all lean on it. It must be structural, not conventional.

### Justification
Directed support edges: assertion ← {assertions..., rule-version}. Could this be encoded as assertions-about-assertions? Yes, mechanically. It is kept primitive anyway, for one reason: **it is the only structure the belief engine and provenance queries traverse in the hot path**, and burying it in generic assertion form would force every traversal through predicate-dispatch. This is the kernel's single concession to performance. (Counter-position worth debating: fold it into Assertion as a `derived_from` list. Cheaper conceptually, less queryable. See D1 in [40-DISCUSSION-AGENDA.md](40-DISCUSSION-AGENDA.md).)

### Context
The evaluation parameter set. Primitive because the belief function's signature is `f(ledger_prefix, context)` — you cannot state the Gneiss Contract without it. Yet contexts are *also* entities described by assertions in the ledger (named, versioned, auditable). This dual nature is deliberate: the kernel needs the concept; the ledger stores the instances. Details in [24-CONTEXTS.md](24-CONTEXTS.md).

## 3. Everything else is a stance

| Seed concept | Reduction |
|---|---|
| Predicate | Entity + defining assertions (`unit=metre`, `applies_to=Silo`, `history_kind=dense`, `introduced_in=V42`). The ontology is just the accepted belief view over predicate-entities. |
| Source, Method | Entities referenced from the transaction/assertion envelope. Their reliability is itself asserted (and defeasible — source invalidation is a decision). |
| Hypothesis | An assertion born with `status_at_birth = proposed`. Nothing else is special about it; admission to belief is the context's business. |
| Decision | An assertion whose subject is another assertion: `accepts(A17)`, `rejects(A17)`, `supersedes(A17, by=A22)`. Actor and reason come from its transaction. |
| Retraction / supersession / invalidation | Decision kinds (predicates on the decision assertion). |
| Rule | Entity whose versioned definitions are value documents; rule-versions appear in justifications of derived assertions. |
| Report / ReportRun | Entities + assertions (definition doc; run records context version + ledger high-water mark + output hash). |
| Version | A name pinned to a transaction id. Ontology version 42 = the set of accepted ontology assertions as of tx T₄₂. |
| Provenance | Not a thing; a *query* over justifications and transactions. |
| Event | Not kernel. An event is an entity (something happened) plus assertions about it. Promoting Event to the kernel would smuggle in event-sourcing's worldview; Gneiss's unit of record is the claim, not the happening. |

## 4. The fixed point: why the regress stops

Seed §12 worries about infinite meta-metadata. The regress stops not by forbidding metadata but by **closure under self-description**: assertions about assertions use the *same* five primitives, so each meta-level adds zero new schema. Concretely:

- A fact is an assertion about an entity.
- A decision is an assertion about an assertion.
- An ontology definition is an assertion about a predicate-entity.
- A context definition is an assertion about a context-entity.
- A source-reliability policy is an assertion about a source-entity.

One schema, arbitrarily deep reference, no new machinery per level. That is the fixed point in the seed's title, made structural. (There is also a cute formal echo: the belief view itself is a *fixpoint* — the least fixed point of the acceptance rules over the ledger; see [22-BELIEF-ENGINE.md](22-BELIEF-ENGINE.md).)

**Discipline that keeps this sane:** meta-assertions are expected to be *shallow in practice* (level 2, rarely 3 — a decision about a decision is an appeal; beyond that, nothing legitimate). Depth is unbounded in the model but should be monitored as a smell in practice.

## 5. Links and identity clusters (§19.2 answered)

A link is just an assertion whose value is an entity reference: `measures(Sensor42, Silo17)`, `sameAs(OCR91, Person456)`. Bidirectionality, confidence, review workflow — all already covered by hypothesis status + decisions.

The exception is **identity clustering**. `sameAs` pairs are evidence; but "these five records are one person" is a *cluster*, and merge/split behavior makes clusters awkward as mere edge-sets:

- Position: an identity cluster is an **entity in the view plane** — a derived belief output (`PersonCluster99` with membership assertions), recomputed from accepted `sameAs` links under the context's clustering rule (transitive closure with conflict handling).
- Merges and splits are then *decisions about links* (accept/reject sameAs edges), and the cluster derivation absorbs them. The cluster id stability problem (users bookmark cluster 99) is handled by minting cluster entities in the ledger once they are referenced externally — at which point they graduate from view to evidence, with membership still revisable.

This "graduation" pattern (derived → referenced → pinned) recurs; it is the same as the seed's cached → published assertion distinction (§19.6).

## 6. Kernel invariants

1. **I1 Append-only:** ledger rows are never updated or deleted (sole exception: redaction destroys value payloads, never structure).
2. **I2 Immutable assertions:** all change is by new assertions targeting old ones.
3. **I3 Total transaction order:** belief determinism and decision acyclicity hang off it.
4. **I4 Purity:** every view derivable from (ledger prefix, context); cache keys must include both.
5. **I5 One schema at every meta-level:** no special tables for decisions-about-decisions etc.
6. **I6 Decisions target the past:** a decision may only reference assertions with lower transaction ids (guarantees stratification — see belief engine).
7. **I7 Typed missingness:** belief views never emit bare NULL for semantic absence.

## 7. What would falsify this kernel

Worth keeping honest. The kernel is wrong if:

- Per-assertion envelope cost makes even *sparse* histories too expensive in practice (mitigation exists for dense data — descriptors, [23-STORAGE.md](23-STORAGE.md) — but if config histories also choke, the model fails).
- Real usage needs assertions edited in place (would indicate the correction vocabulary is too weak).
- Contexts proliferate beyond human comprehension (semantic debt — [32-RISKS.md](32-RISKS.md)); a kernel that only experts can query has failed as a *fixed point of understanding*.
- The stance-reduction leaks: e.g., decisions turn out to need workflow state machines rich enough that "decision = assertion" becomes a fiction maintained by convention.
