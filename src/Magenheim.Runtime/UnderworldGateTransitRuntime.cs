using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Thin Deep Gate transport adapter. Valheim owns player movement; Magenheim supplies the
/// engine-backed destination for instance index 1. A player's layer is derived from the disjoint
/// engine-space instance it physically occupies; it is never stored as global multiplayer state.
/// Return anchors are ephemeral transition safety only and are never persistence/world authority.
/// </summary>
internal static class UnderworldGateTransitRuntime
{
    private static readonly Dictionary<long, SurfaceAnchor> SurfaceReturn = new();
    private static UnderworldRuntimeServices? _services;
    private static ManualLogSource? _log;

    internal static void Configure(UnderworldRuntimeServices services, ManualLogSource log)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    internal static void Reset()
    {
        SurfaceReturn.Clear();
        _services = null;
        _log = null;
    }

    /// <param name="ignoreProgression">Developer console only (UnderworldDevCommands); the gate never sets it.</param>
    internal static bool TryTransit(UnderworldGateRole role, Humanoid humanoid, out string diagnostic, bool ignoreProgression = false)
    {
        diagnostic = string.Empty;
        if (humanoid is not Player player)
        {
            diagnostic = "Only players can traverse the Deep Gate.";
            return false;
        }

        var services = _services;
        var identity = services?.InstanceLifecycle.Identity;
        if (services is null || identity is null ||
            services.InstanceLifecycle.Phase != UnderworldInstancePhase.Active)
        {
            diagnostic = "The paired Underworld instance is not active.";
            return false;
        }

        // Gate role is physical authority. Entry endpoints belong to the Surface and return
        // endpoints belong to the dedicated Underworld engine layer. Never allow a misplaced,
        // duplicated, or stale endpoint to become a cross-layer transit oracle.
        var isUnderworld = UnderworldInstanceLayer.IsUnderworldEnginePosition(player.transform.position);
        if (role == UnderworldGateRole.EnterUnderworld && isUnderworld)
        {
            diagnostic = "This Deep Gate entry can only be used from the Surface.";
            return false;
        }
        if (role == UnderworldGateRole.ReturnToSurface && !isUnderworld)
        {
            diagnostic = "The return Deep Gate can only be used from the Underworld.";
            return false;
        }

        var playerId = player.GetPlayerID();
        if (role == UnderworldGateRole.EnterUnderworld)
        {
            if (!ignoreProgression && !UnderworldProgressionAuthority.IsUnlocked)
            {
                diagnostic = "The Deep Gate has not been unlocked.";
                return false;
            }

            SurfaceReturn[playerId] = new SurfaceAnchor(
                player.transform.position,
                player.transform.rotation);

            var target = UnderworldWorldCenterRegistrar.ResolveEngineCenterPosition();
            if (player.TeleportTo(target + Vector3.up * 1.2f, Quaternion.identity, false))
            {
                _log?.LogInfo(
                    $"Deep Gate moved player {playerId} into Underworld instance {identity.DerivedWorldId}.");
                return true;
            }

            SurfaceReturn.Remove(playerId);
            diagnostic = "Valheim rejected the Underworld teleport.";
            return false;
        }

        if (!SurfaceReturn.TryGetValue(playerId, out var source))
        {
            diagnostic = "No Surface return anchor exists for this gate transit.";
            return false;
        }

        if (player.TeleportTo(source.Position + Vector3.up * 0.25f, source.Rotation, false))
        {
            SurfaceReturn.Remove(playerId);
            _log?.LogInfo($"Deep Gate returned player {playerId} to the Surface.");
            return true;
        }

        diagnostic = "Valheim rejected the Surface return teleport.";
        return false;
    }

    internal static bool HasReturnAnchor(Player player) => SurfaceReturn.ContainsKey(player.GetPlayerID());

    /// <summary>
    /// Surface return without an anchor, for the developer console after a relog below ground.
    /// The destination itself restores Surface classification; no global layer state is mutated.
    /// </summary>
    internal static bool TryReturnTo(Player player, Vector3 position, out string diagnostic)
    {
        diagnostic = string.Empty;
        var services = _services;
        var identity = services?.InstanceLifecycle.Identity;
        if (services is null || identity is null || services.InstanceLifecycle.Phase != UnderworldInstancePhase.Active)
        {
            diagnostic = "The paired Underworld instance is not active.";
            return false;
        }
        if (player.TeleportTo(position + Vector3.up * 0.5f, player.transform.rotation, true))
        {
            SurfaceReturn.Remove(player.GetPlayerID());
            _log?.LogInfo($"Developer console returned player {player.GetPlayerID()} to the Surface without an anchor.");
            return true;
        }
        diagnostic = "Valheim rejected the Surface teleport.";
        return false;
    }

    internal static string Describe(Player player)
    {
        var services = _services;
        var phase = services?.InstanceLifecycle.Phase.ToString() ?? "not configured";
        var layer = UnderworldInstanceLayer.IsUnderworldEnginePosition(player.transform.position) ? "Underworld" : "Surface";
        return $"Underworld instance: {phase}; local layer: {layer}; Deep Gate unlocked: {UnderworldProgressionAuthority.IsUnlocked}; " +
               $"return anchor: {(HasReturnAnchor(player) ? "yes" : "no")}.";
    }

    private readonly record struct SurfaceAnchor(Vector3 Position, Quaternion Rotation);
}
