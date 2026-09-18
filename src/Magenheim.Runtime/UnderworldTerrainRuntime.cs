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
    /// Immutable per-world snapshot of an admitted derived Underworld session.
    /// </summary>
    /// <remarks>
    /// Valheim builds heightmaps on <c>HeightmapBuilder</c>'s worker thread, and that thread is what
    /// calls <c>WorldGenerator.GetBiomeHeight</c>/<c>GetBiome</c>. Reading <c>ZNet.instance</c>,
    /// <c>ZNet.World</c> or <c>ZoneSystem.instance</c> from there is a main-thread-only Unity call
    /// and takes the whole player down with a native crash during world generation, with nothing
    /// written to the log after <c>ZNet.LoadWorld</c>. So the session is resolved once on the main
    /// thread and the generation hooks read only this snapshot plus pure math. A null snapshot means
    /// "not an admitted Underworld session", which is also the correct fail-safe for every Surface
    /// world. Capturing it once also keeps shaping deterministic across chunks.
    /// </remarks>
    private sealed record TerrainSession(UnderworldWorldIdentity Identity, double WaterLevel);

    private static volatile TerrainSession? _session;

    internal static void Configure(UnderworldRuntimeServices services, ManualLogSource log)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _session = null;
        if (_patched) return;
        var harmony = new Harmony(MagenheimPlugin.PluginGuid + ".gameplay");
        harmony.PatchAll(typeof(UnderworldTerrainSessionPatch));
        harmony.PatchAll(typeof(UnderworldTerrainHeightPatch));
        harmony.PatchAll(typeof(UnderworldTerrainBiomePatch));
        _patched = true;
        log.LogInfo("Underworld terrain generation hooks installed for GetBiomeHeight/GetBiome.");
    }

    /// <summary>Main-thread capture of the active world's Underworld admission. Never call this off the main thread.</summary>
    internal static void CaptureSession(object? world)
    {
        var services = _services;
        _session = null;
        if (services is null || world is null) return;
        // Admission is by derived save name, so this needs neither ZNet nor the world's seed fields.
        var saveName = UnderworldRuntimeIdentityResolver.ResolveWorldSaveName(world);
        if (string.IsNullOrWhiteSpace(saveName) ||
            !services.WorldPairStore.TryResolveByDerivedSaveName(saveName, out var identity) || identity is null) return;

        // ZoneSystem does not exist yet this early in ZNet.Awake; 30m is Valheim's own default sea level.
        var waterLevel = ZoneSystem.instance is null ? 30f : ZoneSystem.instance.m_waterLevel;
        _session = new TerrainSession(identity, waterLevel);
        var prefix = identity.DerivedSeedFingerprint.Substring(0, Math.Min(12, identity.DerivedSeedFingerprint.Length));
        _log?.LogInfo($"Underworld terrain shaping active for derived world {prefix}; playable radius {services.SpatialDomain.RadiusMeters:0}m.");
    }

    // Everything below runs on HeightmapBuilder's worker thread. Snapshot plus pure math only:
    // Mathf.PerlinNoise/Clamp01/Lerp are the same native math Valheim's own GetBiomeHeight calls
    // from this thread, but any UnityEngine.Object access here is a crash.
    internal static float ShapeHeight(float wx, float wy, float vanillaHeight)
    {
        var services = _services;
        var session = _session;
        if (services is null || session is null) return vanillaHeight;

        var domain = services.SpatialDomain;
        var radial = Math.Sqrt((double)wx * wx + (double)wy * wy);
        if (radial > domain.RadiusMeters)
        {
            var edge = Mathf.Clamp01((float)((radial - domain.RadiusMeters) / 600d));
            return Mathf.Lerp(vanillaHeight, -120f, edge);
        }

        var result = Evaluate(services, session, wx, wy, vanillaHeight);
        return result.Admitted ? (float)result.Height : vanillaHeight;
    }

    internal static UnderworldTerrainResult SampleTerrain(float wx, float wy, float vanillaHeight)
    {
        var services = _services;
        var session = _session;
        if (services is null || session is null) return default;
        return Evaluate(services, session, wx, wy, vanillaHeight);
    }

    private static UnderworldTerrainResult Evaluate(UnderworldRuntimeServices services, TerrainSession session,
        float wx, float wy, float vanillaHeight)
    {
        var seed = session.Identity.DerivedSeed32;
        var noise = Mathf.PerlinNoise((wx + seed * 0.0137f) * 0.00115f, (wy - seed * 0.0091f) * 0.00115f);
        return UnderworldTerrainLifecycle.Evaluate(services.SpatialDomain,
            new UnderworldTerrainSample(wx, 0d, wy, vanillaHeight, 0d,
                Math.Max(0d, session.WaterLevel - vanillaHeight), noise),
            seed);
    }

    internal static Heightmap.Biome SelectVanillaBiome(float wx, float wy, Heightmap.Biome vanillaBiome) =>
        _session is null ? vanillaBiome : Heightmap.Biome.Meadows;
}

// ZNet.Awake calls WorldGenerator.Initialize on the main thread when a world loads, before any
// terrain is requested, so this is where the Underworld session can be resolved safely. It also
// fires from FejdStartup.Awake and ZNet.RPC_PeerInfo, which correctly clears the snapshot for the
// main menu and for any world that is not the admitted derived save.
[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.Initialize), new Type[] { typeof(World) })]
internal static class UnderworldTerrainSessionPatch
{
    private static void Postfix(World world) => UnderworldTerrainRuntime.CaptureSession(world);
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
