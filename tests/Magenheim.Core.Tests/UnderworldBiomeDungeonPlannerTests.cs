using System;
using System.Linq;
using Magenheim.Core.Underworld;

internal static class UnderworldBiomeDungeonPlannerTests
{
    public static int Run()
    {
        var assertions = 0;
        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition)
                throw new InvalidOperationException("Underworld biome dungeon planner: " + message);
        }

        var definition = UnderworldDungeonCatalog.FungalForest;
        var families = Enumerable.Range(1, 16)
            .Select(index => "fungal-room-" + index.ToString("00"))
            .ToArray();

        var first = UnderworldBiomeDungeonPlanner.Build(definition, 12345, families);
        var second = UnderworldBiomeDungeonPlanner.Build(definition, 12345, families);
        Assert(first.Rooms.SequenceEqual(second.Rooms),
            "Same seed and room kit must produce identical room placement order.");
        var rooted = UnderworldBiomeDungeonPlanner.Build(
            definition,
            12345,
            families,
            families[7]);
        Assert(rooted.Rooms[0].RoomFamilyId == families[7] &&
               rooted.Rooms[0].FamilyUseIndex == 1,
            "An authored entrance family must be pinned to the dungeon root without changing its reuse contract.");

        Assert(first.Connections.SequenceEqual(second.Connections),
            "Same seed and room kit must produce identical topology.");

        var different = UnderworldBiomeDungeonPlanner.Build(definition, 54321, families);
        Assert(!first.Rooms.SequenceEqual(different.Rooms) ||
               !first.Connections.SequenceEqual(different.Connections),
            "Different seeds must materially vary room order or topology.");

        Assert(first.Rooms.Count >= families.Length * definition.MinimumRoomFamilyUses,
            "Generated room count must honor minimum family reuse.");
        Assert(first.Rooms.Count <= families.Length * definition.MaximumRoomFamilyUses,
            "Generated room count must honor maximum family reuse.");
        foreach (var family in families)
        {
            var uses = first.Rooms.Count(room => room.RoomFamilyId == family);
            Assert(uses >= 2 && uses <= 3,
                family + " must appear two or three times.");
        }

        Assert(first.Rooms.Skip(1).All(room => room.Depth > 0),
            "Every non-entrance room must be below the root depth.");
        Assert(first.Rooms.Select(room => room.Branch).Distinct().Count() > 2,
            "Generated topology must branch rather than form one corridor chain.");
        Assert(first.Connections.Count(connection => connection.IsLoop) > 0,
            "Large biome dungeons must contain at least one cross-link/loop.");
        Assert(first.Connections.Count(connection => !connection.IsLoop) == first.Rooms.Count - 1,
            "The non-loop topology must form one connected rooted tree.");

        var rejectedSmallKit = false;
        try
        {
            UnderworldBiomeDungeonPlanner.Build(definition, 1, families.Take(14));
        }
        catch (InvalidOperationException)
        {
            rejectedSmallKit = true;
        }
        Assert(rejectedSmallKit,
            "A room kit below the 15-family production floor must fail closed.");

        var rejectedDeepFracture = false;
        try
        {
            UnderworldBiomeDungeonPlanner.Build(
                UnderworldDungeonCatalog.DeepFracture,
                1,
                Enumerable.Range(1, 20).Select(index => "df-" + index));
        }
        catch (InvalidOperationException)
        {
            rejectedDeepFracture = true;
        }
        Assert(rejectedDeepFracture,
            "Generic planner must never replace the bespoke Deep Fracture expedition.");

        return assertions;
    }
}
