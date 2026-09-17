using System;
using BepInEx.Logging;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

internal sealed class UnderworldWorldSessionLifecycle : MonoBehaviour
{
    private UnderworldRuntimeServices? _services;private UnderworldDeepGateRegistrar? _deepGateRegistrar;private UnderworldDeepGateLocationRegistrar? _deepGateLocationRegistrar;private ManualLogSource? _log;private long? _observedWorldUid;private long? _reconciledWorldUid;private GameObject? _worldCenter;private string? _worldCenterIdentity;private string? _gameplayAuthorityFingerprint;private float _nextBoonReconcileAt;private bool _placedLocalUnderworldPlayer;
    internal void Configure(UnderworldRuntimeServices services,ManualLogSource log){if(_services is not null)throw new InvalidOperationException("Underworld world-session lifecycle is already configured.");_services=services??throw new ArgumentNullException(nameof(services));_log=log??throw new ArgumentNullException(nameof(log));DeepBoonRuntime.Configure(services,log);_deepGateRegistrar=new UnderworldDeepGateRegistrar(log);_deepGateRegistrar.Register();_deepGateLocationRegistrar=new UnderworldDeepGateLocationRegistrar(log);_deepGateLocationRegistrar.Register();}
    internal void SetGameplayAuthorityFingerprint(string fingerprint){if(string.IsNullOrWhiteSpace(fingerprint))throw new ArgumentException("Gameplay authority fingerprint is required.",nameof(fingerprint));if(_gameplayAuthorityFingerprint is not null&&!string.Equals(_gameplayAuthorityFingerprint,fingerprint,StringComparison.Ordinal))throw new InvalidOperationException("Underworld session lifecycle gameplay authority cannot change while the plugin is active.");_gameplayAuthorityFingerprint=fingerprint;}
    private void Update()
    {
        if(_services is null)return;var znet=ZNet.instance;
        if(znet is null){if(_observedWorldUid.HasValue){ResetWorldState();_log?.LogDebug($"Underworld physical session state reset after Valheim world {_observedWorldUid.Value} unloaded.");_observedWorldUid=null;}return;}
        var currentWorldUid=znet.GetWorldUID();
        if(!_observedWorldUid.HasValue){_observedWorldUid=currentWorldUid;TryReconcileTransition(currentWorldUid);TryAdmitWorldCenter();TryPlaceLocalUnderworldPlayer();TryReconcileDeepBoons();return;}
        if(_observedWorldUid.Value!=currentWorldUid){var previous=_observedWorldUid.Value;ResetWorldState();_observedWorldUid=currentWorldUid;_log?.LogInfo($"Underworld physical session boundary changed {previous} -> {currentWorldUid}.");}
        TryReconcileTransition(currentWorldUid);TryAdmitWorldCenter();TryPlaceLocalUnderworldPlayer();TryReconcileDeepBoons();
    }
    private void TryReconcileTransition(long currentWorldUid)
    {
        var fingerprint=_gameplayAuthorityFingerprint;
        if(_services is null||_log is null||_reconciledWorldUid==currentWorldUid||fingerprint is null||fingerprint.Trim().Length==0)return;
        var znet=ZNet.instance;
        if(znet is null||!znet.IsServer()){_reconciledWorldUid=currentWorldUid;return;}
        if(!_services.TryResolveLocalSession(out var identity,out _,out var playerId,out var sessionDiagnostic)||identity is null){_log.LogDebug($"Underworld transition admission deferred: {sessionDiagnostic}");return;}
        var result=_services.WorldSwitchDriver.ReconcileAdmittedSession(playerId,identity,fingerprint,out _,out var pending,out var diagnostic);
        if(result==UnderworldRecoveryLoadResult.Failed){_log.LogError($"Underworld transition admission failed closed: {diagnostic}");_reconciledWorldUid=currentWorldUid;return;}
        if(result==UnderworldRecoveryLoadResult.PendingWorldSwitch){_log.LogWarning(pending is null?$"Underworld transition remains pending after admission: {diagnostic}":$"Underworld transition awaits physical save '{pending.TargetSaveName}': {diagnostic}");_reconciledWorldUid=currentWorldUid;return;}
        _log.LogDebug($"Underworld transition admission reconciled: {diagnostic}");_reconciledWorldUid=currentWorldUid;
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
    private void TryPlaceLocalUnderworldPlayer()\n    {\n        if(_placedLocalUnderworldPlayer||_worldCenter is null||_services is null||_log is null)return;\n        if(!_services.TryResolveLocalSession(out var identity,out var layer,out _,out _)||identity is null||layer!=UnderworldLayer.Underworld)return;\n        var player=Player.m_localPlayer;if(player is null)return;\n        var target=_worldCenter.transform.position+new Vector3(0f,2.5f,-5f);\n        try{player.TeleportTo(target,_worldCenter.transform.rotation,true);_placedLocalUnderworldPlayer=true;_log.LogInfo($"Placed local player at grounded Underworld Conclave test arrival ({target.x:0.0}, {target.y:0.0}, {target.z:0.0}).");}\n        catch(Exception exception){_log.LogWarning($"Underworld test arrival placement deferred: {exception.Message}");}\n    }\n    private void TryReconcileDeepBoons()
    {
        if(ZNet.instance is null||!ZNet.instance.IsServer()||Time.unscaledTime<_nextBoonReconcileAt)return;_nextBoonReconcileAt=Time.unscaledTime+2f;
        foreach(var player in Player.GetAllPlayers())if(player)DeepBoonRuntime.Reconcile(player,out _);
    }
    private void ResetWorldState(){if(_worldCenter)Destroy(_worldCenter);_worldCenter=null;_worldCenterIdentity=null;_reconciledWorldUid=null;DeepBoonRuntime.Reset();_nextBoonReconcileAt=0f;_placedLocalUnderworldPlayer=false;_services?.ResetForWorldUnload();}
    private void OnDestroy(){_deepGateLocationRegistrar?.Dispose();_deepGateLocationRegistrar=null;_deepGateRegistrar?.Dispose();_deepGateRegistrar=null;ResetWorldState();_observedWorldUid=null;}
}
