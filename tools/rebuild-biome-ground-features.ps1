$ErrorActionPreference = 'Stop'
$ids = @('underworld-ground-rootgrass-carpet', 'underworld-ground-rootgrass-tufts', 'underworld-ground-rootgrass-woven', 'underworld-ground-sulfur-drift-crescent', 'underworld-ground-sulfur-drift-rippled', 'underworld-ground-sulfur-drift-bank', 'underworld-ground-blackwater-silt-ripples', 'underworld-ground-frozen-rime-fan', 'underworld-ground-fracture-shale-scree', 'underworld-ground-decay-peat-mat')
& "$PSScriptRoot/blender.ps1" author-biome-ground-features @ids
if ($LASTEXITCODE -ne 0) { throw 'Ground feature authoring failed.' }
& "$PSScriptRoot/blender.ps1" export-model-assets @ids
if ($LASTEXITCODE -ne 0) { throw 'Ground feature export failed.' }
& python "$PSScriptRoot/verify-biome-ground-features.py"
if ($LASTEXITCODE -ne 0) { throw 'Ground feature verification failed.' }
