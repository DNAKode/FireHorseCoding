# The Future Tense: Plans, Forecasts, Expectations, Obligations

*Added 2026-07-05 from discussion. Govert's question: can the future be represented in the Gneiss bedrock? Record-keeping is about the past (sensor installed → reading → derived value → alarm → action, at these times) but also the future ("this must happen by such time", "then we should see such-and-such, or otherwise we need to…"). Answer proposed here: **yes — the future needs a stance library and three small vocabulary additions, not a kernel change.** The demonstration is this document.*

## 1. The one asymmetry the model already encodes

Transaction time can never be in the future — a ledger cannot have learned something at a moment that has not happened. Valid time can. Therefore:

> **The future, in Gneiss, is the region of valid time beyond a context's evidence horizon.** Every future-directed claim is an ordinary assertion with (transaction time = now, valid time ahead). No new axis, no new plane.

Vocabulary: a claim is **retrodictive** when observation could already have reached its valid time, **prospective** when not. One new missingness kind covers the gap: `not_yet_observable` — the valid time queried lies beyond what any evidence could yet report (distinct from `unknown`: nobody *could* know yet). "What will fillLevel be on July 10?", asked July 5, honestly answers: *a forecast, plus `not_yet_observable` for observation-grade truth.*

## 2. The six future stances

All are assertions plus rules — no kernel machinery:

| Stance | What it claims | Discharged by | Defeated/violated by |
|---|---|---|---|
| **Planned fact** | the world will be arranged thus from T ("Sensor88 replaces Sensor42 on Sep 1") | arrival of confirming observation | supersession (plan changed), or contradicting observation on arrival |
| **Forecast** | a method predicts value v at future T, from state at tx N, with confidence | never "discharged" — *outranked* by arriving observation, then **scored** (§3) | being outranked is not defeat; being *wrong* is data |
| **Expectation** | evidence of kind K should arrive by deadline D | matching evidence arrives in window | **closure-licensed silence**: D passes *and* coverage of the evidence channel is closed through D (§4) |
| **Obligation** | actor A must do/ensure X by D, else consequence policy applies | evidence of fulfillment | breach: closure-licensed non-fulfillment past D → consequence rules fire (§5) |
| **Trigger (standing contingency)** | a declared rule: when condition (often an expectation violation), mint event/obligation | n/a — fires; each firing recorded with justification (rule version + licensing closure) | rule superseded by ceremony |
| **Intention/commitment** | actor A commits to future action; carries explicit *achieved-when / impossible-when / deadline / reconsider-when* clauses and a named commitment policy (blind / single-minded / open-minded) — survey 16's finding: BDI implementations kept exactly this lifecycle-as-data and dropped the logic | fulfillment evidence, or drop-condition firing | lifecycle decisions: dropped, revised, violated |

The stances are deliberately distinct because their *logics* differ — a forecast being wrong is normal science, an obligation being breached is a governance event, an expectation lapsing is an operational alarm. Keeping them as separate predicates with separate consequence rules is where modal/deontic vocabulary will help (survey 16); the kernel carries all three identically.

## 3. Arrival reconciliation and the forecast-skill ledger

When the future arrives, ordinary machinery absorbs it:

- **Precedence default: *accepted* observation outranks anticipation.** A new strainer rung, per predicate class — with the qualifier survey 16 insisted on (from meteorological data assimilation practice): observations pass a plausibility/quality gate before outranking; a lexical rung over ungated observations lets one bad sensor defeat a good model. Below the observation rung, competing forecasts for the same target order by issue time (recency-of-issue rung). The radar reading at July 10 wins over the July-5 forecast — the forecast is not retracted, merely outranked.
- **Outranked forecasts become scoreable.** (prediction, outcome) pairs are just belief-view queries; scoring rules (error, calibration, Brier-style) produce derived assertions *about the method-entity*. Invariant (survey 16, C2): **scoring reads at the forecast's transaction time** — grade the forecast *as issued*, never as later revised; proper scoring depends on it. Which closes a loop that should be named: **method skill feeds source precedence** — the same machinery that learned to rank a flaky sensor below a good one learns to rank forecasting models. Predictive models — LeCun-style world models included — plug in as *methods whose claims get graded by arrival, automatically, by the same fold that grades everything else.*
- **"What did we expect then vs what happened"** is not new reporting machinery — it is a cell pair of the existing 2×2 (data-then vs data-now), which means plan-versus-actual reporting, forecast audits, and model regression tracking are all context diffs.

## 4. Expectations: making "no news is bad news" rigorous

An expectation's violation is *absence of evidence*, and Gneiss already has the discipline for that: **I8 — negative conclusions require positive coverage.** The deadline alone licenses nothing. The violation derives only when:

```
violated(E) ⇐ deadline(E) < now
            ∧ no matching evidence in window
            ∧ closure(channel(E), through ≥ deadline(E))     -- the watermark
```

Without the closure assertion, the honest status is `presumably_violated` — survey 16's finding is that this is exactly runtime verification's four-valued verdict lattice (RV-LTL: true / false / presumably-true / presumably-false over a finite prefix with an open future), battle-tested in maintained monitoring tools, and we should adopt it nearly verbatim. The distinction is operational gold: a delivery-confirmation watchdog behaves differently when the EDI feed is known-current (`violated`) versus known-lagging (`presumably_violated`) — and escalation policies can key on the difference. Survey 16 also sharpened the theoretical placement: classical RV *assumes* a complete, ordered trace — its verdicts silently rest on trace-completeness — and **Gneiss's coverage watermark is that hidden assumption reified as data.** Two bonus imports: a **monitorability lint** for the Language (statically warn when a declared expectation can never reach a definitive verdict), and the guidance that *past-form rules* ("X happened and no Y since") are always verdict-yielding — steer rule authors toward them. A monitor pass is just a rule evaluation over (expectations × closures); every firing is recorded with its justification (rule version + the closure that licensed it), so alarms take the witness stand like everything else.

## 5. Obligations: the deontic stance

Structure: (obligor, requirement predicate, deadline, consequence policy ref, issuing authority) — **plus, per survey 16 (C3), the obligation typology as required schema fields**: kind = `achievement | maintenance | punctual` (they have *different violation computations* — deadline-absence vs any-lapse-in-interval vs point check; a single generic obligation record computes wrong verdicts for maintenance norms), `persistent_after_violation` (must you still do it late?), and `preemptive` (does doing it early count?). Norms themselves are assertions with valid/transaction time — the legal-temporality literature's force/efficacy/applicability distinctions map directly onto our bitemporality, including retroactive annulment. Fulfillment and breach follow §4's pattern. Two design notes:

- **Doxastic ≠ deontic, kept separate as stances.** "We believe X will happen" and "A must ensure X" share kernel representation and share nothing else; conflating them (the classic modeling sin) would poison both the belief engine and the governance story. Authority and delegation ([26-DECIDERS.md](26-DECIDERS.md)) already give obligations their issuing/enforcement half.
- **Escalation chains are contrary-to-duty structures.** "If calibration missed its deadline, then readings degrade to flagged-grade and a renewal obligation with a harder deadline is minted" — reparational obligations firing on recorded violations. Survey 16 (C3) confirmed this is the deontic literature's *own* resolution (Governatori's ⊗ reparation chains, Regorous, ODRL's remedy/consequence) — Chisholm's paradox is an argument against embedding norm logic, not against consequence chains. Two flagged cautions: reparation cascades need explicit termination (a violated reparation can mint another — cap the chain); and **weak permission is closure-dependent** — "permitted because no prohibition found" is an `absent_closed`-class inference and must declare its coverage like any other negative conclusion (I8 again). Vocabulary aligned to ODRL where free: duty / remedy / consequence.

## 6. The operational payoff (drift check honored)

None of this is theory for its own sake — these stances are the most operational objects in the whole corpus:

- **The due-list and the overdue-list are belief views** over obligation/expectation stances: maintenance schedules, calibration renewals, delivery watchdogs, compliance deadlines — AIMS-shaped work, currently done with ad hoc date columns and cron jobs everywhere.
- **Alarm logic becomes explainable**: an alarm cites its rule version, its licensing closure, and its evidence — `why(alarm)` for the operations manager.
- **Plan-versus-actual is a context diff**, with reasons attached to every variance.
- **The full loop is testimony**: evidence → belief → rule fires → obligation minted → action taken (recorded — the system as actor) → outcome observed → method graded. Gneiss does not predict and does not act; it *remembers, grounds, schedules, and holds accountable* — the non-stochastic substrate under the perception–prediction–action loop.

## 7. Vocabulary additions (the full bill)

1. Missingness: `not_yet_observable`.
2. Stance statuses for expectations/obligations, aligned to the RV-LTL verdict lattice (survey 16): `open / presumably_holding / discharged / presumably_violated / violated / withdrawn` — where the `presumably_*` values are the honest open-future/closure-lacking verdicts.
3. Precedence rungs: *accepted* observation > anticipation; among anticipations, recency-of-issue (per predicate class, policy data as always).
4. Obligation schema fields: kind (`achievement | maintenance | punctual`), `persistent_after_violation`, `preemptive`.

That is the entire cost. Everything else is stance libraries and rules.

## 8. Open questions / predicted frictions

- **Recurring obligations** (monthly calibration): recurrence as a rule minting instances vs materialized future instances — leaning rule-with-horizon; how far ahead to materialize is an envelope parameter ([29-ENVELOPE.md](29-ENVELOPE.md)).
- **Plan revisions and baselines**: superseding planned facts works, but project-style reporting wants named plan *baselines* — which are just pinned contexts over the plan predicates (earned-value analysis as context diff; satisfying, needs a worked example).
- **Probabilistic forecasts as distributions**, not point values — a value-representation question (document values with distribution payloads?), not a kernel question, but it needs a convention before P3-era providers.
- **Out-of-order arrival** (observation lands after downstream consequences fired on a violation): the ordinary staleness/recompute machinery handles the *beliefs*; whether fired *actions* get compensating obligations is a domain policy — name the pattern.
- **Survey 16 landed same day** ([16-SURVEY-LOGICS.md](16-SURVEY-LOGICS.md)); verdicts absorbed throughout this document. Summary: C1 sound with a polarity correction (violation is the safety-side verdict) and the four-valued lattice adopted; C2 sound with quality-gated observations, recency-of-issue rung, and the score-as-issued invariant; C3 sound *given* the obligation typology; **C4 over-claimed and downgraded** — justification logic supplies operator names (`apply`/`sum`/`check`) and the internalization invariant ("no acceptance without an exhibitable term"), while `why()`'s actual foundation is provenance semirings. That last point reconciles with survey 11's "skip semirings": 11 rejected *storing provenance polynomials* as engine machinery; 16 endorses semiring *semantics* as the formal account of why-combination (⊗ along derivations, ⊕ across alternative supports). Both stand. Two further imports parked for other docs: the **Moore-sentence caveat** for federation (an imported coverage claim can be falsified by the very batch that carries it — imports of closure assertions need precondition checks; belongs in [25-IMPERFECTION.md](25-IMPERFECTION.md) §5b's orbit) and the vocabulary alignments "perfect recall = append-only transaction time, synchrony = watermark, D-consistency = the per-view acceptance contract, factivity = the grade axis" (belongs in the Model paper's related-work section).
