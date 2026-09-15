using System;
using System.Collections.Generic;
using System.Linq;

namespace Magenheim.Core.DeepFractures;

public sealed record PlannedEnemyEncounter(
    string Id,
    string TerritoryModuleId,
    int SpawnCount,
    EnemyEncounterDefinition Definition);

public sealed record DistrictEncounterPlan(
    string ModuleInstanceId,
    string PieceFamilyId,
    OccupationProfile Occupation,
    IReadOnlyList<PlannedEnemyEncounter> Encounters);

public sealed record DeepFractureEncounterPlan(
    int DungeonSeed,
    IReadOnlyList<DistrictEncounterPlan> Districts);

public sealed record DeepFractureEncounterValidationResult(
    IReadOnlyList<string> Errors)
{
    public bool IsValid => Errors.Count == 0;
}

public sealed record DeepFractureEncounterPolicy(
    int MaximumGroupsPerDistrict,
    int MaximumUnitsPerGroup,
    double TerritoryAffinityChance,
    double SupportGroupChance,
    int MixedGroupCount)
{
    public static DeepFractureEncounterPolicy Default => new(
        MaximumGroupsPerDistrict: 3,
        MaximumUnitsPerGroup: 8,
        TerritoryAffinityChance: 1.00d,
        SupportGroupChance: 0.45d,
        MixedGroupCount: 3);

    public void Validate()
    {
        if (MaximumGroupsPerDistrict < 3 || MaximumGroupsPerDistrict > 5)
            throw new InvalidOperationException("Deep Fracture maximum encounter groups per district must be between three and five because the Confluence Heart requires three distinct groups.");
        if (MaximumUnitsPerGroup < 1 || MaximumUnitsPerGroup > 12)
            throw new InvalidOperationException("Deep Fracture maximum units per encounter group must be between one and twelve.");
        ValidateChance(TerritoryAffinityChance, nameof(TerritoryAffinityChance));
        ValidateChance(SupportGroupChance, nameof(SupportGroupChance));
        if (MixedGroupCount < 2 || MixedGroupCount > MaximumGroupsPerDistrict)
            throw new InvalidOperationException("Deep Fracture mixed occupation group count must be at least two and cannot exceed the district group cap.");
    }

    private static void ValidateChance(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d || value > 1d)
            throw new InvalidOperationException($"{name} must be finite and between zero and one inclusive.");
    }
}

public static class DeepFractureEncounterPlanner
{
    private const uint EncounterStreamSalt = 0xE11C0A17u;

    private static readonly CreatureChassis[] UpperMixedPool =
    {
        CreatureChassis.AnnoyanceWisp,
        CreatureChassis.GeodeCrawler,
        CreatureChassis.Shardling,
        CreatureChassis.CrystalParasite,
    };

    private static readonly CreatureChassis[] MiddleMixedPool =
    {
        CreatureChassis.AnnoyanceWisp,
        CreatureChassis.GeodeCrawler,
        CreatureChassis.Shardling,
        CreatureChassis.CrystalParasite,
        CreatureChassis.CrystalRevenant,
        CreatureChassis.FacetSentry,
        CreatureChassis.StoneSentinel,
        CreatureChassis.CrystalHound,
        CreatureChassis.Burrower,
    };

    private static readonly CreatureChassis[] DeepMixedPool =
    {
        CreatureChassis.GeodeCrawler,
        CreatureChassis.Shardling,
        CreatureChassis.CrystalParasite,
        CreatureChassis.CrystalRevenant,
        CreatureChassis.FacetSentry,
        CreatureChassis.StoneSentinel,
        CreatureChassis.CrystalHound,
        CreatureChassis.Burrower,
        CreatureChassis.StoneGuardian,
        CreatureChassis.CrystalGolem,
        CreatureChassis.ObeliskWarden,
    };

    public static DeepFractureEncounterPlan Build(
        DeepFractureDungeonPlan dungeonPlan,
        DeepFractureEncounterPolicy? policyOverride = null)
    {
        if (dungeonPlan is null)
            throw new ArgumentNullException(nameof(dungeonPlan));

        var dungeonValidation = DeepFracturePlanValidator.Validate(dungeonPlan);
        if (!dungeonValidation.IsValid)
            throw new InvalidOperationException("Deep Fracture encounter planning requires a valid dungeon plan: " + string.Join(" | ", dungeonValidation.Errors));

        var policy = policyOverride ?? DeepFractureEncounterPolicy.Default;
        policy.Validate();

        var districts = new List<DistrictEncounterPlan>(dungeonPlan.Modules.Count);
        foreach (var module in dungeonPlan.Modules)
        {
            var moduleRandom = new DeterministicSequence(
                dungeonPlan.Seed,
                EncounterStreamSalt ^ StableHash(module.InstanceId));

            var encounters = BuildDistrictEncounters(dungeonPlan.Scale, module, moduleRandom, policy);
            districts.Add(new DistrictEncounterPlan(
                module.InstanceId,
                module.PieceFamilyId,
                module.Occupation,
                encounters));
        }

        var result = new DeepFractureEncounterPlan(dungeonPlan.Seed, districts.ToArray());
        var validation = DeepFractureEncounterValidator.Validate(dungeonPlan, result, policy);
        if (!validation.IsValid)
            throw new InvalidOperationException("Generated Deep Fracture encounter plan failed validation: " + string.Join(" | ", validation.Errors));

        return result;
    }

    private static IReadOnlyList<PlannedEnemyEncounter> BuildDistrictEncounters(
        DeepFractureScale scale,
        DungeonModuleState module,
        DeterministicSequence random,
        DeepFractureEncounterPolicy policy)
    {
        if (module.Occupation == OccupationProfile.Empty)
            return Array.Empty<PlannedEnemyEncounter>();

        if (string.Equals(module.PieceFamilyId, "DF-20", StringComparison.Ordinal))
            return BuildHeartEncounters(scale, module, random, policy);

        var chassis = new List<CreatureChassis>(policy.MaximumGroupsPerDistrict);
        if (module.Occupation == OccupationProfile.Mixed)
        {
            AddDistinct(chassis, TerritoryAffinity(module.PieceFamilyId));

            var pool = MixedPool(module.DepthBand).ToList();
            while (chassis.Count < policy.MixedGroupCount && pool.Count > 0)
            {
                var selectedIndex = random.NextInt(pool.Count);
                AddDistinct(chassis, pool[selectedIndex]);
                pool.RemoveAt(selectedIndex);
            }
        }
        else
        {
            AddDistinct(chassis, RequiredPrimaryChassis(module.Occupation));

            var affinity = TerritoryAffinity(module.PieceFamilyId);
            if (chassis.Count < policy.MaximumGroupsPerDistrict
                && !chassis.Contains(affinity)
                && random.NextDouble() < policy.TerritoryAffinityChance)
                AddDistinct(chassis, affinity);

            if (chassis.Count < policy.MaximumGroupsPerDistrict
                && random.NextDouble() < policy.SupportGroupChance)
            {
                var supportPool = SupportPool(module.Occupation, module.DepthBand)
                    .Where(candidate => !chassis.Contains(candidate))
                    .ToArray();
                if (supportPool.Length > 0)
                    AddDistinct(chassis, supportPool[random.NextInt(supportPool.Length)]);
            }
        }

        if (chassis.Count == 0)
            throw new InvalidOperationException($"Occupation profile {module.Occupation} produced no encounter chassis for module '{module.InstanceId}'.");

        var groups = new List<PlannedEnemyEncounter>(chassis.Count);
        for (var index = 0; index < chassis.Count; index++)
            groups.Add(CreateEncounter(scale, module, chassis[index], index, random, policy));

        return groups.ToArray();
    }

    private static IReadOnlyList<PlannedEnemyEncounter> BuildHeartEncounters(
        DeepFractureScale scale,
        DungeonModuleState module,
        DeterministicSequence random,
        DeepFractureEncounterPolicy policy)
    {
        if (module.ElementalStates.Count < 2)
            throw new InvalidOperationException("The Confluence Heart requires at least two elemental influences before encounters can be planned.");

        return new[]
        {
            CreateEncounter(scale, module, CreatureChassis.DeepColossus, 0, random, policy, EncounterRole.HeartGuardian, 0),
            CreateEncounter(scale, module, CreatureChassis.ObeliskWarden, 1, random, policy, null, 1),
            CreateEncounter(scale, module, CreatureChassis.CrystalGolem, 2, random, policy, null, 2),
        };
    }

    private static PlannedEnemyEncounter CreateEncounter(
        DeepFractureScale scale,
        DungeonModuleState module,
        CreatureChassis chassis,
        int groupIndex,
        DeterministicSequence random,
        DeepFractureEncounterPolicy policy,
        EncounterRole? roleOverride = null,
        int? forcedAlignmentIndex = null)
    {
        var chassisDefinition = DeepFractureCatalog.EnemyChassis.Single(definition => definition.Chassis == chassis);
        var alignment = SelectAlignment(module, random, forcedAlignmentIndex);
        if (!chassisDefinition.SupportsElementalAlignment)
            alignment = null;

        var components = chassisDefinition.SupportsCrystalComponents
            ? CreateComponents(chassis, alignment)
            : Array.Empty<CrystalComponentDefinition>();

        var encounterDefinition = new EnemyEncounterDefinition(
            chassis,
            alignment,
            roleOverride ?? chassisDefinition.PrimaryRole,
            components,
            module.PieceFamilyId);

        var id = $"{module.InstanceId}.encounter.{groupIndex + 1:00}";
        var count = DetermineSpawnCount(scale, module, chassis, random, policy);
        return new PlannedEnemyEncounter(id, module.InstanceId, count, encounterDefinition);
    }

    private static ElementalAlignment? SelectAlignment(
        DungeonModuleState module,
        DeterministicSequence random,
        int? forcedAlignmentIndex)
    {
        if (module.ElementalStates.Count == 0)
            return null;

        if (forcedAlignmentIndex.HasValue)
            return module.ElementalStates[forcedAlignmentIndex.Value % module.ElementalStates.Count];

        return module.ElementalStates[random.NextInt(module.ElementalStates.Count)];
    }

    private static IReadOnlyList<CrystalComponentDefinition> CreateComponents(
        CreatureChassis chassis,
        ElementalAlignment? alignment)
    {
        var components = new List<CrystalComponentDefinition>();
        var prefix = "magenheim.fracture.component." + chassis.ToString().ToLowerInvariant() + ".";

        if (alignment.HasValue)
            components.Add(new CrystalComponentDefinition(prefix + "elemental_focus", CrystalComponentFunction.ElementalAttack));

        switch (chassis)
        {
            case CreatureChassis.AnnoyanceWisp:
                components.Add(new CrystalComponentDefinition(prefix + "motion_core", CrystalComponentFunction.Movement));
                break;
            case CreatureChassis.GeodeCrawler:
                components.Add(new CrystalComponentDefinition(prefix + "carapace_core", CrystalComponentFunction.Armor));
                break;
            case CreatureChassis.Shardling:
                components.Add(new CrystalComponentDefinition(prefix + "locomotion_core", CrystalComponentFunction.Movement));
                break;
            case CreatureChassis.CrystalParasite:
                components.Add(new CrystalComponentDefinition(prefix + "regrowth_core", CrystalComponentFunction.Regeneration));
                break;
            case CreatureChassis.CrystalRevenant:
                components.Add(new CrystalComponentDefinition(prefix + "armor_core", CrystalComponentFunction.Armor));
                components.Add(new CrystalComponentDefinition(prefix + "resistance_core", CrystalComponentFunction.Resistance));
                break;
            case CreatureChassis.FacetSentry:
                components.Add(new CrystalComponentDefinition(prefix + "control_core", CrystalComponentFunction.EnvironmentalControl));
                break;
            case CreatureChassis.StoneSentinel:
                components.Add(new CrystalComponentDefinition(prefix + "armor_core", CrystalComponentFunction.Armor));
                components.Add(new CrystalComponentDefinition(prefix + "resistance_core", CrystalComponentFunction.Resistance));
                break;
            case CreatureChassis.CrystalHound:
                components.Add(new CrystalComponentDefinition(prefix + "motion_core", CrystalComponentFunction.Movement));
                components.Add(new CrystalComponentDefinition(prefix + "resistance_core", CrystalComponentFunction.Resistance));
                break;
            case CreatureChassis.Burrower:
                components.Add(new CrystalComponentDefinition(prefix + "burrow_core", CrystalComponentFunction.Movement));
                components.Add(new CrystalComponentDefinition(prefix + "armor_core", CrystalComponentFunction.Armor));
                break;
            case CreatureChassis.StoneGuardian:
                components.Add(new CrystalComponentDefinition(prefix + "armor_core", CrystalComponentFunction.Armor));
                components.Add(new CrystalComponentDefinition(prefix + "resistance_core", CrystalComponentFunction.Resistance));
                break;
            case CreatureChassis.CrystalGolem:
                components.Add(new CrystalComponentDefinition(prefix + "armor_core", CrystalComponentFunction.Armor));
                components.Add(new CrystalComponentDefinition(prefix + "regeneration_core", CrystalComponentFunction.Regeneration));
                components.Add(new CrystalComponentDefinition(prefix + "resistance_core", CrystalComponentFunction.Resistance));
                break;
            case CreatureChassis.ObeliskWarden:
                components.Add(new CrystalComponentDefinition(prefix + "control_core", CrystalComponentFunction.EnvironmentalControl));
                components.Add(new CrystalComponentDefinition(prefix + "resistance_core", CrystalComponentFunction.Resistance));
                components.Add(new CrystalComponentDefinition(prefix + "death_anchor", CrystalComponentFunction.DeathAnchor, RequiredForPermanentKill: true));
                break;
            case CreatureChassis.DeepColossus:
                components.Add(new CrystalComponentDefinition(prefix + "armor_core", CrystalComponentFunction.Armor));
                components.Add(new CrystalComponentDefinition(prefix + "resistance_core", CrystalComponentFunction.Resistance));
                components.Add(new CrystalComponentDefinition(prefix + "control_core", CrystalComponentFunction.EnvironmentalControl));
                components.Add(new CrystalComponentDefinition(prefix + "death_anchor", CrystalComponentFunction.DeathAnchor, RequiredForPermanentKill: true));
                break;
        }

        return components.ToArray();
    }

    private static int DetermineSpawnCount(
        DeepFractureScale scale,
        DungeonModuleState module,
        CreatureChassis chassis,
        DeterministicSequence random,
        DeepFractureEncounterPolicy policy)
    {
        var range = BaseCountRange(chassis);
        var count = range.Minimum + random.NextInt(range.Maximum - range.Minimum + 1);

        if (!IsLargeChassis(chassis))
        {
            if (module.DepthBand == DungeonDepthBand.DeepDomains)
                count++;
            if (module.Resources == ResourceState.Rich || module.Resources == ResourceState.GreaterGeode)
                count++;
            if (scale == DeepFractureScale.Grand)
                count++;
        }

        return Math.Min(count, policy.MaximumUnitsPerGroup);
    }

    private static (int Minimum, int Maximum) BaseCountRange(CreatureChassis chassis)
        => chassis switch
        {
            CreatureChassis.AnnoyanceWisp => (2, 5),
            CreatureChassis.GeodeCrawler => (2, 4),
            CreatureChassis.Shardling => (3, 6),
            CreatureChassis.CrystalParasite => (1, 3),
            CreatureChassis.CrystalRevenant => (1, 3),
            CreatureChassis.FacetSentry => (1, 3),
            CreatureChassis.StoneSentinel => (1, 2),
            CreatureChassis.CrystalHound => (2, 4),
            CreatureChassis.Burrower => (1, 3),
            CreatureChassis.StoneGuardian => (1, 1),
            CreatureChassis.CrystalGolem => (1, 1),
            CreatureChassis.ObeliskWarden => (1, 1),
            CreatureChassis.DeepColossus => (1, 1),
            _ => throw new InvalidOperationException($"No encounter count range is defined for chassis {chassis}."),
        };

    private static bool IsLargeChassis(CreatureChassis chassis)
        => chassis == CreatureChassis.StoneGuardian
            || chassis == CreatureChassis.CrystalGolem
            || chassis == CreatureChassis.ObeliskWarden
            || chassis == CreatureChassis.DeepColossus;

    private static CreatureChassis RequiredPrimaryChassis(OccupationProfile occupation)
        => occupation switch
        {
            OccupationProfile.WispRoaming => CreatureChassis.AnnoyanceWisp,
            OccupationProfile.CrawlerColony => CreatureChassis.GeodeCrawler,
            OccupationProfile.ShardlingFeeding => CreatureChassis.Shardling,
            OccupationProfile.RevenantHold => CreatureChassis.CrystalRevenant,
            OccupationProfile.SentryDefense => CreatureChassis.FacetSentry,
            OccupationProfile.GuardianObjective => CreatureChassis.StoneGuardian,
            OccupationProfile.GolemLair => CreatureChassis.CrystalGolem,
            OccupationProfile.Empty => throw new InvalidOperationException("Empty occupation does not define a primary enemy chassis."),
            OccupationProfile.Mixed => throw new InvalidOperationException("Mixed occupation selects from a depth and territory pool rather than one fixed primary chassis."),
            _ => throw new InvalidOperationException($"Unknown occupation profile {occupation}."),
        };

    private static IReadOnlyList<CreatureChassis> SupportPool(
        OccupationProfile occupation,
        DungeonDepthBand depthBand)
        => occupation switch
        {
            OccupationProfile.WispRoaming => depthBand == DungeonDepthBand.UpperFracture
                ? new[] { CreatureChassis.Shardling }
                : new[] { CreatureChassis.Shardling, CreatureChassis.CrystalHound },
            OccupationProfile.CrawlerColony => new[] { CreatureChassis.CrystalParasite, CreatureChassis.Shardling },
            OccupationProfile.ShardlingFeeding => new[] { CreatureChassis.AnnoyanceWisp, CreatureChassis.GeodeCrawler },
            OccupationProfile.RevenantHold => new[] { CreatureChassis.CrystalHound, CreatureChassis.FacetSentry },
            OccupationProfile.SentryDefense => new[] { CreatureChassis.StoneSentinel, CreatureChassis.CrystalHound },
            OccupationProfile.GuardianObjective => new[] { CreatureChassis.FacetSentry, CreatureChassis.StoneSentinel },
            OccupationProfile.GolemLair => new[] { CreatureChassis.StoneSentinel, CreatureChassis.FacetSentry },
            _ => Array.Empty<CreatureChassis>(),
        };

    private static IReadOnlyList<CreatureChassis> MixedPool(DungeonDepthBand depthBand)
        => depthBand switch
        {
            DungeonDepthBand.UpperFracture => UpperMixedPool,
            DungeonDepthBand.MiddleWorks => MiddleMixedPool,
            DungeonDepthBand.DeepDomains => DeepMixedPool,
            DungeonDepthBand.Heart => DeepMixedPool,
            _ => UpperMixedPool,
        };

    private static CreatureChassis TerritoryAffinity(string pieceFamilyId)
        => pieceFamilyId switch
        {
            "DF-01" => CreatureChassis.AnnoyanceWisp,
            "DF-02" => CreatureChassis.GeodeCrawler,
            "DF-03" => CreatureChassis.GeodeCrawler,
            "DF-04" => CreatureChassis.Burrower,
            "DF-05" => CreatureChassis.Shardling,
            "DF-06" => CreatureChassis.CrystalGolem,
            "DF-07" => CreatureChassis.CrystalHound,
            "DF-08" => CreatureChassis.StoneGuardian,
            "DF-09" => CreatureChassis.FacetSentry,
            "DF-10" => CreatureChassis.FacetSentry,
            "DF-11" => CreatureChassis.StoneSentinel,
            "DF-12" => CreatureChassis.StoneGuardian,
            "DF-13" => CreatureChassis.CrystalParasite,
            "DF-14" => CreatureChassis.CrystalParasite,
            "DF-15" => CreatureChassis.FacetSentry,
            "DF-16" => CreatureChassis.StoneSentinel,
            "DF-17" => CreatureChassis.CrystalRevenant,
            "DF-18" => CreatureChassis.CrystalRevenant,
            "DF-19" => CreatureChassis.FacetSentry,
            "DF-20" => CreatureChassis.DeepColossus,
            _ => throw new InvalidOperationException($"No Deep Fracture territory affinity is defined for piece '{pieceFamilyId}'."),
        };

    private static void AddDistinct(List<CreatureChassis> chassis, CreatureChassis candidate)
    {
        if (!chassis.Contains(candidate))
            chassis.Add(candidate);
    }

    private static uint StableHash(string value)
    {
        unchecked
        {
            var hash = 2166136261u;
            foreach (var character in value)
            {
                hash ^= character;
                hash *= 16777619u;
            }
            return hash;
        }
    }
}

public static class DeepFractureEncounterValidator
{
    public static DeepFractureEncounterValidationResult Validate(
        DeepFractureDungeonPlan dungeonPlan,
        DeepFractureEncounterPlan encounterPlan,
        DeepFractureEncounterPolicy? policyOverride = null)
    {
        if (dungeonPlan is null)
            throw new ArgumentNullException(nameof(dungeonPlan));
        if (encounterPlan is null)
            throw new ArgumentNullException(nameof(encounterPlan));

        var errors = new List<string>();
        var dungeonValidation = DeepFracturePlanValidator.Validate(dungeonPlan);
        if (!dungeonValidation.IsValid)
        {
            errors.Add("Encounter validation cannot admit an invalid dungeon plan.");
            errors.AddRange(dungeonValidation.Errors);
            return new DeepFractureEncounterValidationResult(errors);
        }

        var policy = policyOverride ?? DeepFractureEncounterPolicy.Default;
        try
        {
            policy.Validate();
        }
        catch (InvalidOperationException exception)
        {
            errors.Add(exception.Message);
            return new DeepFractureEncounterValidationResult(errors);
        }

        if (encounterPlan.DungeonSeed != dungeonPlan.Seed)
            errors.Add($"Encounter plan seed {encounterPlan.DungeonSeed} does not match dungeon seed {dungeonPlan.Seed}.");
        if (encounterPlan.Districts is null)
        {
            errors.Add("Encounter plan requires district entries.");
            return new DeepFractureEncounterValidationResult(errors);
        }
        if (encounterPlan.Districts.Count != dungeonPlan.Modules.Count)
            errors.Add($"Encounter plan must contain exactly one district entry per module; expected {dungeonPlan.Modules.Count}, received {encounterPlan.Districts.Count}.");

        var pieceIndex = DeepFractureCatalog.CreatePieceIndex();
        var moduleIndex = dungeonPlan.Modules.ToDictionary(module => module.InstanceId, StringComparer.Ordinal);
        var districtIds = new HashSet<string>(StringComparer.Ordinal);
        var encounterIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var district in encounterPlan.Districts)
        {
            if (district is null)
            {
                errors.Add("Encounter plan cannot contain a null district entry.");
                continue;
            }

            if (!districtIds.Add(district.ModuleInstanceId))
                errors.Add($"Encounter plan contains duplicate district '{district.ModuleInstanceId}'.");
            if (!moduleIndex.TryGetValue(district.ModuleInstanceId, out var module))
            {
                errors.Add($"Encounter district '{district.ModuleInstanceId}' does not exist in the dungeon plan.");
                continue;
            }

            if (!string.Equals(district.PieceFamilyId, module.PieceFamilyId, StringComparison.Ordinal))
                errors.Add($"Encounter district '{district.ModuleInstanceId}' piece family does not match dungeon module authority.");
            if (district.Occupation != module.Occupation)
                errors.Add($"Encounter district '{district.ModuleInstanceId}' occupation does not match dungeon module authority.");
            if (district.Encounters is null)
            {
                errors.Add($"Encounter district '{district.ModuleInstanceId}' requires an encounter collection.");
                continue;
            }
            if (district.Encounters.Count > policy.MaximumGroupsPerDistrict)
                errors.Add($"Encounter district '{district.ModuleInstanceId}' exceeds the configured group cap of {policy.MaximumGroupsPerDistrict}.");

            if (module.Occupation == OccupationProfile.Empty && district.Encounters.Count != 0)
                errors.Add($"Empty district '{district.ModuleInstanceId}' must not contain enemy encounters.");
            if (module.Occupation != OccupationProfile.Empty && district.Encounters.Count == 0)
                errors.Add($"Occupied district '{district.ModuleInstanceId}' requires at least one enemy encounter.");

            var districtChassis = new HashSet<CreatureChassis>();
            foreach (var planned in district.Encounters)
            {
                if (planned is null)
                {
                    errors.Add($"Encounter district '{district.ModuleInstanceId}' contains a null encounter.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(planned.Id) || !encounterIds.Add(planned.Id))
                    errors.Add($"Encounter ID '{planned.Id}' is empty or duplicated.");
                else if (!planned.Id.StartsWith(module.InstanceId + ".encounter.", StringComparison.Ordinal))
                    errors.Add($"Encounter ID '{planned.Id}' is not namespaced under its territory module '{module.InstanceId}'.");

                if (!string.Equals(planned.TerritoryModuleId, module.InstanceId, StringComparison.Ordinal))
                    errors.Add($"Encounter '{planned.Id}' territory module does not match district '{module.InstanceId}'.");
                if (planned.SpawnCount < 1 || planned.SpawnCount > policy.MaximumUnitsPerGroup)
                    errors.Add($"Encounter '{planned.Id}' spawn count {planned.SpawnCount} is outside the configured range 1-{policy.MaximumUnitsPerGroup}.");
                if (planned.Definition is null)
                {
                    errors.Add($"Encounter '{planned.Id}' requires an enemy definition.");
                    continue;
                }

                if (!districtChassis.Add(planned.Definition.Chassis))
                    errors.Add($"District '{module.InstanceId}' contains duplicate encounter chassis {planned.Definition.Chassis}; combine them into one group.");

                try
                {
                    planned.Definition.Validate(pieceIndex);
                }
                catch (InvalidOperationException exception)
                {
                    errors.Add($"Encounter '{planned.Id}' definition is invalid: {exception.Message}");
                }

                if (!string.Equals(planned.Definition.TerritoryPieceId, module.PieceFamilyId, StringComparison.Ordinal))
                    errors.Add($"Encounter '{planned.Id}' territory piece does not match module '{module.PieceFamilyId}'.");

                ValidateAlignmentAndComponents(module, planned, errors);
            }

            ValidateOccupationIdentity(module, districtChassis, errors);
            if (string.Equals(module.PieceFamilyId, "DF-20", StringComparison.Ordinal))
                ValidateHeartIdentity(module, district, errors);
        }

        foreach (var module in dungeonPlan.Modules)
        {
            if (!districtIds.Contains(module.InstanceId))
                errors.Add($"Dungeon module '{module.InstanceId}' has no encounter-plan district entry.");
        }

        return new DeepFractureEncounterValidationResult(errors);
    }

    private static void ValidateAlignmentAndComponents(
        DungeonModuleState module,
        PlannedEnemyEncounter planned,
        List<string> errors)
    {
        var alignment = planned.Definition.Alignment;
        var components = planned.Definition.Components ?? Array.Empty<CrystalComponentDefinition>();
        var elementalComponentCount = components.Count(component => component.Function == CrystalComponentFunction.ElementalAttack);

        if (module.ElementalStates.Count == 0)
        {
            if (alignment.HasValue)
                errors.Add($"Encounter '{planned.Id}' is element-aligned inside a district with no elemental state.");
            if (elementalComponentCount != 0)
                errors.Add($"Encounter '{planned.Id}' has an elemental attack component without an elemental district state.");
        }
        else
        {
            if (!alignment.HasValue || !module.ElementalStates.Contains(alignment.Value))
                errors.Add($"Encounter '{planned.Id}' alignment is not one of district '{module.InstanceId}' elemental influences.");
            if (elementalComponentCount != 1)
                errors.Add($"Element-aligned encounter '{planned.Id}' must expose exactly one elemental attack component.");
        }

        foreach (var component in components)
        {
            if (!component.Id.StartsWith("magenheim.fracture.component.", StringComparison.Ordinal))
                errors.Add($"Encounter '{planned.Id}' component '{component.Id}' is outside the Magenheim Deep Fracture namespace.");
        }

        if (planned.Definition.Chassis == CreatureChassis.CrystalGolem)
        {
            RequireComponent(planned, CrystalComponentFunction.Armor, errors);
            RequireComponent(planned, CrystalComponentFunction.Regeneration, errors);
            RequireComponent(planned, CrystalComponentFunction.Resistance, errors);
        }

        if (planned.Definition.Chassis == CreatureChassis.ObeliskWarden
            || planned.Definition.Chassis == CreatureChassis.DeepColossus)
        {
            var deathAnchor = components.SingleOrDefault(component => component.Function == CrystalComponentFunction.DeathAnchor);
            if (deathAnchor is null || !deathAnchor.RequiredForPermanentKill)
                errors.Add($"Encounter '{planned.Id}' must expose a required death-anchor crystal component.");
        }
    }

    private static void RequireComponent(
        PlannedEnemyEncounter planned,
        CrystalComponentFunction function,
        List<string> errors)
    {
        if (!planned.Definition.Components.Any(component => component.Function == function))
            errors.Add($"Encounter '{planned.Id}' chassis {planned.Definition.Chassis} is missing required component function {function}.");
    }

    private static void ValidateOccupationIdentity(
        DungeonModuleState module,
        ISet<CreatureChassis> chassis,
        List<string> errors)
    {
        if (module.Occupation == OccupationProfile.Empty)
            return;

        if (module.Occupation == OccupationProfile.Mixed)
        {
            if (chassis.Count < 2)
                errors.Add($"Mixed district '{module.InstanceId}' must contain at least two distinct enemy chassis.");
            return;
        }

        if (string.Equals(module.PieceFamilyId, "DF-20", StringComparison.Ordinal))
            return;

        var required = module.Occupation switch
        {
            OccupationProfile.WispRoaming => CreatureChassis.AnnoyanceWisp,
            OccupationProfile.CrawlerColony => CreatureChassis.GeodeCrawler,
            OccupationProfile.ShardlingFeeding => CreatureChassis.Shardling,
            OccupationProfile.RevenantHold => CreatureChassis.CrystalRevenant,
            OccupationProfile.SentryDefense => CreatureChassis.FacetSentry,
            OccupationProfile.GuardianObjective => CreatureChassis.StoneGuardian,
            OccupationProfile.GolemLair => CreatureChassis.CrystalGolem,
            _ => throw new InvalidOperationException($"Unknown occupation profile {module.Occupation}."),
        };

        if (!chassis.Contains(required))
            errors.Add($"District '{module.InstanceId}' occupation {module.Occupation} requires chassis {required}.");
    }

    private static void ValidateHeartIdentity(
        DungeonModuleState module,
        DistrictEncounterPlan district,
        List<string> errors)
    {
        var heartGuardian = district.Encounters.SingleOrDefault(encounter => encounter.Definition.Chassis == CreatureChassis.DeepColossus);
        if (heartGuardian is null || heartGuardian.Definition.Role != EncounterRole.HeartGuardian)
            errors.Add("DF-20 Confluence Heart requires exactly one Deep Colossus with the HeartGuardian role.");
        if (!district.Encounters.Any(encounter => encounter.Definition.Chassis == CreatureChassis.ObeliskWarden))
            errors.Add("DF-20 Confluence Heart requires an Obelisk Warden encounter.");
        if (!district.Encounters.Any(encounter => encounter.Definition.Chassis == CreatureChassis.CrystalGolem))
            errors.Add("DF-20 Confluence Heart requires a Crystal Golem encounter.");

        var representedAlignments = district.Encounters
            .Where(encounter => encounter.Definition.Alignment.HasValue)
            .Select(encounter => encounter.Definition.Alignment.Value)
            .Distinct()
            .Count();
        if (representedAlignments < 2 || representedAlignments > module.ElementalStates.Count)
            errors.Add("DF-20 Confluence Heart encounters must represent at least two of the Heart's elemental influences.");
    }
}
