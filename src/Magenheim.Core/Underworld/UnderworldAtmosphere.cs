using System;

namespace Magenheim.Core.Underworld;

public enum UnderworldAtmosphereKind
{
    LuminousSpores,
    BlackwaterMist,
    SulfurMiasma,
    FrozenFog,
    FractureDust,
    DecayMiasma,
}

public enum UnderworldAtmosphereEvent
{
    None,
    Sporefall,
    DeepFog,
    Ashfall,
    ThermalSurge,
    Whiteout,
    StoneRain,
    CrystalResonance,
    BlackBloom,
}

public readonly record struct UnderworldAtmosphereInput(
    UnderworldTerrainBiome Biome,
    double TerrainHeightMeters,
    double WaterDepthMeters,
    double Hazard01,
    UnderworldAtmosphereEvent Event,
    double EventIntensity01,
    double Resistance01,
    double Suppression01);

public readonly record struct UnderworldAtmosphereState(
    UnderworldAtmosphereKind Kind,
    double VisualDensity01,
    double VisibilityMeters,
    double ParticleDensity01,
    double Exposure01,
    double AudioDamping01,
    double FogRed01,
    double FogGreen01,
    double FogBlue01);

/// <summary>
/// Pure shared authority for Underworld obscuration. Biomes reuse one mechanic with different
/// profiles: spores, water mist, sulfur miasma, ice fog, fracture dust and decay aerosol.
/// Resistance reduces gameplay pressure without deleting the visual atmosphere. Suppression is the
/// separate local-clearing verb used by future objects such as the Censer.
/// </summary>
public static class UnderworldAtmosphere
{
    private readonly record struct Profile(
        UnderworldAtmosphereKind Kind,
        double BaseVisual,
        double MinimumVisual,
        double LowlandVisual,
        double WaterVisual,
        double HazardVisual,
        double BaseParticles,
        double BaseExposure,
        double HazardExposure,
        double BaseAudio,
        double ClearVisibility,
        double DenseVisibility,
        double Red,
        double Green,
        double Blue);

    private readonly record struct EventModifier(
        double Visual,
        double Particles,
        double Exposure,
        double Audio);

    public static UnderworldAtmosphereState Evaluate(UnderworldAtmosphereInput input)
    {
        if (!Finite(input.TerrainHeightMeters) || !Finite(input.WaterDepthMeters) ||
            !Finite(input.Hazard01) || !Finite(input.EventIntensity01) ||
            !Finite(input.Resistance01) || !Finite(input.Suppression01))
            throw new ArgumentOutOfRangeException(nameof(input), "Atmosphere inputs must be finite.");

        var profile = For(input.Biome);
        var hazard = Clamp01(input.Hazard01);
        var water = Clamp01(Math.Max(0d, input.WaterDepthMeters) / 8d);
        var lowland = Clamp01((UnderworldTerrainLifecycle.BaseElevationMeters + 8d - input.TerrainHeightMeters) / 55d);
        var eventIntensity = Clamp01(input.EventIntensity01);
        var resistance = Clamp01(input.Resistance01);
        var suppression = Clamp01(input.Suppression01);
        var modifier = EventFor(input.Biome, input.Event);

        var visual = profile.BaseVisual +
                     lowland * profile.LowlandVisual +
                     water * profile.WaterVisual +
                     hazard * profile.HazardVisual +
                     modifier.Visual * eventIntensity;
        visual = Math.Max(profile.MinimumVisual, Clamp01(visual) * (1d - suppression * 0.82d));

        var particles = profile.BaseParticles +
                        hazard * 0.08d +
                        modifier.Particles * eventIntensity;
        particles = Clamp01(particles * (1d - suppression * 0.65d));

        var exposure = profile.BaseExposure +
                       hazard * profile.HazardExposure +
                       modifier.Exposure * eventIntensity;
        exposure = Clamp01(exposure * (1d - resistance * 0.85d) * (1d - suppression * 0.65d));

        var audio = Clamp01(profile.BaseAudio + visual * 0.28d + modifier.Audio * eventIntensity);
        audio = Clamp01(audio * (1d - suppression * 0.35d));

        // Clear cavern air must reveal kilometre-scale cliffs. Lowland miasma and weather
        // still close the view; exposure/particles remain independent of this sight distance.
        var horizon = 14000d * Math.Pow(1d - visual, 3d);
        var visibility = Math.Max(Lerp(profile.ClearVisibility, profile.DenseVisibility, visual), horizon);
        return new UnderworldAtmosphereState(
            profile.Kind,
            visual,
            Math.Max(8d, visibility),
            particles,
            exposure,
            audio,
            Clamp01(profile.Red),
            Clamp01(profile.Green),
            Clamp01(profile.Blue));
    }

    public static bool EventApplies(UnderworldTerrainBiome biome, UnderworldAtmosphereEvent atmosphereEvent)
    {
        if (atmosphereEvent == UnderworldAtmosphereEvent.None ||
            atmosphereEvent == UnderworldAtmosphereEvent.CrystalResonance)
            return true;

        switch (atmosphereEvent)
        {
            case UnderworldAtmosphereEvent.Sporefall:
                return biome == UnderworldTerrainBiome.FungalForest;
            case UnderworldAtmosphereEvent.DeepFog:
                return biome == UnderworldTerrainBiome.BlackwaterDeep ||
                       biome == UnderworldTerrainBiome.FrozenCaverns;
            case UnderworldAtmosphereEvent.Ashfall:
            case UnderworldAtmosphereEvent.ThermalSurge:
                return biome == UnderworldTerrainBiome.SulfurousWastes;
            case UnderworldAtmosphereEvent.Whiteout:
                return biome == UnderworldTerrainBiome.FrozenCaverns;
            case UnderworldAtmosphereEvent.StoneRain:
                return biome == UnderworldTerrainBiome.FractureZones;
            case UnderworldAtmosphereEvent.BlackBloom:
                return biome == UnderworldTerrainBiome.GreatDecay;
            default:
                return false;
        }
    }

    private static EventModifier EventFor(UnderworldTerrainBiome biome, UnderworldAtmosphereEvent atmosphereEvent)
    {
        if (!EventApplies(biome, atmosphereEvent)) return default;

        switch (atmosphereEvent)
        {
            case UnderworldAtmosphereEvent.Sporefall:
                return new EventModifier(0.10d, 0.55d, 0d, 0.04d);
            case UnderworldAtmosphereEvent.DeepFog:
                return biome == UnderworldTerrainBiome.BlackwaterDeep
                    ? new EventModifier(0.42d, 0.18d, 0.05d, 0.22d)
                    : new EventModifier(0.28d, 0.24d, 0.08d, 0.22d);
            case UnderworldAtmosphereEvent.Ashfall:
                return new EventModifier(0.25d, 0.58d, 0.10d, 0.12d);
            case UnderworldAtmosphereEvent.ThermalSurge:
                return new EventModifier(0.18d, 0.22d, 0.36d, 0.08d);
            case UnderworldAtmosphereEvent.Whiteout:
                return new EventModifier(0.55d, 0.68d, 0.18d, 0.42d);
            case UnderworldAtmosphereEvent.StoneRain:
                return new EventModifier(0.30d, 0.62d, 0.12d, 0.20d);
            case UnderworldAtmosphereEvent.CrystalResonance:
                return new EventModifier(0d, 0.16d, 0d, 0.02d);
            case UnderworldAtmosphereEvent.BlackBloom:
                return new EventModifier(0.34d, 0.40d, 0.38d, 0.18d);
            default:
                return default;
        }
    }

    private static Profile For(UnderworldTerrainBiome biome)
    {
        switch (biome)
        {
            case UnderworldTerrainBiome.FungalForest:
                return new Profile(UnderworldAtmosphereKind.LuminousSpores,
                    0.08d, 0.04d, 0.06d, 0.04d, 0d,
                    0.20d, 0d, 0d, 0.04d, 220d, 85d,
                    0.20d, 0.38d, 0.43d);
            case UnderworldTerrainBiome.BlackwaterDeep:
                return new Profile(UnderworldAtmosphereKind.BlackwaterMist,
                    0.20d, 0.12d, 0.18d, 0.24d, 0.06d,
                    0.06d, 0.08d, 0.30d, 0.18d, 180d, 38d,
                    0.08d, 0.15d, 0.19d);
            case UnderworldTerrainBiome.SulfurousWastes:
                return new Profile(UnderworldAtmosphereKind.SulfurMiasma,
                    0.10d, 0.04d, 0.10d, 0d, 0.52d,
                    0.12d, 0.18d, 0.62d, 0.08d, 210d, 38d,
                    0.45d, 0.39d, 0.16d);
            case UnderworldTerrainBiome.FrozenCaverns:
                return new Profile(UnderworldAtmosphereKind.FrozenFog,
                    0.12d, 0.05d, 0.20d, 0.08d, 0.22d,
                    0.10d, 0.15d, 0.42d, 0.12d, 230d, 42d,
                    0.62d, 0.74d, 0.82d);
            case UnderworldTerrainBiome.FractureZones:
                return new Profile(UnderworldAtmosphereKind.FractureDust,
                    0.05d, 0.02d, 0.03d, 0d, 0.10d,
                    0.08d, 0.05d, 0.22d, 0.06d, 250d, 70d,
                    0.34d, 0.30d, 0.40d);
            case UnderworldTerrainBiome.GreatDecay:
                return new Profile(UnderworldAtmosphereKind.DecayMiasma,
                    0.48d, 0.18d, 0.22d, 0.04d, 0.28d,
                    0.34d, 0.28d, 0.52d, 0.22d, 135d, 22d,
                    0.28d, 0.14d, 0.12d);
            default:
                throw new ArgumentOutOfRangeException(nameof(biome), biome, "Unknown Underworld biome.");
        }
    }

    private static double Lerp(double from, double to, double amount) =>
        from + (to - from) * Clamp01(amount);

    private static double Clamp01(double value) => Math.Max(0d, Math.Min(1d, value));

    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
