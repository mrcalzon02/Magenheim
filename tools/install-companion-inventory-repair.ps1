[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
if (Get-Process -Name valheim,r2modman -ErrorAction SilentlyContinue) { throw 'Close Valheim and r2modman before installing.' }
$taskRoot = Split-Path $PSScriptRoot -Parent
$package = Join-Path $taskRoot 'dist/Local-CompanionInventoryRepair-1.3.0-local2'
$receipt = Get-Content -LiteralPath (Join-Path $package 'repair-receipt.json') -Raw | ConvertFrom-Json
$source = Join-Path $package 'BepInEx/plugins/ProfMags-Offline_Companions/Companions.dll'
$target = Join-Path $env:APPDATA 'r2modmanPlus-local/Valheim/profiles/Central Fuckery/BepInEx/plugins/ProfMags-Offline_Companions/Companions.dll'
if ((Get-FileHash -LiteralPath $source).Hash -ne $receipt.repairedHash) { throw 'Repair payload checksum mismatch.' }
$before = (Get-FileHash -LiteralPath $target).Hash
if ($before -eq $receipt.repairedHash) { Write-Output 'Companion inventory repair is already installed.'; return }
if ($before -ne $receipt.expectedInstalledHash) { throw 'Companion vendor DLL changed since inspection; refusing to overwrite it.' }
$backup = Join-Path $taskRoot ('backups/Companions-inventory-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.dll')
Copy-Item -LiteralPath $target -Destination $backup
Copy-Item -LiteralPath $source -Destination $target -Force
if ((Get-FileHash -LiteralPath $target).Hash -ne $receipt.repairedHash) { throw 'Installed companion checksum mismatch.' }
Write-Output "Installed companion inventory repair. Backup: $backup"
