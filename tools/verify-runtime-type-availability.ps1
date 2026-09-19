<#
.SYNOPSIS
    Fails the build when Magenheim.dll references a type the game cannot load at runtime.

.DESCRIPTION
    Magenheim.Runtime targets net462 and runs inside Valheim's Mono. The compiler is happy to emit
    references to assemblies that ship with the .NET Framework reference set but are simply not
    present in valheim_Data/Managed, and nothing complains until the game loads the field. The
    failure is then a TypeLoadException on a single member that takes out the whole registrar chain
    behind it -- content silently missing rather than an error anyone can read.

    System.ValueTuple is the one that keeps happening, because a C# tuple looks like language syntax
    rather than a dependency. Every assembly reference in the built module is checked against what
    the game actually ships, so any future member of this family is caught here instead of in a hand.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Assembly,
    [Parameter(Mandatory)][string]$ManagedDirectory,
    [Parameter(Mandatory)][string]$BepInExPath,
    [Parameter(Mandatory)][string]$CecilPath,
    [string[]]$ShippedAssemblies = @('Magenheim', 'Magenheim.Core')
)
$ErrorActionPreference = 'Stop'
Add-Type -Path $CecilPath

$module = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($Assembly).MainModule
# Everything actually loadable in the game process: the game's managed set, BepInEx's own core and
# whatever the profile has under plugins (Jotunn, Harmony), and the folder Magenheim ships into,
# which is where Magenheim.Core sits next to the assembly being checked.
$available = @{}
# Deliberately NOT the build output directory. That folder holds every assembly the compiler
# resolved against, including ones packaging never copies -- System.ValueTuple is exactly such a
# file, present in bin and absent from the zip -- so trusting it would make this gate agree with the
# build and disagree with the game. Only what ships counts, and build.ps1 ships two assemblies.
$roots = @(
    $ManagedDirectory,
    (Join-Path $BepInExPath 'core'),
    (Join-Path $BepInExPath 'plugins')
)
foreach ($root in $roots) {
    if (-not (Test-Path -LiteralPath $root)) { continue }
    foreach ($dll in Get-ChildItem -LiteralPath $root -Filter *.dll -Recurse -ErrorAction SilentlyContinue) {
        $available[[System.IO.Path]::GetFileNameWithoutExtension($dll.Name)] = $true
    }
}
foreach ($core in 'mscorlib', 'System', 'System.Core', 'System.Xml', 'netstandard') { $available[$core] = $true }
foreach ($shipped in $ShippedAssemblies) { $available[$shipped] = $true }

$missing = @()
foreach ($reference in $module.AssemblyReferences) {
    if (-not $available.ContainsKey($reference.Name)) { $missing += $reference.Name }
}

if ($missing.Count -gt 0) {
    $names = ($missing | Sort-Object -Unique) -join ', '
    throw "Magenheim references assemblies the game does not ship: $names. These compile but throw TypeLoadException at registration and silently remove every registrar behind them. A C# tuple is the usual cause -- use a small struct instead."
}
Write-Output "Verified $($module.AssemblyReferences.Count) assembly references against the game's managed set; all resolvable at runtime."
