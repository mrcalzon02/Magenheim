using System;
using System.Collections.Generic;
using Magenheim.Core.Networking;

namespace Magenheim.Core.Transactions;

public enum RefinementTransactionPlanOutcome
{
    Ready = 0,
    AuthorityDenied = 1,
    MissingSource = 2,
    InsufficientOutputCapacity = 3,
    RefinementRejected = 4,
}

public sealed record RefinementTransactionRequest(
    DefinitionAuthorityResult Authority,
    int SourceCrystalCount,
    int AvailableOutputSlotsAfterConsumption,
    RefinementRequest Refinement);

public sealed record RefinementTransactionPlan(
    RefinementTransactionPlanOutcome Outcome,
    int ConsumeSourceCount,
    Crystal? GrantCrystal,
    int GrantShardCount,
    ElementalAlignment? ShardElement,
    bool AwardExperience,
    double EffectiveFailureChance,
    string Diagnostic)
{
    public bool IsReady => Outcome == RefinementTransactionPlanOutcome.Ready;
    public bool IsFailureOutcome => IsReady && GrantCrystal is null && GrantShardCount > 0;
}

/// <summary>
/// Pure pre-mutation authority for one crystal refinement attempt. Runtime code supplies
/// definition authority, current inventory state, deterministic roll, station identity,
/// and skill level. A Ready plan always consumes exactly one source crystal and either
/// grants exactly one next-tier crystal or the configured matching shards.
/// </summary>
public sealed class RefinementTransactionPlanner
{
    public const int CrystalsConsumedPerAttempt = 1;
    private readonly CrystalRefinementService _refinement;

    public RefinementTransactionPlanner(CrystalRefinementService refinement)
    {
        _refinement = refinement ?? throw new ArgumentNullException(nameof(refinement));
    }

    public RefinementTransactionPlan Plan(RefinementTransactionRequest request)
    {
        if (request is null)
            return Reject(RefinementTransactionPlanOutcome.RefinementRejected, "Transaction request is required.");

        if (!HasMutationAuthority(request.Authority))
            return Reject(RefinementTransactionPlanOutcome.AuthorityDenied,
                "Definition authority is not compatible; refinement mutation is denied.");

        if (request.SourceCrystalCount < CrystalsConsumedPerAttempt)
            return Reject(RefinementTransactionPlanOutcome.MissingSource,
                $"At least {CrystalsConsumedPerAttempt} source crystal is required.");

        if (request.AvailableOutputSlotsAfterConsumption < 0)
            return Reject(RefinementTransactionPlanOutcome.InsufficientOutputCapacity,
                "Available output slots cannot be negative.");

        var result = _refinement.Refine(request.Refinement);
        if (result.Outcome != RefinementOutcome.Success && result.Outcome != RefinementOutcome.FailedDestroyed)
            return Reject(RefinementTransactionPlanOutcome.RefinementRejected, result.Reason);

        // Success grants one crystal stack; a destructive failure with shards grants one
        // shard stack. A zero-shard failure needs no output slot, though canonical rules
        // currently return at least one shard for every destructive failure.
        var requiredOutputSlots = result.Outcome == RefinementOutcome.Success || result.ShardReturnCount > 0 ? 1 : 0;
        if (request.AvailableOutputSlotsAfterConsumption < requiredOutputSlots)
        {
            return Reject(RefinementTransactionPlanOutcome.InsufficientOutputCapacity,
                $"Refinement requires {requiredOutputSlots} output slot(s) after source consumption, " +
                $"but only {request.AvailableOutputSlotsAfterConsumption} are available.");
        }

        if (result.Outcome == RefinementOutcome.Success)
        {
            if (result.Output is null)
                return Reject(RefinementTransactionPlanOutcome.RefinementRejected,
                    "Refinement service reported success without an output crystal.");

            return new RefinementTransactionPlan(
                RefinementTransactionPlanOutcome.Ready,
                CrystalsConsumedPerAttempt,
                result.Output,
                0,
                null,
                result.AwardExperience,
                result.EffectiveFailureChance,
                result.Reason);
        }

        return new RefinementTransactionPlan(
            RefinementTransactionPlanOutcome.Ready,
            CrystalsConsumedPerAttempt,
            null,
            result.ShardReturnCount,
            request.Refinement.Input.Element,
            result.AwardExperience,
            result.EffectiveFailureChance,
            result.Reason);
    }

    private static bool HasMutationAuthority(DefinitionAuthorityResult authority) =>
        authority is not null &&
        authority.Status == DefinitionAuthorityStatus.Compatible &&
        authority.MutationAuthorized;

    private static RefinementTransactionPlan Reject(RefinementTransactionPlanOutcome outcome, string diagnostic) =>
        new(outcome, 0, null, 0, null, false, 0d, diagnostic);
}

public readonly record struct RefinementOperationKey(
    long PeerId,
    long SessionGeneration,
    string OperationId);

public enum RefinementOperationOutcome
{
    Ready = 0,
    PlanRejected = 1,
    InvalidOperationKey = 2,
    DuplicatePrepared = 3,
    DuplicateApplied = 4,
    ConflictingReplay = 5,
}

public sealed record RefinementOperationDecision(
    RefinementOperationOutcome Outcome,
    RefinementTransactionPlan Plan,
    string Diagnostic)
{
    public bool MutationAuthorized => Outcome == RefinementOperationOutcome.Ready && Plan.IsReady;
}

/// <summary>
/// Session-scoped exact-once admission for refinement. Immutable operation intent is
/// captured before mutable inventory/capacity state is replanned so retransmission remains
/// identifiable after the original transaction consumed its source crystal.
/// </summary>
public sealed class RefinementOperationGuard
{
    public const int MaximumOperationIdLength = 128;
    private readonly Dictionary<RefinementOperationKey, OperationRecord> _operations = new();
    private readonly RefinementTransactionPlanner _planner;

    public RefinementOperationGuard(RefinementTransactionPlanner planner)
    {
        _planner = planner ?? throw new ArgumentNullException(nameof(planner));
    }

    public RefinementOperationDecision Begin(RefinementOperationKey key, RefinementTransactionRequest request)
    {
        if (!IsValidKey(key, out var keyError))
            return new RefinementOperationDecision(
                RefinementOperationOutcome.InvalidOperationKey,
                EmptyPlan(keyError),
                keyError);

        var intent = RefinementIntent.From(request);
        if (_operations.TryGetValue(key, out var existing))
        {
            if (!existing.Intent.Equals(intent))
            {
                return new RefinementOperationDecision(
                    RefinementOperationOutcome.ConflictingReplay,
                    EmptyPlan("The operation id was replayed with different immutable refinement intent."),
                    "The operation id was replayed with different immutable refinement intent; mutation is denied.");
            }

            var duplicateOutcome = existing.Applied
                ? RefinementOperationOutcome.DuplicateApplied
                : RefinementOperationOutcome.DuplicatePrepared;
            return new RefinementOperationDecision(
                duplicateOutcome,
                existing.Plan,
                existing.Applied
                    ? "This refinement operation was already applied; replay mutation is denied."
                    : "This refinement operation is already prepared; concurrent/replayed mutation is denied.");
        }

        var plan = _planner.Plan(request);
        if (!plan.IsReady)
            return new RefinementOperationDecision(
                RefinementOperationOutcome.PlanRejected,
                plan,
                plan.Diagnostic);

        _operations.Add(key, new OperationRecord(intent, plan));
        return new RefinementOperationDecision(
            RefinementOperationOutcome.Ready,
            plan,
            "Fresh refinement operation admitted for exactly one mutation attempt.");
    }

    public bool MarkApplied(RefinementOperationKey key)
    {
        if (!_operations.TryGetValue(key, out var record) || record.Applied)
            return false;
        record.Applied = true;
        return true;
    }

    public bool AbortPrepared(RefinementOperationKey key)
    {
        if (!_operations.TryGetValue(key, out var record) || record.Applied)
            return false;
        return _operations.Remove(key);
    }

    public int RetirePeerSessions(long peerId, long currentSessionGeneration)
    {
        if (currentSessionGeneration <= 0)
            throw new ArgumentOutOfRangeException(nameof(currentSessionGeneration), "Session generation must be positive.");

        var stale = new List<RefinementOperationKey>();
        foreach (var key in _operations.Keys)
        {
            if (key.PeerId == peerId && key.SessionGeneration != currentSessionGeneration)
                stale.Add(key);
        }
        foreach (var key in stale)
            _operations.Remove(key);
        return stale.Count;
    }

    private static bool IsValidKey(RefinementOperationKey key, out string error)
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

    private static RefinementTransactionPlan EmptyPlan(string diagnostic) =>
        new(RefinementTransactionPlanOutcome.RefinementRejected, 0, null, 0, null, false, 0d, diagnostic);

    private sealed class OperationRecord
    {
        internal OperationRecord(RefinementIntent intent, RefinementTransactionPlan plan)
        {
            Intent = intent;
            Plan = plan;
        }

        internal RefinementIntent Intent { get; }
        internal RefinementTransactionPlan Plan { get; }
        internal bool Applied { get; set; }
    }

    private sealed class RefinementIntent : IEquatable<RefinementIntent>
    {
        private RefinementIntent(
            ElementalAlignment element,
            CrystalTier tier,
            int skillLevel,
            string stationId,
            long rollBits,
            long maximumFailureReductionBits)
        {
            Element = element;
            Tier = tier;
            SkillLevel = skillLevel;
            StationId = stationId;
            RollBits = rollBits;
            MaximumFailureReductionBits = maximumFailureReductionBits;
        }

        private ElementalAlignment Element { get; }
        private CrystalTier Tier { get; }
        private int SkillLevel { get; }
        private string StationId { get; }
        private long RollBits { get; }
        private long MaximumFailureReductionBits { get; }

        internal static RefinementIntent From(RefinementTransactionRequest request)
        {
            if (request is null || request.Refinement is null)
                return new RefinementIntent(default, default, 0, string.Empty, 0L, 0L);

            var refinement = request.Refinement;
            return new RefinementIntent(
                refinement.Input.Element,
                refinement.Input.Tier,
                refinement.CrystalShapingSkillLevel,
                refinement.StationId ?? string.Empty,
                BitConverter.DoubleToInt64Bits(refinement.Roll),
                BitConverter.DoubleToInt64Bits(refinement.MaximumFailureReduction));
        }

        public bool Equals(RefinementIntent other)
        {
            if (ReferenceEquals(other, null)) return false;
            return Element == other.Element &&
                   Tier == other.Tier &&
                   SkillLevel == other.SkillLevel &&
                   string.Equals(StationId, other.StationId, StringComparison.Ordinal) &&
                   RollBits == other.RollBits &&
                   MaximumFailureReductionBits == other.MaximumFailureReductionBits;
        }

        public override bool Equals(object obj) => obj is RefinementIntent other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Element;
                hash = (hash * 397) ^ (int)Tier;
                hash = (hash * 397) ^ SkillLevel;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(StationId);
                hash = (hash * 397) ^ RollBits.GetHashCode();
                hash = (hash * 397) ^ MaximumFailureReductionBits.GetHashCode();
                return hash;
            }
        }
    }
}
