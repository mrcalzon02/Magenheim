using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Magenheim.Core.Worldgen;

public enum DuplicateRegistrationBehavior
{
    Skip = 0,
    Error = 1,
}

public enum RegistrationIdentityComparison
{
    Exact = 0,
    CaseInsensitive = 1,
}

public sealed record WorldgenCompatibilityPolicy(
    InvalidAreaBehavior InvalidAreaBehavior,
    DuplicateRegistrationBehavior DuplicateRegistrationBehavior,
    bool AdditiveOnly = true)
{
    public bool DetectPrefabCollisions { get; init; } = true;
    public RegistrationIdentityComparison IdentityComparison { get; init; } = RegistrationIdentityComparison.Exact;
    public IReadOnlyCollection<string> ExcludedRegistrationKeys { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> ExcludedPrefabNames { get; init; } = Array.Empty<string>();

    public static WorldgenCompatibilityPolicy Conservative { get; } = new(
        InvalidAreaBehavior.Reject,
        DuplicateRegistrationBehavior.Skip,
        AdditiveOnly: true);
}

public sealed record DesiredWorldgenAddition(
    string RegistrationKey,
    string PrefabName,
    SpawnArea Area);

public sealed record ObservedWorldgenRegistration(
    string RegistrationKey,
    string Owner,
    string PrefabName);

public enum WorldgenPlanAction
{
    Add = 0,
    Skip = 1,
    Error = 2,
}

public sealed record WorldgenPlanEntry(
    DesiredWorldgenAddition Desired,
    WorldgenPlanAction Action,
    SpawnArea NormalizedArea,
    string Diagnostic);

public sealed class WorldgenAdditionPlan
{
    public WorldgenAdditionPlan(IEnumerable<WorldgenPlanEntry> entries)
    {
        Entries = new ReadOnlyCollection<WorldgenPlanEntry>(entries.ToArray());
    }

    public IReadOnlyList<WorldgenPlanEntry> Entries { get; }
    public bool HasErrors => Entries.Any(entry => entry.Action == WorldgenPlanAction.Error);
    public IEnumerable<WorldgenPlanEntry> Additions =>
        Entries.Where(entry => entry.Action == WorldgenPlanAction.Add);
}

public static class WorldgenAdditionPlanner
{
    private const string OwnedPrefix = "magenheim.";

    public static WorldgenAdditionPlan Build(
        IEnumerable<DesiredWorldgenAddition> desired,
        IEnumerable<ObservedWorldgenRegistration> observed,
        WorldgenCompatibilityPolicy? policy = null)
    {
        if (desired is null) throw new ArgumentNullException(nameof(desired));
        if (observed is null) throw new ArgumentNullException(nameof(observed));

        policy ??= WorldgenCompatibilityPolicy.Conservative;
        ValidatePolicy(policy);

        var comparer = policy.IdentityComparison == RegistrationIdentityComparison.CaseInsensitive
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        var observedSnapshot = observed.Where(item => item is not null).ToArray();
        var occupiedKeys = new HashSet<string>(comparer);
        var occupiedPrefabs = new HashSet<string>(comparer);

        foreach (var item in observedSnapshot)
        {
            if (!string.IsNullOrWhiteSpace(item.RegistrationKey))
                occupiedKeys.Add(item.RegistrationKey.Trim());

            if (policy.DetectPrefabCollisions && !string.IsNullOrWhiteSpace(item.PrefabName))
                occupiedPrefabs.Add(item.PrefabName.Trim());
        }

        var excludedKeys = new HashSet<string>(
            (policy.ExcludedRegistrationKeys ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim()),
            comparer);

        var excludedPrefabs = new HashSet<string>(
            (policy.ExcludedPrefabNames ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim()),
            comparer);

        var plannedKeys = new HashSet<string>(comparer);
        var plannedPrefabs = new HashSet<string>(comparer);
        var results = new List<WorldgenPlanEntry>();

        foreach (var addition in desired.ToArray())
        {
            if (addition is null)
                continue;

            if (string.IsNullOrWhiteSpace(addition.RegistrationKey) ||
                !addition.RegistrationKey.StartsWith(OwnedPrefix, StringComparison.Ordinal))
            {
                results.Add(new WorldgenPlanEntry(
                    addition,
                    WorldgenPlanAction.Error,
                    SpawnArea.None,
                    "Registration key must be namespaced with 'magenheim.'."));
                continue;
            }

            if (!string.Equals(addition.RegistrationKey, addition.RegistrationKey.Trim(), StringComparison.Ordinal))
            {
                results.Add(new WorldgenPlanEntry(
                    addition,
                    WorldgenPlanAction.Error,
                    SpawnArea.None,
                    "Registration key cannot contain leading or trailing whitespace."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(addition.PrefabName))
            {
                results.Add(new WorldgenPlanEntry(
                    addition,
                    WorldgenPlanAction.Error,
                    SpawnArea.None,
                    "Prefab name cannot be empty."));
                continue;
            }

            if (!string.Equals(addition.PrefabName, addition.PrefabName.Trim(), StringComparison.Ordinal))
            {
                results.Add(new WorldgenPlanEntry(
                    addition,
                    WorldgenPlanAction.Error,
                    SpawnArea.None,
                    "Prefab name cannot contain leading or trailing whitespace."));
                continue;
            }

            var area = SpawnAreaValidator.Normalize(addition.Area, policy.InvalidAreaBehavior);
            if (!area.IsValid)
            {
                results.Add(new WorldgenPlanEntry(
                    addition,
                    WorldgenPlanAction.Error,
                    SpawnArea.None,
                    area.Diagnostic));
                continue;
            }

            if (excludedKeys.Contains(addition.RegistrationKey) || excludedPrefabs.Contains(addition.PrefabName))
            {
                results.Add(new WorldgenPlanEntry(
                    addition,
                    WorldgenPlanAction.Skip,
                    area.Area,
                    "Addition was disabled by compatibility configuration; no host registration was modified."));
                continue;
            }

            var keyCollision = occupiedKeys.Contains(addition.RegistrationKey) ||
                               plannedKeys.Contains(addition.RegistrationKey);

            var prefabCollision = policy.DetectPrefabCollisions &&
                                  (occupiedPrefabs.Contains(addition.PrefabName) ||
                                   plannedPrefabs.Contains(addition.PrefabName));

            if (keyCollision || prefabCollision)
            {
                var action = policy.DuplicateRegistrationBehavior == DuplicateRegistrationBehavior.Skip
                    ? WorldgenPlanAction.Skip
                    : WorldgenPlanAction.Error;

                var collisionKind = keyCollision && prefabCollision
                    ? "registration key and prefab"
                    : keyCollision
                        ? "registration key"
                        : "prefab";

                results.Add(new WorldgenPlanEntry(
                    addition,
                    action,
                    area.Area,
                    $"The {collisionKind} for '{addition.RegistrationKey}' / '{addition.PrefabName}' is already occupied; existing data was not modified."));
                continue;
            }

            plannedKeys.Add(addition.RegistrationKey);
            if (policy.DetectPrefabCollisions)
                plannedPrefabs.Add(addition.PrefabName);

            results.Add(new WorldgenPlanEntry(
                addition,
                WorldgenPlanAction.Add,
                area.Area,
                area.Diagnostic));
        }

        return new WorldgenAdditionPlan(results);
    }

    private static void ValidatePolicy(WorldgenCompatibilityPolicy policy)
    {
        if (!policy.AdditiveOnly)
            throw new InvalidOperationException("Magenheim compatibility policy cannot disable additive-only worldgen behavior.");
        if (!Enum.IsDefined(typeof(InvalidAreaBehavior), policy.InvalidAreaBehavior))
            throw new InvalidOperationException($"Unknown invalid-area behavior '{policy.InvalidAreaBehavior}'.");
        if (!Enum.IsDefined(typeof(DuplicateRegistrationBehavior), policy.DuplicateRegistrationBehavior))
            throw new InvalidOperationException($"Unknown duplicate-registration behavior '{policy.DuplicateRegistrationBehavior}'.");
        if (!Enum.IsDefined(typeof(RegistrationIdentityComparison), policy.IdentityComparison))
            throw new InvalidOperationException($"Unknown registration identity comparison '{policy.IdentityComparison}'.");
    }
}
