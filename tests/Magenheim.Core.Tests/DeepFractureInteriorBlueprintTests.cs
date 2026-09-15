using System;
using System.Linq;
using Magenheim.Core;
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

        // Regression: a direct center-to-center passage that only touches the expanded
        // footprint boundary is still unsafe because the passage itself has width.
        // The exact slab test must reject the tangent direct segment and choose a bend.
        var tangentPolicy = policy with { ModuleFootprint = 8d, GridSpacing = 40d };
        tangentPolicy.Validate();
        var tangentBlueprint = new DeepFractureInteriorBlueprint(
            DeepFractureScale.Small,
            1,
            100d,
            new[]
            {
                Placement("from", new DeepFractureInteriorPoint(0d, 0d, 0d)),
                Placement("to", new DeepFractureInteriorPoint(40d, 0d, 40d)),
                Placement("blocker", new DeepFractureInteriorPoint(20d, 0d, 28d)),
            },
            new[]
            {
                new DeepFractureInteriorConnectionPlacement(
                    "tangent", "from", "to", DungeonConnectionRole.MainRoute,
                    DungeonConnectorKind.FaultCorridor, DungeonConnectionState.Open,
                    DeepFractureRuntimeConnectionMode.PhysicalPassage, true, true),
            });
        var tangentRoute = DeepFracturePassageRouter.Build(tangentBlueprint, tangentPolicy).Single();
        Assert(tangentRoute.Points.Count == 3, "A passage tangent to an expanded district footprint must bend instead of grazing the district boundary.");

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

                var physicalConnections = blueprint.Connections
                    .Where(connection => connection.Role == DungeonConnectionRole.MainRoute || connection.Role == DungeonConnectionRole.Branch)
                    .ToArray();
                Assert(physicalConnections.All(connection => connection.RuntimeMode == DeepFractureRuntimeConnectionMode.PhysicalPassage),
                    "Main-route and branch edges must compile to physical passages.");
                Assert(
                    blueprint.Connections
                        .Where(connection => connection.Role == DungeonConnectionRole.Loop || connection.Role == DungeonConnectionRole.Shortcut)
                        .All(connection => connection.RuntimeMode == DeepFractureRuntimeConnectionMode.TraversalLink),
                    "Loop and shortcut edges must compile to explicit traversal links rather than asking vanilla room placement to solve cycles.");

                var routes = DeepFracturePassageRouter.Build(blueprint, policy);
                Assert(routes.Count == physicalConnections.Length, "Every physical graph edge must receive exactly one authored passage route.");
                Assert(routes.Select(route => route.ConnectionId).OrderBy(id => id).SequenceEqual(physicalConnections.Select(connection => connection.Id).OrderBy(id => id)),
                    "Authored passage routes must preserve physical connection identity exactly.");
                foreach (var route in routes)
                {
                    var edge = blueprint.Connections.Single(value => value.Id == route.ConnectionId);
                    Assert(route.Points.First() == blueprint.Modules.Single(value => value.ModuleInstanceId == edge.FromModuleId).Center
                        && route.Points.Last() == blueprint.Modules.Single(value => value.ModuleInstanceId == edge.ToModuleId).Center,
                        "Detours preserve both endpoint elevations and identities.");
                    foreach (var obstacle in blueprint.Modules.Where(value => value.ModuleInstanceId != edge.FromModuleId && value.ModuleInstanceId != edge.ToModuleId))
                        for (var segment = 1; segment < route.Points.Count; segment++)
                            Assert(!Intersects(route.Points[segment - 1], route.Points[segment], obstacle.Center, policy.ModuleFootprint / 2d + 4d),
                                "Every routed segment must clear the expanded unrelated district footprint.");
                    Assert(route.Points.Count >= 2, $"Physical route {route.ConnectionId} must contain at least two points.");
                    Assert(route.Points.All(point => !double.IsNaN(point.X) && !double.IsInfinity(point.X) && !double.IsNaN(point.Z) && !double.IsInfinity(point.Z)),
                        $"Physical route {route.ConnectionId} must contain finite coordinates.");
                }

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
        Assert(
            DeepFracturePassageRouter.Build(deterministicA).Zip(DeepFracturePassageRouter.Build(deterministicB),
                (a, b) => a.ConnectionId == b.ConnectionId && a.Points.SequenceEqual(b.Points)).All(equal => equal),
            "Equal interior authority must compile to identical collision-safe physical routes.");

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

    // Independent separating-axis check (router uses slab clipping).
    private static bool Intersects(DeepFractureInteriorPoint a, DeepFractureInteriorPoint b, DeepFractureInteriorPoint center, double extent)
    {
        if (Math.Max(a.X, b.X) < center.X - extent || Math.Min(a.X, b.X) > center.X + extent
            || Math.Max(a.Z, b.Z) < center.Z - extent || Math.Min(a.Z, b.Z) > center.Z + extent) return false;
        var sides = (from x in new[] { center.X - extent, center.X + extent }
                     from z in new[] { center.Z - extent, center.Z + extent }
                     select (b.X - a.X) * (z - a.Z) - (b.Z - a.Z) * (x - a.X)).ToArray();
        return !(sides.All(value => value > 0d) || sides.All(value => value < 0d));
    }

    private static DeepFractureInteriorModulePlacement Placement(string id, DeepFractureInteriorPoint center)
        => new(
            0,
            id,
            "DF-01",
            DungeonDepthBand.UpperFracture,
            center,
            0,
            Array.Empty<ElementalAlignment>(),
            OccupationProfile.Empty,
            ResourceState.Sparse,
            Array.Empty<PlannedEnemyEncounter>());

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
