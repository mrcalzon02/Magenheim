using System;
using System.Linq;
using Magenheim.Core.Underworld;

internal static class UnderworldCinderworksThermalRoutePolicyTests
{
    public static int Run()
    {
        var assertions=0;
        void Check(bool condition,string message)
        {
            assertions++;
            if(!condition)throw new InvalidOperationException(
                "Cinderworks thermal route assertion "+assertions+" failed: "+message);
        }

        foreach(var room in UnderworldSulfurCinderworksCatalog.Rooms)
        {
            var state=UnderworldCinderworksThermalRoutePolicy.Resolve(room);
            state.Validate();
            if(room.HeatRoute==UnderworldCinderworksHeatRoute.Safe)
                Check(!state.HasHazard,"Safe rooms must not own geothermal hazard volume.");
            else
                Check(state.HasHazard,"Every non-safe route must resolve to geothermal pressure.");
        }

        var slag=UnderworldCinderworksThermalRoutePolicy.Resolve(
            UnderworldSulfurCinderworksCatalog.Rooms.First(x=>
                x.HeatRoute==UnderworldCinderworksHeatRoute.SlagChannel));
        Check(slag.HazardId==UnderworldGeothermalHazard.LavaChannel,
            "Slag channels must use canonical lava-channel heat.");

        var cycle=UnderworldCinderworksThermalRoutePolicy.Resolve(
            UnderworldSulfurCinderworksCatalog.Rooms.First(x=>
                x.HeatRoute==UnderworldCinderworksHeatRoute.VentCycle));
        Check(cycle.Cycles,"Vent-cycle rooms must have a timed hazard window.");
        Check(cycle.ActiveSeconds<cycle.PeriodSeconds,
            "Vent-cycle hazard requires a real safe timing window.");

        var activeSamples=Enumerable.Range(0,180)
            .Count(second=>UnderworldCinderworksThermalRoutePolicy.IsActive(cycle,second,77));
        Check(activeSamples>0&&activeSamples<180,
            "Vent-cycle schedule must alternate active/inactive states.");

        var safeRoom=UnderworldSulfurCinderworksCatalog.Rooms.First(x=>
            x.HeatRoute==UnderworldCinderworksHeatRoute.Safe);
        var safe=UnderworldCinderworksThermalRoutePolicy.Resolve(safeRoom);
        Check(!UnderworldCinderworksThermalRoutePolicy.IsActive(safe,50d,12),
            "Hazard-free rooms can never activate.");

        var safePassage=UnderworldCinderworksThermalRoutePolicy.ResolvePassage(safeRoom,safeRoom);
        Check(!safePassage.HasHazard,
            "Safe-to-safe passage must preserve the recovery route.");

        var slagRoom=UnderworldSulfurCinderworksCatalog.Rooms.First(x=>
            x.HeatRoute==UnderworldCinderworksHeatRoute.SlagChannel);
        var slagPassage=UnderworldCinderworksThermalRoutePolicy.ResolvePassage(safeRoom,slagRoom);
        Check(slagPassage.HazardId==UnderworldGeothermalHazard.LavaChannel,
            "Any passage entering a slag route must use lava-channel pressure.");

        var ventRoom=UnderworldSulfurCinderworksCatalog.Rooms.First(x=>
            x.HeatRoute==UnderworldCinderworksHeatRoute.VentCycle);
        var ventPassage=UnderworldCinderworksThermalRoutePolicy.ResolvePassage(safeRoom,ventRoom);
        Check(ventPassage.Cycles,
            "Passage connected to a vent-cycle room must preserve timing gameplay.");

        return assertions;
    }
}
