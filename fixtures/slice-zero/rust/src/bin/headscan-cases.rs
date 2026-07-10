//! `headscan-cases`: the K-D8 narrow-waist case-runner harness (CONTRACT
//! §1.3).
//!
//! Reads JSONL on stdin, one case per line:
//!   `{"name":"case-id","inputB64":"<base64 of raw input bytes>"}`
//! and writes one JSONL line per case, in input order, to stdout:
//!   `{"name":"case-id","result":<canonical result JSON>}\n`

use base64::engine::general_purpose::STANDARD as BASE64;
use base64::Engine as _;
use serde::Deserialize;
use std::io::{self, BufRead, Write};

#[derive(Deserialize)]
struct Case {
    name: String,
    #[serde(rename = "inputB64")]
    input_b64: String,
}

fn main() {
    let stdin = io::stdin();
    let stdout = io::stdout();
    let mut out = stdout.lock();

    for line in stdin.lock().lines() {
        let line = line.expect("failed to read stdin line");
        if line.trim().is_empty() {
            continue;
        }
        let case: Case = serde_json::from_str(&line)
            .unwrap_or_else(|e| panic!("invalid case JSON ({}): {}", e, line));
        let bytes = BASE64
            .decode(case.input_b64.as_bytes())
            .unwrap_or_else(|e| panic!("invalid base64 for case {}: {}", case.name, e));

        let result = headscan::parse(&bytes);
        let result_json = headscan::to_canonical_json(&result);

        out.write_all(b"{\"name\":").unwrap();
        out.write_all(headscan::json_string(&case.name).as_bytes())
            .unwrap();
        out.write_all(b",\"result\":").unwrap();
        out.write_all(result_json.as_bytes()).unwrap();
        out.write_all(b"}\n").unwrap();
    }

    out.flush().unwrap();
}
