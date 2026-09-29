$ErrorActionPreference='Stop'
$ids=@(
'underworld-dungeon-great-decay-carrion-catacombs-ossuary-gate',
'underworld-dungeon-great-decay-carrion-catacombs-processional-hall',
'underworld-dungeon-great-decay-carrion-catacombs-sunken-reliquary',
'underworld-dungeon-great-decay-carrion-catacombs-root-split-crossing',
'underworld-dungeon-great-decay-carrion-catacombs-bone-chute',
'underworld-dungeon-great-decay-carrion-catacombs-miasma-nave',
'underworld-dungeon-great-decay-carrion-catacombs-carrion-sluice',
'underworld-dungeon-great-decay-carrion-catacombs-amber-mortuary',
'underworld-dungeon-great-decay-carrion-catacombs-bone-gravel-crypt',
'underworld-dungeon-great-decay-carrion-catacombs-censer-court',
'underworld-dungeon-great-decay-carrion-catacombs-rotling-warrens',
'underworld-dungeon-great-decay-carrion-catacombs-spore-husk-cloister',
'underworld-dungeon-great-decay-carrion-catacombs-graft-warden-hall',
'underworld-dungeon-great-decay-carrion-catacombs-vanishing-archive',
'underworld-dungeon-great-decay-carrion-catacombs-defiant-work-chapel',
'underworld-dungeon-great-decay-carrion-catacombs-corpse-orchard-antechamber',
'underworld-dungeon-great-decay-carrion-catacombs-passage'
)
& "$PSScriptRoot/blender.ps1" author-carrion-catacombs-dungeon @ids
if($LASTEXITCODE -ne 0){throw 'Carrion Catacombs authoring failed.'}
& "$PSScriptRoot/blender.ps1" verify-carrion-catacombs-dungeon @ids
if($LASTEXITCODE -ne 0){throw 'Carrion Catacombs source verification failed.'}
& "$PSScriptRoot/blender.ps1" export-model-assets @ids
if($LASTEXITCODE -ne 0){throw 'Carrion Catacombs export failed.'}
python "$PSScriptRoot/verify-model-assets.py"
if($LASTEXITCODE -ne 0){throw 'Carrion Catacombs exported model verification failed.'}
Write-Host 'REBUILT 16 Carrion Catacombs room families plus adaptive passage.'
