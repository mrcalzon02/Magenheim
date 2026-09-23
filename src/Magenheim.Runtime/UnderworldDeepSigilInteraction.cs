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
            !_state.GetBool(DiscoveredKey))
            return;

        var locationId = _state.GetString(LocationIdKey);
        if (string.IsNullOrWhiteSpace(locationId) || Minimap.instance == null) return;

        // Only physically resident unique locations have presentation entries. Unsupported biomes
        // remain fail-closed until their real location residency is implemented.
        if (!UnderworldUniqueLocationDiscoveryCatalog.TryResolve(_biomeId, locationId, out var resident))
            return;

        // The ZDO owns only the canonical identity. Re-derive the active world's anchor after load
        // so stale coordinates can never survive a world/seed change. Native terrain admission can
        // trail ZDO replication during instance startup, so an unavailable authority simply retries.
        try
        {
            var anchor = UnderworldTerrainRuntime.ResolveUniqueLocation(locationId, resident.TerrainBiome);
            EnsureDiscoveryPin(anchor.EnginePosition, resident.PinName);
            _restoredPersistedDiscovery = true;
        }
        catch (InvalidOperationException)
        {
            // Retry on a later frame rather than treating a transient load-order gap as lost discovery.
        }
    }

    public string GetHoverName() => "$magenheim_deep_sigil";
    public float GetHoverOffset() => 0f;

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

        // Definition authority determines the identity; this catalog only admits locations whose
        // physical residency is complete. No speculative coordinate is manufactured for backlog content.
        if (!UnderworldUniqueLocationDiscoveryCatalog.TryResolve(
                _biomeId, discovery.UniqueLocationId, out var resident))
            return;

        var anchor = UnderworldTerrainRuntime.ResolveUniqueLocation(
            discovery.UniqueLocationId, resident.TerrainBiome);
        _view.InvokeRPC(sender, RevealRpc, anchor.EnginePosition, resident.PinName);
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

        // Pin mutation must target the player's physical world payload, not whichever tab happens
        // to be selected in the large-map UI. A Deep Sigil can only be interacted with or restored
        // while its native Underworld instance is resident, so the existing physical-map binding
        // scope gives us the Underworld payload and restores the user's selected tab afterwards.
        // This keeps all pin storage/serialization in vanilla Minimap while preventing a Sigil
        // reveal from contaminating the Surface payload when the player is browsing that tab.
        var binding = UnderworldMapTabRuntime.BeginPhysicalMapOperation(map);
        try
        {
            foreach (var pin in RuntimeGameApi.GetMapPins(map))
            {
                if (!string.Equals(pin.m_name, pinName, StringComparison.Ordinal)) continue;
                var delta = pin.m_pos - position;
                if (delta.sqrMagnitude <= 64f) return;
            }

            map.AddPin(position, Minimap.PinType.Icon3, pinName, true, false,
                ownerID: 0L, author: default);
        }
        finally
        {
            binding?.Dispose();
        }
    }
}
