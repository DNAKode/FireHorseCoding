# M1 marquee: "See the port" — the Slice Zero story, end to end.
#
# Runs the full KodePorter-on-Gneiss S1-lite story on the Slice Zero fixture:
# pin + map both sides, dossier a unit, record typed correspondences (including
# one declared adaptation), promote behavior claims with anchored evidence,
# decide them under BOTH autonomy postures (human-gated accepts with reasons;
# the verification claim auto-accepted by policy with zero human minutes),
# run the differential harness Rust-vs-C# over the shared corpus, render the
# Atlas, then advance the source to delta d2 and watch the cone light up.
#
# Artifacts land in showcase/m1/: atlas-base.html, atlas-after-d2.html,
# PORTING.md (the KP-0 floor), ledger-export.jsonl (the golden ledger),
# advance-d2.md, and NOTEBOOK.md is written by hand from these outputs.
#
# Rerun from a clean tree:  pwsh showcase/m1/run-m1.ps1
param([switch]$SkipBuild)
$ErrorActionPreference = 'Stop'

$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$fx = Join-Path $root 'fixtures\slice-zero'
$ws = Join-Path $fx 'workspace\m1'
$out = $PSScriptRoot

function Invoke-Kp {
    & $script:kp @args
    if ($LASTEXITCODE -ne 0) { throw "kp $($args -join ' ') failed with exit code $LASTEXITCODE" }
}

# ---- 0. Build -------------------------------------------------------------------------------
if (-not $SkipBuild) {
    dotnet build (Join-Path $root 'FireHorseCoding.slnx') -v q --nologo | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'solution build failed' }
    dotnet build (Join-Path $fx 'csharp\HeadScan.Cases\HeadScan.Cases.csproj') -v q --nologo | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'HeadScan.Cases build failed' }
}
$kp = Join-Path $root 'KodePorter\src\KodePorter.Cli\bin\Debug\net10.0\KodePorter.Cli.exe'
if (-not (Test-Path $kp)) { throw "kp not found at $kp" }

# ---- 1. Fresh workspace ---------------------------------------------------------------------
if (Test-Path $ws) { Remove-Item -Recurse -Force $ws }
Invoke-Kp init --workspace $ws --name 'slice-zero' --source-root (Join-Path $fx 'rust') --target-root (Join-Path $fx 'csharp')

# ---- 2. Pin + map both sides at base --------------------------------------------------------
Invoke-Kp pin --workspace $ws --side source --root (Join-Path $fx 'rust') --label base --analyzer 'rust-syn@2'
Invoke-Kp map --workspace $ws --side source --label base --dump (Join-Path $fx 'maps\rust-dump-base.json')
Invoke-Kp pin --workspace $ws --side target --root (Join-Path $fx 'csharp') --label base --analyzer 'roslyn@5.6'
Invoke-Kp map --workspace $ws --side target --label base

# ---- 3. The migration unit and its dossier --------------------------------------------------
Invoke-Kp unit new --workspace $ws --id parser-core --name 'Header parser core' `
    --source-anchors 'headscan::parse,headscan::type_value' `
    --target-anchors 'HeadScan.HeaderParser.Parse(string)'

# ---- 4. Typed correspondences (ground-truth.yaml section) -----------------------------------
Invoke-Kp corr add --workspace $ws --id corr-implements --type implements --unit parser-core `
    --source 'headscan' --target 'HeadScan.HeaderParser' `
    --note 'The headscan crate parsing surface implements parser-core; HeadScan.HeaderParser realizes it.'
Invoke-Kp corr add --workspace $ws --id corr-parse --type maps-to --unit parser-core `
    --source 'headscan::parse' --target 'HeadScan.HeaderParser.Parse(string)' `
    --note 'The single entry point both harnesses call.'
Invoke-Kp corr add --workspace $ws --id corr-errorcode --type maps-to --unit parser-core `
    --source 'headscan::ErrorCode' --target 'HeadScan.ErrorCode' `
    --note 'The closed error-code set, as an enum on each side.'
Invoke-Kp corr add --workspace $ws --id corr-adapt-result --type adapts --unit parser-core `
    --divergence-kind adaptation `
    --source 'headscan::parse' --target 'HeadScan.HeaderParser.Parse(string)' `
    --note 'Result<HeaderDoc,ParseError> adapts to the C# ParseResult ok/error union - systematic, policy-level.'
Invoke-Kp corr add --workspace $ws --id corr-covers --type covers --unit parser-core `
    --criterion io-agreement-v1 `
    --note 'Differential run over corpus/cases.jsonl backs the unit claims.'

# ---- 5. Behavior claims with anchored evidence (ground-truth B1/B2/B3) ----------------------
Invoke-Kp claim add --workspace $ws --unit parser-core --id B1 --predicate kp.behavior `
    --value 'Duplicate keys: first occurrence wins.' --anchors 'headscan::parse'
Invoke-Kp claim add --workspace $ws --unit parser-core --id B2 --predicate kp.behavior `
    --value 'Ratio values within 1e-9 above 1 clamp to 1.0.' --anchors 'headscan::type_value'
Invoke-Kp claim add --workspace $ws --unit parser-core --id B3 --predicate kp.behavior `
    --value 'Keys are case-sensitive (Key and key are different keys, not duplicates).' --anchors 'headscan::parse'

# ---- 6. Decisions - the gated posture (human, with reasons); B3 deliberately left proposed --
Invoke-Kp decide --workspace $ws --subject 'behavior:parser-core:B1' --verdict accept `
    --reason 'Verified against rust/src/lib.rs duplicate resolution and the duplicate-two golden.'
Invoke-Kp decide --workspace $ws --subject 'behavior:parser-core:B2' --verdict accept `
    --reason 'Verified against type_value clamp logic and the ratio-clamp-edge golden.'

# ---- 7. Differential verification - the delegated posture (zero human minutes) --------------
# The policy (kp-default@1) auto-accepts a green kp.verification claim; the decision actor in
# the ledger is policy:kp-default@1, not a human.
# Absolute paths: the harness's child-process cwd is the workspace parent, which is NOT the
# fixture root when the workspace is nested (found during the first M1 run — cargo silently
# died looking for rust/Cargo.toml and the stdin pipe closed).
$sourceCmd = "cargo run --quiet --manifest-path `"$(Join-Path $fx 'rust\Cargo.toml')`" --bin headscan-cases"
$targetCmd = "`"$(Join-Path $fx 'csharp\HeadScan.Cases\bin\Debug\net10.0\HeadScan.Cases.exe')`""
Invoke-Kp verify run --workspace $ws --unit parser-core --cases (Join-Path $fx 'corpus\cases.jsonl') `
    --source-cmd $sourceCmd --target-cmd $targetCmd

# ---- 8. The Atlas at base -------------------------------------------------------------------
Invoke-Kp status --workspace $ws
Invoke-Kp atlas --workspace $ws --out (Join-Path $out 'atlas-base.html')

# ---- 9. Advance the source to d2 and watch the cone light up --------------------------------
& (Join-Path $fx 'tools\apply-delta.ps1') -Delta d2
Invoke-Kp advance --workspace $ws --side source --root (Join-Path $fx 'workspace\checkouts\rust-d2') `
    --label d2 --dump (Join-Path $fx 'maps\rust-dump-d2.json')
Invoke-Kp status --workspace $ws
Invoke-Kp atlas --workspace $ws --out (Join-Path $out 'atlas-after-d2.html')

# ---- 10. The KP-0 floor + the golden ledger --------------------------------------------------
Invoke-Kp export --workspace $ws --out (Join-Path $out 'PORTING.md')
Copy-Item (Join-Path $ws 'runs\advance-d2.md') (Join-Path $out 'advance-d2.md') -Force
$verifyNotebook = Get-ChildItem (Join-Path $ws 'runs') -Filter 'verify-parser-core-*.md' | Select-Object -First 1
if ($verifyNotebook) { Copy-Item $verifyNotebook.FullName (Join-Path $out 'verify-run.md') -Force }

# Golden ledger: the canonical export of everything this story appended.
Invoke-Kp export-ledger --workspace $ws --out (Join-Path $out 'ledger-export.jsonl')

Write-Host ''
Write-Host "M1 story complete. Artifacts in $out"
