using System;
using System.Collections.Generic;
using System.Linq;

namespace Magenheim.Core.DeepFractures;

public static class DeepFractureCatalog
{
    private static readonly DeepFracturePieceFamily[] PieceFamilyData =
    {
        Piece("DF-01", "The Fracture Descent", 3, false, true, false, DungeonDepthBand.UpperFracture),
        Piece("DF-02", "The Split Strata", 3, true, true, false, DungeonDepthBand.UpperFracture, DungeonDepthBand.MiddleWorks),
        Piece("DF-03", "The Geode Cathedral", 3, true, true, false, DungeonDepthBand.UpperFracture, DungeonDepthBand.MiddleWorks, DungeonDepthBand.DeepDomains),
        Piece("DF-04", "The Buried River", 3, true, true, false, DungeonDepthBand.UpperFracture, DungeonDepthBand.MiddleWorks, DungeonDepthBand.DeepDomains),
        Piece("DF-05", "The Thermal Veins", 3, true, true, false, DungeonDepthBand.MiddleWorks, DungeonDepthBand.DeepDomains),
        Piece("DF-06", "The Crucible Cavern", 3, true, true, false, DungeonDepthBand.DeepDomains),
        Piece("DF-07", "The Rime Fault", 3, true, true, false, DungeonDepthBand.DeepDomains),
        Piece("DF-08", "The Glacier Vault", 3, true, true, false, DungeonDepthBand.DeepDomains),
        Piece("DF-09", "The Conductor Chasm", 3, true, true, false, DungeonDepthBand.DeepDomains),
        Piece("DF-10", "The Fulmination Gallery", 3, true, true, false, DungeonDepthBand.MiddleWorks, DungeonDepthBand.DeepDomains),
        Piece("DF-11", "The Compression Hall", 3, true, true, false, DungeonDepthBand.MiddleWorks, DungeonDepthBand.DeepDomains),
        Piece("DF-12", "The Seismic Basin", 3, true, true, false, DungeonDepthBand.DeepDomains),
        Piece("DF-13", "The Contaminated Grotto", 3, true, true, false, DungeonDepthBand.DeepDomains),
        Piece("DF-14", "The Dissolution Works", 3, true, true, false, DungeonDepthBand.MiddleWorks, DungeonDepthBand.DeepDomains),
        Piece("DF-15", "The Prism Hall", 3, true, true, false, DungeonDepthBand.DeepDomains),
        Piece("DF-16", "The Sanctified Vault", 3, true, true, false, DungeonDepthBand.MiddleWorks, DungeonDepthBand.DeepDomains),
        Piece("DF-17", "The Echoing Deep", 3, true, true, false, DungeonDepthBand.DeepDomains),
        Piece("DF-18", "The Hollow Ossuary", 3, true, true, false, DungeonDepthBand.MiddleWorks, DungeonDepthBand.DeepDomains),
        Piece("DF-19", "The Shaping Works", 3, true, true, false, DungeonDepthBand.MiddleWorks, DungeonDepthBand.DeepDomains),
        Piece("DF-20", "The Confluence Heart", 1, true, true, true, DungeonDepthBand.Heart),
    };

    private static readonly DeepFractureScaleRule[] ScaleRuleData =
    {
        new(DeepFractureScale.Small, 12, 18),
        new(DeepFractureScale.Full, 24, 36),
        new(DeepFractureScale.Grand, 40, 50),
    };

    private static readonly EnemyChassisDefinition[] EnemyChassisData =
    {
        Enemy(CreatureChassis.AnnoyanceWisp, "Annoyance Wisp", EncounterRole.Nuisance, true, true),
        Enemy(CreatureChassis.GeodeCrawler, "Geode Crawler", EncounterRole.Territorial, true, true),
        Enemy(CreatureChassis.Shardling, "Shardling", EncounterRole.Skirmisher, true, true),
        Enemy(CreatureChassis.CrystalParasite, "Crystal Parasite", EncounterRole.Parasite, true, true),
        Enemy(CreatureChassis.CrystalRevenant, "Crystal Revenant", EncounterRole.Soldier, true, true),
        Enemy(CreatureChassis.FacetSentry, "Facet Sentry", EncounterRole.Sentry, true, true),
        Enemy(CreatureChassis.StoneSentinel, "Stone Sentinel", EncounterRole.Gatekeeper, true, true),
        Enemy(CreatureChassis.CrystalHound, "Crystal Hound", EncounterRole.Pursuer, true, true),
        Enemy(CreatureChassis.Burrower, "Burrower", EncounterRole.Ambusher, true, true),
        Enemy(CreatureChassis.StoneGuardian, "Stone Guardian", EncounterRole.Gatekeeper, true, true),
        Enemy(CreatureChassis.CrystalGolem, "Crystal Golem", EncounterRole.Elite, true, true),
        Enemy(CreatureChassis.ObeliskWarden, "Obelisk Warden", EncounterRole.Fortification, true, true),
        Enemy(CreatureChassis.DeepColossus, "Deep Colossus", EncounterRole.Colossus, true, true),
    };

    public static IReadOnlyList<DeepFracturePieceFamily> PieceFamilies => PieceFamilyData;
    public static IReadOnlyList<DeepFractureScaleRule> ScaleRules => ScaleRuleData;
    public static IReadOnlyList<EnemyChassisDefinition> EnemyChassis => EnemyChassisData;

    public static IReadOnlyDictionary<string, DeepFracturePieceFamily> CreatePieceIndex()
    {
        ValidateDefaults();
        return PieceFamilyData.ToDictionary(piece => piece.Id, StringComparer.Ordinal);
    }

    public static DeepFractureScaleRule GetScaleRule(DeepFractureScale scale)
    {
        var rule = ScaleRuleData.SingleOrDefault(candidate => candidate.Scale == scale);
        if (rule is null)
            throw new InvalidOperationException($"No Deep Fracture scale rule exists for {scale}.");
        return rule;
    }

    public static void ValidateDefaults()
    {
        if (PieceFamilyData.Length != 20)
            throw new InvalidOperationException("The canonical Deep Fracture catalog must contain exactly twenty major piece families.");
        if (EnemyChassisData.Length != 13)
            throw new InvalidOperationException("The canonical Deep Fracture enemy catalog must contain exactly thirteen principal creature chassis.");

        var pieceIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var piece in PieceFamilyData)
        {
            piece.Validate();
            if (!pieceIds.Add(piece.Id))
                throw new InvalidOperationException($"Duplicate Deep Fracture piece ID '{piece.Id}'.");
        }

        var chassis = new HashSet<CreatureChassis>();
        foreach (var enemy in EnemyChassisData)
        {
            enemy.Validate();
            if (!chassis.Add(enemy.Chassis))
                throw new InvalidOperationException($"Duplicate Deep Fracture enemy chassis '{enemy.Chassis}'.");
        }

        foreach (var rule in ScaleRuleData)
            rule.Validate();

        var descent = PieceFamilyData.Single(piece => piece.Id == "DF-01");
        if (!descent.AllowedBands.SequenceEqual(new[] { DungeonDepthBand.UpperFracture }))
            throw new InvalidOperationException("DF-01 must remain an Upper Fracture entrance district.");

        var heart = PieceFamilyData.Single(piece => piece.Id == "DF-20");
        if (!heart.CanHostHeartEncounter || !heart.AllowedBands.SequenceEqual(new[] { DungeonDepthBand.Heart }))
            throw new InvalidOperationException("DF-20 must remain the dedicated Confluence Heart district.");
    }

    private static DeepFracturePieceFamily Piece(string id, string name, int maximumRecommendedReuse, bool supportsElementalMutation, bool supportsOccupationMutation, bool canHostHeartEncounter, params DungeonDepthBand[] allowedBands)
        => new(id, name, allowedBands, maximumRecommendedReuse, supportsElementalMutation, supportsOccupationMutation, canHostHeartEncounter);

    private static EnemyChassisDefinition Enemy(CreatureChassis chassis, string name, EncounterRole primaryRole, bool supportsElementalAlignment, bool supportsCrystalComponents)
        => new(chassis, name, primaryRole, supportsElementalAlignment, supportsCrystalComponents);
}

public static class DeepFracturePlanValidator
{
    public static DeepFracturePlanValidationResult Validate(DeepFractureDungeonPlan plan)
    {
        if (plan is null)
            throw new ArgumentNullException(nameof(plan));

        DeepFractureCatalog.ValidateDefaults();
        var errors = new List<string>();
        if (plan.Modules is null)
        {
            errors.Add("Deep Fracture plan requires a module list.");
            return new DeepFracturePlanValidationResult(errors);
        }

        var scaleRule = DeepFractureCatalog.GetScaleRule(plan.Scale);
        if (plan.Modules.Count < scaleRule.MinimumMajorModules || plan.Modules.Count > scaleRule.MaximumMajorModules)
            errors.Add($"{plan.Scale} Deep Fracture requires {scaleRule.MinimumMajorModules}-{scaleRule.MaximumMajorModules} major modules; received {plan.Modules.Count}.");

        var pieceIndex = DeepFractureCatalog.CreatePieceIndex();
        var instanceIds = new HashSet<string>(StringComparer.Ordinal);
        var reuseCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var descentCount = 0;
        var heartCount = 0;
        DungeonDepthBand? priorBand = null;

        for (var index = 0; index < plan.Modules.Count; index++)
        {
            var module = plan.Modules[index];
            if (module is null)
            {
                errors.Add("Deep Fracture plan cannot contain a null module.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(module.InstanceId) || !instanceIds.Add(module.InstanceId))
                errors.Add($"Deep Fracture module instance ID '{module.InstanceId}' is empty or duplicated.");

            if (!pieceIndex.TryGetValue(module.PieceFamilyId, out var piece))
            {
                errors.Add($"Unknown Deep Fracture piece family '{module.PieceFamilyId}'.");
                continue;
            }

            if (!piece.AllowedBands.Contains(module.DepthBand))
                errors.Add($"{module.PieceFamilyId} is not allowed in depth band {module.DepthBand}.");

            if (priorBand.HasValue && module.DepthBand < priorBand.Value)
                errors.Add($"Depth band regressed from {priorBand.Value} to {module.DepthBand} at module '{module.InstanceId}'.");
            priorBand = module.DepthBand;

            reuseCounts.TryGetValue(module.PieceFamilyId, out var reuseCount);
            reuseCount++;
            reuseCounts[module.PieceFamilyId] = reuseCount;
            if (reuseCount > piece.MaximumRecommendedReuse)
                errors.Add($"{module.PieceFamilyId} exceeds its supported reuse count of {piece.MaximumRecommendedReuse}.");

            if (module.ElementalStates is null)
            {
                errors.Add($"{module.InstanceId} requires an elemental-state collection, even when empty.");
            }
            else
            {
                if (module.ElementalStates.Distinct().Count() != module.ElementalStates.Count)
                    errors.Add($"{module.InstanceId} contains duplicate elemental influences.");
                if (!piece.SupportsElementalMutation && module.ElementalStates.Count > 0)
                    errors.Add($"{module.PieceFamilyId} does not support elemental mutation.");
            }

            if (string.Equals(module.PieceFamilyId, "DF-01", StringComparison.Ordinal))
            {
                descentCount++;
                if (module.ElementalStates is not null && module.ElementalStates.Count > 0)
                    errors.Add("DF-01 Fracture Descent must not predeclare the dungeon's elemental domain.");
            }

            if (string.Equals(module.PieceFamilyId, "DF-20", StringComparison.Ordinal))
            {
                heartCount++;
                if (module.ElementalStates is null || module.ElementalStates.Count < 2)
                    errors.Add("DF-20 Confluence Heart requires at least two elemental influences.");
            }
        }

        if (descentCount != 1)
            errors.Add($"Deep Fracture plan requires exactly one DF-01 Fracture Descent; found {descentCount}.");
        if (heartCount != 1)
            errors.Add($"Deep Fracture plan requires exactly one DF-20 Confluence Heart; found {heartCount}.");

        if (plan.Modules.Count > 0)
        {
            if (!string.Equals(plan.Modules[0].PieceFamilyId, "DF-01", StringComparison.Ordinal))
                errors.Add("DF-01 Fracture Descent must be the first major module.");
            if (!string.Equals(plan.Modules[plan.Modules.Count - 1].PieceFamilyId, "DF-20", StringComparison.Ordinal))
                errors.Add("DF-20 Confluence Heart must be the final major module.");
        }

        return new DeepFracturePlanValidationResult(errors);
    }
}
