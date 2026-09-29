$ErrorActionPreference = 'Stop'
$ids = @('nowhere-king-sword-firmament', 'nowhere-king-sword-null-gate')

& "$PSScriptRoot/blender.ps1" author-nowhere-king-swords @ids
if ($LASTEXITCODE -ne 0) { throw 'Nowhere King sword authoring failed.' }

& "$PSScriptRoot/blender.ps1" verify-nowhere-king-swords @ids
if ($LASTEXITCODE -ne 0) { throw 'Nowhere King sword source verification failed.' }

& "$PSScriptRoot/blender.ps1" export-model-assets @ids
if ($LASTEXITCODE -ne 0) { throw 'Nowhere King sword export failed.' }

Write-Host 'Nowhere King Last Argument pair authored, verified, and exported.'
