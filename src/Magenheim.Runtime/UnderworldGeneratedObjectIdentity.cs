using System;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Native-instance ownership marker for generated Underworld objects.
/// This deliberately does not use ZDO/ZoneSystem identity: those belong to the parent Valheim world.
/// StableObjectId is deterministic inside one derived Underworld and is suitable as the key for the
/// native generated-object persistence store.
/// </summary>
internal sealed class UnderworldGeneratedObjectIdentity : MonoBehaviour
{
    internal string ParentWorldId { get; private set; } = string.Empty;
    internal string DerivedWorldId { get; private set; } = string.Empty;
    internal string DerivedSeedFingerprint { get; private set; } = string.Empty;
    internal string ObjectKind { get; private set; } = string.Empty;
    internal string StableObjectId { get; private set; } = string.Empty;

    internal void Bind(UnderworldWorldIdentity identity, string objectKind)
    {
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        if (string.IsNullOrWhiteSpace(objectKind)) throw new ArgumentException("Generated Underworld object kind is required.", nameof(objectKind));

        ParentWorldId = identity.ParentWorldId;
        DerivedWorldId = identity.DerivedWorldId;
        DerivedSeedFingerprint = identity.DerivedSeedFingerprint;
        ObjectKind = objectKind.Trim();
        StableObjectId = BuildStableObjectId(identity, ObjectKind);
    }

    internal bool IsOwnedBy(UnderworldWorldIdentity identity)
    {
        if (identity is null) return false;
        return string.Equals(ParentWorldId, identity.ParentWorldId, StringComparison.Ordinal)
            && string.Equals(DerivedWorldId, identity.DerivedWorldId, StringComparison.Ordinal)
            && string.Equals(DerivedSeedFingerprint, identity.DerivedSeedFingerprint, StringComparison.Ordinal);
    }

    internal static string BuildStableObjectId(UnderworldWorldIdentity identity, string objectKind)
    {
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        if (string.IsNullOrWhiteSpace(objectKind)) throw new ArgumentException("Generated Underworld object kind is required.", nameof(objectKind));
        return identity.DerivedWorldId + ":" + identity.DerivedSeedFingerprint + ":" + objectKind.Trim();
    }
}
