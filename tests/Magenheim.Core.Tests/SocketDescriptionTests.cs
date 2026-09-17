using System;
using System.Linq;
using Magenheim.Core;
using Magenheim.Core.Socketing;

internal static class SocketDescriptionTests
{
    public static int Run()
    {
        var assertions = 0;

        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition)
                throw new InvalidOperationException($"Socket description assertion {assertions} failed: {message}");
        }

        var definitions = new SocketEffectDefinitionSet(new[]
        {
            new SocketEffectRule(ElementalAlignment.Fire, EquipmentCategory.Weapon, SocketEffectKind.FireDamage, 8d),
            new SocketEffectRule(ElementalAlignment.Fire, EquipmentCategory.Armor, SocketEffectKind.Armor, 2d),
            new SocketEffectRule(ElementalAlignment.Fire, EquipmentCategory.Utility, SocketEffectKind.MovementSpeed, 1.5d),
            new SocketEffectRule(ElementalAlignment.Frost, EquipmentCategory.Weapon, SocketEffectKind.FrostDamage, 6d),
        });

        var simple = SocketEffectDescription.Lines(ElementalAlignment.Fire, CrystalTier.Simple, definitions);
        Assert(simple.Count == 3, "Fire affects weapon, armor and utility, so three lines are described.");
        Assert(simple[0] == "Weapon: +8 fire damage", "Weapon line must name the slot, amount and effect: " + simple[0]);
        Assert(simple[1] == "Armor: +2 armor", "Armor line must read in the player's language: " + simple[1]);
        Assert(simple[2] == "Utility: +1.5% movement speed", "Percentage effects must carry their unit: " + simple[2]);

        // The number shown is this crystal's own number, not a base the player must scale.
        var master = SocketEffectDescription.Lines(ElementalAlignment.Fire, CrystalTier.Master, definitions);
        Assert(master[0] == "Weapon: +28 fire damage",
            "Master scales the weapon effect by 3.5: " + master[0]);
        Assert(master[2] == "Utility: +5.3% movement speed",
            "Fractional results keep one decimal place: " + master[2]);

        // Slots the crystal does nothing for are omitted, not listed as no effect.
        var frost = SocketEffectDescription.Lines(ElementalAlignment.Frost, CrystalTier.Crystal, definitions);
        Assert(frost.Count == 1 && frost[0] == "Weapon: +9 frost damage",
            "Frost defines only a weapon effect, so only that slot is described.");
        Assert(frost.All(line => !line.Contains("Armor")), "An undefined slot must not appear at all.");

        // Rough crystals are not socketable; say so rather than showing an empty effect list.
        var rough = SocketEffectDescription.Lines(ElementalAlignment.Fire, CrystalTier.Rough, definitions);
        Assert(rough.Count == 1 && rough[0].Contains("Refine"),
            "Rough crystals must explain that they need refining: " + rough[0]);

        // An element with no rules at all must still produce something truthful.
        var silent = SocketEffectDescription.Lines(ElementalAlignment.Spirit, CrystalTier.Simple, definitions);
        Assert(silent.Count == 1 && silent[0].Contains("No socket effect"),
            "An element with no rules must say so rather than render blank.");

        var tooltip = SocketEffectDescription.Tooltip(ElementalAlignment.Fire, CrystalTier.Simple, definitions);
        Assert(tooltip.Split('\n').Length == 3, "The tooltip joins one line per described slot.");

        Assert(Throws(() => SocketEffectDescription.Lines(ElementalAlignment.Fire, CrystalTier.Simple, null!)),
            "A missing definition set must be rejected rather than silently described.");

        return assertions;
    }

    private static bool Throws(Action action)
    {
        try { action(); return false; }
        catch (ArgumentException) { return true; }
    }
}
