using System;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Player-facing Deep Sigil interaction. Interaction requests travel through the core's normal
/// ZNetView owner RPC; only the authoritative server owner resolves definitions and mutates ZDO
/// discovery state. The Sigil persists a stable unique-location identity, never a coordinate.
/// </summary>
internal sealed class UnderworldDeepSigilInteraction : MonoBehaviour, Hoverable, Interactable
{
    private const string ActivateRpc = "Magenheim_DeepSigil_Activate";
    private const string DiscoveredKey = "deep-sigil.discovered";
    private const string LocationIdKey = "deep-sigil.location-id";
    private const string BossIdKey = "deep-sigil.boss-id";

    private ZNetView _view = null!;
    private UnderworldZdoStateAdapter _state = null!;
    private string _biomeId = string.Empty;

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
    }
}
