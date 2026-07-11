# KodePorter — M1.5 Contract: The Imperfection Vocabulary (+ Atlas v2, governance tool)

Extends [CONTRACT.md](CONTRACT.md) per ROADMAP §10.5 and charter §14 (two layers, one product).
Theme: **the map describes its own epistemic state, per layer, as small closed sets — rendered
first-class in the Atlas.** All new fields are optional-with-defaults so existing workspaces,
tests, and the Slice Zero story keep working unchanged. Divergences: tag `// DIVERGENCE:` + report.

## 1. Schema additions (the vocabulary)

### 1.1 Cartography — resolution grade + test-ness (map store)
`entity` gains `resolution TEXT NOT NULL DEFAULT 'clean'` (`clean|degraded|gap`) and
`is_test INTEGER NOT NULL DEFAULT 0`.
- CSharpRoslynProvider: entities whose file produced ≥1 Error-severity diagnostic → `degraded`.
  `is_test = 1` when the file path contains a `tests/` or `test/` segment or the containing type
  name ends with `Tests`.
- RustSynProvider: dump entities may carry optional `"resolution"` and `"isTest"` fields
  (see §5 dump format v1.1); absent → clean/0. Rust test-ness: symbolPath contains `::tests::`
  or file under `tests/`.

### 1.2 Identity — continuity candidates (map store)
New table `continuity_candidate(basis_from TEXT, basis_to TEXT, from_id TEXT, to_id TEXT,
heuristic TEXT, status TEXT NOT NULL DEFAULT 'candidate')`. Populated during `Advance`: for each
(removed, added) pair within one side where kind matches and `name` matches exactly (heuristic
`name-kind`) — nothing cleverer (K-D3 discipline). Never auto-confirmed; surfaced in the Atlas.

### 1.3 Correspondence — provenance grade (domain yaml)
`Correspondence` gains `provenance: candidate|asserted|verified` (default `asserted` for existing
rows). `candidate` = machine-inferred, unreviewed (never counted as *corresponded* in health —
counted separately); `verified` is DERIVED for display (an accepted kp.verification claim covers
the correspondence's unit+criterion), never stored.

### 1.4 Understanding — unit depth (domain yaml)
`UnitDoc` gains `depth: thin|dossiered` (default `thin`). Set explicitly via
`kp unit set-depth --id <u> --depth dossiered` — typed judgment, not inferred from prose length.

### 1.5 Absence — typed, per source entity (domain yaml + computed)
New file `.kodeporter/absences.yaml`: list of `{symbolPath, kind: not-yet-ported |
deliberately-dropped | unknown, note}`. The COMPUTED default for any eligible source entity
(non-test fn/method/struct/enum/class, not covered by any unit anchor or correspondence) is
`unknown`. `kp absence set --symbol <sp> --kind <k> [--note <s>]` records an override. Target-only
entities get the mirror classification (`intentional | unexplained`, default `unexplained`) in the
same file with `side: target`.

### 1.6 Evidence — independence (verification)
`VerificationClaimValue` + run report gain `independence: independently-derived |
implementation-coupled | unknown` (default `unknown`), supplied via
`kp verify run --independence <v>` (caller-attested for now). `PolicyEngine.AllowsAutoAccept` may
require a minimum (policy.yaml optional `requiredIndependence: {kpVerification:
independently-derived}`); absent → no constraint (existing behavior).

### 1.7 Health v2
`HealthReport` becomes: mapped, corresponded (asserted/verified correspondences only),
candidates (count of candidate correspondences), implemented, verified, stale,
`absence: {unknown, notYetPorted, deliberatelyDropped}` (source side, computed per §1.5,
**excluding is_test entities**), targetOnly {unexplained, intentional}. `kp status` prints all;
Atlas shows all. Keep `HealthReport` shape changes contained (update call sites + tests +
hand-computed fixture expectations honestly — never bend the fixture to the code).

## 2. Candidate inference (bootstrap tooling, product-side)

`kp candidates infer --workspace <dir> [--heuristic name-norm]` — v1 heuristic `name-norm`:
normalize source symbolPaths (`crate::mod::Type` → last two segments, snake→Pascal) and target
(`Namespace.Type` → last two segments); exact normalized-pair match on kind-compatible entities
(struct/enum/class↔class/struct/enum/record; fn/method↔method) → creates correspondences
`type: maps-to, provenance: candidate, note: "inferred:name-norm"`, id `cand-<n>`. Skips pairs
already covered by any correspondence. Deterministic ordering; prints a summary (created,
skipped, ambiguous — a source symbol matching >3 targets is recorded ONCE as ambiguous and NOT
linked). Unit tests with a small synthetic map.

## 3. `kp note` (two-tier capture, K-A8)

`kp note --workspace <dir> --text <s> [--actor <a>]` → `GneissBinding.Note(...)` → the Gneiss
note inbox. `kp notes --workspace <dir>` lists (id, wall, actor, text, promoted?). Promotion
stays manual for now.

## 4. Binding uses facade v0.1

After Gneiss lands `AppendResult`/`GetAssertion` (CONTRACT-V01.md): remove the ledger-export
aid-recovery scans and their `// DIVERGENCE:` comments from `GneissBinding`; use returned aids
and `GetAssertion`. Behavior identical; tests stay green.

## 5. Rust dump format v1.1 (tools/rust-map-dump)

- **Multi-crate roots:** the tool accepts a directory that is a single crate OR a workspace/tree
  of crates: discover every `Cargo.toml` under the root (excluding any `target/` dir), parse each
  crate's `src/**/*.rs` (and `tests/**/*.rs` marked isTest), prefix symbolPaths with the crate
  name. Output remains ONE deterministic entities array.
- Entity gains optional `"isTest": bool` and `"resolution": "clean"|"gap"` (a file syn fails to
  parse contributes a single file-level entity with resolution `gap` instead of aborting).
- `"provider": "rust-map-dump@0.2.0"`. Regenerate the committed slice-zero dumps IF byte-changes
  result (they should not, single-crate case unchanged — verify; if changed, that is a finding to
  report, not silently commit).

## 6. Atlas v2 (the visualization emphasis — this is a flagship deliverable)

Same self-containment/determinism/escaping rules as CONTRACT §8. New requirements:

1. **Scale**: must open smoothly from `file://` at ≥40k entities. Techniques required: the data
   island stores the entity tree; the DOM renders lazily (children created on expand via
   vanilla JS); long sibling lists render in pages (e.g. 200 + "show more"); no `<details>`-per-
   entity pre-rendering at scale. Budget: initial render < 3s, expand interaction < 100ms at 40k.
2. **Overview panel (new first tab)**: two treemaps (source | target), rectangles = top-level
   crates/namespaces (second level on click-zoom, one level deep is enough), area = non-test
   entity count, fill = coverage class (corresponded / candidate-only / uncovered), amber border
   = contains stale. Pure SVG generated at build time (squarified or slice-dice layout — keep it
   simple and deterministic), no JS libs. Legend + counts. This is the "see the whole port at a
   glance" view.
3. **Imperfection rendering**: resolution `degraded/gap` (hatched/dim node badge), `is_test`
   (dimmed + global toggle "show tests"), correspondence provenance (candidate = dashed/gray
   badge; asserted = solid; verified = green tick), unit depth badge (thin/dossiered), absence
   kinds in health strip with drill-down lists, continuity candidates listed in a small
   "identity" section, evidence independence shown on runs/claims.
4. **Filters**: text search over symbolPath (throttled, from the data island), kind filter,
   "hide tests" default ON, "candidates only" toggle in correspondence tab.
5. Health strip renders Health v2 (all dimensions incl. absence breakdown).
6. Existing tests stay green (update fixture expectations for v2 shape); add: a synthetic
   3k-entity fixture generation test asserting the HTML stays < 15MB and contains lazy-render
   markers, plus the no-malformed-class and self-containment assertions.

## 7. Governance ledger tool (`tools/GovernanceLedger`, new console project in the solution)

A small Gneiss.Cell consumer (references Gneiss.Cell ONLY — no KodePorter): the FireHorseCoding
governance ledger (meta-meta tier).

Verbs: `seed --dir governance` (create ledger.db from scratch and record the seed history — see
below — then export), `record --dir --actor --reason --subject --predicate --value [--decide
accept --target <subject>]` (append one governed decision), `export --dir` (canonical
ledger-export.jsonl + regenerate LENS.html), `rebuild --dir` (recreate ledger.db from
ledger-export.jsonl — the committed jsonl is the durable artifact; *.db stays gitignored).

**Seed content** (predicate `gov.decision`, subject `decision:<id>`, actor `govert` for steward
decisions / `fable` for delegate proposals, each with reason + evidence note citing the commit
hash; dates as valid-from): charters-established (af45f45) · review+roadmaps (57b3716) ·
amendments-G-A/K-A-adopted + M0-decision-lock (3d7eadd) · M1-increment-landed (5c3e96d) ·
behavior-subject-correction (5c3e96d) · post-M1-positioning-map-is-product (648d29c) ·
service-realization-seven-decisions (b5890cf) · morning-positioning-SUPERSEDED-by-service-
realization (record as a Gneiss `supersedes` decision targeting the 648d29c decision's
assertion — the machinery demonstrating correction-without-erasure on our own governance) ·
grounded-semantics-ratified (3fcfd0b).

**LENS.html** (Lens-mini — Gneiss's first visual): self-contained static HTML over the ledger
export: transaction timeline (one row per tx: wall, actor, reason), decision cards (subject,
value, status from a decided-only Ask, superseded items visibly struck-through-but-present with
their superseding decision linked), and an expandable why/decision trail per card. Same
self-containment/escaping/determinism rules as the Atlas. This file is the governance section's
marquee visual.

Tests (in KodePorter.Core.Tests is WRONG — this is Gneiss-side tooling; put
`tools/GovernanceLedger.Tests` xunit project in the solution): seed→export→rebuild round-trip
(rebuilt db re-exports byte-identically), supersession renders struck-through in LENS, LENS
self-containment.

## 8. Slice Zero story compatibility

`showcase/m1/run-m1.ps1` must still run green with zero edits (all new fields default). Do not
extend it in this increment; the FrankenTui probe is the new story.
