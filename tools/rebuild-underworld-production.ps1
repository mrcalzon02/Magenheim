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

# Rootwarren is a large dungeon family rather than an ordinary equipment/material generator.
# Forge it explicitly after the normal production authorities so its 17 authored Blender sources,
# GLBs and runtime payloads exist before the shared model gates inspect the repository.
& "$PSScriptRoot/rebuild-rootwarren-dungeon.ps1"
if($LASTEXITCODE -ne 0){throw 'Rootwarren dungeon production forge failed.'}
& python "$PSScriptRoot/verify-rootwarren-production-contract.py"
if($LASTEXITCODE -ne 0){throw 'Rootwarren source/asset contract failed after forge.'}
& python "$PSScriptRoot/record-rootwarren-generated-assets.py"
if($LASTEXITCODE -ne 0){throw 'Rootwarren generated-asset provenance recording failed.'}
& python "$PSScriptRoot/verify-generated-freshness.py" rootwarren-dungeon-models
if($LASTEXITCODE -ne 0){throw 'Rootwarren generated assets are stale immediately after forge.'}
& python "$PSScriptRoot/promote-rootwarren-runtime.py"
if($LASTEXITCODE -ne 0){throw 'Rootwarren runtime promotion failed after complete asset forge.'}
& python "$PSScriptRoot/verify-rootwarren-production-contract.py"
if($LASTEXITCODE -ne 0){throw 'Rootwarren RuntimeReady contract failed after promotion.'}

# Drowned Vaults use the same asset-earned admission rule as Rootwarren. Their 16 room
# families plus adaptive passage remain catalog-Planned until this forge proves the whole set.
& "$PSScriptRoot/rebuild-drowned-vaults-dungeon.ps1"
if($LASTEXITCODE -ne 0){throw 'Drowned Vault dungeon production forge failed.'}
& python "$PSScriptRoot/verify-drowned-vaults-production-contract.py"
if($LASTEXITCODE -ne 0){throw 'Drowned Vault source/asset contract failed after forge.'}
& python "$PSScriptRoot/record-drowned-vaults-generated-assets.py"
if($LASTEXITCODE -ne 0){throw 'Drowned Vault generated-asset provenance recording failed.'}
& python "$PSScriptRoot/verify-generated-freshness.py" drowned-vaults-dungeon-models
if($LASTEXITCODE -ne 0){throw 'Drowned Vault generated assets are stale immediately after forge.'}
& python "$PSScriptRoot/promote-drowned-vaults-runtime.py"
if($LASTEXITCODE -ne 0){throw 'Drowned Vault runtime promotion failed after complete asset forge.'}
& python "$PSScriptRoot/verify-drowned-vaults-production-contract.py"
if($LASTEXITCODE -ne 0){throw 'Drowned Vault RuntimeReady contract failed after promotion.'}

# Cinderworks remains catalog-Planned until its 16 rooms plus adaptive passage are forged,
# verified, provenance-recorded and reviewed. Heat gameplay already binds to canonical geothermal state.
& "$PSScriptRoot/rebuild-cinderworks-dungeon.ps1"
if($LASTEXITCODE -ne 0){throw 'Cinderworks dungeon production forge failed.'}
& python "$PSScriptRoot/verify-cinderworks-production-contract.py"
if($LASTEXITCODE -ne 0){throw 'Cinderworks source/asset contract failed after forge.'}
& python "$PSScriptRoot/record-cinderworks-generated-assets.py"
if($LASTEXITCODE -ne 0){throw 'Cinderworks generated-asset provenance recording failed.'}
& python "$PSScriptRoot/verify-generated-freshness.py" cinderworks-dungeon-models
if($LASTEXITCODE -ne 0){throw 'Cinderworks generated assets are stale immediately after forge.'}
& python "$PSScriptRoot/promote-cinderworks-runtime.py"
if($LASTEXITCODE -ne 0){throw 'Cinderworks runtime promotion failed after complete asset forge.'}
& python "$PSScriptRoot/verify-cinderworks-production-contract.py"
if($LASTEXITCODE -ne 0){throw 'Cinderworks RuntimeReady contract failed after promotion.'}

# Rime Sepulcher uses the same asset-earned admission rule. Frozen weather pressure remains
# canonical runtime atmosphere authority; the forge only produces geometry and runtime model payloads.
& "$PSScriptRoot/rebuild-rime-sepulcher-dungeon.ps1"
if($LASTEXITCODE -ne 0){throw 'Rime Sepulcher dungeon production forge failed.'}
& python "$PSScriptRoot/verify-rime-sepulcher-production-contract.py"
if($LASTEXITCODE -ne 0){throw 'Rime Sepulcher source/asset contract failed after forge.'}
& python "$PSScriptRoot/record-rime-sepulcher-generated-assets.py"
if($LASTEXITCODE -ne 0){throw 'Rime Sepulcher generated-asset provenance recording failed.'}
& python "$PSScriptRoot/verify-generated-freshness.py" rime-sepulcher-dungeon-models
if($LASTEXITCODE -ne 0){throw 'Rime Sepulcher generated assets are stale immediately after forge.'}
& python "$PSScriptRoot/promote-rime-sepulcher-runtime.py"
if($LASTEXITCODE -ne 0){throw 'Rime Sepulcher runtime promotion failed after complete asset forge.'}
& python "$PSScriptRoot/verify-rime-sepulcher-production-contract.py"
if($LASTEXITCODE -ne 0){throw 'Rime Sepulcher RuntimeReady contract failed after promotion.'}

# Carrion Catacombs close the ordinary-biome dungeon set. Great Decay contamination stays bound
# to the canonical atmosphere/mitigation runtime; Blender owns only the authored spatial payloads.
& "$PSScriptRoot/rebuild-carrion-catacombs-dungeon.ps1"
if($LASTEXITCODE -ne 0){throw 'Carrion Catacombs dungeon production forge failed.'}
& python "$PSScriptRoot/verify-carrion-catacombs-production-contract.py"
if($LASTEXITCODE -ne 0){throw 'Carrion Catacombs source/asset contract failed after forge.'}
& python "$PSScriptRoot/record-carrion-catacombs-generated-assets.py"
if($LASTEXITCODE -ne 0){throw 'Carrion Catacombs generated-asset provenance recording failed.'}
& python "$PSScriptRoot/verify-generated-freshness.py" carrion-catacombs-dungeon-models
if($LASTEXITCODE -ne 0){throw 'Carrion Catacombs generated assets are stale immediately after forge.'}
& python "$PSScriptRoot/promote-carrion-catacombs-runtime.py"
if($LASTEXITCODE -ne 0){throw 'Carrion Catacombs runtime promotion failed after complete asset forge.'}
& python "$PSScriptRoot/verify-carrion-catacombs-production-contract.py"
if($LASTEXITCODE -ne 0){throw 'Carrion Catacombs RuntimeReady contract failed after promotion.'}

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
