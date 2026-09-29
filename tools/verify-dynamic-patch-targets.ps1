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

    # The dungeon placement reconciler depends on Valheim's persisted native location-generation
    # table and the current GenerateLocationsTimeSliced coroutine signature. Force its static
    # constructor now so a game update cannot silently defer a broken spawn contract to runtime.
    $placementRuntime = $runtime.GetType('Magenheim.Runtime.UnderworldDungeonPlacementRuntime', $true)
    [Runtime.CompilerServices.RuntimeHelpers]::RunClassConstructor($placementRuntime.TypeHandle)
    $generatedField = $zoneType.GetField('m_locationsGenerated', $instanceFlags)
    $instancesField = $zoneType.GetField('m_locationInstances', $instanceFlags)
    if ($generatedField -eq $null -or $generatedField.FieldType -ne [bool]) {
        throw 'ZoneSystem.m_locationsGenerated contract changed; Underworld dungeon placement reconciliation is unsafe.'
    }
    if ($instancesField -eq $null -or -not $instancesField.FieldType.IsGenericType) {
        throw 'ZoneSystem.m_locationInstances contract changed; Underworld dungeon placement audit is unavailable.'
    }
    $generationMethods = @($zoneType.GetMethods($instanceFlags) | Where-Object {
        $_.Name -ceq 'GenerateLocationsTimeSliced' -and $_.GetParameters().Length -eq 3
    })
    if ($generationMethods.Count -ne 1) {
        throw "Expected exactly one 3-argument ZoneSystem.GenerateLocationsTimeSliced method; found $($generationMethods.Count)."
    }
    $generationParams = $generationMethods[0].GetParameters()
    if ($generationParams[1].ParameterType.FullName -cne 'System.Diagnostics.Stopwatch' -or
        $generationParams[2].ParameterType.Name -cne 'ZPackage') {
        throw 'ZoneSystem.GenerateLocationsTimeSliced signature changed; Underworld missing-dungeon recovery is unsafe.'
    }
    Write-Output 'Verified native dungeon placement contracts: m_locationsGenerated, m_locationInstances, and GenerateLocationsTimeSliced.'

    $flags = [Reflection.BindingFlags]'Static,Public,NonPublic'
    $dynamic = 0
    $resolvedCount = 0

    # Inspect only resolver classes; unrelated game-facing types can contain interface features
    # supported by Unity Mono but unavailable in the Windows PowerShell 5.1 verifier process.
    Write-Output 'Dynamic patch gate v2: using Cecil metadata for Valheim resolver families that desktop CLR cannot safely materialize.'
    Add-Type -Path (Join-Path $BepInExPath 'core/Mono.Cecil.dll')
    $cecilResolver = New-Object Mono.Cecil.DefaultAssemblyResolver
    foreach ($directory in $searchDirectories) {
        if (Test-Path -LiteralPath $directory) { $cecilResolver.AddSearchDirectory($directory) }
    }
    $reader = New-Object Mono.Cecil.ReaderParameters
    $reader.AssemblyResolver = $cecilResolver
    $metadata = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($RuntimeDll, $reader)
    $gameMetadata = [Mono.Cecil.AssemblyDefinition]::ReadAssembly(
        (Join-Path $GameManagedPath 'assembly_valheim.dll'), $reader)
    $resolverNames = @($metadata.MainModule.Types | Where-Object {
        @($_.CustomAttributes | Where-Object { $_.AttributeType.FullName -eq 'HarmonyLib.HarmonyPatch' }).Count -gt 0 -and
        @($_.Methods | Where-Object Name -in @('TargetMethod', 'TargetMethods')).Count -gt 0
    } | ForEach-Object { $_.FullName })

    function Test-CecilDerivesFrom {
        param($TypeDefinition, [string]$ExpectedBase)
        $cursor = $TypeDefinition
        $seen = @{}
        while ($null -ne $cursor -and $null -ne $cursor.BaseType) {
            if ($cursor.BaseType.FullName -ceq $ExpectedBase) { return $true }
            if ($seen.ContainsKey($cursor.BaseType.FullName)) { return $false }
            $seen[$cursor.BaseType.FullName] = $true
            try { $cursor = $cursor.BaseType.Resolve() }
            catch { return $false }
        }
        return $false
    }

    function Get-CecilBehaviourTargetCount {
        param($AssemblyDefinition, [switch]$ExcludePathfinding)
        $callbackNames = @(
            'Awake','Start','Update','FixedUpdate','LateUpdate',
            'OnTriggerEnter','OnTriggerStay','OnTriggerExit',
            'OnCollisionEnter','OnCollisionStay','OnCollisionExit'
        )
        $count = 0
        foreach ($candidateType in $AssemblyDefinition.MainModule.Types) {
            if ($candidateType.IsAbstract) { continue }
            if ($ExcludePathfinding -and $candidateType.FullName -ceq 'Pathfinding') { continue }
            if (!(Test-CecilDerivesFrom $candidateType 'UnityEngine.MonoBehaviour')) { continue }
            foreach ($candidateMethod in $candidateType.Methods) {
                if ($candidateMethod.IsStatic -or $candidateMethod.IsAbstract -or
                    $candidateMethod.HasGenericParameters -or
                    $candidateMethod.Name -notin $callbackNames) { continue }
                $count++
            }
        }
        return $count
    }

    function Get-CecilGameMethodCount {
        param([string]$TypeName, [string[]]$Names)
        $type = $gameMetadata.MainModule.GetType($TypeName)
        if ($null -eq $type) { throw "Installed assembly_valheim.dll has no type '$TypeName'." }
        return @($type.Methods | Where-Object { $_.Name -in $Names }).Count
    }

    function Get-CecilCharacterScopeTargetCount {
        $names = @('Update','FixedUpdate','LateUpdate')
        $count = 0
        foreach ($typeName in @('Character','Player')) {
            $type = $gameMetadata.MainModule.GetType($typeName)
            if ($null -eq $type) { throw "Installed assembly_valheim.dll has no type '$typeName'." }
            foreach ($name in $names) {
                $matches = @($type.Methods | Where-Object { $_.Name -ceq $name })
                if ($matches.Count -gt 1) {
                    throw "Character instance-scope target '$typeName.$name' is ambiguous in installed metadata ($($matches.Count) methods)."
                }
                if ($matches.Count -eq 1) { $count++ }
            }
        }
        return $count
    }

    $cecilGameResolvers = @{
        'Magenheim.Runtime.UnderworldPrivateAreaRegistryScopePatch' = @('PrivateArea', @('CheckAccess'))
        'Magenheim.Runtime.UnderworldPieceRegistryScopePatch' = @('Piece', @('GetAllPiecesInRadius'))
        'Magenheim.Runtime.UnderworldCraftingStationRegistryScopePatch' = @('CraftingStation', @('HaveBuildStationInRange','FindClosestStationInRange','GetCraftingStation'))
        'Magenheim.Runtime.UnderworldStationExtensionRegistryScopePatch' = @('StationExtension', @('FindExtensions','FindClosestStationInRange','OtherExtensionInRange'))
        'Magenheim.Runtime.UnderworldTerrainModifierRegistryScopePatch' = @('TerrainModifier', @('GetAllInstances','FindClosestModifierPieceInRange','RemoveOthers','GetModifiers'))
        'Magenheim.Runtime.UnderworldZoneSystemInstanceScopePatch' = @('ZoneSystem', @('Awake','Start','Update','FixedUpdate','LateUpdate','SetupLocations','PlaceVegetation','GenerateLocationsTimeSliced','CreateLocalZones','CreateGhostZones','PokeLocalZone','SpawnZone'))
        'Magenheim.Runtime.UnderworldSpawnSystemInstanceScopePatch' = @('SpawnSystem', @('Awake','Start','Update','UpdateSpawning','UpdateSpawnList','FindBaseSpawnPoint','IsSpawnPointGood','Spawn'))
        'Magenheim.Runtime.UnderworldZNetSceneInstanceScopePatch' = @('ZNetScene', @('Update','CreateDestroyObjects','IsAreaReady'))
    }

    foreach ($typeName in $resolverNames) {
        if ($typeName -ceq 'Magenheim.Runtime.UnderworldValheimBehaviourInstanceScopePatch') {
            $dynamic++
            $metadataTargets = Get-CecilBehaviourTargetCount $gameMetadata -ExcludePathfinding
            if ($metadataTargets -le 0) {
                throw "$typeName resolved zero Valheim MonoBehaviour callback targets through Cecil metadata."
            }
            $resolvedCount += $metadataTargets
            Write-Output "Dynamic Harmony resolver $($typeName): $metadataTargets target(s) verified through Cecil metadata."
            continue
        }

        if ($typeName -ceq 'Magenheim.Runtime.UnderworldMagenheimBehaviourInstanceScopePatch') {
            $dynamic++
            $metadataTargets = Get-CecilBehaviourTargetCount $metadata
            if ($metadataTargets -le 0) {
                throw "$typeName resolved zero Magenheim MonoBehaviour callback targets through Cecil metadata."
            }
            $resolvedCount += $metadataTargets
            Write-Output "Dynamic Harmony resolver $($typeName): $metadataTargets target(s) verified through Cecil metadata."
            continue
        }

        if ($typeName -ceq 'Magenheim.Runtime.UnderworldCharacterInstanceScopePatch') {
            $dynamic++
            $metadataTargets = Get-CecilCharacterScopeTargetCount
            if ($metadataTargets -le 0) {
                throw "$typeName resolved zero Character/Player frame callback targets through Cecil metadata."
            }
            $resolvedCount += $metadataTargets
            Write-Output "Dynamic Harmony resolver $($typeName): $metadataTargets target(s) verified through Cecil metadata."
            continue
        }

        if ($cecilGameResolvers.ContainsKey($typeName)) {
            $dynamic++
            $spec = $cecilGameResolvers[$typeName]
            $metadataTargets = Get-CecilGameMethodCount ([string]$spec[0]) ([string[]]$spec[1])
            if ($metadataTargets -le 0) {
                throw "$typeName resolved zero installed-game targets through Cecil metadata."
            }
            $resolvedCount += $metadataTargets
            Write-Output "Dynamic Harmony resolver $($typeName): $metadataTargets target(s) verified through Cecil metadata."
            continue
        }

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
    if ($null -ne $metadata) { $metadata.Dispose() }
    if ($null -ne $gameMetadata) { $gameMetadata.Dispose() }
    [AppDomain]::CurrentDomain.remove_AssemblyResolve($handler)
}
