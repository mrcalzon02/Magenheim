using System;
using System.Linq;
using Magenheim.Core.Underworld;

internal static class UnderworldStationCatalogTests
{
    public static int Run()
    {
        var assertions = 0;
        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition)
                throw new InvalidOperationException("Underworld station assertion " + assertions + " failed: " + message);
        }

        var all = UnderworldStationCatalog.All;
        Assert(all.Count == 6, "There must be exactly one foundational crafting station per canonical Underworld biome.");
        Assert(all.Select(value => value.Biome).Distinct().Count() == 6,
            "Each foundational station must belong to a different Underworld biome.");
        Assert(all.Select(value => value.Prefab).Distinct(StringComparer.Ordinal).Count() == 6,
            "Station prefab identities must be unique.");
        Assert(all.Select(value => value.ModelId).Distinct(StringComparer.Ordinal).Count() == 6,
            "Every station must have a unique owned model identity.");
        Assert(all.All(value => value.ModelId.StartsWith("underworld-station-", StringComparison.Ordinal)),
            "Every station must use the owned Underworld station model namespace.");
        Assert(all.All(value => value.Costs.Count >= 3),
            "Every station must consume a substantial biome material package.");
        Assert(all.All(value => value.Costs.All(cost =>
            UnderworldResourceCatalog.All.Any(resource =>
                string.Equals(resource.Prefab, cost.ResourcePrefab, StringComparison.Ordinal)))),
            "Every station construction cost must resolve to a canonical Underworld raw resource.");

        Assert(all[0].Prefab == UnderworldStationCatalog.MycelialBenchPrefab &&
               all[0].BuildStationPrefab.Length == 0,
            "The Mycelial Bench must be the ungated first foothold.");
        for (var index = 1; index < all.Count; index++)
            Assert(all[index].BuildStationPrefab == all[index - 1].Prefab,
                all[index].Name + " must require the previous tier's station to construct.");

        Assert(all.Single(value => value.Biome == UnderworldTerrainBiome.BlackwaterDeep).PlacementRule ==
               UnderworldStationPlacementRule.Shoreline,
            "The Tidal Basin must retain its shoreline siting intent.");
        Assert(all.Single(value => value.Biome == UnderworldTerrainBiome.SulfurousWastes).PlacementRule ==
               UnderworldStationPlacementRule.GeothermalVent,
            "The Furnace Heart Forge must retain its geothermal siting intent.");
        Assert(all.Single(value => value.Biome == UnderworldTerrainBiome.FractureZones).PlacementRule ==
               UnderworldStationPlacementRule.StableGround,
            "The Anchor Forge must retain its stable-ground siting intent.");
        Assert(all.Last().PlacementRule == UnderworldStationPlacementRule.DeepstoneAttuned,
            "The Crown Reliquary must retain its Deepstone-attuned endgame identity.");

        foreach (var weapon in UnderworldWeaponUpgradeCatalog.All)
            Assert(all.Any(station => station.Prefab == weapon.StationPrefab),
                weapon.Name + " must use a canonical Underworld station rather than an orphan station id.");

        return assertions;
    }
}
