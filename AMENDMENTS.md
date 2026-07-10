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

## 2026-07-11 — Post-M1 positioning and replan (steward direction)

- **KodePorter positioning clarified** (charter §14 paragraph added): the system of record for a
  port — the explicit map with typed imperfections of the mapping itself; orchestration methods
  are consumers, never components. Lessons about porting correctly are absorbed as schema and
  affordances, never as method prose in the product.
- **Orchestration playbook recorded** at [orchestration/PLAYBOOK.md](orchestration/PLAYBOOK.md) —
  Fable-specific agent-orchestration methods from the M1 drive, deliberately fenced from the
  mappings work; may become part of a larger service. Standing tripwire adopted: more
  orchestration prose than product schema in this repo = KodePorter dissolving into methodology.
- **Next increments re-pointed** (roadmap §10.5): M1.5 = the imperfection vocabulary (the map
  describes its own epistemic state, per layer, rendered in the Atlas) + `kp note` + Gneiss facade
  v0.1 + THE-PAGE findings annex. M2′ = FrankenTui probe → **iterative learning loop** (steward
  shape): low-cost agents work and check bounded items, kp records and directs, per-iteration
  health/test deltas measure improvement vs regression, and method-skill accumulates as ledger
  data so "which iterations improve" is a query.
- **Open for steward ratification:** the grounded pairwise conflict semantics and consumed-set
  transitive closure installed during the M1 verification round touch the "fold, not a search"
  principle (judged a deterministic bounded closure, not search — but the call was made by the
  steward's delegate and needs sign-off; see showcase/m1/NOTEBOOK.md and the pending THE-PAGE
  findings annex).
