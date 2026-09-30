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

$profile = (Resolve-Path -LiteralPath $ProfileRoot).Path
if (!(Test-Path -LiteralPath "$profile/BepInEx/core/BepInEx.dll")) { throw 'The selected profile has no BepInEx loader.' }
if (!(Test-Path -LiteralPath "$GameRoot/valheim_Data/Managed/assembly_valheim.dll")) { throw "Valheim managed assemblies not found under $GameRoot." }

$pluginSource = Join-Path $PSScriptRoot 'src/Magenheim.Runtime/MagenheimPlugin.cs'
$pluginVersionLine = Select-String -LiteralPath $pluginSource -Pattern 'internal const string PluginVersion = "([^"]+)";' | Select-Object -First 1
if (!$pluginVersionLine -or $pluginVersionLine.Matches.Count -eq 0) { throw "Unable to read PluginVersion from $pluginSource" }
$pluginVersion = $pluginVersionLine.Matches[0].Groups[1].Value
if ($pluginVersion -notmatch '^\d+\.\d+\.\d+$') { throw 'Invalid plugin version.' }

$release = Get-Content -LiteralPath "$PSScriptRoot/release.json" -Raw | ConvertFrom-Json
if ([string]$release.version -ne $pluginVersion) { throw 'release.json version does not match PluginVersion.' }

[xml]$runtimeProject = Get-Content -LiteralPath "$PSScriptRoot/src/Magenheim.Runtime/Magenheim.Runtime.csproj"
if ([string]$runtimeProject.Project.PropertyGroup.Version -ne $pluginVersion) { throw 'Assembly/plugin version mismatch.' }

$restoreOptions = @()
if ($Offline) { $restoreOptions = @('-p:NuGetAudit=false','-p:RestoreIgnoreFailedSources=true') }

Push-Location $PSScriptRoot
try {
    Write-Host "LOCAL BUILD: compiling Magenheim $pluginVersion against installed Valheim/BepInEx."
    & $DotNet build src/Magenheim.Runtime -c Release @restoreOptions "-p:BepInExPath=$profile/BepInEx" "-p:ValheimManagedPath=$GameRoot/valheim_Data/Managed"
    if ($LASTEXITCODE -ne 0) { throw 'Local runtime compile failed.' }

    $output = Join-Path $PSScriptRoot 'src/Magenheim.Runtime/bin/Release/net462'
    foreach ($assembly in @('Magenheim.dll','Magenheim.Core.dll')) {
        if (!(Test-Path -LiteralPath (Join-Path $output $assembly))) { throw "Compiled assembly missing: $assembly" }
    }

    $assemblyVersion = [Reflection.AssemblyName]::GetAssemblyName((Join-Path $output 'Magenheim.dll')).Version.ToString(3)
    if ($assemblyVersion -ne $pluginVersion) { throw "Compiled assembly version $assemblyVersion does not match $pluginVersion." }

    $package = Join-Path $PSScriptRoot "dist/Local-Magenheim-$pluginVersion"
    if (Test-Path -LiteralPath $package) {
        $resolvedPackage = (Resolve-Path -LiteralPath $package).Path
        $distRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'dist'))
        if ([IO.Path]::GetDirectoryName($resolvedPackage) -ne $distRoot -or
            ((Get-Item -LiteralPath $package).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw "Refusing to clean unexpected package path: $resolvedPackage"
        }
        Remove-Item -LiteralPath $package -Recurse -Force
    }

    $plugin = Join-Path $package 'Magenheim'
    New-Item -ItemType Directory -Force -Path $plugin | Out-Null
    Copy-Item -LiteralPath (Join-Path $output 'Magenheim.dll') -Destination $plugin -Force
    Copy-Item -LiteralPath (Join-Path $output 'Magenheim.Core.dll') -Destination $plugin -Force

    # The csproj copies the committed runtime-consumable asset subset into the build output.
    # Local install consumes that committed output exactly as-is; it never authors/regenerates assets.
    foreach ($directory in @('assets','default-data')) {
        $sourceDirectory = Join-Path $output $directory
        if (!(Test-Path -LiteralPath $sourceDirectory -PathType Container)) {
            throw "Compiled runtime payload is missing committed $directory directory: $sourceDirectory"
        }
        Copy-Item -LiteralPath $sourceDirectory -Destination $plugin -Recurse -Force
    }

    foreach ($file in @('TESTING.md','README.md','CLOSEOUT.md','icon.png')) {
        $path = Join-Path $PSScriptRoot $file
        if (Test-Path -LiteralPath $path -PathType Leaf) { Copy-Item -LiteralPath $path -Destination $package -Force }
    }

    $manifest = [ordered]@{
        name='Magenheim'
        version_number=$pluginVersion
        website_url='https://github.com/mrcalzon02/Magenheim'
        description=[string]$release.description
        dependencies=@('denikson-BepInExPack_Valheim-5.4.2350','ValheimModding-Jotunn-2.30.0','ValheimModding-JsonDotNET-13.0.4')
    }
    $manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $package 'manifest.json')

    $hashes = @(Get-ChildItem -LiteralPath $package -File -Recurse | Sort-Object FullName | ForEach-Object {
        [ordered]@{
            path=$_.FullName.Substring($package.Length + 1).Replace('\','/')
            sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
        }
    })
    ConvertTo-Json -InputObject $hashes -Depth 3 | Set-Content -LiteralPath (Join-Path $package 'checksums.json')

    Compress-Archive -Path "$package/*" -DestinationPath "$package.zip" -Force
    Write-Host "LOCAL PACKAGE READY: $package"
} finally {
    Pop-Location
}
