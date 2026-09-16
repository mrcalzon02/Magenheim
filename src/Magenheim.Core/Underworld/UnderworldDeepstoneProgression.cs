using System;
using System.Collections.Generic;
using System.Linq;

namespace Magenheim.Core.Underworld;

public sealed record UnderworldDeepstoneState(
    string DeepstoneId,
    string BossId,
    string TrophyPrefabName,
    string DeepBoonId,
    bool TrophyMounted,
    bool BoonUnlocked);

public sealed record UnderworldDeepstonePersistenceFact(
    string DeepstoneId,
    bool TrophyMounted,
    bool BoonUnlocked);

public enum UnderworldDeepstoneMountStatus
{
    Mounted,
    AlreadyMounted,
    WrongTrophy,
    PrerequisiteMissing,
    UnknownDeepstone,
}

public sealed record UnderworldDeepstoneMountResult(
    UnderworldDeepstoneMountStatus Status,
    UnderworldDeepstoneState? State,
    string Diagnostic)
{
    public bool Changed => Status == UnderworldDeepstoneMountStatus.Mounted;
}

public enum UnderworldDeepstoneTransactionStatus
{
    Ready,
    MissingTrophy,
    AlreadyMounted,
    WrongTrophy,
    PrerequisiteMissing,
    UnknownDeepstone,
}

/// <summary>
/// Immutable server transaction plan. Runtime may consume the named trophy only when Ready is
/// true, then persist ResultingState. Keeping consumption and state mutation in one plan prevents
/// a client/runtime adapter from granting a boon without paying the trophy or consuming a trophy
/// after progression was rejected.
/// </summary>
public sealed record UnderworldDeepstoneMountTransactionPlan(
    UnderworldDeepstoneTransactionStatus Status,
    string DeepstoneId,
    string TrophyPrefabName,
    int TrophyConsumeCount,
    UnderworldDeepstoneState? ResultingState,
    string Diagnostic)
{
    public bool Ready => Status == UnderworldDeepstoneTransactionStatus.Ready;
}

/// <summary>
/// Pure progression authority for the Deepstone Conclave. Runtime code supplies durable
/// mounted-state facts and inventory operations; this authority decides whether a trophy may
/// activate a stone and which Deep Boon becomes available. No Unity/Valheim state lives here.
/// </summary>
public static class UnderworldDeepstoneProgression
{
    public static IReadOnlyList<UnderworldDeepstoneState> CreateInitialState(UnderworldDefinitionSet definitions)
    {
        if (definitions is null) throw new ArgumentNullException(nameof(definitions));
        var bosses = definitions.Bosses.ToDictionary(value => value.Id, StringComparer.Ordinal);
        return Array.AsReadOnly(definitions.Deepstones
            .OrderBy(value => value.Id, StringComparer.Ordinal)
            .Select(stone =>
            {
                var boss = bosses[stone.BossId];
                return new UnderworldDeepstoneState(
                    stone.Id, boss.Id, boss.TrophyPrefabName, stone.DeepBoonId, false, false);
            })
            .ToArray());
    }

    public static IReadOnlyList<UnderworldDeepstoneState> ReconstructPersistentState(
        UnderworldDefinitionSet definitions,
        IEnumerable<UnderworldDeepstonePersistenceFact> persistedFacts)
    {
        if (definitions is null) throw new ArgumentNullException(nameof(definitions));
        if (persistedFacts is null) throw new ArgumentNullException(nameof(persistedFacts));

        var facts = persistedFacts.ToArray();
        if (facts.Any(value => string.IsNullOrWhiteSpace(value.DeepstoneId)))
            throw new InvalidOperationException("Persisted Deepstone state contains an empty stone identity.");
        var duplicate = facts.GroupBy(value => value.DeepstoneId, StringComparer.Ordinal).FirstOrDefault(group => group.Count() != 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"Persisted Deepstone state contains duplicate stone '{duplicate.Key}'.");

        var initial = CreateInitialState(definitions);
        var canonicalIds = new HashSet<string>(initial.Select(value => value.DeepstoneId), StringComparer.Ordinal);
        var unknown = facts.FirstOrDefault(value => !canonicalIds.Contains(value.DeepstoneId));
        if (unknown is not null)
            throw new InvalidOperationException($"Persisted Deepstone state contains unknown stone '{unknown.DeepstoneId}'.");
        if (facts.Length != initial.Count)
            throw new InvalidOperationException($"Persisted Deepstone state must contain exactly {initial.Count} canonical stones; found {facts.Length}.");

        var factsById = facts.ToDictionary(value => value.DeepstoneId, StringComparer.Ordinal);
        var reconstructed = initial.Select(state =>
        {
            var fact = factsById[state.DeepstoneId];
            if (fact.TrophyMounted != fact.BoonUnlocked)
                throw new InvalidOperationException($"Deepstone '{state.DeepstoneId}' has a partial persisted activation; trophy and boon must be atomic.");
            return state with { TrophyMounted = fact.TrophyMounted, BoonUnlocked = fact.BoonUnlocked };
        }).ToArray();

        var statesById = reconstructed.ToDictionary(value => value.DeepstoneId, StringComparer.Ordinal);
        foreach (var state in reconstructed.Where(value => value.TrophyMounted))
        {
            var boss = definitions.Bosses.First(value => string.Equals(value.Id, state.BossId, StringComparison.Ordinal));
            foreach (var prerequisiteBossId in boss.PrerequisiteBossIds)
            {
                var prerequisite = definitions.Bosses.First(value => string.Equals(value.Id, prerequisiteBossId, StringComparison.Ordinal));
                if (!statesById[prerequisite.DeepstoneSlotId].TrophyMounted)
                    throw new InvalidOperationException($"Persisted Deepstone '{state.DeepstoneId}' is active without prerequisite '{prerequisite.DeepstoneSlotId}'.");
            }
        }

        return Array.AsReadOnly(reconstructed);
    }

    public static UnderworldDeepstoneMountTransactionPlan PlanMountTrophy(
        UnderworldDefinitionSet definitions,
        IEnumerable<UnderworldDeepstoneState> currentStates,
        string deepstoneId,
        string trophyPrefabName,
        int availableTrophyCount)
    {
        if (availableTrophyCount < 0) throw new ArgumentOutOfRangeException(nameof(availableTrophyCount));
        var result = TryMountTrophy(definitions, currentStates, deepstoneId, trophyPrefabName);
        if (!result.Changed)
        {
            var status = result.Status switch
            {
                UnderworldDeepstoneMountStatus.AlreadyMounted => UnderworldDeepstoneTransactionStatus.AlreadyMounted,
                UnderworldDeepstoneMountStatus.WrongTrophy => UnderworldDeepstoneTransactionStatus.WrongTrophy,
                UnderworldDeepstoneMountStatus.PrerequisiteMissing => UnderworldDeepstoneTransactionStatus.PrerequisiteMissing,
                UnderworldDeepstoneMountStatus.UnknownDeepstone => UnderworldDeepstoneTransactionStatus.UnknownDeepstone,
                _ => throw new InvalidOperationException($"Unhandled Deepstone mount status '{result.Status}'."),
            };
            return new UnderworldDeepstoneMountTransactionPlan(status, deepstoneId, trophyPrefabName, 0, result.State, result.Diagnostic);
        }

        if (availableTrophyCount < 1)
            return new UnderworldDeepstoneMountTransactionPlan(
                UnderworldDeepstoneTransactionStatus.MissingTrophy,
                deepstoneId,
                trophyPrefabName,
                0,
                result.State,
                $"Deepstone '{deepstoneId}' requires one '{trophyPrefabName}' in the authoritative inventory.");

        return new UnderworldDeepstoneMountTransactionPlan(
            UnderworldDeepstoneTransactionStatus.Ready,
            deepstoneId,
            trophyPrefabName,
            1,
            result.State,
            result.Diagnostic);
    }

    public static UnderworldDeepstoneMountResult TryMountTrophy(
        UnderworldDefinitionSet definitions,
        IEnumerable<UnderworldDeepstoneState> currentStates,
        string deepstoneId,
        string trophyPrefabName)
    {
        if (definitions is null) throw new ArgumentNullException(nameof(definitions));
        if (currentStates is null) throw new ArgumentNullException(nameof(currentStates));
        if (string.IsNullOrWhiteSpace(deepstoneId)) throw new ArgumentException("Deepstone id is required.", nameof(deepstoneId));
        if (string.IsNullOrWhiteSpace(trophyPrefabName)) throw new ArgumentException("Trophy prefab name is required.", nameof(trophyPrefabName));

        var boss = definitions.Bosses.FirstOrDefault(value => string.Equals(value.DeepstoneSlotId, deepstoneId, StringComparison.Ordinal));
        if (boss is null)
            return new UnderworldDeepstoneMountResult(UnderworldDeepstoneMountStatus.UnknownDeepstone, null, $"Unknown Deepstone '{deepstoneId}'.");

        var states = currentStates.ToDictionary(value => value.DeepstoneId, StringComparer.Ordinal);
        if (!states.TryGetValue(deepstoneId, out var state))
            throw new InvalidOperationException($"Current Deepstone state is missing canonical stone '{deepstoneId}'.");
        if (!string.Equals(state.BossId, boss.Id, StringComparison.Ordinal) ||
            !string.Equals(state.TrophyPrefabName, boss.TrophyPrefabName, StringComparison.Ordinal) ||
            !string.Equals(state.DeepBoonId, boss.DeepBoonId, StringComparison.Ordinal))
            throw new InvalidOperationException($"Current Deepstone state for '{deepstoneId}' does not match definition authority.");

        if (state.TrophyMounted)
            return new UnderworldDeepstoneMountResult(UnderworldDeepstoneMountStatus.AlreadyMounted, state, "The Deepstone trophy is already mounted.");
        if (!string.Equals(trophyPrefabName, boss.TrophyPrefabName, StringComparison.Ordinal))
            return new UnderworldDeepstoneMountResult(UnderworldDeepstoneMountStatus.WrongTrophy, state, $"Deepstone '{deepstoneId}' requires trophy '{boss.TrophyPrefabName}'.");

        foreach (var prerequisiteBossId in boss.PrerequisiteBossIds)
        {
            var prerequisite = definitions.Bosses.First(value => string.Equals(value.Id, prerequisiteBossId, StringComparison.Ordinal));
            if (!states.TryGetValue(prerequisite.DeepstoneSlotId, out var prerequisiteState) || !prerequisiteState.TrophyMounted)
                return new UnderworldDeepstoneMountResult(UnderworldDeepstoneMountStatus.PrerequisiteMissing, state, $"Deepstone '{deepstoneId}' requires completed Deepstone '{prerequisite.DeepstoneSlotId}'.");
        }

        var activated = state with { TrophyMounted = true, BoonUnlocked = true };
        return new UnderworldDeepstoneMountResult(UnderworldDeepstoneMountStatus.Mounted, activated, $"Mounted '{boss.TrophyPrefabName}' and unlocked '{boss.DeepBoonId}'.");
    }
}
