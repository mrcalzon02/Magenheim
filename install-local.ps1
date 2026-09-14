[CmdletBinding()]
param([string]$ProfileRoot = (Join-Path $env:APPDATA 'r2modmanPlus-local/Valheim/profiles/Central Fuckery'))
$ErrorActionPreference = 'Stop'
if (Get-Process valheim -ErrorAction SilentlyContinue) { throw 'Exit Valheim before installing.' }
$profile = (Resolve-Path -LiteralPath $ProfileRoot).Path
if (!(Test-Path -LiteralPath "$profile/BepInEx/core/BepInEx.dll")) { throw 'The selected profile has no BepInEx loader.' }
$source = Join-Path $PSScriptRoot 'dist/Local-Magenheim-0.0.16'
if (!(Test-Path -LiteralPath "$source/Magenheim/Magenheim.dll")) { throw 'Run build.ps1 first.' }
$target = Join-Path $profile 'BepInEx/plugins/Local-Magenheim'
$otherCopies = Get-ChildItem -LiteralPath "$profile/BepInEx/plugins" -Filter Magenheim.dll -Recurse |
    Where-Object { !$_.FullName.StartsWith($target + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) }
if ($otherCopies) { throw "Another Magenheim copy exists: $($otherCopies.FullName -join ', ')" }
if (Test-Path -LiteralPath $target) {
    $backup = Join-Path $PSScriptRoot ('backups/Local-Magenheim-' + (Get-Date -Format yyyyMMdd-HHmmss) + '.zip')
    New-Item -ItemType Directory -Force -Path (Split-Path $backup -Parent) | Out-Null
    Compress-Archive -Path "$target/*" -DestinationPath $backup
    Write-Output "Previous installation backed up: $backup"
}
New-Item -ItemType Directory -Force -Path $target | Out-Null
Copy-Item -Path "$source/*" -Destination $target -Recurse -Force
foreach ($file in Get-ChildItem -LiteralPath $source -File -Recurse) {
    $relative = $file.FullName.Substring($source.Length).TrimStart('\','/')
    $installed = Join-Path $target $relative
    if ((Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath $installed).Hash) {
        throw "Installed file verification failed: $relative"
    }
}
Write-Output "Installed and hash-verified: $target"
