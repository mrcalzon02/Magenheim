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
    public const string BiomeLayoutAlgorithmId = "biome-layout-v2-hex50-voronoi-plasma-rivers";
    public const double ArrivalProtectionRadiusMeters = 80d;
    public const double FullRegionalReliefRadiusMeters = 320d;
    public const double BaseElevationMeters = 45d;
    public const double MaximumSlopeDegrees = 75d;

    // One inner safe core is guaranteed Fungal. Beyond it, Fungal is only a broad bias in the
    // same seeded field as every other biome, so the arrival country has an organic edge.
    public const double GuaranteedFungalRadiusFraction = 0.10d;
    public const double CentralFungalRadiusFraction = 0.16d;
    public const double FungalTransitionRadiusFraction = 0.24d;

    // Macro organization: an ideal hex lattice supplies even region spacing, each site may move
    // by up to 50% of the nominal hex radius, and plasma/domain warp bends the Voronoi edges.
    public const double HexCellRadiusMeters = 900d;
    public const double HexSiteJitterFraction = 0.50d;
    public const double PlasmaWarpFeatureMeters = 1250d;
    public const double PlasmaWarpAmplitudeMeters = 210d;
    public const double BiomeBoundaryBlendMeters = 260d;

    // The Voronoi edge is also the hydrology authority. Strong edge bands become connected
    // Blackwater/ocean-depth channels, giving the Underworld a warped river grid.
    public const double HexRiverGapWidthMeters = 320d;
    public const double HexRiverPlasmaWidthMeters = 90d;
    public const double HexRiverBiomeThreshold = 0.62d;
    public const double HexRiverFullActivationRadiusFraction = 0.18d;

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

        // Ecology/weather ownership remains discrete, but plasma-softened Voronoi borders blend
        // the neighbouring terrain profiles so the cellular geography does not create hard steps.
        if (selection.Secondary != biome && selection.TerrainBlend01 > 0d)
            delta = Lerp(delta, Delta(selection.Secondary), selection.TerrainBlend01 * .5d);
        var fungalBlend = FungalTransition(distance, domain.RadiusMeters);
        if (fungalBlend > 0d && biome != UnderworldTerrainBiome.FungalForest)
            delta = Lerp(delta, Delta(UnderworldTerrainBiome.FungalForest), fungalBlend);

        var height = BaseElevationMeters + delta;

        // The same cellular edge that separates biome territories is the river skeleton. It is
        // physically carved toward Blackwater/ocean depth before monumental landforms are applied,
        // allowing rare natural arches/spires to cross the channels without erasing the network.
        height = ApplyHexRiverCarve(height, sample.WaterLevel, selection.Channel01);

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
        double TerrainBlend01,
        double Channel01);

    private readonly record struct HexSite(int Q, int R, double X, double Z);

    private readonly record struct CellularSample(
        HexSite Nearest,
        HexSite Second,
        double NearestDistance,
        double SecondDistance,
        double Channel01);

    private static BiomeSelection SelectBiome(double x, double z, double distance, double radius, int seed)
    {
        if (distance <= radius * GuaranteedFungalRadiusFraction)
            return new BiomeSelection(
                UnderworldTerrainBiome.FungalForest,
                UnderworldTerrainBiome.FungalForest,
                0d,
                0d);

        var cellular = SampleCellular(seed, x, z);
        var primary = BiomeForSite(seed, cellular.Nearest);
        var secondary = BiomeForSite(seed, cellular.Second);

        // The protected Fungal country is a noisy shoulder rather than a circular ownership rule.
        // A low-frequency field decides where the shoulder protrudes or retreats.
        var fungalBias = CentralFungalBias(distance, radius);
        if (fungalBias > 0d)
        {
            var fungalField = Field01(seed, 0x1f123bb5u, x, z, 3, 2600d);
            if (fungalField + fungalBias * .8d > .82d)
            {
                secondary = primary;
                primary = UnderworldTerrainBiome.FungalForest;
            }
        }

        // Fade the hydrology in outside the guaranteed arrival core so the Deep Gate is never
        // ringed by a synthetic moat. Once active, the deepest channel band is genuine Blackwater.
        var activation = RiverActivation(distance, radius);
        var channel = cellular.Channel01 * activation;
        var boundaryGap = Math.Max(0d, cellular.SecondDistance - cellular.NearestDistance);
        var terrainBlend = 1d - Smooth01(boundaryGap / BiomeBoundaryBlendMeters);

        if (channel >= HexRiverBiomeThreshold)
        {
            var bankBiome = primary;
            primary = UnderworldTerrainBiome.BlackwaterDeep;
            if (bankBiome != primary) secondary = bankBiome;

            // Deep channel cores own their terrain; closer to the bank, allow some neighbouring
            // geology to influence the underwater slope.
            terrainBlend = Math.Min(terrainBlend, (1d - channel) * .75d);
        }

        return new BiomeSelection(primary, secondary, terrainBlend, channel);
    }

    private static CellularSample SampleCellular(int seed, double x, double z)
    {
        // Plasma/domain warp attacks the edges of the cells without erasing their macro hex logic.
        var warpedX = x + SignedField(seed, 0xc2b2ae35u, x, z, 4, PlasmaWarpFeatureMeters) *
                      PlasmaWarpAmplitudeMeters;
        var warpedZ = z + SignedField(seed, 0x27d4eb2fu, x, z, 4, PlasmaWarpFeatureMeters) *
                      PlasmaWarpAmplitudeMeters;

        var horizontal = Math.Sqrt(3d) * HexCellRadiusMeters;
        var vertical = 1.5d * HexCellRadiusMeters;
        var centreR = (int)Math.Round(warpedZ / vertical, MidpointRounding.AwayFromZero);
        var centreQ = (int)Math.Round(
            warpedX / horizontal - .5d * (centreR & 1),
            MidpointRounding.AwayFromZero);

        var nearest = new HexSite();
        var second = new HexSite();
        var nearestDistance = double.MaxValue;
        var secondDistance = double.MaxValue;

        // 5x5 is deliberately wider than mathematically necessary because 50% site jitter plus
        // plasma warp can move a neighbouring seed well away from its ideal lattice position.
        for (var dr = -2; dr <= 2; dr++)
        for (var dq = -2; dq <= 2; dq++)
        {
            var site = BuildHexSite(seed, centreQ + dq, centreR + dr);
            var dx = warpedX - site.X;
            var dz = warpedZ - site.Z;
            var distance = Math.Sqrt(dx * dx + dz * dz);
            if (distance < nearestDistance)
            {
                secondDistance = nearestDistance;
                second = nearest;
                nearestDistance = distance;
                nearest = site;
            }
            else if (distance < secondDistance)
            {
                secondDistance = distance;
                second = site;
            }
        }

        // Voronoi edges are where the first and second site distances converge. A second plasma
        // field varies the channel width, producing braided/bulged river banks rather than ruler
        // straight cellular seams.
        var edgePlasma = SignedField(seed, 0x5bd1e995u, x, z, 4, 700d);
        var width = HexRiverGapWidthMeters + edgePlasma * HexRiverPlasmaWidthMeters;
        var gap = Math.Max(0d, secondDistance - nearestDistance);
        var channel = 1d - Smooth01(gap / Math.Max(80d, width));

        return new CellularSample(nearest, second, nearestDistance, secondDistance, channel);
    }

    private static HexSite BuildHexSite(int seed, int q, int r)
    {
        var horizontal = Math.Sqrt(3d) * HexCellRadiusMeters;
        var vertical = 1.5d * HexCellRadiusMeters;
        var x = horizontal * (q + .5d * (r & 1));
        var z = vertical * r;

        var angle = SiteUnit(seed, q, r, 0x1234abcdu) * Math.PI * 2d;
        var magnitude = Math.Sqrt(SiteUnit(seed, q, r, 0x9a73e5d1u)) *
                        HexCellRadiusMeters * HexSiteJitterFraction;
        return new HexSite(
            q,
            r,
            x + Math.Cos(angle) * magnitude,
            z + Math.Sin(angle) * magnitude);
    }

    private static UnderworldTerrainBiome BiomeForSite(int seed, HexSite site)
    {
        // Fractal regional fields decide which biome wins each cellular seed; a smaller deterministic
        // site term stops a single broad field from swallowing the whole map. The hex grid decides
        // spacing, Voronoi decides ownership, and fractal noise decides the material/ecology family.
        var bestBiome = UnderworldTerrainBiome.FungalForest;
        var bestScore = double.MinValue;
        foreach (UnderworldTerrainBiome candidate in Enum.GetValues(typeof(UnderworldTerrainBiome)))
        {
            var salt = BiomeSalt(candidate);
            var regional = Field01(seed, salt, site.X, site.Z, 2, 4800d);
            var siteNoise = SiteUnit(seed, site.Q, site.R, salt ^ 0x6c8e9cf5u);
            var score = regional * .72d + siteNoise * .28d;
            if (score <= bestScore) continue;
            bestScore = score;
            bestBiome = candidate;
        }
        return bestBiome;
    }

    private static double CentralFungalBias(double distance, double radius)
    {
        var inner = radius * GuaranteedFungalRadiusFraction;
        var outer = radius * .24d;
        if (distance <= inner) return .55d;
        if (distance >= outer) return 0d;
        var t = Smooth01((distance - inner) / (outer - inner));
        return (1d - t) * .55d;
    }

    private static double RiverActivation(double distance, double radius)
    {
        var inner = radius * GuaranteedFungalRadiusFraction;
        var outer = radius * HexRiverFullActivationRadiusFraction;
        if (distance <= inner) return 0d;
        if (distance >= outer) return 1d;
        return Smooth01((distance - inner) / (outer - inner));
    }

    private static double ApplyHexRiverCarve(double height, double waterLevel, double channel01)
    {
        if (channel01 <= 0d) return height;

        // Banks ease in broadly. Once a point crosses the Blackwater ownership threshold, it is
        // guaranteed below the shared water plane; the deepest Voronoi seam reaches ~30m depth.
        var carve = Smooth01((channel01 - .20d) / .80d);
        var desiredDepth = 5d + 25d * Math.Pow(channel01, 1.35d);
        var bed = waterLevel - desiredDepth;
        if (bed < height)
            height = Lerp(height, bed, carve);

        if (channel01 >= HexRiverBiomeThreshold)
        {
            var core = Smooth01(
                (channel01 - HexRiverBiomeThreshold) /
                (1d - HexRiverBiomeThreshold));
            height = Math.Min(height, waterLevel - (2d + 24d * core));
        }

        return height;
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

    private static double SiteUnit(int seed, int q, int r, uint salt)
    {
        unchecked
        {
            var value = (uint)seed ^
                        ((uint)q * 0x9e3779b9u) ^
                        ((uint)r * 0x85ebca6bu) ^
                        salt;
            return UnderworldTerrainNoise.Mix(value) / ((double)uint.MaxValue + 1d);
        }
    }

    private static double Field01(int seed, uint salt, double x, double z, int octaves, double featureMetres) =>
        UnderworldTerrainNoise.Fractal01(
            unchecked((int)UnderworldTerrainNoise.Mix(unchecked((uint)seed) ^ salt)),
            x, z, octaves, featureMetres);

    private static double SignedField(int seed, uint salt, double x, double z, int octaves, double featureMetres) =>
        Field01(seed, salt, x, z, octaves, featureMetres) * 2d - 1d;

    private static double Smooth01(double value)
    {
        value = Clamp(value, 0d, 1d);
        return value * value * (3d - 2d * value);
    }

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
