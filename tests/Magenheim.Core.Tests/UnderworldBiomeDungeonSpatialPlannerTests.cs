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
        foreach (var room in first.Rooms)
            Assert(points.Add(room.X.ToString("R") + "," + room.Z.ToString("R")),
                "Two rooms cannot occupy the same physical grid point.");

        return assertions;
    }
}
