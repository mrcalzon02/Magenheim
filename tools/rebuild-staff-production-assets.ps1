$ErrorActionPreference = 'Stop'
$ids = @(
    'Magenheim_Staff_Earth_Advanced',
    'Magenheim_Staff_Earth_Crystal',
    'Magenheim_Staff_Earth_Master',
    'Magenheim_Staff_Earth_Simple',
    'Magenheim_Staff_Fire_Advanced',
    'Magenheim_Staff_Fire_Crystal',
    'Magenheim_Staff_Fire_Master',
    'Magenheim_Staff_Fire_Simple',
    'Magenheim_Staff_Storm_Advanced',
    'Magenheim_Staff_Storm_Crystal',
    'Magenheim_Staff_Storm_Master',
    'Magenheim_Staff_Storm_Simple',
    'staff-frost-advanced',
    'staff-frost-crystal',
    'staff-frost-master',
    'staff-frost-simple',
    'staff-radiance-advanced',
    'staff-radiance-crystal',
    'staff-radiance-master',
    'staff-radiance-simple',
    'staff-seidr-advanced',
    'staff-seidr-crystal',
    'staff-seidr-master',
    'staff-seidr-simple',
    'staff-spirit-advanced',
    'staff-spirit-crystal',
    'staff-spirit-master',
    'staff-spirit-simple',
    'staff-venom-advanced',
    'staff-venom-crystal',
    'staff-venom-master',
    'staff-venom-simple'
)

& "$PSScriptRoot/blender.ps1" export-model-assets @ids
if ($LASTEXITCODE -ne 0) { throw 'Staff model export failed.' }

python "$PSScriptRoot/verify-model-assets.py"
if ($LASTEXITCODE -ne 0) { throw 'Staff model catalog verification failed.' }

Write-Host "REBUILT $($ids.Count) staff runtime/GLB exports from authoritative Blender sources."
