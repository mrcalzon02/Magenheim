using System;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>
/// Admits the Defiant Censer through the weather-owned mitigation path. The other five biome tools
/// are registered by UnderworldToolRegistrar because their traversal/harvest/light/deployment
/// mechanics now have their own runtime authority.
/// </summary>
internal sealed class UnderworldDefiantCenserRegistrar : IDisposable
{
    private const string DonorPrefab = "Torch";
    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal UnderworldDefiantCenserRegistrar(ManualLogSource log) =>
        _log = log ?? throw new ArgumentNullException(nameof(log));

    internal void Register()
    {
        if (_subscribed || _registered) return;
        PrefabManager.OnVanillaPrefabsAvailable += RegisterCenser;
        _subscribed = true;
    }

    private void RegisterCenser()
    {
        if (_registered) return;
        try
        {
            var definition = ResolveDefinition();
            RequirePrefab(DonorPrefab);
            RequirePrefab(definition.StationPrefab);
            foreach (var cost in definition.Costs) RequirePrefab(cost.Prefab);

            if (PrefabManager.Instance.GetPrefab(definition.Prefab) ||
                CustomItem.IsCustomItem(definition.Prefab))
                throw new InvalidOperationException(
                    "Occupied Defiant Censer identity: " + definition.Prefab);

            var item = new CustomItem(definition.Prefab, DonorPrefab);
            var shared = item.ItemDrop.m_itemData.m_shared;
            shared.m_name = definition.Name;
            shared.m_description =
                definition.GameplayRole +
                " While held in Great Decay it suppresses local atmospheric contamination and " +
                "particulate density; it is a survival tool, not a weapon.";
            shared.m_maxStackSize = 1;
            shared.m_value = 0;
            shared.m_dlc = string.Empty;
            shared.m_useDurability = false;
            shared.m_damages = new HitData.DamageTypes();
            shared.m_damagesPerLevel = new HitData.DamageTypes();
            shared.m_icons = new[] { EarthAssets.Icon(definition.ModelId) };
            ModelAssets.Load(item.ItemPrefab, definition.ModelId, item: true);

            if (!ItemManager.Instance.AddItem(item))
                throw new InvalidOperationException(
                    "Jotunn refused Defiant Censer item " + definition.Prefab);

            var recipe = new RecipeConfig
            {
                Name = "Magenheim_Recipe_" + definition.Prefab,
                Item = definition.Prefab,
                Amount = 1,
                CraftingStation = definition.StationPrefab,
                RepairStation = definition.StationPrefab,
                MinStationLevel = 1,
                Enabled = true,
            };
            foreach (var cost in definition.Costs)
                recipe.AddRequirement(cost.Prefab, cost.Amount);

            if (!ItemManager.Instance.AddRecipe(new CustomRecipe(recipe)))
                throw new InvalidOperationException(
                    "Jotunn refused Defiant Censer recipe " + recipe.Name);

            _registered = true;
            _log.LogInfo(
                "Registered Defiant Censer as a held Great Decay atmosphere-suppression tool.");
        }
        catch (Exception exception)
        {
            _log.LogError("Defiant Censer registration failed: " + exception);
            throw;
        }
        finally
        {
            Dispose();
        }
    }

    private static UnderworldEquipmentDefinition ResolveDefinition()
    {
        foreach (var definition in UnderworldEquipmentCatalog.Tools)
            if (string.Equals(
                    definition.Prefab,
                    "Magenheim_Underworld_Tool_DefiantCenser",
                    StringComparison.Ordinal))
                return definition;

        throw new InvalidOperationException(
            "Underworld equipment authority no longer contains the canonical Defiant Censer.");
    }

    private static void RequirePrefab(string prefab)
    {
        if (PrefabManager.Instance.GetPrefab(prefab) is null &&
            !CustomItem.IsCustomItem(prefab))
            throw new InvalidOperationException(
                "Required Defiant Censer dependency unavailable: " + prefab);
    }

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterCenser;
        _subscribed = false;
    }
}
