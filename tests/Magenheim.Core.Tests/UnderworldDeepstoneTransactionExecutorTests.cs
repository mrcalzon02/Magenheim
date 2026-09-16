using System;
using Magenheim.Core.Underworld;

internal static class UnderworldDeepstoneTransactionExecutorTests
{
    public static int Run()
    {
        var assertions = 0;

        var inventory = 2;
        var persisted = false;
        var applied = UnderworldDeepstoneTransactionExecutor.Execute(1,
            count => inventory >= count && (inventory -= count) >= 0,
            () => persisted = true,
            () => { persisted = false; return true; },
            count => { inventory += count; return true; });
        Assert(applied.Applied && inventory == 1 && persisted, "Successful transaction must consume exactly the authorized trophy count and persist activation."); assertions++;

        inventory = 1;
        var consumeCalls = 0;
        var partial = UnderworldDeepstoneTransactionExecutor.Execute(2,
            _ => ++consumeCalls == 1 && --inventory >= 0,
            () => throw new InvalidOperationException("Persistence must not run after incomplete consumption."),
            () => true,
            count => { inventory += count; return true; });
        Assert(partial.Outcome == UnderworldDeepstoneTransactionOutcome.ConsumptionRejected && inventory == 1, "Partial consumption must restore everything consumed before rejection."); assertions++;

        inventory = 1;
        persisted = true;
        var rejected = UnderworldDeepstoneTransactionExecutor.Execute(1,
            count => inventory >= count && (inventory -= count) >= 0,
            () => false,
            () => { persisted = false; return true; },
            count => { inventory += count; return true; });
        Assert(rejected.Outcome == UnderworldDeepstoneTransactionOutcome.PersistenceRejectedCompensated && inventory == 1 && !persisted, "Persistence rejection must roll back state and restore the trophy."); assertions++;

        inventory = 1;
        var inventoryFailure = UnderworldDeepstoneTransactionExecutor.Execute(1,
            count => inventory >= count && (inventory -= count) >= 0,
            () => false,
            () => true,
            _ => false);
        Assert(inventoryFailure.Outcome == UnderworldDeepstoneTransactionOutcome.CompensationFailed && inventoryFailure.PersistenceRolledBack && !inventoryFailure.InventoryRestored, "Inventory restoration failure must be surfaced as catastrophic compensation failure."); assertions++;

        inventory = 1;
        var persistenceFailure = UnderworldDeepstoneTransactionExecutor.Execute(1,
            count => inventory >= count && (inventory -= count) >= 0,
            () => false,
            () => false,
            count => { inventory += count; return true; });
        Assert(persistenceFailure.Outcome == UnderworldDeepstoneTransactionOutcome.CompensationFailed && !persistenceFailure.PersistenceRolledBack && persistenceFailure.InventoryRestored, "Persistence rollback failure must be surfaced even when inventory compensation succeeds."); assertions++;

        return assertions;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
