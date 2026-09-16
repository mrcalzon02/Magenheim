using System;
using BepInEx.Logging;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>
/// Valheim-safe logical Underworld context controller.
///
/// Valheim owns one live network/world context for the lifetime of a connected session. Magenheim
/// therefore treats the Underworld as an isolated logical layer hosted inside that authoritative
/// world instead of attempting to replace ZNet/WorldGenerator/ZoneSystem underneath connected
/// peers. The derived identity remains the deterministic namespace for generation, persistence
/// and transition authority; it is not interpreted as permission to hot-swap the engine world.
/// </summary>
internal sealed class ValheimLogicalUnderworldWorldContextController : IUnderworldWorldContextController
{
    private readonly ManualLogSource _log;
    private UnderworldWorldIdentity? _identity;
    private UnderworldLayer _activeLayer = UnderworldLayer.Surface;

    internal ValheimLogicalUnderworldWorldContextController(ManualLogSource log)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    public void EnsureActive(UnderworldWorldIdentity identity, UnderworldLayer layer)
    {
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        if (ZNet.instance is null)
            throw new InvalidOperationException("Underworld context cannot change without an active Valheim network/world session.");

        if (_identity is not null && !SamePair(_identity, identity))
            throw new InvalidOperationException("Underworld context controller is already bound to a different parent/derived world pair.");

        _identity ??= identity;
        if (_activeLayer == layer) return;

        _activeLayer = layer;
        _log.LogInfo($"Activated logical Magenheim layer {layer} inside parent Valheim world '{identity.ParentWorldId}'.");
    }

    public bool IsActive(UnderworldWorldIdentity identity, UnderworldLayer layer)
    {
        if (identity is null || ZNet.instance is null || _identity is null) return false;
        return SamePair(_identity, identity) && _activeLayer == layer;
    }

    internal void ResetForWorldUnload()
    {
        _identity = null;
        _activeLayer = UnderworldLayer.Surface;
    }

    private static bool SamePair(UnderworldWorldIdentity left, UnderworldWorldIdentity right) =>
        string.Equals(left.ParentWorldId, right.ParentWorldId, StringComparison.Ordinal) &&
        string.Equals(left.DerivedWorldId, right.DerivedWorldId, StringComparison.Ordinal) &&
        string.Equals(left.DerivedSeedFingerprint, right.DerivedSeedFingerprint, StringComparison.Ordinal);
}
