using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Move only obsolete owned boss sites out of arrival; preserve ordinary saved zones.</summary>
internal static class UnderworldArrivalBossMigration
{
    private static readonly HashSet<int> Checked = new();
    private static readonly MethodInfo Generate = AccessTools.Method(typeof(ZoneSystem),
        "GenerateLocationsTimeSliced", new[] { typeof(ZoneSystem.ZoneLocation), typeof(Stopwatch), typeof(ZPackage) })
        ?? throw new MissingMethodException(typeof(ZoneSystem).FullName, "GenerateLocationsTimeSliced");

    internal static bool AtArrival(Vector3 point) => point.x * point.x + point.z * point.z < 512f * 512f;

    internal static void Apply(ZoneSystem zone)
    {
        if (!zone || ZNet.instance is null || !ZNet.instance.IsServer() || !zone.LocationsGenerated ||
            !ValheimWorldInstanceExecution.TryGetContextForScene(zone.gameObject.scene.handle, out var context) ||
            context is null || !context.InstanceId.IsUnderworld || !Checked.Add(zone.GetInstanceID())) return;
        foreach (var boss in UnderworldBossLocationRegistrar.Locations)
        {
            var remove = new List<Vector2s>();
            foreach (var entry in zone.m_locationInstances)
                if (entry.Value.m_location is not null &&
                    UnderworldZoneCatalogGuard.LocationName(entry.Value.m_location) == boss.PrefabName &&
                    AtArrival(entry.Value.m_position)) remove.Add(entry.Key);
            if (remove.Count == 0) continue;
            foreach (var key in remove) zone.m_locationInstances.Remove(key);
            foreach (var row in zone.m_locations)
                if (row is not null && UnderworldZoneCatalogGuard.LocationName(row) == boss.PrefabName)
                {
                    zone.StartCoroutine(Regenerate(zone, row, boss));
                    break;
                }
            UnityEngine.Debug.Log("Magenheim relocated obsolete arrival boss site: " + boss.PrefabName);
        }
    }

    private static IEnumerator Regenerate(ZoneSystem zone, ZoneSystem.ZoneLocation row,
        UnderworldBossLocationRegistrar.BossLocation boss)
    {
        if (!ValheimWorldInstanceExecution.TryGetContextForScene(zone.gameObject.scene.handle, out var context) ||
            context is null) yield break;
        var native = Generate.Invoke(zone, new object[] { row, Stopwatch.StartNew(), new ZPackage() }) as IEnumerator
            ?? throw new InvalidOperationException("Native boss location generator did not return an iterator.");
        while (true)
        {
            bool more;
            using (ValheimWorldInstanceExecution.Enter(context)) more = native.MoveNext();
            if (!more) break;
            yield return native.Current;
        }
        Vector3? destination = null;
        foreach (var entry in zone.m_locationInstances.Values)
            if (entry.m_location is not null &&
                UnderworldZoneCatalogGuard.LocationName(entry.m_location) == boss.PrefabName && !AtArrival(entry.m_position))
                destination = entry.m_position + Vector3.up * 1.5f;
        if (destination is not { } point) yield break;
        var encounterPrefab = boss.PrefabName.Replace("Magenheim_Underworld_Boss_", "Magenheim_Underworld_Encounter_");
        var existing = new List<ZDO>();
        var cursor = 0;
        while (!context.ZdoMan.GetAllZDOsWithPrefabIterative(encounterPrefab, existing, ref cursor)) yield return null;
        using (ValheimWorldInstanceExecution.Enter(context))
            foreach (var zdo in existing)
            {
                if (!AtArrival(zdo.GetPosition())) continue;
                zdo.SetPosition(point);
                var view = ZNetScene.instance.FindInstance(zdo.m_uid);
                if (view) view.transform.position = point;
            }
    }
}

[HarmonyPatch(typeof(ZoneSystem), "GenerateLocationsIfNeeded")]
internal static class UnderworldArrivalBossMigrationPatch
{
    private static void Postfix(ZoneSystem __instance) => UnderworldArrivalBossMigration.Apply(__instance);
}
