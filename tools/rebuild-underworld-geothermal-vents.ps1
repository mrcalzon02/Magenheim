$ErrorActionPreference = 'Stop'
$ids = @(
    'underworld-geothermal-vent-crown',
    'underworld-geothermal-vent-split',
    'underworld-geothermal-vent-rootbound'
)

& "$PSScriptRoot/blender.ps1" author-underworld-geothermal-vents @ids
if ($LASTEXITCODE -ne 0) { throw 'Underworld geothermal vent authoring failed.' }

& "$PSScriptRoot/blender.ps1" export-model-assets @ids
if ($LASTEXITCODE -ne 0) { throw 'Underworld geothermal vent export failed.' }

python "$PSScriptRoot/verify-model-assets.py"
if ($LASTEXITCODE -ne 0) { throw 'Underworld geothermal vent verification failed.' }

Write-Host 'REBUILT three owned Sulfurous-Wastes geothermal vent models.'
