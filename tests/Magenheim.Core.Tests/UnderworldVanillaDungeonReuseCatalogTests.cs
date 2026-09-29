using System;
using System.Linq;
using Magenheim.Core.Underworld;

internal static class UnderworldVanillaDungeonReuseCatalogTests
{
    public static int Run()
    {
        var assertions = 0;
        void Check(bool value, string message)
        {
            assertions++;
            if (!value)
                throw new InvalidOperationException("Underworld vanilla dungeon reuse: " + message);
        }

        UnderworldVanillaDungeonReuseCatalog.Validate();
        var all = UnderworldVanillaDungeonReuseCatalog.All;

        Check(all.Count == 5, "Five ordinary biome donor families are required.");
        Check(all.All(x => x.LinearRoomScale >= 1.5d),
            "Every donor room family must scale to at least 1.5x linear size.");
        Check(all.All(x => x.RoomCountMultiplier >= 3.5d),
            "Every ordinary dungeon must target at least 3.5x donor room count.");
        Check(all.All(x => x.ReplaceVanillaEnemies && x.ReplaceVanillaLoot && x.UseBiomeResources),
            "Vanilla population/loot may not leak into Underworld donor dungeons.");
        Check(all.All(x => !x.AllowMagenheimAuthoredRoomInjection),
            "Ordinary donor dungeons must stay architecturally vanilla-derived.");
        Check(all.Select(x => x.DonorGeneratorPrefab).Distinct(StringComparer.Ordinal).Count() == 5,
            "Each ordinary biome currently uses a distinct vanilla generator family.");

        Check(UnderworldVanillaDungeonReuseCatalog.FungalForest.DonorGeneratorPrefab == "DG_ForestCrypt" &&
              UnderworldVanillaDungeonReuseCatalog.FungalForest.DonorEntrancePrefabs.Contains("Crypt2"),
            "Fungal Forest must reuse Burial Chamber architecture.");
        Check(UnderworldVanillaDungeonReuseCatalog.BlackwaterDeep.DonorGeneratorPrefab == "DG_SunkenCrypt",
            "Blackwater Deep must reuse Sunken Crypt architecture.");
        Check(UnderworldVanillaDungeonReuseCatalog.SulfurousWastes.DonorGeneratorPrefab == "DG_DvergrTown",
            "Sulfurous Wastes must reuse Infested Mine architecture.");
        Check(UnderworldVanillaDungeonReuseCatalog.FrozenCaverns.DonorGeneratorPrefab == "DG_Cave",
            "Frozen Caverns must reuse Frost Cave architecture.");
        Check(UnderworldVanillaDungeonReuseCatalog.GreatDecay.DonorGeneratorPrefab == "DG_Hole",
            "Great Decay must reuse Winding Tunnel architecture.");

        var fungal = UnderworldVanillaDungeonReuseCatalog.FungalForest;
        Check(fungal.ExpandedMinimumRooms(20) == 70,
            "A vanilla 20-room minimum must expand to 70 rooms at the 3.5x floor.");
        Check(fungal.ExpandedMaximumRooms(40) == 140,
            "A vanilla 40-room maximum must expand to 140 rooms at the 3.5x floor.");

        return assertions;
    }
}
