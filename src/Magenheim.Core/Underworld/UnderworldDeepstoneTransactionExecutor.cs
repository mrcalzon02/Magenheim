using System;
using System.Collections.Generic;

namespace Magenheim.Core.Underworld;

public enum UnderworldDeepstoneTransactionOutcome
{
    Applied,
    ConsumptionRejected,
    PersistenceRejectedCompensated,
    CompensationFailed
}

public sealed record UnderworldDeepstoneTransactionResult(
    UnderworldDeepstoneTransactionOutcome Outcome,
    int ConsumedCount,
    bool PersistenceApplied,
    bool PersistenceRolledBack,
    bool InventoryRestored,
    string Diagnostic)
{
    public bool Applied => Outcome == UnderworldDeepstoneTransactionOutcome.Applied;
}

/// <summary>
/// Deterministic transaction coordinator for a previously-authorized Deepstone mount.
/// Runtime supplies the actual inventory and persistence operations; Core owns ordering,
/// compensation, and the success/failure contract so Valheim adapters cannot drift.
/// </summary>
public static class UnderworldDeepstoneTransactionExecutor
{
    public static UnderworldDeepstoneTransactionResult Execute(
        int authorizedConsumeCount,
        Func<int, bool> consumeOne,
        Func<bool> persistActivation,
        Func<bool> rollbackPersistence,
        Func<int, bool> restoreConsumed)
    {
        if (authorizedConsumeCount <= 0) throw new ArgumentOutOfRangeException(nameof(authorizedConsumeCount));
        ArgumentNullException.ThrowIfNull(consumeOne);
        ArgumentNullException.ThrowIfNull(persistActivation);
        ArgumentNullException.ThrowIfNull(rollbackPersistence);
        ArgumentNullException.ThrowIfNull(restoreConsumed);

        var consumed = 0;
        for (var index = 0; index < authorizedConsumeCount; index++)
        {
            if (!consumeOne(1))
            {
                var restored = consumed == 0 || restoreConsumed(consumed);
                return new UnderworldDeepstoneTransactionResult(
                    restored ? UnderworldDeepstoneTransactionOutcome.ConsumptionRejected : UnderworldDeepstoneTransactionOutcome.CompensationFailed,
                    consumed, false, true, restored,
                    restored
                        ? "Authorized trophy consumption could not complete; any consumed trophies were restored."
                        : "Authorized trophy consumption failed and inventory compensation was incomplete; administrator recovery is required.");
            }
            consumed++;
        }

        if (persistActivation())
            return new UnderworldDeepstoneTransactionResult(UnderworldDeepstoneTransactionOutcome.Applied, consumed, true, false, false, "Deepstone activation committed.");

        var persistenceRolledBack = rollbackPersistence();
        var inventoryRestored = restoreConsumed(consumed);
        if (persistenceRolledBack && inventoryRestored)
            return new UnderworldDeepstoneTransactionResult(UnderworldDeepstoneTransactionOutcome.PersistenceRejectedCompensated, consumed, false, true, true, "Deepstone persistence was rejected; the trophy was restored and no boon was granted.");

        return new UnderworldDeepstoneTransactionResult(
            UnderworldDeepstoneTransactionOutcome.CompensationFailed, consumed, false, persistenceRolledBack, inventoryRestored,
            "Deepstone activation failed and compensation was incomplete; administrator recovery is required.");
    }
}
