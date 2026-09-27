using System;
using BepInEx.Logging;
using Magenheim.Core.Underworld;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    private UnderworldNativeWorldHost? _nativeWorldHost;
    private long? _observedWorldUid;
    private GameObject? _worldCenter;
    private string? _worldCenterInstanceKey;
    private float _nextBoonReconcileAt;
    private bool _physicsSimulationFaulted;

    internal void Configure(UnderworldRuntimeServices services, ManualLogSource log)
    {
        if (_services is not null) throw new InvalidOperationException("Underworld world-session lifecycle is already configured.");
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _nativeWorldHost = new UnderworldNativeWorldHost(services.WorldInstances, log);
        DeepBoonRuntime.Configure(services, log);
        _deepGateRegistrar = new UnderworldDeepGateRegistrar(log);
        _deepGateRegistrar.Register();
        _deepGateLocationRegistrar = new UnderworldDeepGateLocationRegistrar(log);
        _deepGateLocationRegistrar.Register();
        UnderworldDevCommands.Register(log);
    }

    private void FixedUpdate()
    {
        if (_physicsSimulationFaulted || _services is null) return;
        if (!_services.WorldInstances.TryGetContext(UnderworldWorldInstanceId.Underworld, out var context) ||
            context is null || !context.PhysicsScene.IsValid())
            return;

        try
        {
            // Unity does not automatically advance non-default local PhysicsScenes. The Underworld
            // owns one deliberately so identical logical coordinates cannot collide with Surface.
            context.PhysicsScene.Simulate(Time.fixedDeltaTime);
        }
        catch (Exception exception)
        {
            _physicsSimulationFaulted = true;
            _log?.LogError("Underworld local PhysicsScene simulation failed; instance physics is disabled until the world session resets: " + exception);
        }
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
            TickUnderworldPathfinding();
            UnderworldZdoPeerRouter.TickUnderworld(Time.deltaTime);
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
        TickUnderworldPathfinding();
        UnderworldZdoPeerRouter.TickUnderworld(Time.deltaTime);
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
            if (ZNet.World is not null && WorldGenerator.instance is not null && ZoneSystem.instance is not null && ZDOMan.instance is not null)
                _services.WorldInstances.BindSurface(ZNet.World, WorldGenerator.instance, ZoneSystem.instance, ZDOMan.instance);

            if (_nativeWorldHost is null || !_nativeWorldHost.TryCreate(identity, out var hostDiagnostic))
                throw new InvalidOperationException(hostDiagnostic);

            UnderworldInstancePersistence.TryLoadBoundInstance();
            var admission = UnderworldInstanceAdmissionAudit.Validate(_services, identity);
            _services.InstanceLifecycle.EnsureActive(identity);
            _log.LogInfo(
                $"Admitted persistent Underworld instance authority '{identity.DerivedWorldId}' for parent world '{identity.ParentWorldId}'. " +
                $"Admission audit: {admission}");
        }
        catch (Exception exception)
        {
            _log.LogError($"Underworld instance authority admission failed: {exception}");
        }
    }

    private void TryAdmitWorldCenter()
    {
        if (_services is null || _log is null) return;
        if (_services.InstanceLifecycle.Phase != UnderworldInstancePhase.Active ||
            _services.InstanceLifecycle.Identity is not { } identity ||
            !_services.WorldInstances.TryGetContext(UnderworldWorldInstanceId.Underworld, out var context) ||
            context is null)
        {
            if (_worldCenter) Destroy(_worldCenter);
            _worldCenter = null;
            _worldCenterInstanceKey = null;
            return;
        }

        var instanceKey = InstanceKey(identity);
        if (_worldCenter && string.Equals(_worldCenterInstanceKey, instanceKey, StringComparison.Ordinal)) return;
        if (_worldCenter) Destroy(_worldCenter);
        _worldCenter = null;
        _worldCenterInstanceKey = null;

        GameObject? candidate = null;
        var previousScene = SceneManager.GetActiveScene();
        try
        {
            using (ValheimWorldInstanceExecution.Enter(context))
            {
                if (!SceneManager.SetActiveScene(context.Scene))
                    throw new InvalidOperationException("Unable to activate the Underworld Unity scene for world-center composition.");
                candidate = UnderworldWorldCenterRegistrar.Create(identity, _log);
            }

            _worldCenter = candidate;
            candidate = null;
            _worldCenterInstanceKey = instanceKey;
            _log.LogInfo($"Composed Underworld world center directly in native instance scene '{context.Scene.name}'.");
        }
        catch (Exception exception)
        {
            if (candidate) Destroy(candidate);
            _worldCenter = null;
            _worldCenterInstanceKey = null;
            _log.LogError($"Underworld native world-center admission failed: {exception}");
        }
        finally
        {
            if (previousScene.IsValid() && previousScene.isLoaded) SceneManager.SetActiveScene(previousScene);
        }
    }

    private void TickUnderworldPathfinding()
    {
        if (_services is null ||
            !_services.WorldInstances.TryGetContext(UnderworldWorldInstanceId.Underworld, out var context) ||
            context is null ||
            context.PathfindingState is null ||
            Pathfinding.instance is null)
            return;

        try
        {
            using (ValheimWorldInstanceExecution.Enter(context))
                context.PathfindingState.TickActive(Pathfinding.instance);
        }
        catch (Exception exception)
        {
            _log?.LogError("Underworld native pathfinding tick failed: " + exception);
        }
    }

    private void TryReconcileDeepBoons()
    {
        if (ZNet.instance is null || !ZNet.instance.IsServer() || Time.unscaledTime < _nextBoonReconcileAt) return;
        _nextBoonReconcileAt = Time.unscaledTime + 2f;
        foreach (var player in Player.GetAllPlayers())
        {
            if (!player) continue;
            try { _services?.WorldInstances.GetOrBindSurface(player.GetPlayerID()); }
            catch (InvalidOperationException) { }
            DeepBoonRuntime.Reconcile(player, out _);
        }
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
        _physicsSimulationFaulted = false;
        _nativeWorldHost?.Dispose();
        _nativeWorldHost = _services is null || _log is null ? null : new UnderworldNativeWorldHost(_services.WorldInstances, _log);
        UnderworldInstancePersistence.Reset();
        UnderworldZoneInstanceState.Reset();
        _services?.ResetForWorldUnload();
    }

    private void OnDestroy()
    {
        _deepGateLocationRegistrar?.Dispose();
        _deepGateLocationRegistrar = null;
        _deepGateRegistrar?.Dispose();
        _deepGateRegistrar = null;
        ResetWorldState();
        _nativeWorldHost?.Dispose();
        _nativeWorldHost = null;
        _observedWorldUid = null;
    }
}
