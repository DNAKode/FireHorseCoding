# M1 Lab Notebook — "See the port"

**Date:** 2026-07-10 · **Milestone:** M1 (first light), scoped per [AMENDMENTS.md](../../AMENDMENTS.md) to the Port Atlas and the Gneiss aspects it depends on · **Fixture:** Slice Zero (`headscan`, Rust → C#)
**Reproduce:** `pwsh showcase/m1/run-m1.ps1` from the repo root (builds, runs the whole story, regenerates every artifact in this directory).

## What this demonstrates

One migration unit carried through the KodePorter-on-Gneiss loop, live: pin and map both sides →
dossier a unit → record five typed correspondences (including one declared systematic adaptation)
→ promote three behavior claims with anchored evidence → decide under **both autonomy postures**
→ run the **differential harness** Rust-vs-C# over the shared corpus → render the **Atlas** →
advance the source one behavior-changing commit → watch the **cone light up** → export the KP-0
floor and the golden ledger.

## The numbers

| | base | after d2 |
|---|---|---|
| mapped entities (source + target) | 169 (102 + 67) | 169 |
| corresponded | 6 | 6 |
| implemented units | 1 | 1 |
| verified units | 1 | 1 |
| **stale** | **0** | **7** |
| unknown | 61 | 61 |

- **Differential verification:** 28/28 corpus cases byte-identical between `headscan` (Rust) and
  `HeadScan` (C#) under `io-agreement-v1` — see [verify-run.md](verify-run.md) for the re-run
  command. The `kp.verification` claim was **auto-accepted by `policy:kp-default@1`** — zero human
  minutes (K-A10's delegated posture), while B1/B2 were accepted by the human actor with recorded
  reasons and B3 was deliberately left proposed (the gated posture, visible side by side in the
  Atlas claims tab).
- **The d2 cone** ([advance-d2.md](advance-d2.md)): entity diff +1 −1 ~3; stale marks exactly
  `{unit parser-core; corr-implements, corr-parse, corr-adapt-result;
  verify:parser-core:io-agreement-v1; behavior B1, behavior B3}` — and **not** B2,
  corr-errorcode, or corr-covers.
- **Cone precision, measured (K-D11):** anchor-drift staleness flags behavior claims {B1, B3};
  the semantic truth ([ground-truth.yaml](../../fixtures/slice-zero/ground-truth.yaml)) is {B1}
  only — B3 ("keys are case-sensitive") anchors its evidence to `headscan::parse`, whose text
  changed in d2 without touching that behavior. **Behavior-claim cone precision at d2: 1/2.**
  This over-approximation is by design, reported rather than hidden: v0 staleness is anchor
  drift, honestly labeled, and precision is a number we track, not a promise we make.

## Artifacts

- [atlas-base.html](atlas-base.html) — the port, seen: both trees, correspondences, claims with
  labels and `why()`, health strip. Everything green/gray, stale 0.
- [atlas-after-d2.html](atlas-after-d2.html) — the marquee frame: the same view after the source
  moved; amber cone across trees, correspondences, and claims.
- [PORTING.md](PORTING.md) — `kp export`, the KP-0 floor: the whole port state as one markdown
  file any tool or agent can read (the anti-lock-in guarantee, K-A9).
- [ledger-export.jsonl](ledger-export.jsonl) — the **golden ledger**: every transaction this
  story appended (68 rows), replayable and inspectable. The demo and the conformance evidence are
  the same bytes.
- [advance-d2.md](advance-d2.md), [verify-run.md](verify-run.md) — the delta report and the
  harness lab notebook, auto-generated as side effects.

## Integration findings (what the story caught that tests did not)

The vertical slice exists to expose exactly these. All were found while wiring the story, fixed,
and are now covered by the updated contract text and code comments:

1. **Behavior-claim subject collision (model bug).** The contract had all of a unit's behavior
   claims sharing subject `unit:<id>` → one Gneiss claim key → accepted claims *conflicted* and a
   later accept would have silently defeated an earlier one at strainer rung 6. Fixed: subjects
   are `behavior:<unit>:<claimId>`; each claim is an independently disputable judgment (the
   granularity rule, enforced by the schema now). Contract §5 corrected with rationale.
2. **Phantom acceptance from stale facts (display bug).** `kp export` computed claim status by
   asking the subject's view without filtering by predicate — and a `kp.stale` **fact** on the
   subject is itself an accepted assertion, so every stale-marked subject displayed as
   "accepted" (undecided B3 included). Fixed with predicate filtering; staleness is now shown
   alongside status, separately and honestly. A lesson about subject-scoped views that
   generalizes: *display layers must say which predicate they are asking about.*
3. **Cross-agent contract seam:** the fixture's dump tool self-identifies as `rust-map-dump@…`,
   the importer validated for `rust-syn@…` — two agents implemented two examples from two
   contract files. Importer now accepts the known-provider set; the exact provider string is
   pinned in the basis either way.
4. **Export field-casing seam:** the Gneiss ledger export writes camelCase (`inputAid`); the
   justification-walking code read `input_aid` and silently found no evidence anchors (so no
   behavior claim would ever have gone stale). Found because the cone came up 2 short against
   ground truth; the count is now exact.
5. **Harness cwd assumption:** the verification harness runs commands from the *workspace
   parent*; with a nested workspace, relative paths died silently ("the pipe is being closed").
   The story uses absolute paths; better child-process diagnostics are queued.

## The adversarial round (what four skeptics found, all probe-confirmed, all fixed)

After the story first ran green, four independent adversarial verifiers attacked the increment.
Every finding below was demonstrated with a live probe against the compiled code before being
fixed, and each fix landed with regression tests:

1. **Fold blocker — transitive conflict grouping.** Assertions that never pairwise conflicted
   (same value, disjoint valid time, bridged by a third) were being defeated "by" assertions they
   never contested — false provenance in `why()`. Replaced with grounded pairwise semantics: every
   `DefeatedBy` now points at an assertion that actually conflicts with the loser *and itself ends
   accepted*; unresolved pairs and cycles surface as Contested. (Gneiss Test11, three scenarios.)
2. **Consumed-set major — one-hop decisions.** A receipt's consumed set missed
   decision-on-decision chains, so `CheckStale` returned false after a `D3 rejects D2 retracts D1`
   append that demonstrably flipped the receipt's answer. Consumed sets now close transitively
   over the decision graph. (Three new staleness tests, including the exact probe scenario.)
3. **Harness major — duplicate-name laundering.** A stream emitting a wrong-then-right duplicate
   line for one case was scored as a clean pass. Duplicates in either stream now fail that case
   explicitly. (The 28/28 M1 pass was independently re-derived and is genuine — 28 unique names,
   byte-identical outputs — the hole was prospective, not exploited.)
4. **Determinism major — wall-clock in content-hashed claims.** Timestamped absolute report paths
   were baked into the verification claim's value, so the claim's aid — and everything downstream
   — differed between identical runs. Report names are now content-addressed
   (`verify-<unit>-<criterion>-<corpusHash12>`), paths workspace-relative, timestamps display-only.
   Cross-run check above: the verification claim aid is byte-identical between two independent
   story executions.
5. **Atlas major — badge markup.** Multi-word status summaries produced malformed CSS class
   attributes (uncolored badges); the unit summary now renders one badge per status with a
   defensively slugified class token, enforced by a markup-shape regression test.

What the skeptics could NOT break: XSS/escaping (every domain-controlled interpolation traced
through the encoder, payloads probed inert), self-containment (zero external requests), truth of
display (health strip, stale set, decision actors all independently recomputed and matched), and
the goldens (rebuilt from source, byte-identical).

## Gneiss facade gaps observed (queued for the next Gneiss increment, not patched around silently)

- `Append` returns only a `TxId`, not per-item aids — the binding recovers aids by scanning the
  ledger export (documented `// DIVERGENCE:` in `GneissBinding`).
- `Label.ReceiptId` is a fresh GUID per `Ask`, which is nondeterministic; the Atlas displays the
  deterministic `ResultHash` instead.
- No facade API to fetch an assertion by aid (needed twice; both callers parse the export).

## Verdict against the roadmap's sub-gate

This is **S1 (mapped & claimed) complete, plus the S2 decision postures and the S3 delta/staleness
mechanics demonstrated on Slice Zero** — ahead of the M1 line the roadmap drew, short of full M2
(no amnesia drill, no seals — deliberately attic'd this increment). The Atlas initial exit
criterion — *"a stranger opens one HTML file and understands what is mapped, what is claimed, and
what is stale, without running anything"* — is met by [atlas-after-d2.html](atlas-after-d2.html).
