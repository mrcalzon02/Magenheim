using System;
using System.Collections.Generic;
using Magenheim.Core.Definitions;

namespace Magenheim.Core;

public sealed record GeodeCrackingRequest(
    GeodeDefinition Geode,
    double SecondCrystalRoll,
    double ThirdCrystalRoll,
    IReadOnlyList<double> ElementRolls);

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
/// Pure deterministic geode outcome authority. Randomness is supplied explicitly by the caller so
/// multiplayer/runtime code can keep RNG ownership server-side and replay/verify the same decision.
/// The current schema always consumes both bonus rolls and all three possible element rolls even
/// when a bonus crystal does not materialize. Fixed consumption prevents hidden RNG-stream drift.
/// </summary>
public static class GeodeCrackingService
{
    public const int CurrentSchemaElementRollCount = 3;

    public static GeodeCrackingResult Crack(GeodeCrackingRequest request)
    {
        if (request is null)
            return InvalidRequest("Request is required.");
        if (request.Geode is null)
            return InvalidRequest("Geode definition is required.");
        if (request.ElementRolls is null)
            return InvalidRequest("Element rolls are required.");

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

        var crystalCount = 1;
        if (request.SecondCrystalRoll < geode.SecondCrystalChance)
            crystalCount++;
        if (request.ThirdCrystalRoll < geode.ThirdCrystalChance)
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
            $"Cracked '{geode.Id}' into {crystalCount} Rough crystal(s).");
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
