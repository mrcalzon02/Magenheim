$ErrorActionPreference = 'Stop'
$ids = @('underworld-flora-fungal-glowcap', 'underworld-flora-fungal-spirestalk', 'underworld-flora-fungal-puffcap',
         'underworld-flora-fungal-tanglecap', 'underworld-flora-fungal-fern', 'underworld-flora-fungal-sporebrush',
         'underworld-flora-fungal-amber-bed', 'underworld-flora-fungal-lantern-caps', 'underworld-flora-fungal-nursery-caps',
         'underworld-flora-fungal-moss-stone', 'underworld-flora-fungal-root-skirt', 'underworld-flora-fungal-sapling',
         'underworld-resource-worldroot-timber', 'underworld-resource-glowcap-flesh', 'underworld-resource-spire-fibre',
         'underworld-resource-understone')
$variants = @('glowcap-bent', 'glowcap-twin', 'glowcap-elder',
    'spirestalk-young', 'spirestalk-crooked', 'spirestalk-towered',
    'puffcap-forked', 'puffcap-spreading', 'puffcap-clustered',
    'tanglecap-young', 'tanglecap-splayed', 'tanglecap-woven')
$ids = @($variants | ForEach-Object { "underworld-flora-fungal-$_" }) + $ids
& "$PSScriptRoot/blender.ps1" author-underworld-fungal-forest @ids
if ($LASTEXITCODE -ne 0) { throw 'Fungal Forest authoring failed.' }
& "$PSScriptRoot/blender.ps1" export-model-assets @ids
if ($LASTEXITCODE -ne 0) { throw 'Fungal Forest export failed.' }
& python "$PSScriptRoot/verify-model-assets.py"
if ($LASTEXITCODE -ne 0) { throw 'Fungal Forest model verification failed.' }
