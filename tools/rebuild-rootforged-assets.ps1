$ErrorActionPreference = 'Stop'
& "$PSScriptRoot/blender.ps1" author-rootforged-placeables
if ($LASTEXITCODE -ne 0) { throw 'Rootforged authoring failed.' }
$rootModelIds = @(Get-ChildItem "$PSScriptRoot/../assets/models/source/rootforged-*.blend" | ForEach-Object BaseName)
& "$PSScriptRoot/blender.ps1" export-model-assets @rootModelIds
if ($LASTEXITCODE -ne 0) { throw 'Rootforged export failed.' }
& python "$PSScriptRoot/verify-model-assets.py"
if ($LASTEXITCODE -ne 0) { throw 'Rootforged model verification failed.' }
