using System;
using Magenheim.Core;
using Magenheim.Core.Networking;
using Magenheim.Core.Transactions;

internal static class RefinementTransactionTests
{
    internal static int Run()
    {
        var assertions = 0;
        void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            assertions++;
        }

        var service = new CrystalRefinementService(CrystalRefinementService.CreateCanonicalDefaults());
        var planner = new RefinementTransactionPlanner(service);
        var authority = new DefinitionAuthorityResult(
            DefinitionAuthorityStatus.Compatible, true, "test authority");

        var success = planner.Plan(new RefinementTransactionRequest(
            authority,
            1,
            1,
            new RefinementRequest(
                new Crystal(ElementalAlignment.Earth, CrystalTier.Rough),
                0,
                "Magenheim_GeologistWorkstation",
                0.50d)));
        Assert(success.IsReady, "Valid successful refinement transaction should be ready.");
        Assert(success.ConsumeSourceCount == 1, "Successful transaction must consume exactly one source.");
        Assert(success.GrantCrystal is { Element: ElementalAlignment.Earth, Tier: CrystalTier.Simple },
            "Successful transaction must grant the matching next-tier crystal.");
        Assert(success.GrantShardCount == 0 && success.AwardExperience,
            "Successful refinement must not grant failure shards and should award experience.");

        var failure = planner.Plan(new RefinementTransactionRequest(
            authority,
            1,
            1,
            new RefinementRequest(
                new Crystal(ElementalAlignment.Fire, CrystalTier.Simple),
                0,
                "Magenheim_StationUpgrade_FracturingBlock",
                0.05d)));
        Assert(failure.IsReady && failure.IsFailureOutcome,
            "Valid destructive refinement failure should still produce a ready transaction plan.");
        Assert(failure.ConsumeSourceCount == 1 && failure.GrantCrystal is null,
            "Destructive failure must consume exactly one source and grant no next-tier crystal.");
        Assert(failure.GrantShardCount == 2 && failure.ShardElement == ElementalAlignment.Fire,
            "Simple Fire failure must return exactly two matching shards.");
        Assert(!failure.AwardExperience, "Failed refinement must not receive the success experience reward (48241a5).");

        var denied = planner.Plan(new RefinementTransactionRequest(
            DefinitionAuthorityResult.Pending,
            1,
            1,
            new RefinementRequest(
                new Crystal(ElementalAlignment.Earth, CrystalTier.Rough),
                0,
                "Magenheim_GeologistWorkstation",
                0.50d)));
        Assert(denied.Outcome == RefinementTransactionPlanOutcome.AuthorityDenied && !denied.IsReady,
            "Unsynchronized definition authority must deny refinement mutation.");

        var noCapacity = planner.Plan(new RefinementTransactionRequest(
            authority,
            1,
            0,
            new RefinementRequest(
                new Crystal(ElementalAlignment.Earth, CrystalTier.Rough),
                0,
                "Magenheim_GeologistWorkstation",
                0.50d)));
        Assert(noCapacity.Outcome == RefinementTransactionPlanOutcome.InsufficientOutputCapacity,
            "A valid output with no post-consumption capacity must be rejected before mutation.");

        var wrongStation = planner.Plan(new RefinementTransactionRequest(
            authority,
            1,
            1,
            new RefinementRequest(
                new Crystal(ElementalAlignment.Earth, CrystalTier.Simple),
                0,
                "Magenheim_GeologistWorkstation",
                0.50d)));
        Assert(wrongStation.Outcome == RefinementTransactionPlanOutcome.RefinementRejected,
            "Wrong station/upgrade must reject the transaction without consuming inventory.");

        var guard = new RefinementOperationGuard(planner);
        var request = new RefinementTransactionRequest(
            authority,
            1,
            1,
            new RefinementRequest(
                new Crystal(ElementalAlignment.Earth, CrystalTier.Rough),
                0,
                "Magenheim_GeologistWorkstation",
                0.50d));
        var key = new RefinementOperationKey(42, 3, "refine-1");
        var fresh = guard.Begin(key, request);
        Assert(fresh.MutationAuthorized, "Fresh refinement operation should authorize one mutation attempt.");
        var duplicatePrepared = guard.Begin(key, request);
        Assert(duplicatePrepared.Outcome == RefinementOperationOutcome.DuplicatePrepared &&
               !duplicatePrepared.MutationAuthorized,
            "Prepared refinement replay must not authorize duplicate mutation.");
        Assert(guard.MarkApplied(key), "Fresh prepared refinement should be markable as applied exactly once.");
        Assert(!guard.MarkApplied(key), "Applied refinement must not be marked twice.");
        var duplicateApplied = guard.Begin(key, request);
        Assert(duplicateApplied.Outcome == RefinementOperationOutcome.DuplicateApplied &&
               !duplicateApplied.MutationAuthorized,
            "Applied refinement replay must not authorize duplicate mutation.");

        var postMutationReplay = request with
        {
            SourceCrystalCount = 0,
            AvailableOutputSlotsAfterConsumption = 0,
        };
        var duplicateAfterConsumption = guard.Begin(key, postMutationReplay);
        Assert(duplicateAfterConsumption.Outcome == RefinementOperationOutcome.DuplicateApplied &&
               !duplicateAfterConsumption.MutationAuthorized,
            "Applied refinement replay must remain identifiable after the source crystal is consumed.");
        Assert(duplicateAfterConsumption.Plan == fresh.Plan,
            "Applied refinement replay must return the stored original plan rather than replanning against post-mutation inventory.");

        var conflictKey = new RefinementOperationKey(42, 3, "refine-2");
        var first = guard.Begin(conflictKey, request);
        Assert(first.MutationAuthorized, "Second fresh key should prepare normally.");
        var conflictingRequest = request with
        {
            Refinement = request.Refinement with { Roll = 0.01d }
        };
        var conflict = guard.Begin(conflictKey, conflictingRequest);
        Assert(conflict.Outcome == RefinementOperationOutcome.ConflictingReplay,
            "Reusing an operation id with different immutable refinement intent must fail closed.");

        var conflictingAfterInventoryChange = conflictingRequest with
        {
            SourceCrystalCount = 0,
            AvailableOutputSlotsAfterConsumption = 0,
        };
        var postMutationConflict = guard.Begin(conflictKey, conflictingAfterInventoryChange);
        Assert(postMutationConflict.Outcome == RefinementOperationOutcome.ConflictingReplay,
            "Conflicting replay identity must be detected before mutable inventory state can mask it.");
        Assert(guard.AbortPrepared(conflictKey), "Unapplied prepared refinement should be abortable for retry.");

        var missingFreshKey = new RefinementOperationKey(42, 3, "refine-missing");
        var missingFresh = guard.Begin(missingFreshKey, request with { SourceCrystalCount = 0 });
        Assert(missingFresh.Outcome == RefinementOperationOutcome.PlanRejected &&
               missingFresh.Plan.Outcome == RefinementTransactionPlanOutcome.MissingSource,
            "A genuinely fresh operation with no source crystal must still fail normal transaction planning.");

        var staleKey = new RefinementOperationKey(77, 1, "old-session");
        Assert(guard.Begin(staleKey, request).MutationAuthorized,
            "Old-session setup operation should prepare before retirement test.");
        Assert(guard.RetirePeerSessions(77, 2) == 1,
            "Starting a new peer session should retire old refinement replay records.");
        Assert(guard.Begin(new RefinementOperationKey(77, 2, "old-session"), request).MutationAuthorized,
            "The same operation id may be reused only under a new positive session generation.");

        return assertions;
    }
}
