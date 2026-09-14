using System;
using System.Collections.Generic;
using System.Linq;
using Magenheim.Core.Socketing;
using Magenheim.Core.Worldgen;

namespace Magenheim.Core.Definitions;

public sealed record RefinementBalanceOverride(
    CrystalTier SourceTier,
    double BaseFailureChance,
    int MinimumSkillLevel,
    int FailureShardCount);

public sealed record GeodeBalanceOverride(
    string GeodeId,
    double SecondCrystalChance,
    double ThirdCrystalChance,
    IReadOnlyList<ElementWeight> ElementWeights)
{
    public GeodePlacementDefinition? Placement { get; init; }
}

public static class MagenheimDefinitionOverrideApplier
{
    public static MagenheimDefinitionSet Apply(
        MagenheimDefinitionSet baseline,
        IEnumerable<RefinementBalanceOverride> refinementOverrides,
        IEnumerable<GeodeBalanceOverride> geodeOverrides,
        WorldgenCompatibilityPolicy? worldgenCompatibilityOverride = null,
        SocketEffectDefinitionSet? socketEffectsOverride = null)
    {
        if (baseline is null)
            throw new ArgumentNullException(nameof(baseline));
        if (refinementOverrides is null)
            throw new ArgumentNullException(nameof(refinementOverrides));
        if (geodeOverrides is null)
            throw new ArgumentNullException(nameof(geodeOverrides));

        var refinementBySource = refinementOverrides.ToDictionary(item => item.SourceTier);
        var geodeById = geodeOverrides.ToDictionary(item => item.GeodeId, StringComparer.Ordinal);

        RejectUnknownRefinementOverrides(baseline, refinementBySource);
        RejectUnknownGeodeOverrides(baseline, geodeById);

        var refinementRules = baseline.RefinementRules
            .Select(rule => refinementBySource.TryGetValue(rule.SourceTier, out var balance)
                ? rule with
                {
                    BaseFailureChance = balance.BaseFailureChance,
                    MinimumSkillLevel = balance.MinimumSkillLevel,
                    FailureShardCount = balance.FailureShardCount,
                }
                : rule)
            .ToArray();

        var geodes = baseline.Geodes
            .Select(geode => geodeById.TryGetValue(geode.Id, out var balance)
                ? ApplyGeodeBalance(geode, balance)
                : geode)
            .ToArray();

        return MagenheimDefinitionValidator.ValidateAndFreeze(
            baseline.SchemaVersion,
            refinementRules,
            geodes,
            worldgenCompatibilityOverride ?? baseline.WorldgenCompatibility,
            socketEffectsOverride ?? baseline.SocketEffects);
    }

    private static GeodeDefinition ApplyGeodeBalance(
        GeodeDefinition baseline,
        GeodeBalanceOverride balance)
    {
        if (balance.ElementWeights is null)
            throw new InvalidOperationException($"Geode override '{baseline.Id}' must include elemental weights.");

        var weights = balance.ElementWeights.ToArray();
        var baselineElements = baseline.ElementWeights
            .Select(weight => weight.Element)
            .OrderBy(element => element)
            .ToArray();
        var overrideElements = weights
            .Select(weight => weight.Element)
            .OrderBy(element => element)
            .ToArray();

        if (!baselineElements.SequenceEqual(overrideElements))
            throw new InvalidOperationException(
                $"Geode override '{baseline.Id}' cannot add, remove, or replace elemental identities.");

        return baseline with
        {
            SecondCrystalChance = balance.SecondCrystalChance,
            ThirdCrystalChance = balance.ThirdCrystalChance,
            ElementWeights = Array.AsReadOnly(weights),
            Placement = balance.Placement ?? baseline.Placement,
        };
    }

    private static void RejectUnknownRefinementOverrides(
        MagenheimDefinitionSet baseline,
        IReadOnlyDictionary<CrystalTier, RefinementBalanceOverride> overrides)
    {
        var sources = new HashSet<CrystalTier>(baseline.RefinementRules.Select(rule => rule.SourceTier));
        foreach (var source in overrides.Keys)
        {
            if (!sources.Contains(source))
                throw new InvalidOperationException($"No baseline refinement rule exists for override source tier {source}.");
        }
    }

    private static void RejectUnknownGeodeOverrides(
        MagenheimDefinitionSet baseline,
        IReadOnlyDictionary<string, GeodeBalanceOverride> overrides)
    {
        var ids = new HashSet<string>(baseline.Geodes.Select(geode => geode.Id), StringComparer.Ordinal);
        foreach (var id in overrides.Keys)
        {
            if (!ids.Contains(id))
                throw new InvalidOperationException($"No baseline geode exists for override id '{id}'.");
        }
    }
}
