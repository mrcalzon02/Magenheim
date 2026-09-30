$ErrorActionPreference='Stop'
$ids=@(
 'blackstone-throne','blackstone-banner','blackstone-attendant-seat','blackstone-brazier',
 'blackstone-stair','blackstone-dais','blackstone-parapet','blackstone-bridge',
 'blackstone-arch','blackstone-cliff-edge','blackstone-floor-tile','blackstone-spire',
 'blackstone-pillar','blackstone-terrace','blackstone-wall-buttress',
 'blackstone-cathedral-wall','blackstone-gate','dark-throne'
)

python "$PSScriptRoot/generate-blackstone-throne-textures.py"
if($LASTEXITCODE -ne 0){throw 'Blackstone Throne 2D source artwork generation failed.'}

& "$PSScriptRoot/blender.ps1" author-blackstone-throne-set @ids
if($LASTEXITCODE -ne 0){throw 'Blackstone Throne source authoring failed.'}

& "$PSScriptRoot/blender.ps1" export-model-assets @ids
if($LASTEXITCODE -ne 0){throw 'Blackstone Throne model export failed.'}

python "$PSScriptRoot/verify-blackstone-throne-set.py"
if($LASTEXITCODE -ne 0){throw 'Blackstone Throne family verification failed.'}
python "$PSScriptRoot/record-blackstone-throne-generated-assets.py"
if($LASTEXITCODE -ne 0){throw 'Blackstone generated-asset ownership recording failed.'}
python "$PSScriptRoot/verify-generated-freshness.py" blackstone-throne-textures blackstone-throne-set
if($LASTEXITCODE -ne 0){throw 'Blackstone generated-asset freshness verification failed.'}

Write-Host 'REBUILT Blackstone Throne: 17 modular structure models + vertical assembled dark-throne site + owned 2D/PBR graphics.'
