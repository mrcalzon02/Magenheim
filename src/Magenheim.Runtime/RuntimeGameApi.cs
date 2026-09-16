using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace Magenheim.Runtime;

// Keep private game UI access at one explicit boundary; never publicize game assemblies.
internal static class RuntimeGameApi
{
    private static readonly FieldInfo CraftRecipe = AccessTools.Field(typeof(InventoryGui), "m_craftRecipe")
        ?? throw new MissingFieldException(typeof(InventoryGui).FullName, "m_craftRecipe");
    private static readonly MethodInfo UpdatePanel = AccessTools.Method(
        typeof(InventoryGui), "UpdateCraftingPanel", new[] { typeof(bool) })
        ?? throw new MissingMethodException(typeof(InventoryGui).FullName, "UpdateCraftingPanel(bool)");
    private static readonly MethodInfo InventoryChanged = AccessTools.Method(
        typeof(Inventory), "Changed", new[] { typeof(bool), typeof(bool) })
        ?? throw new MissingMethodException(typeof(Inventory).FullName, "Changed(bool, bool)");
    private static readonly FieldInfo MinimapPins = AccessTools.Field(typeof(Minimap), "m_pins")
        ?? throw new MissingFieldException(typeof(Minimap).FullName, "m_pins");

    internal static Recipe? GetCraftRecipe(InventoryGui gui) => CraftRecipe.GetValue(gui) as Recipe;

    // Minimap.m_pins became private in Valheim 1.0.12. Minimap.PinData and its members are still
    // public, so only the collection itself needs to cross this boundary.
    internal static List<Minimap.PinData> GetMapPins(Minimap map) =>
        MinimapPins.GetValue(map) as List<Minimap.PinData> ?? new List<Minimap.PinData>();

    // Inventory.Changed(success, cheatedStateChanged) exists only to drive the cheated-item
    // achievement popup; both stay false because Magenheim mutations are ordinary transactions.
    // Reflection.Invoke never applies the declared defaults, so both arguments must be passed.
    internal static void NotifyInventoryChanged(Inventory? inventory)
    {
        if (inventory is not null) InventoryChanged.Invoke(inventory, new object[] { false, false });
    }

    internal static void RefreshCraftingPanel(InventoryGui? gui)
    {
        if (gui) UpdatePanel.Invoke(gui, new object[] { false });
    }

    internal static long ServerPeerId => ZNet.instance?.GetServerPeer()?.m_uid
        ?? throw new InvalidOperationException("The server peer is unavailable; no operation can be sent.");
}
