using System;
using System.Collections.Generic;

namespace Magenheim.Core;

public sealed class CrystalRefinementService
{
    public const double DefaultMaximumFailureReduction = 0.75d;
    public const double MinimumMaximumFailureReduction = 0.50d;
    public const double MaximumMaximumFailureReduction = 1.00d;

    private readonly IReadOnlyDictionary<CrystalTier, RefinementRule> _rules;

    public CrystalRefinementService(IEnumerable<RefinementRule> rules)
    {
        if (rules is null) throw new ArgumentNullException(nameof(rules));

        var map = new Dictionary<CrystalTier, RefinementRule>();
        foreach (var rule in rules)
        {
            if (rule is null) throw new InvalidOperationException("Refinement rules cannot contain null entries.");
            rule.Validate();

            if (!map.TryAdd(rule.SourceTier, rule))
                throw new InvalidOperationException($"Duplicate refinement rule for {rule.SourceTier}.");
        }

        _rules = map;
    }

    public RefinementResult Refine(RefinementRequest request)
    {
        if (request.Roll < 0d || request.Roll >= 1d || double.IsNaN(request.Roll) || double.IsInfinity(request.Roll))
            return Invalid(RefinementOutcome.InvalidRoll, request.Input,
                "Roll must be finite and in the range [0,1).");

        if (request.CrystalShapingSkillLevel < 0 || request.CrystalShapingSkillLevel > 100)
            return Invalid(RefinementOutcome.InvalidSkill, request.Input,
                "Crystal Shaping skill must be between 0 and 100 inclusive.");

        if (!IsValidMaximumFailureReduction(request.MaximumFailureReduction))
            return Invalid(RefinementOutcome.InvalidConfiguration, request.Input,
                $"MaximumFailureReduction must be between {MinimumMaximumFailureReduction:0.00} and {MaximumMaximumFailureReduction:0.00} inclusive.");

        if (!_rules.TryGetValue(request.Input.Tier, out var rule))
            return Invalid(RefinementOutcome.NoRule, request.Input,
                $"No refinement rule exists for {request.Input.Tier}.");

        if (request.CrystalShapingSkillLevel < rule.MinimumSkillLevel)
            return Invalid(RefinementOutcome.InvalidSkill, request.Input,
                $"Crystal Shaping {rule.MinimumSkillLevel} is required.");

        if (!string.Equals(request.StationId, rule.RequiredStation, StringComparison.Ordinal))
            return Invalid(RefinementOutcome.InvalidStation, request.Input,
                $"Station '{rule.RequiredStation}' is required.");

        var effectiveFailureChance = CalculateEffectiveFailureChance(
            rule.BaseFailureChance,
            request.CrystalShapingSkillLevel,
            request.MaximumFailureReduction);

        if (request.Roll < effectiveFailureChance)
        {
            return new RefinementResult(
                RefinementOutcome.FailedDestroyed,
                null,
                true,
                rule.FailureShardCount,
                effectiveFailureChance,
                $"Refinement failed; the {request.Input.Tier} crystal was destroyed and returned {rule.FailureShardCount} matching shard(s).");
        }

        var output = new Crystal(request.Input.Element, rule.DestinationTier);
        return new RefinementResult(
            RefinementOutcome.Success,
            output,
            true,
            0,
            effectiveFailureChance,
            $"Refined {request.Input.Tier} to {rule.DestinationTier}.");
    }

    public static double CalculateEffectiveFailureChance(
        double baseFailureChance,
        int crystalShapingSkillLevel,
        double maximumFailureReduction = DefaultMaximumFailureReduction)
    {
        if (double.IsNaN(baseFailureChance) || double.IsInfinity(baseFailureChance) || baseFailureChance < 0d || baseFailureChance > 1d)
            throw new ArgumentOutOfRangeException(nameof(baseFailureChance), "Base failure chance must be finite and between 0 and 1 inclusive.");

        if (crystalShapingSkillLevel < 0 || crystalShapingSkillLevel > 100)
            throw new ArgumentOutOfRangeException(nameof(crystalShapingSkillLevel), "Crystal Shaping skill must be between 0 and 100 inclusive.");

        if (!IsValidMaximumFailureReduction(maximumFailureReduction))
            throw new ArgumentOutOfRangeException(nameof(maximumFailureReduction),
                $"Maximum failure reduction must be between {MinimumMaximumFailureReduction:0.00} and {MaximumMaximumFailureReduction:0.00} inclusive.");

        var skillReduction = (crystalShapingSkillLevel / 100d) * maximumFailureReduction;
        return baseFailureChance * (1d - skillReduction);
    }

    public static IReadOnlyList<RefinementRule> CreateCanonicalDefaults() =>
        new[]
        {
            new RefinementRule(CrystalTier.Rough, CrystalTier.Simple, 0.10d, 0, "Magenheim_GeologistWorkstation", 1),
            new RefinementRule(CrystalTier.Simple, CrystalTier.Crystal, 0.20d, 0, "Magenheim_StationUpgrade_FracturingBlock", 2),
            new RefinementRule(CrystalTier.Crystal, CrystalTier.Advanced, 0.30d, 0, "Magenheim_StationUpgrade_FacetingWheel", 3),
            new RefinementRule(CrystalTier.Advanced, CrystalTier.Master, 0.40d, 0, "Magenheim_StationUpgrade_ResonanceFrame", 5)
        };

    private static bool IsValidMaximumFailureReduction(double value) =>
        !double.IsNaN(value) &&
        !double.IsInfinity(value) &&
        value >= MinimumMaximumFailureReduction &&
        value <= MaximumMaximumFailureReduction;

    private static RefinementResult Invalid(RefinementOutcome outcome, Crystal input, string reason) =>
        new(outcome, input, false, 0, 0d, reason);
}
