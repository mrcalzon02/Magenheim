using System;
using System.Linq;
using Magenheim.Core.Worldgen;

internal static class Program
{
    private static int _assertions;

    private static void Main()
    {
        AreaValidationRejectsNone();
        AreaValidationRejectsUnknownBits();
        AreaValidationClampsKnownBits();
        AreaValidationFallsBackOnlyWhenConfigured();
        PlannerAddsNamespacedEntry();
        PlannerSkipsOccupiedForeignKeyWithoutMutation();
        PlannerRejectsUnownedKeys();
        PlannerRejectsDuplicateDesiredKeys();
        PlannerCannotDisableAdditiveOnly();

        Console.WriteLine($"Magenheim.Core.Tests: {_assertions} assertions passed.");
    }

    private static void AreaValidationRejectsNone()
    {
        var result = SpawnAreaValidator.Normalize(SpawnArea.None);
        Assert(!result.IsValid, "None must be invalid by default.");
    }

    private static void AreaValidationRejectsUnknownBits()
    {
        var result = SpawnAreaValidator.Normalize((SpawnArea)8);
        Assert(!result.IsValid, "Unknown area bits must be rejected by default.");
    }

    private static void AreaValidationClampsKnownBits()
    {
        var result = SpawnAreaValidator.Normalize(
            SpawnArea.Median | (SpawnArea)8,
            InvalidAreaBehavior.ClampKnownBits);
        Assert(result.IsValid, "ClampKnownBits should retain valid bits.");
        Assert(result.Area == SpawnArea.Median, "ClampKnownBits retained wrong area.");
    }

    private static void AreaValidationFallsBackOnlyWhenConfigured()
    {
        var result = SpawnAreaValidator.Normalize(
            SpawnArea.None,
            InvalidAreaBehavior.FallbackToAll);
        Assert(result.IsValid && result.Area == SpawnArea.All,
            "FallbackToAll should explicitly produce All.");
    }

    private static void PlannerAddsNamespacedEntry()
    {
        var desired = new[]
        {
            new DesiredWorldgenAddition(
                "magenheim.geode.meadows.earth",
                "Magenheim_Geode_Meadows_Earth",
                SpawnArea.All),
        };

        var plan = WorldgenAdditionPlanner.Build(
            desired,
            Array.Empty<ObservedWorldgenRegistration>());

        Assert(!plan.HasErrors, "Valid namespaced addition should not error.");
        Assert(plan.Additions.Count() == 1, "Valid addition should be planned exactly once.");
    }

    private static void PlannerSkipsOccupiedForeignKeyWithoutMutation()
    {
        var observed = new[]
        {
            new ObservedWorldgenRegistration(
                "magenheim.geode.meadows.earth",
                "OtherMod",
                "OtherPrefab"),
        };

        var desired = new[]
        {
            new DesiredWorldgenAddition(
                "magenheim.geode.meadows.earth",
                "Magenheim_Geode_Meadows_Earth",
                SpawnArea.All),
        };

        var before = observed[0];
        var plan = WorldgenAdditionPlanner.Build(desired, observed);

        Assert(plan.Entries.Single().Action == WorldgenPlanAction.Skip,
            "Occupied key should be skipped under conservative policy.");
        Assert(Equals(before, observed[0]),
            "Observed foreign registration must remain unchanged.");
    }

    private static void PlannerRejectsUnownedKeys()
    {
        var plan = WorldgenAdditionPlanner.Build(
            new[] { new DesiredWorldgenAddition("foreign.key", "Prefab", SpawnArea.All) },
            Array.Empty<ObservedWorldgenRegistration>());

        Assert(plan.HasErrors, "Magenheim planner must reject non-Magenheim keys.");
    }

    private static void PlannerRejectsDuplicateDesiredKeys()
    {
        var desired = new[]
        {
            new DesiredWorldgenAddition("magenheim.same", "A", SpawnArea.All),
            new DesiredWorldgenAddition("magenheim.same", "B", SpawnArea.All),
        };

        var plan = WorldgenAdditionPlanner.Build(desired, Array.Empty<ObservedWorldgenRegistration>());
        Assert(plan.Entries.Count == 2, "Both desired entries should be represented in diagnostics.");
        Assert(plan.Entries[1].Action == WorldgenPlanAction.Skip,
            "Second desired duplicate should be skipped by conservative policy.");
    }

    private static void PlannerCannotDisableAdditiveOnly()
    {
        var threw = false;
        try
        {
            WorldgenAdditionPlanner.Build(
                Array.Empty<DesiredWorldgenAddition>(),
                Array.Empty<ObservedWorldgenRegistration>(),
                new WorldgenCompatibilityPolicy(
                    InvalidAreaBehavior.Reject,
                    DuplicateRegistrationBehavior.Skip,
                    AdditiveOnly: false));
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        Assert(threw, "Compatibility policy must not enable destructive worldgen mode.");
    }

    private static void Assert(bool condition, string message)
    {
        _assertions++;
        if (!condition)
        {
            throw new InvalidOperationException($"Assertion {_assertions} failed: {message}");
        }
    }
}
