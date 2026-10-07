[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$profile = Join-Path $env:APPDATA 'r2modmanPlus-local/Valheim/profiles/Central Fuckery'
$original = Join-Path (Split-Path $taskRoot -Parent) 'Jewelcraft1.0/backups/vendor/Companions.dll'
if ((Get-FileHash -LiteralPath $original).Hash -ne 'E233EA2E061655C07749DE03D524807302DBD29E7341338342C0EEF237329887') {
    throw 'Verified Offline Companions 1.3.0 original backup is required.'
}
$dist = Join-Path $taskRoot 'dist'
$cecil = Join-Path $profile 'BepInEx/core/Mono.Cecil.dll'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
foreach ($name in @('PatchCompanions','AuditCompanions')) {
    & $compiler /nologo "/out:$dist/$name.exe" "/reference:$cecil" (Join-Path $PSScriptRoot "compatibility/$name.cs")
    if ($LASTEXITCODE -ne 0) { throw "$name compilation failed." }
}
Copy-Item -LiteralPath $cecil -Destination $dist -Force
$package = Join-Path $dist 'Local-CompanionInventoryRepair-1.3.0-local3'
$plugin = Join-Path $package 'BepInEx/plugins/ProfMags-Offline_Companions'
New-Item -ItemType Directory -Force -Path $plugin | Out-Null
$output = Join-Path $plugin 'Companions.dll'
& (Join-Path $dist 'PatchCompanions.exe') $original $output (Join-Path (Split-Path $taskRoot -Parent) 'valheim_Data/Managed/assembly_valheim.dll')
if ($LASTEXITCODE -ne 0) { throw 'Companion repair failed.' }
& (Join-Path $dist 'AuditCompanions.exe') $output
if ($LASTEXITCODE -ne 0) { throw 'Companion repair audit failed.' }
& (Join-Path $dist 'AuditCompanions.exe') $original
if ($LASTEXITCODE -eq 0) { throw 'Unpatched negative control unexpectedly passed.' }
@{
    originalHash = (Get-FileHash -LiteralPath $original).Hash
    expectedInstalledHash = '5EBD40C7059792623C3A4CF638BD2655F9A45EBECCD1F30EAC29047EA13F9DA3'
    repairedHash = (Get-FileHash -LiteralPath $output).Hash
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $package 'repair-receipt.json')
'Local Offline Companions 1.3.0 repair: initializes cloned grid Player and CanDropDragOntoItem state through reflection on the private native container field. Preserves vendor version and inventory data. Live inventory acceptance remains required.' | Set-Content -LiteralPath (Join-Path $package 'README.txt')
Compress-Archive -Path "$package/*" -DestinationPath "$package.zip" -Force
Write-Output "Built and audited $package.zip"
