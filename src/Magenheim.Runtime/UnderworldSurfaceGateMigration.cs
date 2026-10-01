using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Magenheim.Runtime;

internal static class UnderworldSurfaceGateMigration
{
    private static readonly HashSet<int> CheckedZones = new();
    private static readonly FieldInfo Instances = AccessTools.Field(typeof(ZoneSystem), "m_locationInstances")
        ?? throw new MissingFieldException(typeof(ZoneSystem).FullName, "m_locationInstances");
    private static readonly MethodInfo Generate = AccessTools.Method(typeof(ZoneSystem), "GenerateLocationsTimeSliced",
        new[] { typeof(ZoneSystem.ZoneLocation), typeof(Stopwatch), typeof(ZPackage) })
        ?? throw new MissingMethodException(typeof(ZoneSystem).FullName, "GenerateLocationsTimeSliced");

    internal static bool IsAllowed(Vector3 point)
    {
        if (point.x * point.x + point.z * point.z < 2500f * 2500f) return false;
        var generator = WorldGenerator.instance;
        return generator is not null &&
            (generator.GetBiome(point) & (Heightmap.Biome.Mountain | Heightmap.Biome.DeepNorth)) != 0;
    }

    internal static void Apply(ZoneSystem zone)
    {
        if (!zone || ZNet.instance is null || !ZNet.instance.IsServer() || !zone.LocationsGenerated ||
            zone.gameObject.scene.name.StartsWith("Magenheim_Underworld_", StringComparison.Ordinal) ||
            !CheckedZones.Add(zone.GetInstanceID())) return;
        if (Instances.GetValue(zone) is not IDictionary instances)
            throw new InvalidOperationException("Native location instance index is unavailable.");
        var remove = new List<object>();
        foreach (DictionaryEntry entry in instances)
        {
            if (entry.Value is not ZoneSystem.LocationInstance instance || instance.m_location is null ||
                UnderworldZoneCatalogGuard.LocationName(instance.m_location) != UnderworldDeepGateLocationRegistrar.LocationName)
                continue;
            if (!IsAllowed(instance.m_position)) remove.Add(entry.Key);
        }
        if (remove.Count == 0) return;
        foreach (var key in remove) instances.Remove(key);
        foreach (var row in zone.m_locations)
        {
            if (row is null || UnderworldZoneCatalogGuard.LocationName(row) != UnderworldDeepGateLocationRegistrar.LocationName) continue;
            // Generate only this owned family. Preserve all other locations and generated zones.
            var routine = Generate.Invoke(zone, new object[] { row, Stopwatch.StartNew(), new ZPackage() }) as IEnumerator
                ?? throw new InvalidOperationException("Native gate location generator did not return an iterator.");
            zone.StartCoroutine(routine);
            break;
        }
        UnityEngine.Debug.Log($"Magenheim migrated {remove.Count} obsolete warm/spawn-area Deep Gate sites; only owned cold-biome sites are regenerated.");
    }
}

[HarmonyPatch(typeof(ZoneSystem), "GenerateLocationsIfNeeded")]
internal static class UnderworldSurfaceGateMigrationPatch
{
    private static void Postfix(ZoneSystem __instance) => UnderworldSurfaceGateMigration.Apply(__instance);
}
