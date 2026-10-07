using System;
using System.Reflection;
using HarmonyLib;

namespace Magenheim.Runtime;

internal static class NullMantleEquipmentState
{
    private static readonly FieldInfo HelmetSlot = AccessTools.Field(typeof(Humanoid), "m_helmetItem")
        ?? throw new MissingFieldException(typeof(Humanoid).FullName, "m_helmetItem");

    internal static bool IsEquipped(Player player)
    {
        if (!player) return false;
        var view = player.GetComponent<ZNetView>();
        if (view && view.IsValid() && !view.IsOwner())
            return view.GetZDO().GetInt(ZDOVars.s_helmetItem, 0) ==
                NowhereKingRewardRegistrar.NullMantlePrefabName.GetStableHashCode();

        // The native equipment slot owns local equip state. Inventory m_equipped flags
        // can remain on items managed by accessory/cosmetic inventory extensions.
        var helmet = HelmetSlot.GetValue(player) as ItemDrop.ItemData;
        return helmet?.m_dropPrefab && string.Equals(helmet.m_dropPrefab.name,
            NowhereKingRewardRegistrar.NullMantlePrefabName, StringComparison.Ordinal);
    }
}
