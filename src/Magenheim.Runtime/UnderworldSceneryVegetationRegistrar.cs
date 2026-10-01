using System;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Native decorative vegetation for biome identity that should not carry donor harvesting,
/// destruction or drops. Native network persistence is retained for vegetation streaming under
/// Magenheim-owned identities, so Surface worldgen can partition them from the detached Underworld
/// catalog without mutating Valheim's original Ashlands prefabs.
/// </summary>
internal sealed class UnderworldSceneryVegetationRegistrar : IDisposable
{
    private sealed record Spec(
        string Id,
        string Donor,
        int Variant,
        float Min,
        float Max,
        float ScaleMin,
        float ScaleMax,
        int GroupMin,
        int GroupMax,
        float GroupRadius,
        float MaxTilt);

    private static readonly Spec[] SulfurousWastes =
    {
        new("ScorchedTree1", "AshlandsTree1", 101, 1f, 3f, .88f, 1.12f, 1, 1, 0f, 34f),
        new("ScorchedTree3", "AshlandsTree3", 203, 1f, 3f, .84f, 1.16f, 1, 1, 0f, 34f),
        new("ScorchedTree4", "AshlandsTree4", 307, 1f, 3f, .88f, 1.14f, 1, 1, 0f, 36f),
        new("ScorchedTree5", "AshlandsTree5", 409, 1f, 3f, .84f, 1.18f, 1, 1, 0f, 36f),
        new("CharredBranch1", "AshlandsBranch1", 503, 2f, 5f, .78f, 1.25f, 1, 2, 3.5f, 44f),
        new("CharredBranch2", "AshlandsBranch2", 607, 2f, 5f, .72f, 1.28f, 1, 2, 3.5f, 44f),
        new("CharredBranch3", "AshlandsBranch3", 709, 2f, 5f, .76f, 1.24f, 1, 2, 3.5f, 44f),
        new("BurntStump1", "AshlandsTreeStump1", 811, 1f, 4f, .82f, 1.18f, 1, 2, 3f, 40f),
        new("BurntStump2", "AshlandsTreeStump2", 907, 1f, 4f, .82f, 1.18f, 1, 2, 3f, 40f),
        new("BurntStump3", "AshlandsTreeStump3", 1009, 1f, 4f, .82f, 1.18f, 1, 2, 3f, 40f),
        new("CinderBush1", "AshlandsBush1", 1103, 3f, 7f, .72f, 1.26f, 2, 4, 5f, 32f),
        new("CinderBush2", "AshlandsBush2", 1201, 3f, 7f, .72f, 1.26f, 2, 4, 5f, 32f),
    };

    private readonly ManualLogSource _log;
    private bool _subscribed;

    internal UnderworldSceneryVegetationRegistrar(ManualLogSource log) =>
        _log = log ?? throw new ArgumentNullException(nameof(log));

    internal void Register()
    {
        if (_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable += RegisterContent;
        _subscribed = true;
    }

    private void RegisterContent()
    {
        var registered = 0;
        try
        {
            foreach (var spec in SulfurousWastes)
            {
                GameObject? prefab = null;
                try
                {
                    var name = "Magenheim_Underworld_Scenery_" + spec.Id;
                    if (PrefabManager.Instance.GetPrefab(name) is not null)
                        throw new InvalidOperationException($"Occupied scenery prefab identity '{name}'.");

                    prefab = UnderworldDonorVisualFactory.CreateSpecific(
                        UnderworldTerrainBiome.SulfurousWastes,
                        spec.Donor,
                        spec.Variant,
                        name);

                    // Native ZoneSystem vegetation requires a registered network identity,
                    // even when its owned visual deliberately has no harvesting behavior.
                    var view=prefab.GetComponent<ZNetView>();
                    if(!view)view=prefab.AddComponent<ZNetView>();
                    view.m_persistent=true;

                    var config = new VegetationConfig
                    {
                        Biome = UnderworldTerrainRuntime.SulfurousWastesBiome,
                        BiomeArea = Heightmap.BiomeArea.Everything,
                        BlockCheck = true,
                        ForcePlacement = false,
                        Min = spec.Min,
                        Max = spec.Max,
                        MinAltitude = 1f,
                        MaxAltitude = 1000f,
                        MinOceanDepth = 0f,
                        MaxOceanDepth = 0f,
                        MinTerrainDelta = 0f,
                        MaxTerrainDelta = 1000f,
                        TerrainDeltaRadius = 4f,
                        MinTilt = 0f,
                        MaxTilt = spec.MaxTilt,
                        InForest = false,
                        ForestThresholdMin = 0f,
                        ForestThresholdMax = 1f,
                        ScaleMin = spec.ScaleMin,
                        ScaleMax = spec.ScaleMax,
                        GroupSizeMin = spec.GroupMin,
                        GroupSizeMax = spec.GroupMax,
                        GroupRadius = spec.GroupRadius,
                        GroundOffset = 0f,
                    };
                    if (!ZoneManager.Instance.AddCustomVegetation(
                            new CustomVegetation(prefab, fixReference: true, config)))
                        throw new InvalidOperationException(
                            $"Jotunn refused native Underworld scenery registration for '{name}'.");
                    prefab = null; // ownership transferred to the registered custom vegetation
                    registered++;
                }
                catch (Exception exception)
                {
                    if (prefab) UnityEngine.Object.Destroy(prefab);
                    _log.LogWarning(
                        $"Sulfurous Wastes scenery '{spec.Id}' unavailable: {exception.Message}");
                }
            }
            _log.LogInfo(
                $"Registered {registered}/{SulfurousWastes.Length} stripped burnt-scenery vegetation rows for Sulfurous Wastes.");
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
