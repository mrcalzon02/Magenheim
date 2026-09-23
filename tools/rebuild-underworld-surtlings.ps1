$ErrorActionPreference = 'Stop'
$ids = foreach ($element in 'fire', 'water', 'earth', 'wind', 'radiance', 'umbral') {
    foreach ($body in 'feminine', 'masculine') { "underworld-surtling-$element-$body" }
}
& "$PSScriptRoot/blender.ps1" author-underworld-surtlings @ids
if ($LASTEXITCODE -ne 0) { throw 'Surtling authoring failed.' }
& "$PSScriptRoot/blender.ps1" export-model-assets @ids
if ($LASTEXITCODE -ne 0) { throw 'Surtling export failed.' }
& python "$PSScriptRoot/verify-model-assets.py"
if ($LASTEXITCODE -ne 0) { throw 'Surtling model verification failed.' }
