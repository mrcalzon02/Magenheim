using System;
using BepInEx.Logging;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>
/// Runtime authority for selecting the player-facing map independently of the physical host
/// coordinates. The reserved ~40km region is simulation/storage space; it must never leak into
/// Minimap presentation as an adjacent continent.
/// </summary>
internal static class UnderworldMapLayerRuntime
{
    private static UnderworldRuntimeServices? _services;
    private static ManualLogSource? _log;
    private static MagenheimMapLayer _selectedLayer = MagenheimMapLayer.Surface;

    internal static MagenheimMapLayer SelectedLayer => _selectedLayer;

    internal static void Configure(UnderworldRuntimeServices services, ManualLogSource log)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _selectedLayer = MagenheimMapLayer.Surface;
    }

    internal static MagenheimMapLayer PlayerLayer()
    {
        var services = _services;
        if (services is null) return MagenheimMapLayer.Surface;
        return services.TryResolveLocalSession(out _, out var layer, out _, out _)
            && layer == UnderworldLayer.Underworld
            ? MagenheimMapLayer.Underworld
            : MagenheimMapLayer.Surface;
    }

    internal static void FollowPlayerLayer()
    {
        Select(PlayerLayer());
    }

    internal static void Select(MagenheimMapLayer layer)
    {
        if (_selectedLayer == layer) return;
        _selectedLayer = layer;
        _log?.LogDebug("Magenheim map layer selected: " + layer + ".");
    }

    internal static bool TryProjectWorldToSelectedMap(double worldX, double worldZ, out MagenheimMapPoint point)
    {
        var services = _services;
        if (services is null)
        {
            point = new MagenheimMapPoint(worldX, worldZ);
            return _selectedLayer == MagenheimMapLayer.Surface;
        }

        try
        {
            point = UnderworldMapProjection.WorldToLayer(services.SpatialDomain, _selectedLayer, worldX, worldZ);
            return true;
        }
        catch (InvalidOperationException)
        {
            point = default;
            return false;
        }
    }

    internal static bool TryProjectSelectedMapToWorld(double mapX, double mapZ, out MagenheimMapPoint point)
    {
        var services = _services;
        if (services is null)
        {
            point = new MagenheimMapPoint(mapX, mapZ);
            return _selectedLayer == MagenheimMapLayer.Surface;
        }

        try
        {
            point = UnderworldMapProjection.LayerToWorld(services.SpatialDomain, _selectedLayer, mapX, mapZ);
            return true;
        }
        catch (InvalidOperationException)
        {
            point = default;
            return false;
        }
    }

    internal static void Reset()
    {
        _selectedLayer = MagenheimMapLayer.Surface;
        _services = null;
        _log = null;
    }
}
