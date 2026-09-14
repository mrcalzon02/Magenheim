using System;
using System.Collections.Generic;
using Magenheim.Core.Networking;

namespace Magenheim.Core.Socketing;

public enum SocketTransactionKind
{
    AddSlot = 0,
    InstallCrystal = 1
}

public enum SocketTransactionOutcome
{
    Ready = 0,
    AuthorityDenied = 1,
    EquipmentIneligible = 2,
    StateExceedsPolicy = 3,
    MissingCrystal = 4,
    MutationRejected = 5,
    InvalidRequest = 6
}

public sealed record SocketTransactionRequest(
    DefinitionAuthorityResult Authority,
    EquipmentDescriptor Equipment,
    SocketEligibilityPolicy Policy,
    SocketState CurrentState,
    SocketTransactionKind Kind,
    Crystal? Crystal,
    int SourceCrystalCount);

public sealed record SocketTransactionPlan(
    SocketTransactionOutcome Outcome,
    SocketState OriginalState,
    SocketState ResultState,
    int ConsumeCrystalCount,
    Crystal? ConsumedCrystal,
    double CrystalShapingExperience,
    string Diagnostic)
{
    public bool IsReady => Outcome == SocketTransactionOutcome.Ready;
    public bool ChangesMetadata => IsReady &&
        (OriginalState.UnlockedSlots != ResultState.UnlockedSlots ||
         OriginalState.InstalledCrystals.Count != ResultState.InstalledCrystals.Count);
}

/// <summary>
/// Pure pre-mutation planner for per-item socket changes. It never touches ItemDrop,
/// inventory, or shared prefabs. Runtime code must revalidate that the exact ItemData
/// instance still has the OriginalState immediately before writing ResultState.
/// </summary>
public static class SocketTransactionPlanner
{
    public static SocketTransactionPlan Plan(SocketTransactionRequest request)
    {
        if (request is null || request.Equipment is null || request.Policy is null || request.CurrentState is null)
            return Invalid(SocketState.Empty, "Socket transaction request and all required state are required.");

        if (!HasMutationAuthority(request.Authority))
            return Reject(SocketTransactionOutcome.AuthorityDenied, request.CurrentState,
                "Definition authority is not compatible; socket mutation is denied.");

        SocketEligibilityResult eligibility;
        try
        {
            eligibility = SocketEligibilityService.Evaluate(request.Equipment, request.Policy);
        }
        catch (Exception exception)
        {
            return Reject(SocketTransactionOutcome.InvalidRequest, request.CurrentState,
                $"Equipment eligibility validation failed: {exception.Message}");
        }

        if (!eligibility.IsEligible)
            return Reject(SocketTransactionOutcome.EquipmentIneligible, request.CurrentState, eligibility.Reason);

        if (request.CurrentState.UnlockedSlots > eligibility.MaximumSlots)
        {
            return Reject(SocketTransactionOutcome.StateExceedsPolicy, request.CurrentState,
                $"Item has {request.CurrentState.UnlockedSlots} unlocked socket(s), above the current policy maximum of {eligibility.MaximumSlots}; " +
                "existing metadata is preserved but new socket mutation is denied until policy/state is reconciled.");
        }

        switch (request.Kind)
        {
            case SocketTransactionKind.AddSlot:
            {
                var mutation = SocketingService.AddSlot(request.CurrentState, eligibility.MaximumSlots);
                if (!mutation.Changed)
                    return Reject(SocketTransactionOutcome.MutationRejected, request.CurrentState, mutation.Reason);

                return new SocketTransactionPlan(
                    SocketTransactionOutcome.Ready,
                    request.CurrentState,
                    mutation.State,
                    0,
                    null,
                    0d,
                    $"Ready to add one socket to '{request.Equipment.PrefabName}' without modifying its shared prefab.");
            }

            case SocketTransactionKind.InstallCrystal:
            {
                if (request.Crystal is null)
                    return Reject(SocketTransactionOutcome.MissingCrystal, request.CurrentState,
                        "A specific Simple-or-better crystal is required for installation.");
                if (request.SourceCrystalCount < 1)
                    return Reject(SocketTransactionOutcome.MissingCrystal, request.CurrentState,
                        "The selected crystal is no longer available for consumption.");

                var mutation = SocketingService.InstallCrystal(request.CurrentState, request.Crystal.Value);
                if (!mutation.Changed)
                    return Reject(SocketTransactionOutcome.MutationRejected, request.CurrentState, mutation.Reason);

                return new SocketTransactionPlan(
                    SocketTransactionOutcome.Ready,
                    request.CurrentState,
                    mutation.State,
                    1,
                    request.Crystal,
                    CrystalShapingExperience.SocketInstall,
                    $"Ready to consume one {request.Crystal.Value.Tier} {request.Crystal.Value.Element} crystal and write socket metadata only to the selected item instance.");
            }

            default:
                return Reject(SocketTransactionOutcome.InvalidRequest, request.CurrentState,
                    $"Unknown socket transaction kind '{(int)request.Kind}'.");
        }
    }

    private static bool HasMutationAuthority(DefinitionAuthorityResult authority) =>
        authority is not null &&
        authority.Status == DefinitionAuthorityStatus.Compatible &&
        authority.MutationAuthorized;

    private static SocketTransactionPlan Invalid(SocketState state, string diagnostic) =>
        Reject(SocketTransactionOutcome.InvalidRequest, state, diagnostic);

    private static SocketTransactionPlan Reject(SocketTransactionOutcome outcome, SocketState state, string diagnostic) =>
        new(outcome, state, state, 0, null, 0d, diagnostic);
}

public readonly record struct SocketOperationKey(
    long PeerId,
    long SessionGeneration,
    string OperationId);

public enum SocketOperationOutcome
{
    Ready = 0,
    PlanRejected = 1,
    InvalidOperationKey = 2,
    DuplicatePrepared = 3,
    DuplicateApplied = 4,
    ConflictingReplay = 5
}

public sealed record SocketOperationDecision(
    SocketOperationOutcome Outcome,
    SocketTransactionPlan Plan,
    string Diagnostic)
{
    public bool MutationAuthorized => Outcome == SocketOperationOutcome.Ready && Plan.IsReady;
}

/// <summary>
/// Session-scoped exact-once reservation for socket metadata mutation. The guard stores
/// no ItemData reference; the runtime must bind the prepared plan to the exact selected
/// item instance and compare its current metadata to Plan.OriginalState before applying.
/// </summary>
public sealed class SocketOperationGuard
{
    public const int MaximumOperationIdLength = 128;
    private readonly Dictionary<SocketOperationKey, OperationRecord> _operations = new();

    public SocketOperationDecision Begin(SocketOperationKey key, SocketTransactionRequest request)
    {
        if (!IsValidKey(key, out var keyError))
            return new SocketOperationDecision(
                SocketOperationOutcome.InvalidOperationKey,
                EmptyPlan(keyError),
                keyError);

        var plan = SocketTransactionPlanner.Plan(request);
        if (!plan.IsReady)
            return new SocketOperationDecision(SocketOperationOutcome.PlanRejected, plan, plan.Diagnostic);

        if (_operations.TryGetValue(key, out var existing))
        {
            if (!PlansMatch(existing.Plan, plan))
            {
                return new SocketOperationDecision(
                    SocketOperationOutcome.ConflictingReplay,
                    EmptyPlan("The operation id was replayed with a different socket plan."),
                    "The operation id was replayed with a different socket plan; mutation is denied.");
            }

            var outcome = existing.Applied
                ? SocketOperationOutcome.DuplicateApplied
                : SocketOperationOutcome.DuplicatePrepared;
            return new SocketOperationDecision(
                outcome,
                existing.Plan,
                existing.Applied
                    ? "This socket operation was already applied; replay mutation is denied."
                    : "This socket operation is already prepared; concurrent/replayed mutation is denied.");
        }

        _operations.Add(key, new OperationRecord(plan));
        return new SocketOperationDecision(
            SocketOperationOutcome.Ready,
            plan,
            "Fresh socket operation admitted for exactly one per-item metadata mutation attempt.");
    }

    public bool MarkApplied(SocketOperationKey key)
    {
        if (!_operations.TryGetValue(key, out var record) || record.Applied)
            return false;
        record.Applied = true;
        return true;
    }

    public bool AbortPrepared(SocketOperationKey key)
    {
        if (!_operations.TryGetValue(key, out var record) || record.Applied)
            return false;
        return _operations.Remove(key);
    }

    public int RetirePeerSessions(long peerId, long currentSessionGeneration)
    {
        if (currentSessionGeneration <= 0)
            throw new ArgumentOutOfRangeException(nameof(currentSessionGeneration), "Session generation must be positive.");

        var stale = new List<SocketOperationKey>();
        foreach (var key in _operations.Keys)
        {
            if (key.PeerId == peerId && key.SessionGeneration != currentSessionGeneration)
                stale.Add(key);
        }
        foreach (var key in stale)
            _operations.Remove(key);
        return stale.Count;
    }

    private static bool IsValidKey(SocketOperationKey key, out string error)
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

    private static bool PlansMatch(SocketTransactionPlan left, SocketTransactionPlan right)
    {
        if (left.Outcome != right.Outcome ||
            left.ConsumeCrystalCount != right.ConsumeCrystalCount ||
            left.ConsumedCrystal != right.ConsumedCrystal ||
            Math.Abs(left.CrystalShapingExperience - right.CrystalShapingExperience) > 0.0000001d)
            return false;
        return StatesMatch(left.OriginalState, right.OriginalState) &&
               StatesMatch(left.ResultState, right.ResultState);
    }

    private static bool StatesMatch(SocketState left, SocketState right)
    {
        if (left.UnlockedSlots != right.UnlockedSlots ||
            left.InstalledCrystals.Count != right.InstalledCrystals.Count)
            return false;
        for (var i = 0; i < left.InstalledCrystals.Count; i++)
            if (left.InstalledCrystals[i] != right.InstalledCrystals[i]) return false;
        return true;
    }

    private static SocketTransactionPlan EmptyPlan(string diagnostic) =>
        new(SocketTransactionOutcome.InvalidRequest, SocketState.Empty, SocketState.Empty, 0, null, 0d, diagnostic);

    private sealed class OperationRecord
    {
        internal OperationRecord(SocketTransactionPlan plan) => Plan = plan;
        internal SocketTransactionPlan Plan { get; }
        internal bool Applied { get; set; }
    }
}
