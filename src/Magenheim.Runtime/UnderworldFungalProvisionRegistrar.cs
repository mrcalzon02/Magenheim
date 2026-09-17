using System;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>
/// Runtime content adapter for the canonical Fungal Forest provisions admitted by Core.
/// Recipes deliberately use existing Valheim ingredients until dedicated Underworld harvest
/// materials become registered content; prefab admission remains owned by Core.
/// </summary>
internal sealed class UnderworldFungalProvisionRegistrar : IDisposable
{
    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal UnderworldFungalProvisionRegistrar(ManualLogSource log) =>
        _log = log ?? throw new ArgumentNullException(nameof(log));

    internal void Register()
    {
        if (_subscribed || _registered) return;
        PrefabManager.OnVanillaPrefabsAvailable += RegisterContent;
        _subscribed = true;
    }

    private void RegisterContent()
    {
        if (_registered) return;
        try
        {
            RegisterFood(UnderworldFungalProvisionCatalog.GlowcapStew, "MushroomMagecap", "Glowcap Stew",
                "A luminous fungal stew from the First Bloom. Its strange proteins are unusually nourishing to those adapted to the Underworld.",
                health: 45f, stamina: 55f, eitr: 25f, duration: 1600f, regen: 3f);
            RegisterFood(UnderworldFungalProvisionCatalog.MycelialBroth, "MushroomMagecap", "Mycelial Broth",
                "A mineral-rich broth steeped with dense fungal tissue. Warm, earthy, and faintly bioluminescent.",
                health: 38f, stamina: 72f, eitr: 18f, duration: 1500f, regen: 3f);
            RegisterFood(UnderworldFungalProvisionCatalog.HeartcapRation, "MisthareSupreme", "Heartcap Ration",
                "A compact ration built around the dense heart tissue of mature Underworld fungi. Difficult to prepare, exceptionally sustaining.",
                health: 78f, stamina: 48f, eitr: 22f, duration: 1800f, regen: 4f);

            RegisterRecipe("Magenheim_Recipe_Underworld_GlowcapStew", UnderworldFungalProvisionCatalog.GlowcapStew,
                ("MushroomMagecap", 2), ("RoyalJelly", 1), ("Sap", 1));
            RegisterRecipe("Magenheim_Recipe_Underworld_MycelialBroth", UnderworldFungalProvisionCatalog.MycelialBroth,
                ("MushroomMagecap", 2), ("MushroomJotunPuffs", 2), ("Sap", 1));
            RegisterRecipe("Magenheim_Recipe_Underworld_HeartcapRation", UnderworldFungalProvisionCatalog.HeartcapRation,
                ("MushroomMagecap", 2), ("RoyalJelly", 2), ("MisthareSupreme", 1));

            _registered = true;
            _log.LogInfo("Registered the three canonical Fungal Forest provisions and cauldron recipes for Spore Communion.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Underworld fungal provision registration failed: {exception}");
            throw;
        }
        finally
        {
            Dispose();
        }
    }

    private static void RegisterFood(string prefab, string sourcePrefab, string name, string description,
        float health, float stamina, float eitr, float duration, float regen)
    {
        if (PrefabManager.Instance.GetPrefab(prefab) || CustomItem.IsCustomItem(prefab))
            throw new InvalidOperationException($"Cannot replace occupied fungal provision identity '{prefab}'.");
        RequirePrefab(sourcePrefab);

        var item = new CustomItem(prefab, sourcePrefab);
        var shared = item.ItemDrop.m_itemData.m_shared;
        shared.m_name = name;
        shared.m_description = description;
        shared.m_food = health;
        shared.m_foodStamina = stamina;
        shared.m_foodEitr = eitr;
        shared.m_foodBurnTime = duration;
        shared.m_foodRegen = regen;
        shared.m_value = 0;
        shared.m_dlc = string.Empty;

        if (!ItemManager.Instance.AddItem(item))
            throw new InvalidOperationException($"Jotunn refused fungal provision '{prefab}'.");
    }

    private static void RegisterRecipe(string name, string item, params (string Prefab, int Amount)[] requirements)
    {
        foreach (var requirement in requirements) RequirePrefab(requirement.Prefab);
        var station = ResolveCauldron();
        var config = new RecipeConfig
        {
            Name = name,
            Item = item,
            Amount = 1,
            CraftingStation = station,
            MinStationLevel = 1,
            Enabled = true,
        };
        foreach (var requirement in requirements) config.AddRequirement(requirement.Prefab, requirement.Amount);
        if (!ItemManager.Instance.AddRecipe(new CustomRecipe(config)))
            throw new InvalidOperationException($"Jotunn refused fungal provision recipe '{name}'.");
    }

    private static string ResolveCauldron()
    {
        foreach (var candidate in new[] { "piece_cauldron", "piece_meadcauldron" })
            if (PrefabManager.Instance.GetPrefab(candidate)) return candidate;
        throw new InvalidOperationException("No cauldron crafting station is available for Underworld fungal provisions.");
    }

    private static void RequirePrefab(string prefab)
    {
        if (PrefabManager.Instance.GetPrefab(prefab) is null && !CustomItem.IsCustomItem(prefab))
            throw new InvalidOperationException($"Required fungal provision dependency '{prefab}' is unavailable.");
    }

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterContent;
        _subscribed = false;
    }
}
