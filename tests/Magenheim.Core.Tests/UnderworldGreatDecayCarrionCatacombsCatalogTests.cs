using System;
using System.Linq;
using Magenheim.Core.Underworld;

internal static class UnderworldGreatDecayCarrionCatacombsCatalogTests
{
    public static int Run()
    {
        var assertions=0;
        void Check(bool value,string message)
        {
            assertions++;
            if(!value)throw new InvalidOperationException(
                "Carrion Catacombs assertion "+assertions+" failed: "+message);
        }

        UnderworldGreatDecayCarrionCatacombsCatalog.Validate();
        var rooms=UnderworldGreatDecayCarrionCatacombsCatalog.Rooms;
        Check(rooms.Count==16,"Carrion Catacombs must own sixteen room families.");
        Check(rooms.Count(x=>x.Route==UnderworldCarrionCatacombsRoute.Sanctuary&&x.HasWorksite)>=2,
            "Great Decay dungeon requires multiple Censer-friendly work/recovery rooms.");
        Check(rooms.Any(x=>x.Route==UnderworldCarrionCatacombsRoute.BlackBloom&&
                          x.BlackBloomIntensity01>=.8d),
            "Carrion Catacombs requires a severe suppressible Black Bloom route.");
        Check(rooms.Where(x=>x.Route==UnderworldCarrionCatacombsRoute.Sanctuary)
            .All(x=>!x.DefiantAdvantage&&x.Contamination01<=.14d),
            "Sanctuaries must remain true non-boon work/recovery spaces.");
        Check(rooms.Count(x=>x.DefiantAdvantage)>=8,
            "Defiant progression must materially affect a large fraction of routes.");

        var plan=UnderworldBiomeDungeonPlanner.Build(
            UnderworldDungeonCatalog.GreatDecay,
            0xCA7710,
            UnderworldGreatDecayCarrionCatacombsCatalog.RoomFamilyIds(),
            UnderworldGreatDecayCarrionCatacombsCatalog.EntranceRoomId);
        Check(plan.Rooms[0].RoomFamilyId==
              UnderworldGreatDecayCarrionCatacombsCatalog.EntranceRoomId,
            "Ossuary Gate must remain the deterministic root.");
        var spatial=UnderworldBiomeDungeonSpatialPlanner.Build(
            plan,
            UnderworldGreatDecayCarrionCatacombsCatalog.SpatialDefinitions());
        Check(spatial.Rooms.Count==plan.Rooms.Count,
            "Carrion topology/spatial plans must retain identical room counts.");
        return assertions;
    }
}
