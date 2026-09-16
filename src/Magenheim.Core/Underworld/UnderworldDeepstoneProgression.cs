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
