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
        var time = ZNet.instance is null ? 0d : ZNet.instance.GetTimeSeconds();
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
        ZDOMan.instance?.GetZDO(peer.m_characterID)?.GetPosition() ?? peer.m_refPos;

    internal static Vector3 PeerPosition(object peer) => PeerPosition(Traverse.Create(peer).Field<ZNetPeer>("m_peer").Value);

    internal static void RefreshAlwaysLoadedPrefabs()
    {
        if (ZNetScene.instance is null) return;

        AlwaysLoad.Clear();
        foreach (var prefab in ZNetScene.instance.m_prefabs)
            if (prefab && prefab.GetComponentInChildren<Teleport>())
                AlwaysLoad.Add(prefab.name.GetStableHashCode());
        AlwaysLoad.Add("LocationProxy".GetStableHashCode());
        AlwaysLoad.Add("_ZoneCtrl".GetStableHashCode());
        AlwaysLoad.Add("_TerrainCompiler".GetStableHashCode());

        AlwaysSend.Clear();
        foreach (var prefab in ZNetScene.instance.m_prefabs)
            if (prefab && prefab.GetComponent<DungeonGenerator>())
                AlwaysSend.Add(prefab.name.GetStableHashCode());
        AlwaysSend.Add("Player".GetStableHashCode());
    }

    internal static bool Accept(ZDO zdo) =>
        AlwaysLoad.Contains(zdo.GetPrefab()) ||
        IsSameLayer(zdo.GetPosition()) ||
        (IsSending && AlwaysSend.Contains(zdo.GetPrefab()));
}

[HarmonyPatch(typeof(ZDOMan), "CreateSyncList")]
internal static class UnderworldLayerCreateSyncListPatch
{
    private static void Prefix(object __0)
    {
        UnderworldLayerIsolation.Check(UnderworldLayerIsolation.PeerPosition(__0));
        UnderworldLayerIsolation.IsSending = true;
    }
}


// Keep the installed game's simulation-distance, portal-sector and ownership rules intact.
[HarmonyPatch(typeof(ZDOMan), "ReleaseNearbyZDOS")]
internal static class UnderworldLayerReleaseNearbyZdosPatch
{
    private static void Prefix(Vector3 refPosition)
    {
        UnderworldLayerIsolation.Check(refPosition);
        UnderworldLayerIsolation.IsSending = false;
    }
}

[HarmonyPatch(typeof(ZDOMan), "IsInPeerActiveArea")]
internal static class UnderworldLayerPeerActiveAreaPatch
{
    private static void Postfix(Vector3 point, long uid, ref bool __result)
    {
        if (!__result || ZNet.instance is null) return;
        var peer = ZNet.instance.GetPeers().FirstOrDefault(value => value.m_uid == uid);
        if (peer is null) return;
        __result = UnderworldInstanceLayer.IsUnderworldEnginePosition(point)
            == UnderworldInstanceLayer.IsUnderworldEnginePosition(UnderworldLayerIsolation.PeerPosition(peer));
    }
}

[HarmonyPatch(typeof(ZNetScene), "CreateDestroyObjects")]
internal static class UnderworldLayerCreateDestroyObjectsPatch
{
    private static double _lastPeerCheck;
    private static bool _nearPeer;

    private static void Prefix()
    {
        if (!Player.m_localPlayer || ZNet.instance is null) return;
        var time = ZNet.instance.GetTimeSeconds();
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

[HarmonyPatch(typeof(ZNetScene), "IsAreaReady")]
internal static class UnderworldLayerIsAreaReadyPatch
{
    private static void Prefix(Vector3 point)
    {
        UnderworldLayerIsolation.Check(point);
        UnderworldLayerIsolation.IsSending = false;
    }
}

// Filter only the objects appended by this sector query. Native visited-sector and portal
// handling must still run, including other mods' additions and the engine's distant selection.
[HarmonyPatch(typeof(ZDOMan), "FindObjects")]
internal static class UnderworldLayerFindObjectsPatch
{
    private static void Prefix(List<ZDO> objects, out int __state) => __state = objects.Count;
    private static void Postfix(List<ZDO> objects, int __state)
    {
        for (var i = objects.Count - 1; i >= __state; i--)
            if (!UnderworldLayerIsolation.Accept(objects[i])) objects.RemoveAt(i);
    }
}

[HarmonyPatch(typeof(ZDOMan), "FindDistantObjects")]
internal static class UnderworldLayerFindDistantObjectsPatch
{
    private static void Prefix(List<ZDO> objects, out int __state) => __state = objects.Count;
    private static void Postfix(List<ZDO> objects, int __state)
    {
        for (var i = objects.Count - 1; i >= __state; i--)
            if (!UnderworldLayerIsolation.IsSameLayer(objects[i].GetPosition())) objects.RemoveAt(i);
    }
}

[HarmonyPatch(typeof(ZoneSystem), "Start")]
internal static class UnderworldLayerPrefabCachePatch
{
    private static void Postfix() => UnderworldLayerIsolation.RefreshAlwaysLoadedPrefabs();
}
