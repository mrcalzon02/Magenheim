using System;

namespace Magenheim.Core;

public enum CrystalTier
{
    Rough = 0,
    Simple = 1,
    Refined = 2,
    Advanced = 3,
    Master = 4
}

public enum ElementalAlignment
{
    Earth,
    Fire,
    Frost,
    Storm,
    Venom,
    Radiance,
    Seidr,
    Spirit
}

public readonly record struct Crystal(ElementalAlignment Element, CrystalTier Tier);

public sealed record RefinementRule(
    CrystalTier SourceTier,
    CrystalTier DestinationTier,
    double BaseFailureChance,
    int MinimumSkillLevel,
    string RequiredStation,
    int FailureShardCount)
{
    public void Validate()
    {
        if (SourceTier == CrystalTier.Master)
            throw new InvalidOperationException("Master crystals cannot have a refinement rule.");

        if ((int)DestinationTier != (int)SourceTier + 1)
            throw new InvalidOperationException($"Refinement must advance exactly one tier: {SourceTier} -> {DestinationTier}.");

        if (double.IsNaN(BaseFailureChance) || double.IsInfinity(BaseFailureChance) || BaseFailureChance < 0d || BaseFailureChance > 1d)
            throw new InvalidOperationException("BaseFailureChance must be finite and between 0 and 1 inclusive.");

        if (MinimumSkillLevel < 0 || MinimumSkillLevel > 100)
            throw new InvalidOperationException("MinimumSkillLevel must be between 0 and 100 inclusive.");

        if (string.IsNullOrWhiteSpace(RequiredStation))
            throw new InvalidOperationException("RequiredStation is required.");

        if (FailureShardCount < 0)
            throw new InvalidOperationException("FailureShardCount cannot be negative.");
    }
}

public sealed record RefinementRequest(
    Crystal Input,
    int CrystalShapingSkillLevel,
    string StationId,
    double Roll,
    double MaximumFailureReduction = 0.75d);

public enum RefinementOutcome
{
    Success,
    FailedDestroyed,
    NoRule,
    InvalidSkill,
    InvalidStation,
    InvalidRoll,
    InvalidConfiguration
}

public sealed record RefinementResult(
    RefinementOutcome Outcome,
    Crystal? Output,
    bool AwardExperience,
    int ShardReturnCount,
    double EffectiveFailureChance,
    string Reason);
