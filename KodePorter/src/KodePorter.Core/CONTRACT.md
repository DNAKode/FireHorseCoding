# KodePorter.Core + Cli — Contract (v0, this increment)

Implements K2 (cartographer), K3-partial (units/dossiers/correspondences/policy/claims), K4-lite
(differential verify), K5-lite (decide with both autonomy postures), K6-lite (advance/stale), and
K-V (the Port Atlas) from [ROADMAP.md](../../ROADMAP.md), scoped per
[AMENDMENTS.md](../../../AMENDMENTS.md). Gneiss is consumed ONLY through `Gneiss.Cell`'s public
facade — no KodePorter type may leak into Gneiss and no Gneiss internals may be touched (sidecar
boundary, charter §4.2). Deviations are findings: tag `// DIVERGENCE:` and surface.

## 1. The port workspace

A directory (default `<fixture>/workspace`, created by `kp init`) holding:

```
workspace/
  kp.json           project: name, direction, sourceRoot, targetRoot (as given), policy ref
  kpmap.db          map store (SQLite, REGENERABLE — the regeneration drill proves it)
  gneiss.db         the Gneiss ledger (durable judgments)
  .kodeporter/      exported diffable domain artifacts (K-A6): units/*.md, correspondences.yaml,
                    policy.yaml, project.yaml  — human-readable without any runtime
  checkouts/        delta checkouts created by apply-delta
  runs/             verification run reports (JSON) + lab notebooks (md)
  atlas/            generated atlas HTML snapshots (dated)
```

## 2. Map store (kpmap.db)

```sql
CREATE TABLE basis (id TEXT PRIMARY KEY,             -- sha256(side|label|tree_hash)
                    side TEXT CHECK(side IN ('source','target')), label TEXT NOT NULL,
                    root TEXT NOT NULL, tree_hash TEXT NOT NULL,
                    toolchain TEXT, analyzer TEXT, created TEXT NOT NULL);
CREATE TABLE entity(id TEXT NOT NULL,                -- sha256(side|kind|symbol_path)  ← stable across bases (K-D3)
                    basis_id TEXT NOT NULL REFERENCES basis(id),
                    kind TEXT NOT NULL, name TEXT NOT NULL, symbol_path TEXT NOT NULL,
                    file TEXT NOT NULL, start_line INT NOT NULL, end_line INT NOT NULL,
                    content_hash TEXT NOT NULL, parent_id TEXT,
                    PRIMARY KEY (id, basis_id));
```

- `tree_hash` = SHA-256 over the sorted list of `(relative-path, sha256(file bytes))` for included
  files (source: `**/*.rs` under `src/` + `Cargo.toml`; target: `**/*.cs` excluding `bin/ obj/`).
- Import is DETERMINISTIC: same root content → byte-identical table dump. Drill: import twice into
  two fresh dbs → `SELECT * ORDER BY id, basis_id` dumps identical.
- Entity identity: `id` from (side, kind, symbolPath) survives bases; `content_hash` drift across
  bases is the change signal. Renames = disappear+appear in v0 (continuity assertions are attic'd —
  do NOT try to be clever here).

## 3. Providers

Shared dump format: fixtures/slice-zero/CONTRACT.md §6.

- **RustSynProvider**: does NOT run cargo itself. Takes the path of a dump JSON produced by
  `tools/rust-map-dump` (the runner script generates it) and imports. Validates `provider` field.
- **CSharpRoslynProvider**: in-proc. Glob `**/*.cs` (exclude bin/obj), parse
  (`CSharpParseOptions.Default` latest), one `CSharpCompilation` with references from
  `AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")`. Walk declared symbols from syntax roots:
  namespace, class, record, struct, enum, enummember, method (symbolPath includes parameter type
  list, e.g. `HeadScan.HeaderParser.Parse(string)`), property, field. Span = full declaration
  syntax span; `content_hash` = SHA-256 of span text with `\r\n`→`\n`. `symbolPath` = fully
  qualified display string without `global::`. Compilation diagnostics of severity Error do NOT
  abort import (map-first principle) but are counted and reported.

## 4. Domain state (files are the source of truth; db caches nothing domain-owned)

- `project.yaml`: name, direction (`rust->csharp`), roots, policyVersion.
- `units/<id>.md`: YAML front matter (`id`, `name`, `status: mapped|in-progress|accepted`,
  `sourceAnchors: [{symbolPath, basisLabel, contentHash}]`, `targetAnchors: [...]`,
  `claims: [aid...]`) + markdown body with sections `## Purpose`, `## Contract`, `## Questions`,
  `## Evidence`.
- `correspondences.yaml`: list of `{id, type: implements|maps-to|adapts|diverges|covers,
  divergenceKind: adaptation|exception|intended|observed|unresolved (only when type=diverges or adapts),
  unit, source: {symbolPath, basisLabel, contentHash} | null, target: {...} | null,
  criterion: io-agreement-v1|api-shape-v1|error-semantics-v1|null, note, claimAid}`.
- `policy.yaml`: `{name, version, autoAccept: {kpVerification: true, kpBehavior: false},
  requiredEvidence: {kpVerification: [verification-run]}}` — the autonomy dial (K-A10): claim
  classes with `autoAccept: true` are accepted by a policy-actor decision the moment their
  mechanical evidence is green (zero human minutes); others wait for `kp decide`.
- Parsing: YAML subset — implement a minimal reader for the exact shapes above (no external YAML
  package; front matter and these files use a constrained `key: value` / list-of-maps format that
  the same code reads and writes round-trip). Keep it boring and exact.

## 5. Gneiss binding (claims — kb/37 §4.3 promotion rule)

Predicates (declared once at `kp init` with comparator exact, stopRung 6):
- `kp.behavior` — subject `unit:<unitId>`; value = Text(claim sentence). Evidence: justification
  refs to evidence assertions.
- `kp.evidence.anchor` — subject `anchor:<sha of symbolPath|basisLabel>`; value = Json({symbolPath,
  basisLabel, contentHash, file, lines}). Fact status (mechanical observation).
- `kp.correspondence` — subject `corr:<id>`; value = Json({type, source, target, unit, criterion}).
- `kp.verification` — subject `verify:<unitId>:<criterion>`; value = Json({verdict, corpusHash,
  sourceBasis, targetBasis, cases, mismatches, reportPath}).
- Proposed vs fact: behavior + correspondence + verification claims enter `Proposed = true`
  (generation is proposal, charter §6.7); anchors enter as facts. Acceptance = Gneiss decision:
  by the policy actor (`actor = "policy:<name>@<version>"`) when autoAccept applies and evidence
  is green (K-A3: the evidence MUST be mechanically re-derivable — for kp.verification the run
  report + rerun command), else by the human via `kp decide` (`actor = "govert"`, reason required).
- The current view context: `kp-current` declared at init (admit decided-only). All status shown in
  the Atlas comes from `Ask("kp-current", ...)` — never from the yaml alone. Receipts retained.

## 6. Verification (K4-lite: io-agreement-v1)

`kp verify run --unit <id> --cases <cases.jsonl> --source-cmd "<cmd>" --target-cmd "<cmd>"`:
pipe the cases file to each command's stdin (working dir = workspace parent), capture stdout JSONL,
compare per case: byte equality of the `result` JSON → pass/fail per case. Write
`runs/verify-<unit>-<timestamp>.json` report {criterion, corpusHash: sha256(cases file),
sourceCmd, targetCmd, sourceBasis, targetBasis, results: [{name, match, sourceResult, targetResult
(only when mismatch)}], verdict: pass|fail}, plus a small markdown lab notebook
`runs/verify-<unit>-<timestamp>.md` (inputs, verdict, counts, THE EXACT RERUN COMMAND).
Then promote/refresh the `kp.verification` claim (proposed) + policy auto-accept if green & policy
allows. A failed run records verdict fail — it is evidence too, and it must NOT be auto-accepted
as a pass (obviously) but IS recorded as a claim with verdict fail, contested-visible in the Atlas.

## 7. Advance & staleness (K6-lite)

`kp advance --side source --root <newCheckout> --label d2`: pin + import the new basis; then:
1. Diff entities vs previous basis of that side by `id`: added / removed / changed(content_hash).
2. Correspondences and unit anchors citing a changed/removed source anchor (`symbolPath` whose
   current `content_hash` ≠ recorded, or gone) → mark stale: recorded IN GNEISS as a fact
   assertion `kp.stale` subject `corr:<id>` / `unit:<id>` value Json({basisLabel, cause,
   changedSymbols}); the Atlas reads staleness from the view. The yaml gets `stale: true` too
   (files stay readable).
3. Claims whose evidence anchors drifted: same mechanism on `kp.behavior`/`kp.verification`
   subjects (their anchor justifications carry contentHash).
4. Emit a delta report `runs/advance-<label>.md`: added/removed/changed counts, cone listing
   (units, correspondences, claims affected). Deterministic ordering.
Staleness v0 is ANCHOR DRIFT, honestly labeled as such everywhere it is shown.

## 8. The Port Atlas (K-V) — `kp atlas --out <path>`

ONE self-contained HTML file. No external requests of any kind (CSP-safe: no CDN, no fonts, no
images; inline CSS + vanilla JS; data embedded as `<script type="application/json">`). Works from
`file://`. Light/dark via `prefers-color-scheme`.

Content (all data read via the map store + `Ask("kp-current", ...)` + domain files):
- **Header**: project name, direction, bases per side (label + short tree hash + entity count),
  generated timestamp, `KodePorter v0`.
- **Health strip** (K-D12, six dimensions, each a count with a drill-down anchor): mapped (entities
  current basis), corresponded (entities covered by any correspondence), implemented (units with
  targetAnchors), verified (units with accepted kp.verification pass), stale (stale marks in force),
  unknown (source entities of kinds fn/method/struct/enum with no unit/correspondence).
- **Two trees**: source | target containment (parent_id), collapsible, node rows show kind badge +
  name; nodes referenced by a selected correspondence highlight in both trees; stale nodes get an
  amber left border.
- **Center tabs**:
  - *Correspondences*: table (type badge, source → target symbolPaths, unit, criterion, status
    from the belief view: accepted/proposed/defeated + stale flag). Click → highlight trees.
  - *Units*: dossier front matter + body rendered with a minimal md renderer (headings, paragraphs,
    lists, code spans only).
  - *Claims*: table (predicate, subject, value/verdict, status accepted|proposed|contested|defeated,
    AutoAdmitted badge when applicable, stale, **label popover**: context name+hash, dataCut,
    consumed count, receipt id; **why** expandable tree from `Why()` serialized into the data
    island). Decisions show WHICH ACTOR decided (`policy:...` vs human) — the autonomy dial made
    visible.
  - *Runs*: verification runs with verdict, per-case counts, mismatch names, rerun command.
- **Footer**: golden-ledger note — path of the ledger export used, its SHA-256.
- Everything sorted deterministically; two `kp atlas` runs over the same workspace differ ONLY in
  the generated-timestamp field (test asserts this by masking the timestamp).

Design bar: clean, quiet, legible (system font stack, generous whitespace, one accent color,
amber = stale, green = accepted/pass, gray = proposed, red = defeated/fail). This file IS the
marquee demo; it will be looked at hard.

## 9. CLI (`kp`, KodePorter.Cli — manual arg parsing, no packages)

```
kp init    --workspace <dir> --name <s> --source-root <p> --target-root <p>
kp pin     --workspace <dir> --side source|target --root <p> --label <s> [--analyzer <s>]
kp map     --workspace <dir> --side source|target --label <s> [--dump <rust-dump.json>]
kp unit    new --workspace <dir> --id <s> --name <s> --source-anchors <sp,sp> [--target-anchors ...]
kp corr    add --workspace <dir> --type <t> --unit <id> [--source <sp>] [--target <sp>]
           [--criterion <c>] [--divergence-kind <k>] [--note <s>]
kp claim   add --workspace <dir> --unit <id> --predicate kp.behavior --value <s> --anchors <sp,sp>
kp decide  --workspace <dir> --subject <claim subject> --verdict accept|reject --reason <s>
kp verify  run --workspace <dir> --unit <id> --cases <p> --source-cmd <s> --target-cmd <s>
kp advance --workspace <dir> --side source --root <p> --label <s> [--dump <json>]
kp status  --workspace <dir>            (text health, same six numbers as the Atlas)
kp export  --workspace <dir> --out <p>  (emit PORTING.md — the KP-0 floor: project, units, policy,
                                         correspondences, claims with status, honest and current)
kp atlas   --workspace <dir> --out <p>
```
Exit codes: 0 ok, 1 usage, 2 domain error (message to stderr, one line, actionable).

## 10. Required tests (KodePorter.Core.Tests)

1. **Regeneration drill**: import a small inline C# source tree twice → identical ordered dumps.
2. **Roslyn provider**: fixed source string → exact expected entities (kinds, symbolPaths, spans
   stable, contentHash changes only when span text changes).
3. **Rust dump import**: a committed sample dump JSON → expected entities; provider field validated.
4. **Diff/stale**: two bases differing in one function body → exactly that entity changed; a
   correspondence anchored to it goes stale; an unrelated one does not (d1 does not cry wolf).
5. **Promotion + autonomy dial**: behavior claim proposed → invisible in decided-only view;
   `kp decide` accept → visible with human actor; verification claim green + policy autoAccept →
   visible with policy actor, zero human decisions recorded for it.
6. **Verify harness**: two tiny stub commands (echo scripts or dotnet inline) — matching outputs →
   pass; injected mismatch → fail with named case.
7. **Atlas determinism + self-containment**: generate twice → identical modulo timestamp; no
   `http://`/`https://` substrings; embedded JSON parses; health numbers match a hand-computed
   fixture expectation.
8. **Export floor**: `kp export` emits PORTING.md containing every unit, correspondence and claim
   status present in the view.
