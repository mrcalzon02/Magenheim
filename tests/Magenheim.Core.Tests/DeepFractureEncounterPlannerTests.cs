using System;
using System.Collections.Generic;
using System.Linq;
using Magenheim.Core;
using Magenheim.Core.DeepFractures;

internal static class DeepFractureEncounterPlannerTests
{
    public static int Run()
    {
        var assertions = 0;

        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition)
                throw new InvalidOperationException($"Deep Fracture encounter assertion {assertions} failed: {message}");
        }

        var policy = DeepFractureEncounterPolicy.Default;
        policy.Validate();
        Assert(policy.MaximumGroupsPerDistrict == 3, "Default encounter group cap must remain three groups per district.");
        Assert(policy.TerritoryAffinityChance == 1d, "Default territory affinity must materially influence occupied districts.");

        foreach (var scale in new[] { DeepFractureScale.Small, DeepFractureScale.Full, DeepFractureScale.Grand })
        {
            for (var seed = 11; seed <= 18; seed++)
            {
                var dungeon = DeepFractureLayoutPlanner.Build(new DeepFractureGenerationRequest(scale, seed));
                var encounters = DeepFractureEncounterPlanner.Build(dungeon);
                var validation = DeepFractureEncounterValidator.Validate(dungeon, encounters);
                Assert(validation.IsValid, $"Generated {scale} encounter plan for seed {seed} must validate.");
                Assert(encounters.Districts.Count == dungeon.Modules.Count, "Encounter planner must emit exactly one district entry per dungeon module.");

                foreach (var district in encounters.Districts)
                {
                    var module = dungeon.Modules.Single(candidate => candidate.InstanceId == district.ModuleInstanceId);
                    if (module.Occupation == OccupationProfile.Empty)
                        Assert(district.Encounters.Count == 0, "Empty occupation must remain encounter-free.");
                    else
                        Assert(district.Encounters.Count > 0, "Occupied district must produce at least one encounter group.");

                    foreach (var encounter in district.Encounters)
                    {
                        Assert(encounter.SpawnCount >= 1 && encounter.SpawnCount <= policy.MaximumUnitsPerGroup, "Encounter spawn counts must remain inside policy bounds.");
                        if (module.ElementalStates.Count == 0)
                            Assert(!encounter.Definition.Alignment.HasValue, "Unaligned districts must not manufacture enemy elemental alignment.");
                        else
                            Assert(encounter.Definition.Alignment.HasValue && module.ElementalStates.Contains(encounter.Definition.Alignment.Value), "Encounter alignment must be selected from the district's elemental influences.");
                    }
                }

                var heartDistrict = encounters.Districts.Single(district => district.PieceFamilyId == "DF-20");
                Assert(heartDistrict.Encounters.Any(encounter => encounter.Definition.Chassis == CreatureChassis.DeepColossus && encounter.Definition.Role == EncounterRole.HeartGuardian), "Confluence Heart must contain a Deep Colossus HeartGuardian.");
                Assert(heartDistrict.Encounters.Any(encounter => encounter.Definition.Chassis == CreatureChassis.ObeliskWarden), "Confluence Heart must contain an Obelisk Warden.");
                Assert(heartDistrict.Encounters.Any(encounter => encounter.Definition.Chassis == CreatureChassis.CrystalGolem), "Confluence Heart must contain a Crystal Golem.");
                Assert(heartDistrict.Encounters.Where(encounter => encounter.Definition.Alignment.HasValue).Select(encounter => encounter.Definition.Alignment!.Value).Distinct().Count() >= 2, "Confluence Heart enemy groups must represent multiple Heart elements.");

                var colossus = heartDistrict.Encounters.Single(encounter => encounter.Definition.Chassis == CreatureChassis.DeepColossus);
                Assert(colossus.Definition.Components.Any(component => component.Function == CrystalComponentFunction.DeathAnchor && component.RequiredForPermanentKill), "Deep Colossus must expose a required death-anchor component.");
                var golem = heartDistrict.Encounters.Single(encounter => encounter.Definition.Chassis == CreatureChassis.CrystalGolem);
                Assert(golem.Definition.Components.Any(component => component.Function == CrystalComponentFunction.Armor), "Crystal Golem must expose an armor component.");
                Assert(golem.Definition.Components.Any(component => component.Function == CrystalComponentFunction.Regeneration), "Crystal Golem must expose a regeneration component.");
                Assert(golem.Definition.Components.Any(component => component.Function == CrystalComponentFunction.Resistance), "Crystal Golem must expose a resistance component.");
            }
        }

        var deterministicDungeon = DeepFractureLayoutPlanner.Build(new DeepFractureGenerationRequest(DeepFractureScale.Full, 424242));
        var deterministicA = DeepFractureEncounterPlanner.Build(deterministicDungeon);
        var deterministicB = DeepFractureEncounterPlanner.Build(deterministicDungeon);
        Assert(EncounterPlansEquivalent(deterministicA, deterministicB), "Equal dungeon authority and encounter policy must produce identical encounter populations.");

        var deepModuleIndex = deterministicDungeon.Modules
            .Select((module, index) => new { module, index })
            .First(entry => entry.module.DepthBand == DungeonDepthBand.DeepDomains && entry.module.PieceFamilyId != "DF-20")
            .index;

        var profileExpectations = new[]
        {
            (OccupationProfile.WispRoaming, (CreatureChassis?)CreatureChassis.AnnoyanceWisp),
            (OccupationProfile.CrawlerColony, (CreatureChassis?)CreatureChassis.GeodeCrawler),
            (OccupationProfile.ShardlingFeeding, (CreatureChassis?)CreatureChassis.Shardling),
            (OccupationProfile.RevenantHold, (CreatureChassis?)CreatureChassis.CrystalRevenant),
            (OccupationProfile.SentryDefense, (CreatureChassis?)CreatureChassis.FacetSentry),
            (OccupationProfile.GuardianObjective, (CreatureChassis?)CreatureChassis.StoneGuardian),
            (OccupationProfile.GolemLair, (CreatureChassis?)CreatureChassis.CrystalGolem),
            (OccupationProfile.Mixed, (CreatureChassis?)null),
            (OccupationProfile.Empty, (CreatureChassis?)null),
        };

        foreach (var expectation in profileExpectations)
        {
            var modules = deterministicDungeon.Modules.ToArray();
            modules[deepModuleIndex] = modules[deepModuleIndex] with { Occupation = expectation.Item1 };
            var adjustedDungeon = deterministicDungeon with { Modules = modules };
            var adjustedEncounters = DeepFractureEncounterPlanner.Build(adjustedDungeon);
            var district = adjustedEncounters.Districts.Single(entry => entry.ModuleInstanceId == modules[deepModuleIndex].InstanceId);

            if (expectation.Item1 == OccupationProfile.Empty)
            {
                Assert(district.Encounters.Count == 0, "Explicit Empty occupation must suppress encounters.");
            }
            else if (expectation.Item1 == OccupationProfile.Mixed)
            {
                Assert(district.Encounters.Select(encounter => encounter.Definition.Chassis).Distinct().Count() >= 2, "Mixed occupation must produce at least two distinct chassis.");
            }
            else
            {
                Assert(district.Encounters.Any(encounter => encounter.Definition.Chassis == expectation.Item2), $"Occupation {expectation.Item1} must include its required primary chassis {expectation.Item2}.");
            }
        }

        var moduleLocalA = DeepFractureEncounterPlanner.Build(deterministicDungeon);
        var changedModules = deterministicDungeon.Modules.ToArray();
        changedModules[deepModuleIndex] = changedModules[deepModuleIndex] with
        {
            Occupation = changedModules[deepModuleIndex].Occupation == OccupationProfile.WispRoaming
                ? OccupationProfile.CrawlerColony
                : OccupationProfile.WispRoaming,
        };
        var moduleLocalB = DeepFractureEncounterPlanner.Build(deterministicDungeon with { Modules = changedModules });
        var changedModuleId = changedModules[deepModuleIndex].InstanceId;
        var unaffectedIds = deterministicDungeon.Modules.Where(module => module.InstanceId != changedModuleId).Select(module => module.InstanceId).ToArray();
        Assert(unaffectedIds.All(moduleId => DistrictsEquivalent(
            moduleLocalA.Districts.Single(district => district.ModuleInstanceId == moduleId),
            moduleLocalB.Districts.Single(district => district.ModuleInstanceId == moduleId))),
            "Changing one district occupation must not reshuffle other districts' deterministic encounter streams.");

        var alignedDistrict = deterministicA.Districts.First(district => district.Encounters.Count > 0 && deterministicDungeon.Modules.Single(module => module.InstanceId == district.ModuleInstanceId).ElementalStates.Count > 0);
        var alignedModule = deterministicDungeon.Modules.Single(module => module.InstanceId == alignedDistrict.ModuleInstanceId);
        var originalEncounter = alignedDistrict.Encounters[0];
        var foreignAlignment = Enum.GetValues(typeof(ElementalAlignment)).Cast<ElementalAlignment>().First(element => !alignedModule.ElementalStates.Contains(element));
        var corruptedEncounter = originalEncounter with
        {
            Definition = originalEncounter.Definition with { Alignment = foreignAlignment },
        };
        var corruptedDistricts = deterministicA.Districts.Select(district =>
            district.ModuleInstanceId == alignedDistrict.ModuleInstanceId
                ? district with { Encounters = district.Encounters.Select(encounter => encounter.Id == originalEncounter.Id ? corruptedEncounter : encounter).ToArray() }
                : district).ToArray();
        var corruptedPlan = deterministicA with { Districts = corruptedDistricts };
        var corruptedValidation = DeepFractureEncounterValidator.Validate(deterministicDungeon, corruptedPlan);
        Assert(!corruptedValidation.IsValid && corruptedValidation.Errors.Any(error => error.Contains("alignment", StringComparison.OrdinalIgnoreCase)), "Encounter alignment outside district authority must fail validation.");

        return assertions;
    }

    private static bool EncounterPlansEquivalent(DeepFractureEncounterPlan left, DeepFractureEncounterPlan right)
    {
        if (left.DungeonSeed != right.DungeonSeed || left.Districts.Count != right.Districts.Count)
            return false;

        for (var index = 0; index < left.Districts.Count; index++)
        {
            if (!DistrictsEquivalent(left.Districts[index], right.Districts[index]))
                return false;
        }

        return true;
    }

    private static bool DistrictsEquivalent(DistrictEncounterPlan left, DistrictEncounterPlan right)
    {
        if (!string.Equals(left.ModuleInstanceId, right.ModuleInstanceId, StringComparison.Ordinal)
            || !string.Equals(left.PieceFamilyId, right.PieceFamilyId, StringComparison.Ordinal)
            || left.Occupation != right.Occupation
            || left.Encounters.Count != right.Encounters.Count)
            return false;

        for (var index = 0; index < left.Encounters.Count; index++)
        {
            var a = left.Encounters[index];
            var b = right.Encounters[index];
            if (!string.Equals(a.Id, b.Id, StringComparison.Ordinal)
                || !string.Equals(a.TerritoryModuleId, b.TerritoryModuleId, StringComparison.Ordinal)
                || a.SpawnCount != b.SpawnCount
                || a.Definition.Chassis != b.Definition.Chassis
                || a.Definition.Alignment != b.Definition.Alignment
                || a.Definition.Role != b.Definition.Role
                || !string.Equals(a.Definition.TerritoryPieceId, b.Definition.TerritoryPieceId, StringComparison.Ordinal)
                || !a.Definition.Components.SequenceEqual(b.Definition.Components))
                return false;
        }

        return true;
    }
}
