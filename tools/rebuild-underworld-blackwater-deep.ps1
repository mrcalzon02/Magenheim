$ErrorActionPreference = 'Stop'
$ids = @(
    'underworld-flora-blackwater-flowstone-spire',
    'underworld-flora-blackwater-broken-column',
    'underworld-flora-blackwater-rimstone-mound',
    'underworld-flora-blackwater-drowned-root-arch',
    'underworld-flora-blackwater-brine-fern',
    'underworld-flora-blackwater-palefinger',
    'underworld-flora-blackwater-pearl-caps',
    'underworld-flora-blackwater-wet-stone',
    'underworld-flora-blackwater-fingerstone-rubble',
    'underworld-flora-blackwater-root-fan',
    'underworld-flora-blackwater-lakebed-shelf',
    'underworld-flora-blackwater-root-fingers',
    'underworld-resource-blackwater-flowstone',
    'underworld-resource-pale-fibre',
    'underworld-resource-blackwater-pearl',
    'underworld-resource-deep-salt')
& "$PSScriptRoot/blender.ps1" author-underworld-blackwater-deep @ids
if ($LASTEXITCODE -ne 0) { throw 'Blackwater authoring failed.' }
& "$PSScriptRoot/blender.ps1" verify-blackwater-sources
if ($LASTEXITCODE -ne 0) { throw 'Blackwater source verification failed.' }
& "$PSScriptRoot/blender.ps1" export-model-assets @ids
if ($LASTEXITCODE -ne 0) { throw 'Blackwater export failed.' }
& python "$PSScriptRoot/verify-model-assets.py"
if ($LASTEXITCODE -ne 0) { throw 'Blackwater model verification failed.' }
