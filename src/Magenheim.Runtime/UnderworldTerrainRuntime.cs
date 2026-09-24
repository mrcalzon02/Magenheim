using System;
using BepInEx.Logging;
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

    internal static void Configure(UnderworldRuntimeServices services, ManualLogSource log)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _ = services.TerrainDomain ?? throw new InvalidOperationException("Native Underworld terrain domain is unavailable.");
        log.LogInfo("Native Underworld terrain authority configured without Surface WorldGenerator hooks.");
    }

    internal static bool ContainsInstancePosition(double x, double y, double z) =>
        _services is not null && _services.InstanceLifecycle.Identity is not null &&
        InstanceAvailable(_services) && _services.TerrainDomain.Contains(x, y, z);

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

    private static bool InstanceAvailable(UnderworldRuntimeServices services)
    {
        var phase = services.InstanceLifecycle.Phase;
        return phase == UnderworldInstancePhase.Admitting || phase == UnderworldInstancePhase.Active;
    }
}
