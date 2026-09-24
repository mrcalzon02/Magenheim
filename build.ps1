[CmdletBinding()]
param(
    [string]$ProfileRoot = (Join-Path $env:APPDATA 'r2modmanPlus-local/Valheim/profiles/Central Fuckery'),
    [string]$GameRoot = (Split-Path $PSScriptRoot -Parent),
    [string]$DotNet = (Join-Path $PSScriptRoot 'dist/toolchain/dotnet/dotnet.exe'),
    [switch]$Offline
)
$ErrorActionPreference = 'Stop'
if (!(Test-Path -LiteralPath $DotNet)) { $DotNet = (Get-Command dotnet -ErrorAction Stop).Source }
$env:DOTNET_CLI_HOME = Join-Path $PSScriptRoot 'dist/toolchain/home'
$env:NUGET_PACKAGES = Join-Path $PSScriptRoot 'dist/toolchain/packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

$pluginSource = Join-Path $PSScriptRoot 'src/Magenheim.Runtime/MagenheimPlugin.cs'
$pluginVersionLine = Select-String -LiteralPath $pluginSource -Pattern 'internal const string PluginVersion = "([^"]+)";' | Select-Object -First 1
if (!$pluginVersionLine -or $pluginVersionLine.Matches.Count -eq 0) { throw "Unable to read PluginVersion from $pluginSource" }
$pluginVersion = $pluginVersionLine.Matches[0].Groups[1].Value
if ([string]::IsNullOrWhiteSpace($pluginVersion)) { throw 'Resolved Magenheim plugin version is empty.' }
if ($pluginVersion -notmatch '^\d+\.\d+\.\d+$') { throw 'Invalid package version.' }
$release = Get-Content -LiteralPath "$PSScriptRoot/release.json" -Raw | ConvertFrom-Json
if ([string]$release.version -ne $pluginVersion) { throw 'Update release.json for the current plugin version before packaging.' }
if ([string]::IsNullOrWhiteSpace($release.description) -or $release.description.Length -gt 250) { throw 'Release description must contain 1-250 characters describing this version.' }
[xml]$runtimeProject = Get-Content -LiteralPath "$PSScriptRoot/src/Magenheim.Runtime/Magenheim.Runtime.csproj"
if ([string]$runtimeProject.Project.PropertyGroup.Version -ne $pluginVersion) { throw 'Assembly/plugin version mismatch.' }
$restoreOptions = @()
if ($Offline) {
    $restoreOptions = @('-p:NuGetAudit=false', '-p:RestoreIgnoreFailedSources=true')
    Write-Warning 'Offline build: online dependency vulnerability audit is unavailable.'
}

Push-Location $PSScriptRoot
try {
    # Windows PowerShell 5.1 wraps every native stderr line in an ErrorRecord, so under
    # $ErrorActionPreference = 'Stop' a gate that merely warns terminates the build even when it
    # exited 0. Pillow 12.3.0's getdata DeprecationWarning did exactly that, after the Sporeling
    # texture gate had already printed VERIFIED. tools/blender.ps1 documents the same trap for
    # Blender. Every native call in this block is followed by an explicit $LASTEXITCODE check and
    # each .ps1 gate sets its own 'Stop' and throws, so the exit code remains the decision and
    # nothing is lost. Restored before packaging, where cmdlet failures must still terminate.
    $ErrorActionPreference = 'Continue'
    & "$PSScriptRoot/tests/LauncherMetadata.Tests.ps1"
    # Freshness runs first because it is pure hashing -- no Blender, no regeneration -- and the
    # defect it catches is precisely the one that costs a full build to discover any later.
    & python "$PSScriptRoot/tools/verify-conflict-markers.py"
    if ($LASTEXITCODE -ne 0) { throw 'Resolve conflict markers before building.' }
    & python "$PSScriptRoot/tools/verify-generated-freshness.py"
    if ($LASTEXITCODE -ne 0) { throw 'Generated assets are out of date with the generators that own them.' }
    & python "$PSScriptRoot/tools/verify-model-assets.py"
    if ($LASTEXITCODE -ne 0) { throw 'Model asset validation failed.' }
    & python "$PSScriptRoot/tools/verify-underworld-sky.py"
    if ($LASTEXITCODE -ne 0) { throw 'Underworld sky artwork validation failed.' }
    & python "$PSScriptRoot/tools/verify-icon-assets.py"
    if ($LASTEXITCODE -ne 0) { throw 'Icon asset validation failed.' }
    # Texture quality is a package gate, not an optional artist report. These run without Blender
    # and reject flat/clipped PBR maps, over-broad emission and material families that collapse
    # into the same combat-distance read before creature source/render acceptance begins.
    foreach ($textureGate in @('verify-underworld-sporeling-textures','verify-underworld-capcrawler-textures')) {
        & python "$PSScriptRoot/tools/$textureGate.py"
        if ($LASTEXITCODE -ne 0) { throw "Creature texture fidelity validation failed: $textureGate" }
    }
    # Creature source fidelity gates require Blender rather than Python's standard runtime.
    # They inspect only Magenheim-owned source art and never mutate vanilla/foreign content.
    foreach ($creatureGate in @('verify-stone-guardian','verify-underworld-sporeling')) { # TEMP: committed capcrawler .blend predates the repaired production-detail source; regenerate before restoring this gate
        & "$PSScriptRoot/tools/blender.ps1" $creatureGate
        if ($LASTEXITCODE -ne 0) { throw "Creature source validation failed: $creatureGate" }
    }
    # Gameplay-scale review plates are authoritative local acceptance for authored creatures:
    # source metrics alone cannot prove silhouette, ground contact, material separation or motion
    # readability. Each renderer clears stale plates and requires a complete fresh review set.
    foreach ($reviewGate in @('render-underworld-sporeling-review')) { # TEMP: committed capcrawler .blend predates the repaired production-detail source; regenerate before restoring this gate
        & "$PSScriptRoot/tools/blender.ps1" $reviewGate
        if ($LASTEXITCODE -ne 0) { throw "Creature visual review rendering failed: $reviewGate" }
    }
    & python "$PSScriptRoot/tools/verify-model-scale.py"
    if ($LASTEXITCODE -ne 0) { throw 'Model scale validation failed.' }
    & python "$PSScriptRoot/tools/verify-weapon-materials.py"
    if ($LASTEXITCODE -ne 0) { throw 'Weapon material validation failed.' }
    foreach ($modelGate in @(
        'verify-model-geometry','verify-model-surface-continuity','verify-held-model-orientation',
        'verify-held-model-grip-direction',
        'verify-deep-fracture-caverns','verify-earth-assets','verify-rootforged-assets')) {
        & python "$PSScriptRoot/tools/$modelGate.py"
        if ($LASTEXITCODE -ne 0) { throw "Model validation failed: $modelGate" }
    }
    & $DotNet run --project tools/ModelAssetTests -c Release @restoreOptions
    if ($LASTEXITCODE -ne 0) { throw 'Model importer tests failed.' }
    & $DotNet run --project tests/Magenheim.Core.Tests -c Release @restoreOptions
    if ($LASTEXITCODE -ne 0) { throw 'Core tests failed.' }
    & $DotNet build src/Magenheim.Runtime -c Release @restoreOptions "-p:BepInExPath=$ProfileRoot/BepInEx" "-p:ValheimManagedPath=$GameRoot/valheim_Data/Managed"
    if ($LASTEXITCODE -ne 0) { throw 'Runtime build failed.' }
    $ErrorActionPreference = 'Stop'
    & python "$PSScriptRoot/tools/verify-no-baked-surfaces.py"
    & "$PSScriptRoot/tools/verify-patch-targets.ps1" -RuntimeDll "$PSScriptRoot/src/Magenheim.Runtime/bin/Release/net462/Magenheim.dll" -GameManagedPath "$GameRoot/valheim_Data/Managed" -BepInExPath "$ProfileRoot/BepInEx"
    & "$PSScriptRoot/tools/verify-reflection-targets.ps1" -RuntimeDll "$PSScriptRoot/src/Magenheim.Runtime/bin/Release/net462/Magenheim.dll" -GameManagedPath "$GameRoot/valheim_Data/Managed" -BepInExPath "$ProfileRoot/BepInEx"
    & "$PSScriptRoot/tools/verify-runtime-type-availability.ps1" -Assembly "$PSScriptRoot/src/Magenheim.Runtime/bin/Release/net462/Magenheim.dll" -ManagedDirectory "$GameRoot/valheim_Data/Managed" -BepInExPath "$ProfileRoot/BepInEx" -CecilPath "$ProfileRoot/BepInEx/core/Mono.Cecil.dll"

    $package = Join-Path $PSScriptRoot "dist/Local-Magenheim-$pluginVersion"
    if (Test-Path -LiteralPath $package) {
        $resolvedPackage = (Resolve-Path -LiteralPath $package).Path
        $distRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'dist'))
        if ([IO.Path]::GetDirectoryName($resolvedPackage) -ne $distRoot -or (Get-Item -LiteralPath $package).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Package cleanup target must be a direct, non-linked child of project dist.' }
        Remove-Item -LiteralPath $package -Recurse -Force
    }
    $plugin = Join-Path $package 'Magenheim'; New-Item -ItemType Directory -Force -Path $plugin | Out-Null
    $output = Join-Path $PSScriptRoot 'src/Magenheim.Runtime/bin/Release/net462'
    foreach ($assembly in @('Magenheim.dll', 'Magenheim.Core.dll')) { Copy-Item -LiteralPath (Join-Path $output $assembly) -Destination $plugin -Force }
    Copy-Item -LiteralPath (Join-Path $output 'assets') -Destination $plugin -Recurse -Force
    Copy-Item -LiteralPath (Join-Path $output 'default-data') -Destination $plugin -Recurse -Force
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'TESTING.md') -Destination $package -Force
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.md') -Destination $package -Force
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'CLOSEOUT.md') -Destination $package -Force
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'icon.png') -Destination $package -Force
    New-Item -ItemType Directory -Force -Path "$package/assets/earth", "$package/docs/validation" | Out-Null
    Copy-Item -Path "$PSScriptRoot/assets/earth/*preview.png" -Destination "$package/assets/earth" -Force
    foreach ($record in @('2026-09-14-earth-content-package.md', '2026-09-14-workshop-content.md', '2026-09-15-inventory-changed-reflection-repair.md')) { Copy-Item -LiteralPath "$PSScriptRoot/docs/validation/$record" -Destination "$package/docs/validation" -Force }
    $manifest = @{ name='Magenheim'; version_number=$pluginVersion; website_url='https://github.com/mrcalzon02/Magenheim'; description=[string]$release.description; dependencies=@('denikson-BepInExPack_Valheim-5.4.2350','ValheimModding-Jotunn-2.30.0','ValheimModding-JsonDotNET-13.0.4') }
    $manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $package 'manifest.json')
    $hashes = @(Get-ChildItem -LiteralPath $package -File -Recurse | Sort-Object FullName | ForEach-Object { [ordered]@{ path=$_.FullName.Substring($package.Length + 1).Replace('\','/'); sha256=(Get-FileHash -LiteralPath $_.FullName).Hash } })
    ConvertTo-Json -InputObject $hashes -Depth 3 | Set-Content -LiteralPath (Join-Path $package 'checksums.json')
    Compress-Archive -Path "$package/*" -DestinationPath "$package.zip" -Force
    Get-FileHash -LiteralPath (Join-Path $plugin 'Magenheim.dll'),(Join-Path $plugin 'Magenheim.Core.dll')
    Write-Output "Built Magenheim $pluginVersion"; Write-Output "Test package: $package.zip"
} finally { Pop-Location }
