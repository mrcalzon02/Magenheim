using System;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Narrow persistence seam for interactive Underworld objects.
/// Valheim remains the persistence/network authority: this adapter only binds Magenheim's
/// deterministic instance identity to an already-valid persistent ZNetView/ZDO and exposes
/// namespaced state access. It never creates ZDOs, saves files, or mirrors network ownership.
/// </summary>
internal sealed class UnderworldZdoStateAdapter : MonoBehaviour
{
    private const string StableObjectIdKey = "magenheim.underworld.object.id";
    private const string DerivedWorldIdKey = "magenheim.underworld.world.id";
    private const string DerivedSeedKey = "magenheim.underworld.world.seed";
    private const string StatePrefix = "magenheim.underworld.state.";

    private ZNetView _view = null!;
    private UnderworldGeneratedObjectIdentity _identity = null!;

    internal bool IsReady => _view != null && _view.IsValid() && _identity != null;
    internal bool HasAuthority => IsReady && _view.IsOwner() && ZNet.instance != null && ZNet.instance.IsServer();
    internal string StableObjectId => IsReady ? _identity.StableObjectId : string.Empty;

    internal void Bind(UnderworldWorldIdentity worldIdentity, UnderworldGeneratedObjectIdentity objectIdentity)
    {
        if (worldIdentity is null) throw new ArgumentNullException(nameof(worldIdentity));
        if (objectIdentity is null) throw new ArgumentNullException(nameof(objectIdentity));
        if (!objectIdentity.IsOwnedBy(worldIdentity))
            throw new InvalidOperationException("Cannot bind ZDO state to an object owned by a different Underworld instance.");

        _view = GetComponent<ZNetView>();
        if (_view == null || !_view.IsValid())
            throw new InvalidOperationException("Persistent Underworld state requires an already-valid Valheim ZNetView/ZDO.");

        _identity = objectIdentity;
        ValidateOrStampIdentity(worldIdentity);
    }

    internal bool GetBool(string key, bool fallback = false)
    {
        EnsureReady();
        return _view.GetZDO().GetBool(StateKey(key), fallback);
    }

    internal string GetString(string key, string fallback = "")
    {
        EnsureReady();
        return _view.GetZDO().GetString(StateKey(key), fallback);
    }

    internal void SetBool(string key, bool value)
    {
        EnsureAuthority();
        _view.GetZDO().Set(StateKey(key), value);
    }

    internal void SetString(string key, string value)
    {
        EnsureAuthority();
        _view.GetZDO().Set(StateKey(key), value ?? string.Empty);
    }

    private void ValidateOrStampIdentity(UnderworldWorldIdentity worldIdentity)
    {
        var zdo = _view.GetZDO();
        var existingObjectId = zdo.GetString(StableObjectIdKey, string.Empty);
        var existingWorldId = zdo.GetString(DerivedWorldIdKey, string.Empty);
        var existingSeed = zdo.GetString(DerivedSeedKey, string.Empty);

        if (existingObjectId.Length > 0 && !string.Equals(existingObjectId, _identity.StableObjectId, StringComparison.Ordinal))
            throw new InvalidOperationException("ZDO is already bound to a different Underworld generated object identity.");
        if (existingWorldId.Length > 0 && !string.Equals(existingWorldId, worldIdentity.DerivedWorldId, StringComparison.Ordinal))
            throw new InvalidOperationException("ZDO is already bound to a different derived Underworld.");
        if (existingSeed.Length > 0 && !string.Equals(existingSeed, worldIdentity.DerivedSeedFingerprint, StringComparison.Ordinal))
            throw new InvalidOperationException("ZDO is already bound to a different Underworld seed fingerprint.");

        if (!HasAuthority)
            return;

        if (existingObjectId.Length == 0) zdo.Set(StableObjectIdKey, _identity.StableObjectId);
        if (existingWorldId.Length == 0) zdo.Set(DerivedWorldIdKey, worldIdentity.DerivedWorldId);
        if (existingSeed.Length == 0) zdo.Set(DerivedSeedKey, worldIdentity.DerivedSeedFingerprint);
    }

    private static string StateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Underworld ZDO state key is required.", nameof(key));
        return StatePrefix + key.Trim();
    }

    private void EnsureReady()
    {
        if (!IsReady) throw new InvalidOperationException("Underworld ZDO state adapter has not been bound to a valid ZNetView.");
    }

    private void EnsureAuthority()
    {
        EnsureReady();
        if (!HasAuthority) throw new InvalidOperationException("Only the authoritative server owner may mutate Underworld ZDO state.");
    }
}
