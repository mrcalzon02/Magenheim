using System;
using System.Collections.Generic;
using Magenheim.Core.Networking;

namespace Magenheim.Core.Transactions;

public enum GeodeOpeningPlanOutcome
{
    Ready = 0,
    AuthorityDenied = 1,
    MissingSource = 2,
    InsufficientOutputCapacity = 3,
    InvalidCrackingRequest = 4,
}

public sealed record GeodeOpeningTransactionRequest(
    DefinitionAuthorityResult Authority,
    int SourceGeodeCount,
    int AvailableOutputSlots,
    GeodeCrackingRequest CrackingRequest);

public sealed record GeodeOpeningTransactionPlan(
    GeodeOpeningPlanOutcome Outcome,
    int ConsumeGeodeCount,
    IReadOnlyList<Crystal> GrantCrystals,
    string Diagnostic)
{
    public bool IsReady => Outcome == GeodeOpeningPlanOutcome.Ready;
}

/// <summary>
/// Pure pre-mutation authority for geode opening. This class never changes inventory.
/// Runtime code must execute a Ready plan atomically on the server and must revalidate
/// inventory ownership/capacity immediately before applying the transaction.
/// </summary>
public static class GeodeOpeningTransactionPlanner
{
    public const int GeodesConsumedPerOpening = 1;

    public static GeodeOpeningTransactionPlan Plan(GeodeOpeningTransactionRequest request)
    {
        if (request is null)
            return Reject(GeodeOpeningPlanOutcome.InvalidCrackingRequest, "Transaction request is required.");

        if (!HasMutationAuthority(request.Authority))
            return Reject(GeodeOpeningPlanOutcome.AuthorityDenied,
                "Definition authority is not compatible; geode mutation is denied.");

        if (request.SourceGeodeCount < GeodesConsumedPerOpening)
            return Reject(GeodeOpeningPlanOutcome.MissingSource,
                $"At least {GeodesConsumedPerOpening} source geode is required.");

        if (request.AvailableOutputSlots < 0)
            return Reject(GeodeOpeningPlanOutcome.InsufficientOutputCapacity,
                "Available output slots cannot be negative.");

        var cracking = GeodeCrackingService.Crack(request.CrackingRequest);
        if (!cracking.IsSuccess)
            return Reject(GeodeOpeningPlanOutcome.InvalidCrackingRequest,
                $"Geode cracking was rejected: {cracking.Reason}");

        if (request.AvailableOutputSlots < cracking.Crystals.Count)
            return Reject(GeodeOpeningPlanOutcome.InsufficientOutputCapacity,
                $"Opening requires {cracking.Crystals.Count} output slot(s), but only {request.AvailableOutputSlots} are available.");

        return new GeodeOpeningTransactionPlan(
            GeodeOpeningPlanOutcome.Ready,
            GeodesConsumedPerOpening,
            cracking.Crystals,
            $"Ready to consume one geode and grant {cracking.Crystals.Count} Rough crystal(s).");
    }

    private static bool HasMutationAuthority(DefinitionAuthorityResult authority) =>
        authority is not null &&
        authority.Status == DefinitionAuthorityStatus.Compatible &&
        authority.MutationAuthorized;

    private static GeodeOpeningTransactionPlan Reject(GeodeOpeningPlanOutcome outcome, string diagnostic) =>
        new(outcome, 0, Array.Empty<Crystal>(), diagnostic);
}
