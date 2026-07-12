# classify-absence@1 (+ companion checker check-absence@1)

- **Queue:** absence-unknown. **Tier:** haiku-class worker, haiku-class checker (+ orchestrator
  sample audit while unproven).
- **Status:** drafted 2026-07-12, **never run**. The first batch MUST be a pilot of ≤50 items
  whose gate numbers decide whether the template scales, is version-bumped, or is retired.
- **The unit of work is a source MODULE (one Rust file), not one entity.** FrankenTui's
  absence-unknown queue is 17,366 entities; judging entities one-by-one would cost more than the
  map saves. One module verdict covers all its contained non-test entities; the dispatcher applies
  the verdict per entity afterwards (`kp absence set` per symbol — mechanical, scriptable). If
  per-entity fan-out proves noisy, a bulk `kp absence set --under <module>` verb is the fix
  (queued in ATTIC terms, not a reason to judge entity-by-entity).
- **Work item fields (dispatcher pre-cuts all of them — the worker never explores):**
  `{MODULE}` (source symbolPath), `{RUST_PATH}` (READ-ONLY), `{ENTITY_COUNT}` (non-test entities
  in the module), `{TARGET_HITS}` (mechanical grep results: the module name and its 3–5 most
  distinctive type/function names searched across the target tree — file:line list, may be empty),
  `{TESTIMONY_HITS}` (grep of the module name across the target repo's own `docs/` divergence/
  status ledgers — testimony, not truth; may be empty).

## Worker prompt body (draft v1)

```
TEMPLATE classify-absence@1. You are classifying ONE unported-looking Rust module. Read the one Rust file given, then judge using ONLY the pre-supplied search results. Do not explore beyond them. Honesty: 'unknown' is a valid verdict; fabricated evidence is the cardinal sin.

MODULE {MODULE} ({ENTITY_COUNT} non-test entities), file (read-only): {RUST_PATH}
Target-side search hits (mechanically produced, may be incomplete): {TARGET_HITS}
Port-project testimony mentioning this module (docs are claims, not facts): {TESTIMONY_HITS}

JUDGE, one verdict:
- not-yet-ported: no credible target counterpart in the hits; module is substantive production code.
- deliberately-dropped: testimony or code evidence says the port intentionally omits it (cite it).
- likely-ported: the hits point at a real counterpart file — name it; this becomes a CANDIDATE correspondence, not an absence.
- unknown: evidence insufficient either way — say what is missing.
Return exactly 2 evidence lines (what you saw that grounds the verdict) and a caveats line. Set id to '{MODULE}'.
```

## Worker output schema

```json
{
  "type": "object",
  "properties": {
    "id": { "type": "string" },
    "verdict": { "type": "string", "enum": ["not-yet-ported", "deliberately-dropped", "likely-ported", "unknown"] },
    "counterpartFile": { "type": "string", "description": "target-relative path; required iff verdict=likely-ported, else empty" },
    "evidence": { "type": "array", "items": { "type": "string" } },
    "caveats": { "type": "string" }
  },
  "required": ["id", "verdict", "counterpartFile", "evidence", "caveats"]
}
```

## Companion checker: check-absence@1 (draft v1)

Independent; receives the worker's verdict + evidence only; re-reads the Rust file and re-runs the
same greps itself (the dispatcher gives it the same pre-cut inputs, not the worker's). Job: refute.
Uphold / overturn / unsure with per-evidence lines. Same schema shape as
[check-confirm@1](check-confirm@1.md).

## Routing (drafted acceptance rule — confirm at pilot gate)

- `not-yet-ported` or `deliberately-dropped` + uphold → `kp absence set` per contained non-test
  entity, then `kp decide --verdict accept --actor "policy:kp-frankentui@1"` on the recorded
  claim; envelope `worker:haiku|template:classify-absence@1`.
- `likely-ported` + uphold → dispatcher creates a candidate correspondence
  (`.kodeporter` candidate row, provenance `candidate`) and routes it to the candidate-review
  queue (confirm-corr@1 territory) — absence classification doubles as candidate discovery.
- `unknown` or checker non-uphold → stays in the queue; recurring unknowns with the same missing
  evidence indicate a dispatcher gap (better pre-cut hits), not a worker failure.

## Known risks to watch at the pilot gate

- Grep-hit quality bounds the whole template: empty `{TARGET_HITS}` on a ported module produces
  false not-yet-ported verdicts. Audit specifically for this failure mode.
- Testimony (the port's own docs) is persuasive to cheap models — the prompt marks it as claims,
  and the checker must not accept a deliberately-dropped verdict whose only evidence is testimony
  without a citation the checker verified.
