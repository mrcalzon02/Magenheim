[CmdletBinding()]
param(
    [string]$ProfileRoot = (Join-Path $env:APPDATA 'r2modmanPlus-local/Valheim/profiles/Central Fuckery'),
    [string]$GameRoot = (Split-Path $PSScriptRoot -Parent),
    [switch]$SkipBuild
)
$ErrorActionPreference = 'Stop'

if (Get-Process valheim -ErrorAction SilentlyContinue) { throw 'Exit Valheim before installing.' }
$profile = (Resolve-Path -LiteralPath $ProfileRoot).Path
if (!(Test-Path -LiteralPath "$profile/BepInEx/core/BepInEx.dll")) { throw 'The selected profile has no BepInEx loader.' }

$pluginSource = Join-Path $PSScriptRoot 'src/Magenheim.Runtime/MagenheimPlugin.cs'
$pluginVersionLine = Select-String -LiteralPath $pluginSource -Pattern 'internal const string PluginVersion = "([^"]+)";' | Select-Object -First 1
if (!$pluginVersionLine -or $pluginVersionLine.Matches.Count -eq 0) {
    throw "Unable to read PluginVersion from $pluginSource"
}
$pluginVersion = $pluginVersionLine.Matches[0].Groups[1].Value
if ([string]::IsNullOrWhiteSpace($pluginVersion)) { throw 'Resolved Magenheim plugin version is empty.' }

if (!$SkipBuild) {
    & (Join-Path $PSScriptRoot 'build.ps1') -ProfileRoot $profile -GameRoot $GameRoot
    if ($LASTEXITCODE -ne 0) { throw 'Magenheim build failed; installation was not modified.' }
}

$source = Join-Path $PSScriptRoot "dist/Local-Magenheim-$pluginVersion"
if (!(Test-Path -LiteralPath "$source/Magenheim/Magenheim.dll")) {
    throw "Built package for Magenheim $pluginVersion is missing. Run build.ps1 or omit -SkipBuild."
}
$manifestPath = Join-Path $source 'manifest.json'
if (!(Test-Path -LiteralPath $manifestPath)) { throw "Package manifest is missing: $manifestPath" }
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ([string]$manifest.version_number -ne $pluginVersion) {
    throw "Package/plugin version mismatch: manifest=$($manifest.version_number), source=$pluginVersion"
}

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
    if (!(Test-Path -LiteralPath $installed)) { throw "Installed file is missing: $relative" }
    if ((Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath $installed).Hash) {
        throw "Installed file verification failed: $relative"
    }
}

$sourceDllHash = (Get-FileHash -LiteralPath "$source/Magenheim/Magenheim.dll").Hash
$installedDllHash = (Get-FileHash -LiteralPath "$target/Magenheim/Magenheim.dll").Hash
if ($sourceDllHash -ne $installedDllHash) { throw 'Installed Magenheim.dll does not match the just-built package.' }

Write-Output "Installed and SHA-256 verified Magenheim $pluginVersion: $target"
Write-Output "Installed Magenheim.dll SHA256: $installedDllHash"
Write-Output "Expected startup diagnostic: Magenheim $pluginVersion"
