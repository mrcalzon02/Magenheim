using HarmonyLib;

namespace Magenheim.Runtime;

/// <summary>Attaches exactly one owner-aware thermal lifecycle component to each Player.</summary>
[HarmonyPatch(typeof(Player), nameof(Player.Awake))]
internal static class UnderworldThermalLifecyclePatch
{
    [HarmonyPostfix]
    private static void Postfix(Player __instance)
    {
        if (__instance == null || __instance.GetComponent<UnderworldThermalLifecycleRuntime>() != null) return;
        __instance.gameObject.AddComponent<UnderworldThermalLifecycleRuntime>();
    }
}
