using System;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using Magenheim.Core.Underworld;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Magenheim.Runtime;

/// <summary>
/// Temporarily presents one world-instance's native services through Valheim's singleton seams.
/// Scopes are synchronous and nestable; Surface globals are restored on dispose.
/// </summary>
internal static class ValheimWorldInstanceExecution
{
    private static readonly AsyncLocal<ValheimWorldInstanceContext?> Current = new();
    private static ValheimWorldInstanceRegistry? _registry;

    private static readonly StaticMember ZoneInstance = StaticMember.Find(typeof(ZoneSystem), "instance", "m_instance");
    private static readonly StaticMember GeneratorInstance = StaticMember.Find(typeof(WorldGenerator), "instance", "m_instance");
    private static readonly FieldInfo? ZNetWorld = AccessTools.Field(typeof(ZNet), "m_world");
    private static readonly FieldInfo? ZNetZdoMan = AccessTools.Field(typeof(ZNet), "m_zdoMan");

    internal static void BindRegistry(ValheimWorldInstanceRegistry registry) =>
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));

    internal static ValheimWorldInstanceContext? Active => Current.Value;

    internal static IDisposable Enter(ValheimWorldInstanceContext context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        return new Scope(context);
    }

    internal static bool TryEnterScene(int sceneHandle, out IDisposable? scope)
    {
        scope = null;
        var registry = _registry;
        if (registry is null || !registry.TryGetContextForScene(sceneHandle, out var context) || context is null)
            return false;

        var current = Current.Value;
        if (ReferenceEquals(current, context))
            return false;

        // Surface is Valheim's ordinary ambient state. Avoid wrapping every Surface callback when no
        // Underworld scope is active; enter Surface only when unwinding/cross-calling from instance 1.
        if (context.InstanceId.IsSurface && (current is null || current.InstanceId.IsSurface))
            return false;

        scope = Enter(context);
        return true;
    }

    internal static bool TryGetAmbientContext(out ValheimWorldInstanceContext? context)
    {
        context = Current.Value;
        if (context is not null) return true;

        var registry = _registry;
        if (registry is null) return false;

        var localPlayer = Player.m_localPlayer;
        if (localPlayer &&
            registry.TryGetContextForScene(localPlayer.gameObject.scene.handle, out context) &&
            context is not null)
            return true;

        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid() &&
            registry.TryGetContextForScene(activeScene.handle, out context) &&
            context is not null)
            return true;

        context = null;
        return false;
    }

    internal static bool TryGetQueryScene(out Scene scene)
    {
        if (TryGetAmbientContext(out var context) && context is not null)
        {
            scene = context.Scene;
            return scene.IsValid();
        }

        scene = default;
        return false;
    }

    private sealed class Scope : IDisposable
    {
        private readonly ValheimWorldInstanceContext? _previousContext;
        private readonly object? _previousZone;
        private readonly object? _previousGenerator;
        private readonly object? _previousWorld;
        private readonly object? _previousZdoMan;
        private readonly Scene _previousActiveScene;
        private readonly bool _changedActiveScene;
        private bool _disposed;

        internal Scope(ValheimWorldInstanceContext context)
        {
            _previousContext = Current.Value;
            _previousZone = ZoneInstance.Get();
            _previousGenerator = GeneratorInstance.Get();
            _previousWorld = ZNetWorld?.GetValue(null);
            _previousZdoMan = ZNet.instance is null ? null : ZNetZdoMan?.GetValue(ZNet.instance);
            _previousActiveScene = SceneManager.GetActiveScene();
            _changedActiveScene =
                context.Scene.IsValid() &&
                context.Scene.isLoaded &&
                _previousActiveScene.handle != context.Scene.handle;

            if (_changedActiveScene && !SceneManager.SetActiveScene(context.Scene))
                throw new InvalidOperationException(
                    $"Unable to activate Unity scene '{context.Scene.name}' for world instance {context.InstanceId}.");

            Current.Value = context;
            ZoneInstance.Set(context.ZoneSystem);
            GeneratorInstance.Set(context.WorldGenerator);
            ZNetWorld?.SetValue(null, context.World);
            if (ZNet.instance is not null && ZNetZdoMan is not null)
                ZNetZdoMan.SetValue(ZNet.instance, context.ZdoMan);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (ZNet.instance is not null && ZNetZdoMan is not null) ZNetZdoMan.SetValue(ZNet.instance, _previousZdoMan);
            ZNetWorld?.SetValue(null, _previousWorld);
            GeneratorInstance.Set(_previousGenerator);
            ZoneInstance.Set(_previousZone);
            Current.Value = _previousContext;

            if (_changedActiveScene &&
                _previousActiveScene.IsValid() &&
                _previousActiveScene.isLoaded &&
                SceneManager.GetActiveScene().handle != _previousActiveScene.handle)
            {
                SceneManager.SetActiveScene(_previousActiveScene);
            }
        }
    }

    private sealed class StaticMember
    {
        private readonly FieldInfo? _field;
        private readonly PropertyInfo? _property;

        private StaticMember(FieldInfo? field, PropertyInfo? property)
        {
            _field = field;
            _property = property;
        }

        internal static StaticMember Find(Type type, params string[] names)
        {
            foreach (var name in names)
            {
                var field = AccessTools.Field(type, name);
                if (field is not null && field.IsStatic) return new StaticMember(field, null);

                var property = AccessTools.Property(type, name);
                if (property is not null && property.GetMethod?.IsStatic == true && property.SetMethod?.IsStatic == true)
                    return new StaticMember(null, property);
            }
            throw new MissingMemberException(type.FullName, string.Join("/", names));
        }

        internal object? Get() => _field is not null ? _field.GetValue(null) : _property!.GetValue(null, null);

        internal void Set(object? value)
        {
            if (_field is not null) _field.SetValue(null, value);
            else _property!.SetValue(null, value, null);
        }
    }
}

/// <summary>
/// Generic scene-context adapter for Valheim MonoBehaviour callbacks. This is the missing engine
/// discriminator for components that are not ZoneSystem, SpawnSystem or Character themselves:
/// AI, ships, cameras, interactables and other ordinary Valheim behaviours in instance 1 receive
/// the same World/WorldGenerator/ZoneSystem/ZDOMan/PhysicsScene context without custom replicas.
/// Surface callbacks are a no-op in the ordinary ambient state.
/// </summary>
[HarmonyPatch]
internal static class UnderworldValheimBehaviourInstanceScopePatch
{
    private static readonly string[] Names =
    {
        "Awake", "Start", "Update", "FixedUpdate", "LateUpdate",
        "OnTriggerEnter", "OnTriggerStay", "OnTriggerExit",
        "OnCollisionEnter", "OnCollisionStay", "OnCollisionExit",
    };

    internal static System.Collections.Generic.IEnumerable<MethodBase> TargetMethods()
    {
        var assembly = typeof(ZoneSystem).Assembly;
        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || !typeof(MonoBehaviour).IsAssignableFrom(type)) continue;
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (method.IsAbstract || method.ContainsGenericParameters || Array.IndexOf(Names, method.Name) < 0) continue;
                yield return method;
            }
        }
    }

    internal static void Prefix(object __instance, out IDisposable? __state)
    {
        __state = null;
        if (__instance is not Component component || !component) return;
        ValheimWorldInstanceExecution.TryEnterScene(component.gameObject.scene.handle, out __state);
    }

    internal static void Finalizer(IDisposable? __state) => __state?.Dispose();
}


/// <summary>
/// Ensures lifecycle callbacks on the Underworld ZoneSystem execute with its instance-native
/// World/WorldGenerator/ZDOMan bindings. Other ZoneSystem instances remain vanilla.
/// </summary>
[HarmonyPatch]
internal static class UnderworldZoneSystemInstanceScopePatch
{
    private static readonly string[] Names =
    {
        "Awake", "Start", "Update", "FixedUpdate", "LateUpdate",
        "SetupLocations", "PlaceVegetation", "GenerateLocationsTimeSliced",
    };

    internal static System.Collections.Generic.IEnumerable<MethodBase> TargetMethods()
    {
        foreach (var method in typeof(ZoneSystem).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            if (Array.IndexOf(Names, method.Name) >= 0)
                yield return method;
    }

    internal static void Prefix(ZoneSystem __instance, out IDisposable? __state)
    {
        __state = null;
        if (!__instance) return;
        ValheimWorldInstanceExecution.TryEnterScene(__instance.gameObject.scene.handle, out __state);
    }

    internal static void Finalizer(IDisposable? __state) => __state?.Dispose();
}

/// <summary>
/// GenerateLocationsTimeSliced is an iterator in current Valheim. Its MoveNext calls resume outside
/// the original method frame, so scope those resumptions too; otherwise location instantiation can
/// fall back into the Surface Unity scene between yields.
/// </summary>
[HarmonyPatch]
internal static class UnderworldZoneSystemLocationIteratorScopePatch
{
    internal static System.Collections.Generic.IEnumerable<MethodBase> TargetMethods()
    {
        foreach (var method in typeof(ZoneSystem).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (!string.Equals(method.Name, "GenerateLocationsTimeSliced", StringComparison.Ordinal)) continue;
            var moveNext = AccessTools.EnumeratorMoveNext(method);
            if (moveNext is not null) yield return moveNext;
        }
    }

    internal static void Prefix(object __instance, out IDisposable? __state)
    {
        __state = null;
        if (__instance is null) return;

        ZoneSystem? zoneSystem = null;
        foreach (var field in __instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (!typeof(ZoneSystem).IsAssignableFrom(field.FieldType)) continue;
            zoneSystem = field.GetValue(__instance) as ZoneSystem;
            if (zoneSystem) break;
        }

        if (!zoneSystem) return;
        ValheimWorldInstanceExecution.TryEnterScene(zoneSystem.gameObject.scene.handle, out __state);
    }

    internal static void Finalizer(IDisposable? __state) => __state?.Dispose();
}


/// <summary>
/// Routes native _ZoneCtrl SpawnSystem callbacks through the world services owned by the scene
/// containing that zone controller. Surface controllers remain Surface-scoped; Underworld
/// controllers therefore evaluate biome, terrain, ZDO ownership and spawning against instance 1.
/// </summary>
[HarmonyPatch]
internal static class UnderworldSpawnSystemInstanceScopePatch
{
    private static readonly string[] Names =
    {
        "Awake", "Start", "Update", "UpdateSpawning", "UpdateSpawnList",
        "FindBaseSpawnPoint", "IsSpawnPointGood", "Spawn",
    };

    internal static System.Collections.Generic.IEnumerable<MethodBase> TargetMethods()
    {
        foreach (var method in typeof(SpawnSystem).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            if (Array.IndexOf(Names, method.Name) >= 0)
                yield return method;
    }

    internal static void Prefix(SpawnSystem __instance, out IDisposable? __state)
    {
        __state = null;
        if (!__instance) return;
        ValheimWorldInstanceExecution.TryEnterScene(__instance.gameObject.scene.handle, out __state);
    }

    internal static void Finalizer(IDisposable? __state) => __state?.Dispose();
}


/// <summary>
/// Routes player/character frame callbacks through the world services owned by the Unity scene
/// containing that character. This is the per-player half of simultaneous multiplayer residency.
/// </summary>
[HarmonyPatch]
internal static class UnderworldCharacterInstanceScopePatch
{
    private static readonly string[] Names = { "Update", "FixedUpdate", "LateUpdate" };

    internal static System.Collections.Generic.IEnumerable<MethodBase> TargetMethods()
    {
        foreach (var type in new[] { typeof(Character), typeof(Player) })
        foreach (var name in Names)
        {
            var method = AccessTools.DeclaredMethod(type, name);
            if (method is not null) yield return method;
        }
    }

    internal static void Prefix(Character __instance, out IDisposable? __state)
    {
        __state = null;
        if (!__instance) return;
        ValheimWorldInstanceExecution.TryEnterScene(__instance.gameObject.scene.handle, out __state);
    }

    internal static void Finalizer(IDisposable? __state) => __state?.Dispose();
}
