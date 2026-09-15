using System;
using System.Linq;
using Magenheim.Core;
using Magenheim.Core.Definitions;
using Magenheim.Core.Worldgen;

internal static class Program
{
    private static int _assertions;
    private static int Main() { try { RunAll(); return 0; } catch (Exception error) { Console.Error.WriteLine(error); return 1; } }
    private static void RunAll()
    {
        RefinementAndAreaBoundaries();
        WorldgenCompatibilityBoundaries();
        _assertions += DefinitionCompatibilityTests.Run();
        _assertions += DefinitionLoaderTests.Run();
        _assertions += DefinitionAuthorityTests.Run();
        _assertions += SocketingTests.Run();
        _assertions += SocketReplayIdentityTests.Run();
        _assertions += BehavioralResonanceTests.Run();
        _assertions += RefinementTransactionTests.Run();
        Console.WriteLine($"Magenheim.Core.Tests: {_assertions} assertions passed (plus separately reported module suites).");
    }
    private static void RefinementAndAreaBoundaries()
    {
        var rules = CrystalRefinementService.CreateCanonicalDefaults();
        Assert(rules.Count == 4, "Four canonical refinement steps.");
        var service = new CrystalRefinementService(rules);
        foreach (var rule in rules)
        {
            Assert((int)rule.DestinationTier == (int)rule.SourceTier + 1, "Refinement advances one tier.");
            foreach (ElementalAlignment element in Enum.GetValues(typeof(ElementalAlignment)))
            {
                var request = new RefinementRequest(new Crystal(element, rule.SourceTier), 100, rule.RequiredStation, .99d);
                var success = service.Refine(request);
                Assert(success.Outcome == RefinementOutcome.Success && success.Output == new Crystal(element, rule.DestinationTier), "Success preserves alignment.");
                Assert(success.AwardExperience, "Success awards experience.");
                var failure = service.Refine(request with { Roll = 0d });
                Assert(failure.Outcome == RefinementOutcome.FailedDestroyed && failure.Output is null && failure.ShardReturnCount == rule.FailureShardCount, "Failure destroys input and returns configured shards.");
                var invalid = service.Refine(request with { StationId = "piece_workbench" });
                Assert(invalid.Outcome == RefinementOutcome.InvalidStation && !invalid.AwardExperience, "Wrong station cannot award XP.");
            }
        }
        Assert(Math.Abs(CrystalRefinementService.CalculateEffectiveFailureChance(.4d, 100) - .1d) < .000001d, "Skill 100 reduces failure by 75% by default.");
        Assert(!SpawnAreaValidator.Normalize(SpawnArea.None).IsValid, "None rejected.");
        Assert(!SpawnAreaValidator.Normalize((SpawnArea)8).IsValid, "Unknown bits rejected.");
        Assert(!SpawnAreaValidator.Normalize((SpawnArea)(-1), InvalidAreaBehavior.ClampKnownBits).IsValid, "Negative mask never clamps.");
        Assert(SpawnAreaValidator.Normalize((SpawnArea)9, InvalidAreaBehavior.ClampKnownBits).Area == SpawnArea.Median, "Known bits survive clamping.");
        Assert(!SpawnAreaValidator.Normalize((SpawnArea)8, InvalidAreaBehavior.ClampKnownBits).IsValid, "Clamping is not fallback.");
        Assert(SpawnAreaValidator.Normalize((SpawnArea)8, InvalidAreaBehavior.FallbackToAll).Area == SpawnArea.All, "Explicit fallback works.");
        Assert(SpawnAreaValidator.ParseConfiguredArea("Everywhere").Area == SpawnArea.All, "Runtime alias parses.");
        Assert(!SpawnAreaValidator.ParseConfiguredArea("Everywhere|FutureArea").IsValid, "Unknown tokens fail closed.");
        Assert(SpawnAreaValidator.ParseConfiguredArea("Everywhere|FutureArea", InvalidAreaBehavior.ClampKnownBits).Area == SpawnArea.All, "Configured parsing preserves known tokens.");
    }
    private static void WorldgenCompatibilityBoundaries()
    {
        var desired = new DesiredWorldgenAddition("magenheim.geode.earth", "Magenheim_Geode_Earth_World", SpawnArea.All);
        var empty = Array.Empty<ObservedWorldgenRegistration>();
        var policy = WorldgenCompatibilityPolicy.Conservative;
        Assert(WorldgenAdditionPlanner.Build(new[] { desired }, empty).Additions.Count() == 1, "Owned free entry is added.");
        foreach (var occupied in new[] {
            new ObservedWorldgenRegistration(desired.RegistrationKey, "OtherMod", "ForeignPrefab"),
            new ObservedWorldgenRegistration("other.key", "OtherMod", desired.PrefabName) })
        {
            var host = new[] { occupied };
            var plan = WorldgenAdditionPlanner.Build(new[] { desired }, host);
            Assert(!plan.Additions.Any() && plan.Entries.Single().Action == WorldgenPlanAction.Skip, "Occupied identity is skipped.");
            Assert(host[0] == occupied, "Foreign registration is preserved.");
        }
        Assert(WorldgenAdditionPlanner.Build(new[] { desired with { RegistrationKey = "foreign.key" } }, empty).HasErrors, "Unowned keys rejected.");
        Assert(WorldgenAdditionPlanner.Build(new[] { desired, desired with { PrefabName = "Magenheim_Other" } }, empty, policy with { DuplicateRegistrationBehavior = DuplicateRegistrationBehavior.Error }).HasErrors, "Duplicate keys rejected.");
        var exclusions = new[] { desired.RegistrationKey };
        var excluded = policy with { ExcludedRegistrationKeys = exclusions };
        Assert(!WorldgenAdditionPlanner.Build(new[] { desired }, empty, excluded).Additions.Any(), "Owned exclusions honored.");
        var insensitive = policy with { IdentityComparison = RegistrationIdentityComparison.CaseInsensitive };
        Assert(!WorldgenAdditionPlanner.Build(new[] { desired }, new[] { new ObservedWorldgenRegistration("other", "OtherMod", desired.PrefabName.ToLowerInvariant()) }, insensitive).Additions.Any(), "Case-insensitive collisions detected.");
        AssertThrows(() => WorldgenAdditionPlanner.Build(new[] { desired }, empty, policy with { AdditiveOnly = false }), "Destructive policy rejected.");
        var geodes = new[] { new GeodeDefinition("magenheim.geode.earth", "Meadows", "Magenheim_Geode_Earth", SpawnArea.All, 1, .35d, .1d, new[] { new ElementWeight(ElementalAlignment.Earth, 1d) }) };
        MagenheimDefinitionSet Snapshot(WorldgenCompatibilityPolicy value) => MagenheimDefinitionValidator.ValidateAndFreeze(MagenheimDefinitionValidator.CurrentSchemaVersion, CrystalRefinementService.CreateCanonicalDefaults(), geodes, value);
        var frozen = Snapshot(excluded);
        exclusions[0] = "foreign.changed";
        Assert(frozen.WorldgenCompatibility.ExcludedRegistrationKeys.Single() == desired.RegistrationKey, "Snapshot owns its exclusions.");
        Assert(Snapshot(policy).Fingerprint != Snapshot(insensitive).Fingerprint, "Compatibility affects fingerprint.");
        AssertThrows(() => Snapshot(policy with { ExcludedRegistrationKeys = new[] { "foreign.key" } }), "Foreign exclusions rejected.");
        AssertThrows(() => Snapshot(policy with { AdditiveOnly = false }), "Destructive snapshot rejected.");
    }
    private static void AssertThrows(Action action, string message)
    {
        var rejected = false;
        try { action(); } catch (InvalidOperationException) { rejected = true; }
        Assert(rejected, message);
    }
    private static void Assert(bool condition, string message)
    {
        _assertions++;
        if (!condition) throw new InvalidOperationException($"Assertion {_assertions} failed: {message}");
    }
}
