# The Learning Loop — instructing, coordinating, and bookkeeping a port at minimal agent cost

*Guidance layer, seeded 2026-07-12 for the FrankenTui flagship (steward direction: "let's learn
how to port kode"). Orchestrator-neutral by intent; evidence so far is one orchestrator's. Every
rule here exists to answer one economic question: **how do you get correct porting work out of
the cheapest possible agents?** The one-sentence answer: move all intelligence that can be
amortized into the map and the templates, so the per-item work is bounded verification or
bounded transformation — the two things cheap models do reliably.*

## 1. INSTRUCT — bounded items, pre-cut context, versioned templates

1. **A work item is map-derived and self-contained.** Every instruction bundles: the item (one
   queue row — a candidate to confirm, an absence to classify, a unit to port), the pre-cut
   context (exact file paths + line ranges from the map's anchors, the applicable policy, the
   acceptance criteria), the output schema, and the honesty clause ("unsure is a valid verdict;
   fabricated evidence is the cardinal sin"). The worker never explores. **Exploration is paid
   once, by the map, and amortized over every worker that reads it** — this is the single
   biggest cost lever, because cheap models fail at open-ended exploration and succeed at
   bounded judgment.
2. **Templates are versioned artifacts, not ad-hoc prompts.** Each instruction template lives in
   the guidance layer with a name and version (`confirm-corr@1`, `classify-absence@1`,
   `port-leaf-fn@1`). The template id goes into the Gneiss method envelope of everything the
   worker produces. Templates therefore *compete on measured acceptance rates* — improving an
   instruction is a version bump whose effect is a query, not an anecdote.
3. **Tier by task shape, escalate by evidence.** Haiku-class for confirm/classify/extract and
   small mechanical translation; Sonnet-class for synthesis, repair, and anything touching
   semantics the map hasn't pinned. Escalation of an item class to a higher tier is a recorded
   decision justified by that class's measured failure rate — never a default.

## 2. COORDINATE — queues are the scheduler; artifacts are the only channel

1. **Queues, not plans.** The map's standing queries schedule the work: the candidate-review
   queue, the absence-unknown queue, the thin-dossier queue, the stale queue, the unverified
   queue. An **iteration** = pick one queue × one template × one tier, drain a bounded batch
   (10–50 items), measure, stop. No grand plan document can go stale if there is no grand plan
   document.
2. **Worker → independent checker, always, and never a conversation.** Each item flows through a
   worker and then a checker with a *different* template whose job is to refute (verify the
   worker's cited evidence exists and supports the verdict). Workers and checkers communicate
   only through recorded artifacts — proposals with evidence — never with each other. Checker
   independence is typed on the evidence (the M1.5 `independence` field). Pipeline per item (no
   barrier); sample-audit with a stronger model only when the checker tier equals the worker
   tier.
3. **Acceptance is policy, human minutes default to zero.** Worker-confirmed + checker-upheld +
   policy-allowed → accepted by the policy actor, on the record. Humans receive only what policy
   routes (contested items, sampled audits, template retirement decisions).
4. **The iteration gate.** After each batch: acceptance rate, overturn rate, cost per accepted
   item, health delta, test-baseline delta. The (queue, template, tier) triple is then reused,
   tuned (version bump), re-tiered, or retired — by its numbers.

## 3. BOOKKEEP — the ledger is the lab notebook; learning is data

1. **Nothing load-bearing lives in prompts or transcripts.** Every proposal, verdict, decision,
   and piece of evidence flows through `kp` into the workspace's Gneiss ledger, with the method
   envelope carrying `worker:<tier>|template:<name>@<ver>` and the actor naming the agent or
   policy. A dead orchestrator loses nothing but labor.
2. **The iteration record is a first-class assertion** (`kp.iteration`, subject
   `iteration:<n>`): queue, template@version, tier, items attempted / accepted / overturned /
   unsure, tokens and wall time (orchestrator-reported), health before/after, test-baseline
   delta. **"Which iterations improve and which regress" is a query over these rows** — the
   loop's memory is method-skill data in the ledger, not orchestrator recollection. Skilled
   (queue, template, tier) triples earn bigger batches; regressing ones are retired with a
   recorded reason.
3. **The Atlas gallery is the fitness chart.** Every iteration ends with an Atlas snapshot into
   the flagship's gallery; the sequence of snapshots is the port's learning curve, visible.
4. **Headline metric: tokens per accepted, independently-checked item** (secondary: human
   minutes, target 0; rework rate; cone precision when deltas land). If the number won't beat
   re-derivation-from-scratch, the map isn't earning — say so in the keep-earning memo, per the
   charter's own kill criteria.

## 4. The cost model, stated honestly

Expected shape (to be validated by the flagship's iteration records): map construction is a
fixed cost (~minutes of machine time + orchestrator setup); each amortized work item then costs
one cheap-model read-judge-report cycle (~10³–10⁴ tokens) instead of an expensive-model
explore-understand-act session (~10⁵–10⁶ tokens). The loop wins when item count × per-item
saving exceeds the map's fixed cost — which is exactly why flagships should be *ports with many
similar items*, and why the first queues drained are the high-count, low-judgment ones
(citation confirmation, absence classification) before the low-count, high-judgment ones
(synthesis of missing units).
