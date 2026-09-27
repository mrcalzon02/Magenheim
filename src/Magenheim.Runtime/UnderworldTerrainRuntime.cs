using System;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>
/// Runtime bridge into deterministic Underworld terrain authority.
/// Terrain is sampled only in native instance coordinates. This type deliberately does not patch
/// or query Surface WorldGenerator terrain: instance materialization belongs to the instance-chunk adapter.
/// </summary>
internal static class UnderworldTerrainRuntime
{
    private static UnderworldRuntimeServices? _services;
    private static ManualLogSource? _log;
    internal const Heightmap.Biome FungalForestBiome = (Heightmap.Biome)1024;
    internal const Heightmap.Biome BlackwaterDeepBiome = (Heightmap.Biome)2048;
    internal const Heightmap.Biome SulfurousWastesBiome = (Heightmap.Biome)4096;
    internal const Heightmap.Biome FrozenCavernsBiome = (Heightmap.Biome)8192;
    internal const Heightmap.Biome FractureZonesBiome = (Heightmap.Biome)16384;
    internal const Heightmap.Biome GreatDecayBiome = (Heightmap.Biome)32768;

    internal static Heightmap.Biome ToNativeBiome(UnderworldTerrainBiome biome) => biome switch
    {
        UnderworldTerrainBiome.FungalForest => FungalForestBiome,
        UnderworldTerrainBiome.BlackwaterDeep => BlackwaterDeepBiome,
        UnderworldTerrainBiome.SulfurousWastes => SulfurousWastesBiome,
        UnderworldTerrainBiome.FrozenCaverns => FrozenCavernsBiome,
        UnderworldTerrainBiome.FractureZones => FractureZonesBiome,
        UnderworldTerrainBiome.GreatDecay => GreatDecayBiome,
        _ => throw new ArgumentOutOfRangeException(nameof(biome), biome, null),
    };

    internal static bool IsUnderworldBiome(Heightmap.Biome biome) =>
        biome == FungalForestBiome ||
        biome == BlackwaterDeepBiome ||
        biome == SulfurousWastesBiome ||
        biome == FrozenCavernsBiome ||
        biome == FractureZonesBiome ||
        biome == GreatDecayBiome;

    internal static void Configure(UnderworldRuntimeServices services, ManualLogSource log)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _ = services.TerrainDomain ?? throw new InvalidOperationException("Native Underworld terrain domain is unavailable.");
        log.LogInfo("Native Underworld terrain authority configured for instance-scoped WorldGenerator hooks; Surface worldgen remains untouched.");
    }

    internal static bool ContainsInstancePosition(double x, double y, double z) =>
        _services is not null && _services.InstanceLifecycle.Identity is not null &&
        InstanceAvailable(_services) && _services.TerrainDomain.Contains(x, y, z);

    internal static bool TryGetAdmittedIdentity(out UnderworldWorldIdentity? identity)
    {
        var services = _services;
        identity = services?.InstanceLifecycle.Identity;
        if (services is null || identity is null || !InstanceAvailable(services))
        {
            identity = null;
            return false;
        }
        return true;
    }

    internal static bool TryGetCapturedSeed(out int seed)
    {
        var identity = _services?.InstanceLifecycle.Identity;
        if (identity is not null)
        {
            seed = identity.DerivedSeed32;
            return true;
        }

        seed = default;
        return false;
    }

    /// <summary>Samples deterministic terrain directly in native Underworld instance coordinates.</summary>
    internal static UnderworldTerrainResult SampleInstanceTerrain(double x, double y, double z, double slopeDegrees = 0d)
    {
        var services = _services;
        var identity = services?.InstanceLifecycle.Identity;
        if (services is null || identity is null || !InstanceAvailable(services)) return default;

        var seed = identity.DerivedSeed32;
        var noise = UnderworldTerrainNoise.Fractal01(seed, x, z);
        var waterLevel = ZoneSystem.instance is null ? 30d : ZoneSystem.instance.m_waterLevel;
        return UnderworldTerrainLifecycle.Evaluate(
            services.TerrainDomain,
            new UnderworldTerrainSample(x, y, z, waterLevel, slopeDegrees, noise),
            seed);
    }

    /// <summary>
    /// Resolves a stable unique-location identity through the active native Underworld instance.
    /// This is a thin runtime bridge to the existing anchor authority; it does not persist coordinates.
    /// </summary>
    internal static UnderworldUniqueLocationAnchor ResolveUniqueLocation(string locationId, UnderworldTerrainBiome biome)
    {
        var services = _services;
        if (services is null || services.InstanceLifecycle.Identity is null || !InstanceAvailable(services))
            throw new InvalidOperationException("Cannot resolve an Underworld unique location without an active native instance.");
        return services.UniqueLocationAnchors.Resolve(locationId, biome);
    }

    internal static bool TrySampleNativeGenerator(WorldGenerator generator, double x, double z, out UnderworldTerrainResult terrain)
    {
        terrain = default;
        var services = _services;
        if (services is null || !InstanceAvailable(services) ||
            !services.WorldInstances.TryGetContext(UnderworldWorldInstanceId.Underworld, out var context) ||
            context is null || !ReferenceEquals(context.WorldGenerator, generator))
            return false;

        // The generator object is the instance discriminator. Do not require AsyncLocal execution
        // state here: HeightmapBuilder/worldgen work may resume on worker threads where the caller's
        // ExecutionContext is not guaranteed to flow, while the dedicated WorldGenerator identity is.
        terrain = SampleInstanceTerrain(x, 0d, z);
        return terrain.Admitted;
    }

    private static bool InstanceAvailable(UnderworldRuntimeServices services)
    {
        var phase = services.InstanceLifecycle.Phase;
        return phase == UnderworldInstancePhase.Admitting || phase == UnderworldInstancePhase.Active;
    }
}

[HarmonyPatch(
    typeof(WorldGenerator),
    nameof(WorldGenerator.GetBiome),
    new[] { typeof(float), typeof(float), typeof(float), typeof(bool) })]
internal static class UnderworldNativeWorldGeneratorBiomePatch
{
    private static bool Prefix(WorldGenerator __instance, float __0, float __1, ref Heightmap.Biome __result)
    {
        if (!UnderworldTerrainRuntime.TrySampleNativeGenerator(__instance, __0, __1, out var terrain)) return true;
        __result = UnderworldTerrainRuntime.ToNativeBiome(terrain.Biome);
        return false;
    }
}

[HarmonyPatch(
    typeof(WorldGenerator),
    nameof(WorldGenerator.GetBiomeHeight),
    new[]
    {
        typeof(Heightmap.Biome),
        typeof(float),
        typeof(float),
        typeof(Color),
        typeof(bool),
        typeof(bool),
    },
    new[]
    {
        ArgumentType.Normal,
        ArgumentType.Normal,
        ArgumentType.Normal,
        ArgumentType.Out,
        ArgumentType.Normal,
        ArgumentType.Normal,
    })]
internal static class UnderworldNativeWorldGeneratorHeightPatch
{
    private static bool Prefix(WorldGenerator __instance, float __1, float __2, ref Color __3, ref float __result)
    {
        if (!UnderworldTerrainRuntime.TrySampleNativeGenerator(__instance, __1, __2, out var terrain)) return true;
        __3 = Color.clear;
        __result = (float)terrain.Height;
        return false;
    }
}
