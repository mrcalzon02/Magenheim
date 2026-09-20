using System;
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
/// </summary>
internal sealed class ValheimUnderworldTransitionPlacementHost : IUnderworldTransitionPlacementHost
{
    private const float PositionTolerance = 1.25f;
    private const float HeadingToleranceDegrees = 8f;
    private readonly IUnderworldWorldContextController _worldContext;
    private readonly ManualLogSource _log;

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
        var rotation = Quaternion.Euler(0f, NormalizeHeading((float)anchor.HeadingDegrees), 0f);
        if (!player.TeleportTo(position, rotation, false))
            throw new InvalidOperationException("Valheim rejected the requested instance-context player teleport.");
        _log.LogDebug($"{layer} placement requested for player {playerId} at native context position {position} heading {anchor.HeadingDegrees:0.##}.");
    }

    public bool ObservePlayerPlacement(string playerId, UnderworldWorldIdentity identity, UnderworldLayer layer, UnderworldAnchor anchor)
    {
        if (identity is null || anchor is null || !_worldContext.IsActive(identity, layer)) return false;
        var player = ResolvePlayer(playerId);
        if (player is null) return false;

        var target = new Vector3((float)anchor.X, (float)anchor.Y, (float)anchor.Z);
        if (Vector3.Distance(player.transform.position, target) > PositionTolerance) return false;
        var headingDelta = Mathf.Abs(Mathf.DeltaAngle(player.transform.eulerAngles.y, NormalizeHeading((float)anchor.HeadingDegrees)));
        return headingDelta <= HeadingToleranceDegrees;
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
}
