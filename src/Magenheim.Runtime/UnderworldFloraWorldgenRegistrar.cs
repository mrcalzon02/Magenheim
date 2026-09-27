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

    private static readonly IReadOnlyDictionary<string, string> ModelBySpecies =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["magenheim.underworld.flora.glowcap"] = "underworld-flora-fungal-glowcap",
            ["magenheim.underworld.flora.spirestalk"] = "underworld-flora-fungal-spirestalk",
        };

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

                if (!ModelBySpecies.TryGetValue(species.Id, out var modelId))
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

                ModelAssets.Load(prefab, modelId, item: false);

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
}
