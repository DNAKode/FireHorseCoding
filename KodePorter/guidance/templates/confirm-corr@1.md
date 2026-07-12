# confirm-corr@1

- **Queue:** candidate-review (header-citation candidates; also applicable to other file-level
  candidate heuristics).
- **Tier:** haiku-class. **Checker:** [check-confirm@1](check-confirm@1.md), mandatory.
- **Status:** proven — it1 (2026-07-12, FrankenTui): 12 items → 10 accepted, 2 refuted-and-upheld,
  0 unsure, 0 fabrication caught; ~102.5k tokens per accepted item including the checker.
- **Work item fields the dispatcher must supply:** `{ID}` (candidate id, e.g. `cand-hc-31`),
  `{NOTE}` (the candidate's evidence note, e.g. the header-citation text), `{RUST_PATH}` and
  `{CSHARP_PATH}` (absolute paths from the map's anchors — both trees are READ-ONLY).

## Prompt body (verbatim; used in it1)

```
TEMPLATE confirm-corr@1. You are confirming ONE candidate porting correspondence. Read exactly two files, judge, report. Do not explore beyond them. Honesty: 'unsure' is a valid verdict; fabricated evidence is the cardinal sin.

CANDIDATE {ID}: the C# file is believed to be the port of the Rust file (its header cites it: {NOTE}).
RUST (read-only): {RUST_PATH}
C# (read-only): {CSHARP_PATH}

JUDGE: does the C# file substantively port the Rust file's public surface and behavior shape (same core types/functions/logic domain), beyond merely citing it? Return verdict confirm/refute/unsure with EXACTLY 3 evidence pairs (a Rust symbol and its C# counterpart, with a one-line note each) for confirm; for refute/unsure, evidence pairs may name mismatches. Set id to '{ID}'.
```

## Output schema (schema-forced)

```json
{
  "type": "object",
  "properties": {
    "id": { "type": "string" },
    "verdict": { "type": "string", "enum": ["confirm", "refute", "unsure"] },
    "evidence": { "type": "array", "items": { "type": "object", "properties": {
      "rustSymbol": { "type": "string" }, "csharpSymbol": { "type": "string" },
      "note": { "type": "string" } }, "required": ["rustSymbol", "csharpSymbol", "note"] } },
    "caveats": { "type": "string" }
  },
  "required": ["id", "verdict", "evidence", "caveats"]
}
```

## Acceptance rule (it1, unchanged)

worker `confirm` + checker `uphold` → `kp corr promote` + `kp decide --verdict accept
--actor "policy:kp-frankentui@1"`. Worker `refute` + checker `uphold` → leave candidate, route to
the divergence-leads queue. Anything else (`unsure`, checker `overturn`/`unsure`) → leave
proposed, route to orchestrator sample. Method envelope: `worker:haiku|template:confirm-corr@1`.

## Notes from the measured run

- Evidence quality at haiku tier was high (trait↔interface, enum↔record-hierarchy,
  formula-identical mappings) *because the item is bounded to two known files* — do not relax the
  "do not explore" clause.
- The two refutations (files that cite their Rust source but do not substantively port it) were
  the highest-value outputs of the batch — refute is a success mode, not a failure mode.
- `caveats` is where intentional-divergence signals surface; read it before deciding routing.
