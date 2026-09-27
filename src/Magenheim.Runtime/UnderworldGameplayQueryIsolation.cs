using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Filters Valheim's process-global gameplay registries by the ambient world-instance scene.
/// This is query isolation only: Magenheim does not maintain a duplicate player/character list.
/// Surface remains the ambient default; Underworld callbacks already enter their instance scope.
/// </summary>
internal static class UnderworldGameplayQueryIsolation
{
    internal static bool TryGetSceneHandle(out int sceneHandle)
    {
        if (ValheimWorldInstanceExecution.TryGetAmbientContext(out var context) &&
            context is not null &&
            context.Scene.IsValid())
        {
            sceneHandle = context.Scene.handle;
            return true;
        }

        sceneHandle = default;
        return false;
    }

    internal static List<Player> FilterPlayers(List<Player> source)
    {
        if (!TryGetSceneHandle(out var sceneHandle)) return source;
        var filtered = new List<Player>(source.Count);
        foreach (var player in source)
            if (player && player.gameObject.scene.handle == sceneHandle)
                filtered.Add(player);
        return filtered;
    }

    internal static List<Character> FilterCharacters(List<Character> source)
    {
        if (!TryGetSceneHandle(out var sceneHandle)) return source;
        var filtered = new List<Character>(source.Count);
        foreach (var character in source)
            if (character && character.gameObject.scene.handle == sceneHandle)
                filtered.Add(character);
        return filtered;
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.GetAllPlayers), System.Type.EmptyTypes)]
internal static class UnderworldGameplayPlayerListPatch
{
    [HarmonyPriority(Priority.Last)]
    private static void Postfix(ref List<Player> __result)
    {
        if (__result is null) return;
        __result = UnderworldGameplayQueryIsolation.FilterPlayers(__result);
    }
}

[HarmonyPatch(typeof(Character), nameof(Character.GetAllCharacters), System.Type.EmptyTypes)]
internal static class UnderworldGameplayCharacterListPatch
{
    [HarmonyPriority(Priority.Last)]
    private static void Postfix(ref List<Character> __result)
    {
        if (__result is null) return;
        __result = UnderworldGameplayQueryIsolation.FilterCharacters(__result);
    }
}
