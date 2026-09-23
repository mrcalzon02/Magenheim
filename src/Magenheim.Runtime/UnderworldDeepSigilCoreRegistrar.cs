using System;
using BepInEx.Logging;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Registers the persistent network object used by Deep Sigil interactions.
/// The object deliberately remains a normal Valheim/Jotunn prefab with ZNetView/ZDO ownership;
/// Magenheim only adds its instance-aware state adapter and does not create a parallel save path.
/// </summary>
internal sealed class UnderworldDeepSigilCoreRegistrar : IDisposable
{
    internal const string PrefabName = "Magenheim_UnderworldDeepSigilCore";
    private const string DonorPrefab = "Pickable_Stone";

    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal UnderworldDeepSigilCoreRegistrar(ManualLogSource log) =>
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
            if (PrefabManager.Instance.GetPrefab(PrefabName) is not null)
                throw new InvalidOperationException($"Cannot replace occupied Deep Sigil core identity '{PrefabName}'.");

            var donor = PrefabManager.Instance.GetPrefab(DonorPrefab)
                ?? throw new InvalidOperationException($"Required Deep Sigil persistent donor '{DonorPrefab}' is unavailable.");
            if (donor.GetComponent<ZNetView>() is null)
                throw new InvalidOperationException($"Deep Sigil donor '{DonorPrefab}' is not a persistent Valheim network object.");

            var core = PrefabManager.Instance.CreateClonedPrefab(PrefabName, donor)
                ?? throw new InvalidOperationException($"Unable to clone Deep Sigil core donor '{DonorPrefab}'.");

            // The donor is selected only for its proven persistent ZNetView/ZDO lifecycle.
            // Sigils are not harvestable stones; remove donor gameplay while retaining its network authority.
            var pickable = core.GetComponent<Pickable>();
            if (pickable is not null) UnityEngine.Object.DestroyImmediate(pickable);
            if (core.GetComponent<UnderworldZdoStateAdapter>() is null)
                core.AddComponent<UnderworldZdoStateAdapter>();

            PrefabManager.Instance.AddPrefab(new CustomPrefab(core, true));
            var registered = PrefabManager.Instance.GetPrefab(PrefabName)
                ?? throw new InvalidOperationException($"Jotunn refused Deep Sigil core '{PrefabName}'.");
            if (registered.GetComponent<ZNetView>() is null || registered.GetComponent<UnderworldZdoStateAdapter>() is null)
                throw new InvalidOperationException("Deep Sigil core registration lost its required ZNetView/state-adapter components.");

            _registered = true;
            _log.LogInfo($"Registered persistent Underworld Deep Sigil core '{PrefabName}' through ordinary Valheim ZNetView/ZDO authority.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Underworld Deep Sigil core registration failed: {exception}");
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
