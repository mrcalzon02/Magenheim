using System;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>
/// Post-victory replacement crafting for the Nowhere King's drops. The Mycelial Bench exists in the
/// first Underworld tier and is only reachable after the Dark Throne victory unlocks the Underworld,
/// so these recipes cannot replace the first legitimate boss kill.
/// </summary>
internal sealed class NowhereKingCraftingRegistrar : IDisposable
{
    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal NowhereKingCraftingRegistrar(ManualLogSource log) =>
        _log = log ?? throw new ArgumentNullException(nameof(log));

    internal void Register()
    {
        if (_subscribed || _registered) return;
        PrefabManager.OnVanillaPrefabsAvailable += RegisterRecipes;
        _subscribed = true;
    }

    private void RegisterRecipes()
    {
        if (_registered) return;
        try
        {
            Require(UnderworldWeaponUpgradeCatalog.MycelialBenchPrefab);

            AddRecipe(
                NowhereKingRewardRegistrar.FirmamentPrefabName,
                Cost("Magenheim_Weapon_CrystalSword", 1),
                Cost(UnderworldFungalRefinementCatalog.WorldrootPlank, 10),
                Cost("Magenheim_Underworld_Resource_Understone", 8),
                Cost(UnderworldFungalRefinementCatalog.SpireCord, 4),
                Cost(UnderworldFungalRefinementCatalog.CuredGlowcap, 2));

            AddRecipe(
                NowhereKingRewardRegistrar.NullGatePrefabName,
                Cost("Magenheim_Weapon_CrystalSword", 1),
                Cost(UnderworldFungalRefinementCatalog.WorldrootPlank, 8),
                Cost("Magenheim_Underworld_Resource_Understone", 12),
                Cost(UnderworldFungalRefinementCatalog.SpireCord, 5),
                Cost(UnderworldFungalRefinementCatalog.CuredGlowcap, 3));

            AddRecipe(
                NowhereKingRewardRegistrar.PairedLastArgumentPrefabName,
                Cost(NowhereKingRewardRegistrar.FirmamentPrefabName, 1),
                Cost(NowhereKingRewardRegistrar.NullGatePrefabName, 1),
                Cost(UnderworldFungalRefinementCatalog.SpireCord, 2),
                Cost(UnderworldFungalRefinementCatalog.CuredGlowcap, 1));

            AddRecipe(
                NowhereKingRewardRegistrar.NullMantlePrefabName,
                Cost(UnderworldFungalRefinementCatalog.WorldrootPlank, 4),
                Cost("Magenheim_Underworld_Resource_Understone", 4),
                Cost(UnderworldFungalRefinementCatalog.SpireCord, 12),
                Cost(UnderworldFungalRefinementCatalog.CuredGlowcap, 5));

            AddRecipe(
                NowhereKingRewardRegistrar.TrophyPrefabName,
                Cost(UnderworldFungalRefinementCatalog.WorldrootPlank, 4),
                Cost("Magenheim_Underworld_Resource_Understone", 6),
                Cost(UnderworldFungalRefinementCatalog.SpireCord, 2),
                Cost(UnderworldFungalRefinementCatalog.CuredGlowcap, 1));

            _registered = true;
            _log.LogInfo(
                "Registered post-Nowhere-King Mycelial Bench recipes for Firmament, Null Gate, " +
                "the paired Last Argument knives, the Null Mantle and the Nowhere King trophy.");
        }
        catch (Exception exception)
        {
            _log.LogError("Nowhere King post-victory crafting registration failed: " + exception);
            throw;
        }
        finally { Dispose(); }
    }

    private static void AddRecipe(string item, params Ingredient[] ingredients)
    {
        Require(item);
        var config = new RecipeConfig
        {
            Name = "Magenheim_Recipe_PostKing_" + item,
            Item = item,
            Amount = 1,
            CraftingStation = UnderworldWeaponUpgradeCatalog.MycelialBenchPrefab,
            RepairStation = UnderworldWeaponUpgradeCatalog.MycelialBenchPrefab,
            MinStationLevel = 1,
            Enabled = true,
        };

        foreach (var ingredient in ingredients)
        {
            Require(ingredient.Prefab);
            config.AddRequirement(ingredient.Prefab, ingredient.Amount);
        }

        if (!ItemManager.Instance.AddRecipe(new CustomRecipe(config)))
            throw new InvalidOperationException("Jotunn refused post-King recipe " + config.Name);
    }

    private static void Require(string prefab)
    {
        if (PrefabManager.Instance.GetPrefab(prefab) is null && !CustomItem.IsCustomItem(prefab))
            throw new InvalidOperationException("Required post-King crafting dependency unavailable: " + prefab);
    }

    private static Ingredient Cost(string prefab, int amount) => new(prefab, amount);

    private readonly struct Ingredient
    {
        internal Ingredient(string prefab, int amount) { Prefab = prefab; Amount = amount; }
        internal string Prefab { get; }
        internal int Amount { get; }
    }

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterRecipes;
        _subscribed = false;
    }
}
