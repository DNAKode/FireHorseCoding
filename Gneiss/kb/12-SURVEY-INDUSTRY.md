# Survey: Industry Platforms & Operational Patterns

*Research-agent survey for Gneiss, 2026-07-04. How industry actually handles bitemporal, correctable, human-curated operational knowledge. Verified against public sources July 2026; confidence notes inline.*

---

## TL;DR — top 5 takeaways for Gneiss

1. **The edit-overlay pattern is proven at scale and is exactly Gneiss's R3.** Palantir Foundry stores human edits as a durable, keyed edit log *separate from* pipeline-produced datasets, and replays them over every rebuild; Zingg Enterprise treats confirmed match/no-match decisions as hard constraints incremental runs must respect; Senzing does it with `TRUSTED_ID` attributes injected as data. Three independent industries converged on the same answer: **human decisions are keyed, append-only assertions replayed over rebuilt machine output — never merged into it.** Copy the mechanics, especially "the overlay only survives if the join key survives."
2. **Do not promise stable entity IDs.** Senzing's design (and Jeff Jonas explicitly, in writing) says entity IDs *must* drift because "one must be able to change their mind about the past." Stable things are *record keys* and *human assertions about record pairs/clusters*; the entity ID is a materialized view. Gneiss should anchor confirm/reject decisions to record-pair (or record-to-anchor) level, never to machine-generated cluster IDs.
3. **Point-in-time-correct joins are the only mass-deployed "evaluation context" (R5) in industry — and they only implement one axis.** Feature stores solved "as-known-then" rigorously (as-of joins against transaction time to prevent label leakage), but nobody bundles cutoff + ontology version + source precedence + missingness into one declared context object. That composite is genuinely novel territory for Gneiss; the vocabulary to steal is `as-of join`, `point-in-time correctness`, `training-serving skew`.
4. **Nobody versions semantics at query time — this is the industry's open wound and Gneiss's opportunity.** Semantic layers (MetricFlow, Cube, LookML) version metric definitions in git only; changing a definition silently restates all history, and there is no "evaluate metric as defined on date X" runtime. OMOP pins vocabulary versions per-study by convention, not mechanism. Gneiss's R4+R5 (ontology version inside the evaluation context) has no off-the-shelf peer.
5. **Accounting is the right UX metaphor for corrections, up to a point.** Closed periods, reversing entries, as-reported vs as-restated, "Big R vs little r" — this vocabulary is battle-tested, intuitive to operations people, and 21 CFR Part 11-style audit rules ("original value never obscured") are literally R2. But accounting has no confidence levels, no defeasible hypotheses, and one privileged ledger — use it for the *correction/reporting* UX, not for the *belief* layer.

---

## 1. Palantir Foundry Ontology

**Pattern.** Object types + link types are a typed semantic graph projected over ordinary datasets. Users mutate objects only through **Actions** — declared, permissioned, validated transaction types (Gneiss's "method" field, institutionalized). Edits land in the object database and are captured in a **writeback dataset** distinct from the backing dataset; the **Funnel** service (Object Storage V2) merges datasource rows + user edits into the indexed view. Ontology schema changes go through **branches + proposals** ("pull requests for the ontology," with reviews); as of May 2026, *Global Branching* is GA — one branch can span data, code, and ontology changes end-to-end before merging to Main.

**What survives rebuilds.** The edit history is stored independently and **reapplied by primary key every time the writeback dataset is rebuilt** — user edits survive full pipeline re-syncs as long as primary keys stay stable. This is the canonical industrial implementation of Gneiss's decision-overlay requirement.

**R-coverage.** R2 partial (edit log is append-only; datasets are transactional/versioned but not bitemporal); R3 **cover**; R4 cover (branches/proposals); R5 partial (branching ≈ context, but no declarative "evaluate under context C"); R6 cover; R7 partial (dataset/job lineage, not assertion-level); R8 cover (Funnel is incremental indexing); R9 partial (it *replaces* your spine); R1 partial (transaction-time time travel on datasets; valid time is your problem).

**Steal / Avoid.** **Steal:** edits-as-keyed-overlay replayed over rebuilds; Actions as the *only* write path (every write has a type, actor, and validation rule); proposals/approvals on ontology changes; branching as the change-testing metaphor. **Avoid:** building a Funnel-style bespoke indexing microservice, a custom object database, or horizontal-scale sync infrastructure — a small team on .NET+SQL gets 90% of this with an `Edits` table keyed by (object PK, property, action_id) and a view that coalesces edits over pipeline output.

---

## 2. MDM & Entity Resolution (Splink, Zingg, Senzing)

**Pattern.** Classic MDM vocabulary: **match keys / blocking keys** narrow candidates; probabilistic or rules scoring produces auto-match, auto-non-match, and a middle band routed to **clerical review** queues; **survivorship rules** (source precedence, most-recent-wins, per-attribute trust scores) assemble a **golden record**. Stewards work review queues; merges/splits are logged.

**The overlay problem — three different answers:**
- **Splink** (MoJ, open source, Splink 4 era): human labels are *training data* — pairwise labels feed m-probability estimation, and notably the API **ignores `clerical_match_score` values, treating every label as a perfect match**. There is no built-in mechanism to persist clerical decisions as constraints across re-runs; that is explicitly the user's problem. Splink gives you scores; you build the decision store.
- **Zingg**: labeled pairs train the model (active-learning loop, ~30–40 labeled pairs bootstrap it). Zingg **Enterprise**'s incremental flow ("living clusters") preserves confirmed matches and confirmed separations as **hard constraints** that incremental merge/split re-evaluation must respect — the open-source tier does not do this.
- **Senzing**: fully incremental, self-correcting ("re-resolve" + redo queue). Human overrides are injected **as data**: `TRUSTED_ID` attributes force records together or apart regardless of feature agreement. Crucially, Senzing is explicit that **entity IDs are not stable** — records move between entities as the system "changes its mind about the past." Jonas's recommended consumption pattern: resolve entity ID at read time via API, or subscribe to affected-entity change feeds; batch snapshots are the last resort.

**What survives rebuilds.** Source records and their keys; human pair/cluster decisions *if* stored as data (Senzing's TRUSTED_ID, Zingg Enterprise constraints). Entity IDs, cluster membership, and golden records do **not** — they are views.

**R-coverage.** R3 partial-to-cover (Zingg Enterprise, Senzing; Splink none); R2 partial (Senzing's re-resolution is genuinely nonmonotonic belief revision); R7 partial (Senzing "why" explanations; survivorship rules are per-attribute provenance); R1/R4/R5 none.

**Steal / Avoid.** **Steal:** the three-band triage (auto-accept / clerical queue / auto-reject) and the word "clerical review"; overrides-as-data (a confirm/reject is an assertion with subject=record-pair, ingested like any other evidence — this collapses R3 into R2); Senzing's affected-entity notification pattern for downstream invalidation; per-attribute survivorship as declarative source-precedence rules (feeds R5). **Avoid:** persistent entity IDs as foreign keys in downstream tables; storing steward decisions against cluster IDs (clusters die on re-run); Splink's implicit stance that decision persistence is out of scope — in Gneiss it's the center.

---

## 3. Feature Stores (Feast, Tecton→Databricks, Snowflake — 2026 status)

**Pattern.** A feature store serves the *same* feature values offline (training) and online (inference). The core trick is the **point-in-time-correct (as-of) join**: for each labeled event at time *t*, join the latest feature value with timestamp ≤ *t* — never later — eliminating training-data leakage and training/serving skew. 2026 status: **Feast** remains the open-source default (LF AI & Data; real maintenance burden per third-party evaluations); **Tecton was acquired by Databricks (announced Aug 2025, ~$900M)** and is being folded into the Lakehouse/Agent Bricks stack; Snowflake and Databricks both ship native feature-store capability tied to their catalogs.

**Why this is Gneiss's R5 in disguise.** A training-dataset build is a report with a declared evaluation context: "evaluate every feature *as known at* each label's timestamp." It is an as-known-then query over transaction time, mechanized and mainstream. The limitation is equally instructive: the context has exactly one dimension (time). No feature store lets you also pin "feature definition version v3" or "exclude source S" — feature *definition* changes silently restate history unless teams manually version feature names (`user_ltv_v2` — the poor man's R4, ubiquitous in practice).

**What survives rebuilds.** Timestamped feature logs (append-only event history). Materialized online stores are disposable projections.

**R-coverage.** R1 partial (transaction time only); R5 partial (single-axis, but rigorous); R8 cover (materialization from offline to online store); R2/R3/R4 none/naming-convention-only.

**Steal / Avoid.** **Steal:** the term *point-in-time correctness* and the as-of join as the primitive query operator of the whole belief store; "training/serving skew" as the argument for one semantic interface over per-modality storage (R6) — it's the same value served through two materializations; timestamp-column discipline (every fact carries event time + ingestion time). **Avoid:** the online/offline dual-store complexity unless latency demands it; `_v2` suffix versioning as your R4 answer.

---

## 4. Semantic Layers (dbt/MetricFlow, Cube, LookML, Malloy)

**Pattern.** Metrics as declarative programs (YAML/LookML/Malloy source) compiled to SQL against storage — measures, dimensions, entities, joins defined once, queried by many tools. 2026 status: **dbt Labs and Fivetran completed their merger June 1, 2026**; MetricFlow is open source again and bundled into the combined platform, with a native MCP server so AI agents query governed definitions. **Cube** repositioned as an "agentic analytics platform" (semantic layer + MCP for agents). **LookML** persists under inconsistent Google investment; **Malloy** remains an influential experimental language, not a mainstream successor.

**The restatement gap (verified by absence — medium-high confidence).** All of these version definitions **in git only**. Change a metric's formula and every historical query silently returns restated numbers; no engine supports "evaluate revenue as defined on 2025-03-01" at query time. The industry's workarounds are social: definition change review (PR approval), changelogs, and forking metric names. Cube/dbt tout "everything as code — version control, CI, isolated environments," which is real governance but *deploy-time*, not *query-time*, versioning.

**What survives rebuilds.** Definitions (in git) and warehouse data; every computed number is disposable. Nothing links a historical *result* to the definition version that produced it unless you log it yourself.

**R-coverage.** R4 partial (git-versioned semantics, no runtime pinning); R5 partial (a semantic model *is* a declared context, minus time/version axes); R8 partial (Cube pre-aggregations, dbt incremental models); R6 cover (one metric API over heterogeneous storage); R1/R2/R3 none.

**Steal / Avoid.** **Steal:** metrics-as-reviewed-code (PR workflow on semantic definitions = Foundry proposals, cheaper); the entity/measure/dimension vocabulary; Gneiss report contracts should **record the semantic-program version hash in every report output** — that single discipline leapfrogs the entire category. **Avoid:** assuming git history is a queryable version store; building a universal SQL-generation layer (adopt one if needed; Gneiss's value is the context semantics, not SQL compilation).

---

## 5. Data Lineage in Practice (OpenLineage/Marquez, dbt, Foundry)

**Pattern.** **OpenLineage** is the open standard: jobs emit START/COMPLETE/FAIL events with input/output datasets plus extensible **facets**; **Marquez** is the LF AI & Data reference server. Baseline granularity is **job/dataset-level**. The `ColumnLineageDatasetFacet` adds column-level lineage — populated automatically by the Spark integration and SQL-parser-based integrations; dbt's platform ships column-level lineage in its Explorer. Foundry's lineage is dataset/build-level via its build graph.

**Honest cost assessment.** Job-level lineage is cheap and near-free to adopt (wrap pipeline steps, emit events). Column-level is achievable where a machine can parse the transformation (SQL, Spark plans) and roughly *unachievable* for arbitrary imperative code, OCR pipelines, or ML inference — precisely Gneiss's messiest links. Marquez's own changelog shows column-lineage correctness/performance still being patched into 2025–26; treat it as maturing, not solved. Nobody in this category does *row/value-level* provenance — that only exists where the data model itself carries it (OMOP, FHIR, and Gneiss's assertion design).

**What survives rebuilds.** The lineage event log (append-only run history — incidentally a decent transaction-time record of "what ran when with which code version").

**R-coverage.** R7 partial (dataset/column granularity, not per-assertion); R5 partial (run events capture code version + inputs = reproducibility context); others none.

**Steal / Avoid.** **Steal:** the run-event envelope (job, run ID, code version, inputs, outputs, facets) as the schema for Gneiss's "method + source" on every derived assertion — emit OpenLineage-compatible events even if no server consumes them yet; facets as the extensibility mechanism. **Avoid:** chasing automated column-level lineage for non-SQL pipelines; because assertions already carry source/method, Gneiss gets value-level provenance *by construction* and only needs job-level lineage on top.

---

## 6. Healthcare Data Models (OMOP CDM, FHIR Provenance/AuditEvent)

**Pattern (OMOP).** Every clinical event row carries the mapped standard `*_concept_id` **and** the verbatim `*_source_value` **and** `*_source_concept_id` — canonical mapping and raw evidence live side by side in the same row, permanently. Unmappable codes get `concept_id = 0` with source preserved (explicit missingness policy — a mapping failure is recorded, not dropped). `*_type_concept_id` states *how* the record entered (EHR order vs pharmacy claim vs patient-reported) — provenance-of-method as a first-class column. **Era tables** (drug eras, condition eras) are pure derivations from exposure records under published rules — rebuildable views, never sources. Vocabulary versions ship via Athena releases; studies pin vocabulary version by convention (R4 by discipline, not mechanism).

**Pattern (FHIR).** `Provenance` records generation: target resource, agents (who/what), entities (derived-from, W3C PROV-aligned), optional signatures. `AuditEvent` records *usage* — the spec itself splits "how this came to be" from "who touched it." Every FHIR resource carries `versionId` + history interactions: updates create versions, never overwrite.

**What survives rebuilds.** Source values and source concepts survive everything, including vocabulary upgrades — you can re-map the entire database to a new ontology version and audit the delta because the input was never discarded. Eras are regenerated.

**R-coverage.** R2 partial (FHIR versioning; OMOP re-ETL convention); R4 partial (versioned vocabularies, conventional pinning); R6 cover (one CDM over source heterogeneity); R7 cover (source_value + type_concept_id; FHIR Provenance); R1 partial (valid-time start/end dates everywhere; transaction time weak in OMOP).

**Steal / Avoid.** **Steal:** the **source_value column pattern** — every mapped/normalized value in Gneiss stores its verbatim input beside the mapping (silo telemetry raw readings, OCR raw strings beside matched identities); `type_concept_id` as a controlled vocabulary for "method"; era derivation as the template for materialized belief views (published rules, deterministic, disposable); `concept_id = 0` as an explicit unmapped-marker rather than NULL. **Avoid:** FHIR's full resource-versioning machinery (heavyweight); OMOP's assumption of periodic full re-ETL — Gneiss needs continuous correction.

---

## 7. Accounting / Financial Restatement as Mental Model

**Pattern.** The journal is append-only; errors are fixed by **adjusting entries** (new entries referencing the period they correct), never by editing posted entries. **Reversing entries** cancel accruals by posting the negation. **Period close** freezes a reporting boundary; post-close corrections either restate prior periods (**"Big R" restatement** — material, reissue the financials) or adjust in the current period (**"little r" revision**), per ASC 250-style materiality policy. **As-reported vs as-restated** views coexist permanently (financial data vendors serve both). 21 CFR Part 11 / GxP audit-trail rules: computer-generated, timestamped trails recording who/what/when/old-and-new value, and — the key phrase — changes "shall not obscure previously recorded information."

**How far the analogy carries.** Remarkably far: append-only journal = R2; posting date vs effective date is folk bitemporality (R1); period close = knowledge cutoff (R5); the Big-R/little-r materiality decision *is* Gneiss's restatement policy, decided per report contract; as-reported vs as-restated = same query under two evaluation contexts; Part 11 audit trails = the compliance-grade floor for the assertion log. This vocabulary is the strongest available for Gneiss's operator-facing UX — "this month's silo report is closed; corrections post to next period unless material" needs no training.

**Where it breaks.** (a) Accounting entries are certain by fiat — no confidence scores, no defeasible hypotheses, no machine-suggested entries awaiting confirm/reject; the metaphor covers *corrections*, not *belief*. (b) One privileged ledger, no multi-source conflict or source precedence. (c) The balancing invariant (debits=credits) gives accounting a global integrity check Gneiss's open-world assertions can't have. (d) Valid-time support is shallow — one effective date, not intervals.

**R-coverage.** R2 cover (conceptually); R1 partial; R5 partial (close + restatement policy); R3/R4/R6–R8 n/a.

**Steal / Avoid.** **Steal:** the entire correction lexicon (close, adjust, reverse, restate, as-reported/as-restated, materiality threshold); Part 11's audit-trail requirements as acceptance criteria for the evidence log; period close as the UX for report freezing. **Avoid:** stretching it to the hypothesis/link layer — a tentative identity match is not a journal entry, and forcing certainty semantics onto defeasible data would be actively harmful.

---

## 8. ERP Effective-Dated Configuration (SAP, Workday)

**Pattern.** Effective-dated entities (SAP infotypes with BEGDA/ENDDA; SuccessFactors effective-dated MDF objects; Workday effective-dated business objects) hold **gapless, non-overlapping validity intervals** — each change is valid "from that date forward" until superseded; the last record ends 9999-12-31. Sparse config history falls out naturally: store only change points; the value at date *d* is the record whose interval covers *d*. Workday goes further and is quietly the most mainstream bitemporal system in existence: it distinguishes **correction** (fix the existing effective-dated record — "we recorded it wrong") from **rescind** (undo as if never entered), and its reporting exposes both **"as-of effective date"** (valid time) and **"as-of entry moment"** (transaction time) — dual-axis time travel shipped to thousands of enterprises. (High confidence on the pattern; exact current Workday terminology not verified against 2026 docs.)

**What survives rebuilds.** The effective-dated record chain is the system of record; nothing is derived, so nothing rebuilds — the fragility instead is retroactive edits triggering downstream recalculation (payroll retro-processing is ERP's version of Gneiss's belief-view invalidation, and it is notoriously the hardest part of every HR/payroll system).

**R-coverage.** R1 cover (Workday) / partial (SAP: valid time strong, transaction time via change docs); R2 partial (corrections tracked, but correction overwrites the valid-time record); R5 partial (as-of parameters at report time); R9 cover (plain rows in a SQL spine — proof Gneiss's substrate choice is sufficient).

**Steal / Avoid.** **Steal:** gapless-interval representation for slowly-changing config (sensor→silo assignments, competition rules); the correct/rescind/supersede verb triad for the operator UI; "as-of date + as-of moment" as the minimal bitemporal query surface. **Avoid:** gaplessness as a universal invariant (Gneiss's world has genuine unknowns — gaps are information); synchronous retro-recalculation cascades — prefer marking downstream views stale.

---

## Cross-cutting patterns worth naming

1. **The Overlay** (Foundry edits, Zingg constraints, Senzing TRUSTED_ID, OMOP source_value columns): durable human/source truth stored keyed and separate, replayed over regenerated machine output. Corollary everywhere: *the overlay is only as durable as its key* — key design is the real decision-survival problem.
2. **Views are cattle, evidence is pedigree** (Senzing entities, OMOP eras, feature-store online stores, semantic-layer results, golden records): every system that works treats resolved/derived/aggregated state as disposable projections over an append-only substrate. Gneiss hypothesis (6) is unanimously confirmed.
3. **Overrides ride the data plane** (TRUSTED_ID, adjusting entries, FHIR resources): the most robust systems encode human decisions *in the same format as ordinary input*, so one pipeline, one audit trail, one replay path handles both. Gneiss's "decisions are assertions" should follow suit.
4. **As-of is the universal query verb** (feature-store PIT joins, Workday dual as-of, Foundry time travel, as-reported financials) — but each system implements only one or two axes. Gneiss's evaluation context generalizes as-of to {time, ontology version, source set, policy}; no surveyed system does the composite.
5. **The three-band triage** (auto-accept / review queue / auto-reject) is the universal human-in-the-loop economics: humans only see the ambiguous middle, and their verdicts are the most expensive data the system owns — which is why losing them on re-run (Splink's gap) is the canonical failure.
6. **Deploy-time vs query-time versioning**: semantics are versioned at deploy time everywhere (git, proposals, vocabulary releases) and at query time almost nowhere. The cheap 80% fix, available today: stamp every output with the version hash of the definitions that produced it.

---

## Sources

- Palantir Foundry: https://www.palantir.com/docs/foundry/ontologies/ontologies-proposals · https://www.palantir.com/docs/foundry/object-backend/overview · https://www.palantir.com/docs/foundry/action-types/overview · https://www.palantir.com/docs/foundry/object-edits/how-edits-applied · https://www.palantir.com/docs/foundry/learning-data-dataeng-08/25 · https://www.palantir.com/docs/foundry/announcements/2026-05
- Entity resolution: https://jeffjonas.medium.com/how-to-handle-drifting-entity-ids-in-entity-resolution-systems-a0483a8282f2 · https://senzing.zendesk.com/hc/en-us/articles/360023523354 · https://senzing.zendesk.com/hc/en-us/articles/360045732894 · https://moj-analytical-services.github.io/splink/index.html · https://moj-analytical-services.github.io/splink/api_docs/training.html · https://www.gov.uk/algorithmic-transparency-records/moj-splink-master-record · https://www.zingg.ai/post/fuzzy-matching-at-scale-part-5-incremental-flow-and-living-clusters
- Feature stores: https://docs.feast.dev/ · https://mlopsplatforms.com/posts/feature-store-comparison-2026/ · https://apxml.com/courses/feature-stores-for-ml/chapter-3-data-consistency-quality/point-in-time-correctness · https://blogs.perficient.com/2025/09/26/databricks-acquires-tecton/
- Semantic layers: https://www.getdbt.com/blog/how-the-dbt-semantic-layer-works · https://www.fivetran.com/press/fivetran-dbt-labs-complete-merger-to-create-the-data-infrastructure-for-trusted-ai-agents · https://cube.dev/ · https://cube.dev/articles/semantic-layer-for-ai-agents-2026 · https://www.malloydata.dev/ · https://docs.cloud.google.com/looker/docs/what-is-lookml
- Lineage: https://openlineage.io/docs/spec/facets/dataset-facets/column_lineage_facet/ · https://marquezproject.ai/ · https://marquezproject.ai/blog/column-lineage-demo/ · https://github.com/OpenLineage/OpenLineage
- Healthcare: https://ohdsi.github.io/CommonDataModel/cdm54.html · https://ohdsi.github.io/CommonDataModel/faq.html · https://www.hl7.org/fhir/provenance.html · https://build.fhir.org/auditevent.html
- ERP effective dating: https://userapps.support.sap.com/sap/support/knowledge/en/2511923 · https://userapps.support.sap.com/sap/support/knowledge/en/2853045

**Confidence notes.** (a) Foundry internals beyond public docs (exact edit-replay mechanics in OSv2 vs documented OSv1 behavior) are inferred from official but incomplete documentation. (b) The claim that no major semantic layer supports query-time definition versioning is verification-by-absence — medium-high confidence. (c) Workday's correction/rescind and dual as-of reporting is well-attested industry knowledge but not verified against current Workday documentation (behind login). (d) Zingg's hard-constraint preservation is Enterprise-tier per vendor material; open-source behavior differs. (e) Accounting/Part 11 section is from settled domain knowledge, not fresh web verification.
