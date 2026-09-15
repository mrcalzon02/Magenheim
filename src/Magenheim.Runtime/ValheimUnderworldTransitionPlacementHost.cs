using System;
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
/// Valheim binding for player placement. It uses the current Character.TeleportTo API only after
/// the world-context controller has proved the requested layer active, and commit observation
/// rechecks both context and player transform so a context drift cannot be mistaken for success.
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
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        if (anchor is null) throw new ArgumentNullException(nameof(anchor));
        var expectedWorldId = layer == UnderworldLayer.Surface ? identity.ParentWorldId : identity.DerivedWorldId;
        if (!string.Equals(anchor.WorldId, expectedWorldId, StringComparison.Ordinal))
            throw new InvalidOperationException("Underworld placement anchor does not belong to the requested layer world.");
        _worldContext.EnsureActive(identity, layer);
        if (!_worldContext.IsActive(identity, layer))
            throw new InvalidOperationException("Valheim did not observe the requested Underworld world context as active.");
    }

    public void PlacePlayer(UnderworldLayer layer, UnderworldAnchor anchor)
    {
        if (anchor is null) throw new ArgumentNullException(nameof(anchor));
        var player = Player.m_localPlayer ?? throw new InvalidOperationException("The local Valheim player is unavailable for Underworld placement.");
        var position = new Vector3((float)anchor.X, (float)anchor.Y, (float)anchor.Z);
        var rotation = Quaternion.Euler(0f, NormalizeHeading(anchor.HeadingDegrees), 0f);
        if (!player.TeleportTo(position, rotation, false))
            throw new InvalidOperationException("Valheim rejected the requested Underworld player teleport.");
        _log.LogDebug($"Underworld placement requested at {position} heading {anchor.HeadingDegrees:0.##} on {layer}.");
    }

    public bool ObservePlayerPlacement(UnderworldWorldIdentity identity, UnderworldLayer layer, UnderworldAnchor anchor)
    {
        if (identity is null || anchor is null || !_worldContext.IsActive(identity, layer)) return false;
        var player = Player.m_localPlayer;
        if (player is null) return false;
        var target = new Vector3((float)anchor.X, (float)anchor.Y, (float)anchor.Z);
        if (Vector3.Distance(player.transform.position, target) > PositionTolerance) return false;
        var headingDelta = Mathf.Abs(Mathf.DeltaAngle(player.transform.eulerAngles.y, NormalizeHeading(anchor.HeadingDegrees)));
        return headingDelta <= HeadingToleranceDegrees;
    }

    private static float NormalizeHeading(float heading)
    {
        var normalized = heading % 360f;
        return normalized < 0f ? normalized + 360f : normalized;
    }
}
