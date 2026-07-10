# Risks, Anti-Patterns, and Kill Criteria

*Extends seed §21's five risks with the failure modes the surveys surfaced, a when-NOT-to-Gneiss list, and honest kill criteria for the whole endeavor.*

## 1. The seed's five, with sharpened mitigations

| Risk (seed) | Sharpened mitigation |
|---|---|
| R1 Too abstract | P-1 retrofit probe is a *gate*, not advice: no kernel code before two mapping memos pass without contortions. Every design doc carries a worked example (wrong-silo story). |
| R2 Too much metadata | The [23-STORAGE.md](23-STORAGE.md) §6 decision procedure ends "plain column, forever, with a clear conscience." Modeling rings are per-predicate opt-in; Fowler's warning (temporality is a tax) is policy. |
| R3 Query complexity | Applications read projections only; the ledger is not a query surface. Named contexts + providers are the API; raw assertion joins in app code are a review-blockable smell. |
| R4 Performance | Dense data never enters the ledger (descriptors). L0 oracle + L1 per-key recompute + L2 traces designed in from day one (survey 13's ladder). EAV pain is dodged structurally, not tuned away. |
| R5 Ambiguous truth | Accounting vocabulary throughout the UX (as reported / restated / contested); every belief badge names its stance. Glossary is binding. |

## 2. New risks (from surveys and from thinking it through)

**Inner-platform effect.** The gravitational pull toward rebuilding a worse database inside the database — custom query language, custom type system, custom constraint engine. Tripwires: the moment someone proposes a Gneiss query DSL richer than "named views + provider calls," or generic constraint evaluation inside the belief engine, stop. SQL remains the query language; Gneiss adds *semantics*, not syntax.

**The uniformity temptation (EAV creep).** Success at ring B1–B2 will tempt migrating comfortable plain columns into assertions "for consistency." Consistency of *contract* is the goal, not consistency of *representation* — seed §2's own answer ("no single representation should be forced to do all jobs") is the tripwire question in every schema review. Magento/OpenMRS EAV histories are the cautionary canon.

**Semantic debt / context proliferation.** Contexts nobody understands are worse than no contexts — they turn "multiple truths" from a governed feature into folklore. Mitigations: the canonical five contexts ([24-CONTEXTS.md](24-CONTEXTS.md) §3) with new named contexts requiring the same review ceremony as an ontology change; a standing report of contexts actually used by report runs (unused contexts get retired); context *count* as an explicit health metric.

**Hypothesis spam.** An over-eager matcher (or agent) can flood the review queue and devalue decisions — the three-band economics (survey 12) invert if the middle band is huge. Mitigations: per-method budgets and precision tracking (a method whose acceptance rate collapses gets its admission threshold raised automatically — which is itself an assertion about the method-entity, pleasingly); review-queue depth as an operational alarm.

**Decision fatigue → rubber-stamping.** If reviewers bulk-accept, the decision layer records fiction. Mitigations: sample-audit of accepted hypotheses; surface disagreement rates; make bulk actions explicit decision kinds (`bulk_accepted`) so belief views can weight them differently later. The most expensive data in the system (survey 12) deserves quality control of its own.

**Knowledge-horizon dishonesty.** Bulk imports with import-time transaction times make "as known then" quietly wrong for the pre-import era. Mitigation is disclosure, not cleverness: every system publishes its horizon; audit contexts before the horizon render a banner, not silent best-effort ([21-TIME.md](21-TIME.md)).

**Solo-maintainer scope.** This is (currently) one person's meta-architecture across several systems. The A2 library must stay small enough to maintain as a side asset — the surveys' repeated lesson that dead frameworks (Cozo, TMS libraries, DDlog) die from unstaffed ambition is a mirror, not just a sourcing note. Tripwire: if Gneiss.Kernel exceeds a few thousand lines or grows a plugin system, it is becoming a platform without a team.

**Ledger operations neglect.** Append-only stores that nobody compacts, snapshots, or archives become the reason the pattern gets blamed ("the audit table ate the disk"). [23-STORAGE.md](23-STORAGE.md) §8 practices are part of the definition of done for P2, not later polish.

## 3. When NOT to use Gneiss (a checklist to keep)

- The data is never corrected, never disputed, single-source, and nobody asks historical questions → plain CRUD. Most tables in most systems.
- Real-time control loops / sub-second decisions → wrong latency class; Gneiss consumes telemetry summaries, it does not sit in the loop.
- Genuinely collaborative-editing semantics (documents, CAD) → that's OT/CRDT territory, not belief revision.
- A domain with one authoritative source and legal finality (the billing ledger itself) → it *is* an append-only ledger already; Gneiss adds vocabulary, not machinery (A0 applies, A2 doesn't).
- Anything where the team won't sustain the review/decision workflow — the model's value collapses if decisions aren't actually made.

## 4. Kill criteria (for the honesty file)

Abandon or radically shrink Gneiss if:

1. **P-1 fails:** the vocabulary needs contortions to describe systems it was distilled *from*.
2. **P1's claim keys prove unstable** in Smoothscrape's real data — decision survival was the founding use case; if it doesn't work there, the generalization is built on sand.
3. **The P2 gate fails:** after both exist, the honest memo concludes A1-native patterns deliver ~all the value at a fraction of the complexity — then Gneiss ships as a *pattern book* (A0/A1 discipline + this kb) and no library, and that outcome should be celebrated as a finding, not mourned as a failure.
4. **Nobody uses the decision workflow** after three months in production — the human layer is load-bearing; without it Gneiss is ceremony.
5. **A credible off-the-shelf composite appears** (the matrix's empty composite gets filled — e.g., XTDB grows decision overlays + evaluation contexts, or Marten grows bitemporal belief semantics). Reassess buy-vs-build immediately; pride is not an architecture principle.
