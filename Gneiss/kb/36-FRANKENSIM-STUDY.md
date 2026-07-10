# FrankenSim as a Realization Study: The Glimpse from the Future

*Added 2026-07-05. Govert shared the FrankenSim plan (pinned verbatim at [ref-frankensim-plan.txt](ref-frankensim-plan.txt); ~100KB, read in the original) — "not real, may or may not work, but a plan with ambition and scope… some of our ideas are there, because they are in the air." Three assignments: steal anything useful; treat FrankenSim as a **realization** of Gneiss (how would it be built with Gneiss as a blessed dependency?); and take in the fearlessness as tone-setting, as counterweight to the mined history.*

## 1. What it is

A blank-slate, memory-safe Rust "continuum" for computational geometry + physics + optimization + rendering on the Franken constellation (asupersync, FrankenSQLite, FrankenNumpy/Torch/Scipy/Pandas/Networkx), zero other runtime dependencies. Mission: "given a physics-based objective and constraints, synthesize the geometry that optimizes it — faster, more correctly, and more verifiably than any existing system." Structure: a ten-principle **Decalogue**, **Twelve Big Bets**, seven layers (SUBSTRATE → BEDROCK → MORPH → FLUX → ASCENT → LUMEN → HELM), a six-tier correctness **Gauntlet**, a phased roadmap with Gauntlet-enforced exit gates, and a **team-of-agents build methodology**. Its founding move: one typed algebra where "geometry, fields, operators, derivatives, error bounds, budgets, provenance, and cancellation are all first-class values that travel together" — `Certified<Field>` as the value that collapses the archipelago into a continuum.

## 2. The ideas in the air (convergence inventory)

The plan independently states, in its own vocabulary, a remarkable share of the Gneiss corpus:

| FrankenSim | Gneiss |
|---|---|
| P9 Provenance-complete: content-addressed artifacts, event-sourced ops log, `explain(artifact)` reconstructs the causal tree; "any study can be replayed, forked, or audited from the ledger alone" | The Gneiss Contract + `why()` — nearly verbatim |
| P2 Determinism as contract: bit-identical across runs and thread counts, replayable from seed + ledger entry | Determinism-to-the-bit (TeaTime lineage) |
| The **Five Explicits** — units, seeds, budgets, versions, capabilities "are never implicit, ever"; constellation lock hash in every op | The envelope + the label; definitions-version stamping |
| Forkable worlds: "a fork is a new branch of the op log sharing every artifact by hash" | What-if contexts (the Worlds lineage, again) |
| Certify-or-escalate; "ML proposes, certified numerics disposes" | Proposals-cheap / decisions-gated; admission policies; grade-constrained plans |
| "A wrong answer wearing a badge" as Sev-0 (certifying the certifiers, §13.2) | "Badges must be earned, never decorative"; drilling the drills |
| Structured errors carrying ranked candidate fixes; "a refusal that teaches is worth ten silent successes" | Presentation-layer steal, pending |
| `estimate(program)` dry runs; capability tokens with metered budgets; idempotency keys | Plan-before-apply; authority lattice + D25's metered decision bandwidth; idempotent append |
| Conformance as the inter-agent contract: "agents never negotiate with each other's internals — both negotiate with the contract" | The witness stand as RFC (D29), applied per-crate |
| "A six-month campaign is a database you query, not a directory you fear" | Gneiss's pitch, in domain dialect |

This is the "in the air" phenomenon Govert named, and it cuts both ways: it confirms the direction (a second mind under different constraints converged), and it warns that **the composite will be reinvented piecemeal, domain by domain, unless the general form exists to be depended on.**

## 3. The steal list

1. **Ambition tags [S]/[F]/[M] with the critical-path rule.** Solid / Frontier / Moonshot, where "nothing tagged [M] sits on the critical path." This is our position/counter/kill-gate discipline compressed into a three-character notation. **Adopt across the Gneiss corpus** — the Model paper's claims, the prototype program, the Language features all get tags; moonshots explicitly off the spine. (Candidate D33.)
2. **The Error Ledger / Time Ledger — quantitative grades.** FrankenSim composes *numeric* error budgets end-to-end and attributes them to sources ("how accurate is this drag number and where did the error come from is a query, not a research project"). Gneiss grades are ordinal (grounded/sealed/attested); the marriage is obvious and powerful: **grade = epistemic class + quantitative bound where the domain supplies one.** Our `why()` becomes attribution not just of lineage but of *uncertainty shares*. (Candidate D34; connects to the fluid-worlds fractional provenance.)
3. **Anytime-valid statistics (e-processes, conformal e-prediction).** The rigorous mathematics of "watch a stream and decide the moment it's decidable, immune to optional stopping." This is the *statistical* twin of our RV-verdict machinery (survey 16) — expectations and forecast-scoring monitors that peek continuously without invalidating themselves. A real upgrade path for [33-FUTURE-TENSE.md](33-FUTURE-TENSE.md)'s monitors and the method-skill ledger. (Candidate for the Language's monitor semantics, [F]-tagged.)
4. **Errors as guidance** — every failure a structured value with machine-readable diagnosis plus fixes *ranked by the cost model*. Goes straight into presentation conformance (the edit-as-testify surface should refuse the same way).
5. **Team-of-agents methodology**: one crate = one contract + executable conformance suite; IR as the integration language; **golden ledgers** (every merged feature lands with a replayable ledger of its acceptance run — sealed report runs as CI artifacts!); the Decalogue as tie-breaker; and the repo organized so "the maximum context an agent needs is one crate + its contracts + the IR spec — deliberately smaller than a frontier context window." That last line is the first *quantitative* design rule for agent-era modularity I've seen, and it belongs in our planning-workflow/beads phase verbatim.
6. **Phase gates as Gauntlet states, not dates.** "Each phase gate is a Gauntlet state, not a date" — exit criteria as conformance conditions. Our prototype program should adopt the phrasing and the practice.
7. **The self-aware risk register.** "Scope creep (this document is the evidence) — High." Honesty as a register entry. Also the tone.

## 4. The realization study: FrankenSim on Gneiss

The Design Ledger (§11.2) *is* a domain-specific Gneiss, hand-rolled: `artifacts` = content-addressed values; `ops` = transactions with envelopes (the Five Explicits); `edges` = justification edges; `metrics` = series bindings; forks = contexts; `explain()` = `why()`; `at(t)` = as-of. **Even 2026's most fearless greenfield plan builds its own ledger/provenance/fork/explain machinery on raw SQLite — the white-space verdict of survey 18, now confirmed from the future as well as the past.**

If Gneiss were a blessed dependency, `fs-ledger` becomes a thin binding and FrankenSim inherits organs its plan currently lacks:

- **Bitemporality and restatement.** The plan has transaction time only. But design campaigns have world-time too: G2 acceptance envelopes get *revised* ("the published Ghia et al. digitization was wrong"), client-facing Pareto reports need as-reported vs as-restated, and "what did we tell the client in week 3 vs what do we now believe about that design" is a question its schema cannot ask. Gneiss asks it natively.
- **Defeasibility and the blast radius.** §13.2's nightmare — a certificate that can be fooled is "a wrong answer wearing a badge," Sev-0 — has no *remediation machinery* in the plan. When a certifier is found fooled, which published results carried its badge? That is source-invalidation plus the defeat cone: one decision quarantines the certifier-method for a period, and every artifact in its justification cone flips to `suspect`, queryably. Gneiss turns their worst incident class from forensic archaeology into a query.
- **Named contexts beyond forks.** Forks are branches; Gneiss adds *interpretation* contexts — the same op log read under audit vs current vs what-if-with-new-cost-models — plus Worlds-grade promotion semantics (which the plan's forks leave unspecified).
- **Recorded forgetting.** "A six-month campaign is a database you query" — and a six-*year* campaign is a database you must prune. The plan has no retention story; Gneiss's seal → purge → receipt, with monotone degradation, is exactly what a content-addressed, dedup-forever store needs before it meets reality.
- **Intent and realizes-edges.** Studies have objectives, but the *client's evolving intent* (the thing "Pareto-31" was chosen against, which changed twice during the campaign) lives outside the system. Realizes-edges from studies to ledgered intent versions would make "why does this study exist and does it still serve the brief" a query — and their semantic design-diffs already build the presentation layer for it.
- **What flows back.** Gneiss takes the quantitative Error-Ledger organ, e-processes, the tags, and capability-token budget metering as the shipped form of D25.

The exercise also calibrates the scope onion: FrankenSim-on-Gneiss is an **A2 embedding** (a library inside HELM), not a platform relationship — which is exactly the posture the Codd program predicts for the ecosystem layer: Gneiss succeeds when a FrankenSim-shaped system *chooses* it over hand-rolling `fs-ledger`, because it's less code and more organs.

## 5. The tone lesson (the actual assignment)

Set against the mine, the contrast is precise. NLS, PIE, STEPS, Agenda died where implementation labor and adoption economics met: too expensive to build fully, too expensive to learn, too expensive to maintain. FrankenSim's fearlessness is not recklessness — it is the recognition that **the implementation-labor constraint has collapsed**, and the plan is engineered for that world: sized for agent swarms, integrated by contracts rather than negotiation, gated by conformance rather than dates, with ambition *tagged and quarantined* so moonshots can't sink the spine. The discipline formula worth stating:

> **Dream in the plan; gate in the build.** Ambition is free when every [M] is flagged, tested by its own tribunal, and off the critical path. Fear is only rational about the spine — so keep the spine [S] and let the edges blaze.

Two implications for Gneiss's own approach. First, the binding constraint has moved from *can we build it* to *can we specify and verify it* — and specification, conformance, provenance, and revision are precisely Gneiss's subject matter. The agent era doesn't just enable Gneiss; it **selects for** it. Second, FrankenSim bypasses the graveyard's adoption economics by being *its own first user* (agents build it, agents use it, golden ledgers land with every merge) — the same move as our Gneiss-on-Gneiss dogfood (D28), independently arrived at. The near-misses died waiting for users to pay capture costs; the new generation pays its own costs with machine labor and ships the discipline as contracts.

Candidate agenda items: **D33** adopt [S]/[F]/[M] tags corpus-wide with the critical-path rule; **D34** quantitative grades (Error-Ledger marriage); **D35** e-processes/conformal e-prediction as the [F] statistics of monitor stances and method-skill scoring.
