using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx.Logging;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

internal sealed class UnderworldWorldSessionLifecycle : MonoBehaviour
{
    private UnderworldRuntimeServices? _services;private UnderworldDeepGateRegistrar? _deepGateRegistrar;private UnderworldDeepGateLocationRegistrar? _deepGateLocationRegistrar;private ManualLogSource? _log;private long? _observedWorldUid;private GameObject? _worldCenter;private string? _worldCenterInstanceKey;private string? _gameplayAuthorityFingerprint;private float _nextTransitionReconcileAt;private float _nextBoonReconcileAt;private float _nextChunkReconcileAt;private bool _placedLocalUnderworldPlayer;private readonly HashSet<string> _reconciledTransitionPlayers=new(StringComparer.Ordinal);
    internal void Configure(UnderworldRuntimeServices services,ManualLogSource log){if(_services is not null)throw new InvalidOperationException("Underworld world-session lifecycle is already configured.");_services=services??throw new ArgumentNullException(nameof(services));_log=log??throw new ArgumentNullException(nameof(log));DeepBoonRuntime.Configure(services,log);_deepGateRegistrar=new UnderworldDeepGateRegistrar(log);_deepGateRegistrar.Register();_deepGateLocationRegistrar=new UnderworldDeepGateLocationRegistrar(log);_deepGateLocationRegistrar.Register();}
    internal void SetGameplayAuthorityFingerprint(string fingerprint){if(string.IsNullOrWhiteSpace(fingerprint))throw new ArgumentException("Gameplay authority fingerprint is required.",nameof(fingerprint));if(_gameplayAuthorityFingerprint is not null&&!string.Equals(_gameplayAuthorityFingerprint,fingerprint,StringComparison.Ordinal))throw new InvalidOperationException("Underworld session lifecycle gameplay authority cannot change while the plugin is active.");_gameplayAuthorityFingerprint=fingerprint;}
    private void Update()
    {
        if(_services is null)return;var znet=ZNet.instance;
        if(znet is null){if(_observedWorldUid.HasValue){ResetWorldState();_log?.LogDebug($"Underworld physical session state reset after Valheim world {_observedWorldUid.Value} unloaded.");_observedWorldUid=null;}return;}
        var currentWorldUid=znet.GetWorldUID();
        if(!_observedWorldUid.HasValue){_observedWorldUid=currentWorldUid;TryReconcileTransitions();TryAdmitWorldCenter();TryPlaceLocalUnderworldPlayer();TryReconcileChunks();TryReconcileDeepBoons();return;}
        if(_observedWorldUid.Value!=currentWorldUid){var previous=_observedWorldUid.Value;ResetWorldState();_observedWorldUid=currentWorldUid;_log?.LogInfo($"Underworld physical session boundary changed {previous} -> {currentWorldUid}.");}
        TryReconcileTransitions();TryAdmitWorldCenter();TryPlaceLocalUnderworldPlayer();TryReconcileChunks();TryReconcileDeepBoons();
    }
    private void TryReconcileTransitions()
    {
        var fingerprint=_gameplayAuthorityFingerprint;
        if(_services is null||_log is null||fingerprint is null||fingerprint.Trim().Length==0||Time.unscaledTime<_nextTransitionReconcileAt)return;
        _nextTransitionReconcileAt=Time.unscaledTime+2f;
        var znet=ZNet.instance;var world=ZNet.World;
        if(znet is null||world is null||!znet.IsServer())return;
        if(!UnderworldRuntimeIdentityResolver.TryResolveWorldIdentity(znet,world,out var identity,out var sessionDiagnostic)||identity is null){_log.LogDebug($"Underworld server transition recovery deferred: {sessionDiagnostic}");return;}
        foreach(var player in Player.GetAllPlayers())
        {
            if(!player)continue;
            var playerId=player.GetPlayerID().ToString(CultureInfo.InvariantCulture);
            if(string.IsNullOrWhiteSpace(playerId)||_reconciledTransitionPlayers.Contains(playerId))continue;
            var result=_services.RecoveryRuntime.LoadAndResume(playerId,identity,fingerprint,out _,out var diagnostic);
            if(result==UnderworldRecoveryLoadResult.Failed){_log.LogError($"Underworld transition admission for player {playerId} failed closed and will retry: {diagnostic}");continue;}
            _reconciledTransitionPlayers.Add(playerId);
            _log.LogDebug($"Underworld transition recovery reconciled player {playerId}: {diagnostic}");
        }
    }
    private void TryAdmitWorldCenter()
    {
        if(_services is null||_log is null)return;
        if(!_services.TryResolveWorldSession(out var identity,out var layer,out _)||identity is null)return;
        if(layer!=UnderworldLayer.Underworld){if(_worldCenter)Destroy(_worldCenter);_worldCenter=null;_worldCenterInstanceKey=null;_services.ChunkStreaming.Clear();return;}
        var instanceKey=InstanceKey(identity);
        if(_worldCenter&&string.Equals(_worldCenterInstanceKey,instanceKey,StringComparison.Ordinal))return;
        if(_worldCenter)Destroy(_worldCenter);
        try{_worldCenter=UnderworldWorldCenterRegistrar.Create(identity,_log);_worldCenterInstanceKey=instanceKey;_log.LogInfo($"Admitted Underworld world center only for physical derived session '{identity.DerivedWorldId}'.");}
        catch(Exception exception){_worldCenter=null;_worldCenterInstanceKey=null;_log.LogError($"Underworld native-instance center admission failed: {exception}");}
    }
    private void TryPlaceLocalUnderworldPlayer()
    {
        if(_placedLocalUnderworldPlayer||_worldCenter is null||_services is null||_log is null)return;
        if(!_services.TryResolveLocalSession(out var identity,out var layer,out _,out _)||identity is null||layer!=UnderworldLayer.Underworld)return;
        var player=Player.m_localPlayer;if(player is null)return;
        var target=_worldCenter.transform.position+new Vector3(0f,2.5f,-5f);
        try{player.TeleportTo(target,_worldCenter.transform.rotation,true);_placedLocalUnderworldPlayer=true;_log.LogInfo($"Placed local player at grounded Underworld Conclave test arrival ({target.x:0.0}, {target.y:0.0}, {target.z:0.0}).");}
        catch(Exception exception){_log.LogWarning($"Underworld test arrival placement deferred: {exception.Message}");}
    }
    private void TryReconcileChunks()
    {
        if(_services is null||Time.unscaledTime<_nextChunkReconcileAt)return;_nextChunkReconcileAt=Time.unscaledTime+0.5f;
        if(!_services.TryResolveWorldSession(out var identity,out var layer,out _)||identity is null||layer!=UnderworldLayer.Underworld){_services.ChunkStreaming.Clear();return;}
        var focuses=new List<(double X,double Z)>();
        var znet=ZNet.instance;
        if(znet is not null&&znet.IsServer())
        {
            foreach(var player in Player.GetAllPlayers())if(player){var position=player.transform.position;focuses.Add((position.x,position.z));}
        }
        else
        {
            var player=Player.m_localPlayer;if(player){var position=player.transform.position;focuses.Add((position.x,position.z));}
        }
        _services.ChunkStreaming.Reconcile(focuses);
    }
    private void TryReconcileDeepBoons()
    {
        if(ZNet.instance is null||!ZNet.instance.IsServer()||Time.unscaledTime<_nextBoonReconcileAt)return;_nextBoonReconcileAt=Time.unscaledTime+2f;
        foreach(var player in Player.GetAllPlayers())if(player)DeepBoonRuntime.Reconcile(player,out _);
    }
    private static string InstanceKey(UnderworldWorldIdentity identity)=>identity.ParentWorldId+"\n"+identity.DerivedWorldId+"\n"+identity.DerivedSeedFingerprint;
    private void ResetWorldState(){if(_worldCenter)Destroy(_worldCenter);_worldCenter=null;_worldCenterInstanceKey=null;_reconciledTransitionPlayers.Clear();DeepBoonRuntime.Reset();_nextTransitionReconcileAt=0f;_nextBoonReconcileAt=0f;_nextChunkReconcileAt=0f;_placedLocalUnderworldPlayer=false;_services?.ResetForWorldUnload();}
    private void OnDestroy(){_deepGateLocationRegistrar?.Dispose();_deepGateLocationRegistrar=null;_deepGateRegistrar?.Dispose();_deepGateRegistrar=null;ResetWorldState();_observedWorldUid=null;}
}
