using System;
using System.Collections.Generic;
using Magenheim.Core.Underworld;

internal static class UnderworldWeatherTests
{
    public static int Run()
    {
        var assertions = 0;
        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new InvalidOperationException(
                $"Underworld weather assertion {assertions} failed: {message}");
        }

        const int seed = 1675883973;
        foreach (UnderworldTerrainBiome biome in Enum.GetValues(typeof(UnderworldTerrainBiome)))
        {
            var first = UnderworldWeatherCycle.Evaluate(biome, seed, 8123.5d);
            var repeat = UnderworldWeatherCycle.Evaluate(biome, seed, 8123.5d);
            Assert(first == repeat, $"{biome} weather must be deterministic for identical seed/time.");
            Assert(UnderworldWeatherCycle.IsAllowed(biome, first.Event),
                $"{biome} selected an event outside its own weather vocabulary.");
            Assert(first.Intensity01 >= 0d && first.Intensity01 <= 1d,
                $"{biome} event intensity must remain normalized.");
            Assert(first.Event != UnderworldAtmosphereEvent.None || first.Intensity01 == 0d,
                $"{biome} calm weather must carry zero event intensity.");

            var observed = new HashSet<UnderworldAtmosphereEvent>();
            for (var period = 0; period < 512; period++)
            {
                var state = UnderworldWeatherCycle.Evaluate(
                    biome, seed, period * UnderworldWeatherCycle.PeriodSeconds + 0.25d);
                Assert(state.Period == period, $"{biome} period index must follow the shared world clock.");
                Assert(UnderworldWeatherCycle.IsAllowed(biome, state.Event),
                    $"{biome} generated an ineligible event during the long-run schedule.");
                observed.Add(state.Event);
            }

            Assert(observed.Contains(UnderworldAtmosphereEvent.None),
                $"{biome} needs calm windows between environmental events.");
            Assert(observed.Contains(UnderworldAtmosphereEvent.CrystalResonance),
                $"{biome} must retain the shared rare Crystal Resonance event.");

            switch (biome)
            {
                case UnderworldTerrainBiome.FungalForest:
                    Assert(observed.Contains(UnderworldAtmosphereEvent.Sporefall),
                        "Fungal Forest schedule must produce Sporefall.");
                    break;
                case UnderworldTerrainBiome.BlackwaterDeep:
                    Assert(observed.Contains(UnderworldAtmosphereEvent.DeepFog),
                        "Blackwater schedule must produce Deep Fog.");
                    break;
                case UnderworldTerrainBiome.SulfurousWastes:
                    Assert(observed.Contains(UnderworldAtmosphereEvent.Ashfall) &&
                           observed.Contains(UnderworldAtmosphereEvent.ThermalSurge),
                        "Sulfurous Wastes schedule must produce both Ashfall and Thermal Surge.");
                    break;
                case UnderworldTerrainBiome.FrozenCaverns:
                    Assert(observed.Contains(UnderworldAtmosphereEvent.DeepFog) &&
                           observed.Contains(UnderworldAtmosphereEvent.Whiteout),
                        "Frozen Caverns schedule must produce both cold fog and Whiteout.");
                    break;
                case UnderworldTerrainBiome.FractureZones:
                    Assert(observed.Contains(UnderworldAtmosphereEvent.StoneRain),
                        "Fracture schedule must produce Stone Rain.");
                    break;
                case UnderworldTerrainBiome.GreatDecay:
                    Assert(observed.Contains(UnderworldAtmosphereEvent.BlackBloom),
                        "Great Decay schedule must produce Black Bloom.");
                    break;
            }
        }

        var changed = false;
        var original = UnderworldWeatherCycle.Evaluate(
            UnderworldTerrainBiome.GreatDecay, seed, 0.5d);
        for (var otherSeed = seed + 1; otherSeed < seed + 64; otherSeed++)
        {
            if (UnderworldWeatherCycle.Evaluate(
                    UnderworldTerrainBiome.GreatDecay, otherSeed, 0.5d) != original)
            {
                changed = true;
                break;
            }
        }
        Assert(changed, "Derived instance seed must affect the subterranean weather sequence.");

        var rejected = false;
        try
        {
            UnderworldWeatherCycle.Evaluate(
                UnderworldTerrainBiome.FungalForest, seed, double.NaN);
        }
        catch (ArgumentOutOfRangeException)
        {
            rejected = true;
        }
        Assert(rejected, "Malformed world time must fail closed.");

        return assertions;
    }
}
