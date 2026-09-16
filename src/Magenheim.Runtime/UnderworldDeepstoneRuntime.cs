using System;
using UnityEngine;

namespace Magenheim.Runtime;

internal readonly record struct UnderworldDeepstonePersistentState(bool TrophyMounted, bool BoonUnlocked);

/// <summary>
/// Runtime identity and synchronized persistence boundary for one physical Deepstone.
/// Progression decisions remain in Magenheim.Core; interaction delegates to the server-authoritative
/// Deepstone transport rather than accepting a client-selected trophy or progression fact.
/// </summary>
internal sealed class UnderworldDeepstoneRuntime : MonoBehaviour, Hoverable, Interactable
{
    private const string MountedKeyPrefix = "magenheim.underworld.deepstone.mounted.";
    private const string BoonKeyPrefix = "magenheim.underworld.deepstone.boon.";
    private string? _deepstoneId;

    internal string DeepstoneId => _deepstoneId ?? string.Empty;
    internal bool IsBound => !string.IsNullOrWhiteSpace(_deepstoneId);
    internal bool TrophyMounted => IsBound && ReadPersistentState(_deepstoneId!).TrophyMounted;
    internal bool BoonUnlocked => IsBound && ReadPersistentState(_deepstoneId!).BoonUnlocked;

    internal void Bind(string deepstoneId)
    {
        if (string.IsNullOrWhiteSpace(deepstoneId)) throw new ArgumentException("Canonical Deepstone id is required.", nameof(deepstoneId));
        if (_deepstoneId is not null && !string.Equals(_deepstoneId, deepstoneId, StringComparison.Ordinal))
            throw new InvalidOperationException($"Deepstone runtime is already bound to '{_deepstoneId}'.");
        _deepstoneId = deepstoneId;
    }

    internal static UnderworldDeepstonePersistentState ReadPersistentState(string deepstoneId)
    {
        if (string.IsNullOrWhiteSpace(deepstoneId)) throw new ArgumentException("Canonical Deepstone id is required.", nameof(deepstoneId));
        var zone = ZoneSystem.instance;
        if (zone == null) return default;
        return new UnderworldDeepstonePersistentState(
            zone.GetGlobalKey(MountedKeyPrefix + deepstoneId),
            zone.GetGlobalKey(BoonKeyPrefix + deepstoneId));
    }

    internal bool PersistAuthorizedActivation(bool trophyMounted, bool boonUnlocked)
    {
        if (!IsBound || !trophyMounted || !boonUnlocked) return false;
        var zone = ZoneSystem.instance;
        var znet = ZNet.instance;
        if (zone == null || znet == null || !znet.IsServer()) return false;
        zone.SetGlobalKey(MountedKeyPrefix + _deepstoneId);
        zone.SetGlobalKey(BoonKeyPrefix + _deepstoneId);
        return true;
    }

    public string GetHoverName() => DisplayName(DeepstoneId);
    public float GetHoverOffset() => 0f;
    public string GetHoverText()
    {
        if (!IsBound) return "Deepstone\nUnbound progression identity.";
        if (TrophyMounted && BoonUnlocked) return $"{DisplayName(DeepstoneId)}\nTrophy mounted. Deep Boon awakened.";
        return $"{DisplayName(DeepstoneId)}\n[Use] Offer the required trophy";
    }

    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold || user is null || !IsBound || (TrophyMounted && BoonUnlocked)) return false;
        var accepted = UnderworldDeepstoneInteractionRpc.TryActivate(this, user, out var diagnostic);
        if (!string.IsNullOrWhiteSpace(diagnostic)) user.Message(MessageHud.MessageType.Center, diagnostic);
        return accepted;
    }

    public bool UseItem(Humanoid user, ItemDrop.ItemData item) => Interact(user, false, false);

    private static string DisplayName(string id)
    {
        var suffix = id.StartsWith("magenheim.underworld.deepstone.", StringComparison.Ordinal)
            ? id.Substring("magenheim.underworld.deepstone.".Length)
            : id;
        if (string.IsNullOrWhiteSpace(suffix)) return "Deepstone";
        return "Deepstone of " + char.ToUpperInvariant(suffix[0]) + suffix.Substring(1);
    }
}
