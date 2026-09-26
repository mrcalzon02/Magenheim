using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Server-authoritative natural Underworld resource population.
/// Deterministic placement comes from Core; durable pickup state remains in native Valheim ZDOs.
/// This runtime never tracks players, writes save files, or derives placement from Surface world space.
/// </summary>
internal static class UnderworldResourcePopulationRuntime
{
    private const string ResourceKindPrefix = "resource:";

    internal static int PopulateCell(
        UnderworldRuntimeServices services,
        UnderworldWorldIdentity identity,
        UnderworldInstanceChunkKey cell,
        ManualLogSource? log)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        if (ZNet.instance is null || !ZNet.instance.IsServer() || ZDOMan.instance is null) return 0;

        var centerX = (cell.X + .5f) * UnderworldEcologyCells.SizeMeters;
        var centerZ = (cell.Z + .5f) * UnderworldEcologyCells.SizeMeters;
        var center = UnderworldTerrainRuntime.SampleInstanceTerrain(centerX, 0, centerZ);
        if (!center.Admitted) return 0;
        var biome = center.Biome;

        var spawned = 0;
        foreach (var placement in UnderworldResourcePlacementPlanner.Plan(identity.DerivedSeed32, cell, biome))
        {
            if (placement.X * placement.X + placement.Z * placement.Z < 36d * 36d) continue;

            var support = UnderworldTerrainRuntime.SampleInstanceTerrain(placement.X, 0, placement.Z);
            if (!support.Admitted || support.Biome != biome) continue;

            var objectKind = ResourceKindPrefix + placement.ResourceId;
            var placementKey = UnderworldStructurePlacementKey.For(cell, placement.Slot);
            var stableObjectId = UnderworldGeneratedObjectIdentity.BuildStableObjectId(identity, objectKind, placementKey);
            if (HasExistingZdo(placement.PickupPrefab, stableObjectId)) continue;

            var source = PrefabManager.Instance.GetPrefab(placement.PickupPrefab);
            if (!source || !source.GetComponent<Pickable>() || !source.GetComponent<ZNetView>())
            {
                log?.LogWarning($"Natural Underworld resource '{placement.PickupPrefab}' is not registered as a native Pickable/ZNetView prefab.");
                continue;
            }

            services.StructureAdmission.Admit(identity, objectKind, cell, placement.Slot, () =>
            {
                var logical = new Vector3((float)placement.X, (float)support.Height, (float)placement.Z);
                var instance = UnityEngine.Object.Instantiate(
                    source,
                    UnderworldInstanceLayer.ToEngine(logical),
                    Quaternion.Euler(0f, (float)placement.RotationDegrees, 0f));
                instance.name = $"{placement.PickupPrefab}_{cell.X}_{cell.Z}_{placement.Slot:00}";
                if (instance.GetComponent<UnderworldZdoStateAdapter>() is null)
                    instance.AddComponent<UnderworldZdoStateAdapter>();
                if (instance.GetComponent<UnderworldPersistentObjectBinding>() is null)
                    instance.AddComponent<UnderworldPersistentObjectBinding>();
                instance.SetActive(true);
                return instance;
            });
            spawned++;
        }

        if (spawned > 0)
            log?.LogDebug($"Admitted {spawned} persistent {biome} resource pickup(s) in Underworld ecology cell ({cell.X},{cell.Z}).");
        return spawned;
    }

    private static bool HasExistingZdo(string prefabName, string stableObjectId)
    {
        var found = new List<ZDO>();
        var cursor = 0;
        var passes = 0;
        while (!ZDOMan.instance.GetAllZDOsWithPrefabIterative(prefabName, found, ref cursor))
        {
            if (++passes > 4096)
                throw new InvalidOperationException($"Aborted native ZDO reconciliation scan for '{prefabName}'.");
        }

        foreach (var zdo in found)
            if (zdo != null && string.Equals(
                zdo.GetString(UnderworldZdoStateAdapter.StableObjectIdKey, string.Empty),
                stableObjectId,
                StringComparison.Ordinal))
                return true;
        return false;
    }
}
