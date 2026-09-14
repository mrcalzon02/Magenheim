using System;
using Magenheim.Core;
using Magenheim.Core.Networking;
using Magenheim.Core.Socketing;

internal static class SocketReplayIdentityTests
{
    internal static int Run()
    {
        var assertions = 0;
        void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            assertions++;
        }

        var authority = new DefinitionAuthorityResult(
            DefinitionAuthorityStatus.Compatible,
            true,
            "test authority");
        var equipment = new EquipmentDescriptor("SwordIron", "vanilla", EquipmentCategory.Weapon);
        var policy = new SocketEligibilityPolicy();
        var openState = new SocketState(1);
        var crystal = new Crystal(ElementalAlignment.Earth, CrystalTier.Simple);
        var request = new SocketTransactionRequest(
            authority,
            equipment,
            policy,
            openState,
            SocketTransactionKind.InstallCrystal,
            crystal,
            1);

        var guard = new SocketOperationGuard();
        var key = new SocketOperationKey(101, 4, "install-replay");
        var fresh = guard.Begin(key, request);
        Assert(fresh.MutationAuthorized,
            "Fresh socket installation should admit exactly one metadata/inventory mutation.");
        Assert(guard.MarkApplied(key),
            "Prepared socket installation should mark applied exactly once.");

        var replayAfterConsumption = request with { SourceCrystalCount = 0 };
        var replay = guard.Begin(key, replayAfterConsumption);
        Assert(replay.Outcome == SocketOperationOutcome.DuplicateApplied && !replay.MutationAuthorized,
            "Applied socket installation must remain identifiable after its source crystal has been consumed.");
        Assert(replay.Plan == fresh.Plan,
            "Applied socket replay must return the stored original plan instead of replanning against source availability.");

        var conflictKey = new SocketOperationKey(101, 4, "install-conflict");
        Assert(guard.Begin(conflictKey, request).MutationAuthorized,
            "Conflict setup operation should prepare normally.");
        var conflictingCrystal = request with
        {
            Crystal = new Crystal(ElementalAlignment.Fire, CrystalTier.Simple),
            SourceCrystalCount = 0,
        };
        var conflict = guard.Begin(conflictKey, conflictingCrystal);
        Assert(conflict.Outcome == SocketOperationOutcome.ConflictingReplay && !conflict.MutationAuthorized,
            "Reusing an operation id for a different crystal must fail as conflicting immutable intent before source replanning.");
        Assert(guard.AbortPrepared(conflictKey),
            "Unapplied conflicting socket setup should remain abortable.");

        var policyConflictKey = new SocketOperationKey(101, 4, "policy-conflict");
        Assert(guard.Begin(policyConflictKey, request).MutationAuthorized,
            "Policy conflict setup operation should prepare normally.");
        var changedPolicy = new SocketEligibilityPolicy(weaponMaxSlots: 2);
        var changedPolicyRequest = request with
        {
            Policy = changedPolicy,
            SourceCrystalCount = 0,
        };
        var policyConflict = guard.Begin(policyConflictKey, changedPolicyRequest);
        Assert(policyConflict.Outcome == SocketOperationOutcome.ConflictingReplay,
            "Reusing an operation id under materially different socket policy must fail closed before source replanning.");

        var freshMissing = guard.Begin(
            new SocketOperationKey(101, 4, "fresh-missing"),
            replayAfterConsumption);
        Assert(freshMissing.Outcome == SocketOperationOutcome.PlanRejected &&
               freshMissing.Plan.Outcome == SocketTransactionOutcome.MissingCrystal,
            "A genuinely fresh install without a source crystal must still be rejected by normal planning.");

        return assertions;
    }
}
