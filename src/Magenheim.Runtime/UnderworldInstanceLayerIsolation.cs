using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Dungeon-Splitter-style layer isolation for the Magenheim-owned Underworld engine layer.
///
/// Valheim sectors are X/Z only. Without a second discriminator, objects occupying the same X/Z
/// sectors in the Surface and Underworld are loaded and synchronized together. The Underworld's
/// indexed world-space uses engine Y only as that discriminator at this adapter boundary. Core
/// world coordinates remain native instance coordinates.
/// </summary>
internal static class UnderworldLayerIsolation
{
    internal static readonly HashSet<int> AlwaysLoad = new();
    internal static readonly HashSet<int> AlwaysSend = new();
    internal static bool IsSending { get; set; }

    internal static bool InUnderworld { get; private set; }
    internal static bool OnSurface { get; private set; } = true;
    private static double _lastUnderworld;
    private static double _lastSurface;
    private const double TransitionGraceSeconds = 2.5d;

    internal static bool IsSameLayer(Vector3 position)
    {
        var underworld = UnderworldInstanceLayer.IsUnderworldEnginePosition(position);
        return underworld ? InUnderworld : OnSurface;
    }

    internal static void Check(Vector3 position)
    {
        InUnderworld = UnderworldInstanceLayer.IsUnderworldEnginePosition(position);
        OnSurface = !InUnderworld;
    }

    internal static void CheckForRemove(Vector3 position)
    {
        var underworld = UnderworldInstanceLayer.IsUnderworldEnginePosition(position);
        var time = ZNet.instance is null ? 0d : ZNet.instance.m_netTime;
        if (underworld)
        {
            InUnderworld = true;
            _lastUnderworld = time;
            OnSurface = time - _lastSurface <= TransitionGraceSeconds;
        }
        else
        {
            OnSurface = true;
            _lastSurface = time;
            InUnderworld = time - _lastUnderworld <= TransitionGraceSeconds;
        }
    }

    internal static Vector3 PeerPosition(ZNetPeer peer) =>
        ZDOMan.instance?.GetZDO(peer.m_characterID)?.m_position ?? peer.m_refPos;

    internal static Vector3 PeerPosition(ZDOMan.ZDOPeer peer) => PeerPosition(peer.m_peer);

    internal static void RefreshAlwaysLoadedPrefabs()
    {
        if (ZNetScene.instance is null) return;

        AlwaysLoad.Clear();
        foreach (var prefab in ZNetScene.instance.m_namedPrefabs.Values)
            if (prefab && prefab.GetComponentInChildren<Teleport>())
                AlwaysLoad.Add(prefab.name.GetStableHashCode());
        AlwaysLoad.Add("LocationProxy".GetStableHashCode());
        AlwaysLoad.Add("_ZoneCtrl".GetStableHashCode());
        AlwaysLoad.Add("_TerrainCompiler".GetStableHashCode());

        AlwaysSend.Clear();
        foreach (var prefab in ZNetScene.instance.m_namedPrefabs.Values)
            if (prefab && prefab.GetComponent<DungeonGenerator>())
                AlwaysSend.Add(prefab.name.GetStableHashCode());
        AlwaysSend.Add("Player".GetStableHashCode());
    }

    internal static bool Accept(ZDO zdo) =>
        AlwaysLoad.Contains(zdo.m_prefab) ||
        IsSameLayer(zdo.m_position) ||
        (IsSending && AlwaysSend.Contains(zdo.m_prefab));
}

[HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.CreateSyncList))]
internal static class UnderworldLayerCreateSyncListPatch
{
    private static void Prefix(ZDOMan.ZDOPeer peer)
    {
        UnderworldLayerIsolation.Check(UnderworldLayerIsolation.PeerPosition(peer));
        UnderworldLayerIsolation.IsSending = true;
    }
}

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.CreateDestroyObjects))]
internal static class UnderworldLayerCreateDestroyObjectsPatch
{
    private static double _lastPeerCheck;
    private static bool _nearPeer;

    private static void Prefix()
    {
        if (!Player.m_localPlayer || ZNet.instance is null) return;
        var time = ZNet.instance.m_netTime;
        var position = ZNet.instance.GetReferencePosition();
        if (time - _lastPeerCheck > 5d)
        {
            _lastPeerCheck = time;
            _nearPeer = ZNet.instance.GetPeers().Any(peer =>
                peer.IsReady() &&
                Utils.DistanceXZ(UnderworldLayerIsolation.PeerPosition(peer), position) < 300f);
        }

        if (_nearPeer) UnderworldLayerIsolation.CheckForRemove(position);
        else UnderworldLayerIsolation.Check(position);
        UnderworldLayerIsolation.IsSending = false;
    }
}

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.IsAreaReady))]
internal static class UnderworldLayerIsAreaReadyPatch
{
    private static void Prefix(Vector3 point)
    {
        UnderworldLayerIsolation.Check(point);
        UnderworldLayerIsolation.IsSending = false;
    }
}

[HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.FindObjects))]
internal static class UnderworldLayerFindObjectsPatch
{
    private static bool Prefix(ZDOMan __instance, Vector2i sector, List<ZDO> objects)
    {
        var index = __instance.SectorToIndex(sector);
        if (index >= 0)
        {
            if (__instance.m_objectsBySector[index] is { } local)
                objects.AddRange(local.Where(UnderworldLayerIsolation.Accept));
            return false;
        }

        if (__instance.m_objectsByOutsideSector.TryGetValue(sector, out var outside))
            objects.AddRange(outside.Where(UnderworldLayerIsolation.Accept));
        return false;
    }
}

[HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.FindDistantObjects))]
internal static class UnderworldLayerFindDistantObjectsPatch
{
    private static bool Prefix(ZDOMan __instance, Vector2i sector, List<ZDO> objects)
    {
        var index = __instance.SectorToIndex(sector);
        if (index >= 0)
        {
            var local = __instance.m_objectsBySector[index];
            if (local is null) return false;
            objects.AddRange(local.Where(zdo =>
                zdo.Distant && UnderworldLayerIsolation.IsSameLayer(zdo.m_position)));
            return false;
        }

        if (__instance.m_objectsByOutsideSector.TryGetValue(sector, out var outside))
            objects.AddRange(outside.Where(zdo =>
                zdo.Distant && UnderworldLayerIsolation.IsSameLayer(zdo.m_position)));
        return false;
    }
}

[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start))]
internal static class UnderworldLayerPrefabCachePatch
{
    private static void Postfix() => UnderworldLayerIsolation.RefreshAlwaysLoadedPrefabs();
}
