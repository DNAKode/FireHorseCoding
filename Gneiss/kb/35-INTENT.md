# Intent and Realization: The Alignment Layer

*Added 2026-07-05 from discussion. Govert's thesis: systems are robust when they are **realizations on top of captured intent** — intent at multiple dimensions, scales, and directions. Gneiss already shares the philosophy (views over contingent deductions from imperfect inputs, traceable, with simple surfaces over a rich multi-modal substrate, providing the base for action management, planning, and forecasting at every level). The Intentional Programming connection: where machinery-reveal happens may be where intent-alignment happens — and Gneiss's job is to make the implicit "this realizes that" relationship of system construction explicit, ledgered, and drift-detecting, exactly as it did for operational state and reasoning. Survey 18 landed same day ([18-SURVEY-INTENTIONAL.md](18-SURVEY-INTENTIONAL.md)); verdicts absorbed throughout and summarized in §10.*

## 1. The dynamic-arrays lesson, generalized

Excel's dynamic arrays look like a small layer over the grid; their impact was outsized because they changed *what a reference is*. `A1:A17` is **extensional** — a frozen realization of the intent "all the orders," correct on the day it was written and silently wrong the day order 18 arrives. `FILTER(orders, ...)` with a spill is **intensional** — the reference *carries the intent*, and the engine realizes it at every calculation. A whole error class (stale ranges, fill-down drift) disappears not because users got more careful but because the reference itself stopped being able to go stale.

Gneiss has now made this same move three times independently, which qualifies it as a load-bearing pattern worth naming:

> **Intension over extension, realized by the engine, revealed on demand.**

| Extensional (frozen realization) | Intensional (captured intent) |
|---|---|
| `A1:A17` | `FILTER(...)` + spill |
| 100k per-item agent verdicts | one standing policy ([26-DECIDERS.md](26-DECIDERS.md)) |
| maintained current-state table | belief view under a context |
| traceability matrix updated by hand | realizes-edges + computed alignment (this document) |

A belief view is a spilled range over the ledger. The Language ([05-CODD-PROGRAM.md](05-CODD-PROGRAM.md)) is dynamic arrays for knowledge: references that express scope-intent under a context, realized at evaluation. And the robustness claim inherits Excel's evidence: the gain comes from the reference *carrying the intent*, so realization cannot silently detach from it.

Survey 18 (I5) upheld the analogy and marked its three honest breaks — Excel's intension has **no time** (recalc annihilates the previous extension; Gneiss keeps every realization), **no provenance** (a spilled cell cannot answer `why()`), and **no identity/envelope/grade** on the intent itself (the formula is anonymous). Which under-sells Gneiss usefully: we are dynamic arrays *plus* history, justification, and attribution. One concrete steal: **`#SPILL!` semantics** — when an intensional realization collides with manually-owned extensional space, Excel refuses loudly with a named error rather than overwriting or silently truncating. Gneiss needs exactly this defined, named collision behavior where rule-derived values meet direct testimony — a `spill-collision` signal in the conflict machinery, never a silent precedence win.

## 2. Where Gneiss already captures intent (the inventory)

Naming what exists before adding anything: versioned **rules** (derivation intent), **report contracts** (what this report means, declared), **consumer contracts** (dependency intent, [27-EVOLUTION.md](27-EVOLUTION.md)), **standing policies** (adjudication intent, compressed intensionally), **evaluation contexts** (interpretation intent — a context *is* a declared intent about how to read the world), **retention policies**, **envelope declarations** ([29-ENVELOPE.md](29-ENVELOPE.md) — engineering intent with drift-defeat), and the **future stances** ([33-FUTURE-TENSE.md](33-FUTURE-TENSE.md)): plans, expectations, obligations, intentions. Every one is declared, versioned, ledgered data.

What is missing is the *general* form: a first-class **Intent stance** and an explicit **realizes** relation, with alignment computed rather than assumed.

## 3. The Intent stance

- **Intent** = an entity plus declaring assertions: the desired condition over the world-model (stated against predicates, so it is checkable), scope, horizon, issuing authority, and optionally `refines(parent-intent)` — intents form hierarchies (§6).
- **realizes(X, I)** = an edge asserting that artifact/configuration/plan/action X exists in service of intent I. Asserted by humans or agents at creation ("this dashboard realizes the safe-storage-monitoring intent"), or derived (this delivery plan realizes contract C because the scheduler cited C). Realization edges are the *upward* sibling of justification edges: justifications ground values in evidence; realizations ground artifacts in purposes.
- **Alignment** = a belief view, not a checkbox: compare the declared condition against current belief → `aligned / drifting / violated / unknown`. Graded like everything else, and **I8 applies with teeth**: you may not claim `aligned` without coverage of the condition — "we believe the temperature is in bounds" requires the sensor channel to be live, not merely silent.
- **Drift is a typed signal** minting review items through the ordinary monitor machinery. An intent about *now* behaves as a maintenance obligation; an intent about the *future* decomposes into plans and expectations. Position: the future stances keep their distinct logics (33 §2's reasons stand) but gain a common parent — they are intent expressed at different horizons and normativity levels.

## 4. Spec beside status (the pattern that shipped)

The one intent-reconciliation loop running at planetary scale is Kubernetes' spec/status split: every object carries its declared desired state (`spec`, human/agent-written) beside its observed state (`status`, computed, never hand-edited), with controllers reconciling and drift first-class. Pending survey confirmation (claim I4), this is the presentation-and-schema pattern for intent entities:

> Every intent's page ([34-PRESENTATION.md](34-PRESENTATION.md) G12) shows **declared** beside **computed** — the condition as stated, the belief as evaluated, the alignment verdict with its grade and its `why()`. Status is a belief view; editing it is unsayable — you edit the world or the intent, never the verdict. Human judgments about alignment enter as *testimony about alignment* with a source envelope — never as edits to the computed view.

Survey 18 (I4) confirmed the pattern and delivered a convergence worth framing: Kubernetes' **`status.observedGeneration`** — status declares which spec version it evaluated, so staleness is detectable — *is* Gneiss's answer-label (context version + evidence high-water mark), independently evolved and proven at planetary scale. Four guards from k8s pathologies, all adopted: **fighting controllers** → per-facet authority over who computes an alignment status (the actor envelope gives server-side-apply-style field ownership naturally); **flapping** → hysteresis on drift-grade transitions; **orphaned intent** → alignment defaults to *unknown-by-absence-of-watcher*, never silently green — every intent needs a declared reconciler or an explicit `unwatched` grade (our own coverage doctrine, I8, applied to intents themselves); **stale status trusted** → label checks non-optional in presentation. Also adopted: `conditions[]` (reason/message/lastTransitionTime) as the standard shape for alignment beliefs, and plan-before-apply ("what would change under this intent version") as a preview query. GitOps' Synced/OutOfSync dashboarding shows drift-as-first-class works commercially — while remaining deploy-time-versioned folk Gneiss (survey 12's lesson again).

## 5. Construction takes the witness stand

The Intentional Programming connection proper. IP's deep commitment — keep the *intent* (domain-level description) as the durable artifact, with identities not names, projections not syntax, and realizations generated — rhymes with the two-plane model applied to *system construction*. But (claim I3, pending survey): the workbench projected the intent; what it lacked, and what every MDA-era failure shares, is a persistent, inspectable, **drift-detecting record of whether the realization still serves the intent**. Models rotted because the realizes-relation lived in nobody's ledger; the code became the truth by default.

Survey 18 (I3) confirmed the historical claim — with one credit owed. IP had versioned intent and generative transformations but no drift concept, because in IP's cosmology drift was *definitionally impossible*: the realization was always freshly regenerated. That assumption broke at every boundary the workbench didn't own. The credit: Capgemini's Pension Workbench had **live FIT-style test tables red/greening beside the formulas as actuaries edited** — a real, in-editor intent-vs-realization alignment check. But it was ephemeral, in-tool, untimestamped: **a reveal without a ledger** — the survey's phrase, and the crispest possible statement of what Gneiss adds. (Steal the UI: realizes-status rendered as live example tables at the reveal layer.) The survey's broadest verdict deserves quoting too: *the realizes-relation as a persistent, drift-detecting, queryable record does not exist assembled anywhere* — IP assumed drift away, MDA died of untracked drift, DOORS records the relation but nothing executes it, Kubernetes shipped the loop over a last-writer-wins intent store. Every tradition built one piece; the assembled organ is novel.

Gneiss applies its own medicine to its own construction: dashboards, reports, pipelines, schemas, and configurations are view-plane artifacts carrying realizes-edges to ledgered intents. "Why does this exist, and does it still serve its purpose?" becomes a query. Which suggests a candidate eighth witness-stand question — offered by ceremony, not decreed:

> **Q8 (candidate) — "What for?"** Every artifact, rule, report, and standing configuration can exhibit the intent it realizes and its current alignment status.

Arguments for: it closes the loop Govert named (operational modeling, reasoning, and state are explicit and derivable — construction's purpose should be too); it is cheap at A0 (a ticket link on a config row is a folk realizes-edge). Argument against: witness-stand scope creep — Q1–Q7 interrogate *claims*; Q8 interrogates *artifacts*, a different target. Parked as **D26**.

## 6. Multi-scale intent: the three examples worked

Govert's examples span the scales, and the machinery composes without addition:

- **"We will build this new silo next year"** — strategic intent; refines into milestones (expectations with deadlines); procurement actions carry realizes-edges to it; alignment = milestone verdict roll-up. Plan-vs-actual is the context diff already noted in 33 §8.
- **"Temperature is rising and might go out of bounds tomorrow"** — a maintenance intent (`keep temp within bounds`, an obligation of maintenance type) whose *status* incorporates a forecast: alignment today is `aligned`, projected alignment tomorrow is `presumably_violated` — the RV-LTL verdicts applied to intent status, which is exactly what an operator's attention should be sorted by.
- **"Grain deliveries to the mill over three weeks per contract"** — the contract is an intent; the delivery schedule is a plan realizing it; each delivery is an expectation with a deadline and closure channel; variance rolls up from missed-delivery verdicts to contract-alignment status. The mill's contract page shows spec (contracted schedule) beside status (deliveries believed made, graded).

Hierarchy mechanics: `refines` edges, per-level alignment views, variance roll-up as derived assertions — entities and edges, no kernel change (the streak continues).

## 7. Reveal-place ≈ alignment-place (Govert's conjecture, adopted as design rule)

The reveal pane ([34-PRESENTATION.md](34-PRESENTATION.md) G3) is where a human judges *both* "is the machinery right?" and "is this what we meant?" — and the second judgment needs the intent in view. Design rule:

> **Every reveal pane answers both "how?" (derivation, down the justification cone) and "what for?" (intent, up the realization edge) — because alignment judgment requires mechanism and purpose in one field of view.**

Consequence for the edit taxonomy (G4 gains a row): *"mechanism correct, intent violated"* — the value is derived correctly by the rule, but the rule no longer serves the declared intent (or the intent itself is stale). The edit routes as a challenge to the rule↔intent linkage or a proposal to revise the intent — not as a data correction. This is a genuinely distinct kind of wrongness that today gets mis-filed as either a data bug or a shrug.

## 8. Risks and the A0 form

- **Intent capture rots** exactly like consumer contracts (27 §5) and traceability matrices: stale intents are worse than none — they veto changes nobody wants and bless drift nobody notices. Mitigations rhyme: derive realizes-edges from observed behavior where possible; expire unrefreshed intents; alignment views that evaluate to `unknown` for lack of coverage are a smell that the intent was never checkable to begin with (unstateable intents should be refused at declaration — if it can't be expressed against predicates, it's a wish, not an intent).
- **Don't build the IP cathedral.** IP's fate (pending survey detail) warns against all-or-nothing intent capture. The A0 form is deliberately humble: a `serves` column linking config rows to a ticket/contract; a one-line intent statement on every report definition; the habit of asking "what for?" in schema review. The rich stance is opt-in per ring, like everything else.
- **Intent ≠ workflow bureaucracy.** Alignment is computed, not committee'd; the ceremony budget stays where decisions are (D25's review-bandwidth economics applies to intent proposals too).

## 9. Agenda

- **D26** — Adopt the Intent stance + realizes relation + spec/status presentation; decide candidate Q8 ("What for?") by ceremony.
- **D27** — The "intension over extension" pattern (§1) as a named Language design principle (references carry intent; extensional freezing is a lint), plus `spill-collision` as a named conflict signal.

## 10. Survey 18: verdicts absorbed

**The headline (TL;DR 1): the assembled organ is novel.** No tradition shipped the realizes-relation as a persistent, drift-detecting, queryable record — each built one piece (IP: the intent store; MDA-where-it-worked: one-directional generation with hand-patching forbidden; DOORS/DO-178C: the links and suspect-flags, alive only where a consumer gates; k8s/GitOps: the reconciliation loop and ergonomics; Spec Kit era: the authoring habit). This joins R3+R5 in the prior-art matrix as Gneiss's demonstrated white space.

**Claim verdicts:** I1 fair (IP identities ↔ entities), with the **interop moat** warning — text/export projections must stay first-class or Gneiss reimplements the ecosystem and dies of it like the tree did. I2 corrected: projection ≈ views is *surface-plus, not deep* — IP projections are lossless and must all agree; Gneiss views are lossy, context-parameterized, and licensed to disagree; the deep echo is narrower and named: *edit-as-testify is IP's edit-the-projection channel*. I3 confirmed with the Pension Workbench credit ("a reveal without a ledger" — §5). I4 adopted with four guards (§4). I5 upheld with three breaks and the `#SPILL!` steal (§1).

**Market timing (survey §8):** the LLM-era specs-as-source wave (Spec Kit et al.) is MDA's round-trip failure in new clothes — worse, with a nondeterministic generator — and it has *rediscovered the intent artifact without rediscovering the reconciliation loop*. Gneiss's Intent stance is precisely the missing organ, at the moment everyone has started writing intent artifacts.

**Delivery lessons folded into §8's risks:** all-or-nothing substrates die (IP); trust-us opacity dies commercially (fifteen years, no public live system — something small must be live and citable early, which is what Track V / P1 already is); realizes-edges survive only when created in the act of work *and* consumed by machinery that fails loudly (DO-178C vs DOORS); edit-as-testify surfaces must feel like ordinary editing (the grammar-cells/projectional-ergonomics lesson) or experts route around them; and "intent" is already a diluted marketing word (intent-based networking) — Gneiss's claims stay operational: *an intent is an entity, a realizes-edge is a ledger row, drift is a computed grade with a label.* One existence proof worth keeping close: the Dutch Tax Administration's ALEF — tax law as executable, expert-authored controlled natural language in MPS — is statutes-as-ledgered-intent running at national scale. Full detail: [18-SURVEY-INTENTIONAL.md](18-SURVEY-INTENTIONAL.md).
