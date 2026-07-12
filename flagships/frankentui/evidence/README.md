# FrankenTui — extracted evidence (durable)

*These files were extracted from the learning loop's in-progress artifacts (the gitignored
`workspace/` and the session's workflow journal, both of which get garbage-collected) into the
committed record, so the empirical basis of the iteration ledger survives the workspace. They are
the audit trail behind [ITERATIONS.md](../ITERATIONS.md) and the "proven" status of the templates
in [KodePorter/guidance/templates/](../../../KodePorter/guidance/templates/README.md).*

## Files

| File | What it is | Provenance |
|---|---|---|
| `it1-worker-checker-evidence.jsonl` | The 24 structured returns from it1 — 12 `confirm-corr@1` worker verdicts (with evidence pairs + caveats) and 12 `check-confirm@1` checker verdicts (with per-evidence lines + reason). | Extracted verbatim from the wf11 workflow journal (`type:result` lines). |
| `it1-candidate-queue.jsonl` | The full 110-row header-citation candidate queue, each row with `id`, `status`, source/target symbolPath, resolved source/target file paths, the citation note, and provenance. This is the **executable A1 work-list** — the dispatcher loads it, filters `status=="pending"` (98 rows), and dispatches. | Extracted by joining `.kodeporter/correspondences.yaml` (candidate rows) with the map DB (`kpmap.db` entity→file), source file read from the citation note. Verified against `it1-items.json` for the 12 already-run items. |

Both are the map at basis `base` (source `rust-syn@2` over `.external/frankentui` tree-hash
`1813ddb0…`; target `roslyn@5.6` over `FrankenTui.Net` tree-hash `0639f975…`). If the workspace is
rebuilt via [run-probe.ps1](../run-probe.ps1) at the same pinned bases, the candidate ids and
symbol paths regenerate deterministically; these files are the snapshot so a rebuild is optional,
not required, to run A1.

## it1 tally (matches the ledger)

- **12 items** dispatched (worker → independent checker, both haiku).
- **10 accepted**: worker `confirm` + checker `uphold` → promoted to asserted + policy-accepted by
  `policy:kp-frankentui@1`. ids: cand-hc-1, 9, 16, 32, 40, 49, 65, 73, 81, 107.
- **2 refuted-and-upheld** (divergence leads → the A2 queue): worker `refute` + checker `uphold`.
- **0 unsure, 0 overturns, 0 fabricated evidence.** Cost ~102.5k haiku tokens per accepted item,
  113 s wall, 0 human minutes.

## The two divergence leads (A2 starts here)

These are the highest-value outputs of it1 — two files that cite their Rust source in a header but
do **not** substantively port it. Each was refuted by a worker and the refutation independently
upheld by a checker reading the same two files. Full evidence is in the JSONL; the substance:

**cand-hc-24 — `ftui-runtime::alpha_investing` → `FrankenTui.Runtime.AlphaInvestor`**
(`src/FrankenTui.Runtime/AlphaInvesting.cs`, 44 lines vs the Rust file's 562). A simplified
reimplementation, not a faithful port:
- alpha strategy differs — Rust multiplies wealth by `investment_fraction` (0.1); C# uses a fixed
  `DefaultAlpha` (0.05) clamped to `[MinAlpha, wealth*0.5]`.
- `TestOutcome::Skipped` (wealth exhaustion) renamed to `NotInvested` and given a new `MaxTests`
  concept — different outcome semantics.
- the FDR fallback procedures (`bonferroni_test()`, `benjamini_hochberg()`) are omitted entirely.
- `RewardFactor` defaults to 1.0 (vs 0.5), doubling wealth replenishment; no p-value clamping; no
  tests.

**cand-hc-57 — `ftui-runtime::schedule_trace` → `FrankenTui.Runtime.ScheduleTrace`**
(`src/FrankenTui.Runtime/ScheduleTrace.cs`). Core event-type domain model replicated, but not a
compatible port:
- checksum algorithm incompatible — Rust encodes discriminants as bytes `0x01–0x0A` + FNV-1a; C#
  uses `type.GetHashCode()`. Identical event sequences produce different checksums.
- the `spawn()`/`start()`/`complete()`/`cancel()` convenience methods are absent (only `Record()`
  exists) — Rust public API contract not met.
- all sampling/comparison infrastructure omitted (`summary()`, `VoiSampler`,
  `GoldenCompareResult`, `IsomorphismProof`).

**Routing (per [classify-divergence@1](../../../KodePorter/guidance/templates/classify-divergence@1.md)):**
both are candidates for `intentional-divergence` (kind `adapted` or `scaffold`) — but that verdict
is A2's to make on the record, with the target-wide grep + `docs/` testimony the template requires;
these summaries are the leads, not the classification.
