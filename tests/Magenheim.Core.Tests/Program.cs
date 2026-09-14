using System;
using System.Linq;
using Magenheim.Core;
using Magenheim.Core.Worldgen;

internal static class Program
{
    private static int _assertions;

    private static void Main()
    {
        RefinementDefaultsUseCanonicalTierSequence();
        RefinementPreservesAlignmentOnSuccess();
        RefinementFailureDestroysSourceAndReturnsShards();
        SkillReductionScalesFailure();
        InvalidStationDoesNotAwardExperience();
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

    private static void RefinementDefaultsUseCanonicalTierSequence()
    {
        var rules = CrystalRefinementService.CreateCanonicalDefaults();
        Assert(rules.Count == 4, "Canonical refinement must contain exactly four transitions.");
        Assert(rules[0].SourceTier == CrystalTier.Rough && rules[0].DestinationTier == CrystalTier.Simple, "Rough must refine to Simple.");
        Assert(rules[1].SourceTier == CrystalTier.Simple && rules[1].DestinationTier == CrystalTier.Crystal, "Simple must refine to Crystal.");
        Assert(rules[2].SourceTier == CrystalTier.Crystal && rules[2].DestinationTier == CrystalTier.Advanced, "Crystal must refine to Advanced.");
        Assert(rules[3].SourceTier == CrystalTier.Advanced && rules[3].DestinationTier == CrystalTier.Master, "Advanced must refine to Master.");
        Assert(Approximately(rules[0].BaseFailureChance, 0.10d) && Approximately(rules[3].BaseFailureChance, 0.40d), "Canonical failure curve must span 10% through 40%.");
    }

    private static void RefinementPreservesAlignmentOnSuccess()
    {
        var service = new CrystalRefinementService(CrystalRefinementService.CreateCanonicalDefaults());
        var result = service.Refine(new RefinementRequest(
            new Crystal(ElementalAlignment.Earth, CrystalTier.Simple),
            0,
            "Magenheim_StationUpgrade_FracturingBlock",
            0.25d));

        Assert(result.Outcome == RefinementOutcome.Success, "A roll above 20% at skill 0 should succeed for Simple -> Crystal.");
        Assert(result.Output is { Tier: CrystalTier.Crystal, Element: ElementalAlignment.Earth }, "Successful refinement must preserve element and advance one tier.");
        Assert(result.AwardExperience, "A valid success should be experience-eligible.");
    }

    private static void RefinementFailureDestroysSourceAndReturnsShards()
    {
        var service = new CrystalRefinementService(CrystalRefinementService.CreateCanonicalDefaults());
        var result = service.Refine(new RefinementRequest(
            new Crystal(ElementalAlignment.Fire, CrystalTier.Simple),
            0,
            "Magenheim_StationUpgrade_FracturingBlock",
            0.10d));

        Assert(result.Outcome == RefinementOutcome.FailedDestroyed, "A roll below effective failure must fail destructively.");
        Assert(result.Output is null, "Destructive failure must not preserve or duplicate the source crystal.");
        Assert(result.ShardReturnCount == 2, "Simple refinement failure must return two matching shards.");
        Assert(result.AwardExperience, "A valid failure should be experience-eligible.");
    }

    private static void SkillReductionScalesFailure()
    {
        var defaultAt100 = CrystalRefinementService.CalculateEffectiveFailureChance(0.40d, 100);
        var perfectAt100 = CrystalRefinementService.CalculateEffectiveFailureChance(0.40d, 100, 1.00d);
        Assert(Approximately(defaultAt100, 0.10d), "75% maximum reduction should reduce 40% base failure to 10% at skill 100.");
        Assert(Approximately(perfectAt100, 0d), "100% configured reduction should permit zero failure at skill 100.");
    }

    private static void InvalidStationDoesNotAwardExperience()
    {
        var service = new CrystalRefinementService(CrystalRefinementService.CreateCanonicalDefaults());
        var result = service.Refine(new RefinementRequest(
            new Crystal(ElementalAlignment.Storm, CrystalTier.Rough),
            50,
            "WrongStation",
            0.99d));
        Assert(result.Outcome == RefinementOutcome.InvalidStation, "Wrong station must invalidate the attempt.");
        Assert(!result.AwardExperience, "Invalid attempts must not award experience.");
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
        var result = SpawnAreaValidator.Normalize(SpawnArea.Median | (SpawnArea)8, InvalidAreaBehavior.ClampKnownBits);
        Assert(result.IsValid, "ClampKnownBits should retain valid bits.");
        Assert(result.Area == SpawnArea.Median, "ClampKnownBits retained wrong area.");
    }

    private static void AreaValidationFallsBackOnlyWhenConfigured()
    {
        var result = SpawnAreaValidator.Normalize(SpawnArea.None, InvalidAreaBehavior.FallbackToAll);
        Assert(result.IsValid && result.Area == SpawnArea.All, "FallbackToAll should explicitly produce All.");
    }

    private static void PlannerAddsNamespacedEntry()
    {
        var desired = new[] { new DesiredWorldgenAddition("magenheim.geode.meadows.earth", "Magenheim_Geode_Meadows_Earth", SpawnArea.All) };
        var plan = WorldgenAdditionPlanner.Build(desired, Array.Empty<ObservedWorldgenRegistration>());
        Assert(!plan.HasErrors, "Valid namespaced addition should not error.");
        Assert(plan.Additions.Count() == 1, "Valid addition should be planned exactly once.");
    }

    private static void PlannerSkipsOccupiedForeignKeyWithoutMutation()
    {
        var observed = new[] { new ObservedWorldgenRegistration("magenheim.geode.meadows.earth", "OtherMod", "OtherPrefab") };
        var desired = new[] { new DesiredWorldgenAddition("magenheim.geode.meadows.earth", "Magenheim_Geode_Meadows_Earth", SpawnArea.All) };
        var before = observed[0];
        var plan = WorldgenAdditionPlanner.Build(desired, observed);
        Assert(plan.Entries.Single().Action == WorldgenPlanAction.Skip, "Occupied key should be skipped under conservative policy.");
        Assert(Equals(before, observed[0]), "Observed foreign registration must remain unchanged.");
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
        Assert(plan.Entries.Count == 2, "Both desired entries should remain represented in diagnostics.");
        Assert(plan.Entries[1].Action == WorldgenPlanAction.Skip, "Second duplicate should be skipped by conservative policy.");
    }

    private static void PlannerCannotDisableAdditiveOnly()
    {
        var threw = false;
        try
        {
            WorldgenAdditionPlanner.Build(
                Array.Empty<DesiredWorldgenAddition>(),
                Array.Empty<ObservedWorldgenRegistration>(),
                new WorldgenCompatibilityPolicy(InvalidAreaBehavior.Reject, DuplicateRegistrationBehavior.Skip, AdditiveOnly: false));
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }
        Assert(threw, "Compatibility policy must not enable destructive worldgen mode.");
    }

    private static bool Approximately(double left, double right) => Math.Abs(left - right) < 0.0000001d;

    private static void Assert(bool condition, string message)
    {
        _assertions++;
        if (!condition)
            throw new InvalidOperationException($"Assertion {_assertions} failed: {message}");
    }
}
