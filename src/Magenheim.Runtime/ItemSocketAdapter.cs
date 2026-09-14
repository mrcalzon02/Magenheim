using System;
using System.Collections.Generic;
using Jotunn.Utils;
using Magenheim.Core.Socketing;

namespace Magenheim.Runtime;

/// <summary>
/// The narrow runtime boundary for Magenheim socket metadata. Socket state lives on
/// individual ItemData instances and only the Magenheim custom-data key is mutated.
/// </summary>
internal static class ItemSocketAdapter
{
    internal static EquipmentDescriptor Describe(ItemDrop.ItemData item, string modOrigin = "")
    {
        if (item is null) throw new ArgumentNullException(nameof(item));
        var prefabName = item.m_dropPrefab ? item.m_dropPrefab.name : item.m_shared.m_name;
        var origin = string.IsNullOrWhiteSpace(modOrigin)
            ? ResolveModOrigin(prefabName)
            : modOrigin.Trim();
        return new EquipmentDescriptor(prefabName, origin, Classify(item));
    }

    internal static bool TryRead(ItemDrop.ItemData item, out SocketState state, out string diagnostic)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));
        if (item.m_customData is null)
        {
            state = SocketState.Empty;
            diagnostic = string.Empty;
            return true;
        }

        return SocketMetadataCodec.TryRead(item.m_customData, out state, out diagnostic);
    }

    internal static void Write(ItemDrop.ItemData item, SocketState state)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));
        if (state is null) throw new ArgumentNullException(nameof(state));
        item.m_customData ??= new Dictionary<string, string>();
        SocketMetadataCodec.Write(item.m_customData, state);
    }

    internal static EquipmentCategory Classify(ItemDrop.ItemData item)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));
        var typeName = item.m_shared.m_itemType.ToString();

        if (typeName == "Shield") return EquipmentCategory.Shield;
        if (typeName is "Helmet" or "Chest" or "Legs" or "Hands" or "Shoulder")
            return EquipmentCategory.Armor;
        if (typeName == "Tool") return EquipmentCategory.Tool;
        if (typeName == "Utility") return EquipmentCategory.Utility;
        if (typeName.IndexOf("Weapon", StringComparison.Ordinal) >= 0 ||
            typeName.IndexOf("Bow", StringComparison.Ordinal) >= 0 ||
            typeName.IndexOf("Atgeir", StringComparison.Ordinal) >= 0 ||
            typeName.IndexOf("Staff", StringComparison.Ordinal) >= 0 ||
            typeName == "Torch")
            return EquipmentCategory.Weapon;

        return EquipmentCategory.Unknown;
    }

    private static string ResolveModOrigin(string prefabName)
    {
        if (string.IsNullOrWhiteSpace(prefabName))
            return string.Empty;

        var modPrefab = ModQuery.GetPrefab(prefabName);
        return modPrefab?.SourceMod?.GUID ?? "vanilla";
    }
}
