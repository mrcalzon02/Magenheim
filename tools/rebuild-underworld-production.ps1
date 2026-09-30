[CmdletBinding()]
param([switch]$SkipReview)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$scope=Get-Content -LiteralPath "$PSScriptRoot/underworld-production-scope.json" -Raw | ConvertFrom-Json

& python "$PSScriptRoot/verify-underworld-production-readiness.py"
if($LASTEXITCODE -ne 0){throw 'Production readiness preflight failed.'}
& python "$PSScriptRoot/verify-deep-dungeon-expansion-gate0.py"
if($LASTEXITCODE -ne 0){throw 'Deep Dungeon Expansion DDE-00 architecture freeze failed.'}
& python "$PSScriptRoot/verify-deep-dungeon-expansion-source-prerequisites.py"
if($LASTEXITCODE -ne 0){throw 'Deep Dungeon Expansion DDE-02..10 source prerequisites failed.'}
& python "$PSScriptRoot/verify-underworld-dungeon-worldgen.py"
if($LASTEXITCODE -ne 0){throw 'Underworld dungeon native-worldgen spawn contract failed.'}

foreach($id in $scope.regenerate){
    Write-Host "=== production generator: $id ==="
    & python "$PSScriptRoot/verify-generated-freshness.py" --update $id
    if($LASTEXITCODE -ne 0){throw "Production generator failed: $id"}
    if($id -eq 'underworld-material-library'){
        & python "$PSScriptRoot/verify-underworld-material-textures.py"
        if($LASTEXITCODE -ne 0){throw 'Underworld material fidelity gate failed.'}
    }
}

# Fungal Forest creature production. These are authored bodies for the existing donor-backed
# gameplay chassis; the donor keeps AI/hitboxes/networking while ModelAssets supplies the visible
# segmented anatomy and authored presentation actions.
$fungalCreatureJobs=@(
    @{Id='underworld-creature-lantern-moth'; Texture='generate-underworld-lantern-moth-textures.py'; Author='author-underworld-lantern-moth'; Animate='animate-underworld-lantern-moth'; Verify='verify-underworld-lantern-moth'},
    @{Id='underworld-creature-sporeling'; Texture='generate-underworld-sporeling-textures.py'; Author='author-underworld-sporeling'; Animate=$null; Verify='verify-underworld-sporeling'},
    @{Id='underworld-creature-capcrawler'; Texture='generate-underworld-capcrawler-textures.py'; Author='author-underworld-capcrawler'; Animate=$null; Verify='verify-underworld-capcrawler'},
    @{Id='underworld-creature-mycelial-stalker'; Texture='generate-underworld-mycelial-stalker-textures.py'; Author='author-underworld-mycelial-stalker'; Animate='animate-underworld-mycelial-stalker'; Verify='verify-underworld-mycelial-stalker'},
    @{Id='underworld-creature-puffback'; Texture='generate-underworld-puffback-textures.py'; Author='author-underworld-puffback'; Animate='animate-underworld-puffback'; Verify='verify-underworld-puffback'},
    @{Id='underworld-creature-shelf-lurker'; Texture='generate-underworld-shelf-lurker-textures.py'; Author='author-underworld-shelf-lurker'; Animate='animate-underworld-shelf-lurker'; Verify='verify-underworld-shelf-lurker'},
    @{Id='underworld-creature-crowncap-brute'; Texture='generate-underworld-crowncap-brute-textures.py'; Author='author-underworld-crowncap-brute'; Animate='animate-underworld-crowncap-brute'; Verify='verify-underworld-crowncap-brute'}
)
$fungalCreatureIds=@()
foreach($job in $fungalCreatureJobs){
    Write-Host "=== Fungal creature production: $($job.Id) ==="
    & python "$PSScriptRoot/$($job.Texture)"
    if($LASTEXITCODE -ne 0){throw "Creature texture generation failed: $($job.Id)"}
    & "$PSScriptRoot/blender.ps1" $job.Author
    if($LASTEXITCODE -ne 0){throw "Creature authoring failed: $($job.Id)"}
    if($job.Animate){
        & "$PSScriptRoot/blender.ps1" $job.Animate
        if($LASTEXITCODE -ne 0){throw "Creature animation authoring failed: $($job.Id)"}
    }
    & "$PSScriptRoot/blender.ps1" $job.Verify
    if($LASTEXITCODE -ne 0){throw "Creature source verification failed: $($job.Id)"}
    $fungalCreatureIds += $job.Id
}
& "$PSScriptRoot/blender.ps1" export-model-assets @fungalCreatureIds
if($LASTEXITCODE -ne 0){throw 'Fungal creature runtime export failed.'}
& python "$PSScriptRoot/verify-model-assets.py" @fungalCreatureIds
if($LASTEXITCODE -ne 0){throw 'Fungal creature runtime payload verification failed.'}

# Ordinary Underworld dungeons intentionally reuse vanilla entrance/room/generator families.
# Do NOT forge/promote the legacy Rootwarren/Drowned Vault/Cinderworks/Rime Sepulcher/Carrion
# bespoke room kits here. Their source assets remain preserved for reference/reuse, while Deep
# Fracture remains the Magenheim-authored dungeon architecture lane. Ordinary dungeon runtime
# admission is governed by UnderworldVanillaDungeonReuseCatalog and live donor cloning instead.

foreach($gate in @(
 'verify-model-assets','verify-model-geometry','verify-model-surface-continuity','verify-model-scale',
 'verify-weapon-materials','verify-held-model-orientation','verify-held-model-grip-direction',
 'verify-icon-assets','verify-authored-surface-coverage'
)){
    & python "$PSScriptRoot/$gate.py"
    if($LASTEXITCODE -ne 0){throw "Production asset gate failed: $gate"}
}

& python "$PSScriptRoot/verify-underworld-production-assets.py"
if($LASTEXITCODE -ne 0){throw 'Underworld production family/PBR admission gate failed.'}

& "$PSScriptRoot/blender.ps1" verify-underworld-armour
if($LASTEXITCODE -ne 0){throw 'Underworld armour attach_skin source gate failed.'}
& "$PSScriptRoot/blender.ps1" verify-object-texture-bindings
if($LASTEXITCODE -ne 0){throw 'Object texture binding gate failed.'}

$selected=@();$selected += $scope.regenerate;$selected += $scope.verify_only
& python "$PSScriptRoot/verify-generated-freshness.py" @selected
if($LASTEXITCODE -ne 0){throw 'Selected production freshness gate failed after regeneration.'}

if(!$SkipReview){
    $review=Join-Path $root 'dist/underworld-production-review'
    if(Test-Path -LiteralPath $review){Remove-Item -LiteralPath $review -Recurse -Force}
    & "$PSScriptRoot/blender.ps1" render-underworld-production-review
    if($LASTEXITCODE -ne 0){throw 'Production review render failed.'}
    & "$PSScriptRoot/blender.ps1" render-underworld-armour-articulation-review
    if($LASTEXITCODE -ne 0){throw 'Underworld armour articulation review failed.'}
}
Write-Host 'PRODUCTION READY: 26 PBR families, 40 raw/refined material items, seven Fungal Forest creature bodies, Crystal weapons, 32 elemental staves, 12 Underworld derivatives, Rootforged, stations, tools and armour regenerated and gated. Ordinary Underworld dungeon architecture uses runtime vanilla-donor reuse; creature bodies are authored Blender assets.'
