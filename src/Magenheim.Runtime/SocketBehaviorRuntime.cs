using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Magenheim.Core.Socketing;
using Magenheim.Runtime.Compatibility;

namespace Magenheim.Runtime;

/// <summary>
/// Provider-aware runtime boundary for behavioral socket effects. It resolves the equipped
/// item's physical socket state once and condenses it into at most one activation per element.
/// Hit/proc processors consume these activations rather than iterating individual sockets, so
/// repeated same-element crystals strengthen one behavior instead of multiplying proc count.
/// </summary>
internal static class SocketBehaviorRuntime
{
    private static ManualLogSource? _log;
    private static EquipmentSocketBackend _backend = EquipmentSocketBackend.Magenheim;
    private static IReadOnlyList<float> _resonanceMultipliers = new[] { 1f };

    internal static void Configure(
        ManualLogSource log,
        EquipmentSocketBackend backend,
        IReadOnlyList<float> resonanceMultipliers)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _backend = backend;
        _resonanceMultipliers = resonanceMultipliers ?? throw new ArgumentNullException(nameof(resonanceMultipliers));
    }

    internal static bool TryPlan(ItemDrop.ItemData item, out IReadOnlyList<BehavioralResonanceActivation> activations)
    {
        activations = Array.Empty<BehavioralResonanceActivation>();
        if (item is null)
            return false;

        IReadOnlyList<Crystal> crystals;
        if (_backend == EquipmentSocketBackend.Jewelcrafting)
        {
            if (_log is null || !JewelcraftingSocketReader.TryReadMagenheimCrystals(item, _log, out var externalCrystals))
                return false;
            crystals = externalCrystals;
        }
        else
        {
            if (!ItemSocketAdapter.TryRead(item, out var state, out var diagnostic))
            {
                _log?.LogWarning($"Ignoring malformed Magenheim behavioral socket metadata on '{ItemIdentity(item)}': {diagnostic}");
                return false;
            }
            crystals = state.InstalledCrystals;
        }

        if (crystals.Count == 0)
            return false;

        activations = BehavioralResonance.Plan(crystals, _resonanceMultipliers);
        return activations.Count != 0;
    }

    internal static bool TryResolveStorm(ItemDrop.ItemData item, out BehavioralResonanceActivation activation)
    {
        activation = null!;
        if (!TryPlan(item, out var activations))
            return false;

        for (var index = 0; index < activations.Count; index++)
        {
            if (activations[index].Element != ElementalAlignment.Storm)
                continue;
            activation = activations[index];
            return true;
        }

        return false;
    }

    private static string ItemIdentity(ItemDrop.ItemData item) =>
        item.m_dropPrefab ? item.m_dropPrefab.name : item.m_shared.m_name;
}
