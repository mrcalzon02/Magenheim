using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using Magenheim.Core.Definitions;

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

        return MagenheimDefinitionOverrideApplier.Apply(
            baseline,
            refinementOverrides,
            geodeOverrides);
    }
}
