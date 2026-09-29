using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Magenheim.Core.Definitions;
using Magenheim.Core.Underworld;
using Magenheim.Core.Worldgen;

namespace Magenheim.Runtime;

/// <summary>
/// Bridges Magenheim's already-approved Surface geode vegetation into the detached Underworld
/// ZoneSystem and proves that required Underworld resource spawners survived catalog partitioning.
/// Prefabs are never re-registered or duplicated.
/// </summary>
internal static class UnderworldWorldgenContentBridge
{
    private static readonly IReadOnlyDictionary<string, UnderworldTerrainBiome> GeodeBiomeById =
        new Dictionary<string, UnderworldTerrainBiome>(StringComparer.Ordinal)
        {
            ["magenheim.geode.meadows.earth"] = UnderworldTerrainBiome.FungalForest,
            ["magenheim.geode.black_forest"] = UnderworldTerrainBiome.FractureZones,
            ["magenheim.geode.swamp"] = UnderworldTerrainBiome.BlackwaterDeep,
            ["magenheim.geode.mountain"] = UnderworldTerrainBiome.FrozenCaverns,
            ["magenheim.geode.plains"] = UnderworldTerrainBiome.FractureZones,
            ["magenheim.geode.mistlands"] = UnderworldTerrainBiome.GreatDecay,
            ["magenheim.geode.ashlands"] = UnderworldTerrainBiome.SulfurousWastes,
            ["magenheim.geode.deep_north"] = UnderworldTerrainBiome.FrozenCaverns,
        };

    private static MagenheimDefinitionSet? _definitions;
    private static ManualLogSource? _log;
    private static readonly HashSet<string> AdmittedGeodePrefabs = new(StringComparer.Ordinal);

    internal static void Configure(MagenheimDefinitionSet definitions, ManualLogSource log)
    {
        _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        AdmittedGeodePrefabs.Clear();
    }

    internal static void PopulateGeodes(ZoneSystem surface, ZoneSystem underworld)
    {
        var definitions = _definitions ?? throw new InvalidOperationException(
            "Underworld worldgen content bridge was not configured.");
        if (!surface) throw new ArgumentNullException(nameof(surface));
        if (!underworld) throw new ArgumentNullException(nameof(underworld));

        AdmittedGeodePrefabs.Clear();
        var added = 0;
        var compatibilitySkipped = 0;

        foreach (var geode in definitions.Geodes)
        {
            if (!GeodeBiomeById.TryGetValue(geode.Id, out var targetBiome))
                throw new InvalidOperationException(
                    $"Geode '{geode.Id}' has no explicit Underworld biome assignment.");

            var worldPrefabName = GeodeWorldPrefabIdentity.FromItemPrefabName(geode.PrefabName);
            var surfaceBiome = JotunnWorldgenAdapter.MapBiome(geode.Biome);
            var source = FindVegetation(surface, worldPrefabName, surfaceBiome);
            if (source is null)
            {
                // Respect the existing additive compatibility planner. A deliberately skipped
                // geode must not be resurrected inside instance 1 behind that policy boundary.
                compatibilitySkipped++;
                _log?.LogWarning(
                    $"Underworld geode '{geode.Id}' skipped because its approved Surface vegetation row '{worldPrefabName}' is absent.");
                continue;
            }

            var underworldBiome = UnderworldTerrainRuntime.ToNativeBiome(targetBiome);
            if (FindVegetation(underworld, worldPrefabName, underworldBiome) is not null)
            {
                AdmittedGeodePrefabs.Add(worldPrefabName);
                continue;
            }

            var clone = source.Clone();
            clone.m_name = (source.m_name ?? worldPrefabName) + "_Underworld_" + targetBiome;
            clone.m_biome = underworldBiome;
            // Surface altitude/ocean filters describe open-sky terrain. Keep authored density,
            // grouping, scale, slope and visuals while admitting the subterranean elevation range.
            clone.m_minAltitude = -1000f;
            clone.m_maxAltitude = 1000f;
            clone.m_minOceanDepth = 0f;
            clone.m_maxOceanDepth = 0f;
            underworld.m_vegetation.Add(clone);
            AdmittedGeodePrefabs.Add(worldPrefabName);
            added++;
        }

        _log?.LogInfo(
            $"Underworld geodes: {AdmittedGeodePrefabs.Count}/{definitions.Geodes.Count} compatibility-approved geode prefabs admitted ({added} instance-only rows added, {compatibilitySkipped} skipped by existing worldgen policy).");
    }

    internal static void ValidateUnderworldCatalog(ZoneSystem underworld)
    {
        if (!underworld) throw new ArgumentNullException(nameof(underworld));

        var missingResources = new List<string>();
        foreach (var resource in UnderworldResourceCatalog.All)
        {
            var biome = UnderworldTerrainRuntime.ToNativeBiome(resource.Biome);
            if (FindVegetation(underworld, resource.PickupPrefab, biome) is null)
                missingResources.Add(resource.PickupPrefab + "@" + resource.Biome);
        }

        if (missingResources.Count != 0)
            throw new InvalidOperationException(
                $"Underworld resource worldgen catalog incomplete: {UnderworldResourceCatalog.All.Count - missingResources.Count}/{UnderworldResourceCatalog.All.Count} required pickup spawners present. Missing: {string.Join(", ", missingResources)}");

        var missingVents = new List<string>();
        foreach (var vent in UnderworldGeothermalVentRegistrar.Vents)
            if (FindVegetation(underworld, vent.Prefab, UnderworldTerrainRuntime.ToNativeBiome(UnderworldTerrainBiome.SulfurousWastes)) is null)
                missingVents.Add(vent.Prefab);
        if (missingVents.Count != 0)
            throw new InvalidOperationException(
                $"Underworld geothermal catalog incomplete: {UnderworldGeothermalVentRegistrar.Vents.Length - missingVents.Count}/{UnderworldGeothermalVentRegistrar.Vents.Length} live-vent spawners present. Missing: {string.Join(", ", missingVents)}");

        var missingDungeons = new List<string>();
        foreach (var dungeon in UnderworldDungeonCatalog.All)
        {
            if (dungeon.Status != UnderworldDungeonStatus.RuntimeReady) continue;
            var expectedBiome = UnderworldTerrainRuntime.ToNativeBiome(dungeon.Biome);
            var found = false;
            foreach (var location in underworld.m_locations)
            {
                if (location is null) continue;
                var name = location.m_prefab.Name ?? string.Empty;
                if (!string.Equals(name, dungeon.PrefabName, StringComparison.Ordinal)) continue;
                if ((location.m_biome & expectedBiome) == Heightmap.Biome.None &&
                    location.m_biome != expectedBiome)
                    continue;
                found = true;
                break;
            }
            if (!found) missingDungeons.Add(dungeon.PrefabName + "@" + dungeon.Biome);
        }

        if (missingDungeons.Count != 0)
            throw new InvalidOperationException(
                "Underworld runtime-ready dungeon catalog incomplete: " +
                string.Join(", ", missingDungeons));

        var missingGeodes = new List<string>();
        foreach (var prefabName in AdmittedGeodePrefabs)
        {
            var found = false;
            foreach (var vegetation in underworld.m_vegetation)
            {
                if (vegetation is null || !vegetation.m_prefab ||
                    !string.Equals(vegetation.m_prefab.name, prefabName, StringComparison.Ordinal) ||
                    !UnderworldTerrainRuntime.IsUnderworldBiome(vegetation.m_biome))
                    continue;
                found = true;
                break;
            }
            if (!found) missingGeodes.Add(prefabName);
        }

        if (missingGeodes.Count != 0)
            throw new InvalidOperationException(
                "Underworld geode worldgen rows disappeared before startup: " +
                string.Join(", ", missingGeodes));

        _log?.LogInfo(
            $"Verified Underworld worldgen resources: {UnderworldResourceCatalog.All.Count}/{UnderworldResourceCatalog.All.Count} resource pickup spawners, {UnderworldGeothermalVentRegistrar.Vents.Length} geothermal vent spawners, {AdmittedGeodePrefabs.Count} geode spawners and {RuntimeReadyDungeonCount()} runtime-ready dungeon family/families are instance-local and generation-ready.");
    }

    private static int RuntimeReadyDungeonCount()
    {
        var count = 0;
        foreach (var dungeon in UnderworldDungeonCatalog.All)
            if (dungeon.Status == UnderworldDungeonStatus.RuntimeReady)
                count++;
        return count;
    }

    private static ZoneSystem.ZoneVegetation? FindVegetation(
        ZoneSystem zoneSystem,
        string prefabName,
        Heightmap.Biome biome)
    {
        foreach (var vegetation in zoneSystem.m_vegetation)
        {
            if (vegetation is null || !vegetation.m_prefab) continue;
            if (!string.Equals(vegetation.m_prefab.name, prefabName, StringComparison.Ordinal)) continue;
            if ((vegetation.m_biome & biome) == Heightmap.Biome.None && vegetation.m_biome != biome) continue;
            return vegetation;
        }
        return null;
    }
}
