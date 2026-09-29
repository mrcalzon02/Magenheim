$ErrorActionPreference = 'Stop'
$ids = @(
    'underworld-weapon-worldroot-club',
    'underworld-weapon-worldroot-bow',
    'underworld-weapon-flowstone-maul',
    'underworld-weapon-blackwater-harpoon',
    'underworld-weapon-emberiron-axe',
    'underworld-weapon-emberiron-greatsword',
    'underworld-weapon-rimesilver-spear',
    'underworld-weapon-icebind-staff',
    'underworld-weapon-titanbone-atgeir',
    'underworld-weapon-shardstone-crossbow',
    'underworld-weapon-amber-blade',
    'underworld-weapon-crown-sceptre'
)

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

Write-Host 'REBUILT all 12 Underworld crystal-chassis weapon derivatives and icons.'
