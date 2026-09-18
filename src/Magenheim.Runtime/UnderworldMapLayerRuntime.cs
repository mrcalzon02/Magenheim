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
    internal const int UnderworldExplorationResolution = 512;
    internal const double DefaultRevealRadiusMeters = 100d;

    private static UnderworldRuntimeServices? _services;
    private static ManualLogSource? _log;
    private static MagenheimMapLayer _selectedLayer = MagenheimMapLayer.Surface;
    private static UnderworldExplorationState? _underworldExploration;
    private static string? _explorationIdentityKey;

    internal static MagenheimMapLayer SelectedLayer => _selectedLayer;
    internal static UnderworldExplorationState? UnderworldExploration => _underworldExploration;

    internal static void Configure(UnderworldRuntimeServices services, ManualLogSource log)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _selectedLayer = MagenheimMapLayer.Surface;
        _underworldExploration = null;
        _explorationIdentityKey = null;
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

    internal static void FollowPlayerLayer() => Select(PlayerLayer());

    internal static void Select(MagenheimMapLayer layer)
    {
        if (_selectedLayer == layer) return;
        _selectedLayer = layer;
        _log?.LogDebug("Magenheim map layer selected: " + layer + ".");
    }

    internal static bool TryGetUnderworldExploration(out UnderworldExplorationState state)
    {
        state = null!;
        var services = _services;
        if (services is null || !services.TryResolveLocalSession(out var identity, out _, out var playerId, out _))
            return false;

        var key = identity.ParentWorldId + "\n" + identity.DerivedSeedFingerprint + "\n" + playerId;
        if (_underworldExploration is not null && string.Equals(_explorationIdentityKey, key, StringComparison.Ordinal))
        {
            state = _underworldExploration;
            return true;
        }

        // Do not silently discard dirty fog if the active player/world identity changes in-process.
        if (_underworldExploration is not null)
        {
            try { SaveUnderworldExploration(); }
            catch (Exception exception) { _log?.LogWarning("Failed to persist outgoing Underworld exploration: " + exception.Message); }
        }

        var fresh = new UnderworldExplorationState(MagenheimMapLayer.Underworld,
            UnderworldExplorationResolution, UnderworldExplorationResolution);
        if (services.ExplorationStateStore.TryRestore(playerId, identity, fresh, out var diagnostic))
            _log?.LogDebug("Restored Underworld logical-map exploration for active player/world.");
        else if (!string.IsNullOrWhiteSpace(diagnostic))
            _log?.LogDebug("Starting fresh Underworld logical-map exploration: " + diagnostic);

        _underworldExploration = fresh;
        _explorationIdentityKey = key;
        state = fresh;
        return true;
    }

    /// <summary>
    /// Reveals logical Underworld fog around a physical world position. The host displacement is
    /// removed before cell selection; surface positions never mutate Underworld discovery state.
    /// Returns the number of newly revealed cells.
    /// </summary>
    internal static int RevealUnderworldAtWorldPosition(double worldX, double worldZ,
        double radiusMeters = DefaultRevealRadiusMeters)
    {
        if (radiusMeters < 0d || double.IsNaN(radiusMeters) || double.IsInfinity(radiusMeters))
            throw new ArgumentOutOfRangeException(nameof(radiusMeters));
        var services = _services;
        if (services is null || !UnderworldSpatialDomain.ContainsHostColumn(services.SpatialDomain, worldX, worldZ) ||
            !TryGetUnderworldExploration(out var exploration)) return 0;

        var logical = UnderworldSpatialDomain.ToLogicalColumn(services.SpatialDomain, worldX, worldZ);
        var viewport = UnderworldMapPresentation.CreateUnderworldViewport(services.SpatialDomain,
            exploration.Width, exploration.Height);
        if (!UnderworldMapPresentation.TryLogicalToCell(viewport, logical.X, logical.Z, out var cellX, out var cellY))
            return 0;

        var metresPerCell = services.SpatialDomain.RadiusMeters * 2d / Math.Min(exploration.Width, exploration.Height);
        var radiusCells = (int)Math.Ceiling(radiusMeters / metresPerCell);
        return exploration.RevealCircle(cellX, cellY, radiusCells);
    }

    internal static bool SaveUnderworldExploration()
    {
        var services = _services;
        if (services is null || _underworldExploration is null ||
            !services.TryResolveLocalSession(out var identity, out _, out var playerId, out _)) return false;
        services.ExplorationStateStore.Save(playerId, identity, _underworldExploration);
        return true;
    }

    internal static bool TryProjectWorldToSelectedMap(double worldX, double worldZ, out MagenheimMapPoint point)
    {
        var services = _services;
        if (services is null)
        {
            point = new MagenheimMapPoint(worldX, worldZ);
            return _selectedLayer == MagenheimMapLayer.Surface;
        }
        try { point = UnderworldMapProjection.WorldToLayer(services.SpatialDomain, _selectedLayer, worldX, worldZ); return true; }
        catch (InvalidOperationException) { point = default; return false; }
    }

    internal static bool TryProjectSelectedMapToWorld(double mapX, double mapZ, out MagenheimMapPoint point)
    {
        var services = _services;
        if (services is null)
        {
            point = new MagenheimMapPoint(mapX, mapZ);
            return _selectedLayer == MagenheimMapLayer.Surface;
        }
        try { point = UnderworldMapProjection.LayerToWorld(services.SpatialDomain, _selectedLayer, mapX, mapZ); return true; }
        catch (InvalidOperationException) { point = default; return false; }
    }

    internal static void Reset()
    {
        try { SaveUnderworldExploration(); }
        catch (Exception exception) { _log?.LogWarning("Failed to persist Underworld exploration during shutdown: " + exception.Message); }
        _selectedLayer = MagenheimMapLayer.Surface;
        _underworldExploration = null;
        _explorationIdentityKey = null;
        _services = null;
        _log = null;
    }
}
