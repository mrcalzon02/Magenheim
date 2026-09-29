$ErrorActionPreference='Stop'
$ids=@(
'underworld-dungeon-blackwater-drowned-vaults-drowned-sinkhole',
'underworld-dungeon-blackwater-drowned-vaults-tide-gallery',
'underworld-dungeon-blackwater-drowned-vaults-dry-ledger',
'underworld-dungeon-blackwater-drowned-vaults-collapsed-dock',
'underworld-dungeon-blackwater-drowned-vaults-siphon-hall',
'underworld-dungeon-blackwater-drowned-vaults-bell-chamber',
'underworld-dungeon-blackwater-drowned-vaults-split-cistern',
'underworld-dungeon-blackwater-drowned-vaults-drowned-shaft',
'underworld-dungeon-blackwater-drowned-vaults-pearl-vault',
'underworld-dungeon-blackwater-drowned-vaults-high-water-archive',
'underworld-dungeon-blackwater-drowned-vaults-lamprey-run',
'underworld-dungeon-blackwater-drowned-vaults-deep-hunter-lair',
'underworld-dungeon-blackwater-drowned-vaults-sunken-quay',
'underworld-dungeon-blackwater-drowned-vaults-broken-causeway',
'underworld-dungeon-blackwater-drowned-vaults-undertow-sluice',
'underworld-dungeon-blackwater-drowned-vaults-abyssal-sanctum',
'underworld-dungeon-blackwater-drowned-vaults-passage'
)
& "$PSScriptRoot/blender.ps1" author-drowned-vaults-dungeon @ids
if($LASTEXITCODE -ne 0){throw 'Drowned Vault authoring failed.'}
& "$PSScriptRoot/blender.ps1" verify-drowned-vaults-dungeon @ids
if($LASTEXITCODE -ne 0){throw 'Drowned Vault source verification failed.'}
& "$PSScriptRoot/blender.ps1" export-model-assets @ids
if($LASTEXITCODE -ne 0){throw 'Drowned Vault export failed.'}
python "$PSScriptRoot/verify-model-assets.py"
if($LASTEXITCODE -ne 0){throw 'Drowned Vault exported model verification failed.'}
Write-Host 'REBUILT 16 Drowned Vault room families plus adaptive passage.'
