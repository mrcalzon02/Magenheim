using System;
using BepInEx.Logging;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Registers the canonical Magenheim-owned Underworld vessel admitted by Core.
/// The initial skiff deliberately clones a stable vanilla small-vessel prefab;
/// its identity, not its source prefab, is the authority used by Deep Current.
/// </summary>
internal sealed class UnderworldVesselRegistrar : IDisposable
{
    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal UnderworldVesselRegistrar(ManualLogSource log) =>
        _log = log ?? throw new ArgumentNullException(nameof(log));

    internal void Register()
    {
        if (_subscribed || _registered) return;
        PrefabManager.OnVanillaPrefabsAvailable += RegisterContent;
        _subscribed = true;
    }

    private void RegisterContent()
    {
        if (_registered) return;
        try
        {
            const string sourcePrefab = "Raft";
            var identity = UnderworldVesselCatalog.BlackwaterSkiff;
            if (PrefabManager.Instance.GetPrefab(identity) is not null)
                throw new InvalidOperationException($"Cannot replace occupied Underworld vessel identity '{identity}'.");
            if (PrefabManager.Instance.GetPrefab(sourcePrefab) is null)
                throw new InvalidOperationException($"Required Underworld vessel source prefab '{sourcePrefab}' is unavailable.");

            var vessel = PrefabManager.Instance.CreateClonedPrefab(identity, sourcePrefab)
                ?? throw new InvalidOperationException($"Unable to clone Underworld vessel source '{sourcePrefab}' as '{identity}'.");

            // Jotunn's AddPrefab(GameObject) overload returns void, so confirm the registration
            // took by reading the prefab back rather than testing a non-existent result.
            PrefabManager.Instance.AddPrefab(vessel);
            if (PrefabManager.Instance.GetPrefab(identity) == null)
                throw new InvalidOperationException($"Jotunn refused Underworld vessel '{identity}'.");

            _registered = true;
            _log.LogInfo($"Registered canonical Underworld vessel '{identity}' from stable vanilla vessel source '{sourcePrefab}'.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Underworld vessel registration failed: {exception}");
            throw;
        }
        finally
        {
            Dispose();
        }
    }

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterContent;
        _subscribed = false;
    }
}
