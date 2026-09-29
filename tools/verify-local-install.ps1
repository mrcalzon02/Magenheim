[CmdletBinding()]
param(
    [string]$ProfilesRoot = (Join-Path $env:APPDATA 'r2modmanPlus-local/Valheim/profiles'),
    [string]$ExpectedProfile = 'Central Fuckery'
)
$ErrorActionPreference = 'Stop'

$pluginSource = Join-Path (Split-Path $PSScriptRoot -Parent) 'src/Magenheim.Runtime/MagenheimPlugin.cs'
$versionMatch = Select-String -LiteralPath $pluginSource -Pattern 'internal const string PluginVersion = "([^"]+)";' | Select-Object -First 1
if (!$versionMatch -or $versionMatch.Matches.Count -eq 0) { throw "Unable to read expected Magenheim version from $pluginSource" }
$expectedVersion = $versionMatch.Matches[0].Groups[1].Value
$checkpointMatch = Select-String -LiteralPath $pluginSource -Pattern 'checkpoint ([A-Za-z0-9-]+) loaded from' | Select-Object -First 1
if (!$checkpointMatch -or $checkpointMatch.Matches.Count -eq 0) { throw "Unable to read runtime checkpoint from $pluginSource" }
$expectedCheckpoint = $checkpointMatch.Matches[0].Groups[1].Value

if (!(Test-Path -LiteralPath $ProfilesRoot -PathType Container)) {
    throw "r2modman Valheim profiles root does not exist: $ProfilesRoot"
}

$rows = @()
foreach ($profileDir in Get-ChildItem -LiteralPath $ProfilesRoot -Directory) {
    foreach ($dll in Get-ChildItem -LiteralPath (Join-Path $profileDir.FullName 'BepInEx/plugins') -Filter Magenheim.dll -File -Recurse -ErrorAction SilentlyContinue) {
        $version = [Reflection.AssemblyName]::GetAssemblyName($dll.FullName).Version.ToString(3)
        $rows += [pscustomobject]@{
            Profile = $profileDir.Name
            Version = $version
            Expected = ($profileDir.Name -eq $ExpectedProfile -and $version -eq $expectedVersion)
            SHA256 = (Get-FileHash -LiteralPath $dll.FullName -Algorithm SHA256).Hash
            Path = $dll.FullName
        }
    }
}

if ($rows.Count -eq 0) {
    throw "No Magenheim.dll exists under any r2modman Valheim profile in $ProfilesRoot"
}

$rows | Sort-Object Profile, Path | Format-Table -AutoSize

$expectedRows = @($rows | Where-Object { $_.Profile -eq $ExpectedProfile })
if ($expectedRows.Count -ne 1) {
    throw "Expected exactly one Magenheim.dll in profile '$ExpectedProfile'; found $($expectedRows.Count)."
}
if ($expectedRows[0].Version -ne $expectedVersion) {
    throw "STALE INSTALL: source expects Magenheim $expectedVersion but '$ExpectedProfile' contains $($expectedRows[0].Version) at $($expectedRows[0].Path)"
}

$otherCurrent = @($rows | Where-Object { $_.Profile -ne $ExpectedProfile -and $_.Version -eq $expectedVersion })
$otherStale = @($rows | Where-Object { $_.Profile -ne $ExpectedProfile -and $_.Version -ne $expectedVersion })
Write-Output "INSTALL PROOF PASS: $ExpectedProfile contains Magenheim $expectedVersion."
Write-Output "Expected startup line: Runtime image $expectedVersion checkpoint $expectedCheckpoint loaded from '<path>'."
if ($otherStale.Count -gt 0) {
    Write-Warning ("Other Valheim profiles contain stale Magenheim DLLs. Launching one of them will reproduce old runtime errors: " +
        (($otherStale | ForEach-Object { "$($_.Profile)=$($_.Version)" }) -join ', '))
}
if ($otherCurrent.Count -gt 0) {
    Write-Output ("Other profiles also contain current Magenheim ${expectedVersion}: " +
        (($otherCurrent | ForEach-Object { $_.Profile }) -join ', '))
}
