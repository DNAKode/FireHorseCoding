# classify-divergence@1

- **Queue:** divergence-leads (candidates whose refutation was upheld by an independent checker —
  the file cites its source but does not substantively port it).
- **Tier:** **sonnet-class** — this is judgment about intent and semantics the map has not pinned
  (LEARNING-LOOP §1.3 task-shape rule), not bounded verification. Low count justifies the tier.
- **Status:** drafted 2026-07-12, **never run**. First batch = the two it1 leads (`cand-hc-24`
  alpha_investing, `cand-hc-57` schedule_trace), 100% orchestrator-audited.
- **Work item fields:** `{ID}`, `{RUST_PATH}`, `{CSHARP_PATH}` (both READ-ONLY), `{PRIOR_RECORD}`
  (the it-N worker verdict + evidence and checker reason, serialized), `{TARGET_HITS}` (mechanical
  grep of the Rust module's distinctive names across the whole target tree), `{TESTIMONY_HITS}`
  (grep across the target repo's `docs/` divergence/status ledgers).

## Prompt body (draft v1)

```
TEMPLATE classify-divergence@1. A candidate correspondence was refuted and the refutation independently upheld: the C# file cites the Rust file but does not substantively port it. Your job is to say WHAT this is instead. Read both files; use the pre-supplied search hits; do not explore beyond them. Honesty: 'unsure' is a valid verdict; fabricated evidence is the cardinal sin.

LEAD {ID}. Prior record: {PRIOR_RECORD}
RUST (read-only): {RUST_PATH}
C# (read-only): {CSHARP_PATH}
Target-wide search hits for this module's names: {TARGET_HITS}
Port-project testimony (claims, not facts): {TESTIMONY_HITS}

CLASSIFY, one verdict:
- intentional-divergence: the C# deliberately replaces/reshapes the Rust design — give kind (adapted | scaffold | dropped-feature) and cite the code or testimony that shows intent.
- not-ported: the citation is aspirational/stale; no substantive port exists anywhere in the hits — the Rust module belongs in the absence queue.
- ported-elsewhere: the real port lives in a different C# file — name it; this becomes a corrected CANDIDATE.
- unsure: say exactly what evidence would settle it.
Return 3 evidence lines and a caveats line. Set id to '{ID}'.
```

## Output schema

```json
{
  "type": "object",
  "properties": {
    "id": { "type": "string" },
    "verdict": { "type": "string", "enum": ["intentional-divergence", "not-ported", "ported-elsewhere", "unsure"] },
    "divergenceKind": { "type": "string", "description": "adapted|scaffold|dropped-feature; required iff intentional-divergence, else empty" },
    "counterpartFile": { "type": "string", "description": "required iff ported-elsewhere, else empty" },
    "evidence": { "type": "array", "items": { "type": "string" } },
    "caveats": { "type": "string" }
  },
  "required": ["id", "verdict", "divergenceKind", "counterpartFile", "evidence", "caveats"]
}
```

## Routing (drafted — confirm at first-batch audit)

- `intentional-divergence` → record a divergence correspondence (`kp corr add --type divergence
  --divergence-kind <kind> --provenance asserted --note <evidence summary>`; create the covering
  unit with `kp unit new` if none exists), then policy decide after audit.
- `not-ported` → drop the candidate's citation claim (leave refuted on the record) and hand the
  module to the absence queue with the evidence attached.
- `ported-elsewhere` → new candidate row (provenance `candidate`) → candidate-review queue.
- `unsure` → steward sample (this queue is low-count; unsure here is worth human minutes).
- Envelope: `worker:sonnet|template:classify-divergence@1`. While unproven there is no cheap
  checker — the audit IS the check; a check-divergence@1 gets authored only if this queue ever
  grows past a handful per iteration.
