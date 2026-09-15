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
    /// Tier power is deliberately ordinal and independent of any individual combat statistic.
    /// Effect definitions convert this normalized progression power into their own magnitude.
    /// </summary>
    public static double TierPower(CrystalTier tier) => tier switch
    {
        CrystalTier.Rough => 1d,
        CrystalTier.Simple => 2d,
        CrystalTier.Crystal => 3d,
        CrystalTier.Advanced => 4d,
        CrystalTier.Master => 5d,
        _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, "Unknown crystal tier."),
    };

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
