using System;
using System.Collections.Generic;
using System.Linq;

namespace Magenheim.Core.Underworld;

public sealed record UnderworldDeepBoonSelectionState(string? SelectedDeepBoonId);

public enum UnderworldDeepBoonSelectionStatus
{
    Selected,
    Cleared,
    AlreadySelected,
    Locked,
    UnknownBoon,
}

public sealed record UnderworldDeepBoonSelectionResult(
    UnderworldDeepBoonSelectionStatus Status,
    UnderworldDeepBoonSelectionState State,
    string Diagnostic)
{
    public bool Changed => Status is UnderworldDeepBoonSelectionStatus.Selected or UnderworldDeepBoonSelectionStatus.Cleared;
}

/// <summary>
/// Pure per-player selection authority for Deep Boons. World progression decides which boons are
/// unlocked; this authority permits one independently selected Deep Boon without touching vanilla
/// Forsaken-power state. Runtime persistence and status-effect application remain adapters.
/// </summary>
public static class UnderworldDeepBoonSelection
{
    public static UnderworldDeepBoonSelectionState CreateInitialState() => new(null);

    public static UnderworldDeepBoonSelectionState Reconstruct(
        UnderworldDefinitionSet definitions,
        IEnumerable<UnderworldDeepstoneState> worldStates,
        string? selectedDeepBoonId)
    {
        if (definitions is null) throw new ArgumentNullException(nameof(definitions));
        var states = ValidateWorldStates(definitions, worldStates);
        if (string.IsNullOrWhiteSpace(selectedDeepBoonId)) return CreateInitialState();

        var canonical = definitions.Deepstones.Any(value => string.Equals(value.DeepBoonId, selectedDeepBoonId, StringComparison.Ordinal));
        if (!canonical) throw new InvalidOperationException($"Persisted Deep Boon selection '{selectedDeepBoonId}' is unknown to definition authority.");
        if (!states.Any(value => value.BoonUnlocked && string.Equals(value.DeepBoonId, selectedDeepBoonId, StringComparison.Ordinal)))
            throw new InvalidOperationException($"Persisted Deep Boon selection '{selectedDeepBoonId}' is not unlocked in authoritative world progression.");
        return new UnderworldDeepBoonSelectionState(selectedDeepBoonId);
    }

    public static UnderworldDeepBoonSelectionResult Select(
        UnderworldDefinitionSet definitions,
        IEnumerable<UnderworldDeepstoneState> worldStates,
        UnderworldDeepBoonSelectionState current,
        string deepBoonId)
    {
        if (definitions is null) throw new ArgumentNullException(nameof(definitions));
        if (current is null) throw new ArgumentNullException(nameof(current));
        if (string.IsNullOrWhiteSpace(deepBoonId)) throw new ArgumentException("Deep Boon id is required.", nameof(deepBoonId));
        var states = ValidateWorldStates(definitions, worldStates);
        if (!definitions.Deepstones.Any(value => string.Equals(value.DeepBoonId, deepBoonId, StringComparison.Ordinal)))
            return new UnderworldDeepBoonSelectionResult(UnderworldDeepBoonSelectionStatus.UnknownBoon, current, $"Unknown Deep Boon '{deepBoonId}'.");
        if (!states.Any(value => value.BoonUnlocked && string.Equals(value.DeepBoonId, deepBoonId, StringComparison.Ordinal)))
            return new UnderworldDeepBoonSelectionResult(UnderworldDeepBoonSelectionStatus.Locked, current, $"Deep Boon '{deepBoonId}' has not been unlocked in this world.");
        if (string.Equals(current.SelectedDeepBoonId, deepBoonId, StringComparison.Ordinal))
            return new UnderworldDeepBoonSelectionResult(UnderworldDeepBoonSelectionStatus.AlreadySelected, current, $"Deep Boon '{deepBoonId}' is already selected.");
        return new UnderworldDeepBoonSelectionResult(UnderworldDeepBoonSelectionStatus.Selected, new UnderworldDeepBoonSelectionState(deepBoonId), $"Selected Deep Boon '{deepBoonId}'.");
    }

    public static UnderworldDeepBoonSelectionResult Clear(UnderworldDeepBoonSelectionState current)
    {
        if (current is null) throw new ArgumentNullException(nameof(current));
        if (current.SelectedDeepBoonId is null)
            return new UnderworldDeepBoonSelectionResult(UnderworldDeepBoonSelectionStatus.AlreadySelected, current, "No Deep Boon is selected.");
        return new UnderworldDeepBoonSelectionResult(UnderworldDeepBoonSelectionStatus.Cleared, CreateInitialState(), "Cleared the selected Deep Boon.");
    }

    private static UnderworldDeepstoneState[] ValidateWorldStates(UnderworldDefinitionSet definitions, IEnumerable<UnderworldDeepstoneState> worldStates)
    {
        if (worldStates is null) throw new ArgumentNullException(nameof(worldStates));
        var states = worldStates.ToArray();
        var canonical = UnderworldDeepstoneProgression.CreateInitialState(definitions);
        if (states.Length != canonical.Count) throw new InvalidOperationException("Deep Boon selection requires the complete canonical Conclave state.");
        var supplied = states.ToDictionary(value => value.DeepstoneId, StringComparer.Ordinal);
        foreach (var expected in canonical)
        {
            if (!supplied.TryGetValue(expected.DeepstoneId, out var actual) || actual.BossId != expected.BossId || actual.TrophyPrefabName != expected.TrophyPrefabName || actual.DeepBoonId != expected.DeepBoonId)
                throw new InvalidOperationException($"Deep Boon selection world state does not match canonical Deepstone '{expected.DeepstoneId}'.");
            if (actual.TrophyMounted != actual.BoonUnlocked)
                throw new InvalidOperationException($"Deepstone '{actual.DeepstoneId}' has a partial activation and cannot authorize boon selection.");
        }
        return states;
    }
}
