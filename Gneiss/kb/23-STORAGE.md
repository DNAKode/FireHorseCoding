# Storage Bindings: The Spine and the Organs

*Develops seed §3, §8, §9, §18, §19.4. How the five-primitive kernel lands on real storage without becoming an EAV horror story.*

*Status per [03-NOTATION.md](03-NOTATION.md): this document is the **relational binding** of the kernel — one existence proof that the substrate contract (S1–S5) is satisfiable on ordinary infrastructure. SQL here is notation; nothing in this document is a platform decision. The conceptual content is the decision procedure (§6), the value-typing discipline (§3), the dense-data binding stance (§4), the claim-key design (§5), and the redaction protocol (§7).*

## 1. The prime directive

> The ledger holds **semantics**; per-modality stores hold **bulk**; projections hold **speed**.

The seed's anatomy metaphor stands: relational spine, specialized organs, semantic nervous system. What this document adds is the discipline that keeps the assertion table from becoming Magento's EAV catastrophe: **the assertion table is the semantic record, never the query surface.** Applications read projections (materialized belief views, typed and indexed per predicate); only the belief engine, provenance queries, and audits read the ledger directly. This is seed Risk 3/4 answered structurally rather than by optimization.

## 2. The ledger, physically

```sql
Tx        (TxId BIGINT identity PK, WallClock, ActorId, ReasonText, BatchRef)
Assertion (AssertionId PK, TxId, SubjectId, PredicateId,
           ValidFrom, ValidTo,                 -- half-open [from, to), UTC
           StatusAtBirth,                      -- fact | proposed
           SourceId, MethodId, Confidence,
           SourceRecordedAt NULL,              -- for imported history (knowledge horizon)
           ClaimKey NULL,                      -- see §5: stable key decisions can target
           ValueKind, ValueInline, ValueRef)   -- see §3
Justification (AssertionId, InputAssertionId NULL, RuleVersionId NULL, Role)
Decision  -- physically: Assertions whose PredicateId ∈ {accepts, rejects, retracts,
          -- supersedes, invalidates_source, redacts} and SubjectId/ClaimKey targets the ledger.
          -- Plus enforced TargetTxId < own TxId (invariant I6) via check on write path.
```

Append-only is enforced operationally, not just conventionally: the application role has INSERT but not UPDATE/DELETE on ledger tables; the sole exception is the redaction procedure (§7). Wrap the ledger tables in SQL Server system-versioned temporal tables (or a trigger-based equivalent) as a cheap tamper-evidence layer — the survey's observation that system versioning "audits the view of the ledger itself for free."

**Data Vault's lesson adopted:** `SourceId` + `MethodId` + load transaction on every row is 20-year-proven warehouse practice (record_source/load_date), not novel ceremony.

## 3. Values: typed, not stringly

One `Value NVARCHAR(MAX)` column is the EAV mistake. Instead a discriminated shape:

| ValueKind | Storage | Used for |
|---|---|---|
| `scalar_num`, `scalar_text`, `scalar_bool`, `scalar_ts` | inline typed columns | measurements, names, flags |
| `entity_ref` | SubjectId-style FK | links (`measures`, `sameAs`) |
| `document` | content-addressed row in `Doc(Hash, Body)` | shape models, calibrations, rule definitions, report specs |
| `series_ref` | descriptor: (series id, range) into native TS store | dense telemetry binding |
| `media_ref` | content-addressed blob pointer | video, images, point clouds |
| `expression` | document containing a derivation spec | derived predicates |

**The OMOP pattern, adopted wholesale:** wherever a value is normalized (canonical unit, mapped identity, parsed number), the assertion also carries the **verbatim source value** (`ValueRawText`). OCR strings live beside matched person refs; raw sensor payloads beside converted metres. Mapping failures are recorded with an explicit unmapped marker, never dropped. This is what makes re-mapping under a new ontology version auditable.

Documents are immutable and content-addressed: a "changed" shape model is a new document + a new assertion; diffing two versions is a view-plane service.

## 4. Dense telemetry: bound, not ingested

Dense series never enter the ledger (per-assertion envelope cost would be fatal — kernel falsification test 1). Instead:

- Samples live in native storage: a plain `(SeriesId, T, Value)` table, TimescaleDB, Parquet files, or the historian AIMS already has.
- The ledger holds **descriptor assertions**: `emits(Sensor42, SeriesS)`, `binds(SeriesS, Silo17.fillLevel, [2026-01-01, ∞))`, calibration documents, unit declarations.
- **Corrections to telemetry are semantic masks, not sample edits**: `invalidates(CalibrationC19, SeriesS, [t1,t2))` is a decision in the ledger; providers filter/adjust at read. The samples remain bit-identical; belief about them changes. (This is the same move as everywhere else in Gneiss, applied at series granularity instead of assertion granularity.)
- Bulk sample loads get one transaction + one batch assertion ("imported 86,400 samples for SeriesS covering [day)"), giving audit granularity without per-sample envelopes.

## 5. The claim-key column

The industry survey's sharpest warning: *"the overlay is only as durable as its key"* — steward decisions stored against machine-generated cluster IDs die on re-run (Splink's gap, Senzing's drifting entity IDs). Gneiss's answer is the `ClaimKey`: a deterministic hash of (predicate, subject natural key, object natural key / value identity) computed at write time for hypothesis-bearing predicates. Decisions may target an `AssertionId` (this exact token) **or** a `ClaimKey` (any assertion making this claim, past or future). Link hypotheses regenerated by a rebuilt matcher get fresh AssertionIds but the *same* ClaimKey, so prior human decisions re-attach by construction. This single column is what generalizes the Smoothscrape overlay soundly.

(Claim-key design rules: built from stable record keys, never from cluster ids or surrogate ids; method is *excluded* — a rejection of `sameAs(A,B)` rejects the claim however it was derived. Open question: whether some decisions should be method-scoped; see [40-DISCUSSION-AGENDA.md](40-DISCUSSION-AGENDA.md) D4.)

## 6. The column-vs-document-vs-assertion decision procedure (seed §19.4 answered)

Ask in order; first hit wins:

1. **Is it identity or referential integrity?** (keys, FKs, uniqueness) → SQL column in an operational table. Gneiss never touches it.
2. **Is it dense/high-volume sampled data?** → native dense store + descriptor assertions (§4).
3. **Does it evolve structurally / is it a configured artifact?** (shapes, calibrations, rule defs) → versioned document + binding assertion.
4. **Is it derived?** → derived assertion with justifications; materialized per policy ([22-BELIEF-ENGINE.md](22-BELIEF-ENGINE.md) §5).
5. **Does it need history, correction, multi-source conflict, or uncertainty?** → sparse assertions; hot read path via projection column.
6. **Otherwise** → plain SQL column, forever, with a clear conscience. (Fowler: full temporality everywhere is a tax; per-predicate opt-in.)

The seed's nine test cases:

| Property | Verdict |
|---|---|
| Store.Name | Column in projection; name-history assertions only if renames are semantically interesting (for Smoothscrape person names: yes — names are evidence). |
| Store.Description | Plain column. Nobody audits descriptions. |
| Store.Shape | Rule 3: versioned document + binding assertion. The canonical example. |
| Silo.Capacity | Rule 5: sparse assertions (commissioning changes matter, conflicts possible) + projection column. |
| Silo.FillLevel | Rule 2: dense store + descriptors. |
| Sensor.Calibration | Rule 3: document + binding; invalidations as decisions. |
| Video.Duration | Plain column (intrinsic property of an immutable artifact). |
| Person.DisplayName | Projection column *derived* from accepted identity cluster + name-source precedence — a belief, not a base fact. |
| Event.StartDate | Sparse assertions if scraped from conflicting sources (CompSeek: yes); plain column if authoritative. |

The general reading: **source-dependent or contested → assertions; intrinsic or single-source → columns.** The same property can be modeled differently in two systems, and that is correct, not inconsistent.

## 7. Redaction (the one destructive act)

Legal/privacy purges must not silently break the Gneiss Contract, so redaction is a protocol:

1. A `redacts(target)` decision is appended (actor, legal basis, scope) — the *fact of redaction* is permanent evidence.
2. Value payloads (inline values, documents, media, raw text) of targeted assertions are overwritten with a tombstone; the assertion skeleton — ids, subject, predicate, times, source, justification edges — survives.
3. Belief views render the result as missingness kind `redacted`; provenance queries still traverse the skeleton ("a value existed here, was used in these derivations, and was redacted under order X").
4. Replay determinism is preserved *structurally* but not *value-wise*; report runs that depended on redacted values are marked non-reproducible. This is honest — the alternative (pretending purged data never existed) breaks audit.

Crypto-shredding (per-subject encryption keys, destroy the key) is the standard implementation candidate where redaction demand is anticipated (GDPR-heavy person data in Smoothscrape/CompSeek).

## 8. Growth and operations

- Partition `Assertion` by predicate history-kind and/or transaction id range; the ledger is write-once, so cold partitions compress and archive well.
- Checkpointed snapshots of standard belief views (per context, per high-water tx) bound replay time after the ledger grows — the event-sourcing snapshot pattern, applied per context.
- Ledger size honesty: sparse assertions at operational cadence are small data. AIMS-scale config + decisions + hypotheses is millions of rows a year, not billions; the billions live in the dense stores where they belong. *(Amended 2026-07-04: "machine-cadence ledger rows = misclassified predicate" was too strong — machine-cadence content is a signal to apply a compression stance: standing policies for verdicts (kb/26), summarization windows for flows (kb/28), descriptors for telemetry. Sometimes it is misclassification; sometimes it is an agent-saturated or instrumented-flow envelope operating as designed. See [29-ENVELOPE.md](29-ENVELOPE.md).)*

## 9. Engine notes (.NET targets)

- **SQLite** — prototype substrate: append-only tables + views are enough; no temporal features needed because Gneiss brings its own.
- **PostgreSQL** — best OSS target: range types for valid time (adopt PG18 `WITHOUT OVERLAPS` where applicable to projections), pg_ivm as optional projection accelerator, Marten as candidate substrate (prototype question).
- **SQL Server** — enterprise target: system-versioned temporal tables as ledger tamper-evidence and projection audit; emulate ranges with paired columns + filtered indexes; EF Core mapping exists for temporal reads.
