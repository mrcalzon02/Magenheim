using HarmonyLib;

namespace Magenheim.Runtime;

/// <summary>
/// Native teleport delays moving the transform for two seconds. During an
/// instance entry the old Surface position is not valid Underworld geography.
/// Resume ordinary discovery once Valheim completes its existing teleport.
/// </summary>
[HarmonyPatch(typeof(Player), "UpdateBiome")]
internal static class UnderworldTeleportBiomeDiscovery
{
    internal static bool Prefix(Player __instance) =>
        !__instance.IsTeleporting() ||
        !ValheimWorldInstanceExecution.TryGetContextForScene(__instance.gameObject.scene.handle, out var context) ||
        context is null || !context.InstanceId.IsUnderworld;
}

