using System;
using System.Collections.Generic;
using System.Linq;

namespace Magenheim.Core.Socketing;

public sealed record ElementalResonance(
    ElementalAlignment Element,
    double EffectiveTierPower,
    IReadOnlyList<Crystal> Crystals);

/// <summary>
/// Collapses a multi-socket crystal set into one numerical resonance per elemental family.
/// Behavioral runtime code must instantiate at most one effect processor for each returned
/// element. Standalone one-crystal Magenheim naturally resolves to the first multiplier only.
/// </summary>
public static class CrystalResonance
{
    public static IReadOnlyList<ElementalResonance> Calculate(
        IEnumerable<Crystal> crystals,
        IReadOnlyList<float> ordinalMultipliers)
    {
        if (crystals is null) throw new ArgumentNullException(nameof(crystals));
        ValidateMultipliers(ordinalMultipliers);

        return crystals
            .GroupBy(crystal => crystal.Element)
            .OrderBy(group => group.Key)
            .Select(group => CalculateElement(group.Key, group, ordinalMultipliers))
            .ToArray();
    }

    public static double MultiplierForOrdinal(int zeroBasedOrdinal, IReadOnlyList<float> ordinalMultipliers)
    {
        ValidateMultipliers(ordinalMultipliers);
        if (zeroBasedOrdinal < 0)
            throw new ArgumentOutOfRangeException(nameof(zeroBasedOrdinal));
        return ordinalMultipliers[Math.Min(zeroBasedOrdinal, ordinalMultipliers.Count - 1)];
    }

    private static ElementalResonance CalculateElement(
        ElementalAlignment element,
        IEnumerable<Crystal> crystals,
        IReadOnlyList<float> ordinalMultipliers)
    {
        var ordered = crystals
            .OrderByDescending(crystal => TierPower(crystal.Tier))
            .ThenByDescending(crystal => crystal.Tier)
            .ToArray();

        double effectivePower = 0d;
        for (var index = 0; index < ordered.Length; index++)
            effectivePower += TierPower(ordered[index].Tier) * MultiplierForOrdinal(index, ordinalMultipliers);

        return new ElementalResonance(element, effectivePower, ordered);
    }

    /// <summary>
    /// Resonance uses the same tier scalar authority as ordinary Magenheim socket effects so a
    /// single crystal is mathematically identical in native and Jewelcrafting-backed equipment.
    /// Rough crystals remain non-socketable and are rejected rather than assigned effect power.
    /// </summary>
    public static double TierPower(CrystalTier tier) => SocketEffectDefinitionSet.TierScalar(tier);

    private static void ValidateMultipliers(IReadOnlyList<float> multipliers)
    {
        if (multipliers is null) throw new ArgumentNullException(nameof(multipliers));
        if (multipliers.Count == 0)
            throw new ArgumentException("At least one resonance multiplier is required.", nameof(multipliers));

        for (var index = 0; index < multipliers.Count; index++)
        {
            var multiplier = multipliers[index];
            if (float.IsNaN(multiplier) || float.IsInfinity(multiplier) || multiplier < 0f)
                throw new ArgumentException($"Resonance multiplier at index {index} must be non-negative and finite.", nameof(multipliers));
        }
    }
}
