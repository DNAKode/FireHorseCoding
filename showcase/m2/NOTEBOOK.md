# M2 Lab Notebook — "The map describes itself" (M1.5 + the FrankenTui probe)

**Dates:** 2026-07-11/12 · **Scope:** M1.5 (the imperfection vocabulary, facade v0.1, the
governance ledger, Atlas v2) + the FrankenTui read-only probe with its rich status Atlas — the
steward's requested deliverable.
**Reproduce:** `pwsh flagships/frankentui/run-probe.ps1` (the probe, ~40s end to end);
`dotnet run --project tools/GovernanceLedger -- export --dir governance` (the LENS);
`dotnet test FireHorseCoding.slnx -m:1` (143 tests; see the flake note below).

## What this section delivered

**The imperfection vocabulary (M1.5):** the map now describes its own epistemic state, per layer
— per-entity resolution grade and test-ness, continuity candidates on advance, correspondence
provenance (candidate/asserted, verified derived), unit depth, typed absences with computed
defaults, verification independence with an optional policy floor, and Health v2 with absence and
target-only breakdowns that exclude tests. Plus `kp note` (two-tier capture), `kp candidates
infer` (name-norm v1), and Gneiss facade v0.1 (per-item aids from `Append`, content-derived
receipt ids, `GetAssertion`) — the M1 workaround divergences deleted.

**The governance ledger (the meta-meta tier, D28 made real):** `tools/GovernanceLedger` seeds the
FireHorseCoding decision history — nine decisions with real actors (govert/fable), reasons quoted
verbatim from AMENDMENTS.md and commit messages (fidelity-checked: 9/9 traceable), and commit
hashes as evidence. The morning-positioning supersession is a genuine Gneiss `supersedes`
decision, and [governance/LENS.html](../../governance/LENS.html) — Gneiss's first visual —
renders it struck-through-but-present with a link to the superseding transaction.
Correction-without-erasure, demonstrated on our own history. The committed durable artifact is
`governance/ledger-export.jsonl`; the db rebuilds from it byte-identically.

**Atlas v2:** lazy-render trees (roots immediate, children on expand, 200-item pagination),
the Overview tab with deterministic squarified SVG treemaps, imperfection badges throughout,
hide-tests default ON, search + kind filters, absence drill-downs, an Identity tab.

## The probe (read-only, both repositories untouched — verified)

| | Source (Rust, `.external/frankentui`) | Target (C#, FrankenTui.Net) |
|---|---|---|
| Structure | 20 workspace crates | 24 csproj in the sln (+3 stray) |
| Entities | 9,725 dumped / 9,656 imported | 16,535 |
| Test share | **87.5%** | 25.8% |
| Resolution | 100% clean | **96.2% degraded** (28,230 diagnostics) |
| Treemap groups (non-test) | 24 (244 test-only hidden) | 35 (adaptive two-segment split) |

Health v2 at probe: mapped 26,191 · corresponded 0 · candidates 5 · absence-unknown 406 ·
target-only-unexplained 6,277. Full probe wall time ≈ 40s; Atlas 8.7MB, opens from `file://`.

## Findings (all recorded in [PROBE-REPORT.md](../../flagships/frankentui/PROBE-REPORT.md); queue in ATTIC.md)

1. **Identity collisions, small but real:** 69 entities (0.71%) silently dropped — per-file Rust
   test crates sharing names. The K-D3 frailty watch, made concrete at brownfield first contact.
2. **`degraded` saturates:** one flat BCL-only compilation marks 96.2% of the target degraded —
   an honest signal rendered useless by its own coverage. Per-project compilation queued. The
   probe also surfaced *genuine* build breakage the repo's own committed test output records
   (64 real errors in FrankenTui.Runtime) — contradicting the HEAD commit's "builds clean."
3. **The provenance pin is ~2 months stale** (PROVENANCE.md vs the actual vendored checkout) —
   exactly the class of drift the map exists to catch; a freshness check is queued.
4. **Candidate inference needs teeth:** name-norm yielded 5 candidates (4 plausible, 1 confirmed
   false positive from generic test-fixture naming) against 6,780 skips. Highest-yield next
   heuristic, discovered by reading the code: FrankenTui.NET files *cite their Rust source file
   in header comments* — a correspondence goldmine the bootstrap should harvest.
5. **Treemap grouping needed brownfield tuning** (fixed same-day, probe re-run): test-only groups
   excluded with honest captions; adaptive two-segment split turns the target monolith into the
   real project structure. Source and target treemaps now visibly mirror each other
   (`ftui-widgets` ↔ `FrankenTui.Widgets`) before any correspondence exists — the "see the whole
   port at a glance" promise, kept.
6. **Test-infra flake (environmental):** parallel full-solution test runs intermittently fail one
   test with SQLite Error 14 (cross-testhost temp contention on Windows); serial runs are stable
   (143/143, twice). Retry hardening queued; use `-m:1` meanwhile.

## Verdict

M1.5 complete and the probe stage of M2′ complete. The imperfection vocabulary earned its keep on
first contact — every probe finding above *is* a typed imperfection the map can now say out loud
(degraded resolution, absence-unknown, candidate provenance, identity continuity). The bootstrap
increment (K8) has a concrete, evidence-backed work queue and one very promising correspondence
heuristic. The learning loop starts from here.
