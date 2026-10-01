[CmdletBinding()]
param([Parameter(Mandatory=$true)][ValidateSet('flora','arenas')][string]$Family)
$ErrorActionPreference='Stop'
if ($Family -eq 'flora') {
    $author='author-underworld-late-flora'
    $ids=@('underworld-flora-sulfur-cinderstalk','underworld-flora-frozen-rimecap','underworld-flora-decay-rotbloom')
    foreach ($species in @('glowcap','spirestalk','cinderstalk')) {
        foreach ($stage in @('felled','stump')) { $ids += "underworld-flora-harvest-$species-$stage" }
    }
} else {
    $author='author-underworld-boss-arenas'
    $ids=@('first-bloom','blackwater-maw','furnace-heart','white-silence','rift-titan','carrion-crown') |
        ForEach-Object { "underworld-boss-arena-$_" }
}
& "$PSScriptRoot/blender.ps1" $author
if($LASTEXITCODE -ne 0){ throw "$Family authoring failed." }
& "$PSScriptRoot/blender.ps1" export-model-assets @ids
if($LASTEXITCODE -ne 0){ throw "$Family native payload export failed." }
& python "$PSScriptRoot/verify-model-assets.py" @ids
if($LASTEXITCODE -ne 0){ throw "$Family payload validation failed." }
