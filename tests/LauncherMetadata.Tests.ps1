$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/../tools/launcher-metadata.ps1"
$other = "- manifestVersion: 1`n  name: Other-Mod`n  description: 'Leave this alone: v0.0.1'`n  enabled: false`n"
$owned = @'
- manifestVersion: 1
  name: Local-Magenheim
  websiteUrl: ''
  description: >-
    Old multi-line
    description.
  installedAtTime: 1
  dependencies:
    - Old-Dependency-1.0.0
  versionNumber:
    major: 0
    minor: 0
    patch: 1
  enabled: true
  onlineSource: false
'@ + "`n"
$manifest = [pscustomobject]@{
    version_number = '0.0.46'; description = "Current: player's skill is visible."
    website_url = 'https://example.com'; dependencies = @('Owner-Dependency-2.0.0')
}
foreach ($newline in @("`n", "`r`n")) {
    $inputText = ($other + $owned + $other).Replace("`r`n", "`n").Replace("`n", $newline)
    $result = Get-MagenheimLauncherUpdate $inputText $manifest 12345
    $otherExpected = $other.Replace("`n", $newline)
    if (!$result.StartsWith($otherExpected) -or !$result.EndsWith($otherExpected)) { throw 'Foreign records changed.' }
    if (!$result.Contains("  description: 'Current: player''s skill is visible.'") -or
        !$result.Contains('    patch: 46') -or !$result.Contains('  installedAtTime: 12345') -or
        !$result.Contains("    - 'Owner-Dependency-2.0.0'") -or $result.Contains('Old-Dependency')) { throw 'Metadata update failed.' }
    if ((Get-MagenheimLauncherUpdate $result $manifest 12345) -cne $result) { throw 'Update is not idempotent.' }
}
foreach ($bad in @($other, ($owned + $owned), $owned.Replace('enabled: true', 'enabled: false'), $owned.Replace('versionNumber:', 'missingVersion:'))) {
    $rejected = $false
    try { Get-MagenheimLauncherUpdate $bad $manifest 12345 | Out-Null } catch { $rejected = $true }
    if (!$rejected) { throw 'Ambiguous, missing or disabled entry was accepted.' }
}
Write-Output 'Launcher metadata tests passed: LF/CRLF, foreign preservation, quoting, dependencies, version, idempotence and invalid-entry rejection.'
