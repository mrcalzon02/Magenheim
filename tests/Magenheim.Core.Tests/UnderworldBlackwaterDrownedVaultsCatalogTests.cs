using System;
using System.Linq;
using Magenheim.Core.Underworld;

internal static class UnderworldBlackwaterDrownedVaultsCatalogTests
{
    public static int Run()
    {
        var assertions = 0;
        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition)
                throw new InvalidOperationException("Blackwater Drowned Vaults catalog: " + message);
        }

        UnderworldBlackwaterDrownedVaultsCatalog.Validate();
        var rooms = UnderworldBlackwaterDrownedVaultsCatalog.Rooms;

        Assert(rooms.Count == 16,
            "Drowned Vaults should begin with sixteen large reusable room families.");
        Assert(rooms.Count(value => value.RouteMode == UnderworldDrownedVaultRouteMode.Dry) >= 2,
            "Blackwater needs real dry-route alternatives.");
        Assert(rooms.Count(value => value.RouteMode == UnderworldDrownedVaultRouteMode.AirPocket) >= 2,
            "Blackwater needs deliberate air/recovery chambers.");
        Assert(rooms.Count(value => value.RouteMode == UnderworldDrownedVaultRouteMode.Flooded ||
                                    value.RouteMode == UnderworldDrownedVaultRouteMode.VerticalWater) >= 6,
            "Underwater traversal must be structurally significant.");
        Assert(rooms.All(value => value.Room.WidthMeters >= 26d &&
                                  value.Room.DepthMeters >= 32d &&
                                  value.Room.HeightMeters >= 18d),
            "Drowned Vault rooms must remain large gameplay spaces.");

        var topology = UnderworldBiomeDungeonPlanner.Build(
            UnderworldDungeonCatalog.BlackwaterDeep,
            unchecked((int)0xB1AC4A7Eu),
            UnderworldBlackwaterDrownedVaultsCatalog.RoomFamilyIds(),
            UnderworldBlackwaterDrownedVaultsCatalog.EntranceRoomId);
        Assert(topology.Rooms[0].RoomFamilyId == UnderworldBlackwaterDrownedVaultsCatalog.EntranceRoomId,
            "Drowned Vaults must always begin at the authored sinkhole family.");
        Assert(topology.Rooms.Count >= 32 && topology.Rooms.Count <= 48,
            "Sixteen room families reused 2-3 times should create 32-48 major placements.");
        Assert(topology.Connections.Any(value => value.IsLoop),
            "Drowned Vault topology needs cross-links for dry/wet route alternatives.");

        var spatial = UnderworldBiomeDungeonSpatialPlanner.Build(
            topology,
            UnderworldBlackwaterDrownedVaultsCatalog.SpatialDefinitions());
        Assert(spatial.Rooms.Count == topology.Rooms.Count,
            "Every planned Drowned Vault room must receive physical placement.");
        Assert(spatial.Connections.Count == topology.Connections.Count,
            "Every Drowned Vault topology edge must receive a physical routed connection.");

        var plannedModes = topology.Rooms
            .Select(value => UnderworldBlackwaterDrownedVaultsCatalog.Require(value.RoomFamilyId).RouteMode)
            .Distinct()
            .Count();
        Assert(plannedModes == Enum.GetValues(typeof(UnderworldDrownedVaultRouteMode)).Length,
            "Every generated Drowned Vault run must include all structural water route modes.");

        return assertions;
    }
}
