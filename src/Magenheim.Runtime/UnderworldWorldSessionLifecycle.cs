using System;
using BepInEx.Logging;
using UnityEngine;

namespace Magenheim.Runtime;

internal sealed class UnderworldWorldSessionLifecycle : MonoBehaviour
{
    private UnderworldRuntimeServices? _services;private UnderworldDeepGateRegistrar? _deepGateRegistrar;private UnderworldDeepGateLocationRegistrar? _deepGateLocationRegistrar;private ManualLogSource? _log;private long? _observedWorldUid;private GameObject? _worldCenter;private string? _worldCenterIdentity;
    internal void Configure(UnderworldRuntimeServices services,ManualLogSource log)
    {
        if(_services is not null)throw new InvalidOperationException("Underworld world-session lifecycle is already configured.");_services=services??throw new ArgumentNullException(nameof(services));_log=log??throw new ArgumentNullException(nameof(log));
        _deepGateRegistrar=new UnderworldDeepGateRegistrar(log);_deepGateRegistrar.Register();_deepGateLocationRegistrar=new UnderworldDeepGateLocationRegistrar(log);_deepGateLocationRegistrar.Register();
    }
    private void Update()
    {
        if(_services is null)return;var znet=ZNet.instance;
        if(znet is null){if(_observedWorldUid.HasValue){ResetWorldState();_log?.LogDebug($"Underworld logical context reset after Valheim world {_observedWorldUid.Value} unloaded.");_observedWorldUid=null;}return;}
        var currentWorldUid=znet.GetWorldUID();
        if(!_observedWorldUid.HasValue){_observedWorldUid=currentWorldUid;TryAdmitWorldCenter();return;}
        if(_observedWorldUid.Value!=currentWorldUid){var previous=_observedWorldUid.Value;ResetWorldState();_observedWorldUid=currentWorldUid;_log?.LogInfo($"Underworld logical context reset for Valheim world change {previous} -> {currentWorldUid}.");}
        TryAdmitWorldCenter();
    }
    private void TryAdmitWorldCenter()
    {
        if(_services is null||_log is null||_worldCenter)return;
        if(!_services.TryResolveLocalSession(out var identity,out _,out _ )||identity is null)return;
        if(string.Equals(_worldCenterIdentity,identity.DerivedWorldId,StringComparison.Ordinal))return;
        try{_worldCenter=UnderworldWorldCenterRegistrar.Create(identity,_services.SpatialDomain,_log);_worldCenterIdentity=identity.DerivedWorldId;}
        catch(Exception exception){_log.LogError($"Underworld reserved-domain center admission failed: {exception}");}
    }
    private void ResetWorldState()
    {
        if(_worldCenter)Destroy(_worldCenter);_worldCenter=null;_worldCenterIdentity=null;_services?.ResetForWorldUnload();
    }
    private void OnDestroy(){_deepGateLocationRegistrar?.Dispose();_deepGateLocationRegistrar=null;_deepGateRegistrar?.Dispose();_deepGateRegistrar=null;ResetWorldState();_observedWorldUid=null;}
}
