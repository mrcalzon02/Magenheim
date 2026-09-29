$ErrorActionPreference='Stop'
$ids=@(
'underworld-dungeon-sulfur-cinderworks-cinder-gate',
'underworld-dungeon-sulfur-cinderworks-slag-nave',
'underworld-dungeon-sulfur-cinderworks-furnace-gallery',
'underworld-dungeon-sulfur-cinderworks-bellows-junction',
'underworld-dungeon-sulfur-cinderworks-chimney-shaft',
'underworld-dungeon-sulfur-cinderworks-vent-choir',
'underworld-dungeon-sulfur-cinderworks-slag-runoff',
'underworld-dungeon-sulfur-cinderworks-emberiron-foundry',
'underworld-dungeon-sulfur-cinderworks-sulfur-kiln',
'underworld-dungeon-sulfur-cinderworks-quench-vault',
'underworld-dungeon-sulfur-cinderworks-ashmite-conveyor',
'underworld-dungeon-sulfur-cinderworks-cinder-hound-yard',
'underworld-dungeon-sulfur-cinderworks-furnace-golem-crucible',
'underworld-dungeon-sulfur-cinderworks-broken-smeltery',
'underworld-dungeon-sulfur-cinderworks-pressure-lock',
'underworld-dungeon-sulfur-cinderworks-furnace-heart-antechamber',
'underworld-dungeon-sulfur-cinderworks-passage'
)
& "$PSScriptRoot/blender.ps1" author-cinderworks-dungeon @ids
if($LASTEXITCODE -ne 0){throw 'Cinderworks authoring failed.'}
& "$PSScriptRoot/blender.ps1" verify-cinderworks-dungeon @ids
if($LASTEXITCODE -ne 0){throw 'Cinderworks source verification failed.'}
& "$PSScriptRoot/blender.ps1" export-model-assets @ids
if($LASTEXITCODE -ne 0){throw 'Cinderworks export failed.'}
python "$PSScriptRoot/verify-model-assets.py"
if($LASTEXITCODE -ne 0){throw 'Cinderworks exported model verification failed.'}
Write-Host 'REBUILT 16 Cinderworks room families plus adaptive passage.'
