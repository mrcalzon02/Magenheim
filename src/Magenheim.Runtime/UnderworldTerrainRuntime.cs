using System;
using BepInEx.Logging;
using HarmonyLib;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Runtime bridge between Valheim's ordinary height generation and Magenheim's deterministic
/// Underworld terrain policy. It is deliberately inert in Surface worlds and menu generation.
/// The derived Underworld remains a normal Valheim world/session; only its terrain samples are
/// reshaped, so zone streaming, networking, saves and Heightmap lifecycle remain engine-owned.
/// </summary>
internal static class UnderworldTerrainRuntime
{
    private static UnderworldRuntimeServices? _services;
    private static ManualLogSource? _log;
    private static string _lastSessionFingerprint = string.Empty;

    internal static void Configure(UnderworldRuntimeServices services, ManualLogSource log)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _lastSessionFingerprint = string.Empty;
    }

    internal static float ShapeHeight(float wx, float wy, float vanillaHeight)
    {
        var services = _services;
        if (services is null || ZNet.instance is null || ZNet.World is null)
            return vanillaHeight;

        if (!UnderworldRuntimeIdentityResolver.TryResolveWorldSession(
                services.WorldPairStore, ZNet.instance, ZNet.World,
                out var identity, out var layer, out _) ||
            identity is null || layer != UnderworldLayer.Underworld)
            return vanillaHeight;

        var domain = services.SpatialDomain;
        var radial = Math.Sqrt((double)wx * wx + (double)wy * wy);
        if (radial > domain.RadiusMeters)
        {
            // The derived world must not leak ordinary surface terrain beyond the playable
            // Underworld. Sink the exterior beneath Valheim's water plane to create a natural
            // hard traversal boundary while retaining the engine's own world-edge lifecycle.
            var edge = Mathf.Clamp01((float)((radial - domain.RadiusMeters) / 600d));
            return Mathf.Lerp(vanillaHeight, -120f, edge);
        }

        var noise = Mathf.PerlinNoise(
            (wx + identity.DerivedSeed32 * 0.0137f) * 0.00115f,
            (wy - identity.DerivedSeed32 * 0.0091f) * 0.00115f);
        var waterLevel = ZoneSystem.m_instance is null ? 30f : ZoneSystem.m_instance.m_waterLevel;
        var waterDepth = Math.Max(0d, waterLevel - vanillaHeight);
        var sample = new UnderworldTerrainSample(
            wx, 0d, wy, vanillaHeight, 0d, waterDepth, noise);
        var result = UnderworldTerrainLifecycle.Evaluate(domain, sample, identity.DerivedSeed32);
        if (!result.Admitted)
            return vanillaHeight;

        if (!string.Equals(_lastSessionFingerprint, identity.DerivedSeedFingerprint, StringComparison.Ordinal))
        {
            _lastSessionFingerprint = identity.DerivedSeedFingerprint;
            _log?.LogInfo($"Underworld terrain shaping active for derived world {identity.DerivedSeedFingerprint[..Math.Min(12, identity.DerivedSeedFingerprint.Length)]}; playable radius {domain.RadiusMeters:0}m.");
        }

        return (float)result.Height;
    }

    internal static Heightmap.Biome SelectVanillaBiome(float wx, float wy, Heightmap.Biome vanillaBiome)
    {
        var services = _services;
        if (services is null || ZNet.instance is null || ZNet.World is null)
            return vanillaBiome;
        if (!UnderworldRuntimeIdentityResolver.TryResolveWorldSession(
                services.WorldPairStore, ZNet.instance, ZNet.World,
                out var identity, out var layer, out _) || identity is null || layer != UnderworldLayer.Underworld)
            return vanillaBiome;

        // Suppress surface-biome ecology in the derived world. Magenheim's own terrain/flora
        // authorities classify the six Underworld ecologies; Meadows is only the neutral Valheim
        // compatibility substrate required by engine systems that demand a built-in biome flag.
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
