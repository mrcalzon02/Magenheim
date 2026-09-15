[CmdletBinding()]
param(
    [string]$ProfileRoot = (Join-Path $env:APPDATA 'r2modmanPlus-local/Valheim/profiles/Central Fuckery'),
    [string]$GameRoot = (Split-Path $PSScriptRoot -Parent),
    [switch]$Offline
)
$ErrorActionPreference = 'Stop'
# Closeout includes tests, packaging, live installation and launcher catalog verification.
& (Join-Path $PSScriptRoot 'install-local.ps1') -ProfileRoot $ProfileRoot -GameRoot $GameRoot -Offline:$Offline
Write-Output 'Testing closeout complete. Open r2modman and confirm the verified version and release description before Launch Modded.'
