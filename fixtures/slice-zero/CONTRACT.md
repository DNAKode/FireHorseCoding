# Slice Zero — Fixture Contract

Jointly owned by Gneiss and KodePorter (roadmaps §9/§9). Ground-truth edits are escalations, not
local fixes. This file is the specification; the ground truth key (`ground-truth.yaml`) is derived
from it.

## 1. The subject: `headscan`, an FHC header-document parser

A deliberately tiny Rust crate with genuine semantic hazards, ported to C#. It parses "FHC header
documents": UTF-8 text of key/value header lines.

### 1.1 Format semantics (normative — both implementations)

1. **Line endings:** `\n` and `\r\n` both accepted, may be mixed. The parser *observes* which were
   used: `lf`, `crlf`, or `mixed` (platform hazard, reported in output).
2. **Comments:** lines whose first character is `#` are ignored entirely.
3. **Blank lines** (empty or whitespace-only) are ignored; a blank line **ends** any continuation.
4. **Header line:** `Key: Value`. Key matches `[A-Za-z][A-Za-z0-9-]*`, **case-sensitive**
   (`Key` and `key` are different keys, NOT duplicates). Value is the rest after the first `:`,
   trimmed of leading/trailing spaces and tabs. A non-comment, non-blank, non-continuation line
   without `:` → error `MissingColon`. A line with `:` but empty key or key not matching the
   pattern → error `BadKey`.
5. **Continuation:** a line starting with space or tab continues the previous header's value:
   append a single space `' '` plus the trimmed continuation text. Continuation with no preceding
   header in force → error `DanglingContinuation`.
6. **Duplicate keys:** the **first** occurrence wins; each later duplicate is discarded and counted
   in `warnings.duplicates`. (THE QUIRK — delta d2 changes this to last-wins.)
7. **Ordering guarantee:** output fields preserve first-appearance order of keys.
8. **Typed values** (checked after continuation assembly, in field order):
   - key ending `-count`: value must parse as decimal u64 (no sign, no separators) → else error
     `BadNumber`. Kind `count`; output as JSON integer `value`.
   - key ending `-ratio`: value must parse as decimal number (digits, optional single `.`); compute
     f64. Range: `< 0` → error `RatioOutOfRange`; `> 1` but `≤ 1 + 1e-9` → **clamp to 1.0** (the
     tolerance hazard); `> 1 + 1e-9` → error `RatioOutOfRange`. Kind `ratio`; output as
     `valueNanos`: integer `floor(v * 1e9 + 0.5)` computed in f64 — both implementations use this
     exact expression so no float-formatting differences can appear.
   - otherwise kind `text`; output string as-is (post trim/continuation).
9. **Limits:** assembled value length > 4096 chars → error `ValueTooLong`.
10. **Fail-fast:** the first error aborts the parse. Errors carry 1-based line number of the
    offending line.

Error codes (closed set): `MissingColon`, `BadKey`, `DanglingContinuation`, `BadNumber`,
`RatioOutOfRange`, `ValueTooLong`.

### 1.2 Canonical result JSON (both sides byte-identical)

Success:
`{"fields":[{"key":"...","kind":"text","line":N,"value":"..."}, {"key":"...","kind":"count","line":N,"value":123}, {"key":"...","kind":"ratio","line":N,"valueNanos":500000000}],"lineEnding":"lf","warnings":{"duplicates":0}}`

Error: `{"error":{"code":"MissingColon","line":3}}`

Rules: UTF-8, no whitespace between tokens, object keys exactly in the orders shown (fields:
key, kind, line, value|valueNanos), `fields` array in first-appearance order, `line` = line of the
key's first occurrence. No trailing newline… except: each JSONL response line ends with `\n`.

### 1.3 Harness protocol (K-D8 narrow waist)

Each side ships a case-runner executable reading JSONL on stdin, one case per line:
`{"name":"case-id","inputB64":"<base64 of raw input bytes>"}`
and writing one JSONL line per case: `{"name":"case-id","result":<canonical result JSON>}`
in input order. Rust: `cargo run --quiet --bin headscan-cases`. C#: the `HeadScan.Cases` console
project.

## 2. Layout

```
fixtures/slice-zero/
  CONTRACT.md                this file — the spec
  ground-truth.yaml          answer key (units, correspondences, claims, per-delta impacts)
  rust/                      cargo crate `headscan` (lib + bin headscan-cases) — THE SOURCE
  csharp/                    HeadScan.Net solution folder — THE TARGET (lib + HeadScan.Cases)
  corpus/cases.jsonl         ~25 cases per §3
  corpus/goldens-base.jsonl  committed output of the Rust runner at base
  corpus/goldens-d2.jsonl    committed output at delta d2
  deltas/d1-benign-refactor.patch
  deltas/d2-duplicate-policy.patch
  deltas/d3-new-output-field.patch     (reserved for a later increment)
  tools/apply-delta.ps1      copies rust/ → workspace/checkouts/rust-<d>, applies patch (git apply)
```

## 3. Corpus (≥ these cases)

happy-lf, happy-crlf, mixed-endings, comments-and-blanks, continuation-basic,
continuation-multi, continuation-after-blank-dangling (err), dangling-continuation-first-line (err),
duplicate-two, duplicate-three-mixed-case (Key/key not duplicates), first-wins-vs-value-order,
missing-colon (err), bad-key-leading-digit (err), empty-key (err), count-ok, count-max-u64
(18446744073709551615), count-overflow (err), count-negative (err), ratio-zero, ratio-one,
ratio-clamp-edge (1.0000000005 → clamp), ratio-too-big (1.1 → err), ratio-negative (err),
value-too-long (4097 chars → err), value-at-limit (4096 → ok), unicode-value (emoji + CJK),
empty-doc, only-comments.

## 4. Deltas

- **d1-benign-refactor:** rename an internal helper and reorder private functions. Behavior
  identical; goldens identical. (Tests that impact analysis does NOT cry wolf: only `contentHash`
  of touched entities changes.)
- **d2-duplicate-policy:** first-wins → last-wins (§1.1 rule 6); `warnings.duplicates` unchanged in
  meaning. Goldens differ exactly for duplicate cases; `goldens-d2.jsonl` committed.
- **d3-new-output-field:** adds `"keyCount": N` to success output (shape change). Reserved; not
  exercised this increment.

## 5. Ground truth (summarized; full detail in ground-truth.yaml)

- **Migration unit** `parser-core`: the parser (Rust `headscan::parse` + typed-value layer) ↔ C#
  `HeadScan.HeaderParser`.
- **Correspondences:** `implements` (unit-level); `maps-to` `headscan::parse` ↔
  `HeadScan.HeaderParser.Parse`; `maps-to` for the error enum ↔ error-code type; `adapts` — Rust
  `Result<HeaderDoc, ParseError>` ↔ C# result-object pattern (kind `adaptation`, systematic,
  policy-level); `covers` — the differential VerificationRun over `corpus/cases.jsonl`.
- **Behavior claims:** B1 "duplicate keys: first occurrence wins" (evidence: rust source anchor +
  duplicate-two golden); B2 "ratio values within 1e-9 above 1 clamp to 1.0" (evidence anchor +
  ratio-clamp-edge); B3 "keys are case-sensitive" (evidence anchor + duplicate-three-mixed-case).
- **Delta impacts:** d1 → no behavior claim stale, touched-entity anchors drift only (correspondence
  content hashes update; no semantic stale). d2 → B1 stale + `covers` verification stale +
  `maps-to` parse stale; B2, B3 unaffected.

## 6. Provider dump format (shared by tools/rust-map-dump and the C# Roslyn provider)

One JSON object: `{"provider":"<name>@<version>","root":"<abs-or-rel>","entities":[Entity...]}`
Entity: `{"kind":"module|struct|enum|variant|fn|method|impl|const|field|namespace|class|record|property|enummember","name":"parse","symbolPath":"headscan::parse" or "HeadScan.HeaderParser.Parse(string)","file":"src/lib.rs","startLine":1,"endLine":40,"contentHash":"<sha256 of span text with \r\n→\n>","parentSymbolPath":"headscan" or null}`
Entities sorted by (file, startLine, symbolPath). `symbolPath` is the stable identity coordinate
(K-D3); the importer computes `entityId = sha256(side + "|" + kind + "|" + symbolPath)`.

`tools/rust-map-dump`: a separate small cargo crate (syn v2, walkdir, serde_json, sha2). Usage:
`cargo run --manifest-path tools/rust-map-dump/Cargo.toml -- <crate-dir> > out.json`. Deterministic
output (sorted). Parses all `.rs` under `<crate-dir>/src`.
