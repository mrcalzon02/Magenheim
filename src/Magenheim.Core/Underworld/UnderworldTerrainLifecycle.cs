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
/// Pure terrain authority for the dedicated Underworld instance. Every coordinate accepted here is
/// native to that instance. Surface host coordinates cannot participate in terrain admission,
/// biome selection, relief, water, cover, or hazard decisions.
/// </summary>
public static class UnderworldTerrainLifecycle
{
    public const double MaximumTerrainDelta = 300d;
    public const double RegionalReliefRadiusFraction = 0.42d;
    public const double BaseElevationMeters = 45d;
    public const double MaximumSlopeDegrees = 75d;
    public const double CentralFungalRadiusFraction = 0.16d;
    public const double FungalTransitionRadiusFraction = 0.22d;

    public static UnderworldTerrainResult Evaluate(
        UnderworldInstanceTerrainDomain domain,
        UnderworldTerrainSample sample,
        int derivedSeed32)
    {
        if (domain is null) throw new ArgumentNullException(nameof(domain));
        if (!Finite(sample.X) || !Finite(sample.Y) || !Finite(sample.Z) ||
            !Finite(sample.WaterLevel) || !Finite(sample.SlopeDegrees) || !Finite(sample.Noise01))
            return default;
        if (!domain.Contains(sample.X, sample.Y, sample.Z)) return default;

        var slope = Clamp(sample.SlopeDegrees, 0d, MaximumSlopeDegrees);
        var noise = Clamp(sample.Noise01, 0d, 1d);
        var distance = Math.Sqrt(sample.X * sample.X + sample.Z * sample.Z);
        var biome = SelectBiome(sample.X, sample.Z, distance, domain.RadiusMeters, derivedSeed32);
        var relief = UnderworldTerrainNoise.ReliefMetres(derivedSeed32, sample.X, sample.Z,
            RegionalReliefWeight(distance, domain.RadiusMeters));
        var delta = BiomeDelta(biome, relief);
        var fungalBlend = FungalTransition(distance, domain.RadiusMeters);
        if (fungalBlend > 0d && biome != UnderworldTerrainBiome.FungalForest)
            delta = Lerp(delta, BiomeDelta(UnderworldTerrainBiome.FungalForest, relief), fungalBlend);

        var height = BaseElevationMeters + Clamp(delta, -MaximumTerrainDelta, MaximumTerrainDelta);
        var water = Math.Max(0d, sample.WaterLevel - height);
        var cover = Cover(biome, noise, slope, water);
        var hazard = Hazard(biome, noise, water);
        if (fungalBlend > 0d && biome != UnderworldTerrainBiome.FungalForest)
        {
            cover = Lerp(cover, Cover(UnderworldTerrainBiome.FungalForest, noise, slope, water), fungalBlend);
            hazard = Lerp(hazard, 0d, fungalBlend);
        }

        return new UnderworldTerrainResult(true, biome, height, water,
            Clamp(cover, 0d, 1d), Clamp(hazard, 0d, 1d));
    }

    /// <summary>
    /// Temporary compatibility bridge for callers not yet migrated from the obsolete host-domain
    /// record. Host coordinates are intentionally discarded before entering terrain authority.
    /// </summary>
    [Obsolete("Use the native UnderworldInstanceTerrainDomain overload. Host placement is not terrain authority.")]
    public static UnderworldTerrainResult Evaluate(
        UnderworldSpatialDomainDefinition legacyDomain,
        UnderworldTerrainSample sample,
        int derivedSeed32)
    {
        if (legacyDomain is null) throw new ArgumentNullException(nameof(legacyDomain));
        var instanceDomain = UnderworldInstanceTerrainDomain.ValidateAndFreeze(
            legacyDomain.RadiusMeters, legacyDomain.LogicalMinY, legacyDomain.LogicalMaxY);
        return Evaluate(instanceDomain, sample, derivedSeed32);
    }

    private static UnderworldTerrainBiome SelectBiome(double x, double z, double distance, double radius, int seed)
    {
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

    private static double RegionalReliefWeight(double distance, double radius)
    {
        var inner = radius * CentralFungalRadiusFraction;
        var outer = radius * RegionalReliefRadiusFraction;
        if (distance <= inner) return 0d;
        if (distance >= outer) return 1d;
        var t = (distance - inner) / (outer - inner);
        return t * t * (3d - 2d * t);
    }

    private static double FungalTransition(double distance, double radius)
    {
        var inner = radius * CentralFungalRadiusFraction;
        var outer = radius * FungalTransitionRadiusFraction;
        if (distance <= inner) return 1d;
        if (distance >= outer) return 0d;
        var t = (distance - inner) / (outer - inner);
        t = t * t * (3d - 2d * t);
        return 1d - t;
    }

    private static double SeedRotation(int seed) =>
        UnderworldTerrainNoise.Mix(unchecked((uint)seed)) / ((double)uint.MaxValue + 1d) * Math.PI * 2d;

    private static double BiomeDelta(UnderworldTerrainBiome biome, double relief) => biome switch
    {
        UnderworldTerrainBiome.FungalForest => 6d + relief * 0.55d,
        UnderworldTerrainBiome.BlackwaterDeep => -30d + relief * 0.40d,
        UnderworldTerrainBiome.SulfurousWastes => 8d + relief * 0.85d,
        UnderworldTerrainBiome.FrozenCaverns => 4d + relief * 0.65d,
        UnderworldTerrainBiome.FractureZones => relief * 1.5d,
        UnderworldTerrainBiome.GreatDecay => -10d + relief * 0.45d,
        _ => 0d,
    };

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
