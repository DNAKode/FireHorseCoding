# FrankenTui — Iteration Ledger

*The human-readable export of the learning loop's bookkeeping (the machine record lives in the
workspace ledger's notes/claims and the gallery). One row per iteration; every number measured,
never estimated.*

## it0 — 2026-07-12 — make the map honest (prerequisites)

- **Delivered:** module-include walking (rust-map-dump v0.4.0: 768/846 files, 73,478 entities,
  0 gaps — up from 273 files / 9,725 entities); per-project Roslyn compilation machinery;
  identity namespacing for per-file test crates + dropped-duplicate surfacing; the
  header-citation heuristic + anchor-rule fix (parentless-root → any module entity);
  `kp decide --actor`; `kp corr promote`; provenance-freshness probe step.
- **Map after it0:** mapped 89,840 (73,305 source + 16,535 target) · candidates 1,741
  (109 header-citation + 1,632 name-norm) · absence-unknown 17,366 · target-only-unexplained
  4,690. Citation matching: 157/187 citations matched, 109 candidates created, 30 unmatched
  (short-form/absent files — recorded).
- **Honest misses:** target resolution still 96.2% degraded — per-project compilation without
  NuGet package resolution doesn't de-saturate a repo that genuinely doesn't compile (next:
  feed references from the global package cache). Atlas at full map = 31.3MB, 2× budget
  (island compaction queued).
- **Gallery:** `gallery/it0-atlas.html`.

## it1 — 2026-07-12 — confirm citations (first cheap-agent batch)

- **Triple:** queue=candidate-review(header-citation) · template=`confirm-corr@1` →
  `check-confirm@1` · tier=haiku (both waves).
- **Batch:** 12 items · **accepted 10** (worker-confirm + checker-uphold → promoted to asserted
  + policy-accepted by `policy:kp-frankentui@1`) · **2 refuted-and-upheld** (cand-hc-24
  alpha_investing, cand-hc-57 schedule_trace — cited but not substantively ported; held as
  candidates, flagged as divergence leads) · 0 unsure · 0 fabrication caught.
- **Cost (measured):** 1,025,327 haiku tokens · 113 s wall · **~102.5k tokens per accepted
  item** · human minutes: **0**.
- **Method notes:** worker evidence quality was high (trait↔interface, enum↔record-hierarchy,
  formula-identical mappings with honest caveats about intentional divergences); checkers added
  real value — both refutations were substantiated, and one checker caught a note imprecision
  without overturning. The LEARNING-LOOP §4 per-item forecast (10³–10⁴ tokens) was ~10× low:
  reading two real source files dominates. Still ≪ an exploration session, and haiku-priced.
- **Map after it1:** corresponded 20 · candidates 1,732.
- **Gallery:** `gallery/it1-atlas.html` · golden ledger `gallery/it1-ledger-export.jsonl`.
- **Evidence (extracted to the committed record):** `evidence/it1-worker-checker-evidence.jsonl`
  (all 24 worker+checker structured returns) and `evidence/it1-candidate-queue.jsonl` (the full
  110-row header-citation queue with resolved file paths — 10 accepted, 2 held, 98 pending; the
  executable A1 work-list). See `evidence/README.md`, including the two divergence leads spelled
  out. These survive workspace garbage-collection; the raw workspace is otherwise gitignored.
- **Next-iteration directions:** scale this triple to the remaining 98 hc-candidates (its
  numbers earn it — WORKPLAN §3 A1); route the two upheld refutations to divergence classification
  (A2); it2 = absence classification per BOOTSTRAP-PLAN (A3). The full phase brief is
  [WORKPLAN.md](../../WORKPLAN.md).
