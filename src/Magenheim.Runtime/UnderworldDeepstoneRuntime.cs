using System;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Runtime identity and synchronized persistence boundary for one physical Deepstone.
/// Progression decisions remain in Magenheim.Core; this component only binds a Conclave
/// object to its canonical id and exposes server-owned durable mounted/boon facts.
/// </summary>
internal sealed class UnderworldDeepstoneRuntime : MonoBehaviour, Hoverable
{
    private const string MountedKeyPrefix = "magenheim.underworld.deepstone.mounted.";
    private const string BoonKeyPrefix = "magenheim.underworld.deepstone.boon.";
    private string? _deepstoneId;

    internal string DeepstoneId => _deepstoneId ?? string.Empty;
    internal bool IsBound => !string.IsNullOrWhiteSpace(_deepstoneId);
    internal bool TrophyMounted => IsBound && ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(MountedKeyPrefix + _deepstoneId);
    internal bool BoonUnlocked => IsBound && ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(BoonKeyPrefix + _deepstoneId);

    internal void Bind(string deepstoneId)
    {
        if (string.IsNullOrWhiteSpace(deepstoneId)) throw new ArgumentException("Canonical Deepstone id is required.", nameof(deepstoneId));
        if (_deepstoneId is not null && !string.Equals(_deepstoneId, deepstoneId, StringComparison.Ordinal))
            throw new InvalidOperationException($"Deepstone runtime is already bound to '{_deepstoneId}'.");
        _deepstoneId = deepstoneId;
    }

    /// <summary>
    /// Persists the atomic state already authorized by UnderworldDeepstoneProgression.
    /// This method deliberately refuses client mutation and refuses partial mount/boon state.
    /// </summary>
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
        return $"{DisplayName(DeepstoneId)}\nThe stone awaits its trophy.";
    }

    private static string DisplayName(string id)
    {
        var suffix = id.StartsWith("magenheim.underworld.deepstone.", StringComparison.Ordinal)
            ? id.Substring("magenheim.underworld.deepstone.".Length)
            : id;
        if (string.IsNullOrWhiteSpace(suffix)) return "Deepstone";
        return "Deepstone of " + char.ToUpperInvariant(suffix[0]) + suffix.Substring(1);
    }
}
