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

        var validPlan = new DeepFractureDungeonPlan(
            DeepFractureScale.Small,
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
                Module("m12", "DF-20", DungeonDepthBand.Heart),
            });

        var validResult = DeepFracturePlanValidator.Validate(validPlan);
        Assert(validResult.IsValid, "A canonical twelve-module Small fracture should validate.");

        var invalidBand = new DeepFractureDungeonPlan(
            DeepFractureScale.Small,
            validPlan.Modules.Select(module =>
                module.InstanceId == "m09"
                    ? module with { DepthBand = DungeonDepthBand.UpperFracture }
                    : module).ToArray());
        var invalidBandResult = DeepFracturePlanValidator.Validate(invalidBand);
        Assert(!invalidBandResult.IsValid && invalidBandResult.Errors.Any(error => error.Contains("DF-15", StringComparison.Ordinal)), "Piece/depth incompatibility must fail closed.");

        var overused = new List<DungeonModuleState>(validPlan.Modules)
        {
            Module("m13", "DF-03", DungeonDepthBand.MiddleWorks),
            Module("m14", "DF-03", DungeonDepthBand.DeepDomains),
            Module("m15", "DF-03", DungeonDepthBand.DeepDomains),
        };
        var overusedResult = DeepFracturePlanValidator.Validate(new DeepFractureDungeonPlan(DeepFractureScale.Small, overused));
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

        return assertions;
    }

    private static DungeonModuleState Module(
        string instanceId,
        string pieceFamilyId,
        DungeonDepthBand depthBand,
        ElementalAlignment? elementalState = null)
        => new(
            instanceId,
            pieceFamilyId,
            depthBand,
            elementalState,
            StructuralDamageState.Intact,
            OccupationProfile.Empty,
            ResourceState.Normal,
            ConnectionState.Open);
}
