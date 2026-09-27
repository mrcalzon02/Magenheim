param(
    [Parameter(Mandatory=$true)][string]$RuntimeDll,
    [Parameter(Mandatory=$true)][string]$GameManagedPath,
    [Parameter(Mandatory=$true)][string]$BepInExPath
)
$ErrorActionPreference = 'Stop'

$searchDirectories = @(
    (Split-Path -Parent $RuntimeDll),
    $GameManagedPath,
    (Join-Path $BepInExPath 'core'),
    (Join-Path $BepInExPath 'plugins')
) | Where-Object { Test-Path -LiteralPath $_ }
$searchDirectories += @(Get-ChildItem -LiteralPath (Join-Path $BepInExPath 'plugins') -Filter *.dll -Recurse |
    ForEach-Object { $_.DirectoryName } | Sort-Object -Unique)

if (-not ('MagenheimGateAssemblyResolver' -as [type])) {
    Add-Type @'
using System;
using System.IO;
using System.Reflection;
public sealed class MagenheimGateAssemblyResolver {
    public string[] Directories;
    public Assembly Resolve(object sender, ResolveEventArgs request) {
        string name = new AssemblyName(request.Name).Name + ".dll";
        foreach (string directory in Directories) {
            string path = Path.Combine(directory, name);
            if (File.Exists(path)) return Assembly.LoadFrom(path);
        }
        return null;
    }
}
'@
}
$resolver = New-Object MagenheimGateAssemblyResolver
$resolver.Directories = [string[]]$searchDirectories
$handler = [Delegate]::CreateDelegate([ResolveEventHandler], $resolver, 'Resolve')

[AppDomain]::CurrentDomain.add_AssemblyResolve($handler)
try {
    $runtime = [Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $RuntimeDll).Path)
    # This initializer only resolves managed fields; it does not enter a scene or call Unity.
    # Resolving Harmony targets alone missed a poisoned adapter initializer that broke startup.
    $adapter = $runtime.GetType('Magenheim.Runtime.ValheimWorldInstanceExecution', $true)
    [Runtime.CompilerServices.RuntimeHelpers]::RunClassConstructor($adapter.TypeHandle)
    foreach ($binding in @(
        @('ZoneInstance', 'ZoneSystem', $true),
        @('GeneratorInstance', 'WorldGenerator', $true),
        @('ZdoInstance', 'ZDOMan', $true),
        @('ZNetWorld', 'World', $true),
        @('ZNetZdoMan', 'ZDOMan', $false)
    )) {
        $field = $adapter.GetField($binding[0], [Reflection.BindingFlags]'Static,NonPublic').GetValue($null)
        if ($field -isnot [Reflection.FieldInfo] -or $field.FieldType.FullName -cne $binding[1] -or
            $field.IsStatic -ne $binding[2] -or $field.IsInitOnly -or $field.IsLiteral) {
            throw "Invalid writable native authority binding: $($binding[0])."
        }
    }
    Write-Output 'Verified world-instance adapter initialization and all five writable native authority bindings.'
    # Exercise the real copier against the installed game's readonly catalog field. No Unity
    # objects are constructed or activated; uninitialized shells contain managed collections only.
    $game = [Reflection.Assembly]::LoadFrom((Join-Path $GameManagedPath 'assembly_valheim.dll'))
    $zoneType = $game.GetType('ZoneSystem', $true)
    $instanceFlags = [Reflection.BindingFlags]'Instance,Public,NonPublic'
    $indexField = $zoneType.GetField('m_locationsByHash', $instanceFlags)
    $sourceZone = [Runtime.Serialization.FormatterServices]::GetUninitializedObject($zoneType)
    $targetZone = [Runtime.Serialization.FormatterServices]::GetUninitializedObject($zoneType)
    $sourceIndex = [Activator]::CreateInstance($indexField.FieldType)
    $targetIndex = [Activator]::CreateInstance($indexField.FieldType)
    $location = [Runtime.Serialization.FormatterServices]::GetUninitializedObject($indexField.FieldType.GetGenericArguments()[1])
    for ($i = 0; $i -lt 260; $i++) { $sourceIndex.Add($i, $location) }
    $targetIndex.Add(-1, $location)
    $indexField.SetValue($sourceZone, $sourceIndex)
    $indexField.SetValue($targetZone, $targetIndex)
    $copier = $runtime.GetType('Magenheim.Runtime.UnderworldNativeWorldHost', $true).GetMethod('CopyZoneConfiguration', [Reflection.BindingFlags]'Static,NonPublic')
    $copier.Invoke($null, @($sourceZone, $targetZone)) | Out-Null
    if ($targetIndex.Count -ne 260 -or $sourceIndex.Count -ne 260 -or $targetIndex.ContainsKey(-1) -or
        ![Object]::ReferenceEquals($indexField.GetValue($targetZone), $targetIndex) -or
        ![Object]::ReferenceEquals($targetIndex[259], $location)) { throw 'Native readonly location catalog copy failed.' }
    $targetIndex.Remove(0) | Out-Null
    if (!$sourceIndex.ContainsKey(0)) { throw 'Location catalog mutation leaked into Surface.' }
    Write-Output 'Verified actual native ZoneSystem copier: 260 catalog entries retained, readonly target preserved, stale entries removed, Surface mutation isolated.'
    $flags = [Reflection.BindingFlags]'Static,Public,NonPublic'
    $dynamic = 0
    $resolvedCount = 0

    # Inspect only resolver classes; unrelated game-facing types can contain interface features
    # supported by Unity Mono but unavailable in the Framework verifier process.
    Add-Type -Path (Join-Path $BepInExPath 'core/Mono.Cecil.dll')
    $metadata = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($RuntimeDll)
    $resolverNames = @($metadata.MainModule.Types | Where-Object {
        @($_.CustomAttributes | Where-Object { $_.AttributeType.FullName -eq 'HarmonyLib.HarmonyPatch' }).Count -gt 0 -and
        @($_.Methods | Where-Object Name -in @('TargetMethod', 'TargetMethods')).Count -gt 0
    } | ForEach-Object { $_.FullName })
    $metadata.Dispose()
    foreach ($typeName in $resolverNames) {
        $type = $runtime.GetType($typeName, $true)
        $single = $type.GetMethod('TargetMethod', $flags)
        $many = $type.GetMethod('TargetMethods', $flags)
        if (!$single -and !$many) { continue }
        if ($single -and $many) { throw "$($type.FullName) declares both TargetMethod and TargetMethods." }

        $dynamic++
        $targets = New-Object System.Collections.Generic.List[Reflection.MethodBase]
        if ($single) {
            $method = $single.Invoke($null, @())
            if ($null -eq $method -or !($method -is [Reflection.MethodBase])) {
                throw "$($type.FullName).TargetMethod did not resolve a MethodBase."
            }
            $targets.Add([Reflection.MethodBase]$method)
        } else {
            $sequence = $many.Invoke($null, @())
            if ($null -eq $sequence) { throw "$($type.FullName).TargetMethods returned null." }
            foreach ($method in $sequence) {
                if ($null -eq $method -or !($method -is [Reflection.MethodBase])) {
                    throw "$($type.FullName).TargetMethods emitted a non-MethodBase value."
                }
                $targets.Add([Reflection.MethodBase]$method)
            }
        }

        if ($targets.Count -eq 0) { throw "$($type.FullName) resolved zero dynamic Harmony targets." }

        $identities = @{}
        foreach ($method in $targets) {
            $identity = "$($method.Module.ModuleVersionId)|$($method.MetadataToken)"
            if ($identities.ContainsKey($identity)) {
                throw "$($type.FullName) resolved duplicate target $($method.DeclaringType.FullName).$($method.Name)."
            }
            $identities[$identity] = $true
            $resolvedCount++
        }

        Write-Output "Dynamic Harmony resolver $($type.FullName): $($targets.Count) target(s)."
    }

    if ($dynamic -eq 0) { throw 'No dynamic Harmony resolvers were inspected.' }
    Write-Output "Verified $dynamic dynamic Harmony resolver(s), $resolvedCount resolved installed-runtime target(s)."
}
finally {
    [AppDomain]::CurrentDomain.remove_AssemblyResolve($handler)
}
