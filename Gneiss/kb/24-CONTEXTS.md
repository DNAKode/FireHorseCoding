# Evaluation Contexts, Missingness, and Report Contracts

*Develops seed §7, §13, §14, §19.8. The context is Gneiss's most novel component — the prior-art matrix found no system that ships it — so this document is deliberately the most concrete.*

## 1. A context is a named, versioned entity in the ledger

The fixed point applies to contexts themselves: a context is an entity, its parameters are assertions, its versions are pinned by transaction id, and changing a context is appending a new version. Consequences:

- Report runs cite `(context version, ledger high-water tx)` — fully reproducible coordinates.
- Context changes are auditable and reviewable (the Foundry proposals pattern: semantics change by pull request, not by someone editing a WHERE clause).
- A context version can never be edited into meaning something else retroactively; determinism of past report runs survives.

## 2. Anatomy

```text
Context v(N):
  data_cutoff            tx id | 'latest'      -- which evidence is visible
  definition_cutoff      tx id | 'latest'      -- which ontology/rules/report defs apply
  source_precedence      → policy version       -- per-predicate source ordering
  conflict_policy        → policy version       -- strainer pipeline + stop rung (see 22 §4)
  closure_policy         → policy version       -- which (scope, predicate) pairs are locally closed
  missingness_rendering  → policy version       -- how typed missingness surfaces in outputs
  admission_policy       → policy version       -- hypothesis admission (decided-only vs threshold)
  restatement_policy     allowed | forbidden | diff-only
  redaction_handling     render-as-missing | fail-closed
  valid_time_frame       optional site-calendar binding (timezone, day boundaries)
```

Every `→ policy version` is itself ledger data. The AGM survey's one usable sentence — *"the preference ordering is the revision operator; store it as versioned data"* — is this table.

## 3. The canonical context set

Small, named, memorable — semantic debt control ([32-RISKS.md](32-RISKS.md)) starts by resisting context proliferation:

| Name | Pins (per the 2×2 in [21-TIME.md](21-TIME.md)) | Use |
|---|---|---|
| `CurrentOperational` | data: latest, defs: latest, restatement allowed | dashboards, apps, agents — the default |
| `AuditAsOf(t)` | data: ≤t, defs: ≤t, restatement forbidden | "what did we say then", disputes, compliance |
| `Backtest(t)` | data: ≤t, defs: latest | matcher/rule evaluation, ML point-in-time sets |
| `Restated(period)` | data: latest, defs: latest, valid-time window = period | corrected history for management reporting |
| `WhatIf(x)` | any cell + explicit overrides (hypothetical rules, sources, decisions) | simulation, migration rehearsal |

Restatement *reports* are diffs between cells (`Restated` vs `AuditAsOf`), each difference carrying its justification ("changed because C1 retracted A1"). Accounting language labels the columns: **as reported** / **as restated** / **why**.

## 4. Missingness: typed absence (seed §13 made concrete)

Belief views never return bare NULL for semantic absence. The taxonomy, upgraded with the HL7 nullFlavor lessons from the KR survey:

| Kind | Meaning | Licenses "no"? |
|---|---|---|
| `unknown` | open-world default: no assertion, no closure | no |
| `not_asked` | nobody/nothing ever attempted observation (HL7 NASK) | no |
| `not_observed` | observation attempted, no value obtained (HL7 ASKU) | no |
| `not_applicable` | predicate meaningless for this subject | n/a |
| `not_configured` | provider/source not set up for this subject | no |
| `not_yet_introduced` | predicate absent from ontology at the pinned definition cutoff | no |
| `absent_closed` | no assertion **and** a closure declaration covers this scope — evidence of absence | **yes** |
| `rejected` | claim existed; defeated by an explicit decision | contextual |
| `retracted` | claim existed; retracted | contextual |
| `contested` | conflicting claims; policy declined to pick (WFS-style undefined) → review queue | no |
| `redacted` | value destroyed for legal reasons; structure remains | no |
| `defaulted` / `backfilled` | value synthesized by declared rule; justification cites the rule | flagged |

The split that earns its keep (per decades of clinical practice): *never-asked* vs *asked-and-unknown*. And the only absence that licenses a confident "no" is `absent_closed`.

**Closure declarations are themselves defeasible assertions** — the watermark pattern from the KR survey: `complete(scope: Competition C registrations, source: Smoothcomp, through: 2026-06-30)`, with its own source, method, and revocability. New information can *defeat a completeness claim*, which automatically reopens `absent_closed` answers to `unknown` in recomputed views. That is the local-closed-world literature (Etzioni's LCW-survival-under-update) landing as two rules in the belief engine, and it is the correct semantics for "the scraper finished this event" in Smoothscrape.

## 5. Report contracts

A report definition (a versioned document in the ledger) declares:

```text
Report: DailySiloMass v7
  query:            massEstimate per silo, valid-time window = report day
  context_binding:  pinned → Context version K
                    | floating → named context, resolved at run
  missingness:      render policy (e.g., not_yet_introduced → "n/a (pre-2025)";
                    contested → footnote + review link)
  restatement:      show-diff | current-only | as-reported-only
```

**Report compilation** (seed §19.8) checks against the bound context *before* running: every referenced predicate exists at the definition cutoff; behavior before predicate introduction is declared, not defaulted; backfill rules exist where the report claims them; meaning-changed predicates (definition superseded between pinned versions) are flagged. Pinned vs floating is exactly the seed's distinction, now with teeth: a floating report must declare missingness and restatement behavior or it does not compile.

**Report runs** are recorded as entities: `(report def version, context version, ledger high-water tx, output hash)`. This buys: byte-for-byte reproducibility; staleness detection (a run is stale iff a later tx defeats or adds anything its trace consulted — the verifying-trace mechanism from the incremental survey); and the industry survey's "cheap 80% fix" — stamping outputs with the definition-version hash — done properly.

## 6. Sequenced/current/nonsequenced (adopting Snodgrass verbatim)

The temporal survey insists — correctly — that this vocabulary is settled. Gneiss query surfaces should name their shape:

- **current**: value now, per context (`CurrentSiloState`)
- **sequenced**: value as a function of valid time over a window (fill-level history)
- **nonsequenced**: questions about the assertions themselves (when was this corrected? how often did shape models change?)

Providers ([23-STORAGE.md](23-STORAGE.md)) expose `value_at` (current/point), `history_between` (sequenced), and ledger queries cover nonsequenced.

## 7. The agent interface (the modern motivation, made concrete)

Contexts are what make LLM/agent integration governable:

- **Agents read** belief views under an explicit named context — never raw ledger, never raw tables. The retrieved claims carry status, confidence, source, and `why()` handles; "current best belief, with provenance" is a dramatically better grounding surface than a vector store over documents.
- **Agents write** only evidence and hypotheses (status `proposed`, with support edges and method = the agent+model version), and at most *propose* decisions. The three-band triage from the MDM survey applies unchanged: high-confidence auto-admit **only** if the context's admission policy says so, and auto-admitted beliefs are badged everywhere.
- **Agent answers cite** `(context version, high-water tx)` — an agent's claim is re-derivable, which converts "the AI said so" into "the AI computed this view, and here is the fold."
- A `WhatIf` context is a safe sandbox for agent-driven exploration (hypothetical rules or links) with zero risk to operational belief.

This section is arguably the strongest commercial motivation for Gneiss and did not exist in the seed document; it should be a first-class agenda item in [40-DISCUSSION-AGENDA.md](40-DISCUSSION-AGENDA.md).

## 8. UX language (Risk 5, retired)

Every surface that shows a belief shows its stance, in accounting-adjacent words operators already trust: *current best belief* · *as reported at the time* · *restated* · *suggested, awaiting confirmation* · *rejected* · *contested — needs review* · *complete through [date]* (closure) · *n/a before [ontology date]*. The glossary table in [02-GLOSSARY.md](02-GLOSSARY.md) is the binding reference.
