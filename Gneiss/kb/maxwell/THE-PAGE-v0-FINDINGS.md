# THE PAGE v0 — Implementation Findings Annex

*Opened 2026-07-11. The page stays frozen at v0 per the bet protocol; divergences discovered
during implementation are findings, recorded here, never silent patches. The first
implementation is `Gneiss.Cell` (built 2026-07-10 from
[the cell contract](../../src/Gneiss.Cell/CONTRACT.md), which mediated the page; a direct
second-implementation-from-the-page test remains scheduled as E8a). Ratifications by the
steward are marked; everything else is delegate judgment awaiting no further action.*

## F1 — `ctx` is a materialized view, not a seventh base relation *(confirms kb/50 §4 flag i)*

Implemented exactly as the freeze review predicted: named contexts are `gneiss.context`
declarations (assertions), resolved at ask time with `DefCut` pinning; `bootCtx` is fixed in
code. The schema's `ctx(...)` row in §(a) should be read as derived. No page change needed at
v0; v1 should restate the banner ("six append-only relations" — and mean it).

## F2 — Multi-candidate conflict resolution: grounded pairwise semantics **(RATIFIED 2026-07-11)**

R5 (`defeated :- conflict(A,B,C), prefers(...)`) is stated pairwise, and the page is silent on
three-plus mutually-chained candidates. The first implementation resolved whole
connected-components through one strainer contest — refuted by adversarial verification with a
live probe: an assertion could be recorded as defeated by a winner it never conflicted with
(same value, disjoint valid time, bridged by a third candidate) — false testimony in `why()`.

**Ratified semantics:** build the pairwise attack graph (an edge only where two candidates
genuinely conflict: overlapping valid time + incompatible values under the predicate's
comparator; each edge contested through the strainer rungs honoring the predicate's StopRung —
a stopped contest yields no attack). Then settle labels by grounded iteration to fixpoint:
a candidate with no live attacker is **accepted**; a candidate attacked by an accepted winner
is **defeated** (defeater recorded = that winner, deterministic pick by (tx, aid)); repeat
until stable. Residue — genuine cycles, or candidates entangled with an unresolved pair — is
**contested**, surfaced with the stopping rung. Guarantees: every `DefeatedBy` names an
assertion that actually conflicts with the loser *and itself ends accepted*; unique result;
deterministic; monotone (labels only ever become more decided); terminates in ≤ component-size
passes; two-candidate behavior unchanged.

**Constitutional wording tightened (steward, 2026-07-11):** "belief is a fold, not a search"
means — and always meant — *unique, deterministic, monotone evaluation: no choice points, no
nonmonotone revision loops*. Bounded monotone closures (the decision-effectiveness pass, the
grounded conflict labeling, the consumed-set closure of F3) satisfy the principle. The E1
kill-signal is narrowed accordingly: what kills the design is ambiguity or solver-style
search, not a terminating monotone iteration.

## F3 — The label's consumed set closes over the decision graph **(ratified with F2)**

§(e) defines the LABEL as *the consumed set of the evaluation*. The first implementation
recorded only decisions one hop from matched assertions; a decision-on-a-decision that the
fold actually read (D3 rejects D2 rejects D1 targets A) was absent from the receipt, so
`CheckStale` missed appends that demonstrably flipped the answer — refuted by live probe.
Fixed: the consumed set closes transitively over the decision graph (decisions targeting
anything in the frontier, including decisions targeting decisions, to fixpoint). This is not
a semantics change; it makes §(e)'s definition true.

## F4 — R13/R14 stratification verification deferred with seals *(kb/50 §4 flag ii)*

Seals (R11–R14 beyond constant `grounded`) were deliberately deferred from the first increment
(attic; scheduled M2/S3 — and now on the product path: a one-shot port's deliverable is its
sealed map). The freeze review's obligation to verify the negation strata of R13/R14
explicitly transfers to that increment unchanged.

## F5 — Facade findings (v0 → v0.1), from the first consumer

The KodePorter binding surfaced three API gaps worked around via ledger-export scans, queued
as facade v0.1: `Append` should return per-item aids; receipt ids should be content-derived
(a random GUID per Ask defeats artifact determinism downstream); an assertion should be
fetchable by aid. None touches page semantics.
