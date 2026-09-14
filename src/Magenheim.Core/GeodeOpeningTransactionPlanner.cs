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

public readonly record struct GeodeOpeningOperationKey(
    long PeerId,
    long SessionGeneration,
    string OperationId);

public enum GeodeOpeningOperationOutcome
{
    Ready = 0,
    PlanRejected = 1,
    InvalidOperationKey = 2,
    DuplicatePrepared = 3,
    DuplicateApplied = 4,
    ConflictingReplay = 5,
}

public sealed record GeodeOpeningOperationDecision(
    GeodeOpeningOperationOutcome Outcome,
    GeodeOpeningTransactionPlan Plan,
    string Diagnostic)
{
    /// <summary>
    /// Only a fresh Ready decision may cause gameplay mutation. Duplicate outcomes
    /// intentionally never authorize a second application of an earlier plan.
    /// </summary>
    public bool MutationAuthorized => Outcome == GeodeOpeningOperationOutcome.Ready && Plan.IsReady;
}

/// <summary>
/// Session-scoped exact-once admission guard for server-side geode opening.
/// The immutable operation intent is recorded before mutation-state revalidation so a network
/// replay remains identifiable after the original transaction consumed its source item.
/// Runtime code calls Begin, applies the returned Ready plan atomically, then calls MarkApplied.
/// If no mutation occurred, AbortPrepared releases the reservation for a legitimate retry.
/// </summary>
public sealed class GeodeOpeningOperationGuard
{
    public const int MaximumOperationIdLength = 128;

    private readonly Dictionary<GeodeOpeningOperationKey, OperationRecord> _operations = new();

    public GeodeOpeningOperationDecision Begin(
        GeodeOpeningOperationKey key,
        GeodeOpeningTransactionRequest request)
    {
        if (!IsValidKey(key, out var keyError))
            return new GeodeOpeningOperationDecision(
                GeodeOpeningOperationOutcome.InvalidOperationKey,
                EmptyPlan(keyError),
                keyError);

        var intent = GeodeOpeningIntent.From(request);
        if (_operations.TryGetValue(key, out var existing))
        {
            if (!existing.Intent.Equals(intent))
            {
                return new GeodeOpeningOperationDecision(
                    GeodeOpeningOperationOutcome.ConflictingReplay,
                    EmptyPlan("The operation id was replayed with different immutable geode-opening intent."),
                    "The operation id was replayed with different immutable geode-opening intent; mutation is denied.");
            }

            var duplicateOutcome = existing.Applied
                ? GeodeOpeningOperationOutcome.DuplicateApplied
                : GeodeOpeningOperationOutcome.DuplicatePrepared;

            return new GeodeOpeningOperationDecision(
                duplicateOutcome,
                existing.Plan,
                existing.Applied
                    ? "This operation was already applied; replay mutation is denied."
                    : "This operation is already prepared; a concurrent/replayed mutation is denied.");
        }

        var plan = GeodeOpeningTransactionPlanner.Plan(request);
        if (!plan.IsReady)
            return new GeodeOpeningOperationDecision(
                GeodeOpeningOperationOutcome.PlanRejected,
                plan,
                plan.Diagnostic);

        _operations.Add(key, new OperationRecord(intent, plan));
        return new GeodeOpeningOperationDecision(
            GeodeOpeningOperationOutcome.Ready,
            plan,
            "Fresh server operation admitted for exactly one mutation attempt.");
    }

    public bool MarkApplied(GeodeOpeningOperationKey key)
    {
        if (!_operations.TryGetValue(key, out var record) || record.Applied)
            return false;

        record.Applied = true;
        return true;
    }

    public bool AbortPrepared(GeodeOpeningOperationKey key)
    {
        if (!_operations.TryGetValue(key, out var record) || record.Applied)
            return false;

        return _operations.Remove(key);
    }

    /// <summary>
    /// Removes operation history belonging to older sessions of a routed peer id.
    /// The caller must supply the newly established positive session generation.
    /// </summary>
    public int RetirePeerSessions(long peerId, long currentSessionGeneration)
    {
        if (currentSessionGeneration <= 0)
            throw new ArgumentOutOfRangeException(nameof(currentSessionGeneration), "Session generation must be positive.");

        var stale = new List<GeodeOpeningOperationKey>();
        foreach (var key in _operations.Keys)
        {
            if (key.PeerId == peerId && key.SessionGeneration != currentSessionGeneration)
                stale.Add(key);
        }

        foreach (var key in stale)
            _operations.Remove(key);

        return stale.Count;
    }

    private static bool IsValidKey(GeodeOpeningOperationKey key, out string error)
    {
        if (key.SessionGeneration <= 0)
        {
            error = "Session generation must be positive.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(key.OperationId))
        {
            error = "Operation id is required.";
            return false;
        }

        if (key.OperationId.Length > MaximumOperationIdLength)
        {
            error = $"Operation id cannot exceed {MaximumOperationIdLength} characters.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static GeodeOpeningTransactionPlan EmptyPlan(string diagnostic) =>
        new(GeodeOpeningPlanOutcome.InvalidCrackingRequest, 0, Array.Empty<Crystal>(), diagnostic);

    private sealed class OperationRecord
    {
        internal OperationRecord(GeodeOpeningIntent intent, GeodeOpeningTransactionPlan plan)
        {
            Intent = intent;
            Plan = plan;
        }

        internal GeodeOpeningIntent Intent { get; }
        internal GeodeOpeningTransactionPlan Plan { get; }
        internal bool Applied { get; set; }
    }

    private sealed class GeodeOpeningIntent : IEquatable<GeodeOpeningIntent>
    {
        private GeodeOpeningIntent(string geodeId, long secondRoll, long thirdRoll, long[] elementRolls)
        {
            GeodeId = geodeId;
            SecondRoll = secondRoll;
            ThirdRoll = thirdRoll;
            ElementRolls = elementRolls;
        }

        private string GeodeId { get; }
        private long SecondRoll { get; }
        private long ThirdRoll { get; }
        private long[] ElementRolls { get; }

        internal static GeodeOpeningIntent From(GeodeOpeningTransactionRequest request)
        {
            if (request is null || request.CrackingRequest is null)
                return new GeodeOpeningIntent(string.Empty, 0L, 0L, Array.Empty<long>());

            var cracking = request.CrackingRequest;
            var geodeId = cracking.Geode?.Id ?? string.Empty;
            var elementRolls = cracking.ElementRolls is null
                ? Array.Empty<long>()
                : CopyRollBits(cracking.ElementRolls);

            return new GeodeOpeningIntent(
                geodeId,
                BitConverter.DoubleToInt64Bits(cracking.SecondCrystalRoll),
                BitConverter.DoubleToInt64Bits(cracking.ThirdCrystalRoll),
                elementRolls);
        }

        public bool Equals(GeodeOpeningIntent other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (!string.Equals(GeodeId, other.GeodeId, StringComparison.Ordinal) ||
                SecondRoll != other.SecondRoll ||
                ThirdRoll != other.ThirdRoll ||
                ElementRolls.Length != other.ElementRolls.Length)
                return false;

            for (var index = 0; index < ElementRolls.Length; index++)
            {
                if (ElementRolls[index] != other.ElementRolls[index])
                    return false;
            }

            return true;
        }

        public override bool Equals(object obj) => obj is GeodeOpeningIntent other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = StringComparer.Ordinal.GetHashCode(GeodeId);
                hash = (hash * 397) ^ SecondRoll.GetHashCode();
                hash = (hash * 397) ^ ThirdRoll.GetHashCode();
                for (var index = 0; index < ElementRolls.Length; index++)
                    hash = (hash * 397) ^ ElementRolls[index].GetHashCode();
                return hash;
            }
        }

        private static long[] CopyRollBits(IReadOnlyList<double> rolls)
        {
            var result = new long[rolls.Count];
            for (var index = 0; index < rolls.Count; index++)
                result[index] = BitConverter.DoubleToInt64Bits(rolls[index]);
            return result;
        }
    }
}
