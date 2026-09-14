param([string]$ModProfile = "$env:APPDATA\r2modmanPlus-local\Valheim\profiles\Central Fuckery", [string]$StagedFolder = 'compat-repair', [string[]]$OnlyPackages = @())
$ErrorActionPreference = 'Stop'
if (Get-Process valheim -ErrorAction SilentlyContinue) { throw 'Close Valheim before applying compatibility repairs' }
$project = Split-Path $PSScriptRoot -Parent
$staged = Join-Path $project ('dist\' + $StagedFolder)
$plugins = Join-Path $ModProfile 'BepInEx\plugins'
$backup = Join-Path $project ('backups\compat-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $backup -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $ModProfile 'BepInEx\LogOutput.log') -Destination (Join-Path $backup 'LogOutput-before.log')
$records = @()
foreach($file in Get-ChildItem -LiteralPath $staged -Recurse -Filter '*.dll') {
    $relative = $file.FullName.Substring($staged.Length + 1)
    if($OnlyPackages.Count -gt 0 -and $relative.Split('\')[0] -notin $OnlyPackages) { continue }
    $target = Join-Path $plugins $relative
    if (!(Test-Path -LiteralPath $target)) { throw "Original plugin not found: $target" }
    $original = Join-Path $backup $relative
    New-Item -ItemType Directory -Path (Split-Path $original -Parent) -Force | Out-Null
    Copy-Item -LiteralPath $target -Destination $original
    $records += [pscustomobject]@{Target=$target; Backup=$original; Before=(Get-FileHash -LiteralPath $target).Hash; After=(Get-FileHash -LiteralPath $file.FullName).Hash; Staged=$file.FullName}
}
$records | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $backup 'restore-manifest.json') -Encoding UTF8
foreach($record in $records) {
    if ((Get-FileHash -LiteralPath $record.Target).Hash -ne $record.Before) { throw 'Plugin changed during repair; stopped' }
    Copy-Item -LiteralPath $record.Staged -Destination $record.Target -Force
    if ((Get-FileHash -LiteralPath $record.Target).Hash -ne $record.After) { throw 'Deployment verification failed' }
}
Write-Output "Repaired $($records.Count) DLLs. Original files and restore manifest: $backup"
