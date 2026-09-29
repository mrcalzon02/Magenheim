using System;
using System.Linq;
using Magenheim.Core.Underworld;

internal static class UnderworldSulfurCinderworksCatalogTests
{
    public static int Run()
    {
        var assertions=0;
        void Check(bool condition,string message)
        {
            assertions++;
            if(!condition)throw new InvalidOperationException(
                "Cinderworks assertion "+assertions+" failed: "+message);
        }

        UnderworldSulfurCinderworksCatalog.Validate();
        var rooms=UnderworldSulfurCinderworksCatalog.Rooms;
        Check(rooms.Count==16,"Cinderworks must own sixteen room families.");
        Check(rooms.Select(x=>x.Room.Id).Distinct(StringComparer.Ordinal).Count()==16,
            "Cinderworks room ids must be unique.");
        Check(rooms.Select(x=>x.Room.ModelId).Distinct(StringComparer.Ordinal).Count()==16,
            "Cinderworks model ids must be unique.");
        Check(rooms.Count(x=>x.HeatRoute==UnderworldCinderworksHeatRoute.Safe&&x.HasRecoveryPocket)>=2,
            "Cinderworks needs multiple true recovery rooms.");
        Check(rooms.Any(x=>x.HeatRoute==UnderworldCinderworksHeatRoute.VentCycle),
            "Cinderworks must contain vent-cycle routing.");
        Check(rooms.Any(x=>x.HeatRoute==UnderworldCinderworksHeatRoute.SlagChannel),
            "Cinderworks must contain slag-channel routing.");
        Check(rooms.Where(x=>x.FurnaceBloodAdvantage).All(x=>x.ThermalPressure01>=.35d),
            "Furnace Blood advantages belong only on meaningful thermal pressure.");
        Check(rooms.Where(x=>x.HeatRoute==UnderworldCinderworksHeatRoute.Safe)
            .All(x=>!x.FurnaceBloodAdvantage),
            "Safe rooms must not be boon-gated.");

        var plan=UnderworldBiomeDungeonPlanner.Build(
            UnderworldDungeonCatalog.SulfurousWastes,
            0x51C1D3,
            UnderworldSulfurCinderworksCatalog.RoomFamilyIds(),
            UnderworldSulfurCinderworksCatalog.EntranceRoomId);
        Check(plan.Rooms[0].RoomFamilyId==UnderworldSulfurCinderworksCatalog.EntranceRoomId,
            "Cinder Gate must remain the deterministic root family.");
        var spatial=UnderworldBiomeDungeonSpatialPlanner.Build(
            plan,
            UnderworldSulfurCinderworksCatalog.SpatialDefinitions());
        Check(spatial.Rooms.Count==plan.Rooms.Count,
            "Cinderworks topology and spatial plans must retain identical room counts.");

        return assertions;
    }
}
