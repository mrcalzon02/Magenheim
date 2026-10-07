using HarmonyLib;

namespace Magenheim.Runtime;

// Observe the native completion edge for live diagnostics; never alter teleport state or
// force-clear Hud's death/sleep/portal fade. Valheim remains the transition authority.
[HarmonyPatch(typeof(Player), "UpdateTeleport")]
internal static class UnderworldNativePortalArrivalLog
{
    private static void Prefix(Player __instance, out bool __state) =>
        __state = __instance.IsTeleporting();

    private static void Postfix(Player __instance, bool __state)
    {
        if (__state && !__instance.IsTeleporting())
            UnderworldGateTransitRuntime.ReportNativeArrival(__instance);
    }
}
