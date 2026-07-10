<#
.SYNOPSIS
    Materializes a Slice Zero delta as a standalone `headscan` crate checkout.

.DESCRIPTION
    Copies the pristine fixtures/slice-zero/rust/ crate (Cargo.toml + src/,
    excluding build artifacts and any lockfile) into a fresh checkout
    directory, then applies the named delta patch from fixtures/slice-zero/deltas/
    on top of it with `git apply`.

    The patches under deltas/ were generated with:
        git diff --no-index --no-color rust-base rust-<delta>
    where `rust-base` and `rust-<delta>` were sibling directories (each a
    direct copy of the crate root, i.e. each directly containing src/,
    Cargo.toml, ...). That means every path in the patch looks like
    `a/rust-base/src/lib.rs` / `b/rust-d2/src/lib.rs` — two leading path
    components (the a/ or b/ marker, then the directory name) ahead of the
    real crate-relative path. Applying with `git apply -p2` strips exactly
    those two components, leaving crate-relative paths (`src/lib.rs`) that
    resolve correctly against the checkout root. This script always applies
    with `-p2` for that reason — do not change it without regenerating the
    patches with a matching directory-naming convention.

    `git apply` resolves the (post `-p`) paths in a patch relative to the
    top of the enclosing git working tree, not relative to the current
    directory — a checkout under fixtures/slice-zero/workspace/ sits inside
    this repo, so a naive `git apply` run from within it silently no-ops
    ("Skipped patch") instead of erroring. To sidestep that, this script
    `git init`s the checkout directory as its own throwaway repository
    before applying (making its own root the apply root), then deletes the
    `.git` it created so the checkout is left as a plain directory.

.PARAMETER Delta
    Which delta to apply: d1, d2, or d3.

.PARAMETER OutDir
    Destination checkout directory. Defaults to
    fixtures/slice-zero/workspace/checkouts/rust-<delta>. If it already
    exists it is deleted and recreated.

.EXAMPLE
    ./apply-delta.ps1 -Delta d1

.EXAMPLE
    ./apply-delta.ps1 -Delta d2 -OutDir C:\scratch\rust-d2-checkout

.NOTES
    Works when invoked from anywhere: all paths are resolved relative to
    this script's own location ($PSScriptRoot), not the caller's working
    directory.

    Equivalent manual apply command, run from inside the checkout directory
    (which must directly contain src/, Cargo.toml, ... AND must not be
    nested inside another git working tree — see DESCRIPTION):
        git init -q
        git apply -p2 --whitespace=nowarn <fixture>/deltas/<delta>.patch
        Remove-Item -Recurse -Force .git
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('d1', 'd2', 'd3')]
    [string]$Delta,

    [string]$OutDir
)

$ErrorActionPreference = 'Stop'

$fixtureRoot = Split-Path -Parent $PSScriptRoot
$rustSrc = Join-Path $fixtureRoot 'rust'
$deltasDir = Join-Path $fixtureRoot 'deltas'

$patchNames = @{
    'd1' = 'd1-benign-refactor.patch'
    'd2' = 'd2-duplicate-policy.patch'
    'd3' = 'd3-new-output-field.patch'
}
$patchPath = Join-Path $deltasDir $patchNames[$Delta]
if (-not (Test-Path -LiteralPath $patchPath)) {
    throw "Patch file not found: $patchPath"
}

if (-not $OutDir) {
    $OutDir = Join-Path $fixtureRoot "workspace/checkouts/rust-$Delta"
}

if (Test-Path -LiteralPath $OutDir) {
    Remove-Item -LiteralPath $OutDir -Recurse -Force -Confirm:$false
}
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

# Copy the pristine crate: Cargo.toml + src/ only. Deliberately excludes
# target/ (build artifacts) and Cargo.lock (a lockfile did not exist in the
# tree the patches were generated from; applying against a checkout that
# already has one is fine too, since the patches never touch it).
Copy-Item -LiteralPath (Join-Path $rustSrc 'Cargo.toml') -Destination $OutDir
Copy-Item -LiteralPath (Join-Path $rustSrc 'src') -Destination $OutDir -Recurse

Write-Host "Copied rust/ -> $OutDir"

$patchPathAbs = (Resolve-Path -LiteralPath $patchPath).ProviderPath

Push-Location $OutDir
try {
    # Make $OutDir its own throwaway git repo so `git apply` resolves the
    # patch's (post -p2) paths relative to $OutDir itself, not the top of
    # whatever outer working tree $OutDir happens to be nested inside (see
    # DESCRIPTION above).
    & git init -q
    if ($LASTEXITCODE -ne 0) {
        throw "git init failed in '$OutDir' (exit $LASTEXITCODE)"
    }
    try {
        & git apply -p2 --whitespace=nowarn $patchPathAbs
        if ($LASTEXITCODE -ne 0) {
            throw "git apply failed for delta '$Delta' (exit $LASTEXITCODE)"
        }
    } finally {
        Remove-Item -LiteralPath (Join-Path $OutDir '.git') -Recurse -Force -Confirm:$false -ErrorAction SilentlyContinue
    }
} finally {
    Pop-Location
}

Write-Host "Applied $($patchNames[$Delta]) -> $OutDir"
