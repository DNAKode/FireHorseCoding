# Survey: Knowledge Representation, Belief & Provenance

*Research-agent survey for Gneiss, 2026-07-04. Focus: what is load-bearing vs. dead weight for an assertion-ledger meta-architecture on .NET + SQL.*

---

## TL;DR — top 5 takeaways for Gneiss

1. **Gneiss is a foundations-style reason-maintenance system, not an AGM revision system — and that's the right choice.** Gneiss never edits belief; it re-derives belief from an append-only base under explicit policy. That is the "foundations" side of the classic foundations-vs-coherence debate (Doyle vs. Gärdenfors), and it is the side that every implemented system ended up on. Take from AGM exactly one thing: revision is impossible without an explicit preference ordering, so make precedence/entrenchment first-class, versioned data.
2. **Stratified Datalog is the correct formal skeleton for the belief engine — but stratification must be an enforced schema invariant, not a discovered property.** Give every assertion/decision an explicit rank; reject writes that create defeat cycles; break residual ties with ledger sequence (stratification-through-time, à la Dedalus). Then `accepted :- asserted, not defeated` has a unique, deterministic, explainable model computable in set-based SQL passes. No solver needed.
3. **The ATMS is the right mental model for evaluation contexts and the wrong implementation.** "Context = assumption set, belief view = label satisfaction" is conceptually sound, but vanilla ATMS labels are monotone in assumptions while Gneiss defeat is nonmonotone, and label maintenance is the ATMS's notorious failure mode. Use the correspondence as a design-sanity lens; implement per-context stratified evaluation instead. No production-grade TMS library has existed for decades; the living descendants are incremental Datalog / view-maintenance engines.
4. **Poor-man's provenance (direct derivation edges + method ID + dirty propagation) is why-provenance, and it is enough.** Full semiring polynomials buy you probability and bag-counting composition, which Gneiss doesn't need. Even the liveliest research system (ProvSQL) is a Postgres research extension; nothing mainstream ever shipped polynomials. Steal PROV's three nouns (Entity/Activity/Agent) as vocabulary and stop there.
5. **The single most transferable lesson from 25 years of RDF pain: attach metadata to assertion *tokens* (stable IDs), never to abstract propositions, and never let metadata-assertion imply target-assertion.** RDF 1.2 finally codified both (opaque triple terms, `rdf:reifies` occurrence-tokens) after reification and singleton graphs failed. Gneiss gets this almost for free — an assertion about an assertion is just an assertion whose subject is a ledger ID — and that construction terminates the metadata regress by itself.

---

## 1. Truth Maintenance Systems (JTMS / ATMS)

**JTMS (Doyle 1979), practically.** A dependency graph of nodes (beliefs) and justifications. Each justification has an *in-list* (nodes that must be believed) and an *out-list* (nodes that must be disbelieved — this is the nonmonotonic part). The labeling algorithm assigns each node IN or OUT such that a node is IN iff some justification has all in-list members IN and all out-list members OUT, and support is well-founded (no believing yourself into existence). On contradiction, dependency-directed backtracking retracts an assumption underlying the conflict. Operationally: a JTMS is an **incremental cache of one consistent world**, with `out-list` = negation-as-failure. Its pathologies — multiple admissible labelings, "odd loops" with no labeling at all — are exactly recursion-through-negation, the same thing that breaks Datalog stratification (§3). This is not a coincidence; it's the same problem in two costumes.

**ATMS (de Kleer 1986), practically.** Instead of one world, every node carries a *label*: the set of minimal *environments* (assumption sets) under which it is derivable. Justifications are Horn (monotone); contradictions are recorded as *nogoods* that prune environments everywhere. Query "what holds in context C?" = "which labels contain a subset of C?" — answerable for **all contexts simultaneously** without re-propagation. The price: label maintenance is worst-case exponential in assumptions, and this bites in practice (the diagnosis literature is full of label-explosion mitigation).

**Modern implementations.** Essentially none that are alive. What exists: the Common Lisp code from Forbus & de Kleer's *Building Problem Solvers* (1993, still downloadable, still the best pedagogy), scattered toy repos (e.g., hbeck/jtms), TMS machinery buried inside Cyc. No maintained production-grade TMS library surfaced in 2024–2026 searches. The ideas survived by migrating: incremental view maintenance (DRed), Differential Dataflow / Materialize, DDlog, and build-system invalidation are all "TMS as infrastructure." That migration is itself the lesson: the durable artifact is the *dependency graph with incremental relabeling*, not the TMS API.

**Verdict for Gneiss: adapt.** Keep the dependency-graph + labeling worldview (it *is* your belief engine); do not port TMS algorithms or look for libraries — implement labeling as stratified set-based SQL.

## 2. AGM belief revision

**The postulates in one paragraph.** Alchourrón, Gärdenfors & Makinson (1985) axiomatize rational change of a logically closed belief set K: *expansion* (add), *contraction* (remove φ and enough to stop implying it), *revision* (add φ, restoring consistency; Levi identity: contract ¬φ, then expand). Contraction postulates: closure, success, inclusion, vacuity, extensionality, and the controversial *recovery*. Representation theorems show any compliant operator is *partial meet contraction* — a selection among maximal non-implying subsets — equivalently driven by an *epistemic entrenchment* ordering: when forced to give something up, give up the least entrenched.

**Practical guidance for revision-as-data.** Three usable insights. (1) The representation theorems say revision is **undefined without an extra-logical preference ordering** — for Gneiss: source precedence, method reliability, recency, and manual-decision priority must be explicit, versioned inputs, because they are literally the revision operator. (2) Hansson's *belief base* revision (operate on the finite base, not its infinite closure) is the only computable variant — Gneiss's ledger is a belief base, so you're already there. (3) Iterated revision is where AGM collapses (hence Darwiche–Pearl 1997 and a still-unsettled literature). Gneiss sidesteps iteration entirely by being *foundational*: don't mutate a belief state, re-derive it from evidence + current policy. That is precisely the JTMS/foundations answer, and the AGM-vs-TMS "coherence vs foundations" debate of the early 1990s ended, for builders, with foundations winning.

**The theory–practice gap** is notorious: AGM operates on logically closed theories, gives no algorithms, and computing entrenchment-based revision is generally intractable. Implemented systems (Williams' SATEN and successors) are academic demos. Nothing shipped.

**Verdict for Gneiss: ignore the machinery, adopt one sentence** — "the preference ordering is the revision operator; store it as versioned data."

## 3. Nonmonotonic reasoning made practical (NAF, stratification, WFS, stable models)

**The landscape in brief.** Negation-as-failure: `not p` succeeds when p is underivable — closed-world negation. A Datalog¬ program is *stratified* when no predicate depends on itself through negation; then strata can be evaluated bottom-up in order and there is a **unique perfect model**, computed in polynomial time with ordinary semi-naive evaluation. When negation is cyclic: *well-founded semantics* (Van Gelder–Ross–Schlipf 1991) still gives one canonical three-valued model (cyclic-negation atoms come out *undefined*), polynomial-time; *stable-model semantics* (Gelfond–Lifschitz 1988) gives zero-or-many two-valued models and is NP-hard — that's ASP (§4). Engineering translation: stratified = deterministic pipeline; WFS = deterministic pipeline plus an honest "unresolved" bucket; stable models = combinatorial search.

**The realistic threat to stratification** is not "decisions about decisions" (a decision *hierarchy* just adds strata) but **defeat defined in terms of acceptance** — e.g., "A is defeated if a currently-*accepted* contradicting assertion outranks it." That one innocent-looking rule is recursion through negation.

**Standard remedies** (all field-proven): (i) define defeat over *raw evidence plus a deterministic total preorder* (precedence, then valid-time, then ledger sequence as final tiebreak) so winners are computable without consulting acceptance — the ledger sequence makes the program *locally stratified through time*, exactly the trick Statelog and Dedalus (Alvaro et al. 2009) formalized; (ii) make stratification a schema constraint: an explicit rank/level on decisions, decisions may target only lower-ranked items, cycle-check at write time; (iii) where genuine mutual defeat must be representable, use WFS and surface *undefined* as a first-class `contested` belief status — for an operational system with human adjudication, "contested, awaiting decision" is a *feature*, not a semantics bug; (iv) Grosof's courteous logic programs show that any defeasible rule set becomes deterministic once you impose a total priority order — which Gneiss's ledger order supplies for free.

**Verdict for Gneiss: adopt** — stratified Datalog¬ as the belief-engine semantics, WFS-style `contested` as the escape hatch, stratification enforced at write time.

## 4. ASP (Clingo) and defeasible logics

Clingo (Potassco) is mature and alive — v5.8.0, April 2025; solid C API; Python, Lua, Rust, Java bindings. **No official or maintained .NET binding found** (P/Invoke over the C API is feasible but you'd own it). ASP would express acceptance policies *beautifully* — choice rules, `#minimize`/weak constraints for preference, and the defeasible-logic tradition (Nute's defeasible logic, Grosof's courteous LP, García & Simari's DeLP) is literally "rules with exceptions and priorities."

But the verdict writes itself from Gneiss's own requirements. A solver performs *nondeterministic model search*; Gneiss requires *deterministic, auditable, replayable* acceptance. If your acceptance policy ever genuinely needs stable-model search — multiple incomparable belief views, solver picks one — that is a policy bug (an under-specified precedence), not a feature request. And operationally: grounding blows up on ledger-sized data, and you'd bolt an opaque C++ runtime onto a .NET+SQL system to compute something a stratified program computes in linear passes.

The one place ASP earns its keep: **offline, as an executable specification.** Write the acceptance policy in ASP; run it against sampled ledgers; diff against the SQL/C# engine as a differential-test oracle; use it to explore policy corner cases before committing. (On pure-.NET Datalog: sharpalog exists — a C# port of Jatalog with stratified negation — but appears dormant; recent maintenance unverified. For Gneiss-scale data you want the rules compiled to SQL anyway.)

**Verdict for Gneiss: adapt** — ASP as offline policy prototyping/test oracle; hand-rolled stratified evaluation in SQL/C# for the engine; never embed the solver in the serving path.

## 5. Open world / closed world / LOCAL closed world

The endpoints are familiar: OWA (description logics, RDF) — absence means unknown; CWA (SQL, Datalog) — absence means false. The load-bearing literature is the middle: **local closed-world / local completeness**. Etzioni, Golden & Weld (AAAI'94/KR, then AIJ 1997) introduced `LCW(φ)` statements — "the KB contains *all* instances satisfying φ" — with tractable inference and, crucially, rules for how LCW statements *survive updates* (new information can destroy a completeness guarantee). Parallel database lineage: Motro 1989 (completeness as an integrity constraint), Levy 1996 (answering queries completely from incomplete databases), and the modern KG revival — Razniewski, Darari, Galárraga et al. on completeness statements and the *partial completeness assumption* (a heuristic, not a guarantee; don't confuse the two).

How practical systems actually declare closure: metadata tables ("feed F is authoritative and complete for competition C through date D"), reconciliation counts, and — the best modern analogy — **streaming watermarks**: a Flink/Kafka watermark is precisely a per-scope closure assertion ("no more events ≤ t will arrive"), revocable and consumed downstream to convert absence-of-data into evidence-of-absence. Gneiss closure declarations should be assertions themselves: subject = (scope, predicate), value = complete-through, with their own source, method, and defeasibility.

**Missingness vocabulary.** The best operational prior art is not academic: **HL7 v3 `nullFlavor`** codes, refined over decades of clinical data exchange — UNK (unknown), NA (not applicable), NASK (not asked), ASKU (asked but unknown), NAV (temporarily unavailable), MSK (masked/withheld), NI (no information). Map to Gneiss: `unknown` (open world default), `not_observed` (looked-and-couldn't vs never-looked — HL7 splits these, and the split earns its keep), `not_applicable`, `not_yet_introduced` (concept absent from the ontology at that valid time — Gneiss-specific, keep it), `rejected` (claim existed, defeated), `absent_closed` (absence derived from a closure declaration — the only absence that licenses a confident "no").

**Verdict for Gneiss: adopt** — closure declarations as first-class defeasible assertions with scope + predicate + complete-through, plus an HL7-informed missingness enum distinguishing *why* a value is missing.

## 6. Provenance theory (semirings, why/how/where)

**The theory.** Green, Karvounarakis & Tannen (PODS 2007): annotate base tuples with elements of a commutative semiring; join multiplies, union/projection adds; then one framework instantiates to lineage, why-provenance (Buneman/Khanna/Tan 2001), how-provenance (the full polynomial in N[X]), plus counting, trust, access-control, and probability semirings by homomorphism from the polynomials. The genuinely useful theorem for an engineer: **N[X] polynomials are universal** — if you record how-provenance you can later reinterpret it in any semiring without recomputation. The honest corollary: if you'll never need probability or bag-counting composition, you don't need polynomials.

**Who shipped it.** Research systems only: Orchestra, Perm, **GProM** (maintenance level unclear), and **ProvSQL** (Senellart; Postgres extension, v1.5.0 on PGXN, MIT, demonstrably active — update-provenance demo at ProvenanceWeek June 2025). No mainstream commercial DB ever shipped semiring provenance. Nineteen years post-PODS-2007, that's a verdict.

**Poor-man's provenance, precisely characterized:** for each derived fact store the set of direct input assertion IDs + the rule/method ID (+ ledger positions of inputs at derivation time). That is why-provenance with one level of how per step; recursive expansion reconstructs the full explanation tree; reverse edges give dirty-propagation for incremental invalidation. It supports everything Gneiss needs — explanation, defeat propagation, recomputation scoping, audit — and nothing it doesn't.

**Verdict for Gneiss: adapt** — direct derivation edges + method ID; skip polynomials unless probabilistic belief scoring ever becomes real, and if it does, look at ProvSQL before building.

## 7. W3C PROV (PROV-DM / PROV-O)

The model: **Entity** (thing with fixed aspects), **Activity** (process over time), **Agent** (bears responsibility), wired by `wasGeneratedBy`, `used`, `wasDerivedFrom`, `wasAttributedTo`, `wasAssociatedWith`, `actedOnBehalfOf`, with "qualified" n-ary patterns when relations need attributes. W3C Recommendation since 2013; real adoption in scientific workflows, archives, some government data — and in nearly every deployment, only a small subset of the vocabulary is used, because the qualified-relation pattern is punishingly verbose.

For Gneiss the mapping is clean and the value is nomenclature, not machinery: Assertion ≈ Entity, MethodRun/ingestion-job/matching-pass ≈ Activity, Source-system/operator ≈ Agent (with `actedOnBehalfOf` neatly covering "operator O confirmed a link proposed by matcher M on behalf of org X"). Naming columns/edges `generated_by`, `used`, `derived_from`, `attributed_to` costs nothing, ends naming debates, and buys a free PROV-O export view if a regulator or partner ever asks.

**Verdict for Gneiss: adapt** — steal the three nouns and six verbs as canonical names; do not adopt RDF serialization, qualified patterns, or the ontology.

## 8. Statement-level metadata in RDF (reification → named graphs → RDF-star/1.2)

Twenty-five years of the exact problem Gneiss faces, with a scorecard. **Classic reification** (`rdf:Statement` + subject/predicate/object triples): 4× blowup, no semantic link between the reification and the actual triple, universally judged a failure. **Named graphs**: metadata on statement *groups* — genuinely successful for source/dataset-level metadata (every quad store), but per-statement use means singleton graphs, which is a workaround wearing a standard's clothes. **RDF-star → RDF 1.2**: triple terms let a triple appear inside another; as of early 2026, RDF 1.2 Concepts and Semantics are W3C Candidate Recommendations with implementations invited — final REC status not verified as of this writing.

Two design decisions from RDF 1.2 are the transferable gold. First, quoted triple terms are **opaque and unasserted**: saying something *about* a statement does not assert the statement. That is exactly Gneiss's evidence/belief split — a retraction references an assertion without endorsing it. Second, the late-added **`rdf:reifies` / reifier** mechanism distinguishes the abstract triple (*type*) from an *occurrence token*, because the working group discovered that metadata almost always attaches to occurrences ("this source said X on Tuesday", twice, differently) — the type/token distinction the original RDF-star proposal missed and had to retrofit.

Gneiss's construction — every assertion gets a ledger ID; assertions-about-assertions are ordinary assertions whose subject is that ID — is the token model, and it terminates the regress structurally: level-2 metadata is just more assertions targeting the same kind of ID, with no new representational machinery at any level. The only rule needed: referencing an ID never implies accepting the assertion behind it (acceptance is the belief engine's job).

**Verdict for Gneiss: adopt the two lessons** (token-level IDs; reference ≠ assertion), **ignore RDF itself.**

## 9. Temporal knowledge graphs (brief)

The academic TKG literature (temporal embeddings, TKG completion/forecasting benchmarks) is ML research — irrelevant to Gneiss. What is *operationally* proven: **Wikidata qualifiers** (point-in-time, start/end, plus rank + references — a billion-scale, human-curated system doing valid-time-plus-source-plus-preference on statements, shallow semantics but real); **XTDB 2.x** (full bitemporality per SQL:2011); **Datomic** (transaction time only — a deliberate scope cut worth noticing); and **SQL:2011 in mainstream engines** — SQL Server system-versioned temporal tables give *transaction time* natively (handy for Gneiss's materialized belief views: audit of the view itself for free), while application/valid time must be ordinary columns. The bitemporal model itself (Snodgrass & Jensen, 1990s) is settled science; use their vocabulary exactly and resist inventing synonyms.

**Verdict for Gneiss: adopt bitemporal columns on the ledger; ignore the TKG-embedding literature entirely.**

---

## The two claims evaluated

### Claim 1: "ATMS environments ≈ Gneiss evaluation contexts"

**Verdict: sound as a conceptual isomorphism; adopt as a mental model and design-review lens; reject as an implementation. One real semantic mismatch.**

The structural mapping holds: assumption ↔ context-selectable premise (an accepted decision, a source-validity interval, a policy version); environment ↔ evaluation context; label satisfaction ↔ membership in the belief view; nogood ↔ policy-conflict constraint. Thinking this way yields genuinely useful discipline — e.g., it forces you to enumerate exactly which things a context is allowed to vary, and it predicts that belief views compose by environment union until a nogood fires.

Three caveats, in descending order of importance. **(1) Monotonicity mismatch — the substantive flaw.** Vanilla ATMS justifications are Horn; labels are monotone: adding assumptions only ever adds beliefs. Gneiss acceptance is nonmonotone in assumptions: adding the assumption "retraction decision D is accepted" *removes* assertions from the belief view. To express that in an ATMS you need out-assumptions/NATMS extensions (Dressler; de Kleer's "Extending the ATMS") or you must encode every potential defeater's *absence* as its own assumption — at which point labels enumerate defeater power-sets and explode. **(2) Economics.** The ATMS's entire value proposition is amortizing derivation across exponentially many contexts queried simultaneously. Gneiss has a handful of named, slowly-changing, materialized contexts; recomputing each with a stratified program is simpler, deterministic, and explainable. **(3) No temporal semantics** — bitemporality has to be bolted on either way. Also note: no living ATMS implementation exists to borrow.

### Claim 2: "Belief views as a stratified Datalog program"

**Verdict: right formalism, and stratification is realistic — but only as an *enforced invariant with a designed tiebreaker*, not as an emergent property. The escape clause ("decision chains form a hierarchy") is doing more load-bearing work than its phrasing admits.**

What genuinely doesn't break stratification: decisions about decisions per se. An acyclic "aboutness" graph of any depth just adds strata; evaluation stays bottom-up and deterministic.

What realistically breaks it, in the order you'll actually hit it: **(1) Defeat defined via acceptance.** The moment a policy says "A is defeated if an *accepted* contradicting assertion outranks it" — the most natural way to write last-write-wins or source-precedence conflict resolution — you have `accepted → not defeated → accepted`: recursion through negation, same stratum. This will be proposed in the first design week; it is the JTMS odd-loop problem wearing SQL clothes. **(2) Preference cycles**: source A over B over C over A for different predicates is fine (stratify per predicate), but for the same predicate it yields mutual defeat with no stratification. **(3) Self-undermining decisions**: a decision that invalidates the source whose data justified making that decision. Rare, but real in long-lived systems, and it is a cycle.

Remedies, all standard: **(a)** Define `defeated` over *raw evidence plus a deterministic total preorder* — precedence rank, then valid time, then **ledger sequence as the final tiebreaker**. Because every assertion has a unique position in an append-only ledger, the program becomes locally stratified through time (the Statelog/Dedalus construction), and a total order makes courteous-LP-style defeasibility deterministic (Grosof). This single move eliminates threat (1) and de-cycles most of (2). **(b)** Materialize the aboutness rank: an explicit level column on decisions, a write-time check that a decision targets only lower-level items — converting "stratification holds because..." from an architectural hope into a CHECK-constraint-grade guarantee. **(c)** For residual genuine conflicts you do *not* want silently tie-broken, adopt the well-founded semantics move: both parties come out *undefined*, surfaced as a first-class `contested` status routed to human adjudication — which for a system whose axioms make human decisions first-class is not a compromise but a fit. Implementation is then unglamorous and good: per-stratum semi-naive set-based SQL, full recompute per context initially, DRed-style incremental (via dependency edges) when volume demands it.

---

## Sources

**Verified via web (July 2026):**
- W3C RDF 1.2 Concepts (CR): https://www.w3.org/TR/rdf12-concepts/ · https://www.w3.org/news/2026/w3c-invites-implementations-of-rdf-1-2-concepts-and-abstract-data-model-and-rdf-1-2-semantics/ · https://w3c.github.io/rdf-new/spec/
- Clingo: https://potassco.org/clingo/ · https://github.com/potassco/clingo (v5.8.0, 2025) — no maintained .NET binding found
- ProvSQL: https://provsql.org/ · https://github.com/PierreSenellart/provsql (v1.5.0) · https://dl.acm.org/doi/10.1145/3736229.3736253 · GProM: https://github.com/IITDBGroup/gprom · Senellart, *Provenance in Databases*: https://pierre.senellart.com/publications/senellart2019provenance.pdf
- XTDB bitemporality: https://docs.xtdb.com/concepts/key-concepts.html
- LCW: https://link.springer.com/chapter/10.1007/3-540-45331-8_5 · Etzioni & Golden: https://www.semanticscholar.org/paper/bb4a4d29ab4599157502c1960425196a39cb6ad1 · Galárraga completeness: https://luisgalarraga.de/completeness-in-kbs/
- hbeck/jtms (toy JTMS): https://github.com/hbeck/jtms · sharpalog: https://github.com/andrzejolszak/sharpalog (maintenance unverified) · Temple U. TMS notes: https://cis.temple.edu/~ingargio/cis587/readings/tms.html

**From the literature (canonical):** Doyle 1979; de Kleer 1986 (+ *Extending the ATMS*); Dressler NATMS 1988; Forbus & de Kleer *Building Problem Solvers* 1993; AGM 1985; Hansson belief-base revision; Darwiche & Pearl 1997; Gelfond & Lifschitz 1988; Van Gelder–Ross–Schlipf 1991; Grosof courteous LP 1997; Nute defeasible logic; Alvaro et al. *Dedalus* 2009; Green–Karvounarakis–Tannen PODS 2007; Buneman–Khanna–Tan 2001; Motro 1989; Levy 1996; Etzioni–Golden–Weld 1997; Snodgrass & Jensen 1994; W3C PROV-DM 2013; HL7 v3 nullFlavor.

**Flagged as unverified:** final REC status of RDF 1.2 as of July 2026; GProM maintenance; sharpalog activity; absence of any .NET Clingo binding (none surfaced — treat as "assume you'd build it").
