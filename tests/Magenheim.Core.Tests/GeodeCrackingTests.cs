using System;
using Magenheim.Core;
using Magenheim.Core.Definitions;
using Magenheim.Core.Worldgen;

internal static class GeodeCrackingTests
{
    public static int Run()
    {
        var assertions = 0;

        var earthOnly = CreateGeode(0.35d, 0.10d,
            new[] { new ElementWeight(ElementalAlignment.Earth, 100d) });

        var guaranteedOnly = GeodeCrackingService.Crack(new GeodeCrackingRequest(
            earthOnly,
            0.35d,
            0.10d,
            new[] { 0d, 0.5d, 0.999999d }));
        Assert(guaranteedOnly.IsSuccess, "Boundary rolls equal to bonus chances must not create bonus crystals.", ref assertions);
        Assert(guaranteedOnly.Crystals.Count == 1, "Meadows cracking must always produce exactly one guaranteed crystal before bonuses.", ref assertions);
        Assert(guaranteedOnly.Crystals[0] == new Crystal(ElementalAlignment.Earth, CrystalTier.Rough), "The guaranteed Meadows crystal must preserve configured Earth alignment and Rough tier.", ref assertions);

        var secondOnly = GeodeCrackingService.Crack(new GeodeCrackingRequest(
            earthOnly,
            0.349999d,
            0.10d,
            new[] { 0d, 0.5d, 0.999999d }));
        Assert(secondOnly.Crystals.Count == 2, "Second-crystal chance must use an independent strict-less-than roll.", ref assertions);

        var thirdOnly = GeodeCrackingService.Crack(new GeodeCrackingRequest(
            earthOnly,
            0.35d,
            0.099999d,
            new[] { 0d, 0.5d, 0.999999d }));
        Assert(thirdOnly.Crystals.Count == 2, "Third-crystal chance must be independent of the second-crystal result.", ref assertions);

        var bothBonuses = GeodeCrackingService.Crack(new GeodeCrackingRequest(
            earthOnly,
            0d,
            0d,
            new[] { 0d, 0.5d, 0.999999d }));
        Assert(bothBonuses.Crystals.Count == 3, "Both successful bonus rolls must yield three total crystals.", ref assertions);
        Assert(bothBonuses.Crystals[2].Tier == CrystalTier.Rough, "Every crystal produced by geode cracking must begin at Rough tier.", ref assertions);

        var mixed = CreateGeode(1d, 1d,
            new[]
            {
                new ElementWeight(ElementalAlignment.Earth, 1d),
                new ElementWeight(ElementalAlignment.Fire, 1d),
                new ElementWeight(ElementalAlignment.Frost, 2d),
            });
        var mixedResult = GeodeCrackingService.Crack(new GeodeCrackingRequest(
            mixed,
            0d,
            0d,
            new[] { 0.249999d, 0.25d, 0.50d }));
        Assert(mixedResult.Crystals[0].Element == ElementalAlignment.Earth, "Weighted selection must keep values below the first cumulative boundary in the first element.", ref assertions);
        Assert(mixedResult.Crystals[1].Element == ElementalAlignment.Fire, "Weighted selection must move to the next element exactly at the cumulative boundary.", ref assertions);
        Assert(mixedResult.Crystals[2].Element == ElementalAlignment.Frost, "Each produced crystal must consume its own element roll rather than sharing one alignment roll.", ref assertions);

        var wrongElementRollCount = GeodeCrackingService.Crack(new GeodeCrackingRequest(
            earthOnly,
            0.9d,
            0.9d,
            new[] { 0d }));
        Assert(wrongElementRollCount.Outcome == GeodeCrackingOutcome.InvalidRequest, "Deterministic cracking must require all three schema element rolls even when no bonus crystal materializes.", ref assertions);
        Assert(wrongElementRollCount.Crystals.Count == 0, "Invalid cracking requests must not emit partial crystal results.", ref assertions);

        var invalidBonusRoll = GeodeCrackingService.Crack(new GeodeCrackingRequest(
            earthOnly,
            1d,
            0d,
            new[] { 0d, 0d, 0d }));
        Assert(invalidBonusRoll.Outcome == GeodeCrackingOutcome.InvalidRequest, "A bonus roll of 1.0 must be rejected because runtime rolls are defined on [0,1).", ref assertions);

        var invalidElementRoll = GeodeCrackingService.Crack(new GeodeCrackingRequest(
            earthOnly,
            0d,
            0d,
            new[] { 0d, double.NaN, 0d }));
        Assert(invalidElementRoll.Outcome == GeodeCrackingOutcome.InvalidRequest, "Non-finite element rolls must fail before any outcome is produced.", ref assertions);

        var invalidGuaranteedCount = CreateGeode(0.35d, 0.10d,
            new[] { new ElementWeight(ElementalAlignment.Earth, 100d) }) with
        {
            GuaranteedCrystalCount = 2,
        };
        var invalidDefinition = GeodeCrackingService.Crack(new GeodeCrackingRequest(
            invalidGuaranteedCount,
            0d,
            0d,
            new[] { 0d, 0d, 0d }));
        Assert(invalidDefinition.Outcome == GeodeCrackingOutcome.InvalidDefinition, "Cracking must reject definitions that violate the one-guaranteed-crystal schema invariant.", ref assertions);

        var invalidWeight = CreateGeode(0.35d, 0.10d,
            new[] { new ElementWeight(ElementalAlignment.Earth, 0d) });
        var invalidWeightResult = GeodeCrackingService.Crack(new GeodeCrackingRequest(
            invalidWeight,
            0d,
            0d,
            new[] { 0d, 0d, 0d }));
        Assert(invalidWeightResult.Outcome == GeodeCrackingOutcome.InvalidDefinition, "Cracking must fail closed on non-positive elemental weights.", ref assertions);

        return assertions;
    }

    private static GeodeDefinition CreateGeode(
        double secondChance,
        double thirdChance,
        ElementWeight[] weights) =>
        new(
            "magenheim.geode.meadows.earth",
            "Meadows",
            "Magenheim_Geode_Meadows_Earth",
            SpawnArea.All,
            1,
            secondChance,
            thirdChance,
            weights);

    private static void Assert(bool condition, string message, ref int assertions)
    {
        assertions++;
        if (!condition)
            throw new InvalidOperationException($"Geode cracking assertion {assertions} failed: {message}");
    }
}
