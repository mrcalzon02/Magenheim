using System;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

internal readonly record struct UnderworldDeepstonePersistentState(bool TrophyMounted, bool BoonUnlocked);

/// <summary>
/// Runtime identity and synchronized persistence boundary for one physical Deepstone.
/// Progression decisions remain in Magenheim.Core; interaction delegates to the server-authoritative
/// Deepstone transport rather than accepting a client-selected trophy or progression fact.
/// Durable keys are scoped to the deterministic derived Underworld identity; Surface/global keys
/// are used only as a one-way migration source for saves created before instance isolation.
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
        if (!TryResolveIdentity(out var identity)) return default;
        return ReadPersistentState(deepstoneId, identity!);
    }

    internal static UnderworldDeepstonePersistentState ReadPersistentState(string deepstoneId, UnderworldWorldIdentity identity)
    {
        if (string.IsNullOrWhiteSpace(deepstoneId)) throw new ArgumentException("Canonical Deepstone id is required.", nameof(deepstoneId));
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        var zone = ZoneSystem.instance;
        if (zone == null) return default;

        TryMigrateLegacyState(zone, deepstoneId, identity);
        return new UnderworldDeepstonePersistentState(
            zone.GetGlobalKey(ScopedKey(MountedKeyPrefix, deepstoneId, identity)),
            zone.GetGlobalKey(ScopedKey(BoonKeyPrefix, deepstoneId, identity)));
    }

    internal bool PersistAuthorizedActivation(bool trophyMounted, bool boonUnlocked)
    {
        if (!IsBound || !trophyMounted || !boonUnlocked || !TryResolveIdentity(out var identity)) return false;
        var zone = ZoneSystem.instance;
        var znet = ZNet.instance;
        if (zone == null || znet == null || !znet.IsServer()) return false;
        zone.SetGlobalKey(ScopedKey(MountedKeyPrefix, _deepstoneId!, identity!));
        zone.SetGlobalKey(ScopedKey(BoonKeyPrefix, _deepstoneId!, identity!));
        var persisted = ReadPersistentState(_deepstoneId!, identity!);
        if (persisted.TrophyMounted && persisted.BoonUnlocked) return true;
        TryRollbackAuthorizedActivation();
        return false;
    }

    internal bool TryRollbackAuthorizedActivation()
    {
        if (!IsBound || !TryResolveIdentity(out var identity)) return false;
        var zone = ZoneSystem.instance;
        var znet = ZNet.instance;
        if (zone == null || znet == null || !znet.IsServer()) return false;
        zone.RemoveGlobalKey(ScopedKey(MountedKeyPrefix, _deepstoneId!, identity!));
        zone.RemoveGlobalKey(ScopedKey(BoonKeyPrefix, _deepstoneId!, identity!));
        var state = ReadPersistentState(_deepstoneId!, identity!);
        return !state.TrophyMounted && !state.BoonUnlocked;
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

    private static bool TryResolveIdentity(out UnderworldWorldIdentity? identity)
    {
        identity = null;
        var znet = ZNet.instance;
        var world = ZNet.World;
        if (znet is null || world is null) return false;
        return UnderworldRuntimeIdentityResolver.TryResolveWorldIdentity(znet, world, out identity, out _);
    }

    private static string ScopedKey(string prefix, string deepstoneId, UnderworldWorldIdentity identity) =>
        prefix + identity.DerivedSeedFingerprint + "." + deepstoneId;

    private static void TryMigrateLegacyState(ZoneSystem zone, string deepstoneId, UnderworldWorldIdentity identity)
    {
        var znet = ZNet.instance;
        if (znet == null || !znet.IsServer()) return;

        var mountedKey = ScopedKey(MountedKeyPrefix, deepstoneId, identity);
        var boonKey = ScopedKey(BoonKeyPrefix, deepstoneId, identity);
        var legacyMountedKey = MountedKeyPrefix + deepstoneId;
        var legacyBoonKey = BoonKeyPrefix + deepstoneId;

        if (!zone.GetGlobalKey(mountedKey) && zone.GetGlobalKey(legacyMountedKey)) zone.SetGlobalKey(mountedKey);
        if (!zone.GetGlobalKey(boonKey) && zone.GetGlobalKey(legacyBoonKey)) zone.SetGlobalKey(boonKey);

        // Legacy keys are deliberately retired after migration so a later rollback cannot
        // accidentally re-import stale Surface/global state into the derived instance.
        if (zone.GetGlobalKey(legacyMountedKey)) zone.RemoveGlobalKey(legacyMountedKey);
        if (zone.GetGlobalKey(legacyBoonKey)) zone.RemoveGlobalKey(legacyBoonKey);
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
