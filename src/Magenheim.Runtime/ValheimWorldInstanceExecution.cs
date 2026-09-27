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
    private static Harmony? _sceneCallbacks;

    // Menu/Surface-only sessions need no generic behaviour interception. Install these broad
    // adapters only once the second scene exists, before its components are activated.
    internal static void EnableSceneCallbacks()
    {
        if (_sceneCallbacks is not null) return;
        var harmony = new Harmony(MagenheimPlugin.PluginGuid + ".scene-callbacks");
        try
        {
            harmony.PatchAll(typeof(UnderworldValheimBehaviourInstanceScopePatch));
            harmony.PatchAll(typeof(UnderworldMagenheimBehaviourInstanceScopePatch));
            _sceneCallbacks = harmony;
        }
        catch { harmony.UnpatchSelf(); throw; }
    }

    internal static void DisableSceneCallbacks()
    {
        _sceneCallbacks?.UnpatchSelf();
        _sceneCallbacks = null;
    }

    // Bind the writable native backing fields explicitly. The public singleton properties can
    // be read-only; a guessed property/field fallback must not poison every patched callback.
    private static readonly FieldInfo ZoneInstance =
        typeof(ZoneSystem).GetField("s_instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(typeof(ZoneSystem).FullName, "s_instance");
    private static readonly FieldInfo GeneratorInstance =
        typeof(WorldGenerator).GetField("m_instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(typeof(WorldGenerator).FullName, "m_instance");
    private static readonly FieldInfo ZdoInstance =
        typeof(ZDOMan).GetField("s_instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(typeof(ZDOMan).FullName, "s_instance");
    private static readonly FieldInfo ZNetWorld =
        typeof(ZNet).GetField("m_world", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(typeof(ZNet).FullName, "m_world");
    private static readonly FieldInfo ZNetZdoMan =
        typeof(ZNet).GetField("m_zdoMan", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(typeof(ZNet).FullName, "m_zdoMan");

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
        if (registry is null || !registry.HasUnderworldContext) return false;

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

    internal static bool TryGetContextForScene(int sceneHandle, out ValheimWorldInstanceContext? context)
    {
        var registry = _registry;
        if (registry is not null && registry.TryGetContextForScene(sceneHandle, out context) && context is not null)
            return true;
        context = null;
        return false;
    }

    internal static bool TryGetContext(UnderworldWorldInstanceId instanceId, out ValheimWorldInstanceContext? context)
    {
        var registry = _registry;
        if (registry is not null && registry.TryGetContext(instanceId, out context) && context is not null)
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
        private readonly object? _previousZdoInstance;
        private readonly object? _previousWorld;
        private readonly object? _previousZdoMan;
        private readonly Scene _previousActiveScene;
        private readonly bool _changedActiveScene;
        private readonly IDisposable? _pathfindingScope;
        private bool _disposed;

        internal Scope(ValheimWorldInstanceContext context)
        {
            _previousContext = Current.Value;
            _previousZone = ZoneInstance.GetValue(null);
            _previousGenerator = GeneratorInstance.GetValue(null);
            _previousZdoInstance = ZdoInstance.GetValue(null);
            _previousWorld = ZNetWorld.GetValue(null);
            _previousZdoMan = ZNet.instance is null ? null : ZNetZdoMan.GetValue(ZNet.instance);
            _previousActiveScene = SceneManager.GetActiveScene();
            _changedActiveScene =
                context.Scene.IsValid() &&
                context.Scene.isLoaded &&
                _previousActiveScene.handle != context.Scene.handle;

            if (_changedActiveScene && !SceneManager.SetActiveScene(context.Scene))
                throw new InvalidOperationException(
                    $"Unable to activate Unity scene '{context.Scene.name}' for world instance {context.InstanceId}.");

            Current.Value = context;
            ZoneInstance.SetValue(null, context.ZoneSystem);
            GeneratorInstance.SetValue(null, context.WorldGenerator);
            ZdoInstance.SetValue(null, context.ZdoMan);
            ZNetWorld.SetValue(null, context.World);
            if (ZNet.instance is not null)
                ZNetZdoMan.SetValue(ZNet.instance, context.ZdoMan);

            var pathfinding = Pathfinding.instance;
            if (context.PathfindingState is not null && pathfinding)
                _pathfindingScope = context.PathfindingState.Enter(pathfinding);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _pathfindingScope?.Dispose();
            if (ZNet.instance is not null) ZNetZdoMan.SetValue(ZNet.instance, _previousZdoMan);
            ZNetWorld.SetValue(null, _previousWorld);
            ZdoInstance.SetValue(null, _previousZdoInstance);
            GeneratorInstance.SetValue(null, _previousGenerator);
            ZoneInstance.SetValue(null, _previousZone);
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
            if (type.IsAbstract || type == typeof(Pathfinding) || !typeof(MonoBehaviour).IsAssignableFrom(type)) continue;
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
/// Applies the same world-instance scope to Magenheim-authored MonoBehaviour callbacks. This is
/// intentionally separate from the Valheim-assembly adapter: Underworld encounters, creature
/// runtimes, rewards and effects often Instantiate native/networked content from their callbacks,
/// and Unity must see the owning instance scene as active for those operations.
/// </summary>
[HarmonyPatch]
internal static class UnderworldMagenheimBehaviourInstanceScopePatch
{
    private static readonly string[] Names =
    {
        "Awake", "Start", "Update", "FixedUpdate", "LateUpdate",
        "OnTriggerEnter", "OnTriggerStay", "OnTriggerExit",
        "OnCollisionEnter", "OnCollisionStay", "OnCollisionExit",
    };

    internal static System.Collections.Generic.IEnumerable<MethodBase> TargetMethods()
    {
        var assembly = typeof(MagenheimPlugin).Assembly;
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
        "CreateLocalZones", "CreateGhostZones", "PokeLocalZone", "SpawnZone",
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
            var iterator = method.GetCustomAttribute<System.Runtime.CompilerServices.IteratorStateMachineAttribute>();
            var moveNext = iterator?.StateMachineType.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
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
/// Heightmap.Regenerate validates queued build data against WorldGenerator.instance and passes that
/// singleton to HeightmapBuilder.RequestTerrainSync. Scope the call by the Heightmap's scene so a
/// completed Underworld build is not discarded merely because Surface is the ambient singleton.
/// </summary>
[HarmonyPatch(typeof(Heightmap), "Regenerate")]
internal static class UnderworldHeightmapRegenerateInstanceScopePatch
{
    private static void Prefix(Heightmap __instance, out IDisposable? __state)
    {
        __state = null;
        if (!__instance) return;
        ValheimWorldInstanceExecution.TryEnterScene(__instance.gameObject.scene.handle, out __state);
    }

    private static void Finalizer(IDisposable? __state) => __state?.Dispose();
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
        if (!__instance ||
            !ValheimWorldInstanceExecution.TryGetContextForScene(__instance.gameObject.scene.handle, out var context) ||
            context is null ||
            ReferenceEquals(ValheimWorldInstanceExecution.Active, context))
            return;

        // SpawnSystem must carry explicit instance identity even for Surface. Its native code reads
        // process-global Player/BaseAI registries, so the query adapters below need to know which
        // scene's population the current zone controller is allowed to see.
        __state = ValheimWorldInstanceExecution.Enter(context);
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
            var method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
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
