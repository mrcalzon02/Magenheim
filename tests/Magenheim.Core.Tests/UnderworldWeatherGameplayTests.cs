using System;
using Magenheim.Core.Underworld;

internal static class UnderworldWeatherGameplayTests
{
    public static int Run()
    {
        var assertions = 0;
        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new InvalidOperationException("Underworld weather gameplay: " + message);
        }

        var sporefall = UnderworldWeatherGameplay.Evaluate(
            UnderworldTerrainBiome.FungalForest,
            UnderworldAtmosphereEvent.Sporefall, 1d, 1d);
        Assert(!sporefall.HasDamage, "Sporefall must remain visually intense but non-damaging.");

        var resonance = UnderworldWeatherGameplay.Evaluate(
            UnderworldTerrainBiome.FractureZones,
            UnderworldAtmosphereEvent.CrystalResonance, 1d, 1d);
        Assert(!resonance.HasDamage, "Crystal Resonance must remain non-hostile.");

        var deepFog = UnderworldWeatherGameplay.Evaluate(
            UnderworldTerrainBiome.BlackwaterDeep,
            UnderworldAtmosphereEvent.DeepFog, 1d, 1d);
        Assert(!deepFog.HasDamage, "Deep Fog is navigation/visibility pressure, not arbitrary health loss.");

        var ash = UnderworldWeatherGameplay.Evaluate(
            UnderworldTerrainBiome.SulfurousWastes,
            UnderworldAtmosphereEvent.Ashfall, 1d, .8d);
        var thermal = UnderworldWeatherGameplay.Evaluate(
            UnderworldTerrainBiome.SulfurousWastes,
            UnderworldAtmosphereEvent.ThermalSurge, 1d, .8d);
        Assert(ash.DamageKind == UnderworldWeatherDamageKind.Fire && ash.DamagePerPulse > 0d,
            "Ashfall must carry hot-particulate fire pressure.");
        Assert(thermal.DamageKind == UnderworldWeatherDamageKind.Fire &&
               thermal.DamagePerPulse > ash.DamagePerPulse,
            "Thermal Surge must be the stronger Sulfur heat event.");

        var whiteout = UnderworldWeatherGameplay.Evaluate(
            UnderworldTerrainBiome.FrozenCaverns,
            UnderworldAtmosphereEvent.Whiteout, 1d, .8d);
        Assert(whiteout.DamageKind == UnderworldWeatherDamageKind.Frost &&
               whiteout.DamagePerPulse > 0d,
            "Whiteout must add real frost pressure.");

        var stone = UnderworldWeatherGameplay.Evaluate(
            UnderworldTerrainBiome.FractureZones,
            UnderworldAtmosphereEvent.StoneRain, 1d, .7d);
        Assert(stone.DamageKind == UnderworldWeatherDamageKind.Blunt &&
               stone.PulseChance01 > 0d && stone.PulseChance01 < 1d &&
               stone.StaggerMultiplier > 1d,
            "Stone Rain must be intermittent blunt/stagger pressure rather than a DOT.");

        var bloom = UnderworldWeatherGameplay.Evaluate(
            UnderworldTerrainBiome.GreatDecay,
            UnderworldAtmosphereEvent.BlackBloom, 1d, .9d);
        Assert(bloom.DamageKind == UnderworldWeatherDamageKind.Poison &&
               bloom.DamagePerPulse > 0d &&
               bloom.CreatureActivityMultiplier > 1d,
            "Black Bloom must increase contamination and biological activity.");

        var mitigated = UnderworldWeatherGameplay.Evaluate(
            UnderworldTerrainBiome.GreatDecay,
            UnderworldAtmosphereEvent.BlackBloom, 1d, .2d);
        Assert(mitigated.DamagePerPulse < bloom.DamagePerPulse,
            "Atmosphere resistance/suppression must materially reduce weather gameplay pressure.");

        var wrongBiome = UnderworldWeatherGameplay.Evaluate(
            UnderworldTerrainBiome.FungalForest,
            UnderworldAtmosphereEvent.BlackBloom, 1d, .9d);
        Assert(!wrongBiome.HasDamage,
            "Biome-incompatible event input must not leak hostile weather into another biome.");

        return assertions;
    }
}
