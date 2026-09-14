using System;

namespace Magenheim.Core.Worldgen;

/// <summary>
/// Derives the dedicated world-object prefab identity for a geode from its authoritative
/// inventory-item prefab identity. The derivation is deterministic so definition authority
/// does not need a second independently editable identity that could drift out of sync.
/// </summary>
public static class GeodeWorldPrefabIdentity
{
    public const string WorldSuffix = "_World";

    public static string FromItemPrefabName(string itemPrefabName)
    {
        if (string.IsNullOrWhiteSpace(itemPrefabName))
            throw new ArgumentException("A geode item prefab name is required.", nameof(itemPrefabName));
        if (!string.Equals(itemPrefabName, itemPrefabName.Trim(), StringComparison.Ordinal))
            throw new ArgumentException("A geode item prefab name cannot contain leading or trailing whitespace.", nameof(itemPrefabName));
        if (!itemPrefabName.StartsWith("Magenheim_", StringComparison.Ordinal))
            throw new ArgumentException("A geode item prefab name must use the Magenheim_ namespace.", nameof(itemPrefabName));
        if (itemPrefabName.EndsWith(WorldSuffix, StringComparison.Ordinal))
            throw new ArgumentException("The authoritative geode item prefab name cannot already use the reserved world-object suffix.", nameof(itemPrefabName));

        return itemPrefabName + WorldSuffix;
    }
}
