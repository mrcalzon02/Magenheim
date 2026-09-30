$ErrorActionPreference = 'Stop'
$ids = @(
    'underworld-tool-sporelight-lantern',
    'underworld-tool-diving-bell-hood',
    'underworld-tool-slag-pick',
    'underworld-tool-rime-chisel',
    'underworld-tool-anchor-spike',
    'underworld-tool-defiant-censer'
)

& "$PSScriptRoot/blender.ps1" author-underworld-tools @ids
if ($LASTEXITCODE -ne 0) { throw 'Underworld tool authoring failed.' }

& "$PSScriptRoot/blender.ps1" export-model-assets @ids
if ($LASTEXITCODE -ne 0) { throw 'Underworld tool export failed.' }

python "$PSScriptRoot/verify-model-assets.py" @ids
if ($LASTEXITCODE -ne 0) { throw 'Underworld tool model verification failed.' }

& "$PSScriptRoot/blender.ps1" render-underworld-tool-icons @ids
if ($LASTEXITCODE -ne 0) { throw 'Underworld tool icon rendering failed.' }

python "$PSScriptRoot/verify-icon-assets.py" @ids
if ($LASTEXITCODE -ne 0) { throw 'Underworld tool icon verification failed.' }

Write-Host 'REBUILT six owned Underworld progression tools and inventory icons.'
