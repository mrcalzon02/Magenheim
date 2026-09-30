[CmdletBinding()]
param([switch]$Offline)
$ErrorActionPreference = 'Stop'

Write-Host '=== Magenheim local test preparation ==='
Write-Host 'Compiling current code and packaging committed runtime assets. No asset regeneration or production forge is run.'
& (Join-Path $PSScriptRoot 'closeout.ps1') -Offline:$Offline
if ($LASTEXITCODE -ne 0) { throw 'Local test preparation failed.' }
Write-Host 'LOCAL TEST PREP COMPLETE: reopen r2modman and Launch Modded.'
