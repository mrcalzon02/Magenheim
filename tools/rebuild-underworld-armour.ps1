$ErrorActionPreference = 'Stop'
python -X utf8 "$PSScriptRoot/author-underworld-donor-armour.py"
if ($LASTEXITCODE -ne 0) { throw 'Native armour palette authoring failed.' }
python -X utf8 "$PSScriptRoot/author-underworld-donor-armour.py" --check
if ($LASTEXITCODE -ne 0) { throw 'Native armour palette verification failed.' }
Write-Host 'Armour production retains vanilla donor geometry, skinning, cloth and body overlays.'
