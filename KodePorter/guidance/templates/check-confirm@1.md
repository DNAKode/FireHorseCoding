# check-confirm@1

- **Role:** independent checker for [confirm-corr@1](confirm-corr@1.md). Its job is to REFUTE.
- **Tier:** haiku-class (same tier as worker → per LEARNING-LOOP §2.2 a stronger-model sample
  audit applies on top; it1 used the orchestrator reading a sample of evidence pairs).
- **Status:** proven — it1 (2026-07-12, FrankenTui): 12 checks, 0 overturns, both worker
  refutations substantiated with verified evidence; one checker caught a note imprecision without
  overturning (correct calibration).
- **Independence:** the checker receives only the worker's *verdict and evidence list* — never the
  worker's reasoning or a conversation. It re-reads both files itself. Evidence independence is
  typed on the record (M1.5 `independence` field) as `independently-derived` only if the checker
  verified against the files, not against the worker's prose.
- **Work item fields:** `{ID}`, `{WORKER_VERDICT}`, `{WORKER_EVIDENCE_JSON}` (the worker's
  evidence array serialized as JSON), `{RUST_PATH}`, `{CSHARP_PATH}` (both READ-ONLY).

## Prompt body (verbatim; used in it1)

```
TEMPLATE check-confirm@1. You are an INDEPENDENT checker. A worker judged candidate {ID} '{WORKER_VERDICT}' with this evidence: {WORKER_EVIDENCE_JSON}.

VERIFY, by reading the two files yourself (nothing else):
RUST: {RUST_PATH}
C#: {CSHARP_PATH}
For each evidence pair: does the named Rust symbol exist in the Rust file, does the named C# symbol exist in the C# file, and does the pairing actually support the verdict? Return uphold (all evidence real and supportive, verdict reasonable), overturn (evidence fabricated/wrong or verdict unjustified), or unsure - with a per-evidence line and a one-line reason. Your job is to REFUTE if you can. Set id to '{ID}'.
```

## Output schema (schema-forced)

```json
{
  "type": "object",
  "properties": {
    "id": { "type": "string" },
    "verdict": { "type": "string", "enum": ["uphold", "overturn", "unsure"] },
    "perEvidence": { "type": "array", "items": { "type": "string" } },
    "reason": { "type": "string" }
  },
  "required": ["id", "verdict", "perEvidence", "reason"]
}
```

## Notes from the measured run

- "Uphold" applies to refute-verdicts too: upholding a worker's *refutation* is what turns a
  suspicious candidate into a divergence lead. Don't conflate uphold with confirm.
- Overturn rate is the template-health signal: a rising overturn rate means the *worker* template
  is broken (version-bump it), not that checking should stop.
- Method envelope: `checker:haiku|template:check-confirm@1`.
