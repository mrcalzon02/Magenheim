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
                if (authority.IsCleared(identity) || authority.IsCleared(legacyIdentity))
                {
                    if (!authority.IsCleared(identity)) authority.MarkCleared(identity);
                    continue;
                }
                if (FindLivingSpawn(identity) is not null) continue;

                var localPose = TacticalPose(placement.PieceFamilyId, encounter.Definition.Chassis, groupIndex, unitIndex, encounter.SpawnCount);
                var worldPosition = district.transform.TransformPoint(localPose.Position);
                var worldRotation = district.transform.rotation * Quaternion.Euler(0f, localPose.YawDegrees, 0f);
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

    private static SpawnPose TacticalPose(string pieceFamilyId, CreatureChassis chassis, int groupIndex, int unitIndex, int groupSize)
    {
        var profile = DistrictProfile(pieceFamilyId);
        var lane = unitIndex - (groupSize - 1) * .5f;
        var flank = lane * profile.Spacing;
        var depth = ((unitIndex & 1) == 0 ? 1f : -1f) * profile.DepthSpread;
        var position = profile.Center + new Vector3(flank, .35f, depth);
        var yaw = Mathf.Atan2(-position.x, -position.z) * Mathf.Rad2Deg;

        switch (chassis)
        {
            case CreatureChassis.FacetSentry:
            case CreatureChassis.ObeliskWarden:
                position = profile.Center + Radial(groupIndex, unitIndex, profile.EdgeRadius);
                yaw = Mathf.Atan2(-position.x, -position.z) * Mathf.Rad2Deg;
                break;
            case CreatureChassis.StoneSentinel:
            case CreatureChassis.StoneGuardian:
            case CreatureChassis.CrystalGolem:
            case CreatureChassis.DeepColossus:
                position = profile.Center + new Vector3(flank * .45f, .35f, depth * .25f);
                break;
            case CreatureChassis.Burrower:
            case CreatureChassis.CrystalParasite:
                position = profile.Center + Radial(groupIndex + 3, unitIndex, profile.EdgeRadius * .82f);
                yaw = Mathf.Atan2(-position.x, -position.z) * Mathf.Rad2Deg;
                break;
            case CreatureChassis.AnnoyanceWisp:
                position.y = 2.8f + (unitIndex % 3) * .7f;
                break;
            case CreatureChassis.CrystalHound:
            case CreatureChassis.Shardling:
                position.x += ((unitIndex & 1) == 0 ? -1f : 1f) * 4f;
                break;
        }

        return new SpawnPose(position, yaw);
    }

    private static DistrictSpawnProfile DistrictProfile(string id) => id switch
    {
        "DF-01" => new DistrictSpawnProfile(new Vector3(0f,0f,12f), 5.5f, 6f, 24f),
        "DF-02" => new DistrictSpawnProfile(new Vector3(0f,0f,0f), 5f, 10f, 27f),
        "DF-03" => new DistrictSpawnProfile(new Vector3(0f,0f,0f), 6f, 7f, 29f),
        "DF-04" => new DistrictSpawnProfile(new Vector3(0f,0f,3f), 5f, 14f, 25f),
        "DF-05" => new DistrictSpawnProfile(new Vector3(0f,0f,0f), 6f, 9f, 29f),
        "DF-06" => new DistrictSpawnProfile(new Vector3(0f,0f,0f), 7f, 5f, 25f),
        "DF-07" => new DistrictSpawnProfile(new Vector3(0f,0f,0f), 5f, 13f, 27f),
        "DF-08" => new DistrictSpawnProfile(new Vector3(0f,0f,0f), 7f, 5f, 24f),
        "DF-09" => new DistrictSpawnProfile(new Vector3(0f,0f,0f), 5f, 12f, 31f),
        "DF-10" => new DistrictSpawnProfile(new Vector3(0f,0f,0f), 5f, 11f, 30f),
        "DF-11" => new DistrictSpawnProfile(new Vector3(0f,0f,0f), 6f, 7f, 25f),
        "DF-12" => new DistrictSpawnProfile(new Vector3(0f,0f,0f), 7f, 5f, 25f),
        "DF-13" => new DistrictSpawnProfile(new Vector3(0f,0f,0f), 5f, 10f, 29f),
        "DF-14" => new DistrictSpawnProfile(new Vector3(0f,0f,0f), 5f, 9f, 28f),
        "DF-15" => new DistrictSpawnProfile(new Vector3(0f,0f,0f), 6f, 8f, 27f),
        "DF-16" => new DistrictSpawnProfile(new Vector3(0f,0f,0f), 6f, 7f, 26f),
        "DF-17" => new DistrictSpawnProfile(new Vector3(0f,0f,0f), 5f, 12f, 30f),
        "DF-18" => new DistrictSpawnProfile(new Vector3(0f,0f,0f), 5f, 10f, 29f),
        "DF-19" => new DistrictSpawnProfile(new Vector3(0f,0f,0f), 6f, 8f, 27f),
        "DF-20" => new DistrictSpawnProfile(new Vector3(0f,0f,0f), 9f, 4f, 30f),
        _ => throw new InvalidOperationException($"No authored encounter placement profile exists for Deep Fracture district '{id}'.")
    };

    private static Vector3 Radial(int groupIndex, int unitIndex, float radius)
    {
        var angle = ((groupIndex * 137.5f) + (unitIndex * 61f)) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(angle) * radius, .35f, Mathf.Sin(angle) * radius);
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

    private readonly struct SpawnPose
    {
        internal SpawnPose(Vector3 position, float yawDegrees) { Position = position; YawDegrees = yawDegrees; }
        internal Vector3 Position { get; }
        internal float YawDegrees { get; }
    }

    private readonly struct DistrictSpawnProfile
    {
        internal DistrictSpawnProfile(Vector3 center, float spacing, float depthSpread, float edgeRadius) { Center = center; Spacing = spacing; DepthSpread = depthSpread; EdgeRadius = edgeRadius; }
        internal Vector3 Center { get; }
        internal float Spacing { get; }
        internal float DepthSpread { get; }
        internal float EdgeRadius { get; }
    }
}
