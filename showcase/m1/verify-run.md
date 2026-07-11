# Verify run: parser-core / io-agreement-v1

- Generated: `2026-07-11T21:29:06Z`
- Cases file: `C:\Work\FireHorseCoding\fixtures\slice-zero\corpus\cases.jsonl`
- Source basis: `base`
- Target basis: `base`
- Source cmd: `cargo run --quiet --manifest-path "C:\Work\FireHorseCoding\fixtures\slice-zero\rust\Cargo.toml" --bin headscan-cases`
- Target cmd: `"C:\Work\FireHorseCoding\fixtures\slice-zero\csharp\HeadScan.Cases\bin\Debug\net10.0\HeadScan.Cases.exe"`
- Verdict: **pass**
- Cases: 28 total, 28 pass, 0 fail

## Rerun

```
kp verify run --workspace C:\Work\FireHorseCoding\fixtures\slice-zero\workspace\m1 --unit parser-core --cases C:\Work\FireHorseCoding\fixtures\slice-zero\corpus\cases.jsonl --source-cmd "cargo run --quiet --manifest-path "C:\Work\FireHorseCoding\fixtures\slice-zero\rust\Cargo.toml" --bin headscan-cases" --target-cmd ""C:\Work\FireHorseCoding\fixtures\slice-zero\csharp\HeadScan.Cases\bin\Debug\net10.0\HeadScan.Cases.exe""
```
