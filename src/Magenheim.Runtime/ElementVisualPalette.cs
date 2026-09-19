using System;
using System.Linq;
using Magenheim.Core;
using Magenheim.Core.Definitions;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Player-facing elemental presentation shared by crystal items and biome geodes.
/// Crystal families use strong element colors; geodes blend the actual weighted biome
/// distribution so mixed geodes communicate their local mineral character at a glance.
/// </summary>
internal static class ElementVisualPalette
{
    internal static Color Tint(ElementalAlignment element) => element switch
    {
        ElementalAlignment.Earth => new Color(0.66f, 0.43f, 0.22f, 1f),
        ElementalAlignment.Fire => new Color(0.95f, 0.24f, 0.08f, 1f),
        ElementalAlignment.Frost => new Color(0.42f, 0.82f, 1.00f, 1f),
        ElementalAlignment.Storm => new Color(0.40f, 0.48f, 1.00f, 1f),
        ElementalAlignment.Venom => new Color(0.38f, 0.80f, 0.20f, 1f),
        ElementalAlignment.Radiance => new Color(1.00f, 0.78f, 0.24f, 1f),
        ElementalAlignment.Seidr => new Color(0.66f, 0.30f, 0.96f, 1f),
        ElementalAlignment.Spirit => new Color(0.26f, 0.92f, 0.78f, 1f),
        _ => throw new ArgumentOutOfRangeException(nameof(element), element, "Unknown elemental alignment.")
    };

    internal static string Essence(ElementalAlignment element) => element switch
    {
        ElementalAlignment.Earth => "Its ochre facets carry weight, endurance, and the patient force of stone.",
        ElementalAlignment.Fire => "Its ember-red facets hold heat, violence, and the appetite of open flame.",
        ElementalAlignment.Frost => "Its pale-blue facets drink warmth from the air and hold the stillness of deep winter.",
        ElementalAlignment.Storm => "Its blue-violet facets prickle with pressure, charge, and the violence of a breaking sky.",
        ElementalAlignment.Venom => "Its green facets hold caustic life, rot, and the chemistry of things that survive by poisoning others.",
        ElementalAlignment.Radiance => "Its golden facets gather clean light and a sharp, sun-bright spiritual resonance.",
        ElementalAlignment.Seidr => "Its violet facets bend toward hidden currents of eitr, omen, and deliberate sorcery.",
        ElementalAlignment.Spirit => "Its blue-green facets feel strangely hollow, as though something unseen is listening through the stone.",
        _ => throw new ArgumentOutOfRangeException(nameof(element), element, "Unknown elemental alignment.")
    };

    internal static string Variant(ElementalAlignment element) =>
        element.ToString().ToLowerInvariant();

    internal static ElementalAlignment DominantElement(GeodeDefinition geode)
    {
        if (geode is null) throw new ArgumentNullException(nameof(geode));
        if (geode.ElementWeights.Count == 0)
            throw new InvalidOperationException($"Geode '{geode.Id}' has no elemental weights.");

        return geode.ElementWeights
            .OrderByDescending(weight => weight.Weight)
            .ThenBy(weight => (int)weight.Element)
            .First()
            .Element;
    }

    internal static Color GeodeTint(GeodeDefinition geode)
    {
        if (geode is null) throw new ArgumentNullException(nameof(geode));
        var total = geode.ElementWeights.Sum(weight => weight.Weight);
        if (total <= 0d || double.IsNaN(total) || double.IsInfinity(total))
            throw new InvalidOperationException($"Geode '{geode.Id}' has no positive finite elemental weight total.");

        var red = 0d;
        var green = 0d;
        var blue = 0d;
        foreach (var weight in geode.ElementWeights)
        {
            var color = Tint(weight.Element);
            var fraction = weight.Weight / total;
            red += color.r * fraction;
            green += color.g * fraction;
            blue += color.b * fraction;
        }

        // A weighted mean of several saturated hues lands near grey, which is why every biome's
        // geode read as the same dull mineral regardless of its elements. Bias the mean toward the
        // dominant element so the biome's signature mineral actually shows, then restore the
        // saturation the averaging removed. Hue still comes from the full weighted mix, so a geode
        // with a secondary element is still visibly different from one without it.
        var mean = new Color((float)red, (float)green, (float)blue, 1f);
        var signature = Tint(DominantElement(geode));
        Color.RGBToHSV(Color.Lerp(mean, signature, DominantBias), out var hue, out var saturation, out var value);
        return Color.HSVToRGB(
            hue,
            Mathf.Clamp01(saturation * SaturationGain + SaturationFloor),
            Mathf.Clamp(value, ValueFloor, ValueCeiling));
    }

    /// <summary>How far the averaged tint is pulled toward the dominant element's own colour.</summary>
    private const float DominantBias = 0.45f;
    private const float SaturationGain = 1.85f;
    private const float SaturationFloor = 0.14f;
    private const float ValueFloor = 0.52f;
    private const float ValueCeiling = 0.94f;

    internal static string GeodeVariant(GeodeDefinition geode) =>
        "geode-" + geode.Biome.ToLowerInvariant().Replace(" ", string.Empty);

    internal static string DisplayBiome(string biome) => biome switch
    {
        "BlackForest" => "Black Forest",
        "DeepNorth" => "Deep North",
        "Ashlands" => "Ashlands",
        "Mistlands" => "Mistlands",
        "Mountain" => "Mountain",
        "Meadows" => "Meadows",
        "Plains" => "Plains",
        "Swamp" => "Swamp",
        _ => biome
    };
}
