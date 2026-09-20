using System;
using BepInEx.Logging;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Owns physical-world lifetime for the Underworld instance infrastructure.
/// Player transfer/recovery is deliberately not reconciled here: the Deep Gate is a thin transport
/// adapter and Valheim remains authoritative for player/network lifecycle.
/// </summary>
internal sealed class UnderworldWorldSessionLifecycle : MonoBehaviour
{
    private UnderworldRuntimeServices? _services;
    private UnderworldDeepGateRegistrar? _deepGateRegistrar;
    private UnderworldDeepGateLocationRegistrar? _deepGateLocationRegistrar;
    private ManualLogSource? _log;
    private long? _observedWorldUid;
    private GameObject? _worldCenter;
    private string? _worldCenterInstanceKey;
    private float _nextBoonReconcileAt;

    internal void Configure(UnderworldRuntimeServices services, ManualLogSource log)
    {
        if (_services is not null) throw new InvalidOperationException("Underworld world-session lifecycle is already configured.");
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        DeepBoonRuntime.Configure(services, log);
        _deepGateRegistrar = new UnderworldDeepGateRegistrar(log);
        _deepGateRegistrar.Register();
        _deepGateLocationRegistrar = new UnderworldDeepGateLocationRegistrar(log);
        _deepGateLocationRegistrar.Register();
    }

    private void Update()
    {
        if (_services is null) return;
        var znet = ZNet.instance;
        if (znet is null)
        {
            if (_observedWorldUid.HasValue)
            {
                ResetWorldState();
                _log?.LogDebug($"Underworld physical session state reset after Valheim world {_observedWorldUid.Value} unloaded.");
                _observedWorldUid = null;
            }
            return;
        }

        var currentWorldUid = znet.GetWorldUID();
        if (!_observedWorldUid.HasValue)
        {
            _observedWorldUid = currentWorldUid;
            TryAdmitWorldCenter();
            TryReconcileDeepBoons();
            return;
        }

        if (_observedWorldUid.Value != currentWorldUid)
        {
            var previous = _observedWorldUid.Value;
            ResetWorldState();
            _observedWorldUid = currentWorldUid;
            _log?.LogInfo($"Underworld physical session boundary changed {previous} -> {currentWorldUid}.");
        }

        TryAdmitWorldCenter();
        TryReconcileDeepBoons();
    }

    private void TryAdmitWorldCenter()
    {
        if (_services is null || _log is null) return;
        if (_services.InstanceLifecycle.Phase != UnderworldInstancePhase.Active || _services.InstanceLifecycle.Identity is not { } identity)
        {
            if (_worldCenter) Destroy(_worldCenter);
            _worldCenter = null;
            _worldCenterInstanceKey = null;
            _services.ChunkStreaming.Clear();
            return;
        }

        var instanceKey = InstanceKey(identity);
        if (_worldCenter && string.Equals(_worldCenterInstanceKey, instanceKey, StringComparison.Ordinal)) return;
        if (_worldCenter) Destroy(_worldCenter);
        _worldCenter = null;
        _worldCenterInstanceKey = null;

        GameObject? candidate = null;
        try
        {
            var wasRecorded = _services.GeneratedObjectStateStore.IsRecorded(identity, UnderworldWorldCenterRegistrar.GeneratedObjectKind);
            candidate = UnderworldWorldCenterRegistrar.Create(identity, _log);
            var znet = ZNet.instance;
            if (znet is not null && znet.IsServer() && !wasRecorded)
                _services.GeneratedObjectStateStore.RecordGenerated(identity, UnderworldWorldCenterRegistrar.GeneratedObjectKind);
            _worldCenter = candidate;
            candidate = null;
            _worldCenterInstanceKey = instanceKey;
            _log.LogInfo($"{(wasRecorded ? "Restored" : "Generated")} Underworld world center for instance '{identity.DerivedWorldId}' through native generated-object registry.");
        }
        catch (Exception exception)
        {
            if (candidate) Destroy(candidate);
            _worldCenter = null;
            _worldCenterInstanceKey = null;
            _log.LogError($"Underworld native-instance center admission failed: {exception}");
        }
    }

    private void TryReconcileDeepBoons()
    {
        if (ZNet.instance is null || !ZNet.instance.IsServer() || Time.unscaledTime < _nextBoonReconcileAt) return;
        _nextBoonReconcileAt = Time.unscaledTime + 2f;
        foreach (var player in Player.GetAllPlayers())
            if (player) DeepBoonRuntime.Reconcile(player, out _);
    }

    private static string InstanceKey(UnderworldWorldIdentity identity) =>
        identity.ParentWorldId + "\n" + identity.DerivedWorldId + "\n" + identity.DerivedSeedFingerprint;

    private void ResetWorldState()
    {
        if (_worldCenter) Destroy(_worldCenter);
        _worldCenter = null;
        _worldCenterInstanceKey = null;
        DeepBoonRuntime.Reset();
        _nextBoonReconcileAt = 0f;
        _services?.ResetForWorldUnload();
    }

    private void OnDestroy()
    {
        _deepGateLocationRegistrar?.Dispose();
        _deepGateLocationRegistrar = null;
        _deepGateRegistrar?.Dispose();
        _deepGateRegistrar = null;
        ResetWorldState();
        _observedWorldUid = null;
    }
}
