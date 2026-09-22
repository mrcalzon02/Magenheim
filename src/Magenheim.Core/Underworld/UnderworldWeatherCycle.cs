using System;

namespace Magenheim.Core.Underworld;

public readonly record struct UnderworldWeatherState(
    long Period,
    UnderworldAtmosphereEvent Event,
    double Intensity01);

/// <summary>
/// Deterministic Underworld weather/event selector driven by the paired instance seed and Valheim's
/// authoritative world clock. The Underworld does not inherit Surface storm tables: each biome
/// selects only from its own subterranean event vocabulary.
/// </summary>
public static class UnderworldWeatherCycle
{
    public const long PeriodSeconds = 240L;

    public static UnderworldWeatherState Evaluate(
        UnderworldTerrainBiome biome,
        int derivedSeed32,
        double worldSeconds)
    {
        if (double.IsNaN(worldSeconds) || double.IsInfinity(worldSeconds) || worldSeconds < 0d)
            throw new ArgumentOutOfRangeException(nameof(worldSeconds),
                "World time must be a finite non-negative value.");

        var period = (long)Math.Floor(worldSeconds / PeriodSeconds);
        var roll = Unit(Hash(derivedSeed32, period, biome, 0x51f15e5du));
        var intensityRoll = Unit(Hash(derivedSeed32, period, biome, 0x8c3c6af1u));
        var selected = Select(biome, roll);
        var intensity = selected == UnderworldAtmosphereEvent.None
            ? 0d
            : 0.62d + intensityRoll * 0.38d;
        return new UnderworldWeatherState(period, selected, intensity);
    }

    public static bool IsAllowed(UnderworldTerrainBiome biome, UnderworldAtmosphereEvent atmosphereEvent)
    {
        switch (biome)
        {
            case UnderworldTerrainBiome.FungalForest:
                return atmosphereEvent == UnderworldAtmosphereEvent.None ||
                       atmosphereEvent == UnderworldAtmosphereEvent.Sporefall ||
                       atmosphereEvent == UnderworldAtmosphereEvent.CrystalResonance;
            case UnderworldTerrainBiome.BlackwaterDeep:
                return atmosphereEvent == UnderworldAtmosphereEvent.None ||
                       atmosphereEvent == UnderworldAtmosphereEvent.DeepFog ||
                       atmosphereEvent == UnderworldAtmosphereEvent.CrystalResonance;
            case UnderworldTerrainBiome.SulfurousWastes:
                return atmosphereEvent == UnderworldAtmosphereEvent.None ||
                       atmosphereEvent == UnderworldAtmosphereEvent.Ashfall ||
                       atmosphereEvent == UnderworldAtmosphereEvent.ThermalSurge ||
                       atmosphereEvent == UnderworldAtmosphereEvent.CrystalResonance;
            case UnderworldTerrainBiome.FrozenCaverns:
                return atmosphereEvent == UnderworldAtmosphereEvent.None ||
                       atmosphereEvent == UnderworldAtmosphereEvent.DeepFog ||
                       atmosphereEvent == UnderworldAtmosphereEvent.Whiteout ||
                       atmosphereEvent == UnderworldAtmosphereEvent.CrystalResonance;
            case UnderworldTerrainBiome.FractureZones:
                return atmosphereEvent == UnderworldAtmosphereEvent.None ||
                       atmosphereEvent == UnderworldAtmosphereEvent.StoneRain ||
                       atmosphereEvent == UnderworldAtmosphereEvent.CrystalResonance;
            case UnderworldTerrainBiome.GreatDecay:
                return atmosphereEvent == UnderworldAtmosphereEvent.None ||
                       atmosphereEvent == UnderworldAtmosphereEvent.BlackBloom ||
                       atmosphereEvent == UnderworldAtmosphereEvent.CrystalResonance;
            default:
                return false;
        }
    }

    private static UnderworldAtmosphereEvent Select(UnderworldTerrainBiome biome, double roll)
    {
        switch (biome)
        {
            case UnderworldTerrainBiome.FungalForest:
                if (roll < 0.60d) return UnderworldAtmosphereEvent.None;
                if (roll < 0.90d) return UnderworldAtmosphereEvent.Sporefall;
                return UnderworldAtmosphereEvent.CrystalResonance;

            case UnderworldTerrainBiome.BlackwaterDeep:
                if (roll < 0.55d) return UnderworldAtmosphereEvent.None;
                if (roll < 0.90d) return UnderworldAtmosphereEvent.DeepFog;
                return UnderworldAtmosphereEvent.CrystalResonance;

            case UnderworldTerrainBiome.SulfurousWastes:
                if (roll < 0.50d) return UnderworldAtmosphereEvent.None;
                if (roll < 0.75d) return UnderworldAtmosphereEvent.Ashfall;
                if (roll < 0.95d) return UnderworldAtmosphereEvent.ThermalSurge;
                return UnderworldAtmosphereEvent.CrystalResonance;

            case UnderworldTerrainBiome.FrozenCaverns:
                if (roll < 0.50d) return UnderworldAtmosphereEvent.None;
                if (roll < 0.70d) return UnderworldAtmosphereEvent.DeepFog;
                if (roll < 0.95d) return UnderworldAtmosphereEvent.Whiteout;
                return UnderworldAtmosphereEvent.CrystalResonance;

            case UnderworldTerrainBiome.FractureZones:
                if (roll < 0.55d) return UnderworldAtmosphereEvent.None;
                if (roll < 0.85d) return UnderworldAtmosphereEvent.StoneRain;
                return UnderworldAtmosphereEvent.CrystalResonance;

            case UnderworldTerrainBiome.GreatDecay:
                if (roll < 0.45d) return UnderworldAtmosphereEvent.None;
                if (roll < 0.90d) return UnderworldAtmosphereEvent.BlackBloom;
                return UnderworldAtmosphereEvent.CrystalResonance;

            default:
                throw new ArgumentOutOfRangeException(nameof(biome), biome, "Unknown Underworld biome.");
        }
    }

    private static uint Hash(int seed, long period, UnderworldTerrainBiome biome, uint salt)
    {
        unchecked
        {
            var lo = (uint)period;
            var hi = (uint)(period >> 32);
            var value = (uint)seed ^
                        UnderworldTerrainNoise.Mix(lo ^ salt) ^
                        UnderworldTerrainNoise.Mix(hi + 0x9e3779b9u) ^
                        ((uint)biome * 0x85ebca6bu);
            return UnderworldTerrainNoise.Mix(value);
        }
    }

    private static double Unit(uint value) => value / ((double)uint.MaxValue + 1d);
}
