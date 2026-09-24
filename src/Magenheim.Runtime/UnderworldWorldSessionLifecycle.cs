using System;
using System.Collections.Generic;
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
    private const float ChunkResidencyReconcileSeconds = 0.5f;

    private UnderworldRuntimeServices? _services;
    private UnderworldDeepGateRegistrar? _deepGateRegistrar;
    private UnderworldDeepGateLocationRegistrar? _deepGateLocationRegistrar;
    private ManualLogSource? _log;
    private long? _observedWorldUid;
    private GameObject? _worldCenter;
    private string? _worldCenterInstanceKey;
    private float _nextBoonReconcileAt;
    private float _nextChunkResidencyReconcileAt;

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
        UnderworldDevCommands.Register(log);
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
            TryAdmitInstanceAuthority(znet);
            TryAdmitWorldCenter();
            TryReconcileChunkResidency();
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

        TryAdmitInstanceAuthority(znet);
        TryAdmitWorldCenter();
        TryReconcileChunkResidency();
        TryReconcileDeepBoons();
    }

    private void TryAdmitInstanceAuthority(ZNet znet)
    {
        if (_services is null || _log is null) return;
        if (_services.InstanceLifecycle.Phase == UnderworldInstancePhase.Active) return;

        var world = ZNet.World;
        if (world is null) return;
        if (!UnderworldRuntimeIdentityResolver.TryResolveWorldIdentity(znet, world, out var identity, out var diagnostic) || identity is null)
        {
            _log.LogWarning($"Underworld instance admission is waiting for authoritative world identity: {diagnostic}");
            return;
        }

        try
        {
            _services.InstanceLifecycle.EnsureAdmitted(identity);
            _services.InstanceLifecycle.EnsureActive(identity);
            _log.LogInfo($"Admitted persistent Underworld instance authority '{identity.DerivedWorldId}' for parent world '{identity.ParentWorldId}'.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Underworld instance authority admission failed: {exception}");
        }
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
            candidate = UnderworldWorldCenterRegistrar.Create(identity, _log);
            if (!_services.ChunkStreaming.Reconcile(new[] { new UnderworldChunkFocus(0d, 0d) }))
                throw new InvalidOperationException("Native Underworld chunk residency rejected an active instance admission.");

            _worldCenter = candidate;
            candidate = null;
            _worldCenterInstanceKey = instanceKey;
            _log.LogInfo($"Composed Underworld world center and admitted native terrain chunks for instance '{identity.DerivedWorldId}'.");
        }
        catch (Exception exception)
        {
            if (candidate) Destroy(candidate);
            _services.ChunkStreaming.Clear();
            _worldCenter = null;
            _worldCenterInstanceKey = null;
            _log.LogError($"Underworld native-instance center admission failed: {exception}");
        }
    }

    private void TryReconcileChunkResidency()
    {
        if (_services is null || Time.unscaledTime < _nextChunkResidencyReconcileAt) return;
        _nextChunkResidencyReconcileAt = Time.unscaledTime + ChunkResidencyReconcileSeconds;

        if (_services.InstanceLifecycle.Phase != UnderworldInstancePhase.Active || _services.InstanceLifecycle.Identity is null)
            return;

        // The origin remains resident for the conclave/return gate. Each player's transform is
        // admitted independently only when it occupies the disjoint engine-space Underworld layer.
        // A Surface player can therefore never make another player's layer classification global.
        var focuses = new List<UnderworldChunkFocus> { new(0d, 0d) };
        foreach (var player in Player.GetAllPlayers())
        {
            if (!player || !UnderworldInstanceLayer.IsUnderworldEnginePosition(player.transform.position)) continue;
            var position = UnderworldInstanceLayer.ToLogical(player.transform.position);
            focuses.Add(new UnderworldChunkFocus(position.x, position.z));
        }

        try
        {
            if (!_services.ChunkStreaming.Reconcile(focuses))
                _log?.LogWarning("Native Underworld chunk residency rejected an active instance-space focus reconciliation.");
        }
        catch (Exception exception)
        {
            _log?.LogError($"Underworld native chunk residency reconciliation failed: {exception}");
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
        _nextChunkResidencyReconcileAt = 0f;
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
