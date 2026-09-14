using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Magenheim.Core;
using Magenheim.Core.Definitions;
using Magenheim.Core.Worldgen;

internal static class DefinitionWorldgenPlannerTests
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        var assertions = Run();
        Console.WriteLine($"DefinitionWorldgenPlannerTests: {assertions} assertions passed.");
    }

    private static int Run()
    {
        var assertions = 0;

        static void Assert(ref int count, bool condition, string message)
        {
            count++;
            if (!condition)
                throw new InvalidOperationException($"DefinitionWorldgenPlanner assertion {count} failed: {message}");
        }

        var baseline = CreateDefinitions(WorldgenCompatibilityPolicy.Conservative, SpawnArea.All);
        var baselinePlan = DefinitionWorldgenPlanner.Build(
            baseline,
            Array.Empty<ObservedWorldgenRegistration>());
        var addition = baselinePlan.Additions.Single();

        Assert(ref assertions, addition.Desired.RegistrationKey == "magenheim.geode.meadows.earth",
            "Definition-derived plan must preserve the authoritative registration key.");
        Assert(ref assertions, addition.Desired.PrefabName == "Magenheim_Geode_Meadows_Earth_World",
            "Definition-derived worldgen plan must use the dedicated mineable world-object prefab identity rather than the inventory item prefab.");
        Assert(ref assertions, addition.Desired.PrefabName != baseline.Geodes.Single().PrefabName,
            "Inventory and world-object prefab identities must remain distinct.");
        Assert(ref assertions, addition.Desired.Biome == "Meadows",
            "Definition-derived plan must carry the authoritative biome into the registration plan.");
        Assert(ref assertions, addition.NormalizedArea == SpawnArea.All,
            "Definition-derived plan must preserve the validated area.");

        var excluded = CreateDefinitions(
            WorldgenCompatibilityPolicy.Conservative with
            {
                ExcludedRegistrationKeys = new[] { "magenheim.geode.meadows.earth" },
            },
            SpawnArea.All);
        var excludedPlan = DefinitionWorldgenPlanner.Build(
            excluded,
            Array.Empty<ObservedWorldgenRegistration>());
        Assert(ref assertions, excludedPlan.Entries.Single().Action == WorldgenPlanAction.Skip,
            "Definition-owned compatibility exclusions must flow into planning without runtime reconstruction.");

        var collisionPlan = DefinitionWorldgenPlanner.Build(
            baseline,
            new[]
            {
                new ObservedWorldgenRegistration(
                    "foreign.registration",
                    "ForeignMod",
                    "Magenheim_Geode_Meadows_Earth_World"),
            });
        Assert(ref assertions, collisionPlan.Entries.Single().Action == WorldgenPlanAction.Skip,
            "Observed world-object prefab collisions must remain non-destructive when planning from definitions.");

        var caseInsensitive = CreateDefinitions(
            WorldgenCompatibilityPolicy.Conservative with
            {
                IdentityComparison = RegistrationIdentityComparison.CaseInsensitive,
            },
            SpawnArea.All);
        var caseInsensitivePlan = DefinitionWorldgenPlanner.Build(
            caseInsensitive,
            new[]
            {
                new ObservedWorldgenRegistration(
                    "MAGENHEIM.GEODE.MEADOWS.EARTH",
                    "ForeignMod",
                    "OtherPrefab"),
            });
        Assert(ref assertions, caseInsensitivePlan.Entries.Single().Action == WorldgenPlanAction.Skip,
            "Definition-owned case-insensitive identity policy must govern runtime collision planning.");

        var fallbackArea = CreateDefinitions(
            WorldgenCompatibilityPolicy.Conservative with
            {
                InvalidAreaBehavior = InvalidAreaBehavior.FallbackToAll,
            },
            (SpawnArea)(-1));
        var fallbackPlan = DefinitionWorldgenPlanner.Build(
            fallbackArea,
            Array.Empty<ObservedWorldgenRegistration>());
        Assert(ref assertions, fallbackPlan.Additions.Single().NormalizedArea == SpawnArea.All,
            "Area normalization admitted by the definition snapshot must remain authoritative in the worldgen plan.");

        var reservedSuffixRejected = false;
        try
        {
            GeodeWorldPrefabIdentity.FromItemPrefabName("Magenheim_Geode_Test_World");
        }
        catch (ArgumentException)
        {
            reservedSuffixRejected = true;
        }
        Assert(ref assertions, reservedSuffixRejected,
            "Inventory prefab identities must not be allowed to consume the reserved world-object suffix.");

        return assertions;
    }

    private static MagenheimDefinitionSet CreateDefinitions(
        WorldgenCompatibilityPolicy policy,
        SpawnArea area)
    {
        var geode = new GeodeDefinition(
            "magenheim.geode.meadows.earth",
            "Meadows",
            "Magenheim_Geode_Meadows_Earth",
            area,
            1,
            0.35d,
            0.10d,
            new[] { new ElementWeight(ElementalAlignment.Earth, 100d) });

        return MagenheimDefinitionValidator.ValidateAndFreeze(
            MagenheimDefinitionValidator.CurrentSchemaVersion,
            CrystalRefinementService.CreateCanonicalDefaults(),
            new[] { geode },
            policy);
    }
}
