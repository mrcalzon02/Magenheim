$ErrorActionPreference='Stop'
$prefix='underworld-dungeon-fungal-rootwarren-'
$ids=@(
 'fracture-mouth','mycelial-gallery','glowcap-vault','spore-basin','root-bridge',
 'sunken-nursery','tangle-junction','shelf-drop','amber-grotto','worldroot-hollow',
 'crawler-nest','stalker-den','puffback-graze','buried-archway','root-squeeze',
 'heartcap-sanctum','passage'
) | ForEach-Object { $prefix + $_ }

& "$PSScriptRoot/blender.ps1" author-rootwarren-dungeon @ids
if($LASTEXITCODE -ne 0){throw 'Rootwarren authoring failed.'}
& "$PSScriptRoot/blender.ps1" verify-rootwarren-dungeon @ids
if($LASTEXITCODE -ne 0){throw 'Rootwarren source verification failed.'}
& "$PSScriptRoot/blender.ps1" export-model-assets @ids
if($LASTEXITCODE -ne 0){throw 'Rootwarren export failed.'}
& python "$PSScriptRoot/verify-model-assets.py"
if($LASTEXITCODE -ne 0){throw 'Rootwarren exported model verification failed.'}
Write-Host "VERIFIED Rootwarren production forge: $($ids.Count) authored/exported models"
