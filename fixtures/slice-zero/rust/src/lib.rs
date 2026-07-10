//! `headscan`: a deliberately tiny parser for "FHC header documents".
//!
//! This crate is the **source** side of the Slice Zero fixture (see
//! `fixtures/slice-zero/CONTRACT.md`, which is the normative specification).
//! Every rule implemented here is cross-referenced to a CONTRACT.md §1.1
//! numbered rule in a comment so the C# port can be checked line-by-line
//! against the same authority.

use std::collections::HashMap;
use std::fmt;

/// Which line-ending style(s) were observed in the input (CONTRACT §1.1
/// rule 1).
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum LineEnding {
    Lf,
    Crlf,
    Mixed,
}

impl LineEnding {
    pub fn as_str(self) -> &'static str {
        match self {
            LineEnding::Lf => "lf",
            LineEnding::Crlf => "crlf",
            LineEnding::Mixed => "mixed",
        }
    }
}

/// The closed set of parse error codes (CONTRACT §1.1, "Error codes").
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum ErrorCode {
    MissingColon,
    BadKey,
    DanglingContinuation,
    BadNumber,
    RatioOutOfRange,
    ValueTooLong,
}

impl ErrorCode {
    pub fn as_str(self) -> &'static str {
        match self {
            ErrorCode::MissingColon => "MissingColon",
            ErrorCode::BadKey => "BadKey",
            ErrorCode::DanglingContinuation => "DanglingContinuation",
            ErrorCode::BadNumber => "BadNumber",
            ErrorCode::RatioOutOfRange => "RatioOutOfRange",
            ErrorCode::ValueTooLong => "ValueTooLong",
        }
    }
}

/// A parse error: an error code plus the 1-based line number of the
/// offending line (CONTRACT §1.1 rule 10, fail-fast).
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub struct ParseError {
    pub code: ErrorCode,
    pub line: usize,
}

impl fmt::Display for ParseError {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        write!(f, "{} at line {}", self.code.as_str(), self.line)
    }
}

impl std::error::Error for ParseError {}

/// The typed value of a successfully parsed field (CONTRACT §1.1 rule 8).
#[derive(Debug, Clone, PartialEq)]
pub enum FieldValue {
    Text(String),
    Count(u64),
    /// `floor(v * 1e9 + 0.5)` computed in f64 (CONTRACT §1.1 rule 8,
    /// `-ratio` bullet) — this exact expression, so no float-formatting
    /// differences can appear between the Rust and C# sides.
    Ratio { value_nanos: i64 },
}

impl FieldValue {
    pub fn kind(&self) -> &'static str {
        match self {
            FieldValue::Text(_) => "text",
            FieldValue::Count(_) => "count",
            FieldValue::Ratio { .. } => "ratio",
        }
    }
}

/// One header field, keyed by its first-appearance line (CONTRACT §1.1
/// rules 6-7).
#[derive(Debug, Clone, PartialEq)]
pub struct Field {
    pub key: String,
    /// 1-based line number of the key's *first* occurrence.
    pub line: usize,
    pub value: FieldValue,
}

/// A fully parsed header document.
#[derive(Debug, Clone, PartialEq)]
pub struct HeaderDoc {
    /// Fields in first-appearance order of their keys (rule 7).
    pub fields: Vec<Field>,
    pub line_ending: LineEnding,
    pub duplicates: u32,
}

const MAX_VALUE_LEN: usize = 4096;
const RATIO_TOLERANCE: f64 = 1e-9;

/// One raw (pre-dedup, pre-typed) header occurrence collected during the
/// line scan.
struct RawOccurrence {
    key: String,
    line: usize,
    value: String,
}

/// Parse `input` (raw bytes of an FHC header document) per CONTRACT.md
/// §1.1.
///
/// Processing happens in two phases:
///  1. A single top-to-bottom line scan that resolves line endings,
///     comments, blank lines, continuation, and structural errors
///     (`MissingColon`, `BadKey`, `DanglingContinuation`, `ValueTooLong`),
///     fail-fast, in line order. This produces a list of raw occurrences
///     (duplicates and all).
///  2. Duplicate resolution (first-wins, rule 6) over those occurrences,
///     followed by typed-value validation (`BadNumber`, `RatioOutOfRange`)
///     "in field order" per rule 8 — i.e. over the deduplicated field list,
///     in first-appearance order.
pub fn parse(input: &[u8]) -> Result<HeaderDoc, ParseError> {
    let text = String::from_utf8_lossy(input);
    let (lines, line_ending) = split_lines(&text);

    let raw_occurrences = scan_lines(&lines)?;

    let mut fields: Vec<Field> = Vec::with_capacity(raw_occurrences.len());
    let mut seen: HashMap<&str, ()> = HashMap::new();
    let mut duplicates: u32 = 0;

    for occ in &raw_occurrences {
        if seen.contains_key(occ.key.as_str()) {
            duplicates += 1;
            continue;
        }
        seen.insert(occ.key.as_str(), ());
        fields.push(Field {
            key: occ.key.clone(),
            line: occ.line,
            value: FieldValue::Text(occ.value.clone()), // placeholder, typed below
        });
    }

    for field in fields.iter_mut() {
        let raw_value = match &field.value {
            FieldValue::Text(s) => s.clone(),
            _ => unreachable!("fields are Text placeholders before typing"),
        };
        field.value = type_value(&field.key, &raw_value, field.line)?;
    }

    Ok(HeaderDoc {
        fields,
        line_ending,
        duplicates,
    })
}

/// Split `text` into logical lines (terminators stripped), and classify the
/// overall line-ending style observed (rule 1).
///
/// A trailing line with no terminator at all (including the degenerate
/// empty-document case) contributes no observation; if *no* terminator was
/// ever observed the line ending defaults to `lf`.
fn split_lines(text: &str) -> (Vec<&str>, LineEnding) {
    let mut lines = Vec::new();
    let mut saw_lf = false;
    let mut saw_crlf = false;

    let bytes = text.as_bytes();
    let len = bytes.len();
    let mut start = 0usize;
    let mut i = 0usize;
    while i < len {
        if bytes[i] == b'\n' {
            let mut end = i;
            if end > start && bytes[end - 1] == b'\r' {
                end -= 1;
                saw_crlf = true;
            } else {
                saw_lf = true;
            }
            lines.push(&text[start..end]);
            start = i + 1;
        }
        i += 1;
    }
    if start < len {
        lines.push(&text[start..len]);
    }

    let line_ending = match (saw_lf, saw_crlf) {
        (true, true) => LineEnding::Mixed,
        (true, false) => LineEnding::Lf,
        (false, true) => LineEnding::Crlf,
        (false, false) => LineEnding::Lf,
    };

    (lines, line_ending)
}

/// A line is blank if it is empty or consists only of spaces/tabs (rule 3).
fn is_blank(line: &str) -> bool {
    line.chars().all(|c| c == ' ' || c == '\t')
}

/// Trim leading/trailing spaces and tabs only (rule 4/5 — not general
/// Unicode whitespace).
fn trim_sp_tab(s: &str) -> &str {
    s.trim_matches(|c| c == ' ' || c == '\t')
}

/// `[A-Za-z][A-Za-z0-9-]*` (rule 4), case-sensitive.
fn is_valid_key(key: &str) -> bool {
    let mut chars = key.chars();
    match chars.next() {
        Some(c) if c.is_ascii_alphabetic() => {}
        _ => return false,
    }
    chars.all(|c| c.is_ascii_alphanumeric() || c == '-')
}

fn check_len(value: &str, line_no: usize) -> Result<(), ParseError> {
    if value.chars().count() > MAX_VALUE_LEN {
        return Err(ParseError {
            code: ErrorCode::ValueTooLong,
            line: line_no,
        });
    }
    Ok(())
}

/// Phase 1: single top-to-bottom scan producing raw occurrences (rules
/// 2-5, 9-10 for structural errors).
fn scan_lines(lines: &[&str]) -> Result<Vec<RawOccurrence>, ParseError> {
    let mut occurrences: Vec<RawOccurrence> = Vec::new();
    // Index into `occurrences` of the header currently eligible to receive
    // continuation lines ("in force"), or None if no header is in force.
    let mut current: Option<usize> = None;

    for (idx, raw_line) in lines.iter().enumerate() {
        let line_no = idx + 1;
        let line = *raw_line;

        // Rule 3: blank lines are ignored and end any continuation.
        if is_blank(line) {
            current = None;
            continue;
        }

        // Rule 2: comment lines (first character is literally '#') are
        // ignored entirely — continuation state is left untouched. Note
        // this means a line like " # not a comment" (leading whitespace
        // before the '#') is NOT a comment; it is a continuation line
        // whose trimmed text starts with '#', by the letter of rule 2
        // ("first character").
        if line.starts_with('#') {
            continue;
        }

        let first = line.chars().next().expect("non-blank line has a first char");
        if first == ' ' || first == '\t' {
            // Rule 5: continuation.
            match current {
                None => {
                    return Err(ParseError {
                        code: ErrorCode::DanglingContinuation,
                        line: line_no,
                    })
                }
                Some(field_idx) => {
                    let cont_text = trim_sp_tab(line);
                    let occ = &occurrences[field_idx];
                    let mut new_value =
                        String::with_capacity(occ.value.len() + 1 + cont_text.len());
                    new_value.push_str(&occ.value);
                    new_value.push(' ');
                    new_value.push_str(cont_text);
                    check_len(&new_value, line_no)?;
                    occurrences[field_idx].value = new_value;
                }
            }
            continue;
        }

        // Rule 4: header line.
        let colon_idx = match line.find(':') {
            None => {
                return Err(ParseError {
                    code: ErrorCode::MissingColon,
                    line: line_no,
                })
            }
            Some(i) => i,
        };
        let key = &line[..colon_idx];
        if !is_valid_key(key) {
            return Err(ParseError {
                code: ErrorCode::BadKey,
                line: line_no,
            });
        }
        let value_raw = &line[colon_idx + 1..];
        let value = trim_sp_tab(value_raw).to_string();
        check_len(&value, line_no)?;

        occurrences.push(RawOccurrence {
            key: key.to_string(),
            line: line_no,
            value,
        });
        current = Some(occurrences.len() - 1);
    }

    Ok(occurrences)
}

/// Phase 2 per-field typing (rule 8).
fn type_value(key: &str, raw_value: &str, line: usize) -> Result<FieldValue, ParseError> {
    if key.ends_with("-count") {
        if raw_value.is_empty() || !raw_value.chars().all(|c| c.is_ascii_digit()) {
            return Err(ParseError {
                code: ErrorCode::BadNumber,
                line,
            });
        }
        match raw_value.parse::<u64>() {
            Ok(n) => Ok(FieldValue::Count(n)),
            Err(_) => Err(ParseError {
                code: ErrorCode::BadNumber,
                line,
            }),
        }
    } else if key.ends_with("-ratio") {
        if !is_valid_ratio_literal(raw_value) {
            return Err(ParseError {
                code: ErrorCode::BadNumber,
                line,
            });
        }
        let normalized = normalize_ratio_literal(raw_value);
        let v: f64 = match normalized.parse() {
            Ok(v) => v,
            Err(_) => {
                return Err(ParseError {
                    code: ErrorCode::BadNumber,
                    line,
                })
            }
        };
        // This branch is unreachable given `is_valid_ratio_literal` above
        // (the format never admits a sign), but is kept to mirror
        // CONTRACT §1.1 rule 8 literally.
        if v < 0.0 {
            return Err(ParseError {
                code: ErrorCode::RatioOutOfRange,
                line,
            });
        }
        let clamped = if v > 1.0 {
            if v <= 1.0 + RATIO_TOLERANCE {
                1.0
            } else {
                return Err(ParseError {
                    code: ErrorCode::RatioOutOfRange,
                    line,
                });
            }
        } else {
            v
        };
        let value_nanos = (clamped * 1e9 + 0.5).floor() as i64;
        Ok(FieldValue::Ratio { value_nanos })
    } else {
        Ok(FieldValue::Text(raw_value.to_string()))
    }
}

/// "digits, optional single '.'" (rule 8, `-ratio` bullet); at least one
/// digit required, no sign.
fn is_valid_ratio_literal(s: &str) -> bool {
    if s.is_empty() {
        return false;
    }
    if s.chars().filter(|&c| c == '.').count() > 1 {
        return false;
    }
    if !s.chars().all(|c| c.is_ascii_digit() || c == '.') {
        return false;
    }
    s.chars().any(|c| c.is_ascii_digit())
}

/// Pad a bare leading/trailing '.' so `str::parse::<f64>` accepts forms
/// like ".5" and "5." that the CONTRACT's ratio grammar permits.
fn normalize_ratio_literal(s: &str) -> String {
    let mut s = s.to_string();
    if s.starts_with('.') {
        s.insert(0, '0');
    }
    if s.ends_with('.') {
        s.push('0');
    }
    s
}

// ---------------------------------------------------------------------
// Canonical JSON (CONTRACT §1.2). Built by hand, field by field, so the
// output is byte-exact regardless of any JSON library's map/struct field
// ordering behavior.
// ---------------------------------------------------------------------

/// Render `result` as the canonical result JSON object (CONTRACT §1.2). No
/// trailing newline.
pub fn to_canonical_json(result: &Result<HeaderDoc, ParseError>) -> String {
    match result {
        Ok(doc) => doc_to_json(doc),
        Err(err) => error_to_json(err),
    }
}

fn error_to_json(err: &ParseError) -> String {
    format!(
        "{{\"error\":{{\"code\":\"{}\",\"line\":{}}}}}",
        err.code.as_str(),
        err.line
    )
}

fn doc_to_json(doc: &HeaderDoc) -> String {
    let mut out = String::new();
    out.push_str("{\"fields\":[");
    for (i, field) in doc.fields.iter().enumerate() {
        if i > 0 {
            out.push(',');
        }
        out.push_str("{\"key\":");
        push_json_string(&mut out, &field.key);
        out.push_str(",\"kind\":\"");
        out.push_str(field.value.kind());
        out.push_str("\",\"line\":");
        out.push_str(&field.line.to_string());
        match &field.value {
            FieldValue::Text(s) => {
                out.push_str(",\"value\":");
                push_json_string(&mut out, s);
            }
            FieldValue::Count(n) => {
                out.push_str(",\"value\":");
                out.push_str(&n.to_string());
            }
            FieldValue::Ratio { value_nanos } => {
                out.push_str(",\"valueNanos\":");
                out.push_str(&value_nanos.to_string());
            }
        }
        out.push('}');
    }
    out.push_str("],\"lineEnding\":\"");
    out.push_str(doc.line_ending.as_str());
    out.push_str("\",\"warnings\":{\"duplicates\":");
    out.push_str(&doc.duplicates.to_string());
    out.push_str("}}");
    out
}

/// Render `s` as a JSON string literal (with quotes).
pub fn json_string(s: &str) -> String {
    let mut out = String::new();
    push_json_string(&mut out, s);
    out
}

fn push_json_string(out: &mut String, s: &str) {
    out.push('"');
    for c in s.chars() {
        match c {
            '"' => out.push_str("\\\""),
            '\\' => out.push_str("\\\\"),
            '\n' => out.push_str("\\n"),
            '\r' => out.push_str("\\r"),
            '\t' => out.push_str("\\t"),
            c if (c as u32) < 0x20 => {
                out.push_str(&format!("\\u{:04x}", c as u32));
            }
            c => out.push(c),
        }
    }
    out.push('"');
}

#[cfg(test)]
mod tests {
    use super::*;

    fn ok(input: &str) -> HeaderDoc {
        parse(input.as_bytes()).expect("expected Ok")
    }

    fn err(input: &str) -> ParseError {
        parse(input.as_bytes()).expect_err("expected Err")
    }

    // Rule 1: line endings.
    #[test]
    fn line_ending_lf() {
        let doc = ok("a: 1\nb: 2\n");
        assert_eq!(doc.line_ending, LineEnding::Lf);
    }

    #[test]
    fn line_ending_crlf() {
        let doc = ok("a: 1\r\nb: 2\r\n");
        assert_eq!(doc.line_ending, LineEnding::Crlf);
    }

    #[test]
    fn line_ending_mixed() {
        let doc = ok("a: 1\nb: 2\r\n");
        assert_eq!(doc.line_ending, LineEnding::Mixed);
    }

    #[test]
    fn line_ending_defaults_lf_when_unobserved() {
        let doc = ok("");
        assert_eq!(doc.line_ending, LineEnding::Lf);
    }

    // Rule 2: comments.
    #[test]
    fn comment_lines_ignored() {
        let doc = ok("# a comment\na: 1\n# another\n");
        assert_eq!(doc.fields.len(), 1);
        assert_eq!(doc.fields[0].key, "a");
    }

    #[test]
    fn leading_whitespace_before_hash_is_not_a_comment() {
        // " #x" starts with a space, so by the letter of rule 2 it is a
        // continuation line, not a comment. With no header in force this
        // is a DanglingContinuation, not a silently-ignored comment.
        let e = err(" # not a comment\n");
        assert_eq!(e.code, ErrorCode::DanglingContinuation);
        assert_eq!(e.line, 1);
    }

    // Rule 3: blank lines.
    #[test]
    fn blank_lines_ignored_and_end_continuation() {
        let doc = ok("a: 1\n   \nb: 2\n");
        assert_eq!(doc.fields.len(), 2);
    }

    #[test]
    fn blank_line_ends_continuation_so_next_continuation_dangles() {
        let e = err("a: 1\n\n  more\n");
        assert_eq!(e.code, ErrorCode::DanglingContinuation);
        assert_eq!(e.line, 3);
    }

    // Rule 4: header line / MissingColon / BadKey.
    #[test]
    fn missing_colon_errors() {
        let e = err("a: 1\nno colon here\n");
        assert_eq!(e.code, ErrorCode::MissingColon);
        assert_eq!(e.line, 2);
    }

    #[test]
    fn bad_key_empty_errors() {
        let e = err(": value\n");
        assert_eq!(e.code, ErrorCode::BadKey);
        assert_eq!(e.line, 1);
    }

    #[test]
    fn bad_key_leading_digit_errors() {
        let e = err("1abc: value\n");
        assert_eq!(e.code, ErrorCode::BadKey);
        assert_eq!(e.line, 1);
    }

    #[test]
    fn value_trimmed_of_spaces_and_tabs() {
        let doc = ok("a: \t  hello world  \t\n");
        match &doc.fields[0].value {
            FieldValue::Text(s) => assert_eq!(s, "hello world"),
            _ => panic!("expected text"),
        }
    }

    // Rule 5: continuation.
    #[test]
    fn continuation_appends_single_space_and_trims() {
        let doc = ok("a: hello\n  world\n\tagain\n");
        match &doc.fields[0].value {
            FieldValue::Text(s) => assert_eq!(s, "hello world again"),
            _ => panic!("expected text"),
        }
    }

    #[test]
    fn dangling_continuation_on_first_line_errors() {
        let e = err("  dangling\n");
        assert_eq!(e.code, ErrorCode::DanglingContinuation);
        assert_eq!(e.line, 1);
    }

    #[test]
    fn continuation_survives_across_comment_line_hazard() {
        // Comments do not touch continuation state, so a continuation line
        // after a comment still attaches to the still-open header.
        let doc = ok("a: hello\n# comment\n  world\n");
        match &doc.fields[0].value {
            FieldValue::Text(s) => assert_eq!(s, "hello world"),
            _ => panic!("expected text"),
        }
    }

    // Rule 6: duplicates, first-wins.
    #[test]
    fn duplicate_keys_first_wins_and_counted() {
        let doc = ok("a: first\na: second\na: third\n");
        assert_eq!(doc.fields.len(), 1);
        match &doc.fields[0].value {
            FieldValue::Text(s) => assert_eq!(s, "first"),
            _ => panic!("expected text"),
        }
        assert_eq!(doc.duplicates, 2);
    }

    #[test]
    fn keys_are_case_sensitive_not_duplicates() {
        let doc = ok("Key: one\nkey: two\nKEY: three\n");
        assert_eq!(doc.fields.len(), 3);
        assert_eq!(doc.duplicates, 0);
    }

    // Rule 7: ordering guarantee.
    #[test]
    fn fields_preserve_first_appearance_order() {
        let doc = ok("z: 1\na: 2\nm: 3\na: 4\n");
        let keys: Vec<&str> = doc.fields.iter().map(|f| f.key.as_str()).collect();
        assert_eq!(keys, vec!["z", "a", "m"]);
    }

    // Rule 8: -count.
    #[test]
    fn count_ok() {
        let doc = ok("x-count: 42\n");
        assert_eq!(doc.fields[0].value, FieldValue::Count(42));
    }

    #[test]
    fn count_max_u64() {
        let doc = ok("x-count: 18446744073709551615\n");
        assert_eq!(doc.fields[0].value, FieldValue::Count(u64::MAX));
    }

    #[test]
    fn count_overflow_errors() {
        let e = err("x-count: 18446744073709551616\n");
        assert_eq!(e.code, ErrorCode::BadNumber);
    }

    #[test]
    fn count_negative_errors() {
        let e = err("x-count: -1\n");
        assert_eq!(e.code, ErrorCode::BadNumber);
    }

    // Rule 8: -ratio.
    #[test]
    fn ratio_zero_and_one() {
        let doc = ok("x-ratio: 0\ny-ratio: 1\n");
        assert_eq!(doc.fields[0].value, FieldValue::Ratio { value_nanos: 0 });
        assert_eq!(
            doc.fields[1].value,
            FieldValue::Ratio {
                value_nanos: 1_000_000_000
            }
        );
    }

    #[test]
    fn ratio_clamp_edge() {
        let doc = ok("x-ratio: 1.0000000005\n");
        assert_eq!(
            doc.fields[0].value,
            FieldValue::Ratio {
                value_nanos: 1_000_000_000
            }
        );
    }

    #[test]
    fn ratio_too_big_errors() {
        let e = err("x-ratio: 1.1\n");
        assert_eq!(e.code, ErrorCode::RatioOutOfRange);
    }

    #[test]
    fn ratio_negative_format_errors_as_bad_number() {
        // The ratio grammar is "digits, optional single '.'" with no sign,
        // so a literal '-' fails the format check (BadNumber) before any
        // range check could apply.
        let e = err("x-ratio: -0.5\n");
        assert_eq!(e.code, ErrorCode::BadNumber);
    }

    // Rule 9: value length limit.
    #[test]
    fn value_at_limit_ok() {
        let value = "a".repeat(4096);
        let input = format!("x: {}\n", value);
        let doc = ok(&input);
        match &doc.fields[0].value {
            FieldValue::Text(s) => assert_eq!(s.chars().count(), 4096),
            _ => panic!("expected text"),
        }
    }

    #[test]
    fn value_too_long_errors() {
        let value = "a".repeat(4097);
        let input = format!("x: {}\n", value);
        let e = err(&input);
        assert_eq!(e.code, ErrorCode::ValueTooLong);
    }

    // Rule 10: fail-fast — first error wins, regardless of later ones.
    #[test]
    fn fail_fast_reports_first_error_only() {
        let e = err("bad line no colon\nanother: bad key !!\n");
        assert_eq!(e.code, ErrorCode::MissingColon);
        assert_eq!(e.line, 1);
    }

    // Canonical JSON shape.
    #[test]
    fn canonical_json_success_shape() {
        let result = parse(b"a: hello\nn-count: 3\nr-ratio: 0.5\n");
        let json = to_canonical_json(&result);
        assert_eq!(
            json,
            "{\"fields\":[{\"key\":\"a\",\"kind\":\"text\",\"line\":1,\"value\":\"hello\"},\
             {\"key\":\"n-count\",\"kind\":\"count\",\"line\":2,\"value\":3},\
             {\"key\":\"r-ratio\",\"kind\":\"ratio\",\"line\":3,\"valueNanos\":500000000}],\
             \"lineEnding\":\"lf\",\"warnings\":{\"duplicates\":0}}"
        );
    }

    #[test]
    fn canonical_json_error_shape() {
        let result = parse(b"no colon\n");
        let json = to_canonical_json(&result);
        assert_eq!(json, "{\"error\":{\"code\":\"MissingColon\",\"line\":1}}");
    }

    #[test]
    fn canonical_json_escapes_strings() {
        let result = parse(b"a: he said \"hi\"\\ok\n");
        let json = to_canonical_json(&result);
        assert!(json.contains("\\\"hi\\\""));
        assert!(json.contains("\\\\ok"));
    }

    #[test]
    fn empty_document_parses_to_empty_fields() {
        let doc = ok("");
        assert!(doc.fields.is_empty());
        assert_eq!(doc.duplicates, 0);
    }

    #[test]
    fn only_comments_parses_to_empty_fields() {
        let doc = ok("# one\n# two\n");
        assert!(doc.fields.is_empty());
    }
}
