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
/// Valheim binding for player placement. Logical Underworld anchors are deterministically mapped
/// into the reserved host spatial band immediately before TeleportTo, while surface anchors remain
/// in ordinary parent-world coordinates. Commit observation repeats the same mapping and verifies
/// both logical layer context and the physical player transform.
/// </summary>
internal sealed class ValheimUnderworldTransitionPlacementHost : IUnderworldTransitionPlacementHost
{
    private const float PositionTolerance = 1.25f;
    private const float HeadingToleranceDegrees = 8f;
    private readonly IUnderworldWorldContextController _worldContext;
    private readonly UnderworldSpatialDomainDefinition _spatialDomain;
    private readonly ManualLogSource _log;

    internal ValheimUnderworldTransitionPlacementHost(
        IUnderworldWorldContextController worldContext,
        ManualLogSource log)
        : this(worldContext, UnderworldSpatialDomain.CreateDefault(), log)
    {
    }

    internal ValheimUnderworldTransitionPlacementHost(
        IUnderworldWorldContextController worldContext,
        UnderworldSpatialDomainDefinition spatialDomain,
        ManualLogSource log)
    {
        _worldContext = worldContext ?? throw new ArgumentNullException(nameof(worldContext));
        _spatialDomain = spatialDomain ?? throw new ArgumentNullException(nameof(spatialDomain));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    public void EnsureTargetContext(UnderworldWorldIdentity identity, UnderworldLayer layer, UnderworldAnchor anchor)
    {
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        if (anchor is null) throw new ArgumentNullException(nameof(anchor));

        // Mapping validates world identity, finite coordinates and reserved Underworld bounds before
        // context activation, so an invalid target cannot switch logical layers first and fail later.
        UnderworldSpatialDomain.ToHostAnchor(_spatialDomain, identity, layer, anchor);
        // Surface and Underworld are regions of the same loaded world. EnsureActive validates
        // world identity only; the target layer cannot become observable until PlacePlayer moves
        // the player across the spatial-domain boundary.
        _worldContext.EnsureActive(identity, layer);
    }

    public void PlacePlayer(UnderworldWorldIdentity identity, UnderworldLayer layer, UnderworldAnchor anchor)
    {
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        if (anchor is null) throw new ArgumentNullException(nameof(anchor));
        var player = Player.m_localPlayer ?? throw new InvalidOperationException("The local Valheim player is unavailable for Underworld placement.");
        var host = UnderworldSpatialDomain.ToHostAnchor(_spatialDomain, identity, layer, anchor);
        var position = new Vector3((float)host.X, (float)host.Y, (float)host.Z);
        var rotation = Quaternion.Euler(0f, NormalizeHeading(host.HeadingDegrees), 0f);
        if (!player.TeleportTo(position, rotation, false))
            throw new InvalidOperationException("Valheim rejected the requested Underworld player teleport.");
        _log.LogDebug($"Underworld placement requested at host {position} heading {host.HeadingDegrees:0.##} for logical layer {layer}.");
    }

    public bool ObservePlayerPlacement(UnderworldWorldIdentity identity, UnderworldLayer layer, UnderworldAnchor anchor)
    {
        if (identity is null || anchor is null || !_worldContext.IsActive(identity, layer)) return false;
        var player = Player.m_localPlayer;
        if (player is null) return false;

        UnderworldHostAnchor host;
        try
        {
            host = UnderworldSpatialDomain.ToHostAnchor(_spatialDomain, identity, layer, anchor);
        }
        catch
        {
            return false;
        }

        var target = new Vector3((float)host.X, (float)host.Y, (float)host.Z);
        if (Vector3.Distance(player.transform.position, target) > PositionTolerance) return false;
        var headingDelta = Mathf.Abs(Mathf.DeltaAngle(player.transform.eulerAngles.y, NormalizeHeading(host.HeadingDegrees)));
        return headingDelta <= HeadingToleranceDegrees;
    }

    private static float NormalizeHeading(float heading)
    {
        var normalized = heading % 360f;
        return normalized < 0f ? normalized + 360f : normalized;
    }
}
