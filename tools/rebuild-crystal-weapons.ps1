$ErrorActionPreference = 'Stop'
$ids = @('crystal-weapon-sword', 'crystal-weapon-greatsword', 'crystal-weapon-knife', 'crystal-weapon-axe',
         'crystal-weapon-battleaxe', 'crystal-weapon-mace', 'crystal-weapon-spear', 'crystal-weapon-atgeir',
         'crystal-weapon-bow', 'crystal-weapon-crossbow')
& "$PSScriptRoot/blender.ps1" author-crystal-weapons @ids
if ($LASTEXITCODE -ne 0) { throw 'Crystal weapon authoring failed.' }
& "$PSScriptRoot/blender.ps1" export-model-assets @ids
if ($LASTEXITCODE -ne 0) { throw 'Crystal weapon export failed.' }
foreach ($gate in 'verify-model-assets', 'verify-weapon-materials', 'verify-model-scale', 'verify-held-model-grip-direction') {
    & python "$PSScriptRoot/$gate.py"
    if ($LASTEXITCODE -ne 0) { throw "Crystal weapon gate failed: $gate" }
}
