using System;
using System.Collections.Generic;
using Magenheim.Runtime.TrueBlacksmithing;

internal static class TrueBlacksmithingFailSafeTests
{
    internal static int Run()
    {
        var checks = 0;
        void Assert(bool condition, string message)
        {
            checks++;
            if (!condition) throw new InvalidOperationException(message);
        }

        var log = new BepInEx.Logging.ManualLogSource();
        var journal = new TrueBlacksmithingMutationJournal(log);
        var recipes = new List<string> { "baseline" };
        var order = new List<string>();
        journal.Apply("first recipe", () => recipes[0] = "manufacturing", () =>
        {
            recipes[0] = "baseline";
            order.Add("first");
        });
        var failed = false;
        try
        {
            journal.Apply("partial registration", () =>
            {
                recipes.Add("partial");
                throw new InvalidOperationException("Injected activation failure");
            }, () => { recipes.Remove("partial"); order.Add("second"); });
        }
        catch (InvalidOperationException) { failed = true; }
        Assert(failed, "Activation failures must reach the circuit breaker.");
        Assert(recipes.Count == 1 && recipes[0] == "baseline", "Partial activation must restore baseline crafting.");
        Assert(string.Join(",", order) == "second,first", "Recipe rollback must run in reverse mutation order.");
        Assert(!journal.HasPendingRollback, "Successful restoration must leave no pending mutation.");
        journal.Dispose();
        Assert(order.Count == 2, "Teardown must not repeat completed recipe restorations.");

        journal = new TrueBlacksmithingMutationJournal(log);
        var temporarilyBlocked = true;
        var successfulReversals = 0;
        journal.Apply("temporarily blocked recipe", () => recipes[0] = "manufacturing", () =>
        {
            if (temporarilyBlocked) throw new InvalidOperationException("Injected restoration failure");
            recipes[0] = "baseline";
        });
        journal.Apply("independent recipe", () => { }, () => successfulReversals++);
        journal.RollbackAll();
        Assert(journal.HasPendingRollback && log.Errors.Count == 1, "A failed restoration must remain pending and be reported.");
        Assert(successfulReversals == 1, "One failed restoration must not block independent recipes.");
        temporarilyBlocked = false;
        journal.Dispose();
        Assert(!journal.HasPendingRollback && recipes[0] == "baseline", "Teardown must retry failed baseline restoration.");
        Assert(successfulReversals == 1, "Rollback retry must not rerun previously restored recipes.");
        failed = false;
        try { journal.Apply("late mutation", () => recipes.Clear(), () => { }); }
        catch (InvalidOperationException) { failed = true; }
        Assert(failed && recipes.Count == 1, "A rolled-back session must reject new recipe mutations.");
        return checks;
    }
}

// Only the logger boundary is substituted. The linked journal above is the actual runtime source.
namespace BepInEx.Logging
{
    internal sealed class ManualLogSource
    {
        internal List<object> Errors { get; } = new List<object>();
        public void LogError(object message) => Errors.Add(message);
    }
}
