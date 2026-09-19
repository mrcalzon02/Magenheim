using System;
using BepInEx.Logging;
using HarmonyLib;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Runtime bridge from Valheim world generation into deterministic Underworld terrain authority.</summary>
internal static class UnderworldTerrainRuntime
{
    private static UnderworldRuntimeServices? _services;
    private static ManualLogSource? _log;
    private static bool _patched;
    private sealed record WorldSnapshot(int Seed, double WaterLevel);
    private static volatile WorldSnapshot? _world;

    internal static void Configure(UnderworldRuntimeServices services, ManualLogSource log)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        UnderworldSpatialDomain.ValidateDefinition(services.SpatialDomain);
        _world = null;
        if (_patched) return;
        var harmony = new Harmony(MagenheimPlugin.PluginGuid + ".gameplay");
        harmony.PatchAll(typeof(UnderworldTerrainWorldPatch));
        harmony.PatchAll(typeof(UnderworldBiomeSectorColumnPatch));
        harmony.PatchAll(typeof(UnderworldBiomeSectorPointPatch));
        harmony.PatchAll(typeof(UnderworldTerrainHeightPatch));
        harmony.PatchAll(typeof(UnderworldTerrainBiomePatch));
        _patched = true;
        log.LogInfo("Underworld terrain generation hooks installed for GetBiomeHeight/GetBiome/GetBiomeSector.");
    }

    internal static void CaptureWorld(World? world)
    {
        var services = _services;
        _world = null;
        if (services is null || world is null) return;
        var waterLevel = ZoneSystem.instance is null ? 30f : ZoneSystem.instance.m_waterLevel;
        _world = new WorldSnapshot(world.m_seed, waterLevel);
        var domain = services.SpatialDomain;
        _log?.LogInfo($"Underworld terrain shaping prepared for seed {world.m_seed} in the reserved region at ({domain.HostCenterX:0}, {domain.HostCenterZ:0}), playable radius {domain.RadiusMeters:0}m; runtime admission is controlled by the shared instance lifecycle.");
    }

    internal static bool TryGetCapturedSeed(out int seed)
    {
        var world = _world;
        if (world is null) { seed = default; return false; }
        seed = world.Seed;
        return true;
    }

    /// <summary>Builds player-facing map data from the exact spatial-domain and seed authorities used by terrain.</summary>
    internal static bool TryBuildBiomeRaster(int width, int height, out UnderworldTerrainBiome[] raster)
    {
        var services = _services;
        var world = _world;
        if (services is null || world is null || !InstanceAvailable(services)) { raster = Array.Empty<UnderworldTerrainBiome>(); return false; }
        raster = UnderworldMapRaster.BuildBiomeRaster(services.SpatialDomain, world.Seed, width, height);
        return true;
    }

    internal static float ShapeHeight(float wx, float wy, float vanillaHeight)
    {
        var services = _services; var world = _world;
        if (services is null || world is null || !InstanceAvailable(services)) return vanillaHeight;
        var domain = services.SpatialDomain;
        if (!UnderworldSpatialDomain.ContainsHostColumn(domain, wx, wy)) return vanillaHeight;
        var result = Evaluate(services, world, wx, wy, vanillaHeight);
        return result.Admitted ? (float)result.Height : vanillaHeight;
    }

    internal static UnderworldTerrainResult SampleTerrain(float wx, float wy, float vanillaHeight)
    {
        var services = _services; var world = _world;
        if (services is null || world is null || !InstanceAvailable(services)) return default;
        if (!UnderworldSpatialDomain.ContainsHostColumn(services.SpatialDomain, wx, wy)) return default;
        return Evaluate(services, world, wx, wy, vanillaHeight);
    }

    private static bool InstanceAvailable(UnderworldRuntimeServices services)
    {
        var phase = services.InstanceLifecycle.Phase;
        return phase == UnderworldInstancePhase.Admitting || phase == UnderworldInstancePhase.Active;
    }

    private static UnderworldTerrainResult Evaluate(UnderworldRuntimeServices services, WorldSnapshot world, float wx, float wy, float vanillaHeight)
    {
        var domain = services.SpatialDomain;
        var local = UnderworldSpatialDomain.ToLogicalColumn(domain, wx, wy);
        var seed = world.Seed;
        var noise = UnderworldTerrainNoise.Fractal01(seed, local.X, local.Z);
        return UnderworldTerrainLifecycle.Evaluate(domain, new UnderworldTerrainSample(local.X, 0d, local.Z, world.WaterLevel, 0d, noise), seed);
    }

    internal static Heightmap.Biome SelectVanillaBiome(float wx, float wy, Heightmap.Biome vanillaBiome)
    {
        var services = _services;
        if (services is null || _world is null || !InstanceAvailable(services)) return vanillaBiome;
        return UnderworldSpatialDomain.ContainsHostColumn(services.SpatialDomain, wx, wy) ? Heightmap.Biome.Meadows : vanillaBiome;
    }

    internal static BiomeSector SelectBiomeSector(double wx, double wz, BiomeSector vanillaSector)
    {
        var services = _services;
        if (services is null || _world is null || !InstanceAvailable(services)) return vanillaSector;
        if (!UnderworldSpatialDomain.ContainsHostColumn(services.SpatialDomain, wx, wz)) return vanillaSector;
        return (BiomeSector.EmptyMeadows ?? vanillaSector)!;
    }
}

[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.Initialize), new Type[] { typeof(World) })]
internal static class UnderworldTerrainWorldPatch { private static void Postfix(World world) => UnderworldTerrainRuntime.CaptureWorld(world); }

[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GetBiomeSector), new Type[] { typeof(float), typeof(float), typeof(bool) })]
internal static class UnderworldBiomeSectorColumnPatch { private static void Postfix(float wx, float wy, ref BiomeSector __result) => __result = UnderworldTerrainRuntime.SelectBiomeSector(wx, wy, __result); }

[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GetBiomeSector), new Type[] { typeof(Vector3), typeof(bool) })]
internal static class UnderworldBiomeSectorPointPatch { private static void Postfix(Vector3 worldPos, ref BiomeSector __result) => __result = UnderworldTerrainRuntime.SelectBiomeSector(worldPos.x, worldPos.z, __result); }

[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GetBiomeHeight), new Type[] { typeof(Heightmap.Biome), typeof(float), typeof(float), typeof(Color), typeof(bool), typeof(bool) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Normal, ArgumentType.Normal })]
internal static class UnderworldTerrainHeightPatch { private static void Postfix(float wx, float wy, ref float __result) => __result = UnderworldTerrainRuntime.ShapeHeight(wx, wy, __result); }

[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GetBiome), new Type[] { typeof(float), typeof(float), typeof(float), typeof(bool) })]
internal static class UnderworldTerrainBiomePatch { private static void Postfix(float wx, float wy, ref Heightmap.Biome __result) => __result = UnderworldTerrainRuntime.SelectVanillaBiome(wx, wy, __result); }
