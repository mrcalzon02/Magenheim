$ErrorActionPreference = 'Stop'
$spec = Get-Content -LiteralPath "$PSScriptRoot/underworld-material-item-specs.json" -Raw | ConvertFrom-Json
$ids = @($spec.PSObject.Properties.Name)
if ($ids.Count -ne 32) { throw "Underworld material-item catalog expected 32 authored models, found $($ids.Count)." }

& "$PSScriptRoot/blender.ps1" author-underworld-material-items @ids
if ($LASTEXITCODE -ne 0) { throw 'Underworld material-item authoring failed.' }

& "$PSScriptRoot/blender.ps1" export-model-assets @ids
if ($LASTEXITCODE -ne 0) { throw 'Underworld material-item export failed.' }

python "$PSScriptRoot/verify-model-assets.py"
if ($LASTEXITCODE -ne 0) { throw 'Underworld material-item model verification failed.' }

& "$PSScriptRoot/blender.ps1" render-underworld-resource-icons @ids
if ($LASTEXITCODE -ne 0) { throw 'Underworld material-item icon rendering failed.' }

python "$PSScriptRoot/verify-icon-assets.py"
if ($LASTEXITCODE -ne 0) { throw 'Underworld material-item icon verification failed.' }

Write-Host 'REBUILT 14 missing raw + 18 refined Underworld material item models and icons.'
