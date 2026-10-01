using HarmonyLib;

namespace Magenheim.Runtime;

/// <summary>Keep native gate controls/effects; replace only an owned gate's destination.</summary>
[HarmonyPatch(typeof(Teleport), nameof(Teleport.Interact))]
internal static class UnderworldNativeGateTransportPatch
{
    private static bool Prefix(Teleport __instance, Humanoid __0, bool __1, bool __2, ref bool __result)
    {
        var endpoint = __instance.GetComponentInParent<UnderworldGateEndpoint>();
        if (!endpoint) return true;
        __result = endpoint.Interact(__0, __1, __2);
        return false;
    }
}
