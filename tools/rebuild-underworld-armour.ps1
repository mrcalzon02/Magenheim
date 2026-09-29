$ErrorActionPreference = 'Stop'
$sets = @('sporeweave','palewater','emberiron','rimeward','stoneanchor','defiant')
$slots = @('helmet','chest','legs','cape')
$ids = @()
foreach ($set in $sets) { foreach ($slot in $slots) { $ids += "underworld-armor-$set-$slot" } }

& "$PSScriptRoot/blender.ps1" author-underworld-armour @ids
if ($LASTEXITCODE -ne 0) { throw 'Underworld armour authoring failed.' }

& "$PSScriptRoot/blender.ps1" verify-underworld-armour @ids
if ($LASTEXITCODE -ne 0) { throw 'Underworld armour source-rig verification failed.' }

& "$PSScriptRoot/blender.ps1" export-model-assets @ids
if ($LASTEXITCODE -ne 0) { throw 'Underworld armour GLB/model export failed.' }

python "$PSScriptRoot/verify-model-assets.py"
if ($LASTEXITCODE -ne 0) { throw 'Underworld armour model verification failed.' }

& "$PSScriptRoot/blender.ps1" render-underworld-armour-icons @ids
if ($LASTEXITCODE -ne 0) { throw 'Underworld armour icon rendering failed.' }

python "$PSScriptRoot/verify-icon-assets.py"
if ($LASTEXITCODE -ne 0) { throw 'Underworld armour icon verification failed.' }

Write-Host 'REBUILT twenty-four owned Underworld armour sources, GLBs and inventory icons.'
