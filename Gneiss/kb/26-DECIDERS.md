# Deciders, Authority, and Decision Volume

*Added 2026-07-04 from discussion. Govert's objection: the corpus keeps privileging "human" accept/reject. Increasingly humans, agents, ML models, and deterministic systems are peer inputs; there is no value in structurally distinguishing them — and the danger is the assumed economics: if decisions are agent-generated, their volume grows enormous, breaking "decisions are precious kilobytes of case law."*

## 1. Parity is already structural; the error was economic

The kernel never distinguished human from machine: a decision is an assertion whose transaction carries an actor and whose envelope carries a method. "Humanness" is a property of the actor-entity, not a kernel type. What the corpus *did* assume — wrongly, as charged — is that decisions are scarce because a human hand signs each one. That assumption is withdrawn. Two real dimensions replace the human/machine axis, both representable as ordinary policy data:

**Regenerability** — can this judgment be recomputed from retained inputs plus a retained method version?

| Decider | Regenerability |
|---|---|
| Deterministic policy (threshold rule, validation check) | fully — the decision is a *derived assertion* wearing a decision costume |
| ML model / LLM agent | mostly — given pinned weights/version + inputs (modulo sampling; pin seeds or accept approximate) |
| Human clerical judgment | no — the judging function is not stored anywhere |

**Authority** — where the decision ranks when decisions conflict. An authority lattice (per scope, per predicate class) is a versioned policy, generalizing source precedence: `site_engineer > agent_matcher_v4 > auto_threshold` for sensor assignments; something else for identity links. Overrule is ordinary supersession by a higher-authority decision; the belief-engine strainer rung "decision wins" becomes "**highest-authority effective decision wins**; ties break by transaction order."

## 2. Retention economics, corrected

[25-IMPERFECTION.md](25-IMPERFECTION.md) §7 said "decisions: kilobytes per year, never purge." Corrected: **retention priority follows regenerability, not the Decision stance**:

- **Non-regenerable adjudications** (human case-specific judgment, and any decision whose inputs/method are gone): permanent core. Still small — human attention doesn't scale, which is precisely why it stays precious.
- **Regenerable verdicts** (deterministic and model-generated at volume): *derived-grade* — purgeable after their accepted outcomes are sealed, exactly like other derived assertions. An agent's 100k accepts are not case law; they are computation output.
- The permanent core remains small **by this rule rather than by assumption**: what is kept forever is judgment that cannot be recomputed, plus the policies and authority grants under which everything else was decided.

## 3. The big lever: intensional compression (statutes, not verdicts)

The deepest answer to decision volume is not storage engineering but representation: **systematic judgment should enter the ledger as standing policy (a rule), not as per-item verdicts.** Law solved this: statutes govern classes of cases; individual rulings exist only where the statute underdetermined or was overridden.

- An agent that would accept 100,000 matches meeting criterion X should instead **propose one standing policy** ("auto-accept method M matches with confidence ≥ x in scope S"), whose adoption is a single decision by whatever authority may delegate that band. Acceptance of the 100k is then *derived* — recomputable, backtestable (the 2×2's backtest cell evaluates a proposed policy against history *before* adoption), explainable ("accepted under AutoAcceptPolicy v3" beats 100k opaque verdicts), and revocable in one act.
- Per-item decisions are reserved for **case-specific judgment**: overrides of the standing policy, and cases the policy routes to review.
- Design tripwire (add to [32-RISKS.md](32-RISKS.md)'s family): *an agent emitting per-item verdicts at machine rate, without case-specific reasons, is a misdesigned agent — it should be proposing a rule.* Review queues audit this: verdict-rate per method is an alarm metric.

This also keeps the three-band triage economics intact at any scale: bands are authority-relative, and the "expensive middle" is defined per decider class.

## 4. Delegation as data

Agent authority is granted, scoped, and expires — as assertions: `delegates(SiteEngineer, agent_matcher_v4, scope=sensor_assignments, band=[0.95,1.0], until=2026-12-31)`. Consequences fall out of existing machinery: expired delegation = decisions outside authority are ineffective in belief views computed under later policy (or flagged `contested`, per policy); revoking a delegation is a decision; sample-audit rates per authority band generalize the rubber-stamping risk to non-human deciders (a model whose audited error rate drifts gets its band narrowed — an assertion about the method-entity).

## 5. What changes where

- [22-BELIEF-ENGINE.md](22-BELIEF-ENGINE.md) §4 strainer: rung 1 reworded to authority-ranked decisions (amendment proposed, not yet applied).
- [24-CONTEXTS.md](24-CONTEXTS.md) §7 agent interface: unchanged in structure, but "decisions reserved for humans" becomes "decisions reserved for adequate authority; standing-policy proposals preferred over verdict streams."
- [25-IMPERFECTION.md](25-IMPERFECTION.md) §7 table: permanent-core row corrected (edit applied, pointing here).
- New agenda items D16, D17 ([40-DISCUSSION-AGENDA.md](40-DISCUSSION-AGENDA.md)).
