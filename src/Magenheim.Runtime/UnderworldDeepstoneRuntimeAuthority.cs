using System;
using System.Collections.Generic;
using System.Linq;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>
/// Single runtime bridge between the validated Underworld definition authority and the six
/// physical Conclave stones. This layer reads durable world facts and asks Core to reconstruct
/// and validate progression; it does not duplicate trophy, prerequisite, or boon rules.
/// </summary>
internal static class UnderworldDeepstoneRuntimeAuthority
{
    private static UnderworldDefinitionSet? _definitions;

    internal static bool IsConfigured => _definitions is not null;

    internal static void Configure(UnderworldDefinitionSet definitions)
    {
        _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        if (_definitions.Deepstones.Count != 6)
            throw new InvalidOperationException($"The Deepstone Conclave requires exactly six canonical stones; definitions contain {_definitions.Deepstones.Count}.");
    }

    internal static IReadOnlyList<UnderworldDeepstoneState> ReconstructWorldState()
    {
        var definitions = RequireDefinitions();
        var facts = definitions.Deepstones.Select(stone =>
        {
            var persisted = UnderworldDeepstoneRuntime.ReadPersistentState(stone.Id);
            return new UnderworldDeepstonePersistenceFact(stone.Id, persisted.TrophyMounted, persisted.BoonUnlocked);
        });
        return UnderworldDeepstoneProgression.ReconstructPersistentState(definitions, facts);
    }

    internal static UnderworldDeepstoneMountTransactionPlan PlanMount(
        string deepstoneId,
        string trophyPrefabName,
        int authoritativeInventoryCount)
    {
        return UnderworldDeepstoneProgression.PlanMountTrophy(
            RequireDefinitions(),
            ReconstructWorldState(),
            deepstoneId,
            trophyPrefabName,
            authoritativeInventoryCount);
    }

    internal static UnderworldBossDefinition BossForStone(string deepstoneId)
    {
        if (string.IsNullOrWhiteSpace(deepstoneId))
            throw new ArgumentException("Deepstone id is required.", nameof(deepstoneId));
        return RequireDefinitions().Bosses.FirstOrDefault(value =>
                   string.Equals(value.DeepstoneSlotId, deepstoneId, StringComparison.Ordinal))
               ?? throw new InvalidOperationException($"Unknown canonical Deepstone '{deepstoneId}'.");
    }

    private static UnderworldDefinitionSet RequireDefinitions() =>
        _definitions ?? throw new InvalidOperationException("Underworld Deepstone runtime authority has not been configured from the validated Magenheim definition set.");
}
