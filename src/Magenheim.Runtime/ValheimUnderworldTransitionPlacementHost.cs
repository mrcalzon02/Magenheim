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
/// the teleport and a short runtime-only settlement window, so reconnects and stalled teleports
/// cannot satisfy or indefinitely retain an acknowledgement issued to an earlier placement.
/// </summary>
internal sealed class ValheimUnderworldTransitionPlacementHost : IUnderworldTransitionPlacementHost
{
    private const float PositionTolerance = 1.25f;
    private const float HeadingToleranceDegrees = 8f;
    private const float PlacementSettlementTimeoutSeconds = 15f;
    private readonly IUnderworldWorldContextController _worldContext;
    private readonly ManualLogSource _log;
    private readonly Dictionary<string, PlacementReceipt> _pendingPlacements = new(StringComparer.Ordinal);

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
        if (!player.TeleportTo(position, rotation, false))
            throw new InvalidOperationException("Valheim rejected the requested instance-context player teleport.");

        _pendingPlacements[playerId] = new PlacementReceipt(player.GetInstanceID(), layer, position, heading, Time.realtimeSinceStartup);
        _log.LogDebug($"{layer} placement requested for player {playerId} at native context position {position} heading {anchor.HeadingDegrees:0.##}.");
    }

    public UnderworldPlacementObservation ObservePlayerPlacement(string playerId, UnderworldWorldIdentity identity, UnderworldLayer layer, UnderworldAnchor anchor)
    {
        if (identity is null || anchor is null || !_worldContext.IsActive(identity, layer)) return UnderworldPlacementObservation.Unavailable;
        if (!_pendingPlacements.TryGetValue(playerId, out var receipt) || receipt.Layer != layer) return UnderworldPlacementObservation.Unavailable;

        var player = ResolvePlayer(playerId);
        if (player is null || player.GetInstanceID() != receipt.PlayerInstanceId)
        {
            _pendingPlacements.Remove(playerId);
            return UnderworldPlacementObservation.Unavailable;
        }

        var target = new Vector3((float)anchor.X, (float)anchor.Y, (float)anchor.Z);
        var heading = NormalizeHeading((float)anchor.HeadingDegrees);
        if (Vector3.Distance(receipt.Target, target) > 0.01f || Mathf.Abs(Mathf.DeltaAngle(receipt.Heading, heading)) > 0.01f)
        {
            _pendingPlacements.Remove(playerId);
            return UnderworldPlacementObservation.Unavailable;
        }

        if (Time.realtimeSinceStartup - receipt.IssuedAtRealtime > PlacementSettlementTimeoutSeconds)
        {
            _pendingPlacements.Remove(playerId);
            _log.LogWarning($"{layer} placement acknowledgement timed out for player {playerId}; durable transition recovery will decide the next action.");
            return UnderworldPlacementObservation.Unavailable;
        }

        if (Vector3.Distance(player.transform.position, target) > PositionTolerance) return UnderworldPlacementObservation.Pending;
        var headingDelta = Mathf.Abs(Mathf.DeltaAngle(player.transform.eulerAngles.y, heading));
        if (headingDelta > HeadingToleranceDegrees) return UnderworldPlacementObservation.Pending;

        _pendingPlacements.Remove(playerId);
        return UnderworldPlacementObservation.Confirmed;
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
