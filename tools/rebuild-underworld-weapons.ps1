$ErrorActionPreference = 'Stop'
$ids = @('underworld-weapon-worldroot-club', 'underworld-weapon-worldroot-bow')

& "$PSScriptRoot/blender.ps1" author-underworld-weapons @ids
if ($LASTEXITCODE -ne 0) { throw 'Underworld weapon authoring failed.' }

& "$PSScriptRoot/blender.ps1" export-model-assets @ids
if ($LASTEXITCODE -ne 0) { throw 'Underworld weapon export failed.' }

python "$PSScriptRoot/verify-model-assets.py"
if ($LASTEXITCODE -ne 0) { throw 'Underworld weapon model catalog verification failed.' }

& "$PSScriptRoot/blender.ps1" render-weapon-icons @ids
if ($LASTEXITCODE -ne 0) { throw 'Underworld weapon icon rendering failed.' }

python "$PSScriptRoot/verify-icon-assets.py"
if ($LASTEXITCODE -ne 0) { throw 'Underworld weapon icon verification failed.' }

Write-Host 'REBUILT Worldroot Club and Worldroot Bow from their Crystal weapon source models.'
