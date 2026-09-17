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
    /// <summary>
    /// Where socketing happens. Socketing is owned solely by the Crystal Enchanting Dais:
    /// opening a slot, installing a crystal and removing one all happen there, and nowhere
    /// else. The Geologist's Workstation refines crystals and crafts with them; it does not
    /// socket. Stated on the crystal itself because the crystal is what the player is holding
    /// when they wonder where to take it.
    /// </summary>
    public const string StationLine = "Socket at the Crystal Enchanting Dais.";

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
            return new[] { "Too rough to socket. Refine it at the Geologist's Workstation first." };

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
        lines.Add(StationLine);
        return lines;
    }

    /// <summary>
    /// What this crystal would do in one specific slot, or null when it does nothing there.
    /// The socket menu shows this against the item the player has actually selected, so the
    /// choice is made against the effect that will apply rather than against a list of all
    /// five slots.
    /// </summary>
    public static string? ForSlot(
        ElementalAlignment element,
        CrystalTier tier,
        EquipmentCategory category,
        SocketEffectDefinitionSet definitions)
    {
        if (definitions is null) throw new ArgumentNullException(nameof(definitions));
        if (tier == CrystalTier.Rough) return null;

        var scalar = SocketEffectDefinitionSet.TierScalar(tier);
        var effects = definitions.Rules
            .Where(rule => rule.Element == element && rule.Category == category)
            .OrderBy(rule => (int)rule.Effect)
            .Select(rule =>
            {
                var wording = Wording.TryGetValue(rule.Effect, out var found)
                    ? found
                    : (Label: rule.Effect.ToString(), Unit: string.Empty);
                return Amount(rule.SimpleMagnitude * scalar, wording.Unit) + " " + wording.Label;
            })
            .ToArray();
        return effects.Length == 0 ? null : string.Join(", ", effects);
    }

    /// <summary>The same content as a single block for an item tooltip.</summary>
    public static string Tooltip(
        ElementalAlignment element,
        CrystalTier tier,
        SocketEffectDefinitionSet definitions)
        => string.Join("\n", Lines(element, tier, definitions));
}
