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

        _assertions += DeepFractureCatalogTests.Run();
        _assertions += DeepFractureEncounterPlannerTests.Run();
        _assertions += DeepFractureInteriorBlueprintTests.Run();
        _assertions += DeepFractureExpeditionPlannerTests.Run();
        _assertions += DeepFractureLocationRegistrationTests.Run();
        _assertions += DefinitionCompatibilityTests.Run();
        _assertions += DefinitionAuthorityTests.Run();
        _assertions += SocketingTests.Run();
        _assertions += RefinementTransactionTests.Run();

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
            0.05d));

        Assert(result.Outcome == RefinementOutcome.Failure, "A roll below 20% at skill 0 should fail Simple -> Crystal.");
        Assert(result.ConsumeSource, "Failed refinement must consume its source crystal.");
        Assert(result.Shards is { Element: ElementalAlignment.Fire }, "Failed refinement must preserve alignment in returned shards.");
    }

    private static void SkillReductionScalesFailure()
    {
        var service = new CrystalRefinementService(CrystalRefinementService.CreateCanonicalDefaults());
        var result = service.Refine(new RefinementRequest(
            new Crystal(ElementalAlignment.Storm, CrystalTier.Advanced),
            100,
            "Magenheim_StationUpgrade_ResonanceFrame",
            0.21d));
        Assert(result.Outcome == RefinementOutcome.Success, "Skill 100 should reduce the 40% Master failure chance enough for a 0.21 roll to succeed.");
    }

    private static void InvalidStationDoesNotAwardExperience()
    {
        var service = new CrystalRefinementService(CrystalRefinementService.CreateCanonicalDefaults());
        var result = service.Refine(new RefinementRequest(
            new Crystal(ElementalAlignment.Earth, CrystalTier.Rough),
            0,
            "piece_workbench",
            0.99d));
        Assert(result.Outcome == RefinementOutcome.InvalidStation, "Wrong station must reject refinement.");
        Assert(!result.AwardExperience, "Invalid refinement must not award experience.");
    }

    private static void AreaValidationRejectsNone() => Assert(!WorldgenAreaMaskValidator.TryValidate(0, false, out _, out _), "Area None must be rejected.");
    private static void AreaValidationRejectsUnknownBits() => Assert(!WorldgenAreaMaskValidator.TryValidate(8, false, out _, out _), "Unknown area bits must be rejected by default.");
    private static void AreaValidationRejectsNegativeClamp() => Assert(!WorldgenAreaMaskValidator.TryValidate(-1, true, out _, out _), "Negative area masks must never be clamped.");
    private static void AreaValidationClampsKnownBits() => Assert(WorldgenAreaMaskValidator.TryValidate(9, true, out var value, out _) && value == 1, "Known bits should survive unknown-bit clamping.");
    private static void AreaValidationFallsBackOnlyWhenConfigured() => Assert(WorldgenAreaMaskValidator.TryValidate(8, true, out var value, out _) && value == WorldgenAreaMaskValidator.Everywhere, "Unknown-only mask may fall back only in configured clamp mode.");
    private static void AreaParserAcceptsEverywhereAlias() => Assert(WorldgenAreaMaskValidator.TryParse("Everywhere", false, out var value, out _) && value == WorldgenAreaMaskValidator.Everywhere, "Everywhere alias must parse.");
    private static void AreaParserRejectsUnknownTokenByDefault() => Assert(!WorldgenAreaMaskValidator.TryParse("Everything|FutureArea", false, out _, out _), "Unknown area token must fail closed.");
    private static void AreaParserClampsRecognizedTokensWhenConfigured() => Assert(WorldgenAreaMaskValidator.TryParse("Everything|FutureArea", true, out var value, out _) && value == WorldgenAreaMaskValidator.Everywhere, "Clamp mode must preserve recognized tokens.");

    private static void PlannerAddsNamespacedEntry()
    {
        var result = AdditiveWorldgenPlanner.Plan(Array.Empty<WorldgenHostEntry>(), new[] { Desired("magenheim.geode.earth", "Magenheim_Geode_Earth_World") }, AdditiveWorldgenCompatibilityPolicy.Default);
        Assert(result.Additions.Count == 1, "Owned free identity should be added.");
    }

    private static void PlannerSkipsOccupiedForeignKeyWithoutMutation()
    {
        var host = new[] { new WorldgenHostEntry("magenheim.geode.earth", "ForeignPrefab", "OtherMod") };
        var result = AdditiveWorldgenPlanner.Plan(host, new[] { Desired("magenheim.geode.earth", "Magenheim_Geode_Earth_World") }, AdditiveWorldgenCompatibilityPolicy.Default);
        Assert(result.Additions.Count == 0 && result.Skips.Count == 1, "Occupied key must be skipped, not replaced.");
    }

    private static void PlannerSkipsOccupiedPrefabWithoutMutation()
    {
        var host = new[] { new WorldgenHostEntry("other.key", "Magenheim_Geode_Earth_World", "OtherMod") };
        var result = AdditiveWorldgenPlanner.Plan(host, new[] { Desired("magenheim.geode.earth", "Magenheim_Geode_Earth_World") }, AdditiveWorldgenCompatibilityPolicy.Default);
        Assert(result.Additions.Count == 0 && result.Skips.Count == 1, "Occupied prefab identity must be skipped.");
    }

    private static void PlannerRejectsUnownedKeys()
    {
        var result = AdditiveWorldgenPlanner.Plan(Array.Empty<WorldgenHostEntry>(), new[] { Desired("foreign.key", "Magenheim_Geode_Earth_World") }, AdditiveWorldgenCompatibilityPolicy.Default);
        Assert(result.Errors.Count == 1, "Unowned worldgen keys must be rejected.");
    }

    private static void PlannerRejectsDuplicateDesiredKeys()
    {
        var desired = new[] { Desired("magenheim.geode.earth", "Magenheim_A"), Desired("magenheim.geode.earth", "Magenheim_B") };
        var result = AdditiveWorldgenPlanner.Plan(Array.Empty<WorldgenHostEntry>(), desired, AdditiveWorldgenCompatibilityPolicy.Default);
        Assert(result.Errors.Count == 1, "Duplicate desired keys must fail closed.");
    }

    private static void PlannerCanExcludeSpecificAddition()
    {
        var policy = AdditiveWorldgenCompatibilityPolicy.Default with { ExcludedOwnedKeys = new[] { "magenheim.geode.earth" } };
        var result = AdditiveWorldgenPlanner.Plan(Array.Empty<WorldgenHostEntry>(), new[] { Desired("magenheim.geode.earth", "Magenheim_Geode_Earth_World") }, policy);
        Assert(result.Additions.Count == 0 && result.Skips.Count == 1, "Configured owned exclusion should skip addition.");
    }

    private static void PlannerSupportsOptionalCaseInsensitiveIdentityComparison()
    {
        var host = new[] { new WorldgenHostEntry("OTHER", "magenheim_geode_earth_world", "OtherMod") };
        var policy = AdditiveWorldgenCompatibilityPolicy.Default with { CaseInsensitiveIdentityComparison = true };
        var result = AdditiveWorldgenPlanner.Plan(host, new[] { Desired("magenheim.geode.earth", "Magenheim_Geode_Earth_World") }, policy);
        Assert(result.Skips.Count == 1, "Case-insensitive compatibility mode should detect occupied prefab identity.");
    }

    private static void PlannerCannotDisableAdditiveOnly()
    {
        var policy = AdditiveWorldgenCompatibilityPolicy.Default with { AdditiveOnly = false };
        var result = AdditiveWorldgenPlanner.Plan(Array.Empty<WorldgenHostEntry>(), new[] { Desired("magenheim.geode.earth", "Magenheim_Geode_Earth_World") }, policy);
        Assert(result.Errors.Count == 1, "Destructive compatibility policy must fail closed.");
    }

    private static void DefinitionSnapshotFreezesCompatibilityPolicy()
    {
        var policy = AdditiveWorldgenCompatibilityPolicy.Default with { ExcludedOwnedKeys = new[] { "magenheim.geode.earth" } };
        var snapshot = DefinitionAuthoritySnapshot.Create(policy);
        Assert(snapshot.CompatibilityPolicy.ExcludedOwnedKeys.Count == 1, "Snapshot should preserve exclusions.");
    }

    private static void DefinitionFingerprintChangesWithCompatibilityPolicy()
    {
        var a = DefinitionAuthoritySnapshot.Create(AdditiveWorldgenCompatibilityPolicy.Default);
        var b = DefinitionAuthoritySnapshot.Create(AdditiveWorldgenCompatibilityPolicy.Default with { CaseInsensitiveIdentityComparison = true });
        Assert(a.Fingerprint != b.Fingerprint, "Compatibility changes must affect definition fingerprint.");
    }

    private static void DefinitionSnapshotRejectsForeignCompatibilityExclusion()
    {
        var threw = false;
        try { DefinitionAuthoritySnapshot.Create(AdditiveWorldgenCompatibilityPolicy.Default with { ExcludedOwnedKeys = new[] { "foreign.key" } }); }
        catch (InvalidOperationException) { threw = true; }
        Assert(threw, "Foreign exclusions must be rejected.");
    }

    private static void DefinitionSnapshotRejectsDestructiveCompatibilityPolicy()
    {
        var threw = false;
        try { DefinitionAuthoritySnapshot.Create(AdditiveWorldgenCompatibilityPolicy.Default with { AdditiveOnly = false }); }
        catch (InvalidOperationException) { threw = true; }
        Assert(threw, "Destructive compatibility policy must be rejected.");
    }

    private static DesiredWorldgenEntry Desired(string key, string prefab)
        => new(key, prefab, true);

    private static bool Approximately(double left, double right)
        => Math.Abs(left - right) < 0.000001d;

    private static void Assert(bool condition, string message)
    {
        _assertions++;
        if (!condition)
            throw new InvalidOperationException($"Assertion {_assertions} failed: {message}");
    }
}