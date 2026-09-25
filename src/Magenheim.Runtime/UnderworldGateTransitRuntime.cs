using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx.Logging;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Thin Deep Gate transport adapter. Valheim owns player movement; Magenheim supplies the
/// engine-backed destination for instance index 1. A player's layer is derived from the disjoint
/// engine-space instance it physically occupies; it is never stored as global multiplayer state.
/// Return anchors are transition safety only and are never population/layer authority. The last
/// legitimate Surface gate origin is also copied to the player's durable ZDO so a relog while in
/// the Underworld cannot strand that player merely because the process-local cache was rebuilt.
/// </summary>
internal static class UnderworldGateTransitRuntime
{
    private const string SurfaceReturnKey = "magenheim.underworld.surface_return.v1";
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

            Vector3 target;
            try { target = UnderworldWorldCenterRegistrar.ResolveEngineCenterPosition(); }
            catch (InvalidOperationException exception) { diagnostic = exception.Message; return false; }
            var source = new SurfaceAnchor(player.transform.position, player.transform.rotation);
            SurfaceReturn[playerId] = source;
            PersistReturnAnchor(player, source);

            if (player.TeleportTo(target + Vector3.up * 1.2f, UnderworldWorldCenterRegistrar.ArrivalRotation, false))
            {
                _log?.LogInfo(
                    $"Deep Gate moved player {playerId} into Underworld instance {identity.DerivedWorldId}.");
                return true;
            }

            SurfaceReturn.Remove(playerId);
            ClearPersistedReturnAnchor(player);
            diagnostic = "Valheim rejected the Underworld teleport.";
            return false;
        }

        if (!TryResolveReturnAnchor(player, out var returnAnchor))
        {
            diagnostic = "No valid Surface Deep Gate return anchor exists for this transit.";
            return false;
        }

        if (player.TeleportTo(returnAnchor.Position + Vector3.up * 0.25f, returnAnchor.Rotation, false))
        {
            SurfaceReturn.Remove(playerId);
            ClearPersistedReturnAnchor(player);
            _log?.LogInfo($"Deep Gate returned player {playerId} to the Surface.");
            return true;
        }

        diagnostic = "Valheim rejected the Surface return teleport.";
        return false;
    }

    internal static bool HasReturnAnchor(Player player) =>
        SurfaceReturn.ContainsKey(player.GetPlayerID()) || TryReadPersistedReturnAnchor(player, out _);

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
            ClearPersistedReturnAnchor(player);
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

    private static bool TryResolveReturnAnchor(Player player, out SurfaceAnchor anchor)
    {
        if (SurfaceReturn.TryGetValue(player.GetPlayerID(), out anchor)) return true;
        if (!TryReadPersistedReturnAnchor(player, out anchor)) return false;

        // Rehydrate only the requesting player's transition cache. This is not a player registry:
        // the physical engine layer remains authoritative for where that player actually is.
        SurfaceReturn[player.GetPlayerID()] = anchor;
        return true;
    }

    private static void PersistReturnAnchor(Player player, SurfaceAnchor anchor)
    {
        var view = player.GetComponent<ZNetView>();
        if (view == null || !view.IsValid()) return;

        var p = anchor.Position;
        var r = anchor.Rotation;
        var payload = string.Join("|", new[]
        {
            p.x.ToString("R", CultureInfo.InvariantCulture),
            p.y.ToString("R", CultureInfo.InvariantCulture),
            p.z.ToString("R", CultureInfo.InvariantCulture),
            r.x.ToString("R", CultureInfo.InvariantCulture),
            r.y.ToString("R", CultureInfo.InvariantCulture),
            r.z.ToString("R", CultureInfo.InvariantCulture),
            r.w.ToString("R", CultureInfo.InvariantCulture)
        });
        view.GetZDO().Set(SurfaceReturnKey, payload);
    }

    private static bool TryReadPersistedReturnAnchor(Player player, out SurfaceAnchor anchor)
    {
        anchor = default;
        var view = player.GetComponent<ZNetView>();
        if (view == null || !view.IsValid()) return false;

        var payload = view.GetZDO().GetString(SurfaceReturnKey, string.Empty);
        if (string.IsNullOrWhiteSpace(payload)) return false;
        var parts = payload.Split('|');
        if (parts.Length != 7) return false;

        var values = new float[7];
        for (var i = 0; i < values.Length; i++)
        {
            if (!float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out values[i])) return false;
        }

        var position = new Vector3(values[0], values[1], values[2]);
        // A legitimate entry anchor can never itself be in the Underworld engine layer. Reject
        // corrupt/stale data rather than allowing persisted transition state to become authority.
        if (UnderworldInstanceLayer.IsUnderworldEnginePosition(position)) return false;

        var rotation = new Quaternion(values[3], values[4], values[5], values[6]);
        anchor = new SurfaceAnchor(position, rotation);
        return true;
    }

    private static void ClearPersistedReturnAnchor(Player player)
    {
        var view = player.GetComponent<ZNetView>();
        if (view == null || !view.IsValid()) return;
        view.GetZDO().Set(SurfaceReturnKey, string.Empty);
    }

    private readonly record struct SurfaceAnchor(Vector3 Position, Quaternion Rotation);
}
