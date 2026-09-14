param(
    [string]$GamePath = (Split-Path $PSScriptRoot -Parent),
    [string]$ModProfile = "$env:APPDATA\r2modmanPlus-local\Valheim\profiles\Central Fuckery"
)
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$managed = Join-Path $GamePath 'valheim_Data\Managed'
$core = Join-Path $ModProfile 'BepInEx\core'
$jotunn = Join-Path $ModProfile 'BepInEx\plugins\ValheimModding-Jotunn\Jotunn.dll'
$json = Join-Path $managed 'Newtonsoft.Json.dll'
$references = @(
    (Join-Path $core 'BepInEx.dll'), (Join-Path $core '0Harmony.dll'), $jotunn, $json,
    (Join-Path $managed 'assembly_valheim.dll'),
    (Join-Path $managed 'assembly_utils.dll'),
    (Join-Path $managed 'UnityEngine.AssetBundleModule.dll'),
    (Join-Path $managed 'UnityEngine.dll'),
    (Join-Path $managed 'UnityEngine.CoreModule.dll'),
    (Join-Path $managed 'UnityEngine.IMGUIModule.dll'),
    (Join-Path $managed 'UnityEngine.InputLegacyModule.dll'),
    (Join-Path $managed 'UnityEngine.PhysicsModule.dll'),
    (Join-Path $managed 'netstandard.dll')
)
foreach ($path in @($compiler) + $references) {
    if (!(Test-Path -LiteralPath $path)) { throw "Required build dependency missing: $path" }
}
$dist = Join-Path $PSScriptRoot 'dist'
$plugin = Join-Path $dist 'BepInEx\plugins\Magenheim'
$testDir = Join-Path $dist 'tests'
New-Item -ItemType Directory -Force -Path $plugin, $testDir | Out-Null
$source = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src\Magenheim') -Recurse -Filter '*.cs' | ForEach-Object FullName)
# Valheim supplies netstandard 2.1 for dependencies compiled against 2.0.
$argsList = @('/nologo', '/target:library', '/optimize+', '/warnaserror+', '/nowarn:1701', "/out:$plugin\Magenheim.dll")
$argsList += @($references | ForEach-Object { '/reference:' + $_ })
& $compiler @argsList @source
if ($LASTEXITCODE -ne 0) { throw 'Plugin compilation failed' }
New-Item -ItemType Directory -Force -Path (Join-Path $plugin 'default-data'), (Join-Path $plugin 'Translations') | Out-Null
Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'config\defaults') -File | Copy-Item -Destination (Join-Path $plugin 'default-data') -Force
Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'Translations') -File | Copy-Item -Destination (Join-Path $plugin 'Translations') -Force
# Tests compile only the pure core, without loading Unity or BepInEx.
$coreSource = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src\Magenheim\Core') -Filter '*.cs' | ForEach-Object FullName)
$testSource = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'tests\Magenheim.Tests') -Filter '*.cs' | ForEach-Object FullName)
& $compiler /nologo /target:exe /warnaserror+ /nowarn:1701 "/out:$testDir\Magenheim.Tests.exe" "/reference:$json" `
    "/reference:$managed\netstandard.dll" @coreSource @testSource
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed' }
Copy-Item -LiteralPath $json -Destination $testDir -Force
# Run the pure tests on installed .NET, whose loader supports the game's Unity-built JSON assembly.
'{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.0"},"rollForward":"LatestMajor"}}' | Set-Content -LiteralPath (Join-Path $testDir 'Magenheim.Tests.runtimeconfig.json') -Encoding UTF8
& dotnet (Join-Path $testDir 'Magenheim.Tests.exe') (Join-Path $PSScriptRoot 'config\defaults\foundation.json')
if ($LASTEXITCODE -ne 0) { throw 'Core tests failed' }
Write-Output "Built and tested: $plugin\Magenheim.dll"
