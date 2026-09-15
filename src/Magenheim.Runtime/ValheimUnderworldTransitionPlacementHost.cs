using System;
using BepInEx.Logging;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Owns the one remaining game-version-specific operation that cannot be represented by Core:
/// selecting/observing the active Valheim world context. Implementations must not report success
/// until the requested parent/derived world is actually the active context.
/// </summary>
internal interface IUnderworldWorldContextController
{
    void EnsureActive(UnderworldWorldIdentity identity, UnderworldLayer layer);
    bool IsActive(UnderworldWorldIdentity identity, UnderworldLayer layer);
}

/// <summary>
/// Valheim binding for Underworld placement and placement observation. World-context switching is
/// deliberately isolated behind IUnderworldWorldContextController; once that controller proves the
/// requested context active, this class uses Valheim's current Character.TeleportTo API and then
/// independently observes world, position and heading before the transaction manager may commit.
/// </summary>
internal sealed class ValheimUnderworldTransitionPlacementHost : IUnderworldTransitionPlacementHost
{
    private const float PositionTolerance = 1.25f;
    private const float HeadingToleranceDegrees = 8f;

    private readonly IUnderworldWorldContextController _worldContext;
    private readonly ManualLogSource _log;

    internal ValheimUnderworldTransitionPlacementHost(
        IUnderworldWorldContextController worldContext,
        ManualLogSource log)
    {
        _worldContext = worldContext ?? throw new ArgumentNullException(nameof(worldContext));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    public void EnsureTargetContext(
        UnderworldWorldIdentity identity,
        UnderworldLayer layer,
        UnderworldAnchor anchor)
    {
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        if (anchor is null) throw new ArgumentNullException(nameof(anchor));

        var expectedWorldId = ExpectedWorldId(identity, layer);
        if (!string.Equals(anchor.WorldId, expectedWorldId, StringComparison.Ordinal))
            throw new InvalidOperationException("Underworld placement anchor does not belong to the requested layer world.");

        _worldContext.EnsureActive(identity, layer);
        if (!_worldContext.IsActive(identity, layer))
            throw new InvalidOperationException("Valheim did not observe the requested Underworld world context as active.");
    }

    public void PlacePlayer(UnderworldLayer layer, UnderworldAnchor anchor)
    {
        if (anchor is null) throw new ArgumentNullException(nameof(anchor));
        var player = Player.m_localPlayer
            ?? throw new InvalidOperationException("The local Valheim player is unavailable for Underworld placement.");

        var position = new Vector3((float)anchor.X, (float)anchor.Y, (float)anchor.Z);
        var rotation = Quaternion.Euler(0f, NormalizeHeading(anchor.HeadingDegrees), 0f);
        if (!player.TeleportTo(position, rotation, false))
            throw new InvalidOperationException("Valheim rejected the requested Underworld player teleport.");

        _log.LogDebug($"Underworld placement requested at {position} heading {anchor.HeadingDegrees:0.##} on {layer}.");
    }

    public bool ObservePlayerPlacement(UnderworldLayer layer, UnderworldAnchor anchor)
    {
        if (anchor is null) return false;
        var player = Player.m_localPlayer;
        if (player is null) return false;

        var target = new Vector3((float)anchor.X, (float)anchor.Y, (float)anchor.Z);
        if (Vector3.Distance(player.transform.position, target) > PositionTolerance)
            return false;

        var headingDelta = Mathf.Abs(Mathf.DeltaAngle(player.transform.eulerAngles.y, NormalizeHeading(anchor.HeadingDegrees)));
        return headingDelta <= HeadingToleranceDegrees;
    }

    internal bool ObservePlayerPlacement(
        UnderworldWorldIdentity identity,
        UnderworldLayer layer,
        UnderworldAnchor anchor)
    {
        return _worldContext.IsActive(identity, layer) && ObservePlayerPlacement(layer, anchor);
    }

    private static string ExpectedWorldId(UnderworldWorldIdentity identity, UnderworldLayer layer) =>
        layer == UnderworldLayer.Surface ? identity.ParentWorldId : identity.DerivedWorldId;

    private static float NormalizeHeading(float heading)
    {
        var normalized = heading % 360f;
        return normalized < 0f ? normalized + 360f : normalized;
    }
}
