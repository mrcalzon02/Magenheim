using System;
using System.Linq;
using Magenheim.Core;
using Magenheim.Core.Socketing;

internal static class SocketEffectResolverTests
{
    internal static int Run()
    {
        var assertions = 0;

        void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            assertions++;
        }

        var definitions = new[]
        {
            new SocketEffectDefinition(
                ElementalAlignment.Earth,
                CrystalTier.Simple,
                EquipmentCategory.Weapon,
                new[]
                {
                    new SocketEffectModifier("magenheim.test.weapon.force", 1.25d),
                    new SocketEffectModifier("magenheim.test.weapon.stagger", 0.10d),
                }),
            new SocketEffectDefinition(
                ElementalAlignment.Fire,
                CrystalTier.Advanced,
                EquipmentCategory.Weapon,
                new[]
                {
                    new SocketEffectModifier("magenheim.test.weapon.force", 2.75d),
                }),
            new SocketEffectDefinition(
                ElementalAlignment.Earth,
                CrystalTier.Simple,
                EquipmentCategory.Armor,
                new[]
                {
                    new SocketEffectModifier("magenheim.test.armor.protection", 0.05d),
                }),
        };

        var resolver = new SocketEffectResolver(definitions);
        var weaponState = new SocketState(2, new[]
        {
            new Crystal(ElementalAlignment.Earth, CrystalTier.Simple),
            new Crystal(ElementalAlignment.Fire, CrystalTier.Advanced),
        });
        var resolved = resolver.Resolve(EquipmentCategory.Weapon, weaponState);
        Assert(resolved.IsResolved && resolved.Outcome == SocketEffectResolutionOutcome.Resolved,
            "Known installed crystals should resolve against matching equipment-category definitions.");
        Assert(resolved.Effects.Count == 2,
            "Effect channels with the same namespaced id should aggregate rather than duplicate.");
        Assert(Math.Abs(resolved.Effects.Single(effect => effect.EffectId == "magenheim.test.weapon.force").Value - 4.0d) < 0.0000001d,
            "Matching effect channels should sum deterministically across installed crystals.");
        Assert(Math.Abs(resolved.Effects.Single(effect => effect.EffectId == "magenheim.test.weapon.stagger").Value - 0.10d) < 0.0000001d,
            "Independent effect channels should retain their supplied value.");

        var missing = resolver.Resolve(
            EquipmentCategory.Armor,
            new SocketState(1, new[] { new Crystal(ElementalAlignment.Fire, CrystalTier.Advanced) }));
        Assert(!missing.IsResolved && missing.Outcome == SocketEffectResolutionOutcome.MissingDefinition,
            "A missing element/tier/category definition must fail closed instead of guessing a foreign-item effect.");

        var unknown = resolver.Resolve(EquipmentCategory.Unknown, weaponState);
        Assert(!unknown.IsResolved && unknown.Outcome == SocketEffectResolutionOutcome.UnknownEquipmentCategory,
            "Unknown equipment must remain untouched until it is safely classified or explicitly included.");

        var empty = resolver.Resolve(EquipmentCategory.Utility, SocketState.Empty);
        Assert(empty.IsResolved && empty.Outcome == SocketEffectResolutionOutcome.NoInstalledCrystals && empty.Effects.Count == 0,
            "An eligible item with no installed crystals should resolve to no effects without error.");

        var duplicateRejected = false;
        try
        {
            _ = new SocketEffectResolver(new[]
            {
                definitions[0],
                definitions[0],
            });
        }
        catch (InvalidOperationException)
        {
            duplicateRejected = true;
        }
        Assert(duplicateRejected,
            "Duplicate element/tier/category effect definitions must be rejected at authority construction.");

        var roughRejected = false;
        try
        {
            _ = new SocketEffectResolver(new[]
            {
                new SocketEffectDefinition(
                    ElementalAlignment.Earth,
                    CrystalTier.Rough,
                    EquipmentCategory.Weapon,
                    new[] { new SocketEffectModifier("magenheim.test.invalid", 1d) }),
            });
        }
        catch (InvalidOperationException)
        {
            roughRejected = true;
        }
        Assert(roughRejected,
            "Rough crystals cannot carry socket effect definitions because they cannot be installed.");

        var foreignEffectRejected = false;
        try
        {
            _ = new SocketEffectResolver(new[]
            {
                new SocketEffectDefinition(
                    ElementalAlignment.Earth,
                    CrystalTier.Simple,
                    EquipmentCategory.Weapon,
                    new[] { new SocketEffectModifier("foreign.effect", 1d) }),
            });
        }
        catch (InvalidOperationException)
        {
            foreignEffectRejected = true;
        }
        Assert(foreignEffectRejected,
            "Socket effect ids must remain within the Magenheim namespace.");

        return assertions;
    }
}
