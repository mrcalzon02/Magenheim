using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Magenheim.Runtime;

/// <summary>The native local tutorial Raven follows the local player's instance.</summary>
[HarmonyPatch(typeof(Raven), "Spawn")]
internal static class UnderworldRavenSceneRouting
{
    private static void Prefix(Raven __instance)
    {
        var player = Player.m_localPlayer;
        if (!__instance || !player || __instance.gameObject.scene.handle == player.gameObject.scene.handle) return;
        SceneManager.MoveGameObjectToScene(__instance.transform.root.gameObject, player.gameObject.scene);
        foreach (var isolation in Object.FindObjectsByType<UnderworldScenePresentationIsolation>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            isolation.RestoreTransferredObjects();
    }
}
