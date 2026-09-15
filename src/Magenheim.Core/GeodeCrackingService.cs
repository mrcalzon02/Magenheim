using System;
using System.Collections.Generic;
using Magenheim.Core.Definitions;

namespace Magenheim.Core;

public sealed record GeodeCrackingRequest(
    GeodeDefinition Geode,
    double SecondCrystalRoll,
    double ThirdCrystalRoll,
    IReadOnlyList<double> ElementRolls,
    int CrystalShapingSkillLevel = 0);

public enum GeodeCrackingOutcome
{
    Success,
    InvalidRequest,
    InvalidDefinition,
}

public sealed record GeodeCrackingResult(
    GeodeCrackingOutcome Outcome,
    IReadOnlyList<Crystal> Crystals,
    string Reason)
{
    public bool IsSuccess => Outcome == GeodeCrackingOutcome.Success;
}

/// <summary>
/// Pure deterministic geode outcome authority. Randomness and the player's authoritative Crystal
/// Shaping skill snapshot are supplied by the caller so multiplayer/runtime code can keep RNG and
/// progression ownership server-side and replay/verify the same decision.
/// </summary>
public static class GeodeCrackingService
{
    public const int CurrentSchemaElementRollCount = 3;
    public const double MaximumSecondCrystalSkillBonus = 0.50d;
    public const double MaximumThirdCrystalSkillBonus = 0.40d;

    public static GeodeCrackingResult Crack(GeodeCrackingRequest request)
    {
        if (request is null)
            return InvalidRequest("Request is required.");
        if (request.Geode is null)
            return InvalidRequest("Geode definition is required.");
        if (request.ElementRolls is null)
            return InvalidRequest("Element rolls are required.");
        if (request.CrystalShapingSkillLevel < 0 || request.CrystalShapingSkillLevel > 100)
            return InvalidRequest("Crystal Shaping skill must be between 0 and 100 inclusive.");

        var geode = request.Geode;
        if (geode.GuaranteedCrystalCount != 1)
            return InvalidDefinition("The current geode schema requires exactly one guaranteed crystal.");
        if (!IsChance(geode.SecondCrystalChance) || !IsChance(geode.ThirdCrystalChance))
            return InvalidDefinition("Bonus crystal chances must be finite values between 0 and 1 inclusive.");
        if (geode.ElementWeights is null || geode.ElementWeights.Count == 0)
            return InvalidDefinition("At least one elemental weight is required.");

        if (!IsRoll(request.SecondCrystalRoll) || !IsRoll(request.ThirdCrystalRoll))
            return InvalidRequest("Bonus rolls must be finite values in the range [0,1).");
        if (request.ElementRolls.Count != CurrentSchemaElementRollCount)
            return InvalidRequest($"Exactly {CurrentSchemaElementRollCount} element rolls are required for deterministic cracking of '{geode.Id}'.");

        for (var i = 0; i < request.ElementRolls.Count; i++)
        {
            if (!IsRoll(request.ElementRolls[i]))
                return InvalidRequest($"Element roll {i} must be a finite value in the range [0,1).");
        }

        var totalWeight = 0d;
        for (var i = 0; i < geode.ElementWeights.Count; i++)
        {
            var weight = geode.ElementWeights[i];
            if (weight is null || double.IsNaN(weight.Weight) || double.IsInfinity(weight.Weight) || weight.Weight <= 0d)
                return InvalidDefinition($"Element weight {i} must be non-null, finite, and greater than zero.");
            totalWeight += weight.Weight;
        }

        if (double.IsInfinity(totalWeight) || totalWeight <= 0d)
            return InvalidDefinition("Element weight total must be finite and greater than zero.");

        var secondChance = CalculateSkillAdjustedBonusChance(
            geode.SecondCrystalChance,
            request.CrystalShapingSkillLevel,
            MaximumSecondCrystalSkillBonus);
        var thirdChance = CalculateSkillAdjustedBonusChance(
            geode.ThirdCrystalChance,
            request.CrystalShapingSkillLevel,
            MaximumThirdCrystalSkillBonus);

        var crystalCount = 1;
        if (request.SecondCrystalRoll < secondChance)
            crystalCount++;
        if (request.ThirdCrystalRoll < thirdChance)
            crystalCount++;

        var crystals = new Crystal[crystalCount];
        for (var i = 0; i < crystalCount; i++)
        {
            var element = SelectElement(geode.ElementWeights, totalWeight, request.ElementRolls[i]);
            crystals[i] = new Crystal(element, CrystalTier.Rough);
        }

        return new GeodeCrackingResult(
            GeodeCrackingOutcome.Success,
            Array.AsReadOnly(crystals),
            $"Cracked '{geode.Id}' into {crystalCount} Rough crystal(s) at Crystal Shaping {request.CrystalShapingSkillLevel}.");
    }

    public static double CalculateSkillAdjustedBonusChance(double baseChance, int skillLevel, double maximumSkillBonus)
    {
        if (!IsChance(baseChance))
            throw new ArgumentOutOfRangeException(nameof(baseChance), "Base chance must be finite and between 0 and 1 inclusive.");
        if (skillLevel < 0 || skillLevel > 100)
            throw new ArgumentOutOfRangeException(nameof(skillLevel), "Crystal Shaping skill must be between 0 and 100 inclusive.");
        if (!IsChance(maximumSkillBonus))
            throw new ArgumentOutOfRangeException(nameof(maximumSkillBonus), "Maximum skill bonus must be finite and between 0 and 1 inclusive.");

        return Math.Min(1d, baseChance + ((skillLevel / 100d) * maximumSkillBonus));
    }

    private static ElementalAlignment SelectElement(
        IReadOnlyList<ElementWeight> weights,
        double totalWeight,
        double roll)
    {
        var target = roll * totalWeight;
        var cumulative = 0d;
        for (var i = 0; i < weights.Count; i++)
        {
            cumulative += weights[i].Weight;
            if (target < cumulative)
                return weights[i].Element;
        }

        return weights[weights.Count - 1].Element;
    }

    private static bool IsChance(double value) =>
        !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0d && value <= 1d;

    private static bool IsRoll(double value) =>
        !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0d && value < 1d;

    private static GeodeCrackingResult InvalidRequest(string reason) =>
        new(GeodeCrackingOutcome.InvalidRequest, Array.Empty<Crystal>(), reason);

    private static GeodeCrackingResult InvalidDefinition(string reason) =>
        new(GeodeCrackingOutcome.InvalidDefinition, Array.Empty<Crystal>(), reason);
}
