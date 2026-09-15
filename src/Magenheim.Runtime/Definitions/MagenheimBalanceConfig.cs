using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using Magenheim.Core.Definitions;
using Magenheim.Core.Socketing;
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
                weights)
            {
                Placement = ReadGeodePlacement(config, geode),
            });
        }

        var socketEffects = ReadSocketEffects(config, baseline.SocketEffects);
        var compatibility = ReadWorldgenCompatibility(config, baseline.WorldgenCompatibility);

        return MagenheimDefinitionOverrideApplier.Apply(
            baseline,
            refinementOverrides,
            geodeOverrides,
            compatibility,
            socketEffects);
    }

    private static SocketEffectDefinitionSet ReadSocketEffects(
        ConfigFile config,
        SocketEffectDefinitionSet baseline)
    {
        var rules = baseline.Rules
            .Select(rule =>
            {
                var section = $"Balance.SocketEffect.{rule.Element}.{rule.Category}";
                var magnitude = config.Bind(
                    section,
                    rule.Effect.ToString(),
                    rule.SimpleMagnitude,
                    "Server-authoritative Simple-tier socket magnitude for this effect channel. Crystal, Advanced, and Master scale from this value through the core tier curve.");
                return rule with { SimpleMagnitude = magnitude.Value };
            })
            .ToArray();

        return new SocketEffectDefinitionSet(rules);
    }

    private static GeodePlacementDefinition ReadGeodePlacement(ConfigFile config, GeodeDefinition geode)
    {
        var baseline = geode.Placement;
        var section = $"Worldgen.Placement.{geode.Id}";
        var configuredMin = config.Bind(section, "MinPerZone", baseline.MinPerZone,
            "Minimum placements per zone. Must be non-negative and <= MaxPerZone.").Value;
        var configuredMax = config.Bind(section, "MaxPerZone", baseline.MaxPerZone,
            "Maximum placements per zone; values between 0 and 1 behave as placement chance in Valheim/Jotunn.").Value;
        var guaranteeSurfaceSpawn = config.Bind(section, "GuaranteeSurfaceSpawn", true,
            "When true, Magenheim raises this geode to at least one placement attempt per eligible newly-generated zone. Disable to restore purely chance-based sparse placement.").Value;

        // Earlier Magenheim defaults used MinPerZone=0 and MaxPerZone<1, which made every biome
        // geode a low-probability vegetation roll. Existing BepInEx configs retain those old values
        // even after mod updates, so merely changing foundation.json would not repair live installs.
        // This new opt-out key migrates existing installs non-destructively: newly generated eligible
        // zones receive at least one geode placement attempt without touching already-generated zones
        // or any vanilla/foreign vegetation registration.
        var effectiveMin = guaranteeSurfaceSpawn ? Math.Max(1.0, configuredMin) : configuredMin;
        var effectiveMax = guaranteeSurfaceSpawn ? Math.Max(effectiveMin, configuredMax) : configuredMax;

        var configuredGroupMin = config.Bind(section, "GroupSizeMin", baseline.GroupSizeMin,
            "Minimum objects per placement group; must be at least 1.").Value;
        var configuredGroupMax = config.Bind(section, "GroupSizeMax", baseline.GroupSizeMax,
            "Maximum objects per placement group; must be >= GroupSizeMin.").Value;
        var configuredGroupRadius = config.Bind(section, "GroupRadius", baseline.GroupRadius,
            "Radius of a placement group; must be non-negative.").Value;
        var migrateLegacyClusterDefaults = config.Bind(section, "MigrateLegacyClusterDefaults", true,
            "When true, only the former Magenheim 1/1/0 geode cluster defaults are upgraded to the current foundation cluster. Custom cluster values remain untouched.").Value;

        // BepInEx preserves old values forever. The pre-cluster Magenheim release wrote exactly
        // 1/1/0 for every geode, so recognize only that complete legacy tuple. This avoids treating
        // a user's deliberate custom values as stale defaults while allowing existing installs to
        // receive the current foundation grouping automatically.
        var legacyClusterTuple = configuredGroupMin == 1
            && configuredGroupMax == 1
            && Math.Abs(configuredGroupRadius) < 0.000001d;
        var effectiveGroupMin = migrateLegacyClusterDefaults && legacyClusterTuple
            ? baseline.GroupSizeMin
            : configuredGroupMin;
        var effectiveGroupMax = migrateLegacyClusterDefaults && legacyClusterTuple
            ? baseline.GroupSizeMax
            : configuredGroupMax;
        var effectiveGroupRadius = migrateLegacyClusterDefaults && legacyClusterTuple
            ? baseline.GroupRadius
            : configuredGroupRadius;

        return new GeodePlacementDefinition(
            config.Bind(section, "BlockCheck", baseline.BlockCheck,
                "Reject placement when normal solid/piece layers already occupy the location.").Value,
            config.Bind(section, "ForcePlacement", baseline.ForcePlacement,
                "Use Valheim force-placement behavior. False is the conservative default.").Value,
            effectiveMin,
            effectiveMax,
            config.Bind(section, "MinAltitude", baseline.MinAltitude,
                "Minimum world altitude for this geode.").Value,
            config.Bind(section, "MaxAltitude", baseline.MaxAltitude,
                "Maximum world altitude for this geode.").Value,
            config.Bind(section, "MinOceanDepth", baseline.MinOceanDepth,
                "Minimum ocean depth accepted for placement.").Value,
            config.Bind(section, "MaxOceanDepth", baseline.MaxOceanDepth,
                "Maximum ocean depth accepted for placement.").Value,
            config.Bind(section, "MinTerrainDelta", baseline.MinTerrainDelta,
                "Minimum terrain height delta around the placement point.").Value,
            config.Bind(section, "MaxTerrainDelta", baseline.MaxTerrainDelta,
                "Maximum terrain height delta around the placement point.").Value,
            config.Bind(section, "TerrainDeltaRadius", baseline.TerrainDeltaRadius,
                "Radius used to measure terrain delta.").Value,
            config.Bind(section, "MinTilt", baseline.MinTilt,
                "Minimum terrain tilt in degrees.").Value,
            config.Bind(section, "MaxTilt", baseline.MaxTilt,
                "Maximum terrain tilt in degrees, validated within 0..90.").Value,
            config.Bind(section, "InForest", baseline.InForest,
                "When true, apply Valheim forest-fractal threshold checks.").Value,
            config.Bind(section, "ForestThresholdMin", baseline.ForestThresholdMin,
                "Minimum forest-fractal threshold when InForest is enabled.").Value,
            config.Bind(section, "ForestThresholdMax", baseline.ForestThresholdMax,
                "Maximum forest-fractal threshold when InForest is enabled.").Value,
            config.Bind(section, "ScaleMin", baseline.ScaleMin,
                "Minimum placed world-object scale; must remain positive.").Value,
            config.Bind(section, "ScaleMax", baseline.ScaleMax,
                "Maximum placed world-object scale; must be >= ScaleMin.").Value,
            effectiveGroupMin,
            effectiveGroupMax,
            effectiveGroupRadius,
            config.Bind(section, "GroundOffset", baseline.GroundOffset,
                "Vertical placement offset; negative values bury the object slightly.").Value);
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
        if (value is null || string.IsNullOrWhiteSpace(value))
            return Array.Empty<string>();

        return value
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Trim())
            .Where(item => item.Length > 0)
            .ToArray();
    }
}
