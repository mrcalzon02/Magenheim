using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Retires unsuitable saved boss sites without touching other locations or generated zones.</summary>
internal static class UnderworldArrivalBossMigration
{
    internal sealed record RetiredSite(UnderworldBossLocationRegistrar.BossLocation Boss, Vector3 Position);

    internal static bool IsRetired(ZoneSystem zone, string prefab, Vector3 point)
    {
        if (!UnderworldArenaGrounding.IsOutsideArrival(point.x, point.z)) return true;
        if (ZNet.instance is null || !ZNet.instance.IsServer() || !zone.LocationsGenerated) return false;
        return !zone.m_locationInstances.TryGetValue(ZoneSystem.GetZone(point), out var site) ||
            site.m_location is null || UnderworldZoneCatalogGuard.LocationName(site.m_location) != prefab ||
            Utils.DistanceXZ(point, site.m_position) > 2f;
    }

    internal static IReadOnlyList<RetiredSite> RetireInvalidSites(ZoneSystem zone, ManualLogSource? log)
    {
        var retired = new List<RetiredSite>();
        foreach (var pair in zone.m_locationInstances.ToArray())
        {
            var row = pair.Value.m_location;
            var point = pair.Value.m_position;
            if (row is null || UnderworldBossSite.Find(row) is not { } boss ||
                UnderworldBossSite.IsSuitable(zone, row, point, boss, pair.Key))
                continue;
            zone.m_locationInstances.Remove(pair.Key);
            retired.Add(new RetiredSite(boss, point));
            log?.LogInfo($"Retired unsuitable saved boss site '{boss.PrefabName}' at ({point.x:F1}, {point.z:F1}).");
        }

        return retired;
    }

    internal static IEnumerator RelocateEncounters(ValheimWorldInstanceContext context,
        IReadOnlyList<RetiredSite> retired)
    {
        var proxies = new List<ZDO>();
        var proxyCursor = 0;
        var proxyPrefab = context.ZoneSystem.m_locationProxyPrefab.name;
        while (!context.ZdoMan.GetAllZDOsWithPrefabIterative(proxyPrefab, proxies, ref proxyCursor)) yield return null;

        var locations = context.ZoneSystem.m_locationInstances.Values.ToArray();
        foreach (var group in retired.GroupBy(site => site.Boss.PrefabName))
        {
            var boss = group.First().Boss;
            var replacement = Array.Find(locations, location => location.m_location is not null &&
                UnderworldZoneCatalogGuard.LocationName(location.m_location) == boss.PrefabName);
            if (replacement.m_location is null) continue;

            var oldPositions = group.Select(site => site.Position).ToArray();
            var locationHash = boss.PrefabName.GetStableHashCode();
            using (ValheimWorldInstanceExecution.Enter(context))
                foreach (var proxy in proxies)
                {
                    if (proxy.GetInt(ZDOVars.s_location) != locationHash ||
                        !oldPositions.Any(old => Utils.DistanceXZ(old, proxy.GetPosition()) <= 2f)) continue;
                    proxy.SetOwner(ZDOMan.GetSessionID());
                    context.ZdoMan.DestroyZDO(proxy);
                }

            var encounterPrefab = boss.PrefabName.Replace("Magenheim_Underworld_Boss_", "Magenheim_Underworld_Encounter_");
            var existing = new List<ZDO>();
            var cursor = 0;
            while (!context.ZdoMan.GetAllZDOsWithPrefabIterative(encounterPrefab, existing, ref cursor)) yield return null;
            if (existing.Count != 1 || !oldPositions.Any(old => Utils.DistanceXZ(old, existing[0].GetPosition()) <= 128f))
                continue;

            var point = replacement.m_position;
            point.y = (float)UnderworldBossSite.GeneratedHeight(point.x, point.z);
            var waterLevel = context.ZoneSystem.m_waterLevel;
            if (!UnderworldBossSite.TryGetOrigin(boss.Family.Biome, point, waterLevel,
                    UnderworldBossSite.GeneratedHeight, out var origin))
                continue;
            point.y = boss.Family.Biome == UnderworldTerrainBiome.BlackwaterDeep
                ? waterLevel - UnderworldArenaGrounding.BossSwimmingDepth : (float)origin + 1.5f;

            using (ValheimWorldInstanceExecution.Enter(context))
            {
                var zdo = existing[0];
                zdo.SetPosition(point);
                var view = ZNetScene.instance.FindInstance(zdo.m_uid);
                if (view) view.transform.position = point;
            }
        }
    }
}
