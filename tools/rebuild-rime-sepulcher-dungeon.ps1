$ErrorActionPreference='Stop'
$ids=@(
'underworld-dungeon-frozen-rime-sepulcher-rime-mouth',
'underworld-dungeon-frozen-rime-sepulcher-long-glass-gallery',
'underworld-dungeon-frozen-rime-sepulcher-burial-colonnade',
'underworld-dungeon-frozen-rime-sepulcher-silent-crossing',
'underworld-dungeon-frozen-rime-sepulcher-icewell-shaft',
'underworld-dungeon-frozen-rime-sepulcher-whiteout-narthex',
'underworld-dungeon-frozen-rime-sepulcher-needle-pass',
'underworld-dungeon-frozen-rime-sepulcher-rimesilver-ossuary',
'underworld-dungeon-frozen-rime-sepulcher-clear-ice-lens-vault',
'underworld-dungeon-frozen-rime-sepulcher-still-air-crypt',
'underworld-dungeon-frozen-rime-sepulcher-frost-tick-niche',
'underworld-dungeon-frozen-rime-sepulcher-iceblind-hunt',
'underworld-dungeon-frozen-rime-sepulcher-cryolith-guard',
'underworld-dungeon-frozen-rime-sepulcher-frozen-archive',
'underworld-dungeon-frozen-rime-sepulcher-shelter-chapel',
'underworld-dungeon-frozen-rime-sepulcher-white-silence-antechamber',
'underworld-dungeon-frozen-rime-sepulcher-passage'
)
& "$PSScriptRoot/blender.ps1" author-rime-sepulcher-dungeon @ids
if($LASTEXITCODE -ne 0){throw 'Rime Sepulcher authoring failed.'}
& "$PSScriptRoot/blender.ps1" verify-rime-sepulcher-dungeon @ids
if($LASTEXITCODE -ne 0){throw 'Rime Sepulcher source verification failed.'}
& "$PSScriptRoot/blender.ps1" export-model-assets @ids
if($LASTEXITCODE -ne 0){throw 'Rime Sepulcher export failed.'}
python "$PSScriptRoot/verify-model-assets.py"
if($LASTEXITCODE -ne 0){throw 'Rime Sepulcher exported model verification failed.'}
Write-Host 'REBUILT 16 Rime Sepulcher room families plus adaptive passage.'
