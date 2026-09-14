using System;
using System.Collections.Generic;
using System.Linq;
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
/// Session-scoped exact-once reservation for socket metadata mutation. Immutable request
/// intent is recorded before mutable source-crystal availability is replanned, so an install
/// remains identifiable after its source crystal was consumed. The guard stores no ItemData
/// reference; runtime still binds the prepared plan to the exact selected item instance and
/// verifies Plan.OriginalState immediately before applying metadata.
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

        var intent = SocketOperationIntent.From(request);
        if (_operations.TryGetValue(key, out var existing))
        {
            if (!existing.Intent.Equals(intent))
            {
                return new SocketOperationDecision(
                    SocketOperationOutcome.ConflictingReplay,
                    EmptyPlan("The operation id was replayed with different immutable socket intent."),
                    "The operation id was replayed with different immutable socket intent; mutation is denied.");
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

        var plan = SocketTransactionPlanner.Plan(request);
        if (!plan.IsReady)
            return new SocketOperationDecision(SocketOperationOutcome.PlanRejected, plan, plan.Diagnostic);

        _operations.Add(key, new OperationRecord(intent, plan));
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

    private static SocketTransactionPlan EmptyPlan(string diagnostic) =>
        new(SocketTransactionOutcome.InvalidRequest, SocketState.Empty, SocketState.Empty, 0, null, 0d, diagnostic);

    private sealed class OperationRecord
    {
        internal OperationRecord(SocketOperationIntent intent, SocketTransactionPlan plan)
        {
            Intent = intent;
            Plan = plan;
        }

        internal SocketOperationIntent Intent { get; }
        internal SocketTransactionPlan Plan { get; }
        internal bool Applied { get; set; }
    }

    private sealed class SocketOperationIntent : IEquatable<SocketOperationIntent>
    {
        private SocketOperationIntent(
            string prefabName,
            string modOrigin,
            EquipmentCategory category,
            SocketTransactionKind kind,
            Crystal? crystal,
            int unlockedSlots,
            Crystal[] installedCrystals,
            SocketPolicyIntent policy)
        {
            PrefabName = prefabName;
            ModOrigin = modOrigin;
            Category = category;
            Kind = kind;
            Crystal = crystal;
            UnlockedSlots = unlockedSlots;
            InstalledCrystals = installedCrystals;
            Policy = policy;
        }

        private string PrefabName { get; }
        private string ModOrigin { get; }
        private EquipmentCategory Category { get; }
        private SocketTransactionKind Kind { get; }
        private Crystal? Crystal { get; }
        private int UnlockedSlots { get; }
        private Crystal[] InstalledCrystals { get; }
        private SocketPolicyIntent Policy { get; }

        internal static SocketOperationIntent From(SocketTransactionRequest request)
        {
            if (request is null || request.Equipment is null || request.Policy is null || request.CurrentState is null)
            {
                return new SocketOperationIntent(
                    string.Empty,
                    string.Empty,
                    EquipmentCategory.Unknown,
                    default,
                    null,
                    0,
                    Array.Empty<Crystal>(),
                    SocketPolicyIntent.Empty);
            }

            return new SocketOperationIntent(
                request.Equipment.PrefabName ?? string.Empty,
                request.Equipment.ModOrigin ?? string.Empty,
                request.Equipment.Category,
                request.Kind,
                request.Crystal,
                request.CurrentState.UnlockedSlots,
                request.CurrentState.InstalledCrystals.ToArray(),
                SocketPolicyIntent.From(request.Policy));
        }

        public bool Equals(SocketOperationIntent other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (!string.Equals(PrefabName, other.PrefabName, StringComparison.Ordinal) ||
                !string.Equals(ModOrigin, other.ModOrigin, StringComparison.Ordinal) ||
                Category != other.Category ||
                Kind != other.Kind ||
                Crystal != other.Crystal ||
                UnlockedSlots != other.UnlockedSlots ||
                InstalledCrystals.Length != other.InstalledCrystals.Length ||
                !Policy.Equals(other.Policy))
                return false;

            for (var index = 0; index < InstalledCrystals.Length; index++)
                if (InstalledCrystals[index] != other.InstalledCrystals[index]) return false;
            return true;
        }

        public override bool Equals(object obj) => obj is SocketOperationIntent other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = StringComparer.Ordinal.GetHashCode(PrefabName);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(ModOrigin);
                hash = (hash * 397) ^ (int)Category;
                hash = (hash * 397) ^ (int)Kind;
                hash = (hash * 397) ^ (Crystal?.GetHashCode() ?? 0);
                hash = (hash * 397) ^ UnlockedSlots;
                for (var index = 0; index < InstalledCrystals.Length; index++)
                    hash = (hash * 397) ^ InstalledCrystals[index].GetHashCode();
                hash = (hash * 397) ^ Policy.GetHashCode();
                return hash;
            }
        }
    }

    private sealed class SocketPolicyIntent : IEquatable<SocketPolicyIntent>
    {
        internal static SocketPolicyIntent Empty { get; } = new(
            0, 0, 0, 0, 0, 0, SocketIdentityComparison.Exact,
            Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<EquipmentCategory>());

        private SocketPolicyIntent(
            int weaponMaxSlots,
            int armorMaxSlots,
            int shieldMaxSlots,
            int toolMaxSlots,
            int utilityMaxSlots,
            int explicitIncludeMaxSlots,
            SocketIdentityComparison identityComparison,
            string[] includedPrefabs,
            string[] excludedPrefabs,
            string[] includedOrigins,
            string[] excludedOrigins,
            EquipmentCategory[] excludedCategories)
        {
            WeaponMaxSlots = weaponMaxSlots;
            ArmorMaxSlots = armorMaxSlots;
            ShieldMaxSlots = shieldMaxSlots;
            ToolMaxSlots = toolMaxSlots;
            UtilityMaxSlots = utilityMaxSlots;
            ExplicitIncludeMaxSlots = explicitIncludeMaxSlots;
            IdentityComparison = identityComparison;
            IncludedPrefabs = includedPrefabs;
            ExcludedPrefabs = excludedPrefabs;
            IncludedOrigins = includedOrigins;
            ExcludedOrigins = excludedOrigins;
            ExcludedCategories = excludedCategories;
        }

        private int WeaponMaxSlots { get; }
        private int ArmorMaxSlots { get; }
        private int ShieldMaxSlots { get; }
        private int ToolMaxSlots { get; }
        private int UtilityMaxSlots { get; }
        private int ExplicitIncludeMaxSlots { get; }
        private SocketIdentityComparison IdentityComparison { get; }
        private string[] IncludedPrefabs { get; }
        private string[] ExcludedPrefabs { get; }
        private string[] IncludedOrigins { get; }
        private string[] ExcludedOrigins { get; }
        private EquipmentCategory[] ExcludedCategories { get; }

        internal static SocketPolicyIntent From(SocketEligibilityPolicy policy)
        {
            var comparer = policy.IdentityComparison == SocketIdentityComparison.CaseInsensitive
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal;
            return new SocketPolicyIntent(
                policy.WeaponMaxSlots,
                policy.ArmorMaxSlots,
                policy.ShieldMaxSlots,
                policy.ToolMaxSlots,
                policy.UtilityMaxSlots,
                policy.ExplicitIncludeMaxSlots,
                policy.IdentityComparison,
                Canonicalize(policy.IncludedPrefabNames, comparer),
                Canonicalize(policy.ExcludedPrefabNames, comparer),
                Canonicalize(policy.IncludedModOrigins, comparer),
                Canonicalize(policy.ExcludedModOrigins, comparer),
                policy.ExcludedCategories.OrderBy(value => (int)value).ToArray());
        }

        public bool Equals(SocketPolicyIntent other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (WeaponMaxSlots != other.WeaponMaxSlots ||
                ArmorMaxSlots != other.ArmorMaxSlots ||
                ShieldMaxSlots != other.ShieldMaxSlots ||
                ToolMaxSlots != other.ToolMaxSlots ||
                UtilityMaxSlots != other.UtilityMaxSlots ||
                ExplicitIncludeMaxSlots != other.ExplicitIncludeMaxSlots ||
                IdentityComparison != other.IdentityComparison ||
                !SequencesEqual(IncludedPrefabs, other.IncludedPrefabs) ||
                !SequencesEqual(ExcludedPrefabs, other.ExcludedPrefabs) ||
                !SequencesEqual(IncludedOrigins, other.IncludedOrigins) ||
                !SequencesEqual(ExcludedOrigins, other.ExcludedOrigins) ||
                ExcludedCategories.Length != other.ExcludedCategories.Length)
                return false;

            for (var index = 0; index < ExcludedCategories.Length; index++)
                if (ExcludedCategories[index] != other.ExcludedCategories[index]) return false;
            return true;
        }

        public override bool Equals(object obj) => obj is SocketPolicyIntent other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = WeaponMaxSlots;
                hash = (hash * 397) ^ ArmorMaxSlots;
                hash = (hash * 397) ^ ShieldMaxSlots;
                hash = (hash * 397) ^ ToolMaxSlots;
                hash = (hash * 397) ^ UtilityMaxSlots;
                hash = (hash * 397) ^ ExplicitIncludeMaxSlots;
                hash = (hash * 397) ^ (int)IdentityComparison;
                AppendHash(ref hash, IncludedPrefabs);
                AppendHash(ref hash, ExcludedPrefabs);
                AppendHash(ref hash, IncludedOrigins);
                AppendHash(ref hash, ExcludedOrigins);
                for (var index = 0; index < ExcludedCategories.Length; index++)
                    hash = (hash * 397) ^ (int)ExcludedCategories[index];
                return hash;
            }
        }

        private static string[] Canonicalize(IReadOnlyList<string> values, StringComparer comparer)
        {
            var result = values
                .Select(value => comparer == StringComparer.OrdinalIgnoreCase ? value.ToUpperInvariant() : value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            return result;
        }

        private static bool SequencesEqual(string[] left, string[] right)
        {
            if (left.Length != right.Length) return false;
            for (var index = 0; index < left.Length; index++)
                if (!string.Equals(left[index], right[index], StringComparison.Ordinal)) return false;
            return true;
        }

        private static void AppendHash(ref int hash, string[] values)
        {
            for (var index = 0; index < values.Length; index++)
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(values[index]);
        }
    }
}
