using System;

namespace Magenheim.Core.Underworld;

public enum UnderworldWeatherDamageKind
{
    None,
    Fire,
    Frost,
    Blunt,
    Poison,
}

/// <summary>
/// Server-consumable gameplay pressure for one deterministic Underworld weather state.
/// Visibility, particles and colour remain UnderworldAtmosphere authority; this layer answers only
/// what the weather should materially do to a player after atmospheric resistance/suppression.
/// </summary>
public readonly record struct UnderworldWeatherGameplayState(
    UnderworldWeatherDamageKind DamageKind,
    double DamagePerPulse,
    double PulseIntervalSeconds,
    double PulseChance01,
    double StaggerMultiplier,
    double CreatureActivityMultiplier)
{
    public bool HasDamage => DamageKind != UnderworldWeatherDamageKind.None && DamagePerPulse > 0d;
}

/// <summary>
/// Pure gameplay coupling for subterranean weather. Event intensity controls the event-specific
/// addition while already-mitigated atmosphere exposure controls environmental pressure. Harmless
/// events remain harmless: Sporefall and Crystal Resonance are not converted into arbitrary DOTs.
/// </summary>
public static class UnderworldWeatherGameplay
{
    public static UnderworldWeatherGameplayState Evaluate(
        UnderworldTerrainBiome biome,
        UnderworldAtmosphereEvent atmosphereEvent,
        double eventIntensity01,
        double exposure01)
    {
        if (double.IsNaN(eventIntensity01) || double.IsInfinity(eventIntensity01) ||
            double.IsNaN(exposure01) || double.IsInfinity(exposure01))
            throw new ArgumentOutOfRangeException(nameof(eventIntensity01));

        var eventIntensity = Clamp01(eventIntensity01);
        var exposure = Clamp01(exposure01);

        // Normal persistent atmosphere matters in the hostile tiers even when the event roll is calm.
        var state = biome switch
        {
            UnderworldTerrainBiome.SulfurousWastes =>
                Damage(UnderworldWeatherDamageKind.Poison, 1.8d * exposure, 6d, 1d, .35d),
            UnderworldTerrainBiome.FrozenCaverns =>
                Damage(UnderworldWeatherDamageKind.Frost, 1.6d * exposure, 6d, 1d, .35d),
            UnderworldTerrainBiome.GreatDecay =>
                Damage(UnderworldWeatherDamageKind.Poison, 2.6d * exposure, 5d, 1d, .45d),
            _ => default,
        };

        if (!UnderworldAtmosphere.EventApplies(biome, atmosphereEvent))
            return state;

        switch (atmosphereEvent)
        {
            case UnderworldAtmosphereEvent.Sporefall:
            case UnderworldAtmosphereEvent.DeepFog:
            case UnderworldAtmosphereEvent.CrystalResonance:
                return state;

            case UnderworldAtmosphereEvent.Ashfall:
                return Merge(state, Damage(
                    UnderworldWeatherDamageKind.Fire,
                    2.5d * eventIntensity * Math.Max(.35d, exposure),
                    5d, 1d, .45d));

            case UnderworldAtmosphereEvent.ThermalSurge:
                return Merge(state, Damage(
                    UnderworldWeatherDamageKind.Fire,
                    6.0d * eventIntensity * Math.Max(.45d, exposure),
                    4d, 1d, .65d));

            case UnderworldAtmosphereEvent.Whiteout:
                return Merge(state, Damage(
                    UnderworldWeatherDamageKind.Frost,
                    4.5d * eventIntensity * Math.Max(.40d, exposure),
                    5d, 1d, .55d));

            case UnderworldAtmosphereEvent.StoneRain:
                return Damage(
                    UnderworldWeatherDamageKind.Blunt,
                    10d * eventIntensity,
                    4d,
                    .30d + eventIntensity * .25d,
                    1.35d);

            case UnderworldAtmosphereEvent.BlackBloom:
                return new UnderworldWeatherGameplayState(
                    UnderworldWeatherDamageKind.Poison,
                    Math.Max(state.DamagePerPulse, 7d * eventIntensity * Math.Max(.45d, exposure)),
                    4.5d,
                    1d,
                    .60d,
                    1d + .35d * eventIntensity);

            default:
                return state;
        }
    }

    private static UnderworldWeatherGameplayState Damage(
        UnderworldWeatherDamageKind kind,
        double damage,
        double interval,
        double chance,
        double stagger) =>
        new(kind, Math.Max(0d, damage), interval, Clamp01(chance), stagger, 1d);

    private static UnderworldWeatherGameplayState Merge(
        UnderworldWeatherGameplayState baseline,
        UnderworldWeatherGameplayState weather)
    {
        if (!weather.HasDamage) return baseline;
        if (!baseline.HasDamage) return weather;

        // Prefer the event's explicit damage language. Add baseline atmospheric burden rather than
        // creating two independent pulses that could double-proc in the same frame.
        return weather with { DamagePerPulse = weather.DamagePerPulse + baseline.DamagePerPulse };
    }

    private static double Clamp01(double value) => Math.Max(0d, Math.Min(1d, value));
}
