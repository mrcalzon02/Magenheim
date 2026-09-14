[CmdletBinding()]
param(
    [string]$ProfileRoot = (Join-Path $env:APPDATA 'r2modmanPlus-local/Valheim/profiles/Central Fuckery'),
    [string]$GameRoot = (Split-Path $PSScriptRoot -Parent),
    [string]$DotNet = (Join-Path $PSScriptRoot 'dist/toolchain/dotnet/dotnet.exe')
)
$ErrorActionPreference = 'Stop'
if (!(Test-Path -LiteralPath $DotNet)) { $DotNet = (Get-Command dotnet -ErrorAction Stop).Source }
$env:DOTNET_CLI_HOME = Join-Path $PSScriptRoot 'dist/toolchain/home'
$env:NUGET_PACKAGES = Join-Path $PSScriptRoot 'dist/toolchain/packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

$pluginSource = Join-Path $PSScriptRoot 'src/Magenheim.Runtime/MagenheimPlugin.cs'
$pluginVersionLine = Select-String -LiteralPath $pluginSource -Pattern 'internal const string PluginVersion = "([^"]+)";' | Select-Object -First 1
if (!$pluginVersionLine -or $pluginVersionLine.Matches.Count -eq 0) {
    throw "Unable to read PluginVersion from $pluginSource"
}
$pluginVersion = $pluginVersionLine.Matches[0].Groups[1].Value
if ([string]::IsNullOrWhiteSpace($pluginVersion)) { throw 'Resolved Magenheim plugin version is empty.' }

Push-Location $PSScriptRoot
try {
    & $DotNet run --project tests/Magenheim.Core.Tests -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Core tests failed.' }
    & $DotNet build src/Magenheim.Runtime -c Release "-p:BepInExPath=$ProfileRoot/BepInEx" "-p:ValheimManagedPath=$GameRoot/valheim_Data/Managed"
    if ($LASTEXITCODE -ne 0) { throw 'Runtime build failed.' }

    $package = Join-Path $PSScriptRoot "dist/Local-Magenheim-$pluginVersion"
    if (Test-Path -LiteralPath $package) {
        Remove-Item -LiteralPath $package -Recurse -Force
    }
    $plugin = Join-Path $package 'Magenheim'
    New-Item -ItemType Directory -Force -Path $plugin | Out-Null
    $output = Join-Path $PSScriptRoot 'src/Magenheim.Runtime/bin/Release/net462'

    # Only our assemblies belong in the package. The profile supplies Jotunn,
    # Newtonsoft.Json, BepInEx and Unity; never copy build-directory dependencies.
    foreach ($assembly in @('Magenheim.dll', 'Magenheim.Core.dll')) {
        Copy-Item -LiteralPath (Join-Path $output $assembly) -Destination $plugin -Force
    }
    Copy-Item -LiteralPath (Join-Path $output 'assets') -Destination $plugin -Recurse -Force
    Copy-Item -LiteralPath (Join-Path $output 'default-data') -Destination $plugin -Recurse -Force
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'TESTING.md') -Destination $package -Force
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.md') -Destination $package -Force
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'icon.png') -Destination $package -Force
    New-Item -ItemType Directory -Force -Path "$package/assets/earth", "$package/docs/validation" | Out-Null
    Copy-Item -Path "$PSScriptRoot/assets/earth/*preview.png" -Destination "$package/assets/earth" -Force
    foreach ($record in @('2026-09-14-earth-content-package.md', '2026-09-14-workshop-content.md')) {
        Copy-Item -LiteralPath "$PSScriptRoot/docs/validation/$record" -Destination "$package/docs/validation" -Force
    }

    $manifest = @{
        name = 'Magenheim'; version_number = $pluginVersion;
        website_url = 'https://github.com/mrcalzon02/Magenheim';
        description = 'Earth minerals and geology workshop: original assets, items, Crystal Shaping, and four buildable pieces.';
        dependencies = @('ValheimModding-Jotunn-2.30.0', 'ValheimModding-JsonDotNET-13.0.4')
    }
    $manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $package 'manifest.json')
    Compress-Archive -Path "$package/*" -DestinationPath "$package.zip" -Force

    Get-FileHash -LiteralPath (Join-Path $plugin 'Magenheim.dll'),(Join-Path $plugin 'Magenheim.Core.dll')
    Write-Output "Built Magenheim $pluginVersion"
    Write-Output "Test package: $package.zip"
} finally { Pop-Location }
