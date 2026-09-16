using System;
using Magenheim.Core;
using Magenheim.Core.DeepFractures;
using UnityEngine;
using Jotunn.Managers;

namespace Magenheim.Runtime;

internal static class DeepFractureEncounterSpawner
{
    private const string SpawnIdentityKey = "magenheim.fracture.spawn.id";

    internal static void Populate(GameObject district, DeepFractureInteriorModulePlacement placement, DeepFractureEncounterAuthority authority, int locationSeed)
    {
        if (district is null) throw new ArgumentNullException(nameof(district));
        if (placement is null) throw new ArgumentNullException(nameof(placement));
        if (authority is null) throw new ArgumentNullException(nameof(authority));
        if (!authority.HasAuthority) return;

        for (var groupIndex = 0; groupIndex < placement.Encounters.Count; groupIndex++)
        {
            var encounter = placement.Encounters[groupIndex];
            var prefabName = ResolvePrefabName(encounter.Definition.Chassis, encounter.Definition.Alignment);
            var source = PrefabManager.Instance.GetPrefab(prefabName)
                ?? throw new InvalidOperationException($"Deep Fracture encounter '{encounter.Id}' requires registered creature prefab '{prefabName}'.");

            for (var unitIndex = 0; unitIndex < encounter.SpawnCount; unitIndex++)
            {
                var legacyIdentity = $"{placement.ModuleInstanceId}.{encounter.Id}.{unitIndex + 1:00}";
                var identity = $"{unchecked((uint)locationSeed):X8}.{legacyIdentity}";

                // Clear state is already isolated by each fracture authority ZDO, so accept the
                // pre-namespace key as a migration fallback. Live creature lookup is global,
                // however, and therefore must use the location-qualified identity exclusively.
                if (authority.IsCleared(identity) || authority.IsCleared(legacyIdentity))
                {
                    if (!authority.IsCleared(identity)) authority.MarkCleared(identity);
                    continue;
                }
                if (FindLivingSpawn(identity) is not null) continue;

                var worldPosition = district.transform.TransformPoint(SpawnOffset(groupIndex, unitIndex));
                var worldRotation = district.transform.rotation * Quaternion.Euler(0f, FacingYaw(groupIndex, unitIndex), 0f);
                var instance = UnityEngine.Object.Instantiate(source, worldPosition, worldRotation);
                instance.name = $"{prefabName}_{encounter.Id}_{unitIndex + 1:00}";

                var character = instance.GetComponent<Character>();
                var view = instance.GetComponent<ZNetView>();
                if (character is null || view is null || !view.IsValid())
                {
                    UnityEngine.Object.Destroy(instance);
                    throw new InvalidOperationException($"Deep Fracture creature '{prefabName}' did not create a valid networked Character/ZNetView instance.");
                }

                view.GetZDO().Set(SpawnIdentityKey, identity);
                var tracker = instance.GetComponent<DeepFractureEncounterDeathTracker>() ?? instance.AddComponent<DeepFractureEncounterDeathTracker>();
                tracker.Bind(character, authority, identity);
                instance.SetActive(true);
            }
        }
    }

    private static Character? FindLivingSpawn(string identity)
    {
        foreach (var character in Character.GetAllCharacters())
        {
            if (character is null || character.IsDead()) continue;
            var view = character.GetComponent<ZNetView>();
            if (view is null || !view.IsValid()) continue;
            if (string.Equals(view.GetZDO().GetString(SpawnIdentityKey, string.Empty), identity, StringComparison.Ordinal)) return character;
        }
        return null;
    }

    private static string ResolvePrefabName(CreatureChassis chassis, ElementalAlignment? alignment)
    {
        if (!alignment.HasValue) throw new InvalidOperationException($"Deep Fracture runtime creature '{chassis}' has no elemental alignment, but registered runtime families are alignment-specific.");
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

    private static float FacingYaw(int groupIndex, int unitIndex) => (groupIndex * 73f + unitIndex * 47f) % 360f;
}
