using System;
using Magenheim.Core;
using Magenheim.Core.Definitions;
using Magenheim.Core.Networking;
using Magenheim.Core.Transactions;
using Magenheim.Core.Worldgen;

internal static class GeodeOpeningTransactionTests
{
    public static int Run()
    {
        var assertions = 0;
        var geode = CreateGeode();
        var compatible = new DefinitionAuthorityResult(
            DefinitionAuthorityStatus.Compatible,
            true,
            "compatible");

        var ready = GeodeOpeningTransactionPlanner.Plan(new GeodeOpeningTransactionRequest(
            compatible,
            1,
            3,
            new GeodeCrackingRequest(geode, 0d, 0d, new[] { 0d, 0d, 0d })));
        Assert(ready.IsReady, "Compatible authority with source and capacity should produce a ready transaction plan.", ref assertions);
        Assert(ready.ConsumeGeodeCount == 1, "A ready plan must consume exactly one source geode.", ref assertions);
        Assert(ready.GrantCrystals.Count == 3, "Successful bonus rolls must be represented exactly in the planned grants.", ref assertions);
        Assert(ready.GrantCrystals[0].Tier == CrystalTier.Rough, "Transaction planning must preserve geode cracking tier authority.", ref assertions);

        var pending = GeodeOpeningTransactionPlanner.Plan(new GeodeOpeningTransactionRequest(
            DefinitionAuthorityResult.Pending,
            1,
            3,
            new GeodeCrackingRequest(geode, 0d, 0d, new[] { 0d, 0d, 0d })));
        Assert(pending.Outcome == GeodeOpeningPlanOutcome.AuthorityDenied, "Pending definition authority must fail closed.", ref assertions);
        Assert(pending.ConsumeGeodeCount == 0 && pending.GrantCrystals.Count == 0, "Authority denial must never emit a partial mutation plan.", ref assertions);

        var inconsistentAuthority = GeodeOpeningTransactionPlanner.Plan(new GeodeOpeningTransactionRequest(
            new DefinitionAuthorityResult(DefinitionAuthorityStatus.Pending, true, "forged/inconsistent"),
            1,
            3,
            new GeodeCrackingRequest(geode, 0d, 0d, new[] { 0d, 0d, 0d })));
        Assert(inconsistentAuthority.Outcome == GeodeOpeningPlanOutcome.AuthorityDenied, "MutationAuthorized alone must not bypass compatible-status admission.", ref assertions);

        var missingSource = GeodeOpeningTransactionPlanner.Plan(new GeodeOpeningTransactionRequest(
            compatible,
            0,
            3,
            new GeodeCrackingRequest(geode, 0d, 0d, new[] { 0d, 0d, 0d })));
        Assert(missingSource.Outcome == GeodeOpeningPlanOutcome.MissingSource, "Opening without a source geode must be rejected before mutation.", ref assertions);
        Assert(missingSource.ConsumeGeodeCount == 0, "Missing source must not schedule consumption.", ref assertions);

        var noCapacity = GeodeOpeningTransactionPlanner.Plan(new GeodeOpeningTransactionRequest(
            compatible,
            1,
            2,
            new GeodeCrackingRequest(geode, 0d, 0d, new[] { 0d, 0d, 0d })));
        Assert(noCapacity.Outcome == GeodeOpeningPlanOutcome.InsufficientOutputCapacity, "A three-crystal result must reject two-slot capacity.", ref assertions);
        Assert(noCapacity.GrantCrystals.Count == 0, "Capacity failure must not expose a partial grant plan.", ref assertions);

        var exactCapacity = GeodeOpeningTransactionPlanner.Plan(new GeodeOpeningTransactionRequest(
            compatible,
            1,
            1,
            new GeodeCrackingRequest(geode, 0.35d, 0.10d, new[] { 0d, 0.5d, 0.9d })));
        Assert(exactCapacity.IsReady && exactCapacity.GrantCrystals.Count == 1, "Exact capacity must admit a guaranteed-only result.", ref assertions);

        var malformedCracking = GeodeOpeningTransactionPlanner.Plan(new GeodeOpeningTransactionRequest(
            compatible,
            1,
            3,
            new GeodeCrackingRequest(geode, 1d, 0d, new[] { 0d, 0d, 0d })));
        Assert(malformedCracking.Outcome == GeodeOpeningPlanOutcome.InvalidCrackingRequest, "Malformed cracking input must fail before a transaction plan is emitted.", ref assertions);
        Assert(malformedCracking.ConsumeGeodeCount == 0 && malformedCracking.GrantCrystals.Count == 0, "Invalid cracking must remain non-mutating.", ref assertions);

        return assertions;
    }

    private static GeodeDefinition CreateGeode() =>
        new(
            "magenheim.geode.meadows.earth",
            "Meadows",
            "Magenheim_Geode_Meadows_Earth",
            SpawnArea.All,
            1,
            0.35d,
            0.10d,
            new[] { new ElementWeight(ElementalAlignment.Earth, 100d) });

    private static void Assert(bool condition, string message, ref int assertions)
    {
        assertions++;
        if (!condition)
            throw new InvalidOperationException($"Geode opening transaction assertion {assertions} failed: {message}");
    }
}
