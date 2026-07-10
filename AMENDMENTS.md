# Charter Amendments and Decision Log

Formative-phase ceremony per G-A3: steward decides, reason recorded, fixtures updated.
Steward: Govert van Drimmelen (G-A4).

## 2026-07-10 — Amendments adopted

By steward direction ("proceed with the roadmap"), amendments **G-A1…G-A9** and **K-A1…K-A10**
as specified in [CHARTER-REVIEW.md](CHARTER-REVIEW.md) §7 are **adopted for the formative phase**.
Charter text integration is deferred (permitted by G-A3); until integrated, the review's §7 wording
governs. Demoted vocabulary and deferred mechanisms are parked in [ATTIC.md](ATTIC.md).

## 2026-07-10 — M0 decision lock

- **Substrate (steward):** modern C# on .NET 10 (`net10.0`), SQLite databases, CLI + static HTML
  tooling and visualization. Falsification conveniences per kb/03-NOTATION; the conceptual model
  stays platform-neutral.
- **Solution:** `FireHorseCoding.slnx`; projects under `Gneiss/src|tests` and `KodePorter/src|tests`;
  shared `Directory.Build.props` (nullable, invariant globalization, deterministic builds).
- **K2 provider spike verdict:** rust-analyzer 1.94.1 is present, but SCIP (protobuf) and LSIF
  (graph JSON) import plumbing is not worth the cost at this envelope. v0 Rust provider is a tiny
  `syn`-based dump tool (`tools/rust-map-dump`) emitting a shared entities-JSON format — syn is the
  canonical Rust parser, output is deterministic and committable. SCIP/LSIF importers → attic.
- **Scope of this increment (per steward):** through the **Port Atlas** (K-V initial exit) with the
  Gneiss aspects it depends on: E1 (ledger+fold), E2 (labels+why), E3-lite (staleness via
  justification edges and anchor drift), facade. Deferred: seals/amnesia (E4), grades beyond
  `grounded`, closure declarations, `gn` CLI, Gneiss Lens, `kp adopt` — all in the attic with
  triggers.
- **Cost posture (steward):** implementation by cost-optimized agents (Sonnet-class) against
  written contracts; steward-tier effort reserved for contracts, integration gates, and review.
- **Autonomy fixture obligation (K-A10/G-A9):** the increment must demonstrate both the delegated
  (zero-human-minutes, policy auto-accept on green mechanical evidence) and gated decision postures.
