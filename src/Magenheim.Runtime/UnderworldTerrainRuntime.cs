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
    private static string _lastSessionFingerprint = string.Empty;
    private static bool _patched;

    internal static void Configure(UnderworldRuntimeServices services, ManualLogSource log)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _lastSessionFingerprint = string.Empty;
        if (_patched) return;
        var harmony = new Harmony(MagenheimPlugin.PluginGuid + ".gameplay");
        harmony.PatchAll(typeof(UnderworldTerrainHeightPatch));
        harmony.PatchAll(typeof(UnderworldTerrainBiomePatch));
        _patched = true;
        log.LogInfo("Underworld terrain generation hooks installed for GetBiomeHeight/GetBiome.");
    }

    internal static float ShapeHeight(float wx, float wy, float vanillaHeight)
    {
        var services = _services;
        if (services is null || ZNet.instance is null || ZNet.World is null) return vanillaHeight;
        if (!UnderworldRuntimeIdentityResolver.TryResolveWorldSession(services.WorldPairStore, ZNet.instance, ZNet.World,
                out var identity, out var layer, out _) || identity is null || layer != UnderworldLayer.Underworld)
            return vanillaHeight;

        var domain = services.SpatialDomain;
        var radial = Math.Sqrt((double)wx * wx + (double)wy * wy);
        if (radial > domain.RadiusMeters)
        {
            var edge = Mathf.Clamp01((float)((radial - domain.RadiusMeters) / 600d));
            return Mathf.Lerp(vanillaHeight, -120f, edge);
        }

        var noise = Mathf.PerlinNoise((wx + identity.DerivedSeed32 * 0.0137f) * 0.00115f,
            (wy - identity.DerivedSeed32 * 0.0091f) * 0.00115f);
        var waterLevel = ZoneSystem.m_instance is null ? 30f : ZoneSystem.m_instance.m_waterLevel;
        var sample = new UnderworldTerrainSample(wx, 0d, wy, vanillaHeight, 0d,
            Math.Max(0d, waterLevel - vanillaHeight), noise);
        var result = UnderworldTerrainLifecycle.Evaluate(domain, sample, identity.DerivedSeed32);
        if (!result.Admitted) return vanillaHeight;

        if (!string.Equals(_lastSessionFingerprint, identity.DerivedSeedFingerprint, StringComparison.Ordinal))
        {
            _lastSessionFingerprint = identity.DerivedSeedFingerprint;
            var prefix = identity.DerivedSeedFingerprint.Substring(0, Math.Min(12, identity.DerivedSeedFingerprint.Length));
            _log?.LogInfo($"Underworld terrain shaping active for derived world {prefix}; playable radius {domain.RadiusMeters:0}m.");
        }
        return (float)result.Height;
    }

    internal static Heightmap.Biome SelectVanillaBiome(float wx, float wy, Heightmap.Biome vanillaBiome)
    {
        var services = _services;
        if (services is null || ZNet.instance is null || ZNet.World is null) return vanillaBiome;
        if (!UnderworldRuntimeIdentityResolver.TryResolveWorldSession(services.WorldPairStore, ZNet.instance, ZNet.World,
                out var identity, out var layer, out _) || identity is null || layer != UnderworldLayer.Underworld)
            return vanillaBiome;
        return Heightmap.Biome.Meadows;
    }
}

[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GetBiomeHeight),
    new Type[] { typeof(Heightmap.Biome), typeof(float), typeof(float) })]
internal static class UnderworldTerrainHeightPatch
{
    private static void Postfix(float wx, float wy, ref float __result) =>
        __result = UnderworldTerrainRuntime.ShapeHeight(wx, wy, __result);
}

[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GetBiome),
    new Type[] { typeof(float), typeof(float) })]
internal static class UnderworldTerrainBiomePatch
{
    private static void Postfix(float wx, float wy, ref Heightmap.Biome __result) =>
        __result = UnderworldTerrainRuntime.SelectVanillaBiome(wx, wy, __result);
}
