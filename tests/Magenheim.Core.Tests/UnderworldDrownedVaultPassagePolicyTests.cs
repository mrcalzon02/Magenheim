using System;
using Magenheim.Core.Underworld;

internal static class UnderworldDrownedVaultPassagePolicyTests
{
    public static int Run()
    {
        var assertions=0;
        void Assert(bool value,string message)
        {
            assertions++;
            if(!value)throw new InvalidOperationException("Drowned Vault passage policy: "+message);
        }

        var rooms=UnderworldBlackwaterDrownedVaultsCatalog.Rooms;
        foreach(var from in rooms)
        foreach(var to in rooms)
        {
            var forward=UnderworldDrownedVaultPassagePolicy.Resolve(from,to);
            var reverse=UnderworldDrownedVaultPassagePolicy.Resolve(to,from);
            forward.Validate();
            Assert(forward==reverse,"transition policy must be symmetric.");

            if(from.RouteMode==UnderworldDrownedVaultRouteMode.Dry &&
               to.RouteMode==UnderworldDrownedVaultRouteMode.Dry)
                Assert(!forward.HasWater&&forward.RouteMode==UnderworldDrownedVaultRouteMode.Dry,
                    "dry-to-dry connections must stay dry.");

            var submergedFrom=from.RouteMode is UnderworldDrownedVaultRouteMode.Flooded or UnderworldDrownedVaultRouteMode.VerticalWater;
            var submergedTo=to.RouteMode is UnderworldDrownedVaultRouteMode.Flooded or UnderworldDrownedVaultRouteMode.VerticalWater;
            if(submergedFrom&&submergedTo)
                Assert(forward.RouteMode==UnderworldDrownedVaultRouteMode.Flooded &&
                       forward.WaterDepthMeters==UnderworldDrownedVaultPassagePolicy.FloodedWaterDepthMeters &&
                       !forward.HasDryLedge,
                    "two submerged endpoints require a fully flooded connection.");
            else if(!(from.RouteMode==UnderworldDrownedVaultRouteMode.Dry &&
                      to.RouteMode==UnderworldDrownedVaultRouteMode.Dry))
                Assert(forward.RouteMode==UnderworldDrownedVaultRouteMode.Mixed &&
                       forward.HasDryLedge &&
                       forward.WaterDepthMeters==UnderworldDrownedVaultPassagePolicy.MixedWaterDepthMeters,
                    "all other wet transitions must preserve the adaptive dry side ledge.");
        }

        return assertions;
    }
}
