using System;
using System.Linq;
using Magenheim.Core;
using Magenheim.Core.Definitions;
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
        AreaValidationRejectsNegativeClamp();
        AreaValidationClampsKnownBits();
        AreaValidationFallsBackOnlyWhenConfigured();
        AreaParserAcceptsEverywhereAlias();
        AreaParserRejectsUnknownTokenByDefault();
        AreaParserClampsRecognizedTokensWhenConfigured();

        PlannerAddsNamespacedEntry();
        PlannerSkipsOccupiedForeignKeyWithoutMutation();
        PlannerSkipsOccupiedPrefabWithoutMutation();
        PlannerRejectsUnownedKeys();
        PlannerRejectsDuplicateDesiredKeys();
        PlannerCanExcludeSpecificAddition();
        PlannerSupportsOptionalCaseInsensitiveIdentityComparison();
        PlannerCannotDisableAdditiveOnly();

        DefinitionSnapshotFreezesCompatibilityPolicy();
        DefinitionFingerprintChangesWithCompatibilityPolicy();
        DefinitionSnapshotRejectsForeignCompatibilityExclusion();
        DefinitionSnapshotRejectsDestructiveCompatibilityPolicy();

        _assertions += DefinitionCompatibilityTests.Run();
        _assertions += DefinitionAuthorityTests.Run();

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

    private static void AreaValidationRejectsNegativeClamp()
    {
        var result = SpawnAreaValidator.Normalize((SpawnArea)(-1), InvalidAreaBehavior.ClampKnownBits);
        Assert(!result.IsValid, "Negative flag values must not sign-extend into All when clamping.");
    }

    private static void AreaValidationClampsKnownBits()
    {
        var result = SpawnAreaValidator.Normalize(SpawnArea.Median | (SpawnArea)8, InvalidAreaBehavior.ClampKnownBits);
        Assert(result.IsValid, "ClampKnownBits should retain valid positive bits.");
        Assert(result.Area == SpawnArea.Median, "ClampKnownBits retained wrong area.");
    }

    private static void AreaValidationFallsBackOnlyWhenConfigured()
    {
        var result = SpawnAreaValidator.Normalize(SpawnArea.None, InvalidAreaBehavior.FallbackToAll);
        Assert(result.IsValid && result.Area == SpawnArea.All, "FallbackToAll should explicitly produce All.");
    }

    private static void AreaParserAcceptsEverywhereAlias()
    {
        var result = SpawnAreaValidator.ParseConfiguredArea("Everywhere");
        Assert(result.IsValid && result.Area == SpawnArea.All, "Everywhere should map to the abstract All area.");
    }

    private static void AreaParserRejectsUnknownTokenByDefault()
    {
        var result = SpawnAreaValidator.ParseConfiguredArea("Median,banana");
        Assert(!result.IsValid, "Unknown textual area tokens must fail under conservative policy.");
    }

    private static void AreaParserClampsRecognizedTokensWhenConfigured()
    {
        var result = SpawnAreaValidator.ParseConfiguredArea("Median,banana", InvalidAreaBehavior.ClampKnownBits);
        Assert(result.IsValid && result.Area == SpawnArea.Median, "Configured textual clamping should keep only recognized areas.");
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

    private static void PlannerSkipsOccupiedPrefabWithoutMutation()
    {
        var observed = new[] { new ObservedWorldgenRegistration("other.key", "OtherMod", "Magenheim_Geode_Meadows_Earth") };
        var desired = new[] { new DesiredWorldgenAddition("magenheim.geode.meadows.earth", "Magenheim_Geode_Meadows_Earth", SpawnArea.All) };
        var before = observed[0];
        var plan = WorldgenAdditionPlanner.Build(desired, observed);
        Assert(plan.Entries.Single().Action == WorldgenPlanAction.Skip, "Occupied prefab should be skipped under conservative policy.");
        Assert(Equals(before, observed[0]), "Prefab collision detection must remain observation-only.");
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

    private static void PlannerCanExcludeSpecificAddition()
    {
        var policy = WorldgenCompatibilityPolicy.Conservative with
        {
            ExcludedRegistrationKeys = new[] { "magenheim.geode.meadows.earth" },
        };
        var desired = new[] { new DesiredWorldgenAddition("magenheim.geode.meadows.earth", "Magenheim_Geode_Meadows_Earth", SpawnArea.All) };
        var plan = WorldgenAdditionPlanner.Build(desired, Array.Empty<ObservedWorldgenRegistration>(), policy);
        Assert(plan.Entries.Single().Action == WorldgenPlanAction.Skip, "Configured exclusion should skip only the Magenheim addition.");
    }

    private static void PlannerSupportsOptionalCaseInsensitiveIdentityComparison()
    {
        var policy = WorldgenCompatibilityPolicy.Conservative with
        {
            IdentityComparison = RegistrationIdentityComparison.CaseInsensitive,
        };
        var observed = new[] { new ObservedWorldgenRegistration("MAGENHEIM.GEODE.MEADOWS.EARTH", "OtherMod", "OtherPrefab") };
        var desired = new[] { new DesiredWorldgenAddition("magenheim.geode.meadows.earth", "Magenheim_Geode_Meadows_Earth", SpawnArea.All) };
        var plan = WorldgenAdditionPlanner.Build(desired, observed, policy);
        Assert(plan.Entries.Single().Action == WorldgenPlanAction.Skip, "Case-insensitive compatibility mode should detect differently-cased occupied identities.");
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

    private static void DefinitionSnapshotFreezesCompatibilityPolicy()
    {
        var policy = WorldgenCompatibilityPolicy.Conservative with
        {
            ExcludedRegistrationKeys = new[] { "magenheim.geode.meadows.earth" },
            ExcludedPrefabNames = new[] { "Magenheim_Geode_Meadows_Earth" },
        };

        var snapshot = CreateDefinitionSnapshot(policy);
        Assert(snapshot.SchemaVersion == MagenheimDefinitionValidator.CurrentSchemaVersion, "Definition snapshot should use the current schema.");
        Assert(snapshot.WorldgenCompatibility.ExcludedRegistrationKeys.Single() == "magenheim.geode.meadows.earth", "Compatibility key exclusions should survive validation.");
        Assert(snapshot.WorldgenCompatibility.ExcludedPrefabNames.Single() == "Magenheim_Geode_Meadows_Earth", "Compatibility prefab exclusions should survive validation.");
    }

    private static void DefinitionFingerprintChangesWithCompatibilityPolicy()
    {
        var baseline = CreateDefinitionSnapshot(WorldgenCompatibilityPolicy.Conservative);
        var changed = CreateDefinitionSnapshot(WorldgenCompatibilityPolicy.Conservative with
        {
            IdentityComparison = RegistrationIdentityComparison.CaseInsensitive,
        });

        Assert(!string.Equals(baseline.Fingerprint, changed.Fingerprint, StringComparison.Ordinal), "Gameplay fingerprint must change when compatibility policy changes.");
    }

    private static void DefinitionSnapshotRejectsForeignCompatibilityExclusion()
    {
        var threw = false;
        try
        {
            CreateDefinitionSnapshot(WorldgenCompatibilityPolicy.Conservative with
            {
                ExcludedRegistrationKeys = new[] { "othermod.geode" },
            });
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        Assert(threw, "Compatibility exclusions must not target foreign registration namespaces.");
    }

    private static void DefinitionSnapshotRejectsDestructiveCompatibilityPolicy()
    {
        var threw = false;
        try
        {
            CreateDefinitionSnapshot(new WorldgenCompatibilityPolicy(
                InvalidAreaBehavior.Reject,
                DuplicateRegistrationBehavior.Skip,
                AdditiveOnly: false));
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        Assert(threw, "Definition admission must reject destructive worldgen compatibility policy.");
    }

    private static MagenheimDefinitionSet CreateDefinitionSnapshot(WorldgenCompatibilityPolicy policy)
    {
        var geodes = new[]
        {
            new GeodeDefinition(
                "magenheim.geode.meadows.earth",
                "Meadows",
                "Magenheim_Geode_Meadows_Earth",
                SpawnArea.All,
                1,
                0.35d,
                0.10d,
                new[] { new ElementWeight(ElementalAlignment.Earth, 100d) }),
        };

        return MagenheimDefinitionValidator.ValidateAndFreeze(
            MagenheimDefinitionValidator.CurrentSchemaVersion,
            CrystalRefinementService.CreateCanonicalDefaults(),
            geodes,
            policy);
    }

    private static bool Approximately(double left, double right) => Math.Abs(left - right) < 0.0000001d;

    private static void Assert(bool condition, string message)
    {
        _assertions++;
        if (!condition)
            throw new InvalidOperationException($"Assertion {_assertions} failed: {message}");
    }
}
