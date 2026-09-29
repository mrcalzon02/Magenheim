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
    public const string BiomeLayoutAlgorithmId = "biome-layout-v1-warped-multifield";
    public const double ArrivalProtectionRadiusMeters = 80d;
    public const double FullRegionalReliefRadiusMeters = 320d;
    public const double BaseElevationMeters = 45d;
    public const double MaximumSlopeDegrees = 75d;

    // One inner safe core is guaranteed Fungal. Beyond it, Fungal is only a broad bias in the
    // same seeded field as every other biome, so the arrival country has an organic edge.
    public const double GuaranteedFungalRadiusFraction = 0.10d;
    public const double CentralFungalRadiusFraction = 0.16d;
    public const double FungalTransitionRadiusFraction = 0.24d;

    public const double BiomeRegionFeatureMeters = 2800d;
    public const double BiomeBroadFeatureMeters = 6200d;
    public const double BiomeWarpFeatureMeters = 4800d;
    public const double BiomeWarpAmplitudeMeters = 850d;
    public const double BiomeBlendScoreWidth = 0.08d;

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
        var selection = SelectBiome(sample.X, sample.Z, distance, domain.RadiusMeters, derivedSeed32);
        var biome = selection.Primary;
        var regionalWeight = RegionalReliefWeight(distance, domain.RadiusMeters);
        double Delta(UnderworldTerrainBiome selected) => UnderworldBiomeTerrain.HeightDelta(
            selected, derivedSeed32, sample.X, sample.Z, regionalWeight);
        var delta = Delta(biome);

        // Ecology/weather ownership remains discrete, but near-tied biome fields blend their
        // terrain profiles so organic borders do not create artificial height steps.
        if (selection.Secondary != biome && selection.ScoreMargin < BiomeBlendScoreWidth)
        {
            var t = Clamp(selection.ScoreMargin / BiomeBlendScoreWidth, 0d, 1d);
            t = t * t * (3d - 2d * t);
            delta = Lerp(delta, Delta(selection.Secondary), .5d * (1d - t));
        }
        var fungalBlend = FungalTransition(distance, domain.RadiusMeters);
        if (fungalBlend > 0d && biome != UnderworldTerrainBiome.FungalForest)
            delta = Lerp(delta, Delta(UnderworldTerrainBiome.FungalForest), fungalBlend);

        var height = BaseElevationMeters + delta;
        // A missing monument must not impose a zero-height floor on basins. Blend an actual
        // footprint from its local terrain base to its absolute summit without clipping either.
        height = UnderworldMonumentalLandforms.ApplyToTerrain(domain, derivedSeed32, sample.X, sample.Z, height);
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

    private readonly record struct BiomeSelection(
        UnderworldTerrainBiome Primary,
        UnderworldTerrainBiome Secondary,
        double ScoreMargin);

    private static BiomeSelection SelectBiome(double x, double z, double distance, double radius, int seed)
    {
        if (distance <= radius * GuaranteedFungalRadiusFraction)
            return new BiomeSelection(
                UnderworldTerrainBiome.FungalForest,
                UnderworldTerrainBiome.FungalForest,
                1d);

        var warpedX = x + SignedField(seed, 0xc2b2ae35u, x, z, 3, BiomeWarpFeatureMeters) * BiomeWarpAmplitudeMeters;
        var warpedZ = z + SignedField(seed, 0x27d4eb2fu, x, z, 3, BiomeWarpFeatureMeters) * BiomeWarpAmplitudeMeters;

        var heat = Field01(seed, 0x165667b1u, warpedX, warpedZ, 4, 5200d);
        var moisture = Field01(seed, 0xd3a2646cu, warpedX, warpedZ, 4, 4600d);
        var tectonic = Field01(seed, 0xfd7046c5u, warpedX, warpedZ, 4, 3600d);
        var decay = Field01(seed, 0xb55a4f09u, warpedX, warpedZ, 4, 4200d);
        var fungal = Field01(seed, 0x9e3779b9u, warpedX, warpedZ, 4, 3200d);

        var bestBiome = UnderworldTerrainBiome.FungalForest;
        var secondBiome = UnderworldTerrainBiome.FungalForest;
        var bestScore = double.MinValue;
        var secondScore = double.MinValue;

        foreach (UnderworldTerrainBiome candidate in Enum.GetValues(typeof(UnderworldTerrainBiome)))
        {
            var salt = BiomeSalt(candidate);
            var score =
                Field01(seed, salt, warpedX, warpedZ, 4, BiomeRegionFeatureMeters) * .70d +
                Field01(seed, salt ^ 0x6c8e9cf5u, warpedX, warpedZ, 3, BiomeBroadFeatureMeters) * .30d;

            switch (candidate)
            {
                case UnderworldTerrainBiome.FungalForest:
                    score += fungal * .14d - .09d + CentralFungalBias(distance, radius);
                    break;
                case UnderworldTerrainBiome.BlackwaterDeep:
                    score += moisture * .16d + (1d - heat) * .05d;
                    break;
                case UnderworldTerrainBiome.SulfurousWastes:
                    score += heat * .18d + tectonic * .05d;
                    break;
                case UnderworldTerrainBiome.FrozenCaverns:
                    score += (1d - heat) * .18d + (1d - moisture) * .04d;
                    break;
                case UnderworldTerrainBiome.FractureZones:
                    score += tectonic * .18d + Math.Abs(heat - moisture) * .05d;
                    break;
                case UnderworldTerrainBiome.GreatDecay:
                    score += decay * .15d + moisture * .06d;
                    break;
            }

            if (score > bestScore)
            {
                secondScore = bestScore;
                secondBiome = bestBiome;
                bestScore = score;
                bestBiome = candidate;
            }
            else if (score > secondScore)
            {
                secondScore = score;
                secondBiome = candidate;
            }
        }

        return new BiomeSelection(bestBiome, secondBiome, Math.Max(0d, bestScore - secondScore));
    }

    private static double CentralFungalBias(double distance, double radius)
    {
        var inner = radius * GuaranteedFungalRadiusFraction;
        var outer = radius * .26d;
        if (distance <= inner) return .55d;
        if (distance >= outer) return 0d;
        var t = Clamp((distance - inner) / (outer - inner), 0d, 1d);
        t = t * t * (3d - 2d * t);
        return (1d - t) * .55d;
    }

    private static uint BiomeSalt(UnderworldTerrainBiome biome) => biome switch
    {
        UnderworldTerrainBiome.FungalForest => 0x1f123bb5u,
        UnderworldTerrainBiome.BlackwaterDeep => 0x34d7a2c1u,
        UnderworldTerrainBiome.SulfurousWastes => 0x5a17d3e9u,
        UnderworldTerrainBiome.FrozenCaverns => 0x72c8b4f3u,
        UnderworldTerrainBiome.FractureZones => 0x8f31a6d7u,
        UnderworldTerrainBiome.GreatDecay => 0xa5c94e21u,
        _ => 0x4cf5ad43u,
    };

    private static double Field01(int seed, uint salt, double x, double z, int octaves, double featureMetres) =>
        UnderworldTerrainNoise.Fractal01(
            unchecked((int)UnderworldTerrainNoise.Mix(unchecked((uint)seed) ^ salt)),
            x, z, octaves, featureMetres);

    private static double SignedField(int seed, uint salt, double x, double z, int octaves, double featureMetres) =>
        Field01(seed, salt, x, z, octaves, featureMetres) * 2d - 1d;

    private static double RegionalReliefWeight(double distance, double radius)
    {
        var inner = Math.Min(ArrivalProtectionRadiusMeters, radius * .01d);
        var outer = Math.Min(FullRegionalReliefRadiusMeters, radius * .04d);
        if (distance <= inner) return 0d;
        if (distance >= outer) return 1d;
        var t = (distance - inner) / (outer - inner);
        return t * t * (3d - 2d * t);
    }

    private static double FungalTransition(double distance, double radius)
    {
        var inner = radius * GuaranteedFungalRadiusFraction;
        var outer = radius * FungalTransitionRadiusFraction;
        if (distance <= inner) return 1d;
        if (distance >= outer) return 0d;
        var t = (distance - inner) / (outer - inner);
        t = t * t * (3d - 2d * t);
        return 1d - t;
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
