using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx.Logging;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

internal interface IUnderworldWorldContextController
{
    void EnsureActive(UnderworldWorldIdentity identity, UnderworldLayer layer);
    bool IsActive(UnderworldWorldIdentity identity, UnderworldLayer layer);
}

/// <summary>
/// Valheim player-placement boundary for Surface and dedicated Underworld instance contexts.
/// Placement is keyed by the durable Valheim player id so server recovery never aliases a remote
/// transition onto Player.m_localPlayer. Underworld anchors are native instance coordinates.
/// Placement acknowledgement is additionally bound to the concrete Player component that accepted
/// the teleport and a short runtime-only settlement window. Timed-out placements enter a bounded
/// runtime-only retry delay so a persistent engine/context failure cannot produce a tight teleport loop.
/// </summary>
internal sealed class ValheimUnderworldTransitionPlacementHost : IUnderworldTransitionPlacementHost
{
    private const float PositionTolerance = 1.25f;
    private const float HeadingToleranceDegrees = 8f;
    private const float PlacementSettlementTimeoutSeconds = 15f;
    private const float PlacementRetryDelaySeconds = 5f;
    private readonly IUnderworldWorldContextController _worldContext;
    private readonly ManualLogSource _log;
    private readonly Dictionary<string, PlacementReceipt> _pendingPlacements = new(StringComparer.Ordinal);
    private readonly Dictionary<string, float> _retryNotBefore = new(StringComparer.Ordinal);

    internal ValheimUnderworldTransitionPlacementHost(IUnderworldWorldContextController worldContext, ManualLogSource log)
    {
        _worldContext = worldContext ?? throw new ArgumentNullException(nameof(worldContext));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    public void EnsureTargetContext(UnderworldWorldIdentity identity, UnderworldLayer layer, UnderworldAnchor anchor)
    {
        ValidateAnchor(identity, anchor);
        _worldContext.EnsureActive(identity, layer);
    }

    public void PlacePlayer(string playerId, UnderworldWorldIdentity identity, UnderworldLayer layer, UnderworldAnchor anchor)
    {
        ValidateAnchor(identity, anchor);
        if (!_worldContext.IsActive(identity, layer))
            throw new InvalidOperationException($"Cannot place player before {layer} context is active.");

        var player = ResolvePlayer(playerId) ?? throw new InvalidOperationException($"Valheim player '{playerId}' is unavailable for Underworld placement.");
        var position = new Vector3((float)anchor.X, (float)anchor.Y, (float)anchor.Z);
        var heading = NormalizeHeading((float)anchor.HeadingDegrees);
        var rotation = Quaternion.Euler(0f, heading, 0f);

        _pendingPlacements.Remove(playerId);
        _retryNotBefore.Remove(playerId);
        if (!player.TeleportTo(position, rotation, false))
            throw new InvalidOperationException("Valheim rejected the requested instance-context player teleport.");

        _pendingPlacements[playerId] = new PlacementReceipt(player.GetInstanceID(), layer, position, heading, Time.realtimeSinceStartup);
        _log.LogDebug($"{layer} placement requested for player {playerId} at native context position {position} heading {anchor.HeadingDegrees:0.##}.");
    }

    public UnderworldPlacementObservation ObservePlayerPlacement(string playerId, UnderworldWorldIdentity identity, UnderworldLayer layer, UnderworldAnchor anchor)
    {
        if (identity is null || anchor is null || !_worldContext.IsActive(identity, layer)) return UnderworldPlacementObservation.Unavailable;

        if (!_pendingPlacements.TryGetValue(playerId, out var receipt) || receipt.Layer != layer)
        {
            if (!_retryNotBefore.TryGetValue(playerId, out var retryNotBefore)) return UnderworldPlacementObservation.Unavailable;
            if (Time.realtimeSinceStartup < retryNotBefore) return UnderworldPlacementObservation.Pending;
            _retryNotBefore.Remove(playerId);
            return UnderworldPlacementObservation.Unavailable;
        }

        var player = ResolvePlayer(playerId);
        if (player is null || player.GetInstanceID() != receipt.PlayerInstanceId)
        {
            ClearPlacementRuntimeState(playerId);
            return UnderworldPlacementObservation.Unavailable;
        }

        var target = new Vector3((float)anchor.X, (float)anchor.Y, (float)anchor.Z);
        var heading = NormalizeHeading((float)anchor.HeadingDegrees);
        if (Vector3.Distance(receipt.Target, target) > 0.01f || Mathf.Abs(Mathf.DeltaAngle(receipt.Heading, heading)) > 0.01f)
        {
            ClearPlacementRuntimeState(playerId);
            return UnderworldPlacementObservation.Unavailable;
        }

        if (Time.realtimeSinceStartup - receipt.IssuedAtRealtime > PlacementSettlementTimeoutSeconds)
        {
            _pendingPlacements.Remove(playerId);
            _retryNotBefore[playerId] = Time.realtimeSinceStartup + PlacementRetryDelaySeconds;
            _log.LogWarning($"{layer} placement acknowledgement timed out for player {playerId}; durable transition recovery may retry after {PlacementRetryDelaySeconds:0.#} seconds.");
            return UnderworldPlacementObservation.Pending;
        }

        if (Vector3.Distance(player.transform.position, target) > PositionTolerance) return UnderworldPlacementObservation.Pending;
        var headingDelta = Mathf.Abs(Mathf.DeltaAngle(player.transform.eulerAngles.y, heading));
        if (headingDelta > HeadingToleranceDegrees) return UnderworldPlacementObservation.Pending;

        ClearPlacementRuntimeState(playerId);
        return UnderworldPlacementObservation.Confirmed;
    }

    private void ClearPlacementRuntimeState(string playerId)
    {
        _pendingPlacements.Remove(playerId);
        _retryNotBefore.Remove(playerId);
    }

    private static Player? ResolvePlayer(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId)) return null;
        foreach (var player in Player.GetAllPlayers())
        {
            if (!player) continue;
            var candidate = player.GetPlayerID().ToString(CultureInfo.InvariantCulture);
            if (string.Equals(candidate, playerId, StringComparison.Ordinal)) return player;
        }
        return null;
    }

    private static void ValidateAnchor(UnderworldWorldIdentity identity, UnderworldAnchor anchor)
    {
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        if (anchor is null) throw new ArgumentNullException(nameof(anchor));
        if (!Finite(anchor.X) || !Finite(anchor.Y) || !Finite(anchor.Z) || !Finite(anchor.HeadingDegrees))
            throw new InvalidOperationException("Transition anchor contains non-finite instance coordinates.");
    }

    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    private static float NormalizeHeading(float heading)
    {
        var normalized = heading % 360f;
        return normalized < 0f ? normalized + 360f : normalized;
    }

    private readonly struct PlacementReceipt
    {
        internal PlacementReceipt(int playerInstanceId, UnderworldLayer layer, Vector3 target, float heading, float issuedAtRealtime)
        {
            PlayerInstanceId = playerInstanceId;
            Layer = layer;
            Target = target;
            Heading = heading;
            IssuedAtRealtime = issuedAtRealtime;
        }

        internal int PlayerInstanceId { get; }
        internal UnderworldLayer Layer { get; }
        internal Vector3 Target { get; }
        internal float Heading { get; }
        internal float IssuedAtRealtime { get; }
    }
}
