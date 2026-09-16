using System;
using Magenheim.Core.Underworld;

internal static class UnderworldDeepstoneRequestLedgerTests
{
    public static int Run()
    {
        var assertions = 0;
        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new InvalidOperationException($"Underworld Deepstone request-ledger assertion {assertions} failed: {message}");
        }

        var ledger = new UnderworldDeepstoneRequestLedger(3);
        Assert(!ledger.TryAdmit(0, 1, 1), "An unidentified peer must never enter mutation replay state.");
        Assert(!ledger.TryAdmit(7, 0, 1), "A request without an admitted authority generation must fail closed.");
        Assert(!ledger.TryAdmit(7, 1, 0), "A non-positive request identity must fail closed.");
        Assert(ledger.Count == 0, "Rejected identities must not consume replay-ledger capacity.");

        Assert(ledger.TryAdmit(7, 1, 1), "The first valid request tuple must be admitted.");
        Assert(!ledger.TryAdmit(7, 1, 1), "The same peer/generation/request tuple must be rejected as replay.");
        Assert(ledger.TryAdmit(7, 2, 1), "A new authority generation must create a distinct replay scope.");
        Assert(ledger.TryAdmit(8, 1, 1), "A different authenticated peer must have an independent request scope.");
        Assert(ledger.Count == 3, "The ledger must retain exactly its configured bounded capacity.");

        Assert(ledger.TryAdmit(7, 1, 2), "A later request must be admitted while evicting the oldest retained tuple.");
        Assert(ledger.Count == 3, "Eviction must keep replay memory bounded.");
        Assert(ledger.TryAdmit(7, 1, 1), "An evicted tuple may be admitted again only after it has left the bounded replay window.");

        ledger.Clear();
        Assert(ledger.Count == 0, "Session reset must clear retained replay identities.");
        Assert(ledger.TryAdmit(7, 1, 1), "A cleared ledger must admit a fresh session request.");
        return assertions;
    }
}
