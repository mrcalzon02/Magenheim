using System;
using System.Collections.Generic;
using System.Linq;

namespace Magenheim.Core.Socketing;

/// <summary>
/// Immutable activation supplied to proc-style runtime effects. One activation represents one
/// elemental processor for the entire item, regardless of how many physical sockets contributed.
/// </summary>
public sealed record BehavioralResonanceActivation(
    ElementalAlignment Element,
    double EffectiveTierPower,
    CrystalTier HighestTier,
    int ContributingCrystalCount);

/// <summary>
/// Converts numerical crystal resonance into the authoritative behavioral-effect boundary.
/// Runtime effects such as Storm chaining or Venom contamination must iterate this plan rather
/// than raw sockets, preventing Jewelcrafting multi-slot equipment from spawning duplicate proc
/// processors while still allowing additional same-element crystals to strengthen one processor.
/// </summary>
public static class BehavioralResonance
{
    public static IReadOnlyList<BehavioralResonanceActivation> Plan(
        IEnumerable<Crystal> crystals,
        IReadOnlyList<float> ordinalMultipliers)
    {
        if (crystals is null) throw new ArgumentNullException(nameof(crystals));

        return CrystalResonance.Calculate(crystals, ordinalMultipliers)
            .Select(resonance => new BehavioralResonanceActivation(
                resonance.Element,
                resonance.EffectiveTierPower,
                HighestTier(resonance.Crystals),
                resonance.Crystals.Count))
            .ToArray();
    }

    private static CrystalTier HighestTier(IReadOnlyList<Crystal> crystals)
    {
        if (crystals.Count == 0)
            throw new InvalidOperationException("Behavioral resonance cannot be created without a contributing crystal.");

        // CrystalResonance orders strongest tier first. Keeping the selection explicit makes this
        // boundary robust if its internal ordering changes later.
        return crystals
            .OrderByDescending(crystal => CrystalResonance.TierPower(crystal.Tier))
            .ThenByDescending(crystal => crystal.Tier)
            .First()
            .Tier;
    }
}
