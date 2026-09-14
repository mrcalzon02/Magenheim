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

public sealed record WorldgenCompatibilityPolicy(
    InvalidAreaBehavior InvalidAreaBehavior,
    DuplicateRegistrationBehavior DuplicateRegistrationBehavior,
    bool AdditiveOnly = true)
{
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
        if (!policy.AdditiveOnly)
        {
            throw new InvalidOperationException(
                "Magenheim compatibility policy cannot disable additive-only worldgen behavior.");
        }

        var observedSnapshot = observed.ToArray();
        var occupiedKeys = new HashSet<string>(
            observedSnapshot.Select(item => item.RegistrationKey),
            StringComparer.Ordinal);

        var plannedKeys = new HashSet<string>(StringComparer.Ordinal);
        var results = new List<WorldgenPlanEntry>();

        foreach (var addition in desired.ToArray())
        {
            if (addition is null)
            {
                continue;
            }

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

            if (string.IsNullOrWhiteSpace(addition.PrefabName))
            {
                results.Add(new WorldgenPlanEntry(
                    addition,
                    WorldgenPlanAction.Error,
                    SpawnArea.None,
                    "Prefab name cannot be empty."));
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

            var duplicate = occupiedKeys.Contains(addition.RegistrationKey) ||
                            !plannedKeys.Add(addition.RegistrationKey);

            if (duplicate)
            {
                var action = policy.DuplicateRegistrationBehavior == DuplicateRegistrationBehavior.Skip
                    ? WorldgenPlanAction.Skip
                    : WorldgenPlanAction.Error;

                results.Add(new WorldgenPlanEntry(
                    addition,
                    action,
                    area.Area,
                    $"Registration key '{addition.RegistrationKey}' is already occupied; foreign/previous data was not modified."));
                continue;
            }

            results.Add(new WorldgenPlanEntry(
                addition,
                WorldgenPlanAction.Add,
                area.Area,
                area.Diagnostic));
        }

        return new WorldgenAdditionPlan(results);
    }
}
