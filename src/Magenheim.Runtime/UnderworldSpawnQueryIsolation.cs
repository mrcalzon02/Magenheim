using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Scene-filters Valheim's process-global spawn query registries while a native SpawnSystem is
/// explicitly executing for one world instance. The underlying static registries are never
/// mutated; callers receive filtered copies only for the duration of that scoped spawn operation.
/// </summary>
internal static class UnderworldSpawnQueryIsolation
{
    internal static bool TryGetScene(out int sceneHandle)
    {
        var context = ValheimWorldInstanceExecution.Active;
        if (context is null)
        {
            sceneHandle = default;
            return false;
        }

        sceneHandle = context.Scene.handle;
        return true;
    }

    internal static bool InScene(Component component, int sceneHandle) =>
        component && component.gameObject.scene.handle == sceneHandle;
}

[HarmonyPatch(typeof(Player), nameof(Player.GetAllPlayers), new System.Type[0])]
internal static class UnderworldSpawnPlayerListPatch
{
    private static void Postfix(ref List<Player> __result)
    {
        if (!UnderworldSpawnQueryIsolation.TryGetScene(out var sceneHandle) || __result is null) return;
        var filtered = new List<Player>(__result.Count);
        foreach (var player in __result)
            if (UnderworldSpawnQueryIsolation.InScene(player, sceneHandle))
                filtered.Add(player);
        __result = filtered;
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.IsPlayerInRange), new[] { typeof(Vector3), typeof(float) })]
internal static class UnderworldSpawnPlayerRangePatch
{
    private static bool Prefix(Vector3 __0, float __1, ref bool __result)
    {
        if (!UnderworldSpawnQueryIsolation.TryGetScene(out var sceneHandle)) return true;

        foreach (var player in Player.GetAllPlayers())
        {
            if (!UnderworldSpawnQueryIsolation.InScene(player, sceneHandle)) continue;
            if (Vector3.Distance(player.transform.position, __0) >= __1) continue;
            __result = true;
            return false;
        }

        __result = false;
        return false;
    }
}

[HarmonyPatch]
internal static class UnderworldSpawnBaseAiListPatch
{
    internal static MethodBase TargetMethod() =>
        AccessTools.PropertyGetter(typeof(BaseAI), nameof(BaseAI.BaseAIInstances))
        ?? throw new MissingMethodException(typeof(BaseAI).FullName, "get_BaseAIInstances");

    private static void Postfix(ref List<BaseAI> __result)
    {
        if (!UnderworldSpawnQueryIsolation.TryGetScene(out var sceneHandle) || __result is null) return;
        var filtered = new List<BaseAI>(__result.Count);
        foreach (var ai in __result)
            if (UnderworldSpawnQueryIsolation.InScene(ai, sceneHandle))
                filtered.Add(ai);
        __result = filtered;
    }
}

[HarmonyPatch(typeof(Character), nameof(Character.GetAllCharacters), new System.Type[0])]
internal static class UnderworldSpawnCharacterListPatch
{
    private static void Postfix(ref List<Character> __result)
    {
        if (!UnderworldSpawnQueryIsolation.TryGetScene(out var sceneHandle) || __result is null) return;
        var filtered = new List<Character>(__result.Count);
        foreach (var character in __result)
            if (UnderworldSpawnQueryIsolation.InScene(character, sceneHandle))
                filtered.Add(character);
        __result = filtered;
    }
}
