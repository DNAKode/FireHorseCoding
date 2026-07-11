# FrankenTui read-only probe (roadmap section 10.5 M2-prime, "probe" stage; K2b).
#
# Pure reconnaissance over the real FrankenTui/FrankenTui.NET brownfield pair: pin + cartograph
# both sides at their current commits, infer candidate correspondences (never silently accepted),
# capture Health v2, and render the Port Atlas. Strictly read-only against C:\Work\FrankenTui.Net
# and its .external\frankentui vendored upstream: only 'git log/status/rev-parse' and read-only
# file globbing (RustSynProvider reads a pre-generated dump; CSharpRoslynProvider only parses
# syntax trees in-proc) ever touch that tree. No 'dotnet build'/'cargo build' runs inside
# FrankenTui.Net; the rust-map-dump tool's own build happens inside FireHorseCoding's fixtures
# tree (fixtures/slice-zero/tools/rust-map-dump), reading FrankenTui.Net's vendored sources only.
#
# Artifacts land in flagships/frankentui/: atlas-probe.html, PROBE-REPORT.md (written by hand from
# this run's captured output), and workspace/ (gitignored: kp/ map+ledger dbs, rust-dump.json,
# probe-log.txt). The Atlas is also copied to showcase/m2/frankentui-atlas-probe.html.
#
# Rerun from a clean tree:  pwsh flagships/frankentui/run-probe.ps1
param(
    [switch]$SkipBuild,
    [string]$FrankenTuiNetRoot = 'C:\Work\FrankenTui.Net'
)
$ErrorActionPreference = 'Stop'

$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$fx = Join-Path $root 'fixtures\slice-zero'
$ftui = $FrankenTuiNetRoot
$ftuiExternal = Join-Path $ftui '.external\frankentui'
$wsDir = Join-Path $PSScriptRoot 'workspace'
$ws = Join-Path $wsDir 'kp'
$log = Join-Path $wsDir 'probe-log.txt'
$out = $PSScriptRoot

if (-not (Test-Path $ftui)) { throw "FrankenTui.Net not found at $ftui" }
if (-not (Test-Path $ftuiExternal)) { throw ".external/frankentui not found under $ftui" }

New-Item -ItemType Directory -Force -Path $wsDir | Out-Null
Set-Content -Path $log -Value "FrankenTui probe run - $(Get-Date -Format o)"

function Write-Log {
    param([string]$Message)
    Write-Host $Message
    Add-Content -Path $log -Value $Message
}

function Invoke-Kp {
    & $script:kp @args
    if ($LASTEXITCODE -ne 0) { throw "kp $($args -join ' ') failed with exit code $LASTEXITCODE" }
}

# Runs kp, capturing stdout lines AND exit code, logging both, and timing wall clock.
function Invoke-KpTimed {
    param([string]$StepName, [string[]]$KpArgs)
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $lines = & $script:kp @KpArgs
    $sw.Stop()
    if ($LASTEXITCODE -ne 0) {
        Write-Log "FAILED: kp $($KpArgs -join ' ') (exit $LASTEXITCODE)"
        $lines | ForEach-Object { Write-Log $_ }
        throw "$StepName failed with exit code $LASTEXITCODE"
    }
    Write-Log "`n== $StepName =="
    $lines | ForEach-Object { Write-Log $_ }
    Write-Log "$StepName wall time: $($sw.Elapsed)"
    return [pscustomobject]@{ Lines = $lines; Elapsed = $sw.Elapsed }
}

# ---- 0. Build (FireHorseCoding + the rust-map-dump tool only; never FrankenTui.Net) ----------
if (-not $SkipBuild) {
    Write-Log "== Build =="
    dotnet build (Join-Path $root 'FireHorseCoding.slnx') -v q --nologo | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'FireHorseCoding solution build failed' }
}
$kp = Join-Path $root 'KodePorter\src\KodePorter.Cli\bin\Debug\net10.0\KodePorter.Cli.exe'
if (-not (Test-Path $kp)) { throw "kp not found at $kp" }

# ---- 1. Provenance (read-only git queries; no writes inside FrankenTui.Net) -------------------
Write-Log "`n== Provenance =="
$ftuiHead = (git -C $ftui rev-parse HEAD).Trim()
$ftuiDirtyCount = (git -C $ftui status --porcelain | Measure-Object).Count
Write-Log "FrankenTui.Net HEAD: $ftuiHead"
Write-Log "FrankenTui.Net dirty entries (git status --porcelain count): $ftuiDirtyCount"

$hasGitmodules = Test-Path (Join-Path $ftui '.gitmodules')
$externalDotGit = Join-Path $ftuiExternal '.git'
$externalIsGitDir = Test-Path $externalDotGit -PathType Container
$externalIsGitFile = Test-Path $externalDotGit -PathType Leaf
Write-Log ".gitmodules present in FrankenTui.Net: $hasGitmodules"
Write-Log ".external/frankentui/.git is a directory (own git repo): $externalIsGitDir"
Write-Log ".external/frankentui/.git is a file (submodule gitlink): $externalIsGitFile"

if ($externalIsGitDir) {
    $extHead = (git -C $ftuiExternal rev-parse HEAD).Trim()
    $extDirtyCount = (git -C $ftuiExternal status --porcelain | Measure-Object).Count
    $extRemotes = (git -C $ftuiExternal remote -v) -join ' | '
    Write-Log ".external/frankentui HEAD: $extHead"
    Write-Log ".external/frankentui dirty entries: $extDirtyCount"
    Write-Log ".external/frankentui remotes: $extRemotes"
}

$provenanceMd = Join-Path $ftui 'PROVENANCE.md'
if (Test-Path $provenanceMd) {
    $bootstrapLine = Select-String -Path $provenanceMd -Pattern 'Bootstrap reference commit' | Select-Object -First 1
    if ($bootstrapLine) { Write-Log "PROVENANCE.md marker: $($bootstrapLine.Line.Trim())" }
}

# ---- 2. Rust dump (build happens only inside fixtures/slice-zero/tools/rust-map-dump) ---------
Write-Log "`n== Rust dump =="
$dumpManifest = Join-Path $fx 'tools\rust-map-dump\Cargo.toml'
$dumpOut = Join-Path $wsDir 'rust-dump.json'
$dumpStderr = Join-Path $wsDir 'rust-dump.stderr.log'

$swBuild = [System.Diagnostics.Stopwatch]::StartNew()
& cargo build --release --manifest-path $dumpManifest 2>&1 | Tee-Object -FilePath $dumpStderr | Out-Host
$swBuild.Stop()
if ($LASTEXITCODE -ne 0) { throw "cargo build of rust-map-dump failed; see $dumpStderr" }
Write-Log "rust-map-dump cargo build wall time: $($swBuild.Elapsed)"

$swRun = [System.Diagnostics.Stopwatch]::StartNew()
& cargo run --release --manifest-path $dumpManifest -- $ftuiExternal 2>>$dumpStderr | Out-File -FilePath $dumpOut -Encoding utf8
$swRun.Stop()
if ($LASTEXITCODE -ne 0) { throw "rust-map-dump run failed; see $dumpStderr" }

$dumpJson = Get-Content $dumpOut -Raw | ConvertFrom-Json
$dumpEntityCount = $dumpJson.entities.Count
Write-Log "rust-map-dump run wall time: $($swRun.Elapsed)"
Write-Log "rust-map-dump entities: $dumpEntityCount"
Write-Log "rust-map-dump provider: $($dumpJson.provider)"

# ---- 3. kp init -------------------------------------------------------------------------------
Write-Log "`n== kp init =="
if (Test-Path $ws) { Remove-Item -Recurse -Force $ws }
Invoke-Kp init --workspace $ws --name 'frankentui' --source-root $ftuiExternal --target-root $ftui

# ---- 4. Pin + map both sides ------------------------------------------------------------------
Invoke-Kp pin --workspace $ws --side source --root $ftuiExternal --label base --analyzer 'rust-syn@2'
$mapSource = Invoke-KpTimed -StepName 'kp map source' -KpArgs @('map', '--workspace', $ws, '--side', 'source', '--label', 'base', '--dump', $dumpOut)

Invoke-Kp pin --workspace $ws --side target --root $ftui --label base --analyzer 'roslyn@5.6'
$mapTarget = Invoke-KpTimed -StepName 'kp map target' -KpArgs @('map', '--workspace', $ws, '--side', 'target', '--label', 'base')

# ---- 5. Candidate inference --------------------------------------------------------------------
$infer = Invoke-KpTimed -StepName 'kp candidates infer' -KpArgs @('candidates', 'infer', '--workspace', $ws)

# ---- 6. Status (Health v2) ----------------------------------------------------------------------
$status = Invoke-KpTimed -StepName 'kp status' -KpArgs @('status', '--workspace', $ws)

# ---- 7. Atlas -------------------------------------------------------------------------------------
Write-Log "`n== Atlas =="
$atlasOut = Join-Path $out 'atlas-probe.html'
$swAtlas = [System.Diagnostics.Stopwatch]::StartNew()
Invoke-Kp atlas --workspace $ws --out $atlasOut
$swAtlas.Stop()
Write-Log "kp atlas wall time: $($swAtlas.Elapsed)"

$atlasBytes = (Get-Item $atlasOut).Length
Write-Log "Atlas size: $atlasBytes bytes ($([math]::Round($atlasBytes/1MB, 2)) MB)"

$atlasText = Get-Content $atlasOut -Raw
$hasOverview = $atlasText -match 'Overview'
$hasSvg = $atlasText -match '<svg'
$dataIslandMatch = [regex]::Match($atlasText, '<script type="application/json"[^>]*>(.*?)</script>', 'Singleline')
$dataIslandParses = $false
if ($dataIslandMatch.Success) {
    try { $null = $dataIslandMatch.Groups[1].Value | ConvertFrom-Json; $dataIslandParses = $true } catch { $dataIslandParses = $false }
}
Write-Log "Atlas sanity: hasOverviewText=$hasOverview hasSvg=$hasSvg dataIslandParses=$dataIslandParses"

$showcaseDir = Join-Path $root 'showcase\m2'
New-Item -ItemType Directory -Force -Path $showcaseDir | Out-Null
Copy-Item $atlasOut (Join-Path $showcaseDir 'frankentui-atlas-probe.html') -Force

Write-Host ''
Write-Host "Probe complete. Artifacts in $out (workspace + log under $wsDir)."
Write-Host "Write flagships/frankentui/PROBE-REPORT.md by hand from $log and the workspace outputs."
