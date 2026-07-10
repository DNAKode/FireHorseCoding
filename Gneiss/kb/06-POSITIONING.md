# Positioning: Bedrock Under Structured Thinking

*Added 2026-07-05 from discussion. Two things Govert said, one framing and one steer, both recorded as governing.*

## 1. The division of labor (the world-model complement)

LeCun-style world models know things about the world and know how the world works — they predict future state from state + action. They are stochastic, powerful, and unaccountable by construction. Gneiss is deliberately the other thing:

> Gneiss is the database / knowledge base / precise ontology / metadata-to-data-pointer layer — flexibly but durably and **non-stochastically** recording both the drivers (data, sources, inputs) and the meaningful inference results (strengthenable, weakenable) that feed further thinking or action. Our "database" is built knowing that facts need provenance and are contingent, deductions are imperfect and retractable, truth is tenuous and temporal, and storage and inference systems are fallible.

(Near-verbatim Govert; kept as the working positioning statement.)

The division of labor, sharpened: **models imagine; agents decide and act; Gneiss remembers, grounds, schedules, and holds accountable.** Predictive models plug in as *methods* — their claims enter as forecasts, get outranked by arriving observations, and get scored into skill records that feed source precedence ([33-FUTURE-TENSE.md](33-FUTURE-TENSE.md) §3). The stochastic components can be as wild as they like precisely because the substrate beneath them is not: non-stochasticity is not a limitation of Gneiss, it is the *point* of Gneiss — the fixed point under everything that isn't fixed.

"Bedrock underlying structured thinking" is not too floral once cashed out operationally: the inputs and outputs of thinking are recorded with provenance; the thinking itself (human, model, rule) stays outside, named as method, ranked by earned reliability, and answerable to the witness stand.

## 2. The steer: theory is bedrock, not product

Recorded as a governing correction to how the Codd program ([05-CODD-PROGRAM.md](05-CODD-PROGRAM.md)) is read:

> We started with operational systems and were drifting to theory. The Model, belief algebra, theorems, etc. are valuable **not as the focus of the system** — we are *not* building "an elegant knowledge representation and belief management system" — but as the bedrock **under** 'the system' that agents (human and ML) interact with.

Concrete implications, so this is a policy and not a mood:

1. **The value track stays first.** P-1 / P1 in a real system remain the leading commitment; theory artifacts (Model paper, Lean spikes) are side-bets that must never block operational progress.
2. **The Language exists to hide the theory.** Its success metric: operators and agents who never learned the words "stratified fold" or "monotone degradation" still get corrections-without-deletion, restatement-with-reasons, decisions-that-survive-rebuilds, and honest missingness. (The Codd analogy, properly read, says the same: nobody who used Oracle read Codd 1970.)
3. **Theory admission rule:** a theorem, logic, or formalism enters the corpus only attached to an operational payoff it licenses (the optimizer, the drills, the monitor semantics). Survey briefs are written accordingly ("ruthlessly practical; what pays rent").
4. **The future-tense work is the exhibit**: modal/temporal/deontic logic gets surveyed *because* due-lists, watchdogs, and explainable alarms are operational objects AIMS-shaped systems need ([33-FUTURE-TENSE.md](33-FUTURE-TENSE.md) §6) — not because belief logics are interesting, though they are.

## 3. Research axes register (updated 2026-07-05)

Standing axes: temporal/bitemporal data systems (10) · KR/belief/provenance theory (11) · industry platforms & operational patterns (12) · incremental computation (13) · modal & temporal logics of belief, BDI, deontic logic, justification logic, runtime verification (16 — landed 2026-07-05; absorbed into 33) · wiki principles & the machinery-reveal tradition (17 — landed 2026-07-05; absorbed into 34) · intentional programming & intent-capture traditions (18 — landed 2026-07-05; absorbed into 35) · **the history mine (19A augmentation / 19B PARC–VPRI / 19C near-misses — landed 2026-07-05; synthesized in 19-MINE-SYNTHESIS)** · material-flow & traceability standards (15 — queued behind probe P-1c).

Lineage statement, upgraded per the mine: Gneiss mechanizes Section III of Engelbart's 1962 framework (antecedent links, kernel-level provenance, criteria-parameterized views) with the semantics he left informal, over the Journal's append-only substrate, claiming the lot Licklider staked in 1968 — the *modeling* function, not the switching function.

Each axis answers to the admission rule in §2.3.
