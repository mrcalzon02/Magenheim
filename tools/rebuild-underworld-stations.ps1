$ErrorActionPreference = 'Stop'
$ids = @(
    'underworld-station-mycelial-bench',
    'underworld-station-tidal-basin',
    'underworld-station-furnace-heart-forge',
    'underworld-station-silence-table',
    'underworld-station-anchor-forge',
    'underworld-station-crown-reliquary'
)

& "$PSScriptRoot/blender.ps1" author-underworld-stations @ids
if ($LASTEXITCODE -ne 0) { throw 'Underworld station authoring failed.' }

& "$PSScriptRoot/blender.ps1" export-model-assets @ids
if ($LASTEXITCODE -ne 0) { throw 'Underworld station export failed.' }

python "$PSScriptRoot/verify-model-assets.py"
if ($LASTEXITCODE -ne 0) { throw 'Underworld station model verification failed.' }

& "$PSScriptRoot/blender.ps1" render-underworld-station-icons @ids
if ($LASTEXITCODE -ne 0) { throw 'Underworld station icon rendering failed.' }

python "$PSScriptRoot/verify-icon-assets.py"
if ($LASTEXITCODE -ne 0) { throw 'Underworld station icon verification failed.' }

Write-Host 'REBUILT six owned Underworld crafting-station models and Hammer icons.'
