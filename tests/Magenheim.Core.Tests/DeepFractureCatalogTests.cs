using System;
using System.Collections.Generic;
using System.Linq;
using Magenheim.Core;
using Magenheim.Core.DeepFractures;

internal static class DeepFractureCatalogTests
{
    public static int Run()
    {
        var assertions = 0;

        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition)
                throw new InvalidOperationException($"Deep Fracture assertion {assertions} failed: {message}");
        }

        DeepFractureCatalog.ValidateDefaults();

        Assert(DeepFractureCatalog.PieceFamilies.Count == 20, "Canonical dungeon grammar must expose twenty major piece families.");
        Assert(DeepFractureCatalog.PieceFamilies.Select(piece => piece.Id).Distinct(StringComparer.Ordinal).Count() == 20, "Piece family IDs must be unique.");
        Assert(DeepFractureCatalog.PieceFamilies.Single(piece => piece.Id == "DF-01").Name == "The Fracture Descent", "DF-01 identity drifted.");
        Assert(DeepFractureCatalog.PieceFamilies.Single(piece => piece.Id == "DF-20").CanHostHeartEncounter, "DF-20 must own Heart encounter capability.");

        var small = DeepFractureCatalog.GetScaleRule(DeepFractureScale.Small);
        var full = DeepFractureCatalog.GetScaleRule(DeepFractureScale.Full);
        var grand = DeepFractureCatalog.GetScaleRule(DeepFractureScale.Grand);
        Assert(small.MinimumMajorModules == 12 && small.MaximumMajorModules == 18, "Small fracture range must remain 12-18.");
        Assert(full.MinimumMajorModules == 24 && full.MaximumMajorModules == 36, "Full fracture range must remain 24-36.");
        Assert(grand.MinimumMajorModules == 40 && grand.MaximumMajorModules == 50, "Grand fracture range must remain 40-50.");

        Assert(DeepFractureCatalog.EnemyChassis.Count == 13, "Enemy ecology must expose thirteen principal chassis.");
        Assert(DeepFractureCatalog.EnemyChassis.Any(enemy => enemy.Chassis == CreatureChassis.AnnoyanceWisp && enemy.PrimaryRole == EncounterRole.Nuisance), "Annoyance Wisp role drifted.");
        Assert(DeepFractureCatalog.EnemyChassis.Any(enemy => enemy.Chassis == CreatureChassis.DeepColossus && enemy.PrimaryRole == EncounterRole.Colossus), "Deep Colossus role drifted.");

        var validStructuralPlan = new DeepFractureDungeonPlan(
            DeepFractureScale.Small,
            42,
            new[]
            {
                Module("m01", "DF-01", DungeonDepthBand.UpperFracture),
                Module("m02", "DF-02", DungeonDepthBand.UpperFracture),
                Module("m03", "DF-03", DungeonDepthBand.UpperFracture, ElementalAlignment.Earth),
                Module("m04", "DF-04", DungeonDepthBand.MiddleWorks, ElementalAlignment.Frost),
                Module("m05", "DF-05", DungeonDepthBand.MiddleWorks, ElementalAlignment.Fire),
                Module("m06", "DF-10", DungeonDepthBand.MiddleWorks, ElementalAlignment.Storm),
                Module("m07", "DF-14", DungeonDepthBand.MiddleWorks, ElementalAlignment.Venom),
                Module("m08", "DF-19", DungeonDepthBand.MiddleWorks),
                Module("m09", "DF-15", DungeonDepthBand.DeepDomains, ElementalAlignment.Radiance),
                Module("m10", "DF-17", DungeonDepthBand.DeepDomains, ElementalAlignment.Spirit),
                Module("m11", "DF-12", DungeonDepthBand.DeepDomains, ElementalAlignment.Earth),
                Module("m12", "DF-20", DungeonDepthBand.Heart, ElementalAlignment.Fire, ElementalAlignment.Frost, ElementalAlignment.Storm),
            },
            Array.Empty<DungeonConnection>());

        var validPlan = DeepFractureConnectionPlanner.Attach(validStructuralPlan);
        var validResult = DeepFracturePlanValidator.Validate(validPlan);
        Assert(validResult.IsValid, "A canonical twelve-module Small fracture with generated connections should validate.");

        var invalidBand = validPlan with
        {
            Modules = validPlan.Modules.Select(module =>
                module.InstanceId == "m09"
                    ? module with { DepthBand = DungeonDepthBand.UpperFracture }
                    : module).ToArray(),
        };
        var invalidBandResult = DeepFracturePlanValidator.Validate(invalidBand);
        Assert(!invalidBandResult.IsValid && invalidBandResult.Errors.Any(error => error.Contains("DF-15", StringComparison.Ordinal)), "Piece/depth incompatibility must fail closed.");

        var overusedModules = new List<DungeonModuleState>(validStructuralPlan.Modules);
        overusedModules.RemoveAt(11);
        overusedModules.Add(Module("m13", "DF-03", DungeonDepthBand.DeepDomains));
        overusedModules.Add(Module("m14", "DF-03", DungeonDepthBand.DeepDomains));
        overusedModules.Add(Module("m15", "DF-03", DungeonDepthBand.DeepDomains));
        overusedModules.Add(Module("m16", "DF-20", DungeonDepthBand.Heart, ElementalAlignment.Earth, ElementalAlignment.Storm));
        var overusedStructuralPlan = new DeepFractureDungeonPlan(DeepFractureScale.Small, 42, overusedModules, Array.Empty<DungeonConnection>());
        var overusedPlan = DeepFractureConnectionPlanner.Attach(overusedStructuralPlan);
        var overusedResult = DeepFracturePlanValidator.Validate(overusedPlan);
        Assert(!overusedResult.IsValid && overusedResult.Errors.Any(error => error.Contains("reuse", StringComparison.OrdinalIgnoreCase)), "Fourth use of a standard major piece must be rejected.");

        var encounter = new EnemyEncounterDefinition(
            CreatureChassis.CrystalGolem,
            ElementalAlignment.Storm,
            EncounterRole.Elite,
            new[]
            {
                new CrystalComponentDefinition("conductor.left", CrystalComponentFunction.ElementalAttack),
                new CrystalComponentDefinition("conductor.right", CrystalComponentFunction.EnvironmentalControl),
            },
            "DF-09");
        encounter.Validate(DeepFractureCatalog.CreatePieceIndex());
        assertions++;

        var defaultPolicy = DeepFractureGenerationPolicy.Default;
        defaultPolicy.Validate();
        Assert(!defaultPolicy.DomainAlignments.Contains(ElementalAlignment.Seidr), "Deep Fracture default domains must follow the seven designed dungeon elements rather than silently adding Seidr.");

        var connectionPolicy = DeepFractureConnectionPolicy.Default;
        connectionPolicy.Validate();
        Assert(connectionPolicy.SmallShortcutCount == 1 && connectionPolicy.FullShortcutCount == 2 && connectionPolicy.GrandShortcutCount == 3, "Default shortcut counts must scale with expedition size.");

        foreach (var scale in new[] { DeepFractureScale.Small, DeepFractureScale.Full, DeepFractureScale.Grand })
        {
            for (var seed = 1; seed <= 8; seed++)
            {
                var generated = DeepFractureLayoutPlanner.Build(new DeepFractureGenerationRequest(scale, seed));
                var validation = DeepFracturePlanValidator.Validate(generated);
                Assert(validation.IsValid, $"Generated {scale} fracture for seed {seed} must validate.");
                Assert(generated.Modules[0].PieceFamilyId == "DF-01", "Generated fracture must begin with DF-01.");
                Assert(generated.Modules[generated.Modules.Count - 1].PieceFamilyId == "DF-20", "Generated fracture must end with DF-20.");
                Assert(generated.Modules.Where(module => module.DepthBand == DungeonDepthBand.DeepDomains).All(module => module.ElementalStates.Count >= 1), "Generated Deep Domain districts must carry elemental influence.");
                Assert(generated.Modules[generated.Modules.Count - 1].ElementalStates.Count >= 2, "Generated Confluence Heart must combine multiple elemental influences.");
                Assert(generated.Connections.Count(connection => connection.Role == DungeonConnectionRole.Shortcut) == connectionPolicy.GetShortcutCount(scale), "Generated shortcut count must follow scale policy.");
                Assert(generated.Connections.Where(connection => connection.Role == DungeonConnectionRole.Shortcut).All(connection => connection.State == DungeonConnectionState.Dormant && !connection.RequiredForHeartReachability && connection.IsBidirectional), "Shortcuts must begin dormant, optional, and return-capable.");
                Assert(generated.Connections.Any(connection => connection.Role == DungeonConnectionRole.Branch), "Default graph generation must produce exploration branches rather than a single corridor chain.");
            }
        }

        var deterministicA = DeepFractureLayoutPlanner.Build(new DeepFractureGenerationRequest(DeepFractureScale.Full, 8675309));
        var deterministicB = DeepFractureLayoutPlanner.Build(new DeepFractureGenerationRequest(DeepFractureScale.Full, 8675309));
        Assert(PlansEquivalent(deterministicA, deterministicB), "Equal scale, seed, and policy must produce an identical Deep Fracture plan and connection graph.");

        var severedRequiredRoute = deterministicA with
        {
            Connections = deterministicA.Connections.Where(connection => connection != deterministicA.Connections.First(candidate => candidate.RequiredForHeartReachability)).ToArray(),
        };
        var severedResult = DeepFracturePlanValidator.Validate(severedRequiredRoute);
        Assert(!severedResult.IsValid && severedResult.Errors.Any(error => error.Contains("Heart", StringComparison.OrdinalIgnoreCase)), "Removing a required route edge must break Heart reachability validation.");

        var invalidPolicyThrew = false;
        try
        {
            DeepFractureLayoutPlanner.Build(new DeepFractureGenerationRequest(
                DeepFractureScale.Small,
                7,
                DeepFractureGenerationPolicy.Default with { DeepDomainsFraction = 0.60d }));
        }
        catch (InvalidOperationException)
        {
            invalidPolicyThrew = true;
        }
        Assert(invalidPolicyThrew, "Invalid depth-band policy must fail before generation.");

        var stackedConnectionsThrew = false;
        try
        {
            DeepFractureConnectionPlanner.Attach(deterministicA);
        }
        catch (InvalidOperationException)
        {
            stackedConnectionsThrew = true;
        }
        Assert(stackedConnectionsThrew, "Connection planning must reject stacked mutation over an already-connected plan.");

        return assertions;
    }

    private static DungeonModuleState Module(
        string instanceId,
        string pieceFamilyId,
        DungeonDepthBand depthBand,
        params ElementalAlignment[] elementalStates)
        => new(
            instanceId,
            pieceFamilyId,
            depthBand,
            elementalStates,
            StructuralDamageState.Intact,
            OccupationProfile.Empty,
            ResourceState.Normal);

    private static bool PlansEquivalent(DeepFractureDungeonPlan left, DeepFractureDungeonPlan right)
    {
        if (left.Scale != right.Scale || left.Seed != right.Seed || left.Modules.Count != right.Modules.Count || left.Connections.Count != right.Connections.Count)
            return false;

        for (var index = 0; index < left.Modules.Count; index++)
        {
            var a = left.Modules[index];
            var b = right.Modules[index];
            if (!string.Equals(a.InstanceId, b.InstanceId, StringComparison.Ordinal)
                || !string.Equals(a.PieceFamilyId, b.PieceFamilyId, StringComparison.Ordinal)
                || a.DepthBand != b.DepthBand
                || a.StructuralDamage != b.StructuralDamage
                || a.Occupation != b.Occupation
                || a.Resources != b.Resources
                || !a.ElementalStates.SequenceEqual(b.ElementalStates))
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
