using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using Magenheim.Core.Definitions;
using Magenheim.Core.Worldgen;

namespace Magenheim.Runtime.Definitions;

internal static class MagenheimBalanceConfig
{
    internal static MagenheimDefinitionSet Apply(ConfigFile config, MagenheimDefinitionSet baseline)
    {
        var refinementOverrides = new List<RefinementBalanceOverride>();
        foreach (var rule in baseline.RefinementRules)
        {
            var section = $"Balance.Refinement.{rule.SourceTier}";
            var failure = config.Bind(section, "BaseFailureChance", rule.BaseFailureChance,
                "Server-authoritative base failure chance before Crystal Shaping reduction. Range 0..1.");
            var skill = config.Bind(section, "MinimumSkillLevel", rule.MinimumSkillLevel,
                "Minimum Crystal Shaping skill required for this refinement step.");
            var shards = config.Bind(section, "FailureShardCount", rule.FailureShardCount,
                "Matching shards returned when this valid refinement attempt fails.");

            refinementOverrides.Add(new RefinementBalanceOverride(
                rule.SourceTier,
                failure.Value,
                skill.Value,
                shards.Value));
        }

        var geodeOverrides = new List<GeodeBalanceOverride>();
        foreach (var geode in baseline.Geodes)
        {
            var section = $"Balance.Geode.{geode.Id}";
            var second = config.Bind(section, "SecondCrystalChance", geode.SecondCrystalChance,
                "Independent probability of a second crystal when this geode is opened. Range 0..1.");
            var third = config.Bind(section, "ThirdCrystalChance", geode.ThirdCrystalChance,
                "Independent probability of a third crystal when this geode is opened. Range 0..1.");

            var weights = geode.ElementWeights
                .Select(weight => new ElementWeight(
                    weight.Element,
                    config.Bind(section, $"Weight.{weight.Element}", weight.Weight,
                        "Relative elemental weight. Must remain positive and finite.").Value))
                .ToArray();

            geodeOverrides.Add(new GeodeBalanceOverride(
                geode.Id,
                second.Value,
                third.Value,
                weights));
        }

        var compatibility = ReadWorldgenCompatibility(config, baseline.WorldgenCompatibility);

        return MagenheimDefinitionOverrideApplier.Apply(
            baseline,
            refinementOverrides,
            geodeOverrides,
            compatibility);
    }

    private static WorldgenCompatibilityPolicy ReadWorldgenCompatibility(
        ConfigFile config,
        WorldgenCompatibilityPolicy baseline)
    {
        const string section = "Worldgen.Compatibility";

        var invalidArea = config.Bind(section, "InvalidAreaBehavior", baseline.InvalidAreaBehavior,
            "How invalid configured spawn areas are handled before registration: Reject, ClampKnownBits, or FallbackToAll.");
        var duplicate = config.Bind(section, "DuplicateRegistrationBehavior", baseline.DuplicateRegistrationBehavior,
            "How occupied Magenheim registration identities are handled: Skip or Error. Existing host content is never modified.");
        var detectPrefabs = config.Bind(section, "DetectPrefabCollisions", baseline.DetectPrefabCollisions,
            "When true, prefab names are collision-checked in addition to registration keys.");
        var identity = config.Bind(section, "IdentityComparison", baseline.IdentityComparison,
            "Identity comparison mode for collision detection: Exact or CaseInsensitive.");
        var excludedKeys = config.Bind(section, "ExcludedRegistrationKeys",
            string.Join(",", baseline.ExcludedRegistrationKeys),
            "Comma-separated Magenheim registration keys to suppress. Values must begin with 'magenheim.'.");
        var excludedPrefabs = config.Bind(section, "ExcludedPrefabNames",
            string.Join(",", baseline.ExcludedPrefabNames),
            "Comma-separated Magenheim prefab names to suppress. Values must begin with 'Magenheim_'.");

        return baseline with
        {
            InvalidAreaBehavior = invalidArea.Value,
            DuplicateRegistrationBehavior = duplicate.Value,
            AdditiveOnly = true,
            DetectPrefabCollisions = detectPrefabs.Value,
            IdentityComparison = identity.Value,
            ExcludedRegistrationKeys = ParseCsv(excludedKeys.Value),
            ExcludedPrefabNames = ParseCsv(excludedPrefabs.Value),
        };
    }

    private static string[] ParseCsv(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Array.Empty<string>();

        return value
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Trim())
            .Where(item => item.Length > 0)
            .ToArray();
    }
}
