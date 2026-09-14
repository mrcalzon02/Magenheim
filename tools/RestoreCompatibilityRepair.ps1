param([Parameter(Mandatory=$true)][string]$Manifest)
$ErrorActionPreference = 'Stop'
if (Get-Process valheim -ErrorAction SilentlyContinue) { throw 'Close Valheim before restoring' }
$records = Get-Content -LiteralPath $Manifest -Raw | ConvertFrom-Json
foreach($r in $records) {
    if((Get-FileHash -LiteralPath $r.Backup).Hash -ne $r.Before) { throw 'Backup verification failed' }
    if((Get-FileHash -LiteralPath $r.Target).Hash -ne $r.After) { throw 'Installed file changed after repair; refusing to overwrite an update' }
}
foreach($r in $records) { Copy-Item -LiteralPath $r.Backup -Destination $r.Target -Force }
Write-Output "Restored $($records.Count) original DLLs"
