using System;
using BepInEx.Logging;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>
/// Physical-session authority for the paired Surface/Underworld worlds. This controller never
/// pretends that changing a logical flag changes Valheim's active world. A requested context is
/// active only when the live ZNet world resolves through the persisted world-pair manifest to the
/// requested physical layer and canonical pair.
/// </summary>
internal sealed class ValheimPhysicalUnderworldWorldContextController : IUnderworldWorldContextController
{
    private readonly UnderworldWorldPairManifestStore _pairStore;
    private readonly ManualLogSource _log;

    internal ValheimPhysicalUnderworldWorldContextController(UnderworldWorldPairManifestStore pairStore, ManualLogSource log)
    {
        _pairStore = pairStore ?? throw new ArgumentNullException(nameof(pairStore));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    public void EnsureActive(UnderworldWorldIdentity identity, UnderworldLayer layer)
    {
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        if (!TryResolveLive(out var liveIdentity, out var liveLayer, out var diagnostic) || liveIdentity is null)
            throw new InvalidOperationException($"Cannot establish physical Underworld context: {diagnostic}");
        if (!SamePair(liveIdentity, identity))
            throw new InvalidOperationException("The active Valheim world belongs to a different Surface/Underworld pair.");
        if (liveLayer != layer)
            throw new UnderworldPhysicalWorldSwitchRequiredException(identity, liveLayer, layer);
    }

    public bool IsActive(UnderworldWorldIdentity identity, UnderworldLayer layer)
    {
        if (identity is null) return false;
        return TryResolveLive(out var liveIdentity, out var liveLayer, out _) && liveIdentity is not null &&
               SamePair(liveIdentity, identity) && liveLayer == layer;
    }

    internal void ResetForWorldUnload()
    {
        // No mutable layer state exists to leak across sessions. The live ZNet world is authority.
        _log.LogDebug("Released physical Underworld world-context observation for world unload.");
    }

    private bool TryResolveLive(out UnderworldWorldIdentity? identity, out UnderworldLayer layer, out string diagnostic)
    {
        identity = null; layer = UnderworldLayer.Surface;
        var znet = ZNet.instance;
        var world = ZNet.World;
        if (znet is null || world is null) { diagnostic = "Valheim network/world metadata is unavailable."; return false; }
        return UnderworldRuntimeIdentityResolver.TryResolveWorldSession(_pairStore, znet, world, out identity, out layer, out diagnostic);
    }

    private static bool SamePair(UnderworldWorldIdentity left, UnderworldWorldIdentity right) =>
        string.Equals(left.ParentWorldId, right.ParentWorldId, StringComparison.Ordinal) &&
        string.Equals(left.DerivedWorldId, right.DerivedWorldId, StringComparison.Ordinal) &&
        string.Equals(left.DerivedSeedFingerprint, right.DerivedSeedFingerprint, StringComparison.Ordinal);
}

/// <summary>Signals the transition orchestrator that placement must wait for a real Valheim world load.</summary>
internal sealed class UnderworldPhysicalWorldSwitchRequiredException : InvalidOperationException
{
    internal UnderworldPhysicalWorldSwitchRequiredException(UnderworldWorldIdentity identity, UnderworldLayer current, UnderworldLayer target)
        : base($"Physical Valheim world switch required for '{identity.DerivedSeedFingerprint}': {current} -> {target}.")
    { Identity = identity; CurrentLayer = current; TargetLayer = target; }
    internal UnderworldWorldIdentity Identity { get; }
    internal UnderworldLayer CurrentLayer { get; }
    internal UnderworldLayer TargetLayer { get; }
}
