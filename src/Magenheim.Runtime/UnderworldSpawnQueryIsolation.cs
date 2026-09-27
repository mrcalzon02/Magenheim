using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Gives Valheim's native SpawnSystem an instance-local view of process-global player and creature
/// registries. This is a scoped query adapter only: no player population list or custom spawn loop
/// is maintained by Magenheim.
/// </summary>
internal static class UnderworldSpawnQueryIsolation
{
    private static readonly AsyncLocal<int?> SceneHandle = new();

    internal static IDisposable Enter(SpawnSystem spawnSystem)
    {
        if (!spawnSystem) throw new ArgumentNullException(nameof(spawnSystem));
        return new Scope(spawnSystem.gameObject.scene.handle);
    }

    internal static bool TryGetSceneHandle(out int sceneHandle)
    {
        if (SceneHandle.Value is int value)
        {
            sceneHandle = value;
            return true;
        }
        sceneHandle = default;
        return false;
    }

    internal static bool IsInQueryScene(Component component) =>
        component && TryGetSceneHandle(out var sceneHandle) &&
        component.gameObject.scene.handle == sceneHandle;

    internal static List<Player> FilterPlayers(List<Player> source)
    {
        if (!TryGetSceneHandle(out var sceneHandle)) return source;

        var filtered = new List<Player>(source.Count);
        foreach (var player in source)
            if (player && player.gameObject.scene.handle == sceneHandle)
                filtered.Add(player);
        return filtered;
    }

    private sealed class Scope : IDisposable
    {
        private readonly int? _previous;
        private bool _disposed;

        internal Scope(int sceneHandle)
        {
            _previous = SceneHandle.Value;
            SceneHandle.Value = sceneHandle;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            SceneHandle.Value = _previous;
        }
    }
}

/// <summary>
/// Keep the scene discriminator live across every native SpawnSystem method that reads global player,
/// AI or tagged-object registries. The owning SpawnSystem already carries its native world-service
/// scope through UnderworldSpawnSystemInstanceScopePatch.
/// </summary>
[HarmonyPatch]
internal static class UnderworldSpawnQueryScopePatch
{
    private static readonly string[] Names =
    {
        "UpdateSpawning",
        "UpdateSpawnList",
        "GetPlayersInZone",
        "GetPlayersNearZone",
        "FindBaseSpawnPoint",
        "IsSpawnPointGood",
    };

    internal static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (var method in typeof(SpawnSystem).GetMethods(
                     BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            if (Array.IndexOf(Names, method.Name) >= 0)
                yield return method;
    }

    private static void Prefix(SpawnSystem __instance, out IDisposable? __state)
    {
        __state = __instance ? UnderworldSpawnQueryIsolation.Enter(__instance) : null;
    }

    private static void Finalizer(IDisposable? __state) => __state?.Dispose();
}

/// <summary>
/// SpawnSystem uses Player.GetAllPlayers both for zone candidates and pheromone/status-effect
/// modifiers. During a native spawn query expose only players in the SpawnSystem's Unity scene.
/// </summary>
[HarmonyPatch(typeof(Player), nameof(Player.GetAllPlayers), Type.EmptyTypes)]
internal static class UnderworldSpawnPlayerListPatch
{
    private static void Postfix(ref List<Player> __result)
    {
        if (__result is null || !UnderworldSpawnQueryIsolation.TryGetSceneHandle(out _)) return;
        __result = UnderworldSpawnQueryIsolation.FilterPlayers(__result);
    }
}

/// <summary>
/// Native spawn-point rejection uses this process-global proximity query. Re-evaluate against the
/// spawn query's scene so a Surface player at the same logical coordinates cannot block an
/// Underworld spawn, or vice versa.
/// </summary>
[HarmonyPatch(
    typeof(Player),
    nameof(Player.IsPlayerInRange),
    new[] { typeof(Vector3), typeof(float) })]
internal static class UnderworldSpawnPlayerRangePatch
{
    private static bool Prefix(Vector3 __0, float __1, ref bool __result)
    {
        if (!UnderworldSpawnQueryIsolation.TryGetSceneHandle(out var sceneHandle)) return true;

        foreach (var player in Player.s_players)
        {
            if (!player || player.gameObject.scene.handle != sceneHandle) continue;
            if (Vector3.Distance(player.transform.position, __0) >= __1) continue;
            __result = true;
            return false;
        }

        __result = false;
        return false;
    }
}

[HarmonyPatch(
    typeof(Player),
    nameof(Player.GetPlayersInRangeXZ),
    new[] { typeof(Vector3), typeof(float) })]
internal static class UnderworldSpawnPlayerRangeCountPatch
{
    private static bool Prefix(Vector3 __0, float __1, ref int __result)
    {
        if (!UnderworldSpawnQueryIsolation.TryGetSceneHandle(out var sceneHandle)) return true;

        var count = 0;
        foreach (var player in Player.s_players)
        {
            if (!player || player.gameObject.scene.handle != sceneHandle) continue;
            if (Utils.DistanceXZ(player.transform.position, __0) < __1) count++;
        }

        __result = count;
        return false;
    }
}

/// <summary>
/// SpawnSystem population caps normally count BaseAI/GameObjects across every loaded Unity scene.
/// Keep Valheim's exact cap semantics but restrict candidates to the active spawn-instance scene.
/// </summary>
[HarmonyPatch(
    typeof(SpawnSystem),
    nameof(SpawnSystem.GetNrOfInstances),
    new[]
    {
        typeof(GameObject),
        typeof(Vector3),
        typeof(float),
        typeof(bool),
        typeof(bool),
    })]
internal static class UnderworldSpawnInstanceCountPatch
{
    private static bool Prefix(
        GameObject __0,
        Vector3 __1,
        float __2,
        bool __3,
        bool __4,
        ref int __result)
    {
        if (!UnderworldSpawnQueryIsolation.TryGetSceneHandle(out var sceneHandle)) return true;

        var prefab = __0;
        var center = __1;
        var maxRange = __2;
        var eventCreaturesOnly = __3;
        var procreationOnly = __4;
        var cloneName = prefab.name + "(Clone)";

        if (prefab.GetComponent<BaseAI>() is not null)
        {
            var count = 0;
            foreach (var ai in BaseAI.BaseAIInstances)
            {
                if (!ai || ai.gameObject.scene.handle != sceneHandle || ai.gameObject.name != cloneName) continue;
                if (maxRange > 0f && Vector3.Distance(center, ai.transform.position) > maxRange) continue;

                if (eventCreaturesOnly)
                {
                    var monster = ai as MonsterAI;
                    if (monster && !monster.IsEventCreature()) continue;
                }

                if (procreationOnly)
                {
                    var procreation = ai.GetComponent<Procreation>();
                    if (procreation && !procreation.ReadyForProcreation()) continue;
                }

                count++;
            }

            __result = count;
            return false;
        }

        var objectCount = 0;
        foreach (var spawned in GameObject.FindGameObjectsWithTag("spawned"))
        {
            if (!spawned || spawned.scene.handle != sceneHandle || !spawned.name.CustomStartsWith(cloneName)) continue;
            if (maxRange > 0f && Vector3.Distance(center, spawned.transform.position) > maxRange) continue;
            objectCount++;
        }

        __result = objectCount;
        return false;
    }
}

/// <summary>
/// The private duplicate-prevention query uses the same global AI/tag registries as population caps.
/// Resolve it dynamically because it is private, while the build-time dynamic-target verifier pins
/// the actual installed Valheim method.
/// </summary>
[HarmonyPatch]
internal static class UnderworldSpawnHaveInstanceInRangePatch
{
    internal static MethodBase TargetMethod() =>
        AccessTools.Method(
            typeof(SpawnSystem),
            "HaveInstanceInRange",
            new[] { typeof(GameObject), typeof(Vector3), typeof(float) })
        ?? throw new MissingMethodException(
            typeof(SpawnSystem).FullName,
            "HaveInstanceInRange(GameObject,Vector3,float)");

    private static bool Prefix(GameObject __0, Vector3 __1, float __2, ref bool __result)
    {
        if (!UnderworldSpawnQueryIsolation.TryGetSceneHandle(out var sceneHandle)) return true;

        var prefab = __0;
        var center = __1;
        var minDistance = __2;
        var prefabName = prefab.name;

        if (prefab.GetComponent<BaseAI>() is not null)
        {
            foreach (var ai in BaseAI.BaseAIInstances)
            {
                if (!ai || ai.gameObject.scene.handle != sceneHandle) continue;
                if (!ai.gameObject.name.CustomStartsWith(prefabName)) continue;
                if (Utils.DistanceXZ(ai.transform.position, center) >= minDistance) continue;
                __result = true;
                return false;
            }

            __result = false;
            return false;
        }

        foreach (var spawned in GameObject.FindGameObjectsWithTag("spawned"))
        {
            if (!spawned || spawned.scene.handle != sceneHandle) continue;
            if (!spawned.name.CustomStartsWith(prefabName)) continue;
            if (Utils.DistanceXZ(spawned.transform.position, center) >= minDistance) continue;
            __result = true;
            return false;
        }

        __result = false;
        return false;
    }
}
