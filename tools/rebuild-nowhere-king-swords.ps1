$ErrorActionPreference = 'Stop'
$ids = @('nowhere-king-sword-firmament', 'nowhere-king-sword-null-gate')

& "$PSScriptRoot/blender.ps1" author-nowhere-king-swords @ids
if ($LASTEXITCODE -ne 0) { throw 'Nowhere King sword authoring failed.' }

& "$PSScriptRoot/blender.ps1" verify-nowhere-king-swords @ids
if ($LASTEXITCODE -ne 0) { throw 'Nowhere King sword source verification failed.' }

& "$PSScriptRoot/blender.ps1" export-model-assets @ids
if ($LASTEXITCODE -ne 0) { throw 'Nowhere King sword export failed.' }

& "$PSScriptRoot/blender.ps1" render-weapon-icons @ids
if ($LASTEXITCODE -ne 0) { throw 'Nowhere King sword icon rendering failed.' }

python "$PSScriptRoot/verify-nowhere-king-generated-assets.py"
if ($LASTEXITCODE -ne 0) { throw 'Nowhere King sword generated-asset verification failed.' }

Write-Host 'REBUILT Firmament and Null Gate only: source, GLB, runtime payload, catalog and icons.'
