using System;
using BepInEx.Logging;
using HarmonyLib;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

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
        UnderworldMapPresentationRuntime.Reset();
        UnderworldExplorationPlayerPatch.Reset();
        var harmony = new Harmony(MagenheimPlugin.PluginGuid + ".gameplay");\n        harmony.PatchAll(typeof(UnderworldExplorationPlayerPatch));\n        harmony.PatchAll(typeof(UnderworldMinimapAwakePatch));
    }

    internal static MagenheimMapLayer PlayerLayer()
    {
        var services = _services;
        if (services is null) return MagenheimMapLayer.Surface;
        return services.TryResolveLocalSession(out _, out var layer, out _, out _)
            && layer == UnderworldLayer.Underworld ? MagenheimMapLayer.Underworld : MagenheimMapLayer.Surface;
    }

    internal static void FollowPlayerLayer() => Select(PlayerLayer());

    internal static void Select(MagenheimMapLayer layer)
    {
        if (_selectedLayer == layer) return;
        if (_selectedLayer == MagenheimMapLayer.Underworld && _underworldExploration is not null)
        {
            try { SaveUnderworldExploration(); }
            catch (Exception exception) { Warn("Failed to persist Underworld exploration while leaving selected layer: " + exception.Message); }
        }
        _selectedLayer = layer;
        if (layer == MagenheimMapLayer.Underworld) UnderworldMapPresentationRuntime.ForceRefresh();
        _log?.LogDebug("Magenheim map layer selected: " + layer + ".");
    }

    internal static bool TryGetUnderworldExploration(out UnderworldExplorationState state)
    {
        state = null!;
        var services = _services;
        if (services is null || !services.TryResolveLocalSession(out var identity, out _, out var playerId, out _) || identity is null) return false;
        var key = identity.ParentWorldId + "\n" + identity.DerivedSeedFingerprint + "\n" + playerId;
        if (_underworldExploration is not null && string.Equals(_explorationIdentityKey, key, StringComparison.Ordinal))
        {
            state = _underworldExploration;
            return true;
        }
        if (_underworldExploration is not null)
        {
            try { SaveUnderworldExploration(); }
            catch (Exception exception) { Warn("Failed to persist outgoing Underworld exploration: " + exception.Message); }
        }
        var fresh = new UnderworldExplorationState(MagenheimMapLayer.Underworld,
            UnderworldExplorationResolution, UnderworldExplorationResolution);
        if (services.ExplorationStateStore.TryRestore(playerId, identity, fresh, out var diagnostic))
            _log?.LogDebug("Restored Underworld logical-map exploration for active player/world.");
        else if (!string.IsNullOrWhiteSpace(diagnostic))
            _log?.LogDebug("Starting fresh Underworld logical-map exploration: " + diagnostic);
        _underworldExploration = fresh;
        _explorationIdentityKey = key;
        UnderworldMapPresentationRuntime.ForceRefresh();
        state = fresh;
        return true;
    }

    internal static int RevealUnderworldAtWorldPosition(double worldX, double worldZ, double radiusMeters = DefaultRevealRadiusMeters)
    {
        if (radiusMeters < 0d || double.IsNaN(radiusMeters) || double.IsInfinity(radiusMeters)) throw new ArgumentOutOfRangeException(nameof(radiusMeters));
        var services = _services;
        if (services is null || !UnderworldSpatialDomain.ContainsHostColumn(services.SpatialDomain, worldX, worldZ) || !TryGetUnderworldExploration(out var exploration)) return 0;
        var logical = UnderworldSpatialDomain.ToLogicalColumn(services.SpatialDomain, worldX, worldZ);
        var viewport = UnderworldMapPresentation.CreateUnderworldViewport(services.SpatialDomain, exploration.Width, exploration.Height);
        if (!UnderworldMapPresentation.TryLogicalToCell(viewport, logical.X, logical.Z, out var cellX, out var cellY)) return 0;
        var metresPerCell = services.SpatialDomain.RadiusMeters * 2d / Math.Min(exploration.Width, exploration.Height);
        var revealed = exploration.RevealCircle(cellX, cellY, (int)Math.Ceiling(radiusMeters / metresPerCell));
        if (revealed > 0) UnderworldMapPresentationRuntime.RefreshFogTexture();
        return revealed;
    }

    internal static bool SaveUnderworldExploration()
    {
        var services = _services;
        if (services is null || _underworldExploration is null || !services.TryResolveLocalSession(out var identity, out _, out var playerId, out _) || identity is null) return false;
        services.ExplorationStateStore.Save(playerId, identity, _underworldExploration);
        return true;
    }

    internal static bool TryProjectWorldToSelectedMap(double worldX, double worldZ, out MagenheimMapPoint point)
    {
        var services = _services;
        if (services is null) { point = new MagenheimMapPoint(worldX, worldZ); return _selectedLayer == MagenheimMapLayer.Surface; }
        try { point = UnderworldMapProjection.WorldToLayer(services.SpatialDomain, _selectedLayer, worldX, worldZ); return true; }
        catch (InvalidOperationException) { point = default; return false; }
    }

    internal static bool TryProjectSelectedMapToWorld(double mapX, double mapZ, out MagenheimMapPoint point)
    {
        var services = _services;
        if (services is null) { point = new MagenheimMapPoint(mapX, mapZ); return _selectedLayer == MagenheimMapLayer.Surface; }
        try { point = UnderworldMapProjection.LayerToWorld(services.SpatialDomain, _selectedLayer, mapX, mapZ); return true; }
        catch (InvalidOperationException) { point = default; return false; }
    }

    internal static void Warn(string message) => _log?.LogWarning(message);

    internal static void Reset()
    {
        try { SaveUnderworldExploration(); }
        catch (Exception exception) { Warn("Failed to persist Underworld exploration during shutdown: " + exception.Message); }
        UnderworldMapPresentationRuntime.Reset();
        _selectedLayer = MagenheimMapLayer.Surface;
        _underworldExploration = null;
        _explorationIdentityKey = null;
        _services = null;
        _log = null;
        UnderworldExplorationPlayerPatch.Reset();
    }
}

[HarmonyPatch(typeof(Player), "Update", new Type[0])]
internal static class UnderworldExplorationPlayerPatch
{
    private const float RevealIntervalSeconds = 0.75f;
    private const float PersistenceIntervalSeconds = 30f;
    private static float _nextRevealAt;
    private static float _nextPersistenceAt;
    private static bool _dirty;
    private static MagenheimMapLayer _lastPhysicalLayer = MagenheimMapLayer.Surface;

    private static void Postfix(Player __instance)
    {
        if (!ReferenceEquals(__instance, Player.m_localPlayer)) return;
        var now = Time.unscaledTime;
        if (now < _nextRevealAt) return;
        _nextRevealAt = now + RevealIntervalSeconds;
        try
        {
            var physicalLayer = UnderworldMapLayerRuntime.PlayerLayer();
            if (_lastPhysicalLayer == MagenheimMapLayer.Underworld && physicalLayer != MagenheimMapLayer.Underworld)
            {
                // Selected map tabs are intentionally independent from the player's physical layer.
                // Persist against the physical transition itself so walking/teleporting out cannot
                // strand up to a persistence interval of discoveries merely because the player left
                // the Underworld map tab selected.
                if (_dirty && UnderworldMapLayerRuntime.SaveUnderworldExploration()) _dirty = false;
                _nextPersistenceAt = now + PersistenceIntervalSeconds;
            }
            _lastPhysicalLayer = physicalLayer;
            if (physicalLayer != MagenheimMapLayer.Underworld) return;

            var position = __instance.transform.position;
            if (UnderworldMapLayerRuntime.RevealUnderworldAtWorldPosition(position.x, position.z) > 0) _dirty = true;
            if (_dirty && now >= _nextPersistenceAt)
            {
                if (UnderworldMapLayerRuntime.SaveUnderworldExploration()) _dirty = false;
                _nextPersistenceAt = now + PersistenceIntervalSeconds;
            }
        }
        catch (Exception exception) { UnderworldMapLayerRuntime.Warn("Underworld exploration lifecycle update failed: " + exception.Message); }
    }

    internal static void Reset()
    {
        _nextRevealAt = 0f;
        _nextPersistenceAt = 0f;
        _dirty = false;
        _lastPhysicalLayer = MagenheimMapLayer.Surface;
    }
}
