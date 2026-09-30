[CmdletBinding()]
param(
    [switch]$Offline
)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$python = (Get-Command python -ErrorAction Stop).Source

Write-Host '=== Magenheim local test preparation ==='
Write-Host 'Checking committed/generated asset freshness before build...'
& $python "$root/tools/verify-generated-freshness.py"
if ($LASTEXITCODE -ne 0) {
    Write-Warning 'Generated production assets are incomplete or stale. Running the one-run Underworld production forge locally.'
    Write-Warning 'This legitimately regenerates production assets and freshness provenance in the working tree; it may take a while.'
    & "$root/tools/rebuild-underworld-production.ps1" -SkipReview

    Write-Host 'Rechecking global generated-asset freshness after production forge...'
    & $python "$root/tools/verify-generated-freshness.py"
    if ($LASTEXITCODE -ne 0) {
        throw 'Generated asset freshness still fails after the one-run production forge.'
    }
}

Write-Host 'Production assets are fresh. Building, packaging, installing, and verifying the local r2modman profile...'
& "$root/closeout.ps1" -Offline:$Offline
Write-Host 'LOCAL TEST PREP COMPLETE: reopen r2modman and Launch Modded only after the launcher/version proof lines above.'
