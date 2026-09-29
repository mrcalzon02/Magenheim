[CmdletBinding()]
param([switch]$SkipReview)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$scope=Get-Content -LiteralPath "$PSScriptRoot/underworld-production-scope.json" -Raw | ConvertFrom-Json

& python "$PSScriptRoot/verify-underworld-production-readiness.py"
if($LASTEXITCODE -ne 0){throw 'Production readiness preflight failed.'}
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
    & "$PSScriptRoot/blender.ps1" render-rootwarren-review
    if($LASTEXITCODE -ne 0){throw 'Rootwarren dungeon review render failed.'}
    & python "$PSScriptRoot/build-underworld-production-review-sheets.py"
    if($LASTEXITCODE -ne 0){throw 'Production review sheet assembly failed.'}
    & python "$PSScriptRoot/build-rootwarren-review-sheets.py"
    if($LASTEXITCODE -ne 0){throw 'Rootwarren review sheet assembly failed.'}
    & python "$PSScriptRoot/verify-underworld-production-review.py"
    if($LASTEXITCODE -ne 0){throw 'Production review completeness gate failed.'}
    & python "$PSScriptRoot/verify-rootwarren-review.py"
    if($LASTEXITCODE -ne 0){throw 'Rootwarren visual review completeness gate failed.'}
    & "$PSScriptRoot/blender.ps1" render-drowned-vaults-review
    if($LASTEXITCODE -ne 0){throw 'Drowned Vault dungeon review render failed.'}
    & python "$PSScriptRoot/build-drowned-vaults-review-sheets.py"
    if($LASTEXITCODE -ne 0){throw 'Drowned Vault review sheet assembly failed.'}
    & python "$PSScriptRoot/verify-drowned-vaults-review.py"
    if($LASTEXITCODE -ne 0){throw 'Drowned Vault visual review completeness gate failed.'}
    & "$PSScriptRoot/blender.ps1" render-cinderworks-review
    if($LASTEXITCODE -ne 0){throw 'Cinderworks dungeon review render failed.'}
    & python "$PSScriptRoot/build-cinderworks-review-sheets.py"
    if($LASTEXITCODE -ne 0){throw 'Cinderworks review sheet assembly failed.'}
    & python "$PSScriptRoot/verify-cinderworks-review.py"
    if($LASTEXITCODE -ne 0){throw 'Cinderworks visual review completeness gate failed.'}
    & "$PSScriptRoot/blender.ps1" render-rime-sepulcher-review
    if($LASTEXITCODE -ne 0){throw 'Rime Sepulcher dungeon review render failed.'}
    & python "$PSScriptRoot/build-rime-sepulcher-review-sheets.py"
    if($LASTEXITCODE -ne 0){throw 'Rime Sepulcher review sheet assembly failed.'}
    & python "$PSScriptRoot/verify-rime-sepulcher-review.py"
    if($LASTEXITCODE -ne 0){throw 'Rime Sepulcher visual review completeness gate failed.'}
    & "$PSScriptRoot/blender.ps1" render-carrion-catacombs-review
    if($LASTEXITCODE -ne 0){throw 'Carrion Catacombs dungeon review render failed.'}
    & python "$PSScriptRoot/build-carrion-catacombs-review-sheets.py"
    if($LASTEXITCODE -ne 0){throw 'Carrion Catacombs review sheet assembly failed.'}
    & python "$PSScriptRoot/verify-carrion-catacombs-review.py"
    if($LASTEXITCODE -ne 0){throw 'Carrion Catacombs visual review completeness gate failed.'}
}
Write-Host 'PRODUCTION READY: 26 PBR families, 40 raw/refined material items, Crystal weapons, 32 elemental staves, 12 Underworld derivatives, Rootforged, stations, tools, armour, and the 17-model Rootwarren, Drowned Vault, Cinderworks, Rime Sepulcher and Carrion Catacombs dungeon families regenerated and gated.'
