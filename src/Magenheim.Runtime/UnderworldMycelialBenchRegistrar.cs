using System;
using System.IO;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Registers the six foundational Underworld crafting stations. The native donor contributes only
/// proven Valheim CraftingStation/Piece behavior; when the authored station payload is present its
/// renderer is replaced by Magenheim-owned geometry from the model library.
/// </summary>
internal sealed class UnderworldMycelialBenchRegistrar : IDisposable
{
    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal UnderworldMycelialBenchRegistrar(ManualLogSource log) =>
        _log = log ?? throw new ArgumentNullException(nameof(log));

    internal void Register()
    {
        if (_subscribed || _registered) return;
        PrefabManager.OnVanillaPrefabsAvailable += RegisterStations;
        _subscribed = true;
    }

    private void RegisterStations()
    {
        if (_registered) return;
        var registered = 0;
        try
        {
            foreach (var definition in UnderworldStationCatalog.All)
            {
                RegisterStation(definition);
                registered++;
            }

            _registered = true;
            _log.LogInfo("Registered " + registered +
                " Underworld biome stations in progression order; authored station meshes are used whenever their exported payload is installed.");
        }
        catch (Exception exception)
        {
            _log.LogError("Underworld station registration failed after " + registered + " stations: " + exception);
        }
        finally
        {
            Dispose();
        }
    }

    private void RegisterStation(UnderworldStationDefinition definition)
    {
        RequirePrefab(definition.DonorPrefab);
        foreach (var cost in definition.Costs) RequirePrefab(cost.ResourcePrefab);
        if (!string.IsNullOrEmpty(definition.BuildStationPrefab)) RequirePrefab(definition.BuildStationPrefab);

        if (PrefabManager.Instance.GetPrefab(definition.Prefab))
            throw new InvalidOperationException("Occupied Underworld station identity: " + definition.Prefab);

        var requirements = new RequirementConfig[definition.Costs.Count];
        for (var index = 0; index < definition.Costs.Count; index++)
        {
            var cost = definition.Costs[index];
            requirements[index] = new RequirementConfig(cost.ResourcePrefab, cost.Amount, 0, true);
        }

        var config = new PieceConfig
        {
            Name = definition.Name,
            Description = definition.Description,
            PieceTable = "Hammer",
            Category = "Crafting",
            CraftingStation = definition.BuildStationPrefab,
            Requirements = requirements,
            Icon = HasAuthoredIcon(definition.ModelId) ? EarthAssets.Icon(definition.ModelId) : null,
        };

        var custom = new CustomPiece(definition.Prefab, definition.DonorPrefab, config);
        var prefab = custom.PiecePrefab;
        var station = prefab.GetComponent<CraftingStation>()
            ?? throw new InvalidOperationException(definition.DonorPrefab + " lost CraftingStation behavior.");
        station.m_name = definition.Name;

        if (HasAuthoredModel(definition.ModelId))
            ModelAssets.Load(prefab, definition.ModelId, preserveParticles: definition.PreserveDonorParticles);
        else
            ApplyBiomeFallbackAccent(prefab, definition.Biome);

        ConfigureCollider(prefab, definition.Dimensions);

        if (!PieceManager.Instance.AddPiece(custom))
            throw new InvalidOperationException("Jotunn refused Underworld station " + definition.Name + ".");

        _log.LogInfo("Registered Underworld station " + definition.Name + " (" + definition.PlacementRule +
            "), built at " + (string.IsNullOrEmpty(definition.BuildStationPrefab) ? "Hammer foothold" : definition.BuildStationPrefab) + ".");
    }

    private static void ConfigureCollider(GameObject prefab, UnderworldStationDimensions dimensions)
    {
        foreach (var collider in prefab.GetComponentsInChildren<Collider>(true))
            if (!collider.isTrigger) collider.enabled = false;

        var colliderObject = new GameObject("Magenheim_Underworld_Station_Collider");
        colliderObject.transform.SetParent(prefab.transform, false);
        var collider = colliderObject.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, dimensions.HeightMeters * 0.5f, 0f);
        collider.size = new Vector3(dimensions.WidthMeters, dimensions.HeightMeters, dimensions.DepthMeters);
    }

    private static void ApplyBiomeFallbackAccent(GameObject prefab, UnderworldTerrainBiome biome)
    {
        var tint = FallbackTint(biome);
        foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
        {
            var material = renderer.sharedMaterial;
            if (!material) continue;
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            if (material.HasProperty("_Color")) block.SetColor("_Color", tint);
            if (material.HasProperty("_BaseColor")) block.SetColor("_BaseColor", tint);
            renderer.SetPropertyBlock(block);
        }
    }

    private static Color FallbackTint(UnderworldTerrainBiome biome)
    {
        switch (biome)
        {
            case UnderworldTerrainBiome.BlackwaterDeep: return new Color(0.36f, 0.48f, 0.52f, 1f);
            case UnderworldTerrainBiome.SulfurousWastes: return new Color(0.52f, 0.30f, 0.16f, 1f);
            case UnderworldTerrainBiome.FrozenCaverns: return new Color(0.60f, 0.76f, 0.84f, 1f);
            case UnderworldTerrainBiome.FractureZones: return new Color(0.46f, 0.40f, 0.54f, 1f);
            case UnderworldTerrainBiome.GreatDecay: return new Color(0.45f, 0.36f, 0.20f, 1f);
            default: return new Color(0.46f, 0.58f, 0.32f, 1f);
        }
    }

    private static bool HasAuthoredModel(string modelId)
    {
        var directory = Path.GetDirectoryName(typeof(UnderworldMycelialBenchRegistrar).Assembly.Location);
        return !string.IsNullOrEmpty(directory) &&
               File.Exists(Path.Combine(directory, "assets", "models", "runtime", modelId + ".model.json"));
    }

    private static bool HasAuthoredIcon(string modelId)
    {
        var directory = Path.GetDirectoryName(typeof(UnderworldMycelialBenchRegistrar).Assembly.Location);
        return !string.IsNullOrEmpty(directory) &&
               File.Exists(Path.Combine(directory, "assets", "earth", modelId + ".icon.png"));
    }

    private static void RequirePrefab(string prefab)
    {
        if (PrefabManager.Instance.GetPrefab(prefab) is null && !CustomItem.IsCustomItem(prefab))
            throw new InvalidOperationException("Required Underworld station dependency unavailable: " + prefab);
    }

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterStations;
        _subscribed = false;
    }
}
