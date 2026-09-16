using System;
using System.IO;
using BepInEx.Logging;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>
/// Owns the complete Underworld transition service graph for one plugin lifetime.
/// Construction is side-effect free; world identity is bound only when a live world session is available.
/// </summary>
internal sealed class UnderworldRuntimeServices
{
    private readonly ValheimLogicalUnderworldWorldContextController _worldContext;

    private UnderworldRuntimeServices(
        UnderworldSpatialDomainDefinition spatialDomain,
        UnderworldTransitionStateStore stateStore,
        ValheimLogicalUnderworldWorldContextController worldContext,
        ValheimUnderworldTransitionPlacementHost placementHost,
        StoredUnderworldTransitionHost transitionHost,
        UnderworldWorldTransitionManager transitionManager,
        UnderworldTransitionRecoveryRuntime recoveryRuntime)
    {
        SpatialDomain = spatialDomain;
        StateStore = stateStore;
        _worldContext = worldContext;
        PlacementHost = placementHost;
        TransitionHost = transitionHost;
        TransitionManager = transitionManager;
        RecoveryRuntime = recoveryRuntime;
    }

    internal UnderworldSpatialDomainDefinition SpatialDomain { get; }
    internal UnderworldTransitionStateStore StateStore { get; }
    internal ValheimUnderworldTransitionPlacementHost PlacementHost { get; }
    internal StoredUnderworldTransitionHost TransitionHost { get; }
    internal UnderworldWorldTransitionManager TransitionManager { get; }
    internal UnderworldTransitionRecoveryRuntime RecoveryRuntime { get; }

    internal static UnderworldRuntimeServices Create(string pluginConfigDirectory, ManualLogSource log)
    {
        if (string.IsNullOrWhiteSpace(pluginConfigDirectory))
            throw new ArgumentException("Plugin config directory is required for Underworld persistence.", nameof(pluginConfigDirectory));
        if (log is null) throw new ArgumentNullException(nameof(log));

        var spatialDomain = UnderworldSpatialDomain.CreateDefault();
        var stateRoot = Path.Combine(Path.GetFullPath(pluginConfigDirectory), "Magenheim", "underworld-transitions");
        var stateStore = new UnderworldTransitionStateStore(stateRoot, log);
        var worldContext = new ValheimLogicalUnderworldWorldContextController(log);
        var placementHost = new ValheimUnderworldTransitionPlacementHost(worldContext, spatialDomain, log);
        var transitionHost = new StoredUnderworldTransitionHost(stateStore, placementHost);
        var transitionManager = new UnderworldWorldTransitionManager(transitionHost, log);
        var recoveryRuntime = new UnderworldTransitionRecoveryRuntime(stateStore, transitionManager, log);

        return new UnderworldRuntimeServices(
            spatialDomain,
            stateStore,
            worldContext,
            placementHost,
            transitionHost,
            transitionManager,
            recoveryRuntime);
    }

    internal bool TryResolveLocalSession(
        out UnderworldWorldIdentity? identity,
        out string playerId,
        out string diagnostic) =>
        UnderworldRuntimeIdentityResolver.TryResolveLocalSession(out identity, out playerId, out diagnostic);

    internal void ResetForWorldUnload() => _worldContext.ResetForWorldUnload();
}
