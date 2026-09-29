using System;
using System.Linq;
using Magenheim.Core.Underworld;

internal static class UnderworldDungeonCatalogTests
{
    public static int Run()
    {
        var assertions = 0;
        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition)
                throw new InvalidOperationException("Underworld dungeon catalog: " + message);
        }

        UnderworldDungeonCatalog.Validate();
        var all = UnderworldDungeonCatalog.All;

        Assert(all.Count == 6, "There must be exactly one dungeon program per canonical biome.");
        Assert(all.Select(value => value.Biome).Distinct().Count() == 6,
            "Dungeon programs must cover all six Underworld biomes exactly once.");
        Assert(all.Select(value => value.Id).Distinct(StringComparer.Ordinal).Count() == all.Count,
            "Dungeon ids must remain unique.");
        Assert(all.Select(value => value.PrefabName).Distinct(StringComparer.Ordinal).Count() == all.Count,
            "Dungeon prefab identities must remain unique.");

        var fracture = UnderworldDungeonCatalog.DeepFracture;
        Assert(fracture.Biome == UnderworldTerrainBiome.FractureZones,
            "Deep Fracture must remain a Fracture Zones dungeon.");
        Assert(fracture.Status == UnderworldDungeonStatus.RuntimeReady,
            "Deep Fracture must remain runtime-admitted while ordinary biome dungeons advance independently.");
        Assert(fracture.Quantity == 6 && fracture.MinDistanceFromSimilarMeters >= 1200d,
            "Underworld Deep Fractures must remain sparse destinations.");
        Assert(fracture.TargetRoomFamilyMinimum == 20 &&
               fracture.TargetRoomFamilyMaximum == 20 &&
               fracture.MinimumRoomFamilyUses == 1 &&
               fracture.MaximumRoomFamilyUses == 1,
            "The established exact-plan Deep Fracture must not be silently converted into the generic reusable-kit generator.");

        var ordinary = all
            .Where(value => !string.Equals(
                value.Id,
                fracture.Id,
                StringComparison.Ordinal))
            .ToArray();
        Assert(ordinary.Length == 5,
            "Five ordinary biome dungeon programs must exist alongside Deep Fracture.");
        foreach (var dungeon in ordinary)
        {
            Assert(dungeon.TargetRoomFamilyMinimum == 15 &&
                   dungeon.TargetRoomFamilyMaximum == 20,
                dungeon.DisplayName + " must retain the 15-20 large-room-family production target.");
            Assert(dungeon.MinimumRoomFamilyUses == 2 &&
                   dungeon.MaximumRoomFamilyUses == 3,
                dungeon.DisplayName + " must retain the 2-3 uses-per-family run target.");
        }

        Assert(all.Count(value => value.Status == UnderworldDungeonStatus.RuntimeReady) >= 1,
            "At least Deep Fracture must remain runtime-ready while ordinary dungeons are promoted independently.");

        return assertions;
    }
}
