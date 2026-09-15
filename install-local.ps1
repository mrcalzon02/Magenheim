[CmdletBinding()]
param(
    [string]$ProfileRoot = (Join-Path $env:APPDATA 'r2modmanPlus-local/Valheim/profiles/Central Fuckery'),
    [string]$GameRoot = (Split-Path $PSScriptRoot -Parent),
    [switch]$SkipBuild,
    [switch]$Offline
)
$ErrorActionPreference = 'Stop'

if (Get-Process valheim -ErrorAction SilentlyContinue) { throw 'Exit Valheim before installing.' }
if (Get-Process | Where-Object ProcessName -match '^(r2modman|Thunderstore Mod Manager)$') {
    throw 'Close r2modman before installing so its cached catalog cannot overwrite the updated version.'
}
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
    & (Join-Path $PSScriptRoot 'build.ps1') -ProfileRoot $profile -GameRoot $GameRoot -Offline:$Offline
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

# Validate the completed package before backing up or writing any profile files.
$checksumPath = Join-Path $source 'checksums.json'
if (!(Test-Path -LiteralPath $checksumPath)) { throw 'Package checksums are missing; rebuild the package.' }
# Windows PowerShell writes a top-level JSON array to the pipeline as a single item, so
# wrapping the pipeline in @(...) yields one nested array instead of one entry per file.
# Every $entry.path then member-enumerates to all paths at once and the whole package fails
# verification. Bind the parse result to a variable first, then wrap that.
$parsedChecksums = Get-Content -LiteralPath $checksumPath -Raw | ConvertFrom-Json
$entries = @($parsedChecksums)
if ($entries.Count -eq 0) { throw 'Package checksums are empty; rebuild the package.' }
$seenPaths = @{}
foreach ($entry in $entries) {
    $relativePath = [string]$entry.path
    $checkedPath = [IO.Path]::GetFullPath((Join-Path $source $relativePath))
    if (!$checkedPath.StartsWith($source + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or
        $seenPaths.ContainsKey($checkedPath)) { throw "Invalid checksum path: $relativePath" }
    $seenPaths[$checkedPath] = $true
    if (!(Test-Path -LiteralPath $checkedPath -PathType Leaf) -or
        (Get-FileHash -LiteralPath $checkedPath).Hash -ne [string]$entry.sha256) {
        throw "Package integrity check failed: $relativePath"
    }
}
foreach ($file in Get-ChildItem -LiteralPath $source -File -Recurse) {
    if ($file.FullName -ne $checksumPath -and !$seenPaths.ContainsKey($file.FullName)) {
        throw "Unlisted package file: $($file.FullName)"
    }
}
$assemblyVersion = [Reflection.AssemblyName]::GetAssemblyName("$source/Magenheim/Magenheim.dll").Version
if ($assemblyVersion.ToString(3) -ne $pluginVersion) { throw 'Packaged assembly version does not match the manifest.' }

. (Join-Path $PSScriptRoot 'tools/launcher-metadata.ps1')
$catalogPath = Join-Path $profile 'mods.yml'
$catalogBefore = [IO.File]::ReadAllText($catalogPath)
$catalogAfter = Get-MagenheimLauncherUpdate -Text $catalogBefore -Manifest $manifest -InstalledAt ([DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds())

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

# Publish launcher metadata only after the payload has been verified.
if ((Get-Process | Where-Object ProcessName -match '^(r2modman|Thunderstore Mod Manager)$') -or
    [IO.File]::ReadAllText($catalogPath) -cne $catalogBefore) { throw 'Launcher catalog changed during install; close the launcher and rerun installation.' }
$catalogBackup = Join-Path $PSScriptRoot ('backups/mods-' + (Get-Date -Format yyyyMMdd-HHmmss-fff) + '.yml')
New-Item -ItemType Directory -Force -Path (Split-Path $catalogBackup -Parent) | Out-Null
$catalogTemp = $catalogPath + '.magenheim-' + [guid]::NewGuid().ToString('N') + '.tmp'
[IO.File]::WriteAllText($catalogTemp, $catalogAfter, [Text.UTF8Encoding]::new($false))
[IO.File]::Replace($catalogTemp, $catalogPath, $catalogBackup)
if ([IO.File]::ReadAllText($catalogPath) -cne $catalogAfter) { throw 'Launcher catalog read-back verification failed.' }

Write-Output "Installed and SHA-256 verified Magenheim ${pluginVersion}: $target"
Write-Output "Installed Magenheim.dll SHA256: $installedDllHash"
Write-Output "Expected startup diagnostic: Magenheim $pluginVersion"
Write-Output "Verified launcher entry: Magenheim v$pluginVersion by Local (enabled)"
Write-Output "Launcher description: $($manifest.description)"
Write-Output "Launcher catalog backup: $catalogBackup"
