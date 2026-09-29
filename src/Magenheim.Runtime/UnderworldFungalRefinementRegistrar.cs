using System;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>
/// Materializes the first Underworld refining rung at the Mycelial Bench. Donor items provide
/// native inventory behavior only; the owned prefab identities and recipes remain Magenheim-owned.
/// </summary>
internal sealed class UnderworldFungalRefinementRegistrar : IDisposable
{
    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal UnderworldFungalRefinementRegistrar(ManualLogSource log) =>
        _log = log ?? throw new ArgumentNullException(nameof(log));

    internal void Register()
    {
        if (_subscribed || _registered) return;
        PrefabManager.OnVanillaPrefabsAvailable += RegisterRefinements;
        _subscribed = true;
    }

    private void RegisterRefinements()
    {
        if (_registered) return;
        var items = 0;
        var recipes = 0;
        try
        {
            RequirePrefab(UnderworldWeaponUpgradeCatalog.MycelialBenchPrefab);

            foreach (var definition in UnderworldFungalRefinementCatalog.All)
            {
                RegisterItem(definition);
                items++;
            }

            foreach (var definition in UnderworldFungalRefinementCatalog.All)
            {
                RegisterRecipe(definition);
                recipes++;
            }

            _registered = true;
            _log.LogInfo(
                "Registered " + items + " Fungal Forest refined materials and " + recipes +
                " Mycelial Bench refinement recipes.");
        }
        catch (Exception exception)
        {
            _log.LogError(
                "Fungal Forest refinement registration failed after " + items +
                " items and " + recipes + " recipes: " + exception);
        }
        finally
        {
            Dispose();
        }
    }

    private static void RegisterItem(UnderworldFungalRefinementDefinition definition)
    {
        RequirePrefab(definition.DonorPrefab);
        if (PrefabManager.Instance.GetPrefab(definition.Prefab) || CustomItem.IsCustomItem(definition.Prefab))
            throw new InvalidOperationException("Occupied Fungal refinement identity: " + definition.Prefab);

        var item = new CustomItem(definition.Prefab, definition.DonorPrefab);
        var shared = item.ItemDrop.m_itemData.m_shared;
        shared.m_name = definition.Name;
        shared.m_description =
            "Refined at the Mycelial Bench from Fungal Forest ecology. " +
            "This material exists to carry the Underworld progression dependency into equipment and construction.";
        shared.m_itemType = ItemDrop.ItemData.ItemType.Material;
        shared.m_maxStackSize = 50;
        shared.m_weight = 0.5f;
        shared.m_value = 0;
        shared.m_dlc = string.Empty;
        shared.m_food = 0f;
        shared.m_foodStamina = 0f;
        shared.m_foodEitr = 0f;
        shared.m_foodBurnTime = 0f;
        shared.m_foodRegen = 0f;
        shared.m_consumeStatusEffect = null;
        var model = UnderworldResourceVisuals.ModelFor(definition.Prefab)
            ?? throw new InvalidOperationException("Missing authored Fungal refinement visual mapping: " + definition.Prefab);
        shared.m_icons = new[] { EarthAssets.Icon(model) };
        UnderworldResourceVisuals.Apply(item.ItemPrefab, model);

        if (!ItemManager.Instance.AddItem(item))
            throw new InvalidOperationException("Jotunn refused Fungal refinement item " + definition.Prefab);
    }

    private static void RegisterRecipe(UnderworldFungalRefinementDefinition definition)
    {
        foreach (var cost in definition.Costs) RequirePrefab(cost.Prefab);

        var config = new RecipeConfig
        {
            Name = "Magenheim_Recipe_" + definition.Prefab,
            Item = definition.Prefab,
            Amount = definition.OutputAmount,
            CraftingStation = UnderworldWeaponUpgradeCatalog.MycelialBenchPrefab,
            RepairStation = UnderworldWeaponUpgradeCatalog.MycelialBenchPrefab,
            MinStationLevel = 1,
            Enabled = true,
        };
        foreach (var cost in definition.Costs)
            config.AddRequirement(cost.Prefab, cost.Amount);

        if (!ItemManager.Instance.AddRecipe(new CustomRecipe(config)))
            throw new InvalidOperationException("Jotunn refused Fungal refinement recipe " + config.Name);
    }

    private static void RequirePrefab(string prefab)
    {
        if (PrefabManager.Instance.GetPrefab(prefab) is null && !CustomItem.IsCustomItem(prefab))
            throw new InvalidOperationException("Required Fungal refinement dependency unavailable: " + prefab);
    }

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterRefinements;
        _subscribed = false;
    }
}
