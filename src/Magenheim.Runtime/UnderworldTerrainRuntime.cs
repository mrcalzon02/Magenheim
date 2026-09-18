using System;
using System.Diagnostics;
using System.Threading;
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
        // Validate the spatial domain exactly once here. The per-column helpers deliberately skip it
        // because it recomputes a SHA-256 fingerprint.
        UnderworldSpatialDomain.ValidateDefinition(services.SpatialDomain);
        _world = null;
        if (_patched) return;
        var harmony = new Harmony(MagenheimPlugin.PluginGuid + ".gameplay");
        harmony.PatchAll(typeof(UnderworldTerrainWorldPatch));
        harmony.PatchAll(typeof(UnderworldBiomeSectorPatch));
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
        // Fractal lattice noise in metres. Mathf.PerlinNoise with the seed folded into the
        // coordinate put every column at the same float-rounded input and generated a flat plane.
        var noise = UnderworldTerrainNoise.Fractal01(seed, local.X, local.Z);
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

    /// <summary>
    /// The biome sector for a map-space cell, which is the answer almost everything except terrain
    /// height actually consumes.
    /// </summary>
    /// <remarks>
    /// Overriding <see cref="SelectVanillaBiome"/> alone made the game disagree with itself. The
    /// sector out at the reserved region is Ocean, so <c>Player.UpdateBiome</c> logged
    /// "GetBiome error Ocean -> Meadows" every tick, <c>SpawnSystem.UpdateSpawnList</c> threw a
    /// NullReferenceException every tick, ocean fish spawned on dry Underworld ground, and the sky,
    /// weather and ground texture all stayed Ashlands, because
    /// <c>EnvMan.GetEnvironmentOverride</c> and <c>Heightmap.RebuildRenderMesh</c> read the sector
    /// rather than the biome.
    ///
    /// Every caller reaches the sector through the (int, int, bool) overload, so this is the single
    /// place to answer. <c>BiomeSector.EmptyMeadows</c> is vanilla's own shared instance, built as
    /// <c>new BiomeSector(null, Meadows)</c> and already returned by this same method for cells it
    /// cannot resolve, so handing it back is a path every consumer already handles. It also now
    /// agrees with what <see cref="SelectVanillaBiome"/> reports.
    ///
    /// This runs on HeightmapBuilder's worker thread as well as the main thread, so it stays on
    /// static maths and a static field read.
    /// </remarks>
    private static long _nextSectorDiagnosticTimestamp;

    internal static BiomeSector SelectBiomeSector(int gridX, int gridY, BiomeSector vanillaSector)
    {
        var services = _services;
        if (services is null || _world is null) return vanillaSector;
        var wx = AltBiomeWorldData.MapSpaceToWorldSpace((float)gridX);
        var wz = AltBiomeWorldData.MapSpaceToWorldSpace((float)gridY);
        var inside = UnderworldSpatialDomain.ContainsHostColumn(services.SpatialDomain, wx, wz);

        // Temporary instrumentation. GetBiomeSector is also reached from HeightmapBuilder's
        // worker thread, so this probe must not touch UnityEngine.Time (or any UnityEngine.Object).
        // Stopwatch is process-monotonic managed state and Interlocked makes the throttle safe when
        // main-thread and worker-thread callers race through this method.
        if (inside && TryAcquireSectorDiagnosticWindow())
        {
            _log?.LogWarning(
                $"[sector probe] grid=({gridX},{gridY}) world=({wx:0.0},{wz:0.0}) inside={inside} " +
                $"vanilla={(vanillaSector is null ? "<null>" : vanillaSector.Biome.ToString())} " +
                $"vanillaType={(vanillaSector?.BiomeType is null ? "<null>" : vanillaSector.BiomeType.Biome.ToString())} " +
                $"meadows={(BiomeSector.EmptyMeadows is null ? "<null>" : BiomeSector.EmptyMeadows.Biome.ToString())} " +
                $"meadowsType={(BiomeSector.EmptyMeadows?.BiomeType is null ? "<null>" : BiomeSector.EmptyMeadows.BiomeType.Biome.ToString())}");
        }

        // Fall back to vanilla rather than ever handing a consumer a null sector. The
        // null-forgiveness is honest: outside the region this hands back exactly what vanilla
        // returned, whatever that was.
        return (inside ? BiomeSector.EmptyMeadows ?? vanillaSector : vanillaSector)!;
    }

    private static bool TryAcquireSectorDiagnosticWindow()
    {
        var now = Stopwatch.GetTimestamp();
        var interval = Stopwatch.Frequency * 5L;
        while (true)
        {
            var next = Interlocked.Read(ref _nextSectorDiagnosticTimestamp);
            if (now < next) return false;
            if (Interlocked.CompareExchange(ref _nextSectorDiagnosticTimestamp, now + interval, next) == next)
                return true;
        }
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

// Every biome-sector caller -- Player, Minimap, EnvMan, SpawnSystem, SpawnArea, ZoneSystem
// vegetation placement, HeightmapBuilder and Heightmap's render mesh -- reaches the sector through
// this (int, int, bool) overload, so patching it alone keeps the whole game consistent about what
// the reserved region is.
[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GetBiomeSector),
    new Type[] { typeof(int), typeof(int), typeof(bool) })]
internal static class UnderworldBiomeSectorPatch
{
    private static void Postfix(int gridx, int gridy, ref BiomeSector __result) =>
        __result = UnderworldTerrainRuntime.SelectBiomeSector(gridx, gridy, __result);
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
