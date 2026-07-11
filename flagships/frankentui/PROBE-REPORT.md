# FrankenTui Read-Only Probe — Report

Roadmap [10.5 M2-prime](../../KodePorter/ROADMAP.md), probe stage (K2b, extended per steward
instruction with a light-touch `kp candidates infer` pass — no Gneiss ledger writes occur:
`candidates infer` only touches `.kodeporter/correspondences.yaml`, never `GneissBinding`).
Produced by [run-probe.ps1](run-probe.ps1); raw command output in
`workspace/probe-log.txt` (gitignored). Strictly read-only against `C:\Work\FrankenTui.Net`: only
`git log/status/rev-parse` and in-proc syntax parsing (globbing + `File.ReadAllText`, no build, no
restore, no write) ever touched that tree. Run date: 2026-07-11.

## 1. Provenance

| | |
|---|---|
| FrankenTui.Net HEAD | `afd20f0827abde692bb2d5163e528965728869ef` (2026-06-09, "checkpoint: [build claim retracted below] solution builds clean (Runtime model = single-type canonical)") |
| FrankenTui.Net working tree | **dirty — 304 porcelain entries** (263 modified `M`, 40 untracked `??`, 1 deleted `D`) |
| `.external/frankentui` | **not a submodule** — no `.gitmodules` in FrankenTui.Net, and `.external/` is listed in FrankenTui.Net's own `.gitignore` (0 files tracked under it by the parent repo). It is a **plain vendored tree that happens to carry its own live `.git` directory** — a full clone, not a gitlink. |
| `.external/frankentui` HEAD | `33ad1c57d545292242e41a477c8278c70ed7e0d6` (2026-05-19, "deps(telemetry): bump opentelemetry ecosystem 0.31→0.32, …") |
| `.external/frankentui` working tree | dirty — 1 porcelain entry (near-clean) |
| `.external/frankentui` remotes | `origin` → `Dicklesworthstone/frankentui` (true upstream), `fork` → `govert/frankentui` |
| Recorded pin vs. actual checkout | `PROVENANCE.md`'s "Bootstrap reference commit" is `7a91089366bd4644e086d5a422cb76b052e3de17` dated 2026-03-09. The tree actually checked out is `33ad1c5…`, dated 2026-05-19 — **the vendored tree has moved ~2 months past the recorded pin and `PROVENANCE.md` was never updated.** Open finding, §7. |

Analyzer notes for the pinned basis (K-D2): source analyzer `rust-syn@2` via `rust-map-dump@0.2.0`
(syn v2 AST walk, no type resolution); target analyzer `roslyn@5.6`
(`Microsoft.CodeAnalysis.CSharp` 5.6.0, syntax-only — see §5 on why "diagnostics" here means
"Roslyn couldn't resolve a name inside one flat compilation," not "the code is wrong" by itself,
though independent evidence below shows it also happens to be wrong).

## 2. Scale table

| | Source (Rust, `.external/frankentui`) | Target (C#, `FrankenTui.Net`) |
|---|---|---|
| Repo shape | 20 Cargo workspace member crates (`crates/*`), `fuzz/` excluded | 24 `.csproj` in the `.sln` (+4 solution folders); 3 more scratch `.csproj` outside the `.sln` (`.tmp/`, `sandbox/`) not mapped (target-root is the whole tree, so their `.cs` files ARE included in the Roslyn import even though they're not in the `.sln`) |
| Files walked | 273 distinct `.rs` files touched by the dump (of 846 total `.rs` under the tree — the tool only compiles recognized crate-root files: `lib.rs`/`main.rs`/`src/bin/*.rs`/direct-child `tests/*.rs`; deeper `mod`-included files and `tests/<subdir>/` helpers are its documented non-goal, see §7) | 566 tracked `.cs` files (731 `*.cs` on disk including `bin/`+`obj/` copies, which the importer's own bin/obj-segment filter correctly excludes) |
| Dump / raw entities | 9,725 emitted by `rust-map-dump` | — |
| Imported entities | **9,656** (69 dropped — see §7 identity-collision finding) | **16,535** |
| Kind mix (top) | fn 5,622 · field 1,398 · method 791 · module 710 · struct 313 · variant 281 · impl 275 · const 207 · enum 59 | method 9,010 · field 2,323 · property 2,039 · class 1,133 · enummember 1,105 · record 633 · enum 230 · struct 34 · namespace 28 |
| Test share (`is_test`) | **8,455 / 9,656 = 87.5%** | 4,267 / 16,535 = 25.8% |
| Resolution grade | 9,656 / 9,656 clean (0 gap; syn parsed every recognized root file) | **629 / 16,535 clean (3.8%) — 15,906 / 16,535 degraded (96.2%)** |
| Diagnostics (Error severity) | n/a (no cargo build run — the probe never invokes cargo check) | **28,230** Roslyn Error-severity diagnostics across one flat `TRUSTED_PLATFORM_ASSEMBLIES`-only compilation |
| Top-level treemap groups (`TopLevelKey`, `::`/`.`-split) | **268** | ~21 real namespaces (+ a handful of singleton outliers — nested/local types without a `FrankenTui.*` prefix) |

## 3. Timings

| Step | Wall time |
|---|---|
| `dotnet build FireHorseCoding.slnx` | 4.6 s |
| `cargo build --release` (rust-map-dump, inside `fixtures/slice-zero/tools/rust-map-dump`) | 2.4 s |
| `cargo run --release` (the dump itself, 9,725 entities over 20 crates) | 7.6 s |
| `kp map` source (import pre-generated dump) | 0.58 s |
| `kp map` target (Roslyn, whole `FrankenTui.Net` tree, 16,535 entities) | 20.8 s |
| `kp candidates infer` | 0.65 s |
| `kp status` | 0.76 s |
| `kp atlas` | 2.46 s |
| **Total probe wall time (build through atlas)** | **≈ 40 s** |

No step approached the 15-minute pathological-runtime threshold; no OOM observed. The 8.7 MB Atlas
is well under the 15 MB CONTRACT-M15 §6.1 budget. Step 9's "STOP that step" clause was never
triggered — every step in §1 above of run-probe.ps1 completed cleanly. The findings below are about
**data quality and representational fit**, not runtime scale failure.

## 4. Candidate inference — stats and the 10-sample plausibility table

`kp candidates infer --heuristic name-norm`: **created 5, skipped 6,780, ambiguous 0.**

Only 5 candidates exist, so the "sample of 10" is all 5 — noted honestly rather than padded. Read
the actual Rust and C# regions for each (paths/lines below are basis-pinned coordinates from the
map store, not editorializing):

| id | source → target | verdict | why |
|---|---|---|---|
| cand-1 | `baseline_capture::percentile` (`crates/ftui-demo-showcase/tests/baseline_capture.rs:29`) → `FrankenTui.Testing.Harness.BaselineCapture.Percentile(...)` (`src/FrankenTui.Testing.Harness/BaselineCapture.cs:185`) | **plausible, high confidence** | Both compute a percentile from a sorted array using the identical `ceil(len*p) → clamp(len-1)` index formula. Same algorithm, same purpose, source in a test-perf-harness file mapping to a production harness class — a real match. |
| cand-2 | `deterministic_replay::CounterModel::update` (`crates/ftui-runtime/tests/deterministic_replay.rs:91`) → `FrankenTui.Tests.Headless.CounterModel.Update(...)` (`tests/FrankenTui.Tests.Headless/StringModelTests.cs:26`) | **false positive** | Coincidental name collision. The Rust `CounterModel` here handles `Msg::Key/Resize/Click/Tick/Other`; the C# `CounterModel` in `StringModelTests.cs` handles `TestMsg.Increment/Decrement/Quit/NoOp` — different message shapes, different test. The C# file's own header comment says it ports `.external/frankentui/crates/ftui-runtime/src/string_model.rs` (a **different** Rust file entirely), confirming this pairing is wrong. "CounterModel" is a generic test-fixture name reused independently on both sides. |
| cand-3 | `e2e_observability_pipeline::UnifiedEvidenceLedger::domain_count` (`crates/ftui-runtime/tests/e2e_observability_pipeline.rs:281`) → `FrankenTui.Runtime.UnifiedEvidenceLedger.DomainCount(...)` (`src/FrankenTui.Runtime/UnifiedEvidence.cs:204`) | **plausible, high confidence** | Same class name, same method names (`record`/`domain_count` ↔ `Record`/`DomainCount`), same ring-buffer-of-evidence-entries semantics (Rust: `Vec::remove(0)`-then-push at capacity; C#: head-pointer circular buffer — same externally observable FIFO-eviction behavior). Worth flagging separately: the Rust original lives in a **test** file while the C# counterpart is **production** code (`src/`) — a test-helper apparently got promoted to a real production type during porting. Correct match, but crosses a test/prod boundary that a human should confirm was intentional. |
| cand-4 | `e2e_observability_pipeline::UnifiedEvidenceLedger::record` → `FrankenTui.Runtime.UnifiedEvidenceLedger.Record(...)` | **plausible, high confidence** | Same pair as cand-3, the other public method. |
| cand-5 | `renderable_snapshots::tree_tests::tree_node_toggle` (`crates/ftui-widgets/tests/renderable_snapshots.rs:646`) → `FrankenTui.Tests.Headless.TreeTests.TreeNodeToggle()` (`tests/FrankenTui.Tests.Headless/TreeTests.cs:70`) | **plausible, high confidence** | Both are the identical unit test: construct an expanded tree node, toggle, assert collapsed. Test-to-test, straightforward. |

**Honest precision estimate: 4/5 correct (80%) on an n=5 sample** — far too small to generalize a
rate, but large enough to demonstrate the failure mode that matters: the `name-norm` heuristic
cannot distinguish a genuine correspondence from a **coincidental name collision between
independently-authored, identically-named test fixtures** (cand-2's `CounterModel`). Given that
87.5% of the source map is test code (§2) and test suites conventionally reuse small, generic
helper names (`CounterModel`, `TestMsg`, `Model`, …) across unrelated files, this failure mode is
structural, not a one-off — expect it to recur at any scale this heuristic is pointed at a
test-heavy corpus. Only 5 candidates were created against 6,780 skipped pairs; the low creation
rate itself is informative (§7) rather than a sign the heuristic is merely conservative.

## 5. Health v2 (full `kp status` output)

```
mapped: 26191
corresponded: 0
candidates: 5
implemented: 0
verified: 0
stale: 0
absence:
  unknown: 406
  notYetPorted: 0
  deliberatelyDropped: 0
targetOnly:
  unexplained: 6277
  intentional: 0
```

`mapped` = 9,656 + 16,535 = 26,191, checks out. `corresponded` is 0 because all 5 inferred links are
`provenance: candidate` (candidates are counted separately, never as `corresponded`, per
CONTRACT-M15 §1.7 — working as specified). `absence.unknown` = 406 is computed over **non-test**
source entities only (1,201 non-test source entities exist; 406 of those have no unit anchor or
correspondence — the other 795 are presumably covered indirectly, though nothing in this probe
asserts units or non-candidate correspondences, so the 795/406 split deserves a closer look before
being read as "already explained"). `targetOnly.unexplained` = 6,277 is the mirror figure on the
C# side and is enormous relative to `corresponded`/`candidates` — expected at this pre-bootstrap
stage (only 5 machine-inferred, unreviewed links exist total) and not itself a red flag; it is the
number the next increment (bootstrap candidate inference at scale, plus a smarter heuristic per
§7) exists to bring down.

## 6. MESS CATALOG

- **Dirty working tree at scale**: 304 porcelain entries in FrankenTui.Net (263 modified, 40
  untracked, 1 deleted) at the pinned HEAD. `.external/frankentui`'s own nested repo is
  comparatively clean (1 entry).
- **Vendored-but-git-backed upstream, no submodule wiring**: `.external/frankentui` is a full git
  clone (own `.git`, two remotes) sitting inside a `.gitignore`d path with no `.gitmodules` entry —
  neither a clean vendor-drop nor a proper submodule; whoever maintains the pin has to remember to
  do it by hand (and, per §1, evidently hasn't recently — `PROVENANCE.md` is ~2 months stale
  against the actual checkout).
- **Monorepo-of-many-projects shape**: 20 Rust workspace crates vs. 24 real C# `.csproj` (+4
  solution folders) in the `.sln`, plus 3 more scratch `.csproj` living outside the `.sln`
  entirely (`.tmp/showcase-capture`, `.tmp/wtd-selftest`, `sandbox/plasma-color-dotnet`) that the
  target-root-is-the-whole-tree mapping strategy pulls in anyway.
- **Test topology dominates both sides but asymmetrically**: source is 87.5% test entities (Rust
  integration tests compile as their own crate per file — 246 of 273 walked files are under a
  `tests/` directory); target is 25.8% test entities. The map's `is_test` filtering (CONTRACT-M15
  §1.5, absence excludes test entities) is doing real, necessary work here, not decorative work.
- **A large fraction of `.cs` files fail to resolve inside one flat compilation**: 96.2% of target
  entities are `degraded` (15,906/16,535), driven by 28,230 Error-severity Roslyn diagnostics.
  Detailed in §7 — this is the single biggest number in the whole probe and needs its own honest
  treatment, not a passing mention.
- **The repo's own build state contradicts its last commit message.** `test_build_out.txt`, a file
  already committed/present in the working tree (not generated by this probe — the HARD RAIL
  forbids running `dotnet build` inside FrankenTui.Net, so this was read, not reproduced), shows
  **64 compiler errors in `FrankenTui.Runtime`** (duplicate partial-type declarations, missing
  types `ISubscription<>`/`StrategyEvidence`/`SubId`/`StopSignal`, `CS0708` instance members in a
  static class). The HEAD commit's own message claims the solution "builds clean." Taken together
  with the 304 dirty entries, the most likely reading is that the repo is mid-refactor at HEAD and
  the clean-build claim was true at commit time but the working tree has since drifted into a
  broken state — but this probe cannot independently confirm that timeline without building, which
  it is not permitted to do. Reported as testimony, not verified fact.
- **Stray Windows scratch artifacts at repo root**: a literal file named `nul` (contents show a
  `dir /s /b *.csproj`-style command whose output redirection to `NUL` on Windows/Git-Bash created
  a real file instead of discarding output — a classic footgun, not a real target), plus
  `build_out.txt`, `probe_extras.txt`, `probe_focus.txt`, `probe_live.txt`, `cmp.ps1` — ad hoc
  console-dump and comparison scratch files sitting uncommitted or loosely tracked at top level.
- **Docs-as-status-tracking, extensively**: `docs/` holds 43 markdown files, a material fraction of
  which are explicitly status/sync/gap-register documents rather than reference docs: `210-STS-port
  -status.md`, `240-MAP-module-mapping-ledger.md`, `242-MAP-upstream-sync-workflow.md`,
  `244-MAP-divergence-ledgers.md`, `245-MAP-divergence-triage-policy.md`,
  `246-MAP-upstream-contract-gap-register.md`, `335-HST-host-divergence-ledger.md`,
  `336-HST-inline-mode-divergence-ledger.md`, `357-VRF-shared-sample-comparison-scaffold.md`,
  `364-DEM-full-showcase-parity-plan.md`, `365-DEM-showcase-comparison-harness.md`,
  `370-SCR-showcase-screen-checklist.md`, plus three dated blocker notes
  (`2026-03-09-big-batch-blockers.md`, `2026-03-09-hosted-parity-blockers.md`,
  `2026-03-12-windows-conpty-evidence-blocker.md`) and root-level `EXTERNALS.md`,
  `FULL-PORTING-COMPARISON.md`, `PHASE-C-FULL-COMPARISON.md`, `PHASE-C-OUTSTANDING.md`,
  `PORTING-CHECKLIST`, `PORTING-EXECUTION-PLAN.md`, `PORTING-OUTSTANDING.md`,
  `PORTING-REMAINING-GAP.md`. This is exactly the "collection of… status documents" the KodePorter
  charter contrasts itself against — a rich, ready-made testimony corpus for K8's bootstrap
  (ingest as testimony, reconcile, not truth) and a concrete illustration of the orientation-cost
  problem the M4 benchmark measures.

## 7. OPEN FINDINGS

1. **The source-side Overview treemap is functionally degenerate at this repo's shape — 268
   top-level groups instead of ~20 real crates.** `AtlasOverviewBuilder.TopLevelKey` groups by the
   first `::`-segment of `symbolPath` (CONTRACT-M15 §6.2). For `src/` entities this correctly
   yields the owning crate name (`ftui-layout`, `ftui-runtime`, …) because a crate root's own name
   literally *is* the crate name. But `rust-map-dump` (deliberately, and correctly per Rust's own
   compilation model — see its module doc comment) treats each direct-child `tests/<name>.rs` file
   as **its own crate root named `<name>`**, matching what `rustc --crate-name` would actually call
   it — Cargo really does compile every integration-test file as an independent crate with no
   namespace relationship to its containing package. Since 246 of the 273 walked source files are
   such test files, the treemap's "top-level crate" grouping is overwhelmingly individual test-file
   names (`screen_snapshots`, `shadow_run_comparator`, `mpc_vs_pi_evaluation`, …) rather than the
   20 real package crates — 268 distinct groups measured directly against the raw dump. The C#
   target side, by contrast, groups cleanly into ~21 real namespaces (verified by symbol-path
   prefix distribution) because C# namespaces don't have this per-file-is-a-compilation-unit
   wrinkle. **This is a faithful representation of Rust's real structure, not a dump-tool bug, but
   it defeats the Overview panel's stated purpose ("see the whole port at a glance") on the source
   side of exactly this kind of test-heavy monorepo — the fixture flattered KodePorter here just as
   the roadmap predicted it would.** A fix would need `TopLevelKey` (or the dump format) to
   distinguish "real package crate" from "per-file test crate" and group the latter under their
   containing package directory instead of treating them as peers of it — worth a fixture-driven
   follow-up before the next brownfield target rather than fixed silently here.
2. **69 entities were silently dropped by (kind, symbolPath) deduplication — same root cause as
   #1.** The dump emitted 9,725 entities; only 9,656 were imported. `EntityResolution
   .SortAndDeduplicate` treats (kind, symbolPath) as the schema's entity identity and keeps only
   the first occurrence in (file, startLine, symbolPath) order, silently discarding the rest — by
   design, for legitimate cases like a partial type's second declaration. But because per-file test
   crates aren't namespaced under their containing package (#1), **two independently-compiled
   Rust crates that happen to ship a same-named test file collide** — confirmed by sampling:
   `module 'capability_sim_e2e'` (2×), `module 'pty_canonicalize'` (2×),
   `struct 'mpc_vs_pi_evaluation::PiController'` (2×), and 66 more, spread across
   field(29)/method(13)/fn(10)/module(7)/impl(6)/struct(4). This is a real, measured (0.71% of the
   dump) coverage gap, invisible in `kp status` (dropped entities never become an `absence` row —
   they simply never existed as far as the map is concerned) — worth surfacing as a dump-time
   warning count in a future increment rather than a purely silent drop, and it is a concrete,
   small-scale instance of exactly the identity-scheme stress the roadmap's K-D3/K8 frailty watch
   is looking for (§8 "file and symbol identity cannot survive ordinary repository evolution" —
   here it's *ordinary monorepo scale*, not evolution across commits, that breaks the global
   uniqueness assumption).
3. **The C# target map is 96.2% "degraded" (15,906/16,535 entities, 28,230 Error diagnostics) —
   this number is real but needs interpretation, not just a headline.** Two compounding causes,
   both consistent with the evidence gathered: (a) `CSharpRoslynProvider` builds **one flat
   `CSharpCompilation`** referencing only `TRUSTED_PLATFORM_ASSEMBLIES` (BCL) — it has no
   project-to-project reference graph, so any type defined in one `.csproj` and consumed from
   another (routine in a 24-project solution) is unresolvable, cascading into `CS0246` "type not
   found" errors that mark the *consuming* file degraded even though nothing may be wrong with it;
   (b) independently, `test_build_out.txt` (existing repo file, read not reproduced — §6) shows
   `FrankenTui.Runtime` genuinely fails to compile in isolation with 64 real errors (duplicate
   partial-type members, missing types), so some non-trivial share of the 28,230 diagnostics likely
   reflects actual breakage, not just the flat-compilation artifact. **This probe cannot cleanly
   separate (a) from (b) without either giving `CSharpRoslynProvider` a project-reference graph or
   running `dotnet build` — the latter forbidden by the HARD RAIL, so it is reported as an open
   question rather than resolved.** Practically: at this scale, "resolution: degraded" as currently
   computed is a near-useless discriminator (it flags almost everything), which undercuts
   CONTRACT-M15 §1.1's intent ("the '39 diagnostics' become addressable facts") — the imperfection
   vocabulary needs the provider to at least approximate the real project graph (e.g. reference
   each project's own directory as one compilation, or feed real project references from the
   `.sln`) before "degraded" is a useful signal on a real multi-project solution rather than a
   near-constant.
4. **`PROVENANCE.md`'s recorded pin is stale by about two months against the actual `.external
   /frankentui` checkout** (§1) — the kind of drift the map-vs-testimony distinction (K-D3) exists
   to catch, but nothing currently checks it automatically; worth a `kp pin --verify-provenance`-
   shaped follow-up, or at minimum a note in the bootstrap dossier.
5. **Candidate inference's low yield (5 created / 6,780 skipped) is itself informative, not just a
   small number to report past.** Given 1,201 non-test source entities and roughly a comparable
   order of non-test target entities, 5 matches is a very low hit rate for `name-norm` even
   accounting for genuine architectural divergence between the two codebases — consistent with a
   real Rust→C# port where naming conventions (snake_case free functions vs. PascalCase methods
   inside classes, module-qualified vs. namespace-qualified) diverge enough that exact
   normalized-name matching under-fires broadly, not just on the one collision case found in §4's
   sample. The next bootstrap increment (K8) should expect to need a materially richer heuristic
   (or agent-proposed candidates) rather than treating `name-norm` alone as sufficient at this
   scale — matching the roadmap's own framing of `name-norm` as "v1 heuristic."
6. **The Atlas rendered correctly and passed every automated sanity check** — 8.7 MB (well under
   the 15 MB budget), both `<svg>` treemaps present, "Overview" text present, the JSON data island
   (9,068,100 chars) round-trips through `ConvertFrom-Json` cleanly, and health numbers in the
   Atlas's embedded data match `kp status`'s console output exactly. No rendering-scale problem was
   found at ~26k mapped entities — the imperfections above are about what the map *contains* and
   *how source-side entities are grouped*, not about the Atlas failing to represent it.

## 8. Artifacts

- `flagships/frankentui/atlas-probe.html` (8.7 MB) — copied to `showcase/m2/frankentui-atlas-probe.html`.
- `flagships/frankentui/workspace/kp/` — the probe's kp workspace (`kpmap.db` 15.4 MB, `gneiss.db`
  90 KB created-but-unused by `candidates infer`, `.kodeporter/correspondences.yaml` with the 5
  candidate rows from §4). Gitignored (`flagships/*/workspace/`).
- `flagships/frankentui/workspace/rust-dump.json` — the raw `rust-map-dump@0.2.0` output (9,725
  entities). Gitignored.
- `flagships/frankentui/workspace/probe-log.txt` — full console transcript of the run this report
  is drawn from. Gitignored.
- `flagships/frankentui/run-probe.ps1` — the reproducible script (committed).

---

## Update 2026-07-12 — treemap findings addressed; probe re-run

The two Overview-treemap findings above were fixed in AtlasOverviewBuilder and the probe re-run
(same bases, same health numbers, atlas regenerated):

- **Source treemap:** test-only groups (zero non-test entities) are excluded from layout and
  count; caption now reads "24 groups · 1201 non-test entities (244 test-only groups not shown ·
  8455 test entities hidden by default)".
- **Target treemap:** adaptive grouping (a first-segment group holding >50% of a side's non-test
  entities splits by two segments) replaces the single "FrankenTui" monolith with the real
  project structure: FrankenTui.Widgets / .Runtime / .Extras / .Render / .Core / .Backend / ...
  — 35 groups. The source and target treemaps now visibly mirror each other (ftui-widgets ↔
  FrankenTui.Widgets) before any correspondence is asserted.

Remaining findings (identity collisions, degraded-resolution saturation, stale provenance pin,
candidate-heuristic yield) stand as recorded and are queued in ATTIC.md for the bootstrap
increment.
