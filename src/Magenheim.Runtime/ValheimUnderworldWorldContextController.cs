using System;
using BepInEx.Logging;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>
/// Owns the currently selected Magenheim instance-world context independently of the player's
/// physical transform. This is a transition/context boundary only: native Underworld terrain,
/// exploration and persistence remain owned by their dedicated instance authorities and must
/// never be inferred from or projected into Surface coordinates.
/// </summary>
internal sealed class ValheimUnderworldWorldContextController : IUnderworldWorldContextController
{
    private readonly ManualLogSource _log;
    private readonly object _gate = new();
    private UnderworldWorldIdentity? _activeIdentity;
    private UnderworldLayer _activeLayer = UnderworldLayer.Surface;

    internal ValheimUnderworldWorldContextController(UnderworldSpatialDomainDefinition spatialDomain, ManualLogSource log)
    {
        _ = spatialDomain ?? throw new ArgumentNullException(nameof(spatialDomain)); // compatibility ctor; placement is not context authority.
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    public void EnsureActive(UnderworldWorldIdentity identity, UnderworldLayer layer)
    {
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        if (!TryResolveLiveIdentity(out var liveIdentity, out var diagnostic) || liveIdentity is null)
            throw new InvalidOperationException($"Cannot establish Magenheim world context: {diagnostic}");
        if (!SameWorld(liveIdentity, identity))
            throw new InvalidOperationException("The active Valheim parent world does not match the Underworld transition identity.");

        lock (_gate)
        {
            // One runtime session may bind to exactly one deterministic Underworld instance.  A
            // concurrent/recovered transition carrying a different derived identity must fail
            // closed instead of silently replacing the instance underneath another transition.
            if (_activeIdentity is not null && !SameInstance(_activeIdentity, identity))
                throw new InvalidOperationException(
                    $"Magenheim world context is already bound to Underworld instance '{_activeIdentity.DerivedWorldId}' and cannot be rebound to '{identity.DerivedWorldId}' before world unload.");

            if (_activeIdentity is not null && _activeLayer == layer)
                return;

            _activeIdentity = identity;
            _activeLayer = layer;
            _log.LogDebug($"Activated Magenheim instance context {layer} for instance {identity.DerivedWorldId}.");
        }
    }

    public bool IsActive(UnderworldWorldIdentity identity, UnderworldLayer layer)
    {
        if (identity is null) return false;
        lock (_gate)
            return _activeIdentity is not null && _activeLayer == layer && SameInstance(_activeIdentity, identity);
    }

    internal void ResetForWorldUnload()
    {
        lock (_gate)
        {
            _activeIdentity = null;
            _activeLayer = UnderworldLayer.Surface;
        }
        _log.LogDebug("Released explicit Underworld instance context for world unload.");
    }

    private static bool TryResolveLiveIdentity(out UnderworldWorldIdentity? identity, out string diagnostic)
    {
        identity = null;
        var znet = ZNet.instance;
        var world = ZNet.World;
        if (znet is null || world is null)
        {
            diagnostic = "Valheim network/world metadata is unavailable.";
            return false;
        }

        return UnderworldRuntimeIdentityResolver.TryResolveWorldIdentity(znet, world, out identity, out diagnostic);
    }

    private static bool SameWorld(UnderworldWorldIdentity left, UnderworldWorldIdentity right) =>
        string.Equals(left.ParentWorldId, right.ParentWorldId, StringComparison.Ordinal) &&
        string.Equals(left.ParentSeed, right.ParentSeed, StringComparison.Ordinal);

    private static bool SameInstance(UnderworldWorldIdentity left, UnderworldWorldIdentity right) =>
        SameWorld(left, right) &&
        string.Equals(left.DerivedWorldId, right.DerivedWorldId, StringComparison.Ordinal) &&
        string.Equals(left.DerivedSeedFingerprint, right.DerivedSeedFingerprint, StringComparison.Ordinal);
}
