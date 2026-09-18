using System;

namespace Magenheim.Core.Underworld;

public enum UnderworldTerrainBiome
{
    FungalForest,
    BlackwaterDeep,
    SulfurousWastes,
    FrozenCaverns,
    FractureZones,
    GreatDecay,
}

public readonly record struct UnderworldTerrainSample(
    double X,
    double Y,
    double Z,
    double WaterLevel,
    double SlopeDegrees,
    double Noise01);

public readonly record struct UnderworldTerrainResult(
    bool Admitted,
    UnderworldTerrainBiome Biome,
    double Height,
    double WaterDepth,
    double Cover01,
    double Hazard01);

/// <summary>
/// Pure terrain lifecycle authority for the logical Underworld. Runtime world-generation
/// adapters provide Valheim's base height/noise samples; this policy decides whether the
/// sample belongs to the reserved Underworld, assigns one of the six canonical terrain
/// ecologies, and applies bounded terrain shaping without inventing physical ceiling geometry.
/// </summary>
public static class UnderworldTerrainLifecycle
{
    public const double MaximumTerrainDelta = 48d;

    /// <summary>
    /// The elevation the Underworld generates around, in world metres.
    /// </summary>
    /// <remarks>
    /// Terrain used to be produced as a bounded delta on the vanilla height of the same column. That
    /// only worked while the Underworld was imagined as replacing an ordinary world. The reserved
    /// region sits far beyond <c>waterEdge</c>, where vanilla returns edge-of-map deep ocean, so
    /// shaping relative to it generated an Underworld hundreds of metres under water -- the player
    /// arrived to find no terrain at all. The region now generates its own absolute elevation, which
    /// is what Valheim itself does for Deep North via <c>deepNorthYOffset</c>.
    ///
    /// 45m sits above the 30m water line by more than the Fungal Forest's downward delta, so the
    /// first biome is dry land, while Blackwater Deep (-18 to -34) becomes a genuine sea, Fracture
    /// Zones cut below the waterline and rise into walls, and the Great Decay reads as wetland.
    /// </remarks>
    public const double BaseElevationMeters = 45d;
    public const double MaximumSlopeDegrees = 75d;
    public const double CentralFungalRadiusFraction = 0.16d;
    public const double FungalTransitionRadiusFraction = 0.22d;

    public static UnderworldTerrainResult Evaluate(
        UnderworldSpatialDomainDefinition domain,
        UnderworldTerrainSample sample,
        int derivedSeed32)
    {
        UnderworldSpatialDomain.ValidateDefinition(domain);
        if (!Finite(sample.X) || !Finite(sample.Y) || !Finite(sample.Z) ||
            !Finite(sample.WaterLevel) || !Finite(sample.SlopeDegrees) || !Finite(sample.Noise01))
            return default;

        var radiusSquared = domain.RadiusMeters * domain.RadiusMeters;
        if (sample.X * sample.X + sample.Z * sample.Z > radiusSquared ||
            sample.Y < domain.LogicalMinY || sample.Y > domain.LogicalMaxY)
            return default;

        var slope = Clamp(sample.SlopeDegrees, 0d, MaximumSlopeDegrees);
        var noise = Clamp(sample.Noise01, 0d, 1d);
        var distance = Math.Sqrt(sample.X * sample.X + sample.Z * sample.Z);
        var biome = SelectBiome(sample.X, sample.Z, distance, domain.RadiusMeters, derivedSeed32);
        var delta = TerrainDelta(biome, noise, slope);

        // Blend the terrain shape around the protected Fungal Forest basin. This prevents a single
        // sample step across a biome province boundary from producing a cliff while keeping the
        // outer province identity intact.
        var fungalBlend = FungalTransition(distance, domain.RadiusMeters);
        if (fungalBlend > 0d && biome != UnderworldTerrainBiome.FungalForest)
            delta = Lerp(delta, TerrainDelta(UnderworldTerrainBiome.FungalForest, noise, slope), fungalBlend);

        // Water depth follows from the generated ground, so seas and wetlands are a consequence of
        // the terrain rather than an input copied from whatever vanilla had at this column.
        var height = BaseElevationMeters + Clamp(delta, -MaximumTerrainDelta, MaximumTerrainDelta);
        var water = Math.Max(0d, sample.WaterLevel - height);

        var cover = Cover(biome, noise, slope, water);
        var hazard = Hazard(biome, noise, water);
        if (fungalBlend > 0d && biome != UnderworldTerrainBiome.FungalForest)
        {
            cover = Lerp(cover, Cover(UnderworldTerrainBiome.FungalForest, noise, slope, water), fungalBlend);
            hazard = Lerp(hazard, 0d, fungalBlend);
        }

        return new UnderworldTerrainResult(
            true,
            biome,
            height,
            water,
            Clamp(cover, 0d, 1d),
            Clamp(hazard, 0d, 1d));
    }

    private static UnderworldTerrainBiome SelectBiome(double x, double z, double distance, double radius, int seed)
    {
        // Broad angular provinces keep each ecology legible while the seed rotates the map.
        // The center is always Fungal Forest so first admission cannot strand a player in a
        // lethal biome; outer terrain transitions through the remaining five provinces.
        if (distance <= radius * CentralFungalRadiusFraction) return UnderworldTerrainBiome.FungalForest;

        var angle = Math.Atan2(z, x) + SeedRotation(seed);
        if (angle < 0d) angle += Math.PI * 2d;
        if (angle >= Math.PI * 2d) angle -= Math.PI * 2d;
        var sector = (int)Math.Floor(angle / (Math.PI * 2d / 5d));
        return sector switch
        {
            0 => UnderworldTerrainBiome.BlackwaterDeep,
            1 => UnderworldTerrainBiome.SulfurousWastes,
            2 => UnderworldTerrainBiome.FrozenCaverns,
            3 => UnderworldTerrainBiome.FractureZones,
            _ => UnderworldTerrainBiome.GreatDecay,
        };
    }

    private static double FungalTransition(double distance, double radius)
    {
        var inner = radius * CentralFungalRadiusFraction;
        var outer = radius * FungalTransitionRadiusFraction;
        if (distance <= inner) return 1d;
        if (distance >= outer) return 0d;
        var t = (distance - inner) / (outer - inner);
        // Smoothstep keeps both ends derivative-continuous and avoids a visible terrain crease.
        t = t * t * (3d - 2d * t);
        return 1d - t;
    }

    // Mapping the raw seed linearly across the uint range made province rotation useless in
    // practice: ordinary small seeds all landed within ~1e-6 rad of zero, so every such world
    // received an identical province layout. Avalanche the seed first, using the same finalizer
    // as UnderworldTerrain.Hash, so neighbouring seeds rotate to unrelated angles.
    private static double SeedRotation(int seed) => Avalanche(unchecked((uint)seed)) / ((double)uint.MaxValue + 1d) * Math.PI * 2d;

    private static uint Avalanche(uint value)
    {
        unchecked
        {
            value ^= value >> 16; value *= 0x7feb352du;
            value ^= value >> 15; value *= 0x846ca68bu;
            value ^= value >> 16;
            return value;
        }
    }

    private static double TerrainDelta(UnderworldTerrainBiome biome, double noise, double slope)
    {
        var centered = noise * 2d - 1d;
        return biome switch
        {
            UnderworldTerrainBiome.FungalForest => centered * 10d - slope * 0.04d,
            UnderworldTerrainBiome.BlackwaterDeep => -18d - noise * 16d,
            UnderworldTerrainBiome.SulfurousWastes => centered * 14d + noise * noise * 12d,
            UnderworldTerrainBiome.FrozenCaverns => centered * 8d - slope * 0.02d,
            UnderworldTerrainBiome.FractureZones => centered * 34d + (noise > 0.72d ? 10d : -6d),
            UnderworldTerrainBiome.GreatDecay => -6d + centered * 12d - noise * 5d,
            _ => 0d,
        };
    }

    private static double Cover(UnderworldTerrainBiome biome, double noise, double slope, double waterDepth) => biome switch
    {
        UnderworldTerrainBiome.FungalForest => waterDepth <= 0.5d && slope <= 32d ? 0.55d + noise * 0.45d : 0.15d,
        UnderworldTerrainBiome.BlackwaterDeep => waterDepth <= 1d ? 0.2d + noise * 0.25d : 0.05d,
        UnderworldTerrainBiome.SulfurousWastes => slope <= 38d ? 0.08d + noise * 0.12d : 0.03d,
        UnderworldTerrainBiome.FrozenCaverns => slope <= 42d ? 0.18d + noise * 0.22d : 0.08d,
        UnderworldTerrainBiome.FractureZones => slope <= 55d ? 0.1d + noise * 0.2d : 0.04d,
        UnderworldTerrainBiome.GreatDecay => waterDepth <= 0.5d ? 0.4d + noise * 0.45d : 0.18d,
        _ => 0d,
    };

    private static double Hazard(UnderworldTerrainBiome biome, double noise, double waterDepth) => biome switch
    {
        UnderworldTerrainBiome.FungalForest => 0d,
        UnderworldTerrainBiome.BlackwaterDeep => Clamp(0.2d + Math.Max(0d, waterDepth) * 0.08d, 0d, 0.8d),
        UnderworldTerrainBiome.SulfurousWastes => 0.35d + noise * 0.65d,
        UnderworldTerrainBiome.FrozenCaverns => 0.25d + noise * 0.55d,
        UnderworldTerrainBiome.FractureZones => 0.2d + Math.Abs(noise - 0.5d) * 1.2d,
        UnderworldTerrainBiome.GreatDecay => 0.3d + noise * 0.6d,
        _ => 0d,
    };

    private static double Lerp(double from, double to, double amount) => from + (to - from) * Clamp(amount, 0d, 1d);
    private static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(max, value));
    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
