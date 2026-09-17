using System;
using System.Collections.Generic;
using Magenheim.Core.Networking;

namespace Magenheim.Core.Socketing;

public enum SocketExtractionOutcome
{
    SuccessReturnedCrystal = 0,
    FailedShatteredCrystal = 1,
    AuthorityDenied = 2,
    InvalidStation = 3,
    InvalidSkill = 4,
    InvalidRoll = 5,
    InvalidConfiguration = 6,
    NoInstalledCrystal = 7,
    InvalidIndex = 8,
    RoughCrystalInvalid = 9
}

public sealed record SocketExtractionRequest(
    DefinitionAuthorityResult Authority,
    SocketState CurrentState,
    int CrystalIndex,
    int CrystalShapingSkillLevel,
    string StationId,
    double Roll,
    double MaximumFailureReduction = CrystalRefinementService.DefaultMaximumFailureReduction);

public sealed record SocketExtractionPlan(
    SocketExtractionOutcome Outcome,
    SocketState OriginalState,
    SocketState ResultState,
    Crystal? ReturnedCrystal,
    ElementalAlignment? ShardElement,
    int ShardReturnCount,
    double EffectiveFailureChance,
    double CrystalShapingExperience,
    string Diagnostic)
{
    public bool MutationAuthorized =>
        Outcome == SocketExtractionOutcome.SuccessReturnedCrystal ||
        Outcome == SocketExtractionOutcome.FailedShatteredCrystal;

    public bool ReturnsCrystal =>
        Outcome == SocketExtractionOutcome.SuccessReturnedCrystal && ReturnedCrystal is not null;
}

/// <summary>
/// Pure planner for removing one installed crystal from per-item socket metadata.
///
/// This is socket removal, the third socket operation alongside opening a slot and
/// installing a crystal. It is unrelated to the geode/refinement chain, which never
/// extracts anything. The required station was previously the Faceting Wheel, a
/// Geologist's Workstation upgrade that also gates Crystal -> Advanced refinement; that
/// shared constant was the only thing tying socket removal to the refinement chain, and it
/// made a socket operation look like a refinement step. Socketing is now wholly owned by
/// the Crystal Enchanting Dais.
///
/// Removal remains intentionally risky: the crystal can shatter and return shards instead.
/// Runtime code must atomically apply ResultState and the returned crystal/shards.
/// </summary>
public static class SocketExtractionService
{
    public const string RequiredStationId = "Magenheim_CrystalEnchantingDais";

    public static SocketExtractionPlan Plan(SocketExtractionRequest request)
    {
        if (request is null || request.CurrentState is null)
            return Reject(SocketExtractionOutcome.NoInstalledCrystal, SocketState.Empty,
                "Socket extraction requires a current per-item socket state.");

        if (!HasMutationAuthority(request.Authority))
            return Reject(SocketExtractionOutcome.AuthorityDenied, request.CurrentState,
                "Definition authority is not compatible; crystal extraction is denied.");

        if (!string.Equals(request.StationId, RequiredStationId, StringComparison.Ordinal))
            return Reject(SocketExtractionOutcome.InvalidStation, request.CurrentState,
                "Removing a socketed crystal requires the Crystal Enchanting Dais.");

        if (request.CrystalShapingSkillLevel < 0 || request.CrystalShapingSkillLevel > 100)
            return Reject(SocketExtractionOutcome.InvalidSkill, request.CurrentState,
                "Crystal Shaping skill must be between 0 and 100 inclusive.");

        if (double.IsNaN(request.Roll) || double.IsInfinity(request.Roll) || request.Roll < 0d || request.Roll >= 1d)
            return Reject(SocketExtractionOutcome.InvalidRoll, request.CurrentState,
                "Extraction roll must be finite and in the range [0,1).");

        if (double.IsNaN(request.MaximumFailureReduction) ||
            double.IsInfinity(request.MaximumFailureReduction) ||
            request.MaximumFailureReduction < CrystalRefinementService.MinimumMaximumFailureReduction ||
            request.MaximumFailureReduction > CrystalRefinementService.MaximumMaximumFailureReduction)
        {
            return Reject(SocketExtractionOutcome.InvalidConfiguration, request.CurrentState,
                $"Maximum failure reduction must be between {CrystalRefinementService.MinimumMaximumFailureReduction:0.00} and {CrystalRefinementService.MaximumMaximumFailureReduction:0.00} inclusive.");
        }

        if (request.CurrentState.InstalledCrystals.Count == 0)
            return Reject(SocketExtractionOutcome.NoInstalledCrystal, request.CurrentState,
                "The selected item has no installed crystal to extract.");

        if (request.CrystalIndex < 0 || request.CrystalIndex >= request.CurrentState.InstalledCrystals.Count)
            return Reject(SocketExtractionOutcome.InvalidIndex, request.CurrentState,
                "Installed crystal index is outside the selected item's socket state.");

        var crystal = request.CurrentState.InstalledCrystals[request.CrystalIndex];
        if (crystal.Tier == CrystalTier.Rough)
            return Reject(SocketExtractionOutcome.RoughCrystalInvalid, request.CurrentState,
                "Rough crystals are not socketable and cannot be extracted from valid socket state.");

        if (!TryGetExtractionRisk(crystal.Tier, out var baseFailure, out var shards))
            return Reject(SocketExtractionOutcome.InvalidConfiguration, request.CurrentState,
                $"No extraction risk rule exists for crystal tier '{crystal.Tier}'.");

        var effectiveFailure = CrystalRefinementService.CalculateEffectiveFailureChance(
            baseFailure,
            request.CrystalShapingSkillLevel,
            request.MaximumFailureReduction);

        var removal = SocketingService.RemoveCrystal(request.CurrentState, request.CrystalIndex);
        if (!removal.Changed)
            return Reject(SocketExtractionOutcome.InvalidIndex, request.CurrentState, removal.Reason);

        if (request.Roll < effectiveFailure)
        {
            return new SocketExtractionPlan(
                SocketExtractionOutcome.FailedShatteredCrystal,
                request.CurrentState,
                removal.State,
                null,
                crystal.Element,
                shards,
                effectiveFailure,
                CrystalShapingExperience.CrystalExtraction,
                $"Extraction failed; the {crystal.Tier} {crystal.Element} crystal shattered and returned {shards} matching shard(s).");
        }

        return new SocketExtractionPlan(
            SocketExtractionOutcome.SuccessReturnedCrystal,
            request.CurrentState,
            removal.State,
            crystal,
            null,
            0,
            effectiveFailure,
            CrystalShapingExperience.CrystalExtraction,
            $"Extracted the {crystal.Tier} {crystal.Element} crystal intact.");
    }

    public static bool TryGetExtractionRisk(CrystalTier tier, out double baseFailureChance, out int shardReturnCount)
    {
        switch (tier)
        {
            case CrystalTier.Simple:
                baseFailureChance = 0.10d;
                shardReturnCount = 1;
                return true;
            case CrystalTier.Crystal:
                baseFailureChance = 0.20d;
                shardReturnCount = 2;
                return true;
            case CrystalTier.Advanced:
                baseFailureChance = 0.30d;
                shardReturnCount = 3;
                return true;
            case CrystalTier.Master:
                baseFailureChance = 0.40d;
                shardReturnCount = 5;
                return true;
            default:
                baseFailureChance = 0d;
                shardReturnCount = 0;
                return false;
        }
    }

    private static bool HasMutationAuthority(DefinitionAuthorityResult authority) =>
        authority is not null &&
        authority.Status == DefinitionAuthorityStatus.Compatible &&
        authority.MutationAuthorized;

    private static SocketExtractionPlan Reject(
        SocketExtractionOutcome outcome,
        SocketState state,
        string diagnostic) =>
        new(outcome, state, state, null, null, 0, 0d, 0d, diagnostic);
}

public readonly record struct SocketExtractionOperationKey(
    long PeerId,
    long SessionGeneration,
    string OperationId);

public enum SocketExtractionOperationOutcome
{
    Ready = 0,
    PlanRejected = 1,
    InvalidOperationKey = 2,
    DuplicatePrepared = 3,
    DuplicateApplied = 4,
    ConflictingReplay = 5
}

public sealed record SocketExtractionOperationDecision(
    SocketExtractionOperationOutcome Outcome,
    SocketExtractionPlan Plan,
    string Diagnostic)
{
    public bool MutationAuthorized =>
        Outcome == SocketExtractionOperationOutcome.Ready && Plan.MutationAuthorized;
}

/// <summary>
/// Session-scoped exact-once reservation for extraction. It stores no Valheim item
/// reference; runtime must still re-read the selected ItemData metadata before applying.
/// </summary>
public sealed class SocketExtractionOperationGuard
{
    public const int MaximumOperationIdLength = 128;
    private readonly Dictionary<SocketExtractionOperationKey, OperationRecord> _operations = new();

    public SocketExtractionOperationDecision Begin(
        SocketExtractionOperationKey key,
        SocketExtractionRequest request)
    {
        if (!IsValidKey(key, out var keyError))
        {
            return new SocketExtractionOperationDecision(
                SocketExtractionOperationOutcome.InvalidOperationKey,
                EmptyPlan(keyError),
                keyError);
        }

        var plan = SocketExtractionService.Plan(request);
        if (!plan.MutationAuthorized)
            return new SocketExtractionOperationDecision(
                SocketExtractionOperationOutcome.PlanRejected,
                plan,
                plan.Diagnostic);

        if (_operations.TryGetValue(key, out var existing))
        {
            if (!PlansMatch(existing.Plan, plan))
            {
                return new SocketExtractionOperationDecision(
                    SocketExtractionOperationOutcome.ConflictingReplay,
                    EmptyPlan("The operation id was replayed with a different extraction plan."),
                    "The operation id was replayed with a different extraction plan; mutation is denied.");
            }

            var outcome = existing.Applied
                ? SocketExtractionOperationOutcome.DuplicateApplied
                : SocketExtractionOperationOutcome.DuplicatePrepared;
            return new SocketExtractionOperationDecision(
                outcome,
                existing.Plan,
                existing.Applied
                    ? "This extraction was already applied; replay mutation is denied."
                    : "This extraction is already prepared; concurrent/replayed mutation is denied.");
        }

        _operations.Add(key, new OperationRecord(plan));
        return new SocketExtractionOperationDecision(
            SocketExtractionOperationOutcome.Ready,
            plan,
            "Fresh extraction admitted for exactly one per-item metadata mutation attempt.");
    }

    public bool MarkApplied(SocketExtractionOperationKey key)
    {
        if (!_operations.TryGetValue(key, out var record) || record.Applied)
            return false;
        record.Applied = true;
        return true;
    }

    public bool AbortPrepared(SocketExtractionOperationKey key)
    {
        if (!_operations.TryGetValue(key, out var record) || record.Applied)
            return false;
        return _operations.Remove(key);
    }

    public int RetirePeerSessions(long peerId, long currentSessionGeneration)
    {
        if (currentSessionGeneration <= 0)
            throw new ArgumentOutOfRangeException(nameof(currentSessionGeneration),
                "Session generation must be positive.");

        var stale = new List<SocketExtractionOperationKey>();
        foreach (var key in _operations.Keys)
        {
            if (key.PeerId == peerId && key.SessionGeneration != currentSessionGeneration)
                stale.Add(key);
        }

        foreach (var key in stale)
            _operations.Remove(key);
        return stale.Count;
    }

    private static bool IsValidKey(SocketExtractionOperationKey key, out string error)
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

    private static bool PlansMatch(SocketExtractionPlan left, SocketExtractionPlan right)
    {
        if (left.Outcome != right.Outcome ||
            left.ReturnedCrystal != right.ReturnedCrystal ||
            left.ShardElement != right.ShardElement ||
            left.ShardReturnCount != right.ShardReturnCount ||
            Math.Abs(left.EffectiveFailureChance - right.EffectiveFailureChance) > 0.0000001d ||
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

    private static SocketExtractionPlan EmptyPlan(string diagnostic) =>
        new(SocketExtractionOutcome.NoInstalledCrystal, SocketState.Empty, SocketState.Empty,
            null, null, 0, 0d, 0d, diagnostic);

    private sealed class OperationRecord
    {
        internal OperationRecord(SocketExtractionPlan plan) => Plan = plan;
        internal SocketExtractionPlan Plan { get; }
        internal bool Applied { get; set; }
    }
}
