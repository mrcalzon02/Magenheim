using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// A second native ZoneSystem must run its ordinary location/vegetation initialization, but Valheim's
/// Start also installs process-global global-key/location-icon RPC handlers and subscribes
/// ZRoutedRpc.m_onNewPeer. Surface owns those campaign/network handlers once for the entire save.
/// </summary>
[HarmonyPatch(typeof(ZoneSystem), "Start")]
internal static class UnderworldZoneSystemStartIsolationPatch
{
    private static readonly MethodInfo SetupLocations =
        AccessTools.Method(typeof(ZoneSystem), "SetupLocations")
        ?? throw new MissingMethodException(typeof(ZoneSystem).FullName, "SetupLocations");
    private static readonly MethodInfo ValidateVegetation =
        AccessTools.Method(typeof(ZoneSystem), "ValidateVegetation")
        ?? throw new MissingMethodException(typeof(ZoneSystem).FullName, "ValidateVegetation");
    private static readonly FieldInfo StartTime =
        AccessTools.Field(typeof(ZoneSystem), "m_startTime")
        ?? throw new MissingFieldException(typeof(ZoneSystem).FullName, "m_startTime");
    private static readonly FieldInfo LastFixedTime =
        AccessTools.Field(typeof(ZoneSystem), "m_lastFixedTime")
        ?? throw new MissingFieldException(typeof(ZoneSystem).FullName, "m_lastFixedTime");

    private static bool Prefix(ZoneSystem __instance)
    {
        if (!__instance ||
            !ValheimWorldInstanceExecution.TryGetContextForScene(
                __instance.gameObject.scene.handle,
                out var context) ||
            context is null ||
            !context.InstanceId.IsUnderworld)
            return true;

        IDisposable? scope = null;
        try
        {
            if (!ReferenceEquals(ValheimWorldInstanceExecution.Active, context))
                scope = ValheimWorldInstanceExecution.Enter(context);

            // These are the native non-network portions of ZoneSystem.Start.
            __instance.UpdateWorldRates();
            SetupLocations.Invoke(__instance, Array.Empty<object>());
            UnderworldZoneInstanceState.NotifyZoneReady(__instance);
            ValidateVegetation.Invoke(__instance, Array.Empty<object>());
            var fixedTime = Time.fixedTime;
            StartTime.SetValue(__instance, fixedTime);
            LastFixedTime.SetValue(__instance, fixedTime);

            // Deliberately skip:
            //   * ZRoutedRpc.m_onNewPeer subscription
            //   * SetGlobalKey/RemoveGlobalKey registration on servers
            //   * GlobalKeys/LocationIcons registration on clients
            // Those are one-per-campaign handlers owned by the Surface ZoneSystem. The Underworld
            // shares the same three native global-key collections, so key reads are immediately
            // coherent without duplicate RPC authority.
            return false;
        }
        finally
        {
            scope?.Dispose();
        }
    }
}
