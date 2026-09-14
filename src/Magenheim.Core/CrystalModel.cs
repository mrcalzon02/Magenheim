using System;

namespace Magenheim.Core;

public enum CrystalTier
{
    Rough = 0,
    Simple = 1,
    Crystal = 2,
    Advanced = 3,
    Master = 4
}

public enum ElementalAlignment
{
    Earth,
    Fire,
    Frost,
    Lightning,
    Poison,
    Nature,
    Water,
    Air,
    Arcane
}

public readonly record struct Crystal(ElementalAlignment Element, CrystalTier Tier);

public sealed record RefinementRule(
    CrystalTier SourceTier,
    CrystalTier DestinationTier,
    double SuccessChance,
    int MinimumSkillLevel,
    string RequiredStation,
    bool DestroyOnFailure = false)
{
    public void Validate()
    {
        if (DestinationTier != SourceTier + 1)
            throw new InvalidOperationException($"Refinement must advance exactly one tier: {SourceTier} -> {DestinationTier}.");

        if (SuccessChance < 0d || SuccessChance > 1d)
            throw new InvalidOperationException("SuccessChance must be between 0 and 1 inclusive.");

        if (MinimumSkillLevel < 0)
            throw new InvalidOperationException("MinimumSkillLevel cannot be negative.");

        if (string.IsNullOrWhiteSpace(RequiredStation))
            throw new InvalidOperationException("RequiredStation is required.");
    }
}

public sealed record RefinementRequest(
    Crystal Input,
    int CrystalShapingSkillLevel,
    string StationId,
    double Roll);

public enum RefinementOutcome
{
    Success,
    FailedPreserved,
    FailedDestroyed,
    InvalidSkill,
    InvalidStation,
    InvalidRoll
}

public sealed record RefinementResult(
    RefinementOutcome Outcome,
    Crystal? Output,
    bool AwardExperience,
    string Reason);
