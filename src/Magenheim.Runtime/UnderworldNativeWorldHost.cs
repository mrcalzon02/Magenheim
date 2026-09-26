using System;
using System.Collections;
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
            var zoneSystem = CreateZoneSystem(surface.ZoneSystem, _scene);

            var context = new ValheimWorldInstanceContext(
                UnderworldWorldInstanceId.Underworld,
                world,
                generator,
                zoneSystem,
                zdoMan,
                _scene,
                physics);

            _registry.Bind(context);
            ValheimWorldInstanceExecution.BindRegistry(_registry);

            // Awake/Start of the copied ZoneSystem must see its own native world services.
            using (ValheimWorldInstanceExecution.Enter(context))
                _zoneRoot!.SetActive(true);

            _log.LogInfo(
                $"Created native Underworld world instance '{identity.DerivedWorldId}' in Unity scene '{_scene.name}' " +
                "with independent WorldGenerator/ZoneSystem/ZDOMan authority.");
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

    private static World CloneWorld(World source, UnderworldWorldIdentity identity)
    {
        var clone = (World)(typeof(object)
            .GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(source, Array.Empty<object>()) ?? throw new InvalidOperationException("Valheim World clone failed."));

        SetFieldIfPresent(clone, "m_name", source.m_name + "::MagenheimUnderworld");
        SetFieldIfPresent(clone, "m_seedName", identity.DerivedSeedFingerprint);
        SetFieldIfPresent(clone, "m_seed", identity.DerivedSeed32);
        SetFieldIfPresent(clone, "m_uid", StableInstanceUid(source.m_uid, identity.DerivedSeedFingerprint));
        return clone;
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
        return (WorldGenerator)(ctor.Invoke(new object[] { world })
            ?? throw new InvalidOperationException("WorldGenerator constructor returned null."));
    }

    private static ZDOMan CreateZdoMan(ZDOMan surface)
    {
        var sectors = AccessTools.Field(typeof(ZDOMan), "m_objectsBySector")?.GetValue(surface) as Array
            ?? throw new MissingFieldException(typeof(ZDOMan).FullName, "m_objectsBySector");
        var width = (int)Math.Round(Math.Sqrt(sectors.Length));
        if (width <= 0 || width * width != sectors.Length)
            throw new InvalidOperationException("Surface ZDO sector table is not square.");

        var ctor = typeof(ZDOMan).GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(int) },
            null) ?? throw new MissingMethodException(typeof(ZDOMan).FullName, ".ctor(int)");
        return (ZDOMan)(ctor.Invoke(new object[] { width })
            ?? throw new InvalidOperationException("ZDOMan constructor returned null."));
    }

    private ZoneSystem CreateZoneSystem(ZoneSystem source, Scene scene)
    {
        _zoneRoot = new GameObject("Magenheim_Underworld_ZoneSystem");
        _zoneRoot.SetActive(false);
        SceneManager.MoveGameObjectToScene(_zoneRoot, scene);
        var clone = _zoneRoot.AddComponent<ZoneSystem>();
        CopyZoneConfiguration(source, clone);
        return clone;
    }

    private static void CopyZoneConfiguration(ZoneSystem source, ZoneSystem target)
    {
        foreach (var field in typeof(ZoneSystem).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (field.IsInitOnly || field.IsLiteral || field.Name.IndexOf("generated", StringComparison.OrdinalIgnoreCase) >= 0)
                continue;

            var value = field.GetValue(source);
            if (value is null) { field.SetValue(target, null); continue; }
            if (value is UnityEngine.Object || field.FieldType.IsValueType || value is string)
            {
                field.SetValue(target, value);
                continue;
            }

            if (value is IList list)
            {
                if (Activator.CreateInstance(field.FieldType) is IList copy)
                {
                    foreach (var item in list) copy.Add(item);
                    field.SetValue(target, copy);
                }
                continue;
            }

            if (value is IDictionary dictionary)
            {
                if (Activator.CreateInstance(field.FieldType) is IDictionary copy)
                {
                    foreach (DictionaryEntry entry in dictionary) copy.Add(entry.Key, entry.Value);
                    field.SetValue(target, copy);
                }
            }
        }
    }

    private static void SetFieldIfPresent(object target, string name, object value)
    {
        var field = AccessTools.Field(target.GetType(), name);
        if (field is not null) field.SetValue(target, value);
    }

    private void DestroyPartial()
    {
        if (_zoneRoot) UnityEngine.Object.Destroy(_zoneRoot);
        _zoneRoot = null;
        if (_scene.IsValid() && _scene.isLoaded) SceneManager.UnloadSceneAsync(_scene);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        DestroyPartial();
    }
}
