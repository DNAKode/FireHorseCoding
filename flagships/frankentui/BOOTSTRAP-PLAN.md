# FrankenTui — Bootstrap & Learning-Loop Plan

*2026-07-12. Applies [guidance/LEARNING-LOOP.md](../../KodePorter/guidance/LEARNING-LOOP.md) to
the first flagship. The probe (PROBE-REPORT.md) supplied the evidence; this plan turns it into
iterations. Working copy discipline: the upstream Rust tree stays read-only forever; target-side
builds/tests/port work happen on a local **clone** of FrankenTui.Net under
`workspace/clone/` (the steward's working repository, with its 304 dirty entries, is never
touched).*

## Iteration 0 — make the map honest (prerequisites, from the probe's findings)

1. **Per-project Roslyn compilation**: parse the .sln/.csproj graph, compile per project in
   dependency order with `CompilationReference`s — so `degraded` means something again
   (target: well under 20% degraded, reported honestly whatever it lands at).
2. **Identity namespacing** for per-file Rust test crates (qualify with owning package) +
   dropped-collision surfacing in ImportResult and the report (69 silent drops → 0 silent).
3. **The header-citation heuristic** (`kp candidates infer --heuristic header-citation`):
   FrankenTui.NET files cite their Rust source files in header comments — harvest them into
   file-level candidate correspondences (provenance `candidate`), the highest-yield seed the
   probe found.
4. **Provenance freshness check** in the probe script (recorded pin vs actual checkout).
5. `kp decide --actor` so policy actors can decide on the record (zero-human-minutes path for
   correspondence confirmations).

Exit: probe re-run; Atlas snapshot `gallery/it0-*.html`; candidate queue populated.

## Iteration 1 — confirm citations (the first cheap-agent batch)

- **Queue:** candidate-review (header-citation candidates). **Template:** `confirm-corr@1`
  (worker: read the cited Rust file + the C# file, verdict confirm/refute/unsure + 3 concrete
  evidence pairs). **Checker:** `check-confirm@1` (independent: verify each evidence pair exists
  and supports; uphold/overturn). **Tier:** haiku-class both.
- **Acceptance:** worker-confirm + checker-uphold → accepted by `policy:kp-frankentui@1`;
  anything else → left proposed (routed to the next iteration or a human sample).
- **Batch:** 10–15 items. **Bookkeeping:** method envelopes on every proposal; `kp.iteration`
  record with tokens/wall/acceptance; Atlas snapshot `gallery/it1-*.html`.

## Iterations 2+ (directional, each gated by its predecessor's numbers)

- **it2 — absence classification** (absence-unknown queue, `classify-absence@1`, haiku): is this
  uncovered Rust entity not-yet-ported, test-only, or upstream-internal? High count, low
  judgment — prime cheap-agent territory.
- **it3 — baseline reality** (on the clone): build + headless test run; inherited failures
  separated from everything the loop later does; the 2,815/134 historical baseline re-measured.
- **it4 — first port synthesis wave** (small leaf gaps from the absence queue, `port-leaf-fn@1`,
  sonnet-class, differential/compile evidence required) — only after it1–it3 prove the
  bookkeeping and the checker economics.
- Template/tier changes between iterations are version bumps recorded in the iteration ledger;
  the gallery shows whether the port's health curve bends.

## What "learning how to port kode" means here, concretely

Every iteration leaves behind: (a) accepted, evidence-backed map content; (b) a `kp.iteration`
row that scores the (queue, template, tier) triple; (c) an Atlas snapshot. The guidance layer
harvests (b) — templates that survive contact get promoted toward the meta-ledger; templates
that regress get retired with reasons. The port is the product; the *measured method* is the
service's compounding asset.
