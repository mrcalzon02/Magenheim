using System;
using System.Collections.Generic;
using System.Linq;
using Magenheim.Core.Underworld;

internal static class UnderworldBiomeDungeonSpatialPlannerTests
{
    public static int Run()
    {
        var assertions = 0;
        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition)
                throw new InvalidOperationException("Underworld biome dungeon spatial planner: " + message);
        }

        UnderworldFungalRootwarrenCatalog.Validate();
        var topology = UnderworldBiomeDungeonPlanner.Build(
            UnderworldDungeonCatalog.FungalForest,
            0x24681357,
            UnderworldFungalRootwarrenCatalog.RoomFamilyIds());
        var first = UnderworldBiomeDungeonSpatialPlanner.Build(
            topology,
            UnderworldFungalRootwarrenCatalog.Rooms);
        var second = UnderworldBiomeDungeonSpatialPlanner.Build(
            topology,
            UnderworldFungalRootwarrenCatalog.Rooms);

        Assert(first.Rooms.SequenceEqual(second.Rooms),
            "Same topology must embed identically.");
        Assert(first.Connections.SequenceEqual(second.Connections),
            "Same topology must route identical physical corridors.");
        Assert(first.CellSizeMeters >= 80d,
            "Large Rootwarren rooms need a conservative physical spacing envelope.");
        Assert(first.Rooms[0].X == 0d && first.Rooms[0].Z == 0d,
            "Entrance room must remain the spatial origin.");
        Assert(first.Rooms.Skip(1).Any(room => room.Y < 0d),
            "Rootwarren must descend rather than remaining one flat plane.");

        var defs = UnderworldFungalRootwarrenCatalog.Rooms
            .ToDictionary(value => value.Id, StringComparer.Ordinal);
        UnderworldBiomeDungeonSpatialPlanner.Validate(first, defs);

        var points = new HashSet<string>(StringComparer.Ordinal);
        var occupied = new HashSet<string>(StringComparer.Ordinal);
        foreach (var room in first.Rooms)
        {
            Assert(points.Add(room.X.ToString("R") + "," + room.Z.ToString("R")),
                "Two rooms cannot occupy the same physical grid point.");
            occupied.Add(room.GridX + "," + room.GridZ);
        }

        Assert(first.Connections.Count == topology.Connections.Count,
            "Every abstract dungeon edge must own one routed physical corridor.");
        foreach (var connection in first.Connections)
        {
            Assert(connection.Waypoints.Count >= 2,
                "Every routed corridor must contain start/end waypoints.");
            for (var index = 1; index < connection.Waypoints.Count - 1; index++)
            {
                var waypoint = connection.Waypoints[index];
                Assert(!occupied.Contains(waypoint.GridX + "," + waypoint.GridZ),
                    "Intermediate corridor cells must route around occupied room cells.");
            }
        }

        var foundDetour = false;
        for (var seed = 1; seed <= 24 && !foundDetour; seed++)
        {
            var candidateTopology = UnderworldBiomeDungeonPlanner.Build(
                UnderworldDungeonCatalog.FungalForest,
                seed,
                UnderworldFungalRootwarrenCatalog.RoomFamilyIds());
            var candidateSpatial = UnderworldBiomeDungeonSpatialPlanner.Build(
                candidateTopology,
                UnderworldFungalRootwarrenCatalog.Rooms);
            foundDetour = candidateSpatial.Connections.Any(value => value.Waypoints.Count > 2);
        }
        Assert(foundDetour,
            "Sampled Rootwarren plans must exercise routed detours rather than only straight adjacency.");

        return assertions;
    }
}
