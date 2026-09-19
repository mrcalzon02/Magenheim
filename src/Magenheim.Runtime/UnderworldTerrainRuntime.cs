using System;
using BepInEx.Logging;
using HarmonyLib;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Runtime bridge into deterministic Underworld terrain authority.</summary>
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
        _ = services.TerrainDomain ?? throw new InvalidOperationException("Native Underworld terrain domain is unavailable.");
        UnderworldSpatialDomain.ValidateDefinition(services.SpatialDomain); // legacy placement adapter only
        _world = null;
        if (_patched) return;
        var harmony = new Harmony(MagenheimPlugin.PluginGuid + ".gameplay");
        harmony.PatchAll(typeof(UnderworldTerrainWorldPatch));
        harmony.PatchAll(typeof(UnderworldBiomeSectorColumnPatch));
        harmony.PatchAll(typeof(UnderworldBiomeSectorPointPatch));
        harmony.PatchAll(typeof(UnderworldTerrainHeightPatch));
        harmony.PatchAll(typeof(UnderworldTerrainBiomePatch));
        _patched = true;
        log.LogInfo("Native Underworld terrain authority configured; legacy Surface worldgen hooks remain isolated pending instance-chunk adapter replacement.");
    }

    internal static void CaptureWorld(World? world)
    {
        var services = _services;
        _world = null;
        if (services is null || world is null) return;
        var waterLevel = ZoneSystem.instance is null ? 30f : ZoneSystem.instance.m_waterLevel;
        _world = new WorldSnapshot(world.m_seed, waterLevel);
        _log?.LogInfo($"Underworld instance terrain context prepared; playable radius {services.TerrainDomain.RadiusMeters:0}m. Surface host placement is not terrain authority.");
    }

    internal static bool TryGetCapturedSeed(out int seed)
    {
        var identity = _services?.InstanceLifecycle.Identity;
        if (identity is not null) { seed = identity.DerivedSeed32; return true; }
        seed = default;
        return false;
    }

    /// <summary>Samples terrain directly in native Underworld instance coordinates.</summary>
    internal static UnderworldTerrainResult SampleInstanceTerrain(double x, double y, double z, double slopeDegrees = 0d)
    {
        var services = _services;
        var world = _world;
        var identity = services?.InstanceLifecycle.Identity;
        if (services is null || world is null || identity is null || !InstanceAvailable(services)) return default;
        var seed = identity.DerivedSeed32;
        var noise = UnderworldTerrainNoise.Fractal01(seed, x, z);
        return UnderworldTerrainLifecycle.Evaluate(
            services.TerrainDomain,
            new UnderworldTerrainSample(x, y, z, world.WaterLevel, slopeDegrees, noise),
            seed);
    }

    /// <summary>Builds player-facing map data. Legacy raster migration is tracked separately.</summary>
    internal static bool TryBuildBiomeRaster(int width, int height, out UnderworldTerrainBiome[] raster)
    {
        var services = _services;
        var identity = services?.InstanceLifecycle.Identity;
        if (services is null || identity is null || !InstanceAvailable(services)) { raster = Array.Empty<UnderworldTerrainBiome>(); return false; }
        raster = UnderworldMapRaster.BuildBiomeRaster(services.SpatialDomain, identity.DerivedSeed32, width, height);
        return true;
    }

    // Everything below this line is the quarantined legacy Surface-host adapter. New gameplay
    // consumers must use SampleInstanceTerrain. Existing callers remain only until their bounded migration.
    internal static float ShapeHeight(float wx, float wy, float vanillaHeight)
    {
        var services = _services; var world = _world;
        if (services is null || world is null || !InstanceAvailable(services)) return vanillaHeight;
        var domain = services.SpatialDomain;
        if (!UnderworldSpatialDomain.ContainsHostColumn(domain, wx, wy)) return vanillaHeight;
        var local = UnderworldSpatialDomain.ToLogicalColumn(domain, wx, wy);
        var result = SampleInstanceTerrain(local.X, 0d, local.Z);
        return result.Admitted ? (float)result.Height : vanillaHeight;
    }

    [Obsolete("Legacy host-coordinate adapter. Migrate callers to SampleInstanceTerrain with native instance coordinates.")]
    internal static UnderworldTerrainResult SampleTerrain(float wx, float wy, float vanillaHeight)
    {
        var services = _services;
        if (services is null || !UnderworldSpatialDomain.ContainsHostColumn(services.SpatialDomain, wx, wy)) return default;
        var local = UnderworldSpatialDomain.ToLogicalColumn(services.SpatialDomain, wx, wy);
        return SampleInstanceTerrain(local.X, 0d, local.Z);
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

    private static bool InstanceAvailable(UnderworldRuntimeServices services)
    {
        var phase = services.InstanceLifecycle.Phase;
        return phase == UnderworldInstancePhase.Admitting || phase == UnderworldInstancePhase.Active;
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
