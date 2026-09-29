using System;
using System.Linq;
using Magenheim.Core.Underworld;

internal static class UnderworldWeaponUpgradeCatalogTests
{
    public static int Run()
    {
        var assertions = 0;
        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition)
                throw new InvalidOperationException("Underworld weapon upgrade assertion " + assertions + " failed: " + message);
        }

        var all = UnderworldWeaponUpgradeCatalog.All;
        Assert(all.Count == 12, "The six biome parity tiers must expose the twelve planned weapon identities.");
        Assert(all.Select(value => value.Prefab).Distinct(StringComparer.Ordinal).Count() == all.Count,
            "Underworld weapon prefab identities must be unique.");
        Assert(all.All(value =>
                value.BasePrefab.StartsWith("Magenheim_Weapon_Crystal", StringComparison.Ordinal) ||
                (value.BasePrefab.StartsWith("Magenheim_Staff_", StringComparison.Ordinal) &&
                 value.BasePrefab.EndsWith("_Crystal", StringComparison.Ordinal))),
            "Every Underworld weapon must descend from a Magenheim crystal-grade weapon or staff, never vanilla.");
        Assert(all.All(value => value.Ingredients.Count >= 3),
            "Every Underworld weapon must add a real biome-material package to its crystal chassis.");
        Assert(all.SelectMany(value => value.Ingredients)
                .All(cost => UnderworldResourceCatalog.All.Any(resource =>
                    string.Equals(resource.Prefab, cost.Prefab, StringComparison.Ordinal))),
            "Every Underworld weapon material dependency must resolve to the canonical Underworld resource catalog.");

        var fungal = UnderworldWeaponUpgradeCatalog.FungalForestSlice;
        Assert(fungal.Count == 2, "The first playable Fungal Forest slice is Worldroot Club plus Worldroot Bow.");
        Assert(fungal.All(value => value.StationPrefab == UnderworldWeaponUpgradeCatalog.MycelialBenchPrefab),
            "Both first-tier weapons must be gated by the Mycelial Bench.");
        Assert(fungal.Single(value => value.Name == "Worldroot Club").BasePrefab == "Magenheim_Weapon_CrystalMace",
            "Worldroot Club must visibly and mechanically descend from the Crystal Mace chassis.");
        Assert(fungal.Single(value => value.Name == "Worldroot Bow").BasePrefab == "Magenheim_Weapon_CrystalBow",
            "Worldroot Bow must visibly and mechanically descend from the Crystal Bow chassis.");

        Assert(all.Single(value => value.Name == "Amber Blade").BasePrefab == "Magenheim_Weapon_CrystalSword",
            "The Great Decay sword must consume and inherit the Crystal Sword.");
        Assert(all.Single(value => value.Name == "Emberiron Greatsword").BasePrefab == "Magenheim_Weapon_CrystalGreatsword",
            "The Sulfurous Wastes greatsword must consume and inherit the Crystal Greatsword.");
        Assert(all.Single(value => value.Name == "Rimesilver Spear").BasePrefab == "Magenheim_Weapon_CrystalSpear",
            "The Frozen Caverns spear must consume and inherit the Crystal Spear.");
        Assert(all.Single(value => value.Name == "Titanbone Atgeir").BasePrefab == "Magenheim_Weapon_CrystalAtgeir",
            "The Fracture Zones atgeir must consume and inherit the Crystal Atgeir.");
        Assert(all.Single(value => value.Name == "Shardstone Crossbow").BasePrefab == "Magenheim_Weapon_CrystalCrossbow",
            "The Fracture Zones crossbow must consume and inherit the Crystal Crossbow.");

        return assertions;
    }
}
