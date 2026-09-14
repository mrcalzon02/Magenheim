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

        var request = new GeodeOpeningTransactionRequest(
            compatible,
            1,
            3,
            new GeodeCrackingRequest(geode, 0d, 0d, new[] { 0d, 0d, 0d }));

        var ready = GeodeOpeningTransactionPlanner.Plan(request);
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

        var guard = new GeodeOpeningOperationGuard();
        var key = new GeodeOpeningOperationKey(77L, 1L, "open-0001");
        var first = guard.Begin(key, request);
        Assert(first.MutationAuthorized, "A fresh valid operation key must authorize exactly one mutation attempt.", ref assertions);

        var duplicatePrepared = guard.Begin(key, request);
        Assert(duplicatePrepared.Outcome == GeodeOpeningOperationOutcome.DuplicatePrepared, "A replay before completion must be rejected as already prepared.", ref assertions);
        Assert(!duplicatePrepared.MutationAuthorized, "Duplicate prepared operations must never authorize a second mutation.", ref assertions);

        Assert(guard.MarkApplied(key), "The first prepared operation must transition to applied exactly once.", ref assertions);
        Assert(!guard.MarkApplied(key), "An applied operation must not transition to applied twice.", ref assertions);

        var duplicateApplied = guard.Begin(key, request);
        Assert(duplicateApplied.Outcome == GeodeOpeningOperationOutcome.DuplicateApplied, "A completed operation replay must be recognized as already applied.", ref assertions);
        Assert(!duplicateApplied.MutationAuthorized, "Applied replays must never authorize duplicate output.", ref assertions);

        var conflictingRequest = new GeodeOpeningTransactionRequest(
            compatible,
            1,
            1,
            new GeodeCrackingRequest(geode, 0.35d, 0.10d, new[] { 0d, 0.5d, 0.9d }));
        var conflict = guard.Begin(key, conflictingRequest);
        Assert(conflict.Outcome == GeodeOpeningOperationOutcome.ConflictingReplay, "Reusing an operation id for a different mutation plan must fail closed.", ref assertions);
        Assert(!conflict.MutationAuthorized, "Conflicting replays must never authorize mutation.", ref assertions);

        var retryKey = new GeodeOpeningOperationKey(77L, 1L, "open-0002");
        var retryFirst = guard.Begin(retryKey, request);
        Assert(retryFirst.MutationAuthorized, "A second fresh operation should prepare normally.", ref assertions);
        Assert(guard.AbortPrepared(retryKey), "A prepared operation with no mutation may be aborted for safe retry.", ref assertions);
        Assert(guard.Begin(retryKey, request).MutationAuthorized, "An aborted non-mutating reservation may be retried.", ref assertions);

        var staleSessionKey = new GeodeOpeningOperationKey(77L, 1L, "open-stale");
        Assert(guard.Begin(staleSessionKey, request).MutationAuthorized, "Old session setup must prepare before retirement coverage.", ref assertions);
        var retired = guard.RetirePeerSessions(77L, 2L);
        Assert(retired >= 1, "Starting a new peer session must retire old replay history for that routed peer id.", ref assertions);
        var newSession = guard.Begin(new GeodeOpeningOperationKey(77L, 2L, "open-stale"), request);
        Assert(newSession.MutationAuthorized, "The same client operation id in a new server session generation must be a distinct operation key.", ref assertions);

        var invalidGeneration = guard.Begin(new GeodeOpeningOperationKey(77L, 0L, "invalid"), request);
        Assert(invalidGeneration.Outcome == GeodeOpeningOperationOutcome.InvalidOperationKey, "Session generation zero must fail closed.", ref assertions);
        Assert(!invalidGeneration.MutationAuthorized, "Invalid operation keys must never authorize mutation.", ref assertions);

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
