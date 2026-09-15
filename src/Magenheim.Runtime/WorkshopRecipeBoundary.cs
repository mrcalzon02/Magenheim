using System;
using System.Collections.Generic;
using HarmonyLib;

namespace Magenheim.Runtime;

/// <summary>
/// Non-destructive recipe visibility boundary for the specialist Geologist's Workstation.
/// Foreign recipes are suppressed only while InventoryGui rebuilds this station's recipe list;
/// their ObjectDB records and original enabled state are restored immediately afterwards.
/// </summary>
[HarmonyPatch(typeof(InventoryGui), "UpdateRecipeList")]
internal static class WorkshopRecipeBoundary
{
    private static readonly Dictionary<Recipe, bool> SavedEnabledState = new Dictionary<Recipe, bool>();

    private static void Prefix()
    {
        Restore();
        var player = Player.m_localPlayer;
        var database = ObjectDB.instance;
        if (player == null || database == null || database.m_recipes == null) return;
        var station = player.GetCurrentCraftingStation();
        if (!TargetsStation(station)) return;

        foreach (var recipe in database.m_recipes)
        {
            if (recipe == null || recipe.m_craftingStation == null || !TargetsStation(recipe.m_craftingStation) || IsMagenheimRecipe(recipe)) continue;
            SavedEnabledState[recipe] = recipe.m_enabled;
            recipe.m_enabled = false;
        }
    }

    private static void Postfix() => Restore();

    private static Exception? Finalizer(Exception? __exception)
    {
        Restore();
        return __exception;
    }

    private static void Restore()
    {
        foreach (var pair in SavedEnabledState)
            if (pair.Key != null) pair.Key.m_enabled = pair.Value;
        SavedEnabledState.Clear();
    }

    private static bool TargetsStation(CraftingStation? station)
    {
        if (station == null) return false;
        var name = station.gameObject != null ? station.gameObject.name : station.name;
        return string.Equals(Normalize(name), WorkshopRegistrar.StationPrefab, StringComparison.Ordinal);
    }

    private static bool IsMagenheimRecipe(Recipe recipe)
    {
        if (recipe.name.StartsWith("Magenheim_", StringComparison.Ordinal)) return true;
        var item = recipe.m_item;
        return item != null && item.gameObject != null && Normalize(item.gameObject.name).StartsWith("Magenheim_", StringComparison.Ordinal);
    }

    private static string Normalize(string value)
    {
        const string clone = "(Clone)";
        return value.EndsWith(clone, StringComparison.Ordinal) ? value.Substring(0, value.Length - clone.Length) : value;
    }
}
