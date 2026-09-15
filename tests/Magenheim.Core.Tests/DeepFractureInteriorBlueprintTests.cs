using System;
using System.Linq;
using Magenheim.Core.DeepFractures;

internal static class DeepFractureInteriorBlueprintTests
{
    public static int Run()
    {
        var assertions = 0;

        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition)
                throw new InvalidOperationException($"Deep Fracture interior blueprint assertion {assertions} failed: {message}");
        }

        var policy = DeepFractureInteriorProjectionPolicy.Default;
        policy.Validate();
        Assert(policy.SmallColumns == 4 && policy.FullColumns == 6 && policy.GrandColumns == 8, "Default projection columns must scale with expedition size.");
        Assert(policy.GridSpacing > policy.ModuleFootprint, "Default projection must reserve physical clearance between major districts.");

        foreach (var scale in new[] { DeepFractureScale.Small, DeepFractureScale.Full, DeepFractureScale.Grand })
        {
            for (var seed = 1; seed <= 6; seed++)
            {
                var dungeon = DeepFractureLayoutPlanner.Build(new DeepFractureGenerationRequest(scale, seed));
                var encounters = DeepFractureEncounterPlanner.Build(dungeon);
                var blueprint = DeepFractureInteriorBlueprintCompiler.Build(dungeon, encounters, policy);
                var validation = DeepFractureInteriorBlueprintValidator.Validate(dungeon, encounters, blueprint, policy);

                Assert(validation.IsValid, $"Projected {scale} fracture for seed {seed} must validate.");
                Assert(blueprint.Modules.Count == dungeon.Modules.Count, "Interior projection must preserve module count.");
                Assert(blueprint.Connections.Count == dungeon.Connections.Count, "Interior projection must preserve connection count.");
                Assert(blueprint.Modules[0].PieceFamilyId == "DF-01", "Interior projection must begin with the Fracture Descent.");
                Assert(blueprint.Modules[blueprint.Modules.Count - 1].PieceFamilyId == "DF-20", "Interior projection must end with the Confluence Heart.");
                Assert(blueprint.InteriorRadius > 0d, "Interior projection must expose a positive containing radius.");

                Assert(
                    blueprint.Connections
                        .Where(connection => connection.Role == DungeonConnectionRole.MainRoute || connection.Role == DungeonConnectionRole.Branch)
                        .All(connection => connection.RuntimeMode == DeepFractureRuntimeConnectionMode.PhysicalPassage),
                    "Main-route and branch edges must compile to physical passages.");
                Assert(
                    blueprint.Connections
                        .Where(connection => connection.Role == DungeonConnectionRole.Loop || connection.Role == DungeonConnectionRole.Shortcut)
                        .All(connection => connection.RuntimeMode == DeepFractureRuntimeConnectionMode.TraversalLink),
                    "Loop and shortcut edges must compile to explicit traversal links rather than asking vanilla room placement to solve cycles.");

                var moduleIds = blueprint.Modules.Select(module => module.ModuleInstanceId).ToArray();
                Assert(moduleIds.SequenceEqual(dungeon.Modules.Select(module => module.InstanceId)), "Interior projection must preserve authoritative module order.");

                foreach (var module in blueprint.Modules)
                {
                    var district = encounters.Districts.Single(candidate => candidate.ModuleInstanceId == module.ModuleInstanceId);
                    Assert(
                        module.Encounters.Select(encounter => encounter.Id).SequenceEqual(district.Encounters.Select(encounter => encounter.Id)),
                        $"Projected district {module.ModuleInstanceId} must preserve its encounter groups exactly.");
                }

                for (var left = 0; left < blueprint.Modules.Count; left++)
                {
                    for (var right = left + 1; right < blueprint.Modules.Count; right++)
                    {
                        Assert(
                            blueprint.Modules[left].Center.HorizontalDistanceTo(blueprint.Modules[right].Center) >= policy.ModuleFootprint,
                            $"Projected modules {blueprint.Modules[left].ModuleInstanceId} and {blueprint.Modules[right].ModuleInstanceId} must not overlap their nominal footprints.");
                    }
                }
            }
        }

        var deterministicDungeon = DeepFractureLayoutPlanner.Build(new DeepFractureGenerationRequest(DeepFractureScale.Full, 24680));
        var deterministicEncounters = DeepFractureEncounterPlanner.Build(deterministicDungeon);
        var deterministicA = DeepFractureInteriorBlueprintCompiler.Build(deterministicDungeon, deterministicEncounters);
        var deterministicB = DeepFractureInteriorBlueprintCompiler.Build(deterministicDungeon, deterministicEncounters);
        Assert(BlueprintsEquivalent(deterministicA, deterministicB), "Equal dungeon and encounter authority must compile to an identical interior blueprint.");

        var invalidEncounterPlan = deterministicEncounters with { DungeonSeed = deterministicEncounters.DungeonSeed + 1 };
        var invalidEncounterThrew = false;
        try
        {
            DeepFractureInteriorBlueprintCompiler.Build(deterministicDungeon, invalidEncounterPlan);
        }
        catch (InvalidOperationException)
        {
            invalidEncounterThrew = true;
        }
        Assert(invalidEncounterThrew, "Interior projection must reject an encounter plan whose seed drifted from dungeon authority.");

        var invalidPolicyThrew = false;
        try
        {
            DeepFractureInteriorBlueprintCompiler.Build(
                deterministicDungeon,
                deterministicEncounters,
                policy with { GridSpacing = policy.ModuleFootprint });
        }
        catch (InvalidOperationException)
        {
            invalidPolicyThrew = true;
        }
        Assert(invalidPolicyThrew, "Interior projection must reject spacing that cannot preserve module clearance.");

        var firstConnection = deterministicA.Connections[0];
        var tamperedConnections = deterministicA.Connections.ToArray();
        tamperedConnections[0] = firstConnection with
        {
            RuntimeMode = firstConnection.RuntimeMode == DeepFractureRuntimeConnectionMode.PhysicalPassage
                ? DeepFractureRuntimeConnectionMode.TraversalLink
                : DeepFractureRuntimeConnectionMode.PhysicalPassage,
        };
        var tampered = deterministicA with { Connections = tamperedConnections };
        var tamperedValidation = DeepFractureInteriorBlueprintValidator.Validate(
            deterministicDungeon,
            deterministicEncounters,
            tampered);
        Assert(!tamperedValidation.IsValid && tamperedValidation.Errors.Any(error => error.Contains("runtime mode", StringComparison.OrdinalIgnoreCase)), "Interior validation must reject connection-mode drift from the authoritative graph role.");

        return assertions;
    }

    private static bool BlueprintsEquivalent(DeepFractureInteriorBlueprint left, DeepFractureInteriorBlueprint right)
    {
        if (left.Scale != right.Scale
            || left.Seed != right.Seed
            || left.InteriorRadius != right.InteriorRadius
            || left.Modules.Count != right.Modules.Count
            || left.Connections.Count != right.Connections.Count)
            return false;

        for (var index = 0; index < left.Modules.Count; index++)
        {
            var a = left.Modules[index];
            var b = right.Modules[index];
            if (a.SequenceIndex != b.SequenceIndex
                || !string.Equals(a.ModuleInstanceId, b.ModuleInstanceId, StringComparison.Ordinal)
                || !string.Equals(a.PieceFamilyId, b.PieceFamilyId, StringComparison.Ordinal)
                || a.DepthBand != b.DepthBand
                || a.Center != b.Center
                || a.YawDegrees != b.YawDegrees
                || a.Occupation != b.Occupation
                || a.Resources != b.Resources
                || !a.ElementalStates.SequenceEqual(b.ElementalStates)
                || !a.Encounters.Select(encounter => encounter.Id).SequenceEqual(b.Encounters.Select(encounter => encounter.Id)))
                return false;
        }

        for (var index = 0; index < left.Connections.Count; index++)
        {
            if (!Equals(left.Connections[index], right.Connections[index]))
                return false;
        }

        return true;
    }
}
