using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx.Logging;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>
/// Runtime projection of durable Deep Boon selection. This is deliberately separate from vanilla
/// Forsaken powers: gameplay patches ask this authority which single Magenheim boon is active.
/// Durable selection remains the source of truth and is revalidated through Core before admission.
/// </summary>
internal static class DeepBoonRuntime
{
    private static readonly Dictionary<long, string> ActiveByPlayer = new();
    private static UnderworldRuntimeServices? _services;
    private static ManualLogSource? _log;

    internal static void Configure(UnderworldRuntimeServices services, ManualLogSource log)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        ActiveByPlayer.Clear();
    }

    internal static bool IsActive(Player player, string deepBoonId)
    {
        if (player is null || string.IsNullOrWhiteSpace(deepBoonId)) return false;
        return ActiveByPlayer.TryGetValue(player.GetPlayerID(), out var active) && string.Equals(active, deepBoonId, StringComparison.Ordinal);
    }

    internal static string? GetActive(Player player)
    {
        if (player is null) return null;
        return ActiveByPlayer.TryGetValue(player.GetPlayerID(), out var active) ? active : null;
    }

    internal static bool Reconcile(Player player, out string diagnostic)
    {
        if (player is null) { diagnostic = "Deep Boon runtime reconciliation requires a player."; return false; }
        if (_services is null || ZNet.instance is null || !ZNet.instance.IsServer()) { diagnostic = "Deep Boon runtime reconciliation requires server authority."; return false; }
        var world = ZNet.World;
        // Split the null check out: short-circuiting past TryResolveWorldSession would leave
        // its out parameters, including diagnostic, definitely unassigned.
        if (world is null)
        {
            diagnostic = "Deep Boon runtime reconciliation requires a loaded world.";
            Remove(player);
            return false;
        }
        if (!UnderworldRuntimeIdentityResolver.TryResolveWorldSession(_services.SpatialDomain, ZNet.instance, world, out var identity, out var layer, out diagnostic) || identity is null)
        {
            Remove(player);
            return false;
        }
        if (layer != UnderworldLayer.Underworld)
        {
            Remove(player);
            diagnostic = "Deep Boon runtime projection is inactive outside the paired Underworld.";
            return true;
        }

        var playerId = player.GetPlayerID().ToString(CultureInfo.InvariantCulture);
        string? persistedId = null;
        if (_services.DeepBoonSelectionStore.TryLoad(playerId, identity, out var persisted, out var loadDiagnostic)) persistedId = persisted.SelectedDeepBoonId;
        else if (loadDiagnostic.IndexOf("candidate missing", StringComparison.Ordinal) < 0) _log?.LogWarning($"Deep Boon runtime could not recover selection for player {playerId}: {loadDiagnostic}");

        UnderworldDeepBoonSelectionState state;
        try { state = UnderworldDeepstoneRuntimeAuthority.ReconstructDeepBoonSelection(persistedId); }
        catch (Exception exception)
        {
            Remove(player);
            diagnostic = "Deep Boon runtime rejected persisted selection against current Conclave authority: " + exception.Message;
            return false;
        }

        if (string.IsNullOrWhiteSpace(state.SelectedDeepBoonId))
        {
            Remove(player);
            diagnostic = "No Deep Boon is selected.";
            return true;
        }

        // Guarded by the IsNullOrWhiteSpace return above, which does not narrow null-state
        // against these reference assemblies.
        ActiveByPlayer[player.GetPlayerID()] = state.SelectedDeepBoonId!;
        diagnostic = $"Deep Boon '{state.SelectedDeepBoonId}' is the player's sole active Magenheim boon.";
        return true;
    }

    internal static void Remove(Player player)
    {
        if (player is not null) ActiveByPlayer.Remove(player.GetPlayerID());
    }

    internal static void Reset()
    {
        ActiveByPlayer.Clear();
    }
}
