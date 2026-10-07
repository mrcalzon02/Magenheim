using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Registers authored Underworld flora through Valheim's ordinary ZoneSystem vegetation pipeline.
/// Species without a verified authored model or native-compatible placement contract fail closed.
/// </summary>
internal sealed class UnderworldFloraWorldgenRegistrar : IDisposable
{
    private const string NativeTreeDonor = "YggaShoot1";

    private readonly UnderworldFloraDefinitionSet _definitions;
    private readonly ManualLogSource _log;
    private bool _subscribed;

    internal UnderworldFloraWorldgenRegistrar(UnderworldFloraDefinitionSet definitions, ManualLogSource log)
    {
        _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    internal void Register()
    {
        if (_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable += RegisterContent;
        _subscribed = true;
    }

    private void RegisterContent()
    {
        try
        {
            if (PrefabManager.Instance.GetPrefab(NativeTreeDonor) is null)
                throw new InvalidOperationException($"Native flora donor '{NativeTreeDonor}' is unavailable.");

            var registered = 0;
            foreach (var species in _definitions.Species)
            {
                if (species.RequiresRockFace)
                {
                    _log.LogWarning(
                        $"Underworld flora '{species.Id}' is not registered yet: its rock-face requirement " +
                        "does not map to a native ZoneVegetation field and will not be approximated.");
                    continue;
                }

                var speciesName = species.Id.Substring("magenheim.underworld.flora.".Length);
                if (!UnderworldFloraHarvest.All.TryGetValue(speciesName, out var harvest))
                {
                    _log.LogWarning(
                        $"Underworld flora '{species.Id}' is not registered: no verified authored runtime model is mapped.");
                    continue;
                }

                if (PrefabManager.Instance.GetPrefab(species.PrefabName) is not null)
                    throw new InvalidOperationException($"Occupied flora prefab identity '{species.PrefabName}'.");

                var prefab = PrefabManager.Instance.CreateClonedPrefab(species.PrefabName, NativeTreeDonor)
                    ?? throw new InvalidOperationException(
                        $"Unable to clone native flora donor '{NativeTreeDonor}' for '{species.PrefabName}'.");

                UnderworldFloraHarvest.Configure(prefab, speciesName, harvest);

                var config = new VegetationConfig
                {
                    Biome = JotunnWorldgenAdapter.MapBiome(species.BiomeId),
                    BiomeArea = JotunnWorldgenAdapter.MapArea(Magenheim.Core.Worldgen.SpawnArea.All),
                    BlockCheck = true,
                    ForcePlacement = false,
                    Min = 1f,
                    Max = 3f,
                    MinAltitude = -1000f,
                    MaxAltitude = 1000f,
                    MinOceanDepth = 0f,
                    MaxOceanDepth = 0f,
                    MinTerrainDelta = 0f,
                    MaxTerrainDelta = 1000f,
                    TerrainDeltaRadius = 4f,
                    MinTilt = 0f,
                    MaxTilt = species.MaxSlopeDegrees,
                    InForest = false,
                    ForestThresholdMin = 0f,
                    ForestThresholdMax = 1f,
                    ScaleMin = 0.95f,
                    ScaleMax = 1.05f,
                    GroupSizeMin = 1,
                    GroupSizeMax = 1,
                    GroupRadius = 0f,
                    GroundOffset = 0f,
                };

                var vegetation = new CustomVegetation(prefab, fixReference: true, config);
                if (!ZoneManager.Instance.AddCustomVegetation(vegetation))
                    throw new InvalidOperationException(
                        $"Jotunn refused native vegetation registration for '{species.PrefabName}'.");

                registered++;
            }

            RegisterFungalVariantsAndCover();
            _log.LogInfo(
                $"Registered {registered}/{_definitions.Species.Count} authoritative Underworld flora species " +
                "through native ZoneSystem vegetation. Unsupported placement contracts remain fail-closed.");
        }
        finally
        {
            Dispose();
        }
    }

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterContent;
        _subscribed = false;
    }

    private void RegisterFungalVariantsAndCover()
    {
        foreach (var species in new[] { "Glowcap", "Spirestalk" })
        {
            var donor = PrefabManager.Instance.GetPrefab("Magenheim_Underworld_" + species)
                ?? throw new InvalidOperationException("Missing authored canopy donor: " + species);
            var variants = species == "Glowcap" ? new[] { "bent", "elder", "twin" } :
                new[] { "young", "towered", "crooked" };
            foreach (var variant in variants)
            {
                var name = "Magenheim_Underworld_Scenery_" + species + "_" + variant;
                var prefab = PrefabManager.Instance.CreateClonedPrefab(name, donor);
                // Retain the configured species' native tree, logs, stump and yields.
                prefab.GetComponent<TreeBase>().m_trunk = UnderworldFloraHarvest.ApplyVisual(prefab,
                    "underworld-flora-fungal-" + species.ToLowerInvariant() + "-" + variant, false);
                AddFungalVegetation(prefab, .25f, .75f, 30f);
            }
        }
        foreach (var model in new[] { "fern", "sporebrush", "nursery-caps", "moss-stone",
                     "amber-bed", "sapling", "lantern-caps", "root-skirt" })
        {
            var prefab = new GameObject("Magenheim_Underworld_Scenery_Fungal_" + model);
            prefab.SetActive(false);
            prefab.AddComponent<ZNetView>().m_persistent = true;
            ModelAssets.Load(prefab, "underworld-flora-fungal-" + model);
            AddFungalVegetation(prefab, .5f, 1.5f, 45f);
            if (prefab.transform.parent is null || prefab.transform.parent.gameObject.activeInHierarchy)
                throw new InvalidOperationException("Fungal cover prototype needs an inactive prefab container.");
            prefab.SetActive(true);
        }
        _log.LogInfo("Registered six authored harvestable canopy variants and eight Fungal Forest ground-cover types with native vegetation placement.");
    }

    private static void AddFungalVegetation(GameObject prefab, float min, float max, float tilt)
    {
        if (!ZoneManager.Instance.AddCustomVegetation(new CustomVegetation(prefab, false,
                new VegetationConfig {
                    Biome = UnderworldTerrainRuntime.FungalForestBiome,
                    BiomeArea = Heightmap.BiomeArea.Everything, Min = min, Max = max,
                    MinAltitude = 1, MaxAltitude = 1200, MinOceanDepth = 0, MaxOceanDepth = 0,
                    MinTerrainDelta = 0, MaxTerrainDelta = 200, TerrainDeltaRadius = 2,
                    MinTilt = 0, MaxTilt = tilt, ScaleMin = .9f, ScaleMax = 1.1f,
                    GroupSizeMin = 1, GroupSizeMax = 1, BlockCheck = true, ForcePlacement = false
                })))
            throw new InvalidOperationException("Native Fungal Forest vegetation registration refused: " + prefab.name);
    }
}
