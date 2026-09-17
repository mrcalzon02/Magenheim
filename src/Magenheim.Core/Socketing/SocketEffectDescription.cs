using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Magenheim.Core.Socketing;

/// <summary>
/// Player-facing description of what a crystal does in each kind of slot.
///
/// Reported from play: a crystal's description does not say what it does in a weapon, in
/// armour, or in a utility item, so the player cannot tell before committing the socket.
///
/// The text is generated from the same validated <see cref="SocketEffectDefinitionSet"/> the
/// runtime calculates with, rather than written by hand beside it. Hand-written effect text
/// drifts the moment balance changes, and a description that lies about a number is worse
/// than one that omits it.
/// </summary>
public static class SocketEffectDescription
{
    /// <summary>Label and unit for each effect, in the player's language rather than the enum's.</summary>
    private static readonly IReadOnlyDictionary<SocketEffectKind, (string Label, string Unit)> Wording =
        new Dictionary<SocketEffectKind, (string, string)>
        {
            [SocketEffectKind.BluntDamage] = ("blunt damage", ""),
            [SocketEffectKind.FireDamage] = ("fire damage", ""),
            [SocketEffectKind.FrostDamage] = ("frost damage", ""),
            [SocketEffectKind.LightningDamage] = ("lightning damage", ""),
            [SocketEffectKind.PoisonDamage] = ("poison damage", ""),
            [SocketEffectKind.SpiritDamage] = ("spirit damage", ""),
            [SocketEffectKind.Armor] = ("armor", ""),
            [SocketEffectKind.Knockback] = ("knockback", ""),
            [SocketEffectKind.Stagger] = ("stagger", ""),
            [SocketEffectKind.KnockbackResistance] = ("knockback resistance", ""),
            [SocketEffectKind.CarryWeight] = ("carry weight", ""),
            [SocketEffectKind.MiningEfficiency] = ("mining speed", "%"),
            [SocketEffectKind.MovementSpeed] = ("movement speed", "%"),
            [SocketEffectKind.StaminaRegeneration] = ("stamina regeneration", "%"),
            [SocketEffectKind.EitrRegeneration] = ("eitr regeneration", "%"),
            [SocketEffectKind.HealthRegeneration] = ("health regeneration", "%"),
            [SocketEffectKind.Illumination] = ("light", ""),
        };

    /// <summary>The slots a player can actually socket, in the order they are described.</summary>
    private static readonly EquipmentCategory[] Slots =
    {
        EquipmentCategory.Weapon,
        EquipmentCategory.Armor,
        EquipmentCategory.Shield,
        EquipmentCategory.Tool,
        EquipmentCategory.Utility,
    };

    private static string SlotName(EquipmentCategory category) => category switch
    {
        EquipmentCategory.Weapon => "Weapon",
        EquipmentCategory.Armor => "Armor",
        EquipmentCategory.Shield => "Shield",
        EquipmentCategory.Tool => "Tool",
        EquipmentCategory.Utility => "Utility",
        _ => category.ToString()
    };

    private static string Amount(double value, string unit)
    {
        // Whole numbers read as whole numbers; fractions keep one place. Either way the sign
        // is explicit, because a socket can reduce a stat as well as raise it.
        var rounded = Math.Round(value, 1, MidpointRounding.AwayFromZero);
        var text = Math.Abs(rounded - Math.Truncate(rounded)) < 0.05d
            ? Math.Truncate(rounded).ToString("0", CultureInfo.InvariantCulture)
            : rounded.ToString("0.0", CultureInfo.InvariantCulture);
        return (rounded >= 0 ? "+" : "") + text + unit;
    }

    /// <summary>
    /// One line per slot that the crystal affects, at this crystal's own tier. Slots the
    /// crystal does nothing for are omitted rather than listed as no effect, so the line the
    /// player reads is the line that matters.
    /// </summary>
    public static IReadOnlyList<string> Lines(
        ElementalAlignment element,
        CrystalTier tier,
        SocketEffectDefinitionSet definitions)
    {
        if (definitions is null) throw new ArgumentNullException(nameof(definitions));
        if (tier == CrystalTier.Rough)
            return new[] { "Too rough to socket. Refine it first." };

        var scalar = SocketEffectDefinitionSet.TierScalar(tier);
        var lines = new List<string>();
        foreach (var slot in Slots)
        {
            var effects = definitions.Rules
                .Where(rule => rule.Element == element && rule.Category == slot)
                .OrderBy(rule => (int)rule.Effect)
                .Select(rule =>
                {
                    var wording = Wording.TryGetValue(rule.Effect, out var found)
                        ? found
                        : (Label: rule.Effect.ToString(), Unit: string.Empty);
                    return Amount(rule.SimpleMagnitude * scalar, wording.Unit) + " " + wording.Label;
                })
                .ToArray();
            if (effects.Length > 0)
                lines.Add(SlotName(slot) + ": " + string.Join(", ", effects));
        }

        if (lines.Count == 0)
            lines.Add("No socket effect is defined for this crystal.");
        return lines;
    }

    /// <summary>The same content as a single block for an item tooltip.</summary>
    public static string Tooltip(
        ElementalAlignment element,
        CrystalTier tier,
        SocketEffectDefinitionSet definitions)
        => string.Join("\n", Lines(element, tier, definitions));
}
