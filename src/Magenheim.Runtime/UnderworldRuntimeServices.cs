using System;
using System.IO;
using BepInEx.Logging;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

internal sealed class UnderworldRuntimeServices
{
    private readonly ValheimPhysicalUnderworldWorldContextController _worldContext;
    private UnderworldRuntimeServices(UnderworldSpatialDomainDefinition spatialDomain,UnderworldTransitionStateStore stateStore,UnderworldWorldPairManifestStore worldPairStore,UnderworldDeepBoonSelectionStore deepBoonSelectionStore,ValheimPhysicalUnderworldWorldContextController worldContext,ValheimUnderworldTransitionPlacementHost placementHost,StoredUnderworldTransitionHost transitionHost,UnderworldWorldTransitionManager transitionManager,UnderworldTransitionRecoveryRuntime recoveryRuntime,UnderworldPhysicalWorldSwitchCoordinator worldSwitchCoordinator,UnderworldPhysicalWorldSwitchDriver worldSwitchDriver)
    {SpatialDomain=spatialDomain;StateStore=stateStore;WorldPairStore=worldPairStore;DeepBoonSelectionStore=deepBoonSelectionStore;_worldContext=worldContext;PlacementHost=placementHost;TransitionHost=transitionHost;TransitionManager=transitionManager;RecoveryRuntime=recoveryRuntime;WorldSwitchCoordinator=worldSwitchCoordinator;WorldSwitchDriver=worldSwitchDriver;}
    internal UnderworldSpatialDomainDefinition SpatialDomain{get;} internal UnderworldTransitionStateStore StateStore{get;} internal UnderworldWorldPairManifestStore WorldPairStore{get;} internal UnderworldDeepBoonSelectionStore DeepBoonSelectionStore{get;} internal ValheimUnderworldTransitionPlacementHost PlacementHost{get;} internal StoredUnderworldTransitionHost TransitionHost{get;} internal UnderworldWorldTransitionManager TransitionManager{get;} internal UnderworldTransitionRecoveryRuntime RecoveryRuntime{get;} internal UnderworldPhysicalWorldSwitchCoordinator WorldSwitchCoordinator{get;} internal UnderworldPhysicalWorldSwitchDriver WorldSwitchDriver{get;}
    internal static UnderworldRuntimeServices Create(string pluginConfigDirectory,ManualLogSource log)
    {
        if(string.IsNullOrWhiteSpace(pluginConfigDirectory))throw new ArgumentException("Plugin config directory is required for Underworld persistence.",nameof(pluginConfigDirectory));if(log is null)throw new ArgumentNullException(nameof(log));var root=Path.Combine(Path.GetFullPath(pluginConfigDirectory),"Magenheim");var spatialDomain=UnderworldSpatialDomain.CreateDefault();var stateStore=new UnderworldTransitionStateStore(Path.Combine(root,"underworld-transitions"),log);var worldPairStore=new UnderworldWorldPairManifestStore(Path.Combine(root,"underworld-world-pairs"),log);var deepBoonSelectionStore=new UnderworldDeepBoonSelectionStore(Path.Combine(root,"underworld-deep-boon-selections"),log);var worldContext=new ValheimPhysicalUnderworldWorldContextController(worldPairStore,log);var placementHost=new ValheimUnderworldTransitionPlacementHost(worldContext,spatialDomain,log);var transitionHost=new StoredUnderworldTransitionHost(stateStore,placementHost);var transitionManager=new UnderworldWorldTransitionManager(transitionHost,log);var recoveryRuntime=new UnderworldTransitionRecoveryRuntime(stateStore,transitionManager,log);var worldSwitchCoordinator=new UnderworldPhysicalWorldSwitchCoordinator(worldPairStore,worldContext,log);var worldSwitchDriver=new UnderworldPhysicalWorldSwitchDriver(worldSwitchCoordinator,recoveryRuntime,log);return new UnderworldRuntimeServices(spatialDomain,stateStore,worldPairStore,deepBoonSelectionStore,worldContext,placementHost,transitionHost,transitionManager,recoveryRuntime,worldSwitchCoordinator,worldSwitchDriver);
    }
    internal bool TryResolveLocalSession(out UnderworldWorldIdentity? identity,out UnderworldLayer layer,out string playerId,out string diagnostic)
    {
        if(!UnderworldRuntimeIdentityResolver.TryResolveLocalSession(WorldPairStore,out identity,out layer,out playerId,out diagnostic)||identity is null)return false;
        if(layer==UnderworldLayer.Surface)
        {
            var world=ZNet.World;var saveName=world is null?string.Empty:UnderworldRuntimeIdentityResolver.ResolveWorldSaveName(world);if(string.IsNullOrWhiteSpace(saveName)){diagnostic="Surface world save identity is unavailable; reversible Underworld pairing cannot be established.";identity=null;playerId=string.Empty;return false;}WorldPairStore.EnsureManifest(identity,saveName);
        }
        return true;
    }
    internal bool TryResolveLocalSession(out UnderworldWorldIdentity? identity,out string playerId,out string diagnostic)=>TryResolveLocalSession(out identity,out _,out playerId,out diagnostic);
    internal void ResetForWorldUnload(){_worldContext.ResetForWorldUnload();WorldSwitchDriver.ResetForWorldUnload();}
    internal void Shutdown(){WorldSwitchDriver.AbandonForProcessShutdown();_worldContext.ResetForWorldUnload();}
}
