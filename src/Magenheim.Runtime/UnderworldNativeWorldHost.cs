using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using Magenheim.Core.Underworld;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Magenheim.Runtime;

/// <summary>
/// Creates the native Underworld world-service bundle inside the current Valheim save/session.
/// The Underworld owns a separate Unity scene/physics scene and separate WorldGenerator,
/// ZoneSystem and ZDOMan instances. It does not allocate host coordinates or another save.
/// </summary>
internal sealed class UnderworldNativeWorldHost : IDisposable
{
    private const string ScenePrefix = "Magenheim_Underworld_";
    private readonly ValheimWorldInstanceRegistry _registry;
    private readonly ManualLogSource _log;
    private Scene _scene;
    private GameObject? _zoneRoot;
    private ZDOMan? _underworldZdoMan;
    private ValheimPathfindingInstanceState? _underworldPathfindingState;
    private bool _disposed;

    internal UnderworldNativeWorldHost(ValheimWorldInstanceRegistry registry, ManualLogSource log)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    internal bool TryCreate(UnderworldWorldIdentity identity, out string diagnostic)
    {
        diagnostic = string.Empty;
        if (_disposed) { diagnostic = "Underworld native world host is disposed."; return false; }
        if (_registry.HasUnderworldContext) return true;

        if (!_registry.TryGetContext(UnderworldWorldInstanceId.Surface, out var surface) || surface is null)
        {
            diagnostic = "Surface native Valheim services are not bound.";
            return false;
        }

        try
        {
            var parameters = new CreateSceneParameters(LocalPhysicsMode.Physics3D);
            _scene = SceneManager.CreateScene(ScenePrefix + identity.DerivedSeedFingerprint, parameters);
            var physics = _scene.GetPhysicsScene();
            if (!_scene.IsValid() || !physics.IsValid())
                throw new InvalidOperationException("Unity did not create a valid Underworld scene/physics scene.");

            var world = CloneWorld(surface.World, identity);
            var generator = CreateWorldGenerator(world);
            var zdoMan = CreateZdoMan(surface.ZdoMan);
            _underworldZdoMan = zdoMan;
            ValidateDistinctZdoNamespace(surface.ZdoMan, zdoMan);
            var zoneSystem = CreateZoneSystem(surface.ZoneSystem, _scene);
            UnderworldZoneSystemLifetimeGuard.Register(zoneSystem, surface.ZoneSystem);
            var pathfinding = Pathfinding.instance
                ?? throw new InvalidOperationException("Valheim Pathfinding singleton is unavailable.");
            var pathfindingState = ValheimPathfindingInstanceState.CreateUnderworld(pathfinding, _log);
            _underworldPathfindingState = pathfindingState;
            UnderworldTerrainRuntime.CaptureInstanceWaterLevel(zoneSystem.m_waterLevel);

            var context = new ValheimWorldInstanceContext(
                UnderworldWorldInstanceId.Underworld,
                world,
                generator,
                zoneSystem,
                zdoMan,
                _scene,
                physics,
                pathfindingState);

            _registry.Bind(context);
            ValheimWorldInstanceExecution.BindRegistry(_registry);

            // Awake/Start of the copied ZoneSystem must see its own native world services.
            using (ValheimWorldInstanceExecution.Enter(context))
                _zoneRoot!.SetActive(true);

            ValidateSurfaceSingletonsRestored(surface);

            _log.LogInfo(
                $"Created native Underworld world instance '{identity.DerivedWorldId}' in Unity scene '{_scene.name}' " +
                "with independent WorldGenerator/ZoneSystem/ZDOMan/Pathfinding state authority.");
            return true;
        }
        catch (Exception exception)
        {
            diagnostic = "Native Underworld world creation failed: " + exception.Message;
            _log.LogError(diagnostic + Environment.NewLine + exception);
            DestroyPartial();
            return false;
        }
    }

    private static void ValidateSurfaceSingletonsRestored(ValheimWorldInstanceContext surface)
    {
        if (!ReferenceEquals(ZDOMan.instance, surface.ZdoMan))
            throw new InvalidOperationException("Underworld admission did not restore Surface ZDOMan.instance.");
        if (!ReferenceEquals(ZoneSystem.instance, surface.ZoneSystem))
            throw new InvalidOperationException("Underworld admission did not restore Surface ZoneSystem.instance.");
        if (!ReferenceEquals(WorldGenerator.instance, surface.WorldGenerator))
            throw new InvalidOperationException("Underworld admission did not restore Surface WorldGenerator.instance.");
        if (!ReferenceEquals(ZNet.World, surface.World))
            throw new InvalidOperationException("Underworld admission did not restore Surface ZNet.World.");
    }

    private static World CloneWorld(World source, UnderworldWorldIdentity identity)
    {
        var clone = (World)(typeof(object)
            .GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(source, Array.Empty<object>()) ?? throw new InvalidOperationException("Valheim World clone failed."));

        foreach (var field in typeof(World).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            var value = field.GetValue(source);
            if (value is null || field.FieldType.IsValueType || value is string || value is Delegate)
                continue;

            if (field.Name == "m_biomeData")
            {
                field.SetValue(clone, null);
                continue;
            }

            if (TryCloneCollection(field.FieldType, value, out var detachedCollection))
            {
                field.SetValue(clone, detachedCollection);
                continue;
            }

            throw new InvalidOperationException(
                $"Cannot isolate mutable Valheim World field '{field.Name}' ({field.FieldType.FullName}) for the Underworld instance.");
        }

        SetFieldIfPresent(clone, "m_name", GetFieldRequired<string>(source, "m_name") + "::MagenheimUnderworld");
        SetFieldIfPresent(clone, "m_seedName", identity.DerivedSeedFingerprint);
        SetFieldIfPresent(clone, "m_seed", identity.DerivedSeed32);
        SetFieldIfPresent(clone, "m_uid", StableInstanceUid(GetFieldRequired<long>(source, "m_uid"), identity.DerivedSeedFingerprint));
        ValidateDetachedWorldState(source, clone);
        return clone;
    }

    private static void ValidateDetachedWorldState(World surface, World underworld)
    {
        foreach (var field in typeof(World).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (field.FieldType.IsValueType || field.FieldType == typeof(string) || typeof(Delegate).IsAssignableFrom(field.FieldType))
                continue;

            var surfaceValue = field.GetValue(surface);
            var underworldValue = field.GetValue(underworld);
            if (surfaceValue is null || underworldValue is null) continue;
            if (ReferenceEquals(surfaceValue, underworldValue))
                throw new InvalidOperationException(
                    $"Underworld World still shares mutable field '{field.Name}' with Surface.");
        }
    }

    private static long StableInstanceUid(long parentUid, string fingerprint)
    {
        unchecked
        {
            ulong hash = 1469598103934665603UL;
            foreach (var ch in fingerprint)
            {
                hash ^= ch;
                hash *= 1099511628211UL;
            }
            hash ^= (ulong)parentUid;
            hash *= 1099511628211UL;
            return (long)(hash & 0x7fffffffffffffffUL);
        }
    }

    private static WorldGenerator CreateWorldGenerator(World world)
    {
        var ctor = typeof(WorldGenerator).GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(World) },
            null) ?? throw new MissingMethodException(typeof(WorldGenerator).FullName, ".ctor(World)");

        // Valheim's constructor clears process-global biome memo tables even though the generator
        // itself is an ordinary per-world object. Preserve Surface's memo contents while creating
        // instance 1; Underworld bypasses those static memos entirely through Harmony patches.
        var cacheSnapshot = UnderworldWorldGeneratorCacheIsolation.CaptureSurfaceCaches();
        try
        {
            return (WorldGenerator)(ctor.Invoke(new object[] { world })
                ?? throw new InvalidOperationException("WorldGenerator constructor returned null."));
        }
        finally
        {
            UnderworldWorldGeneratorCacheIsolation.RestoreSurfaceCaches(cacheSnapshot);
        }
    }

    private static ZDOMan CreateZdoMan(ZDOMan surface)
    {
        // Do not call ZDOMan(int) a second time. Valheim's constructor mutates the process-wide
        // ZDOMan singleton, registers duplicate DestroyZDO/RequestZDO routed RPC handlers, resets
        // the global ZDOID allocator and reinitializes the global ZDOExtraData store. A second call
        // can therefore corrupt Surface state before the Underworld has admitted a single zone.
        //
        // Start from Valheim's already-initialized native manager layout, then detach every mutable
        // instance store. Native ZDOMan methods remain the implementation authority.
        var clone = (ZDOMan)(typeof(object)
            .GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(surface, Array.Empty<object>())
            ?? throw new InvalidOperationException("Valheim ZDOMan clone failed."));

        var identityFields = Array.FindAll(
            typeof(ZDOMan).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            field => field.Name is "m_sessionID" or "m_myid");
        if (identityFields.Length == 0)
            throw new MissingFieldException(typeof(ZDOMan).FullName, "m_sessionID/m_myid");
        var detachedSessionId = GenerateDistinctSessionId(surface, identityFields[0]);

        foreach (var field in typeof(ZDOMan).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            var value = field.GetValue(surface);
            if (field.Name is "m_sessionID" or "m_myid")
            {
                SetField(field, clone, Convert.ChangeType(detachedSessionId, field.FieldType));
                continue;
            }

            if (field.Name == "m_nextUid")
            {
                SetField(field, clone, Convert.ChangeType(1u, field.FieldType));
                continue;
            }

            if (field.Name == "m_nextSendPeer")
            {
                SetField(field, clone, Convert.ChangeType(-1, field.FieldType));
                continue;
            }

            if (field.Name == "m_saveData")
            {
                SetField(field, clone, null);
                continue;
            }

            if (value is null || field.FieldType.IsValueType || value is string || value is Delegate)
                continue;

            if (value is Array array)
            {
                SetField(field, clone, Array.CreateInstance(array.GetType().GetElementType()!, ArrayLengths(array)));
                continue;
            }

            if (field.Name == "m_tempNearObjectsForRemoval")
            {
                SetField(field, clone, CloneNestedScratchList(value));
                continue;
            }

            if (field.Name == "m_tempNearObjectsForRemovalAreasActive")
            {
                SetField(field, clone, CloneValueList(value));
                continue;
            }

            if (TryCreateEmptyCollection(value.GetType(), out var emptyCollection))
            {
                SetField(field, clone, emptyCollection);
                continue;
            }

            if (TryCreateFreshReference(value.GetType(), out var freshReference))
            {
                SetField(field, clone, freshReference);
                continue;
            }

            throw new InvalidOperationException(
                $"Cannot isolate mutable ZDOMan field '{field.Name}' ({field.FieldType.FullName}) for the Underworld instance.");
        }

        ValidateDetachedZdoState(surface, clone);
        return clone;
    }

    private static long GenerateDistinctSessionId(ZDOMan surface, FieldInfo identityField)
    {
        var surfaceId = Convert.ToInt64(identityField.GetValue(surface));
        var generateUid = AccessTools.Method(typeof(Utils), "GenerateUID", Type.EmptyTypes)
            ?? throw new MissingMethodException(typeof(Utils).FullName, "GenerateUID()");
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var candidate = Convert.ToInt64(generateUid.Invoke(null, Array.Empty<object>()));
            if (candidate != 0L && candidate != surfaceId) return candidate;
        }
        throw new InvalidOperationException("Valheim did not provide a distinct non-zero session ID for the Underworld ZDO namespace.");
    }

    private static int[] ArrayLengths(Array array)
    {
        var lengths = new int[array.Rank];
        for (var i = 0; i < lengths.Length; i++) lengths[i] = array.GetLength(i);
        return lengths;
    }

    private static object CloneNestedScratchList(object source)
    {
        if (source is not IList list)
            throw new InvalidOperationException("ZDOMan nested scratch field is no longer an IList.");
        var clone = CreateCollectionInstance(source.GetType(), source.GetType()) as IList
            ?? throw new InvalidOperationException("Unable to create the ZDOMan nested scratch list.");
        foreach (var item in list)
        {
            if (item is null) { clone.Add(null); continue; }
            if (!TryCreateEmptyCollection(item.GetType(), out var inner))
                throw new InvalidOperationException($"Unable to detach nested ZDOMan scratch collection {item.GetType().FullName}.");
            clone.Add(inner);
        }
        return clone;
    }

    private static object CloneValueList(object source)
    {
        if (source is not IList list)
            throw new InvalidOperationException("ZDOMan value scratch field is no longer an IList.");
        var clone = CreateCollectionInstance(source.GetType(), source.GetType()) as IList
            ?? throw new InvalidOperationException("Unable to create the ZDOMan value scratch list.");
        foreach (var item in list) clone.Add(item);
        return clone;
    }

    private static bool TryCreateEmptyCollection(Type type, out object? collection)
    {
        collection = null;
        if (type == typeof(string) || typeof(Delegate).IsAssignableFrom(type) || !typeof(IEnumerable).IsAssignableFrom(type))
            return false;
        try
        {
            collection = Activator.CreateInstance(type);
            return collection is not null;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryCreateFreshReference(Type type, out object? value)
    {
        value = null;
        if (type.IsAbstract || type.IsInterface || typeof(UnityEngine.Object).IsAssignableFrom(type))
            return false;
        try
        {
            value = Activator.CreateInstance(type, nonPublic: true);
            return value is not null;
        }
        catch
        {
            return false;
        }
    }

    private static void SetField(FieldInfo field, object target, object? value)
    {
        try
        {
            field.SetValue(target, value);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"Unable to initialize detached ZDOMan field '{field.Name}'.",
                exception);
        }
    }

    private static void ValidateDetachedZdoState(ZDOMan surface, ZDOMan underworld)
    {
        foreach (var field in typeof(ZDOMan).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (field.FieldType.IsValueType || field.FieldType == typeof(string) || typeof(Delegate).IsAssignableFrom(field.FieldType))
                continue;
            var surfaceValue = field.GetValue(surface);
            var underworldValue = field.GetValue(underworld);
            if (surfaceValue is null || underworldValue is null) continue;
            if (ReferenceEquals(surfaceValue, underworldValue))
                throw new InvalidOperationException(
                    $"Detached Underworld ZDOMan still shares mutable field '{field.Name}' with Surface.");
        }

        var objects = AccessTools.Field(typeof(ZDOMan), "m_objectsByID")?.GetValue(underworld) as IDictionary
            ?? throw new MissingFieldException(typeof(ZDOMan).FullName, "m_objectsByID");
        if (objects.Count != 0)
            throw new InvalidOperationException("Detached Underworld ZDOMan inherited Surface ZDO objects.");

        var peers = AccessTools.Field(typeof(ZDOMan), "m_peers")?.GetValue(underworld) as IEnumerable
            ?? throw new MissingFieldException(typeof(ZDOMan).FullName, "m_peers");
        foreach (var _ in peers)
            throw new InvalidOperationException("Detached Underworld ZDOMan inherited Surface peers.");
    }

    private static void ValidateDistinctZdoNamespace(ZDOMan surface, ZDOMan underworld)
    {
        var identityField =
            AccessTools.Field(typeof(ZDOMan), "m_sessionID") ??
            throw new MissingFieldException(typeof(ZDOMan).FullName, "m_sessionID/m_myid");
        var surfaceId = Convert.ToInt64(identityField.GetValue(surface));
        var underworldId = Convert.ToInt64(identityField.GetValue(underworld));

        if (surfaceId == 0L || underworldId == 0L)
            throw new InvalidOperationException(
                $"Valheim ZDO manager identity is invalid (Surface={surfaceId}, Underworld={underworldId}).");
        if (surfaceId == underworldId)
            throw new InvalidOperationException(
                $"Valheim assigned the same ZDO manager identity {surfaceId} to Surface and Underworld. " +
                "A second native ZDO namespace cannot be admitted safely.");
    }

    private ZoneSystem CreateZoneSystem(ZoneSystem source, Scene scene)
    {
        _zoneRoot = new GameObject("Magenheim_Underworld_ZoneSystem");
        _zoneRoot.SetActive(false);
        SceneManager.MoveGameObjectToScene(_zoneRoot, scene);
        var clone = _zoneRoot.AddComponent<ZoneSystem>();
        CopyZoneConfiguration(source, clone);

        // Surface has already assembled the native/Jotunn catalog before instance 1 is admitted.
        // Keep the populated location/vegetation/hash catalogs, but remove the source prefab lists
        // before Awake so Valheim cannot instantiate a second process-global set of
        // LocationList / AltBiomeList objects.
        ClearCatalogSourcePrefabs(clone);
        ValidateDetachedZoneSystemState(source, clone);
        return clone;
    }

    private static void CopyZoneConfiguration(ZoneSystem source, ZoneSystem target)
    {
        foreach (var field in typeof(ZoneSystem).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (field.IsLiteral) continue;

            if (IsSharedCampaignStateField(field.Name))
            {
                // Global keys belong to the parent campaign/save, not to a physical world instance.
                // Share exactly these native collections; generated zones/locations remain detached.
                field.SetValue(target, field.GetValue(source));
                continue;
            }

            if (field.IsInitOnly || IsRuntimeStateField(field.Name))
                continue;

            var value = field.GetValue(source);
            if (value is null)
            {
                // Preserve the target's constructor/field-initializer defaults rather than
                // overwriting a runtime-owned collection with null.
                continue;
            }

            if (value is UnityEngine.Object || field.FieldType.IsValueType || value is string)
            {
                field.SetValue(target, value);
                continue;
            }

            if (TryCloneCollection(field.FieldType, value, out var clone))
                field.SetValue(target, clone);
            // Unsupported reference types deliberately retain the fresh ZoneSystem default.
            // Sharing arbitrary mutable runtime state with Surface would violate instance isolation.
        }
    }

    private static void ClearCatalogSourcePrefabs(ZoneSystem target)
    {
        foreach (var name in new[] { "m_locationLists", "m_altBiomeLists" })
        {
            var field = AccessTools.Field(typeof(ZoneSystem), name)
                ?? throw new MissingFieldException(typeof(ZoneSystem).FullName, name);
            if (field.GetValue(target) is not IList list)
                throw new InvalidOperationException(
                    $"Underworld ZoneSystem source list '{name}' is not IList-compatible.");
            list.Clear();
        }
    }

    private static void ValidateDetachedZoneSystemState(ZoneSystem surface, ZoneSystem underworld)
    {
        foreach (var name in new[]
        {
            "m_zonePrefab",
            "m_zoneCtrlPrefab",
            "m_locationProxyPrefab",
            "m_waterLevel",
            "m_locationVersion",
            "m_locationScenes",
            "m_locationLists",
            "m_altBiomeLists",
            "m_vegetation",
            "m_locations",
            "m_locationsByHash",
        })
        {
            var field = AccessTools.Field(typeof(ZoneSystem), name)
                ?? throw new MissingFieldException(typeof(ZoneSystem).FullName, name);
            var sourceValue = field.GetValue(surface);
            var targetValue = field.GetValue(underworld);

            if (sourceValue is IList sourceList)
            {
                if (targetValue is not IList targetList)
                    throw new InvalidOperationException($"Underworld ZoneSystem field '{name}' lost its list configuration.");
                if (ReferenceEquals(sourceList, targetList))
                    throw new InvalidOperationException($"Underworld ZoneSystem field '{name}' still aliases Surface.");

                if (name is "m_locationLists" or "m_altBiomeLists")
                {
                    if (targetList.Count != 0)
                        throw new InvalidOperationException(
                            $"Underworld ZoneSystem source list '{name}' retained {targetList.Count} prefab entries and would duplicate process-global catalog registration during Awake.");
                    continue;
                }

                if (sourceList.Count != targetList.Count)
                    throw new InvalidOperationException(
                        $"Underworld ZoneSystem field '{name}' copied {targetList.Count} entries from Surface's {sourceList.Count}.");
                continue;
            }

            if (sourceValue is IDictionary sourceDictionary)
            {
                if (targetValue is not IDictionary targetDictionary)
                    throw new InvalidOperationException($"Underworld ZoneSystem field '{name}' lost its dictionary configuration.");
                if (ReferenceEquals(sourceDictionary, targetDictionary))
                    throw new InvalidOperationException($"Underworld ZoneSystem field '{name}' still aliases Surface.");
                if (sourceDictionary.Count != targetDictionary.Count)
                    throw new InvalidOperationException(
                        $"Underworld ZoneSystem field '{name}' copied {targetDictionary.Count} entries from Surface's {sourceDictionary.Count}.");
                continue;
            }

            if (sourceValue is UnityEngine.Object)
            {
                if (!ReferenceEquals(sourceValue, targetValue))
                    throw new InvalidOperationException($"Underworld ZoneSystem asset field '{name}' did not preserve Valheim's configured prefab reference.");
                continue;
            }

            if (!Equals(sourceValue, targetValue))
                throw new InvalidOperationException($"Underworld ZoneSystem scalar field '{name}' did not preserve Surface configuration.");
        }

        foreach (var field in typeof(ZoneSystem).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (field.FieldType.IsValueType || field.FieldType == typeof(string) ||
                typeof(Delegate).IsAssignableFrom(field.FieldType) ||
                typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
                continue;

            var surfaceValue = field.GetValue(surface);
            var underworldValue = field.GetValue(underworld);
            if (surfaceValue is null || underworldValue is null) continue;

            if (IsSharedCampaignStateField(field.Name))
            {
                if (!ReferenceEquals(surfaceValue, underworldValue))
                    throw new InvalidOperationException(
                        $"Campaign global-key field '{field.Name}' is not shared between Surface and Underworld.");
                continue;
            }

            if (ReferenceEquals(surfaceValue, underworldValue))
                throw new InvalidOperationException(
                    $"Underworld ZoneSystem still shares mutable field '{field.Name}' ({field.FieldType.FullName}) with Surface.");
        }
    }

    private static bool IsSharedCampaignStateField(string name) =>
        string.Equals(name, "m_globalKeys", StringComparison.Ordinal) ||
        string.Equals(name, "m_globalKeysEnums", StringComparison.Ordinal) ||
        string.Equals(name, "m_globalKeysValues", StringComparison.Ordinal);

    private static bool IsRuntimeStateField(string name)
    {
        var n = name.ToLowerInvariant();
        return n.Contains("generated") ||
               n.Contains("loaded") ||
               n.Contains("active") ||
               n.Contains("spawned") ||
               n.Contains("locationinstances") ||
               n.Contains("globalkeys") ||
               n.Contains("temp") ||
               n.Contains("cached");
    }

    private static bool TryCloneCollection(Type declaredType, object value, out object? clone)
    {
        clone = null;

        if (value is Array array)
        {
            clone = array.Clone();
            return true;
        }

        if (value is IDictionary dictionary)
        {
            var concrete = CreateCollectionInstance(declaredType, value.GetType()) as IDictionary;
            if (concrete is null) return false;
            foreach (DictionaryEntry entry in dictionary) concrete.Add(entry.Key, entry.Value);
            clone = concrete;
            return true;
        }

        if (value is IList list)
        {
            var concrete = CreateCollectionInstance(declaredType, value.GetType()) as IList;
            if (concrete is null) return false;
            foreach (var item in list) concrete.Add(item);
            clone = concrete;
            return true;
        }

        return false;
    }

    private static object? CreateCollectionInstance(Type declaredType, Type runtimeType)
    {
        foreach (var type in new[] { runtimeType, declaredType })
        {
            if (type.IsArray || type.IsInterface || type.IsAbstract) continue;
            try
            {
                return Activator.CreateInstance(type);
            }
            catch
            {
                // Try the next candidate. Unsupported fields retain their fresh target default.
            }
        }
        return null;
    }

    private static T GetFieldRequired<T>(object target, string name)
    {
        var field = AccessTools.Field(target.GetType(), name)
            ?? throw new MissingFieldException(target.GetType().FullName, name);
        var value = field.GetValue(target);
        if (value is T typed) return typed;
        throw new InvalidOperationException($"{target.GetType().Name}.{name} did not contain {typeof(T).Name}.");
    }

    private static void SetFieldIfPresent(object target, string name, object? value)
    {
        var field = AccessTools.Field(target.GetType(), name);
        if (field is not null) field.SetValue(target, value);
    }

    private void DestroyPartial()
    {
        if (_zoneRoot) UnityEngine.Object.Destroy(_zoneRoot);
        _zoneRoot = null;

        var zdoMan = _underworldZdoMan;
        _underworldZdoMan = null;
        var pathfindingState = _underworldPathfindingState;
        _underworldPathfindingState = null;

        if (_scene.IsValid() && _scene.isLoaded)
        {
            var unload = SceneManager.UnloadSceneAsync(_scene);
            if (unload is not null)
            {
                unload.completed += _ =>
                {
                    pathfindingState?.Dispose();
                    pathfindingState?.Dispose();
        ReleaseDetachedZdoState(zdoMan);
                };
                return;
            }
        }

        ReleaseDetachedZdoState(zdoMan);
    }

    private static void ReleaseDetachedZdoState(ZDOMan? zdoMan)
    {
        if (zdoMan is null) return;

        // Never call ZDOMan.ShutDown() on the child manager: vanilla ShutDown performs the
        // process-wide ZDOExtraData.Reset(), which would erase Surface state. Once the Underworld
        // scene has unloaded (all ZNetViews gone), release only this manager's native ZDO objects.
        // ZDOPool.Release -> ZDO.Reset -> ZDOExtraData.Release(uid), so shared static extra-data
        // stores lose only the globally unique instance-1 keys.
        var objectsField = AccessTools.Field(typeof(ZDOMan), "m_objectsByID")
            ?? throw new MissingFieldException(typeof(ZDOMan).FullName, "m_objectsByID");
        if (objectsField.GetValue(zdoMan) is not IDictionary objects)
            throw new InvalidOperationException("Underworld ZDOMan object store is not dictionary-compatible.");

        var zdos = new List<ZDO>(objects.Count);
        foreach (DictionaryEntry entry in objects)
            if (entry.Value is ZDO zdo) zdos.Add(zdo);

        foreach (var zdo in zdos)
            ZDOPool.Release(zdo);

        objects.Clear();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        DestroyPartial();
    }
}
