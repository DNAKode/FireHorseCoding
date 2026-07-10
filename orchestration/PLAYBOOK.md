# The Orchestration Playbook

**What this is:** the agent-orchestration methods discovered while building the M1 increment
(2026-07-10/11), recorded at steward direction. This is **process knowledge — how Fable-class
orchestrators direct agent fleets to build software correctly**. It may grow into part of a larger
service.

**What this is not:** KodePorter. KodePorter is the explicit representation of a system mapping —
the map, its typed imperfections, and the operations that keep it honest. Orchestrators (this
playbook included) are *consumers* of that representation, never components of it. The boundary
rule when a lesson could go either way: **if the lesson is about the domain, it becomes schema in
the product; if it is about directing labor, it lives here.** (Worked example: "adversarial
verification catches implementer blind spots" is labor direction — it lives here; "verification
evidence has an independence property" is domain — it became a typed field on VerificationRuns.)

Evidence base: one increment — 4 workflows, 13 agents, ~2.2M subagent tokens, ~10 kLOC + tests +
artifacts in ~a day; 5 confirmed major defects found by verification under 68 green tests; zero
agent-level failures. One data point, honestly labeled as such. Revise per drive.

---

## 1. Contracts before agents

Write one `CONTRACT.md` per component, at full orchestrator effort, before any implementation
agent launches. A good contract carries: exact schemas and API shapes; semantics with pointers to
the governing spec; determinism rules; the required test list; explicit non-goals; and the
divergence protocol (deviations tagged `// DIVERGENCE:` in code AND reported in the return).

**Observed:** three parallel mid-tier agents built largely correct components from contracts
alone. Every major integration defect traced to the *contracts* (a subject-scheme design bug, a
provider-name mismatch between two contracts, an ambiguity inherited from the upstream spec) —
none to agent negligence. **When implementation labor is cheap and reliable, the specification
becomes the dominant defect source. Invest at the top.**

## 2. Cross-contract consistency pass

The seams live *between* contracts. Before an implementation wave, one explicit pass (orchestrator
or a dedicated agent) checks the joints: shared formats named identically on both sides, field
casing, example values that two documents state differently. Every WF2 integration failure in the
M1 drive was an inter-contract joint (provider string, JSONL field casing, harness cwd assumption).

## 3. Directory ownership and pre-decoupling

Each parallel agent owns disjoint directories and is told so explicitly. Break shared build edges
*before* the wave (we removed a project reference so two agents could not observe each other's
half-built code, and restored it at integration). Result: zero merge conflicts, zero cross-talk
across 13 agents in a shared working tree — no worktree isolation needed.

## 4. Cost-tiering

Mid-tier (Sonnet-class) agents for all implementation against contracts; raise per-agent effort
only for semantically delicate components (the belief fold, the flagship visual). Orchestrator-tier
attention is reserved for: contracts, integration gates, finding triage, fix design, and review.
The steward's minutes are the binding budget; spend model capability where semantics are subtle,
not uniformly.

## 5. Honesty framing in every brief

Every agent brief includes, verbatim-ish: *"failing tests you cannot fix go in blockers — never
weaken a test or golden; test-laundering is the project's cardinal sin"* and *"report deviations
as findings, don't silently patch."* Observed effect: agents self-reported their own divergences
and environment traps accurately (the fixture agent surfaced two non-obvious gotchas unprompted:
`git apply` silently no-oping in nested repos, and a root `.gitignore` swallowing a deliverable).
The divergences field of the structured return is consistently where the highest-value information
lives — read it first.

## 6. Structured returns, schema-forced

Force every agent's final answer through a schema: `{summary, filesCreated, commandsRun,
testsPassed, testsFailed, divergences, blockers}`. Verification agents return typed findings with
severities and mandatory concrete failure scenarios. Free-text reports hide gaps; schemas make
"blockers: []" a claim the agent must own.

## 7. The build is the oracle — re-verify everything locally

Never accept "all green" from a report: re-run the builds and suites yourself at every gate. IDE
diagnostics are unreliable in both directions (they showed phantom errors on green code twice, and
real errors once). Agent reports were honest in this drive — and were still re-verified, because
the cost is seconds and the failure mode is silent.

## 8. Story-first integration

Before any component is "done," the orchestrator writes and runs a single end-to-end **story
script** — the marquee demo — against ground truth. In M1 the story caught two model-level defects
that 68 green unit tests missed (claim-key collision across sibling claims; predicate-conflated
status display). The story script doubles as the UX audit: every sharp edge in the CLI surfaced
while writing it.

## 9. Adversarial verification as a standing gate

After integration, launch independent skeptic agents with a **refute-don't-certify** brief and a
hard rule: findings require a *live probe* (construct the failing input and run it against the
compiled artifact — static reading is not evidence). Give each verifier a distinct lens (spec
conformance, artifact integrity/XSS, determinism drills, evidence-chain honesty). M1 yield: five
confirmed majors including a kernel-semantics blocker sitting under a fully green suite, at ~530k
tokens — the cheapest defect-finding per token in the whole drive.

Corollary: **implementer-authored tests inherit the implementer's blind spots.** Spec-derived
tests should be authored by a different agent than the implementation.

## 10. Fix agents receive the algorithm, not the problem

For semantically delicate fixes, the orchestrator designs the exact fix (in M1: the grounded
pairwise conflict semantics, specified step by step with required test scenarios) and the agent
implements it. For mechanical fixes, the finding alone suffices. Match delegation depth to
semantic risk.

## 11. Checkpoint commits between waves

Commit after every wave (scaffold, foundation, integration, fixes). Clean bases make agent
recovery cheap, diffs reviewable, and attribution trivial.

## 12. Workflow shape

`parallel(foundation) → pipeline(integration) → parallel(adversarial verify) → parallel(fixes)`,
with barriers only where a stage genuinely needs all prior results. Run workflows in the
background; integrate and re-plan between them — the orchestrator stays in the loop at wave
boundaries, not inside waves.

## 13. Probe the environment before scaffolding

Check toolchains and versions first (`dotnet --list-sdks`, `cargo --version`, analyzer
availability). M1's probe caught a preview SDK silently retargeting projects to the wrong
framework — a one-line `global.json` fix at minute five instead of a mystery at hour five.

## 14. Land every gate with its own evidence

Every wave's exit ships a replayable artifact: the golden ledger, the auto-generated lab notebook,
the deterministic demo. "The demo and the conformance run are the same bytes" is as useful for
orchestration hygiene as it is for product showmanship — it makes progress claims checkable after
the fact.

---

## Standing tripwire (steward-set, 2026-07-11)

If the FireHorseCoding repo accumulates more orchestration prose than product schema, KodePorter
is dissolving into a methodology. This playbook is the designated container for method knowledge
precisely so the product surface never becomes one.
