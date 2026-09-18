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

    /// <summary>
    /// Immutable snapshot of the active world, captured on the main thread.
    /// </summary>
    /// <remarks>
    /// Valheim builds heightmaps on <c>HeightmapBuilder</c>'s worker thread, and that thread is what
    /// calls <c>WorldGenerator.GetBiomeHeight</c>/<c>GetBiome</c>. Reading <c>ZNet.instance</c>,
    /// <c>ZNet.World</c> or <c>ZoneSystem.instance</c> from there is a main-thread-only Unity call
    /// and takes the whole player down with a native crash during world generation, with nothing
    /// written to the log after <c>ZNet.LoadWorld</c>. So the world is read once on the main thread
    /// and the generation hooks use only this snapshot plus pure math.
    ///
    /// There is no "Underworld session" any more. The Underworld is a reserved region of this same
    /// world, so whether a column is Underworld is decided purely by its coordinates. Every loaded
    /// world gets a snapshot; columns outside the region are returned untouched.
    /// </remarks>
    private sealed record WorldSnapshot(int Seed, double WaterLevel);

    private static volatile WorldSnapshot? _world;

    internal static void Configure(UnderworldRuntimeServices services, ManualLogSource log)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _world = null;
        if (_patched) return;
        var harmony = new Harmony(MagenheimPlugin.PluginGuid + ".gameplay");
        harmony.PatchAll(typeof(UnderworldTerrainWorldPatch));
        harmony.PatchAll(typeof(UnderworldTerrainHeightPatch));
        harmony.PatchAll(typeof(UnderworldTerrainBiomePatch));
        _patched = true;
        log.LogInfo("Underworld terrain generation hooks installed for GetBiomeHeight/GetBiome.");
    }

    /// <summary>Main-thread capture of the active world. Never call this off the main thread.</summary>
    internal static void CaptureWorld(World? world)
    {
        var services = _services;
        _world = null;
        if (services is null || world is null) return;

        // The Underworld generates from the surface world's seed verbatim; the map differs because
        // the generation algorithm differs, not because the seed differs.
        // ZoneSystem does not exist yet this early in ZNet.Awake; 30m is Valheim's own default sea level.
        var waterLevel = ZoneSystem.instance is null ? 30f : ZoneSystem.instance.m_waterLevel;
        _world = new WorldSnapshot(world.m_seed, waterLevel);
        var domain = services.SpatialDomain;
        _log?.LogInfo(
            $"Underworld terrain shaping active for seed {world.m_seed} in the reserved region at " +
            $"({domain.HostCenterX:0}, {domain.HostCenterZ:0}), playable radius {domain.RadiusMeters:0}m.");
    }

    // Everything below runs on HeightmapBuilder's worker thread. Snapshot plus pure math only:
    // Mathf.PerlinNoise/Clamp01/Lerp are the same native math Valheim's own GetBiomeHeight calls
    // from this thread, but any UnityEngine.Object access here is a crash.
    internal static float ShapeHeight(float wx, float wy, float vanillaHeight)
    {
        var services = _services;
        var world = _world;
        if (services is null || world is null) return vanillaHeight;

        // Surface columns are never touched. Only columns inside the reserved region are Underworld.
        var domain = services.SpatialDomain;
        if (!UnderworldSpatialDomain.ContainsHostColumn(domain, wx, wy)) return vanillaHeight;

        var result = Evaluate(services, world, wx, wy, vanillaHeight);
        return result.Admitted ? (float)result.Height : vanillaHeight;
    }

    internal static UnderworldTerrainResult SampleTerrain(float wx, float wy, float vanillaHeight)
    {
        var services = _services;
        var world = _world;
        if (services is null || world is null) return default;
        if (!UnderworldSpatialDomain.ContainsHostColumn(services.SpatialDomain, wx, wy)) return default;
        return Evaluate(services, world, wx, wy, vanillaHeight);
    }

    private static UnderworldTerrainResult Evaluate(UnderworldRuntimeServices services, WorldSnapshot world,
        float wx, float wy, float vanillaHeight)
    {
        // The terrain rules are written around a region centred on the origin, so the world column is
        // translated into region-local coordinates before evaluation.
        var domain = services.SpatialDomain;
        var local = UnderworldSpatialDomain.ToLogicalColumn(domain, wx, wy);
        var seed = world.Seed;
        var noise = Mathf.PerlinNoise(
            ((float)local.X + seed * 0.0137f) * 0.00115f,
            ((float)local.Z - seed * 0.0091f) * 0.00115f);
        return UnderworldTerrainLifecycle.Evaluate(domain,
            new UnderworldTerrainSample(local.X, 0d, local.Z, world.WaterLevel, 0d, noise),
            seed);
    }

    internal static Heightmap.Biome SelectVanillaBiome(float wx, float wy, Heightmap.Biome vanillaBiome)
    {
        var services = _services;
        if (services is null || _world is null) return vanillaBiome;
        // Neutralising vanilla ecology applies only inside the region; the surface keeps its biomes.
        return UnderworldSpatialDomain.ContainsHostColumn(services.SpatialDomain, wx, wy)
            ? Heightmap.Biome.Meadows
            : vanillaBiome;
    }
}

// ZNet.Awake calls WorldGenerator.Initialize on the main thread when a world loads, before any
// terrain is requested, so this is where the active world can be read safely. It also fires from
// FejdStartup.Awake and ZNet.RPC_PeerInfo, which keeps the snapshot correct for the main menu and
// for joining a server.
[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.Initialize), new Type[] { typeof(World) })]
internal static class UnderworldTerrainWorldPatch
{
    private static void Postfix(World world) => UnderworldTerrainRuntime.CaptureWorld(world);
}

// Valheim 1.0.12 takes the biome mask out-parameter and two optional generation flags, so the
// three-argument signature this patch used to declare no longer resolves to any installed method.
// An attribute cannot hold a by-ref Type, so the out-parameter is declared through Harmony's
// parallel ArgumentType array.
[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GetBiomeHeight),
    new Type[] { typeof(Heightmap.Biome), typeof(float), typeof(float), typeof(Color), typeof(bool), typeof(bool) },
    new ArgumentType[]
    {
        ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal,
        ArgumentType.Out, ArgumentType.Normal, ArgumentType.Normal,
    })]
internal static class UnderworldTerrainHeightPatch
{
    private static void Postfix(float wx, float wy, ref float __result) =>
        __result = UnderworldTerrainRuntime.ShapeHeight(wx, wy, __result);
}

// 1.0.12 added the optional ocean-level and water-always-ocean arguments to the float overload;
// the two-argument form no longer identifies an installed method.
[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GetBiome),
    new Type[] { typeof(float), typeof(float), typeof(float), typeof(bool) })]
internal static class UnderworldTerrainBiomePatch
{
    private static void Postfix(float wx, float wy, ref Heightmap.Biome __result) =>
        __result = UnderworldTerrainRuntime.SelectVanillaBiome(wx, wy, __result);
}
