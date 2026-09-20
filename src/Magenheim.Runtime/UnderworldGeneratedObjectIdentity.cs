using System;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Native-instance ownership marker for generated Underworld objects.
/// This deliberately does not use ZDO/ZoneSystem identity: those belong to the parent Valheim world.
/// StableObjectId is deterministic inside one derived Underworld and is suitable as the key for the
/// native generated-object persistence store. Repeatable structure kinds must additionally provide a
/// deterministic instance-local placement key so two ruins of the same kind never alias one record.
/// </summary>
internal sealed class UnderworldGeneratedObjectIdentity : MonoBehaviour
{
    internal string ParentWorldId { get; private set; } = string.Empty;
    internal string DerivedWorldId { get; private set; } = string.Empty;
    internal string DerivedSeedFingerprint { get; private set; } = string.Empty;
    internal string ObjectKind { get; private set; } = string.Empty;
    internal string PlacementKey { get; private set; } = string.Empty;
    internal string StableObjectId { get; private set; } = string.Empty;

    internal void Bind(UnderworldWorldIdentity identity, string objectKind) => Bind(identity, objectKind, string.Empty);

    internal void Bind(UnderworldWorldIdentity identity, string objectKind, string placementKey)
    {
        Validate(identity, objectKind);

        ParentWorldId = identity.ParentWorldId;
        DerivedWorldId = identity.DerivedWorldId;
        DerivedSeedFingerprint = identity.DerivedSeedFingerprint;
        ObjectKind = objectKind.Trim();
        PlacementKey = NormalizePlacementKey(placementKey);
        StableObjectId = BuildStableObjectId(identity, ObjectKind, PlacementKey);
    }

    internal bool IsOwnedBy(UnderworldWorldIdentity identity)
    {
        if (identity is null) return false;
        return string.Equals(ParentWorldId, identity.ParentWorldId, StringComparison.Ordinal)
            && string.Equals(DerivedWorldId, identity.DerivedWorldId, StringComparison.Ordinal)
            && string.Equals(DerivedSeedFingerprint, identity.DerivedSeedFingerprint, StringComparison.Ordinal);
    }

    internal static string BuildStableObjectId(UnderworldWorldIdentity identity, string objectKind) =>
        BuildStableObjectId(identity, objectKind, string.Empty);

    internal static string BuildStableObjectId(UnderworldWorldIdentity identity, string objectKind, string placementKey)
    {
        Validate(identity, objectKind);
        var normalizedPlacement = NormalizePlacementKey(placementKey);
        var prefix = identity.DerivedWorldId + ":" + identity.DerivedSeedFingerprint + ":" + objectKind.Trim();
        return normalizedPlacement.Length == 0 ? prefix : prefix + ":" + normalizedPlacement;
    }

    private static void Validate(UnderworldWorldIdentity identity, string objectKind)
    {
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        if (string.IsNullOrWhiteSpace(identity.ParentWorldId) || string.IsNullOrWhiteSpace(identity.DerivedWorldId) || string.IsNullOrWhiteSpace(identity.DerivedSeedFingerprint))
            throw new ArgumentException("Complete Underworld instance identity is required.", nameof(identity));
        if (string.IsNullOrWhiteSpace(objectKind)) throw new ArgumentException("Generated Underworld object kind is required.", nameof(objectKind));
    }

    private static string NormalizePlacementKey(string placementKey) => (placementKey ?? string.Empty).Trim();
}
