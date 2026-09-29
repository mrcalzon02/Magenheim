using System;
using System.Collections.Generic;
using System.Linq;
using Magenheim.Core.Underworld;

internal static class UnderworldEquipmentCatalogTests
{
    public static int Run()
    {
        var assertions = 0;
        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition)
                throw new InvalidOperationException("Underworld equipment assertion " + assertions + " failed: " + message);
        }

        var tools = UnderworldEquipmentCatalog.Tools;
        var armour = UnderworldEquipmentCatalog.Armour;
        var all = UnderworldEquipmentCatalog.All;

        Assert(tools.Count == 6, "Each biome requires one interaction-opening progression tool.");
        Assert(armour.Count == 24, "Each biome requires a four-piece armour set.");
        Assert(all.Count == 30, "Six tools plus twenty-four armour pieces make the canonical equipment program.");
        Assert(all.Select(value => value.Prefab).Distinct(StringComparer.Ordinal).Count() == all.Count,
            "Equipment prefab identities must be unique.");
        Assert(all.Select(value => value.ModelId).Distinct(StringComparer.Ordinal).Count() == all.Count,
            "Equipment model identities must be unique.");
        Assert(tools.Select(value => value.Biome).Distinct().Count() == 6,
            "Tools must cover all six Underworld biomes.");
        Assert(tools.Single(value => value.Prefab == "Magenheim_Underworld_Tool_DivingBellHood").Slot == UnderworldEquipmentSlot.Helmet,
            "The Diving Bell Hood is a wearable helmet utility, not a fake held Tool-slot item.");
        Assert(tools.Where(value => value.Prefab != "Magenheim_Underworld_Tool_DivingBellHood")
            .All(value => value.Slot == UnderworldEquipmentSlot.Tool),
            "The other five biome tools remain held/deployable Tool-slot equipment.");

        foreach (var station in UnderworldStationCatalog.All)
        {
            Assert(all.Any(value => value.StationPrefab == station.Prefab),
                station.Name + " must have equipment to produce.");
        }

        foreach (UnderworldTerrainBiome biome in Enum.GetValues(typeof(UnderworldTerrainBiome)))
        {
            var set = armour.Where(value => value.Biome == biome).ToArray();
            Assert(set.Length == 4, biome + " must have four armour pieces.");
            Assert(set.Any(value => value.Slot == UnderworldEquipmentSlot.Helmet), biome + " helmet missing.");
            Assert(set.Any(value => value.Slot == UnderworldEquipmentSlot.Chest), biome + " chest missing.");
            Assert(set.Any(value => value.Slot == UnderworldEquipmentSlot.Legs), biome + " legs missing.");
            Assert(set.Any(value => value.Slot == UnderworldEquipmentSlot.Cape), biome + " cape missing.");
        }

        var raw = new HashSet<string>(UnderworldResourceCatalog.All.Select(value => value.Prefab), StringComparer.Ordinal);
        var fungalRefined = new HashSet<string>(UnderworldFungalRefinementCatalog.All.Select(value => value.Prefab), StringComparer.Ordinal);
        var biomeRefined = new HashSet<string>(UnderworldBiomeRefinementCatalog.All.Select(value => value.Prefab), StringComparer.Ordinal);
        Assert(all.SelectMany(value => value.Costs).All(value =>
                raw.Contains(value.Prefab) || fungalRefined.Contains(value.Prefab) || biomeRefined.Contains(value.Prefab)),
            "Every admitted equipment dependency must resolve to a canonical raw or implemented refined material.");
        Assert(all.Where(value => value.Biome != UnderworldTerrainBiome.FungalForest)
            .SelectMany(value => value.Costs).All(value => biomeRefined.Contains(value.Prefab)),
            "Every post-Fungal equipment recipe must consume processed station output rather than raw biome pickups.");

        Assert(UnderworldEquipmentCatalog.FungalForestSlice.Count == 5,
            "Fungal Forest first playable equipment slice must be one tool plus four armour pieces.");
        Assert(UnderworldEquipmentCatalog.FungalForestSlice.All(value =>
            value.StationPrefab == UnderworldStationCatalog.MycelialBenchPrefab),
            "Every Fungal Forest equipment recipe must be gated by the Mycelial Bench.");

        return assertions;
    }
}
