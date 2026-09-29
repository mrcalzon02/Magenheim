using System;
using System.Linq;
using Magenheim.Core.Underworld;

internal static class UnderworldFrozenRimeSepulcherCatalogTests
{
    public static int Run()
    {
        var assertions=0;
        void Check(bool value,string message)
        {
            assertions++;
            if(!value)throw new InvalidOperationException(
                "Rime Sepulcher assertion "+assertions+" failed: "+message);
        }

        UnderworldFrozenRimeSepulcherCatalog.Validate();
        var rooms=UnderworldFrozenRimeSepulcherCatalog.Rooms;
        Check(rooms.Count==16,"Rime Sepulcher must own sixteen room families.");
        Check(rooms.Count(x=>x.Route==UnderworldRimeSepulcherRoute.Shelter)>=2,
            "Frozen dungeon requires multiple low-exposure shelters.");
        Check(rooms.Any(x=>x.Route==UnderworldRimeSepulcherRoute.WhiteoutChoke&&x.WhiteoutIntensity01>=.7),
            "Frozen dungeon requires a severe but mitigatable whiteout route.");
        Check(rooms.Any(x=>x.Route==UnderworldRimeSepulcherRoute.ClearGallery&&x.Room.DepthMeters>=50),
            "Frozen dungeon must preserve at least one long readable sightline.");
        Check(rooms.Where(x=>x.Route==UnderworldRimeSepulcherRoute.Shelter)
            .All(x=>!x.RimeboundAdvantage&&x.ColdExposure01<=.12),
            "Shelters must remain true non-boon recovery rooms.");

        var plan=UnderworldBiomeDungeonPlanner.Build(
            UnderworldDungeonCatalog.FrozenCaverns,
            0x71CE51,
            UnderworldFrozenRimeSepulcherCatalog.RoomFamilyIds(),
            UnderworldFrozenRimeSepulcherCatalog.EntranceRoomId);
        Check(plan.Rooms[0].RoomFamilyId==UnderworldFrozenRimeSepulcherCatalog.EntranceRoomId,
            "Rime Mouth must remain the deterministic root.");
        var spatial=UnderworldBiomeDungeonSpatialPlanner.Build(
            plan,
            UnderworldFrozenRimeSepulcherCatalog.SpatialDefinitions());
        Check(spatial.Rooms.Count==plan.Rooms.Count,
            "Rime topology/spatial plans must retain identical room counts.");
        return assertions;
    }
}
