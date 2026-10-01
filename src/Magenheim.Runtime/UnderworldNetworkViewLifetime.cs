using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Unity destroys local scene objects without removing their native ZNetScene index entries.
/// Valheim 1.0's ZNetView.OnDestroy only releases soft references. RemoveObjects assumes every
/// indexed view still has a ZDO; repair that index before native streaming dereferences it.
/// Persisted ZDOs remain owned by their manager and are never deleted by this cleanup.
/// </summary>
[HarmonyPatch(typeof(ZNetScene), "RemoveObjects")]
internal static class UnderworldNetworkViewLifetimePatch
{
    private static void Prefix(Dictionary<ZDO, ZNetView> ___m_instances)
    {
        List<ZDO>? stale = null;
        foreach (var pair in ___m_instances)
        {
            if (pair.Value && pair.Value.GetZDO() is not null) continue;
            (stale ??= new List<ZDO>()).Add(pair.Key);
        }
        if (stale is null) return;
        foreach (var zdo in stale) ___m_instances.Remove(zdo);
        Debug.LogWarning($"Magenheim repaired {stale.Count} stale native network-view index entries; persisted ZDOs retained.");
    }
}
