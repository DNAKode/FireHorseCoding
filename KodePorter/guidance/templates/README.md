# Template registry

*The versioned instruction templates of the learning loop (LEARNING-LOOP §1.2). A template is a
named, versioned artifact that competes on measured acceptance rates — improving one is a version
bump whose effect is a query over `kp.iteration` records, not an anecdote. The prompt bodies here
are the exact text dispatched to workers (parameterized with `{PLACEHOLDERS}`); the method envelope
of everything a worker produces carries `worker:<tier>|template:<name>@<ver>`.*

| Template | Queue | Tier | Status | Measured record |
|---|---|---|---|---|
| [confirm-corr@1](confirm-corr@1.md) | candidate-review | haiku | **proven** | it1: 12 items, 10 accepted, 2 refuted-and-upheld, 0 fabrication, ~102.5k tok/accepted (pair with checker) |
| [check-confirm@1](check-confirm@1.md) | (checker for confirm-corr) | haiku | **proven** | it1: 12 checks, 10 uphold-confirm, 2 uphold-refute, 0 overturn; both refutations substantiated |
| [classify-absence@1](classify-absence@1.md) | absence-unknown | haiku | **drafted, unproven** | never run — first batch MUST be a pilot (≤50 modules) with 100% orchestrator audit of a sample |
| check-absence@1 ([same file](classify-absence@1.md)) | (checker for classify-absence) | haiku | **drafted, unproven** | never run |
| [classify-divergence@1](classify-divergence@1.md) | divergence-leads | sonnet | **drafted, unproven** | never run — first batch is the two it1 refutations, 100% audited |

Rules that apply to every template (from LEARNING-LOOP §1–2):

- The honesty clause is part of the template, never optional: *"unsure is a valid verdict;
  fabricated evidence is the cardinal sin."*
- Workers never explore — the dispatcher pre-cuts context (exact file paths from the map's
  anchors). If a worker needs to explore, the work item was cut wrong.
- Output is schema-forced (the JSON schema in each template file). Free-text reports hide gaps.
- Workers and checkers communicate only through recorded artifacts, never with each other.
- Changing a prompt body = a version bump + a new file (`name@2.md`); the old file stays, because
  old iteration records reference it.
