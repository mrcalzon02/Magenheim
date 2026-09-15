function Get-MagenheimLauncherUpdate {
    param([string]$Text, $Manifest, [long]$InstalledAt)
    # Edit only our existing top-level record. Other records remain byte-for-byte intact.
    $records = [regex]::Matches($Text, '(?ms)^- manifestVersion:.*?(?=^- manifestVersion:|\z)')
    $matches = @($records | Where-Object { $_.Value -match '(?m)^  name: Local-Magenheim\r?$' })
    if ($matches.Count -ne 1) { throw 'Expected exactly one Local-Magenheim launcher entry. Import the package once through r2modman first.' }
    $record = $matches[0]
    $updated = $record.Value
    $newline = if ($updated.Contains("`r`n")) { "`r`n" } else { "`n" }
    $version = [version]$Manifest.version_number
    $description = ([string]$Manifest.description).Replace("'", "''")
    $website = ([string]$Manifest.website_url).Replace("'", "''")
    if ($description -match '[\r\n]' -or $website -match '[\r\n]') { throw 'Launcher descriptions and URLs must be single-line values.' }
    $fields = [ordered]@{
        description = "  description: '$description'$newline"
        websiteUrl = "  websiteUrl: '$website'$newline"
        installedAtTime = "  installedAtTime: $InstalledAt$newline"
        dependencies = "  dependencies:$newline" + (($Manifest.dependencies | ForEach-Object {
            "    - '" + ([string]$_).Replace("'", "''") + "'$newline"
        }) -join '')
        versionNumber = "  versionNumber:$newline    major: $($version.Major)$newline    minor: $($version.Minor)$newline    patch: $($version.Build)$newline"
    }
    foreach ($field in $fields.Keys) {
        $pattern = '(?m)^  ' + [regex]::Escape($field) + ':[^\r\n]*\r?\n(?:^    [^\r\n]*\r?\n)*'
        $found = [regex]::Matches($updated, $pattern)
        if ($found.Count -ne 1) { throw "Expected exactly one launcher field: $field" }
        $match = $found[0]
        $updated = $updated.Substring(0, $match.Index) + $fields[$field] + $updated.Substring($match.Index + $match.Length)
    }
    if ($updated -notmatch '(?m)^  enabled: true\r?$') { throw 'Magenheim is disabled in the launcher. Enable it before testing closeout.' }
    return $Text.Substring(0, $record.Index) + $updated + $Text.Substring($record.Index + $record.Length)
}
