# The Attic Register

Parked, not deleted (review §4 gloss: nothing is set in stone). Each entry: what, why parked,
promotion trigger. Reviewed at every gate.

| Item | Parked because | Promotion trigger |
|---|---|---|
| `sprout`/`commit` (what-if worlds) | G-A1: most speculative mechanism; slice needs proposals+decisions only | a domain demonstrates transactional what-if need; Phase 4 |
| `import`/federation verbs, watermarks | G-A1: off-spine `[F]` | ~~second ledger exists; A3-shaped need~~ **trigger now visible (2026-07-11): the three-tier topology — projects importing pinned knowledge from the KodePorter meta-ledger — is the A3-shaped need; promote when the meta-ledger bootstraps** |
| Seals, purge, amnesia drill (E4) | this increment ends at the Atlas; schema stubs present | M2 / sub-gate S3 — **and now a product path (2026-07-11): a one-shot port seals its map; the seal is the port's receipt** |
| Grades beyond constant `grounded`; coverage map; `absent_closed`/closure declarations | depend on E4 | M2 |
| Gneiss Lens (E2b) | steward scoped this increment to the KodePorter Atlas | next Gneiss increment (M1 completion) |
| `gn` CLI | facade suffices for KodePorter in-proc; conformance kit is the real consumer | E5 conformance kit or first non-KodePorter consumer |
| SCIP/LSIF Rust importer | protobuf/graph plumbing cost at fixture envelope | second language pair, or Rust-side reference edges needed |
| `kp adopt` (KP-0 ingestion) | Atlas does not need it; `kp export` ships first | first external/brownfield port (FrankenTui bootstrap) |
| Supersession interval clipping (strainer rung 3 full form) | wrong-silo fixture uses retraction; v0 clips only strict containment | first fixture needing partial-overlap clipping |
| KodePorter nouns demoted by K-A1 (SemanticUnit, Capability, BehavioralContract-as-object, TransformationRule, CompatibilityBridge, Risk, AgentRun, Milestone/AcceptanceGate; Adaptation/Exception/KnownDeviation → `Divergence.kind`) | vocabulary must earn its teaching cost through use | slice + brownfield evidence; TS→Go mining report (R1) |
| Cross-language reference edges; call/dataflow/ownership overlays | v0 maps containment + declarations (+ C# refs best-effort) | a fixture or benchmark question that needs them |
| Delegation lattice (kb/26 full form) | one steward, one agent fleet; dial suffices | second decider type in real use |

## Added 2026-07-10, from M1 integration and the adversarial verification round

| Item | Parked because | Promotion trigger |
|---|---|---|
| Gneiss facade: `Append` returning per-item aids | binding recovers aids via ledger-export scan (documented DIVERGENCE) | next Gneiss increment (facade v0.1) |
| Gneiss facade: deterministic (content-derived) `Label.ReceiptId` | random GUID per Ask; Atlas substitutes `ResultHash` | facade v0.1 |
| Gneiss facade: fetch assertion by aid | two callers parse the export instead | facade v0.1 |
| Criterion-based staleness for anchor-less `covers` correspondences | v0 staleness is anchor drift only; corr-covers is structurally exempt (its verification CLAIM does get stale-marked via the unit's criteria, so the signal exists — only the yaml corr entry lacks the flag) | first fixture where the claim-level signal is insufficient |
| Why-tree status vocabulary alignment ("proposed" vs "proposed-unadmitted") | two code paths, two label sets; not a truth error | Atlas polish pass |
| Harness distinctness attestation (source-cmd vs target-cmd provably different implementations) | inherent limit of black-box stdout diffing; noted in CONTRACT §6 | provenance binding of commands to map entities |

## Added 2026-07-12, from the FrankenTui probe and M1.5 close

| Item | Parked because | Promotion trigger |
|---|---|---|
| Per-project Roslyn compilation (project-reference graph instead of one flat BCL compilation) | probe finding: 96.2% of the C# map marked `degraded` (28,230 errors) — the signal saturates and stops being addressable | FrankenTui bootstrap (K8): required before resolution grades mean anything at brownfield scale |
| Rust dump identity namespacing for per-file test crates | probe finding: 69 (kind,symbolPath) collisions silently dropped (0.71%) — K-D3 stress made concrete | bootstrap; fix = qualify per-file test-crate symbolPaths with their owning package |
| Import collision surfacing (dropped-duplicate count in ImportResult + Atlas) | collisions currently invisible in health | with the namespacing fix above |
| Provenance-pin freshness check (`kp` verb or probe step comparing recorded pin vs actual vendored checkout) | probe finding: FrankenTui.Net's PROVENANCE.md pin is ~2 months stale vs .external/frankentui | bootstrap |
| Richer candidate heuristics (path-structure, doc-comment citations — FrankenTui.NET files cite their Rust source in headers!) + agent-proposed candidates | name-norm yielded 5 candidates / 6,780 skips with a structural false-positive mode (generic test-fixture names) | bootstrap (K8) — the header-comment citation heuristic looks highest-yield |
| Transient SQLite-open retry in GneissLedger.Create/Open | parallel full-solution test runs flake ~1/run with SQLite Error 14 (environmental, Windows, cross-testhost contention); serial runs stable 143/143 twice | next Gneiss increment; until then run `dotnet test -m:1` for full-solution verification |
