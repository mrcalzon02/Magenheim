using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Limits each native ZoneSystem's server peer loop to the peers already owned by that instance's
/// ZDOMan. Deep Gate transit moves the native ZNetPeer between those managers, so this is a view
/// over Valheim's existing network authority rather than a second player/population registry.
/// </summary>
internal static class UnderworldZonePeerFilter
{
    private static readonly AsyncLocal<ZDOMan?> ActiveManager = new();
    private static readonly FieldInfo PeersField = AccessTools.Field(typeof(ZDOMan), "m_peers")
        ?? throw new MissingFieldException(typeof(ZDOMan).FullName, "m_peers");
    private static readonly Type ZdoPeerType = AccessTools.Inner(typeof(ZDOMan), "ZDOPeer")
        ?? throw new MissingMemberException(typeof(ZDOMan).FullName, "ZDOPeer");
    private static readonly FieldInfo NetworkPeerField = AccessTools.Field(ZdoPeerType, "m_peer")
        ?? throw new MissingFieldException(ZdoPeerType.FullName, "m_peer");

    internal static IDisposable Enter(ZDOMan manager)
    {
        if (manager is null) throw new ArgumentNullException(nameof(manager));
        return new Scope(manager);
    }

    internal static void Filter(ref List<ZNetPeer> peers)
    {
        var manager = ActiveManager.Value;
        if (manager is null || peers is null || peers.Count == 0) return;

        var native = PeersField.GetValue(manager) as IEnumerable
            ?? throw new InvalidOperationException("ZDOMan.m_peers is not enumerable.");
        var allowed = new HashSet<ZNetPeer>();
        foreach (var entry in native)
        {
            if (entry is null) continue;
            if (NetworkPeerField.GetValue(entry) is ZNetPeer peer && peer is not null)
                allowed.Add(peer);
        }

        if (allowed.Count == peers.Count && peers.TrueForAll(allowed.Contains)) return;
        peers = peers.FindAll(allowed.Contains);
    }

    private sealed class Scope : IDisposable
    {
        private readonly ZDOMan? _previous;
        private bool _disposed;

        internal Scope(ZDOMan manager)
        {
            _previous = ActiveManager.Value;
            ActiveManager.Value = manager;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            ActiveManager.Value = _previous;
        }
    }
}

[HarmonyPatch(typeof(ZoneSystem), "Update")]
internal static class UnderworldZonePeerFilterScopePatch
{
    [HarmonyPriority(Priority.First)]
    private static void Prefix(ZoneSystem __instance, out IDisposable? __state)
    {
        __state = null;
        if (!__instance ||
            !ValheimWorldInstanceExecution.TryGetContextForScene(__instance.gameObject.scene.handle, out var context) ||
            context is null)
            return;
        __state = UnderworldZonePeerFilter.Enter(context.ZdoMan);
    }

    [HarmonyPriority(Priority.Last)]
    private static void Finalizer(IDisposable? __state) => __state?.Dispose();
}

[HarmonyPatch(typeof(ZNet), nameof(ZNet.GetPeers), new Type[] { })]
internal static class UnderworldZonePeerListPatch
{
    private static void Postfix(ref List<ZNetPeer> __result) =>
        UnderworldZonePeerFilter.Filter(ref __result);
}

/// <summary>
/// CreateLocalZones uses ZNet's one process-wide reference position. Only the ZoneSystem that owns
/// the local player's Unity scene may consume that position. Dedicated servers have no local
/// player, so the Underworld skips the meaningless local-origin pass and relies on its filtered
/// native peer ghost-generation loop.
/// </summary>
[HarmonyPatch(typeof(ZoneSystem), "CreateLocalZones", new[] { typeof(Vector3) })]
internal static class UnderworldZoneLocalReferenceGuardPatch
{
    private static bool Prefix(ZoneSystem __instance, ref bool __result)
    {
        if (!__instance ||
            !ValheimWorldInstanceExecution.TryGetContextForScene(__instance.gameObject.scene.handle, out var zoneContext) ||
            zoneContext is null)
            return true;

        var local = Player.m_localPlayer;
        if (!local)
        {
            if (zoneContext.InstanceId.IsSurface) return true;
            __result = false;
            return false;
        }

        if (local.gameObject.scene.handle == zoneContext.Scene.handle) return true;
        __result = false;
        return false;
    }
}
