using System;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Player-facing Deep Sigil interaction. Interaction requests travel through the core's normal
/// ZNetView owner RPC; only the authoritative server owner resolves definitions and mutates ZDO
/// discovery state. The Sigil persists a stable unique-location identity, never a coordinate.
/// A transient reply gives the activating client the resolved native-instance anchor so the existing
/// Valheim Minimap can own the actual discovery pin. Persisted discovery is reconstructed from that
/// stable identity when a discovered Sigil is loaded again; coordinates remain derived state.
/// </summary>
internal sealed class UnderworldDeepSigilInteraction : MonoBehaviour, Hoverable, Interactable
{
    private const string ActivateRpc = "Magenheim_DeepSigil_Activate";
    private const string RevealRpc = "Magenheim_DeepSigil_Reveal";
    private const string DiscoveredKey = "deep-sigil.discovered";
    private const string LocationIdKey = "deep-sigil.location-id";
    private const string BossIdKey = "deep-sigil.boss-id";
    private const string FractureBiomeId = "magenheim.underworld.biome.fracture";
    private const string FracturePinName = "$magenheim_underworld_suspended_court";

    private ZNetView _view = null!;
    private UnderworldZdoStateAdapter _state = null!;
    private string _biomeId = string.Empty;
    private bool _restoredPersistedDiscovery;

    internal void Configure(string biomeId)
    {
        if (string.IsNullOrWhiteSpace(biomeId))
            throw new ArgumentException("Deep Sigil biome identity is required.", nameof(biomeId));
        _biomeId = biomeId.Trim();
    }

    private void Awake()
    {
        _view = GetComponent<ZNetView>();
        _state = GetComponent<UnderworldZdoStateAdapter>();
        if (_view == null || _state == null)
            throw new InvalidOperationException("Deep Sigil interaction requires ZNetView and UnderworldZdoStateAdapter on the same persistent core.");
        _view.Register(ActivateRpc, new Action<long>(RpcActivate));
        _view.Register<Vector3, string>(RevealRpc, RpcReveal);
    }

    private void Update()
    {
        if (_restoredPersistedDiscovery || _state == null || !_state.IsReady ||
            !string.Equals(_biomeId, FractureBiomeId, StringComparison.Ordinal) ||
            !_state.GetBool(DiscoveredKey))
            return;

        var locationId = _state.GetString(LocationIdKey);
        if (string.IsNullOrWhiteSpace(locationId)) return;

        // The ZDO owns only the canonical identity. Re-derive the active world's anchor after load
        // so stale coordinates can never survive a world/seed change. Delay completion until the
        // native instance and vanilla Minimap are both ready.
        if (Minimap.instance == null || UnderworldRuntimeServices.Current?.InstanceLifecycle.Identity == null)
            return;

        try
        {
            var anchor = UnderworldTerrainRuntime.ResolveUniqueLocation(
                locationId,
                UnderworldTerrainBiome.FractureZones);
            EnsureDiscoveryPin(anchor.EnginePosition, FracturePinName);
            _restoredPersistedDiscovery = true;
        }
        catch (InvalidOperationException)
        {
            // Native terrain admission can trail ZDO replication during instance startup. Retry on
            // a later frame rather than treating a transient load-order gap as lost discovery.
        }
    }

    public string GetHoverName() => "$magenheim_deep_sigil";

    public string GetHoverText()
    {
        if (_state != null && _state.IsReady && _state.GetBool(DiscoveredKey))
        {
            var locationId = _state.GetString(LocationIdKey);
            return string.IsNullOrEmpty(locationId)
                ? "$magenheim_deep_sigil_discovered"
                : $"$magenheim_deep_sigil_discovered\n{locationId}";
        }
        return "$magenheim_deep_sigil_activate";
    }

    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold || _view == null || !_view.IsValid() || string.IsNullOrEmpty(_biomeId)) return false;
        _view.InvokeRPC(ActivateRpc);
        return true;
    }

    public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

    private void RpcActivate(long sender)
    {
        if (_state == null || !_state.HasAuthority || string.IsNullOrEmpty(_biomeId)) return;

        var discovery = UnderworldDeepstoneRuntimeAuthority.ResolveDeepSigil(_biomeId);
        var existing = _state.GetString(LocationIdKey);
        if (existing.Length > 0 && !string.Equals(existing, discovery.UniqueLocationId, StringComparison.Ordinal))
            throw new InvalidOperationException("Deep Sigil ZDO is already bound to a different unique boss-location identity.");

        _state.SetString(LocationIdKey, discovery.UniqueLocationId);
        _state.SetString(BossIdKey, discovery.BossId);
        _state.SetBool(DiscoveredKey, true);

        // Fracture is the first completed physical unique-location consumer. Keep unsupported biome
        // identities fail-closed until their boss locations are resident rather than manufacturing
        // speculative coordinates or pins for unfinished content.
        if (!string.Equals(_biomeId, FractureBiomeId, StringComparison.Ordinal)) return;
        var anchor = UnderworldTerrainRuntime.ResolveUniqueLocation(
            discovery.UniqueLocationId,
            UnderworldTerrainBiome.FractureZones);
        _view.InvokeRPC(sender, RevealRpc, anchor.EnginePosition, FracturePinName);
    }

    private void RpcReveal(long sender, Vector3 position, string pinName)
    {
        // This RPC is targeted only to the activating peer. The coordinate is intentionally
        // transient: durable Sigil state remains the canonical location identity in the ZDO.
        EnsureDiscoveryPin(position, pinName);
        _restoredPersistedDiscovery = true;
    }

    private static void EnsureDiscoveryPin(Vector3 position, string pinName)
    {
        var map = Minimap.instance;
        if (map == null || string.IsNullOrWhiteSpace(pinName)) return;

        foreach (var pin in map.m_pins)
        {
            if (!string.Equals(pin.m_name, pinName, StringComparison.Ordinal)) continue;
            var delta = pin.m_pos - position;
            if (delta.sqrMagnitude <= 64f) return;
        }

        map.AddPin(position, Minimap.PinType.Icon3, pinName, true, false,
            ownerID: 0L, author: default);
    }
}
