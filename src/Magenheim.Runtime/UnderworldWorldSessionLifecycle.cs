using System;
using BepInEx.Logging;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

internal sealed class UnderworldWorldSessionLifecycle : MonoBehaviour
{
    private UnderworldRuntimeServices? _services;private UnderworldDeepGateRegistrar? _deepGateRegistrar;private UnderworldDeepGateLocationRegistrar? _deepGateLocationRegistrar;private ManualLogSource? _log;private long? _observedWorldUid;private GameObject? _worldCenter;private string? _worldCenterIdentity;private float _nextBoonReconcileAt;
    internal void Configure(UnderworldRuntimeServices services,ManualLogSource log){if(_services is not null)throw new InvalidOperationException("Underworld world-session lifecycle is already configured.");_services=services??throw new ArgumentNullException(nameof(services));_log=log??throw new ArgumentNullException(nameof(log));DeepBoonRuntime.Configure(services,log);_deepGateRegistrar=new UnderworldDeepGateRegistrar(log);_deepGateRegistrar.Register();_deepGateLocationRegistrar=new UnderworldDeepGateLocationRegistrar(log);_deepGateLocationRegistrar.Register();}
    private void Update()
    {
        if(_services is null)return;var znet=ZNet.instance;
        if(znet is null){if(_observedWorldUid.HasValue){ResetWorldState();_log?.LogDebug($"Underworld physical session state reset after Valheim world {_observedWorldUid.Value} unloaded.");_observedWorldUid=null;}return;}
        var currentWorldUid=znet.GetWorldUID();
        if(!_observedWorldUid.HasValue){_observedWorldUid=currentWorldUid;TryAdmitWorldCenter();TryReconcileDeepBoons();return;}
        if(_observedWorldUid.Value!=currentWorldUid){var previous=_observedWorldUid.Value;ResetWorldState();_observedWorldUid=currentWorldUid;_log?.LogInfo($"Underworld physical session boundary changed {previous} -> {currentWorldUid}.");}
        TryAdmitWorldCenter();TryReconcileDeepBoons();
    }
    private void TryAdmitWorldCenter()
    {
        if(_services is null||_log is null)return;
        if(!_services.TryResolveLocalSession(out var identity,out var layer,out _,out _)||identity is null)return;
        if(layer!=UnderworldLayer.Underworld){if(_worldCenter)Destroy(_worldCenter);_worldCenter=null;_worldCenterIdentity=null;return;}
        if(_worldCenter&&string.Equals(_worldCenterIdentity,identity.DerivedWorldId,StringComparison.Ordinal))return;
        if(_worldCenter)Destroy(_worldCenter);
        try{_worldCenter=UnderworldWorldCenterRegistrar.Create(identity,_services.SpatialDomain,_log);_worldCenterIdentity=identity.DerivedWorldId;_log.LogInfo($"Admitted Underworld world center only for physical derived session '{identity.DerivedWorldId}'.");}
        catch(Exception exception){_worldCenter=null;_worldCenterIdentity=null;_log.LogError($"Underworld reserved-domain center admission failed: {exception}");}
    }
    private void TryReconcileDeepBoons()
    {
        if(ZNet.instance is null||!ZNet.instance.IsServer()||Time.unscaledTime<_nextBoonReconcileAt)return;_nextBoonReconcileAt=Time.unscaledTime+2f;
        foreach(var player in Player.GetAllPlayers())if(player)DeepBoonRuntime.Reconcile(player,out _);
    }
    private void ResetWorldState(){if(_worldCenter)Destroy(_worldCenter);_worldCenter=null;_worldCenterIdentity=null;DeepBoonRuntime.Reset();_nextBoonReconcileAt=0f;_services?.ResetForWorldUnload();}
    private void OnDestroy(){_deepGateLocationRegistrar?.Dispose();_deepGateLocationRegistrar=null;_deepGateRegistrar?.Dispose();_deepGateRegistrar=null;ResetWorldState();_observedWorldUid=null;}
}
