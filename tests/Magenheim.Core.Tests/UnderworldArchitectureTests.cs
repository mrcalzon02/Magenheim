using System;
using System.Linq;
using Magenheim.Core.Underworld;

internal static class UnderworldArchitectureTests
{
    public static int Run()
    {
        var assertions = 0;

        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition)
                throw new InvalidOperationException(
                    $"Underworld architecture assertion {assertions} failed: {message}");
        }

        var catalog = UnderworldArchitectureCatalog.CreateInitialStructuralSlice();
        Assert(catalog.Pieces.Count == 14, "Rootforged structural slice must contain fourteen pieces.");
        Assert(catalog.Fingerprint.Length == 64, "Architecture fingerprint must be SHA-256 hex.");

        var beams = catalog.Pieces.Where(value => value.Kind == UnderworldBuildPieceKind.Beam).ToArray();
        Assert(beams.Length == 3, "Initial slice must contain three plain Worldroot beams.");
        Assert(beams.Any(value => value.Dimensions.WidthMeters == 2), "A 2m Worldroot beam is required.");
        Assert(beams.Any(value => value.Dimensions.WidthMeters == 4), "A 4m Worldroot beam is required.");
        Assert(beams.Any(value => value.Dimensions.WidthMeters == 8), "An 8m Worldroot beam is required.");

        var pillars = catalog.Pieces.Where(value => value.Kind == UnderworldBuildPieceKind.Pillar).ToArray();
        Assert(pillars.Length == 3, "Initial slice must contain three Worldroot pillars.");
        Assert(pillars.Any(value => value.Dimensions.HeightMeters == 2), "A 2m Worldroot pillar is required.");
        Assert(pillars.Any(value => value.Dimensions.HeightMeters == 4), "A 4m Worldroot pillar is required.");
        Assert(pillars.Any(value => value.Dimensions.HeightMeters == 8), "An 8m Worldroot pillar is required.");

        var ribs = catalog.Pieces.Where(value => value.Kind == UnderworldBuildPieceKind.ArchRib).ToArray();
        Assert(ribs.Length == 2, "Initial slice must contain 4m and 8m Rootforged arch ribs.");
        Assert(ribs.All(value => value.Tier == UnderworldBuildTier.RootforgedIron), "Initial arch ribs must belong to the Rootforged Iron tier.");
        Assert(!catalog.Pieces.Any(value => value.Tier == UnderworldBuildTier.Silverbound), "Silverbound construction must remain deferred from the first slice.");

        var junctions = catalog.Pieces.Where(value => value.Kind == UnderworldBuildPieceKind.YBrace
            || value.Kind == UnderworldBuildPieceKind.TBrace || value.Kind == UnderworldBuildPieceKind.ForkedColumn).ToArray();
        Assert(junctions.Length == 3, "Y/T braces and forked columns must all be available.");
        Assert(junctions.All(value => value.CraftingStationPrefabName == "piece_workbench"
            && value.Tier == UnderworldBuildTier.Rootstone && value.Costs.Count == 1
            && value.Costs[0].ResourceId == UnderworldArchitectureValidator.WorldrootTimberResourceId),
            "Unreinforced junctions must remain available through the existing timber/workbench recipe.");

        var reversed = UnderworldArchitectureValidator.ValidateAndFreeze(
            UnderworldArchitectureValidator.CurrentSchemaVersion,
            catalog.Pieces.Reverse());
        Assert(
            reversed.Fingerprint == catalog.Fingerprint,
            "Architecture fingerprint must be independent of piece source ordering.");

        var changedCosts = catalog.Pieces
            .Select(value => value.Id.EndsWith("worldroot_beam_4m", StringComparison.Ordinal)
                ? value with
                {
                    Costs = Array.AsReadOnly(new[]
                    {
                        new UnderworldBuildCost(UnderworldArchitectureValidator.WorldrootTimberResourceId, 5),
                    }),
                }
                : value)
            .ToArray();
        var changed = UnderworldArchitectureValidator.ValidateAndFreeze(
            UnderworldArchitectureValidator.CurrentSchemaVersion,
            changedCosts);
        Assert(
            changed.Fingerprint != catalog.Fingerprint,
            "Gameplay-significant build-cost changes must alter the architecture fingerprint.");

        AssertThrows(
            Assert,
            () => UnderworldArchitectureValidator.ValidateAndFreeze(
                UnderworldArchitectureValidator.CurrentSchemaVersion,
                catalog.Pieces.Concat(new[] { catalog.Pieces[0] })),
            "Duplicate build-piece identities must fail closed.");

        var foreignId = catalog.Pieces
            .Select((value, index) => index == 0 ? value with { Id = "foreign.build.foundation" } : value)
            .ToArray();
        AssertThrows(
            Assert,
            () => UnderworldArchitectureValidator.ValidateAndFreeze(
                UnderworldArchitectureValidator.CurrentSchemaVersion,
                foreignId),
            "Build-piece identities outside the Magenheim Underworld namespace must be rejected.");

        var foreignPrefab = catalog.Pieces
            .Select((value, index) => index == 0 ? value with { PrefabName = "ForeignFoundation" } : value)
            .ToArray();
        AssertThrows(
            Assert,
            () => UnderworldArchitectureValidator.ValidateAndFreeze(
                UnderworldArchitectureValidator.CurrentSchemaVersion,
                foreignPrefab),
            "Runtime prefab identities outside the Magenheim Underworld namespace must be rejected.");

        var invalidDimensions = catalog.Pieces
            .Select((value, index) => index == 0
                ? value with { Dimensions = new UnderworldBuildDimensions(0, 1, 2) }
                : value)
            .ToArray();
        AssertThrows(
            Assert,
            () => UnderworldArchitectureValidator.ValidateAndFreeze(
                UnderworldArchitectureValidator.CurrentSchemaVersion,
                invalidDimensions),
            "Zero or negative structural dimensions must be rejected.");

        var invalidCost = catalog.Pieces
            .Select((value, index) => index == 0
                ? value with
                {
                    Costs = Array.AsReadOnly(new[]
                    {
                        new UnderworldBuildCost(UnderworldArchitectureValidator.UnderstoneResourceId, 0),
                    }),
                }
                : value)
            .ToArray();
        AssertThrows(
            Assert,
            () => UnderworldArchitectureValidator.ValidateAndFreeze(
                UnderworldArchitectureValidator.CurrentSchemaVersion,
                invalidCost),
            "Zero or negative build costs must be rejected.");

        var duplicateCost = catalog.Pieces
            .Select((value, index) => index == 0
                ? value with
                {
                    Costs = Array.AsReadOnly(new[]
                    {
                        new UnderworldBuildCost(UnderworldArchitectureValidator.UnderstoneResourceId, 3),
                        new UnderworldBuildCost(UnderworldArchitectureValidator.UnderstoneResourceId, 3),
                    }),
                }
                : value)
            .ToArray();
        AssertThrows(
            Assert,
            () => UnderworldArchitectureValidator.ValidateAndFreeze(
                UnderworldArchitectureValidator.CurrentSchemaVersion,
                duplicateCost),
            "Duplicate resource-cost identities on one build piece must be rejected.");

        return assertions;
    }

    private static void AssertThrows(
        Action<bool, string> assert,
        Action action,
        string message)
    {
        var threw = false;
        try
        {
            action();
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        assert(threw, message);
    }
}
