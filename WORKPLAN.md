# WORKPLAN — the non-Fable phase

*2026-07-12. Dispatch brief for the next work phase, which runs **without Fable** — steward-directed,
executed by lighter orchestrators (Opus/Sonnet-class sessions dispatching haiku-class workers per
the learning loop). Everything an orchestrator needs is in the repo; nothing load-bearing lives in
any prior conversation. If this document and the ledger/queues ever disagree, the recorded queue
numbers win and this document gets a dated correction note — per LEARNING-LOOP §2.1, the queues
are the scheduler; this brief only records the rails, the earned next batches, and the gates.*

## 0. Session bootstrap (paste into a fresh orchestrator session)

```
You are orchestrating the next phase of the FireHorseCoding program (Gneiss + KodePorter).
Read, in order: WORKPLAN.md, flagships/frankentui/ITERATIONS.md,
KodePorter/guidance/LEARNING-LOOP.md, KodePorter/guidance/PLAYBOOK.md,
KodePorter/guidance/templates/README.md, ATTIC.md.
Obey WORKPLAN §2 invariants without exception. Execute the next open item in WORKPLAN §3
unless the steward directs otherwise. Every iteration ends with: a kp note iteration record,
an Atlas snapshot into flagships/frankentui/gallery/, an ITERATIONS.md row with measured
numbers, and a checkpoint commit. Report measured numbers only; "unsure" is a valid verdict;
fabricated evidence is the cardinal sin.
```

## 1. State at handoff (measured 2026-07-12)

- Repo: `main` @ `64b90ee`, clean, pushed to `DNAKode/FireHorseCoding`. Solution builds
  (`dotnet build FireHorseCoding.slnx`); tests **155/155** under serial run, 0 failures
  (Gneiss.Cell 36 · KodePorter.Core 100 · GovernanceLedger 19; `dotnet test -m:1`, measured
  2026-07-12 — see §B gotchas for why serial).
- FrankenTui workspace (`flagships/frankentui/workspace/kp`), `kp status` verbatim:
  mapped **89,840** · corresponded **20** · candidates **1,732** · implemented 0 · verified 0 ·
  stale 0 · absence unknown **17,366** · target-only unexplained **4,585**.
- Learning loop: it0 + it1 complete and recorded in
  [ITERATIONS.md](flagships/frankentui/ITERATIONS.md). it1 headline: 12 items, 10 accepted,
  2 refuted-and-upheld, 1,025,327 haiku tokens, 113 s wall, **~102.5k tokens per accepted item,
  0 human minutes**. The `confirm-corr@1` + `check-confirm@1` triple is proven and has earned a
  bigger batch (LEARNING-LOOP §3.2).
- Governance ledger: 12 tx exported in `governance/ledger-export.jsonl`, rendered in
  `governance/LENS.html`.
- Template registry now durable: [KodePorter/guidance/templates/](KodePorter/guidance/templates/README.md)
  (two proven, two drafted-unproven).

## 2. Invariants — the do-not-break list

1. **`C:\Work\FrankenTui.Net` is READ-ONLY, always.** Permitted against that tree: `git log`,
   `git status`, `git rev-parse`, `git clone <that-path> <our-workspace-path>`, and in-process
   file *reads*. Never build, restore, write, or create files there. Target-side building and all
   future port work happen only on the clone under `flagships/frankentui/workspace/clone/`
   (gitignored).
2. **`.external/frankentui` (the vendored Rust upstream inside that tree) is read-only forever.**
3. **Never weaken a test or a golden.** Failing tests you cannot fix go in a blockers note —
   test-laundering is the cardinal sin (PLAYBOOK §5).
4. **The build is the oracle.** Re-run builds and suites at every gate; never accept an agent's
   "all green"; IDE/LSP diagnostics are unreliable in both directions (PLAYBOOK §7). Full-solution
   test runs use `dotnet test -m:1`.
5. **Everything on the record, through `kp`.** Every proposal/verdict/decision flows into the
   workspace ledger with a method envelope (`worker:<tier>|template:<name>@<ver>`); every decision
   names its actor (`--actor policy:kp-frankentui@1` for policy acceptance — never let it default).
   Templates are versioned files in `KodePorter/guidance/templates/`; changing prompt text = a
   version bump + new file.
6. **Constitutional (ratified 2026-07-11, not open at this tier):** grounded pairwise conflict
   semantics; consumed-set transitive closure; evaluation is unique, deterministic, monotone — no
   choice points, no nonmonotone revision loops. Changes to charters, THE-PAGE, or ratified
   semantics are steward-only decisions.
7. **Decisions of substance → [AMENDMENTS.md](AMENDMENTS.md) + a governance-ledger record**
   (§5). Parked work → [ATTIC.md](ATTIC.md) with a named promotion trigger. Nothing is silently
   dropped: if a batch bounds coverage, the bound is recorded.
8. **Checkpoint commits between waves; pushing to `origin main` is authorized.** Never rewrite
   pushed history.
9. **Every work section ends with a visualization artifact** (Atlas snapshot into the flagship
   gallery; LENS refresh when governance changes). This is how the steward judges value — it is a
   deliverable, not decoration.
10. Never describe anything as "production ready".

## 3. Track A — the FrankenTui learning loop (main line, in order)

### A1. it1b — drain the header-citation candidate queue (~99 items) — EARNED, run first

- **Recipe:** exactly it1's, scaled. Queue = the **98 `status:"pending"` rows** in the committed,
  turnkey work-list [flagships/frankentui/evidence/it1-candidate-queue.jsonl](flagships/frankentui/evidence/it1-candidate-queue.jsonl)
  — each row already carries `id`, source/target symbolPath, and resolved source/target file
  paths, so the dispatcher loads the file and dispatches with no map query or rebuild. (The live
  source of record is `workspace/kp/.kodeporter/correspondences.yaml`; the committed JSONL is its
  snapshot so A1 runs even if the workspace was cleaned. If regenerating from scratch, run-probe
  reproduces the same ids deterministically.) Worker `confirm-corr@1` → checker `check-confirm@1`,
  haiku both, pipelined per item, never conversing. Exact prompt bodies and schemas:
  [templates/](KodePorter/guidance/templates/README.md).
- **Batch shape:** two waves of ~50 (LEARNING-LOOP §2.1 bounds an iteration at 10–50 items), with
  the iteration gate between waves — the second wave runs only if the first wave's numbers hold
  (§6 stop rules).
- **Per accepted item:** `kp corr promote --id <id> --evidence-note "<wave summary + evidence>"`
  then `kp decide --subject corr:<id> --verdict accept --reason "<...>"
  --actor "policy:kp-frankentui@1"`.
- **Refuted-and-upheld items:** do NOT decide; add to the divergence-leads list (→ A2).
- **Close-out (mandatory, both waves):** `kp note` iteration record with measured
  tokens/wall/acceptance; `kp atlas` snapshot to `flagships/frankentui/gallery/it1b-atlas.html`;
  `kp export-ledger` golden copy beside it; ITERATIONS.md row; commit.
- **Budget expectation (measured basis):** ~100k haiku tokens per accepted item → ~10M haiku
  tokens for the full drain. If measured cost per accepted item doubles vs it1, stop and record
  (§6).

### A2. Divergence classification — the upheld refutations (starts with cand-hc-24, cand-hc-57)

- **Recipe:** [classify-divergence@1](KodePorter/guidance/templates/classify-divergence@1.md),
  sonnet-class, first batch 100% orchestrator-audited (template is unproven). The dispatcher
  pre-cuts target-wide grep hits and `docs/` testimony hits per the template's item contract.
- **Recording:** per the template's routing table (divergence corr / absence hand-off / corrected
  candidate). Iteration record + ITERATIONS.md row even though the batch is tiny — unproven
  templates especially need their numbers on the record.

### A3. it2 — absence classification pilot (absence-unknown queue: 17,366 entities)

- **The unit of work is a source module (file), not an entity** — one verdict covers all its
  contained non-test entities; the dispatcher fans out `kp absence set --symbol <s> --kind <k>`
  mechanically afterwards. Rationale and full item contract:
  [classify-absence@1](KodePorter/guidance/templates/classify-absence@1.md).
- **Pilot first:** ≤50 modules, haiku worker + haiku checker + orchestrator audit of ≥10 sampled
  verdicts (audit specifically for the empty-grep → false "not-yet-ported" failure mode named in
  the template). Gate on the pilot before draining further.
- **Bonus output:** `likely-ported` verdicts become new candidate correspondences — absence
  classification doubles as candidate discovery for the review queue.

### A4. it3 — baseline reality, on the clone

- `git clone C:\Work\FrankenTui.Net flagships\frankentui\workspace\clone` (reads only `.git`;
  the 304 dirty working-tree entries do NOT come along — the clone is committed HEAD `afd20f0`,
  which is the point: it cleanly separates committed truth from working-tree drift).
- `dotnet build` the clone's solution; run the headless test project(s). Expect failures — the
  repo's own committed `test_build_out.txt` shows 64 compile errors in `FrankenTui.Runtime` at
  some prior state, and the historical testimony claims 2,815 passing / 134 failing headless
  tests. **Whatever is measured is the inherited baseline**: record it via `kp note`
  (`it3 baseline: ...`) and an ITERATIONS.md row. From this moment on, inherited failures are
  permanently separated from anything the loop introduces.
- No fix work in it3. Measurement only. If the clone does not build at all, that IS the baseline
  — record the error inventory honestly.

### A5. it4 — first synthesis wave — GATED

- Only after A1–A4 land green, and **only with an explicit steward go** (this is the first time
  the loop writes code; the steward said "maybe, maybe not" to a full port — the numbers from
  A1–A4 are the input to that decision).
- Shape when unlocked: small leaf gaps from the absence queue; a `port-leaf-fn@1` template
  (sonnet-class, to be authored then, with compile/differential evidence required and clone-branch
  isolation); batch ≤10.

### A6. The name-norm candidate queue (~1,632 items) — NOT earned yet

Do not drain wholesale. The probe proved its structural false-positive mode (generic test-fixture
names). After it1b, decide at the gate: either a dispatcher pre-filter (non-test both sides +
path-structure agreement) shrinks it to a reviewable queue, or it waits for a better heuristic.
Record whichever choice is made.

## 4. Track B — engineering promotions from ATTIC (between iterations, contract-first)

In priority order; each is contract-first (PLAYBOOK §1), lands with tests, and updates its
ATTIC row. Do not batch them into iteration waves — they are separate commits.

1. **NuGet reference feeding for per-project Roslyn** (ATTIC, trigger already met): resolve
   references from the global package cache so target resolution grades discriminate again
   (currently 96.2% `degraded` — saturated). Acceptance: degraded share drops to whatever honest
   number the real project graph yields, reported as-is in the next Atlas.
2. **Atlas data-island compaction**: full-map Atlas is 31.3 MB vs the 15 MB CONTRACT-M15 §6.1
   budget — a standing budget breach, promoted before the gallery grows further.
3. **Bulk absence verb** (`kp absence set --under <module>` or equivalent): unblocks A3's fan-out
   if per-entity CLI calls prove too slow at 17k scale. Promote on demand from A3.
4. **impl-block symbolPath disambiguation** (131 surfaced (kind,symbolPath) duplicates).
5. **Transient SQLite-open retry in GneissLedger** (unlocks parallel test runs; until then §2.4
   serial rule stands).
6. **kp.iteration as a declared predicate** (iteration records currently ride `kp note`; small,
   makes "which iterations improve" a typed query — LEARNING-LOOP §3.2's intent).

## 5. Track C — governance and bookkeeping obligations (standing)

- Phase-start is already recorded (see `governance/ledger-export.jsonl` tail). Record each
  landing (iteration close, ATTIC promotion, gate decision) as it happens:

  ```
  dotnet run --project tools/GovernanceLedger -- record --dir governance \
    --actor <orchestrator-name> --reason "<why>" --subject decision:<slug> \
    --predicate gov.decision --value "<one-line outcome>" --wall <iso8601-utc>
  dotnet run --project tools/GovernanceLedger -- export --dir governance
  ```

  Commit the regenerated `ledger-export.jsonl` + `LENS.html`.
- ITERATIONS.md gets one row per iteration, measured numbers only, including failures.
- AMENDMENTS.md gets an entry for any decision of substance; the steward ratifies.
- **Collect, don't author:** the keep-earning memo (M3) is written later, WITH the steward, FROM
  the economics rows this phase produces. Produce the rows; do not write the memo.

## 6. Gates, budgets, stop rules

**The iteration gate** (LEARNING-LOOP §2.4) after every batch: acceptance rate, overturn rate,
tokens per accepted item, health delta, test-baseline delta. Reference numbers: it1 = 83%
acceptance, 0% overturn, ~102.5k tok/accepted, 0 human minutes.

**Stop rules — halt the queue, record the numbers, escalate to the steward:**

- Any fabricated evidence caught (checker or audit) → immediate halt of that (queue, template,
  tier) triple.
- Wave acceptance < 60%, or checker overturn > 25% → the worker template is broken: version-bump
  or re-tier with a recorded reason before continuing (never grind a failing triple).
- Tokens per accepted item > 2× the previous iteration of the same triple → dispatcher problem;
  stop scaling and investigate.
- Any roadmap §8 frailty signal (candidate hypothesis-spam, rubber-stamp audit failures,
  map-maintenance cost exceeding re-investigation savings) → record and raise; these feed the
  kill-watch, they are not routine noise.

**Escalate to the steward (do not decide locally):** template retirements; tier escalations
(recorded decisions per LEARNING-LOOP §1.3); anything in §7; any write outside this repo, the
workspace, or the clone.

## 7. Out of scope for this phase — parked, with owners

- Meta-ledger bootstrap + `import`/federation verb promotion (constitutional surface; steward +
  a Fable-class session).
- Seals/amnesia drill (E4), grades beyond constant `grounded` — scheduled at M2/S3.
- Keep-earning memo authoring (M3, steward; this phase only collects the data).
- Orientation benchmark design/run (M4).
- Website / knowledge site; any publishing decision for flagship ports (explicitly deferred by
  the steward).
- Onboarding a second flagship (finish FrankenTui's queues first).
- Charter, THE-PAGE, or ratified-semantics edits.

## 8. Definition of phase-done

The phase is done when: the header-citation queue is drained (A1); its divergence leads are
classified (A2); the absence pilot has gated numbers and a scale/hold decision (A3); the clone
baseline is measured and recorded (A4); Track B items 1–2 are landed or explicitly re-parked with
reasons; and every one of those has its ITERATIONS.md row, gallery frame, governance record, and
commit. The exit artifact is the gallery + ITERATIONS.md + LENS — sufficient for the steward (or
a returning Fable) to judge the phase from the visuals and the ledger alone, per the program's
own standard: no status meeting, just the record.

---

## Appendix A — command crib (verified working 2026-07-12)

```powershell
# Build + test (serial: SQLite Error 14 flakes under parallel test hosts — ATTIC)
dotnet build FireHorseCoding.slnx
dotnet test FireHorseCoding.slnx -m:1

# kp CLI — after build the exe is at:
#   KodePorter\src\KodePorter.Cli\bin\Debug\net10.0\KodePorter.Cli.exe
# or: dotnet run --project KodePorter\src\KodePorter.Cli\KodePorter.Cli.csproj -- <verb> ...
# THE WORKSPACE PATH IS THE kp SUBDIRECTORY, not the workspace root:
$kp = 'C:\Work\FireHorseCoding\KodePorter\src\KodePorter.Cli\bin\Debug\net10.0\KodePorter.Cli.exe'
$ws = 'C:\Work\FireHorseCoding\flagships\frankentui\workspace\kp'
& $kp status --workspace $ws
& $kp notes --workspace $ws
& $kp corr promote --workspace $ws --id cand-hc-31 --evidence-note "..."
& $kp decide --workspace $ws --subject corr:cand-hc-31 --verdict accept --reason "..." --actor "policy:kp-frankentui@1"
& $kp absence set --workspace $ws --symbol <symbolPath> --kind not-yet-ported --note "..."
& $kp note --workspace $ws --text "itN record: ..." --actor <orchestrator>
& $kp atlas --workspace $ws --out flagships\frankentui\gallery\itN-atlas.html
& $kp export-ledger --workspace $ws --out flagships\frankentui\gallery\itN-ledger-export.jsonl

# Full verb/flag reference: & $kp --help   (or KpCliApp.cs)
# Probe re-run (read-only, regenerates map from pinned bases): flagships\frankentui\run-probe.ps1
# Rust side dump tool: fixtures\slice-zero\tools\rust-map-dump (cargo, v0.4.0)

# Governance ledger:
dotnet run --project tools/GovernanceLedger -- record --dir governance --actor <a> --reason <r> `
  --subject decision:<slug> --predicate gov.decision --value <v> --wall <iso8601>
dotnet run --project tools/GovernanceLedger -- export --dir governance
```

## Appendix B — operational gotchas not already in PLAYBOOK.md

- `--workspace` points at `...\workspace\kp`, not `...\workspace` (run-probe.ps1 sets
  `$ws = Join-Path $wsDir 'kp'`). Wrong path fails with a missing-db error, not a hint.
- `kp decide --actor` defaults to `govert` — ALWAYS pass `--actor` explicitly for agent/policy
  decisions, or the record lies about who decided.
- `global.json` pins SDK 10.0.301 (`rollForward: latestFeature`) because a preview SDK silently
  retargeted projects. Don't remove it; don't add per-csproj TFMs (Directory.Build.props owns
  `net10.0`).
- Orchestrator Workflow scripts: `args` may arrive as a JSON *string* — guard with
  `typeof args === 'string' ? JSON.parse(args) : args`.
- Agent structured summaries can be junk ("test") while the underlying work is real and complete,
  and vice versa — only ground truth (build, tests, files on disk, `kp status`) counts.
- Gneiss ledger-export field casing differs by producer (`inputAid` vs `input_aid`) — parsers in
  this repo accept both; keep it that way.
- Provider strings: both `rust-syn@…` and `rust-map-dump@…` prefixes are accepted
  (KnownProviderPrefixes) — don't "normalize" one away.
- Give harness/story scripts absolute paths; a subagent's cwd is not guaranteed to be the repo
  root (a relative cargo path once failed with "the pipe is being closed").
- The full-map Atlas currently breaches its 15 MB budget at 31.3 MB (known, Track B #2). Gallery
  snapshots still open fine in a browser; don't email them.
- A root `.gitignore` pattern can swallow a deliverable (`flagships/*/workspace/` is intentional —
  the clone and dbs must NOT be committed; gallery files MUST be).
