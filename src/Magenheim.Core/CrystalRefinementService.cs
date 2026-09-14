using System;
using System.Collections.Generic;

namespace Magenheim.Core;

public sealed class CrystalRefinementService
{
    private readonly IReadOnlyDictionary<CrystalTier, RefinementRule> _rules;

    public CrystalRefinementService(IEnumerable<RefinementRule> rules)
    {
        if (rules is null) throw new ArgumentNullException(nameof(rules));

        var map = new Dictionary<CrystalTier, RefinementRule>();
        foreach (var rule in rules)
        {
            if (rule is null) throw new InvalidOperationException("Refinement rules cannot contain null entries.");
            rule.Validate();

            if (rule.SourceTier == CrystalTier.Master)
                throw new InvalidOperationException("Master crystals cannot have a refinement rule.");

            if (!map.TryAdd(rule.SourceTier, rule))
                throw new InvalidOperationException($"Duplicate refinement rule for {rule.SourceTier}.");
        }

        _rules = map;
    }

    public RefinementResult Refine(RefinementRequest request)
    {
        if (request.Roll < 0d || request.Roll >= 1d || double.IsNaN(request.Roll))
            return new RefinementResult(RefinementOutcome.InvalidRoll, request.Input, false,
                "Roll must be in the range [0,1).");

        if (!_rules.TryGetValue(request.Input.Tier, out var rule))
            return new RefinementResult(RefinementOutcome.NoRule, request.Input, false,
                $"No refinement rule exists for {request.Input.Tier}.");

        if (request.CrystalShapingSkillLevel < rule.MinimumSkillLevel)
            return new RefinementResult(RefinementOutcome.InvalidSkill, request.Input, false,
                $"Crystal Shaping {rule.MinimumSkillLevel} is required.");

        if (!string.Equals(request.StationId, rule.RequiredStation, StringComparison.Ordinal))
            return new RefinementResult(RefinementOutcome.InvalidStation, request.Input, false,
                $"Station '{rule.RequiredStation}' is required.");

        if (request.Roll < rule.SuccessChance)
        {
            var output = new Crystal(request.Input.Element, rule.DestinationTier);
            return new RefinementResult(RefinementOutcome.Success, output, true,
                $"Refined {request.Input.Tier} to {rule.DestinationTier}.");
        }

        if (rule.DestroyOnFailure)
            return new RefinementResult(RefinementOutcome.FailedDestroyed, null, true,
                "Refinement failed and the crystal was destroyed.");

        return new RefinementResult(RefinementOutcome.FailedPreserved, request.Input, true,
            "Refinement failed; the crystal was preserved.");
    }

    public static IReadOnlyList<RefinementRule> CreateCanonicalDefaults(string stationId = "magenheim.geologists_workstation") =>
        new[]
        {
            new RefinementRule(CrystalTier.Rough, CrystalTier.Simple, 0.90d, 0, stationId),
            new RefinementRule(CrystalTier.Simple, CrystalTier.Crystal, 0.80d, 10, stationId),
            new RefinementRule(CrystalTier.Crystal, CrystalTier.Advanced, 0.70d, 25, stationId),
            new RefinementRule(CrystalTier.Advanced, CrystalTier.Master, 0.60d, 50, stationId)
        };
}
