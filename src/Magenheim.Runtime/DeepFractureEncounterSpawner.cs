using System;
using Magenheim.Core;
using Magenheim.Core.DeepFractures;
using UnityEngine;
using Jotunn.Managers;

namespace Magenheim.Runtime;

internal static class DeepFractureEncounterSpawner
{
    internal static void Populate(GameObject district, DeepFractureInteriorModulePlacement placement)
    {
        if (district is null) throw new ArgumentNullException(nameof(district));
        if (placement is null) throw new ArgumentNullException(nameof(placement));

        for (var groupIndex = 0; groupIndex < placement.Encounters.Count; groupIndex++)
        {
            var encounter = placement.Encounters[groupIndex];
            var prefabName = ResolvePrefabName(encounter.Definition.Chassis, encounter.Definition.Alignment);
            var source = PrefabManager.Instance.GetPrefab(prefabName)
                ?? throw new InvalidOperationException($"Deep Fracture encounter '{encounter.Id}' requires registered creature prefab '{prefabName}'.");

            for (var unitIndex = 0; unitIndex < encounter.SpawnCount; unitIndex++)
            {
                var instance = UnityEngine.Object.Instantiate(source, district.transform, false);
                instance.name = $"{prefabName}_{encounter.Id}_{unitIndex + 1:00}";
                instance.transform.localPosition = SpawnOffset(groupIndex, unitIndex);
                instance.transform.localRotation = Quaternion.Euler(0f, FacingYaw(groupIndex, unitIndex), 0f);
                instance.SetActive(true);
            }
        }
    }

    private static string ResolvePrefabName(CreatureChassis chassis, ElementalAlignment? alignment)
    {
        if (!alignment.HasValue)
            throw new InvalidOperationException($"Deep Fracture runtime creature '{chassis}' has no elemental alignment, but registered runtime families are alignment-specific.");

        var prefix = chassis switch
        {
            CreatureChassis.AnnoyanceWisp => DeepFractureCreatureRegistrar.AnnoyanceWispPrefix,
            CreatureChassis.GeodeCrawler => DeepFractureCreatureRegistrar.GeodeCrawlerPrefix,
            CreatureChassis.Shardling => DeepFractureCreatureRegistrar.ShardlingPrefix,
            CreatureChassis.CrystalParasite => DeepFractureCreatureRegistrar.CrystalParasitePrefix,
            CreatureChassis.CrystalRevenant => DeepFractureCreatureRegistrar.CrystalRevenantPrefix,
            CreatureChassis.FacetSentry => DeepFractureCreatureRegistrar.FacetSentryPrefix,
            CreatureChassis.StoneSentinel => DeepFractureCreatureRegistrar.StoneSentinelPrefix,
            CreatureChassis.CrystalHound => DeepFractureCreatureRegistrar.CrystalHoundPrefix,
            CreatureChassis.Burrower => DeepFractureCreatureRegistrar.BurrowerPrefix,
            CreatureChassis.StoneGuardian => DeepFractureCreatureRegistrar.StoneGuardianPrefix,
            CreatureChassis.CrystalGolem => DeepFractureCreatureRegistrar.CrystalGolemPrefix,
            CreatureChassis.ObeliskWarden => DeepFractureCreatureRegistrar.ObeliskWardenPrefix,
            CreatureChassis.DeepColossus => DeepFractureCreatureRegistrar.DeepColossusPrefix,
            _ => throw new InvalidOperationException($"No runtime Deep Fracture creature family is registered for chassis '{chassis}'.")
        };
        return prefix + alignment.Value;
    }

    private static Vector3 SpawnOffset(int groupIndex, int unitIndex)
    {
        var angle = ((groupIndex * 137.5f) + (unitIndex * 61f)) * Mathf.Deg2Rad;
        var radius = 4.5f + (groupIndex * 3.25f) + ((unitIndex % 3) * 1.6f);
        return new Vector3(Mathf.Cos(angle) * radius, 0.35f, Mathf.Sin(angle) * radius);
    }

    private static float FacingYaw(int groupIndex, int unitIndex)
        => (groupIndex * 73f + unitIndex * 47f) % 360f;
}
