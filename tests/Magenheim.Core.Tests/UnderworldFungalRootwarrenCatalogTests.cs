using System;
using System.Linq;
using Magenheim.Core.Underworld;

internal static class UnderworldFungalRootwarrenCatalogTests
{
    public static int Run()
    {
        var assertions = 0;
        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition)
                throw new InvalidOperationException("Fungal Rootwarren catalog: " + message);
        }

        UnderworldFungalRootwarrenCatalog.Validate();
        var rooms = UnderworldFungalRootwarrenCatalog.Rooms;

        Assert(rooms.Count == 16,
            "Rootwarren production kit should currently contain sixteen large room families.");
        Assert(rooms.Select(value => value.Id).Distinct(StringComparer.Ordinal).Count() == rooms.Count,
            "Room ids must be unique.");
        Assert(rooms.Select(value => value.ModelId).Distinct(StringComparer.Ordinal).Count() == rooms.Count,
            "Room model ids must be unique.");
        Assert(rooms.All(value => value.WidthMeters >= 22d &&
                                  value.DepthMeters >= 32d &&
                                  value.HeightMeters >= 14d),
            "Rootwarren rooms must remain genuinely large gameplay spaces.");

        var plan = UnderworldBiomeDungeonPlanner.Build(
            UnderworldDungeonCatalog.FungalForest,
            0x13572468,
            UnderworldFungalRootwarrenCatalog.RoomFamilyIds());

        Assert(plan.Rooms.Count >= 32 && plan.Rooms.Count <= 48,
            "Sixteen room families reused 2-3 times should produce 32-48 major room placements.");
        Assert(plan.Connections.Count(value => value.IsLoop) > 0,
            "Rootwarren topology must include cross-links.");
        Assert(plan.Rooms.Select(value => value.Branch).Distinct().Count() > 2,
            "Rootwarren topology must branch.");

        return assertions;
    }
}
