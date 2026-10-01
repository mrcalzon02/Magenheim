using System;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>Registers the twenty-four owned Underworld armour pieces through native Valheim equipment donors.</summary>
internal sealed class UnderworldArmourRegistrar : IDisposable
{
    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;
    internal UnderworldArmourRegistrar(ManualLogSource log) => _log = log ?? throw new ArgumentNullException(nameof(log));

    internal void Register()
    {
        if (_subscribed || _registered) return;
        PrefabManager.OnVanillaPrefabsAvailable += RegisterArmour;
        _subscribed = true;
    }

    private void RegisterArmour()
    {
        if (_registered) return;
        var count=0;
        try
        {
            foreach(var definition in UnderworldEquipmentCatalog.Armour){RegisterOne(definition);count++;}
            _registered=true;
            _log.LogInfo($"Registered {count} owned Underworld armour pieces through native attach_skin equipment donors.");
        }
        catch(Exception exception)
        {
            _log.LogError($"Underworld armour registration failed after {count} pieces: {exception}");
            throw;
        }
        finally{Dispose();}
    }

    private static void RegisterOne(UnderworldEquipmentDefinition definition)
    {
        var donor=Donor(definition.Slot);
        RequirePrefab(donor);RequirePrefab(definition.StationPrefab);
        foreach(var cost in definition.Costs)RequirePrefab(cost.Prefab);
        if(PrefabManager.Instance.GetPrefab(definition.Prefab)||CustomItem.IsCustomItem(definition.Prefab))
            throw new InvalidOperationException("Occupied Underworld armour identity: "+definition.Prefab);

        var item=new CustomItem(definition.Prefab,donor);
        var shared=item.ItemDrop.m_itemData.m_shared;
        shared.m_name=definition.Name;
        shared.m_description=definition.GameplayRole;
        shared.m_maxStackSize=1;
        shared.m_value=0;
        shared.m_dlc=string.Empty;
        // Keep the donor icon: the archived raw-geometry icon no longer describes this equipment.
        var balance=UnderworldArmourBalanceCatalog.Require(definition.Biome,definition.Slot);
        shared.m_armor=balance.Armor;
        shared.m_armorPerLevel=balance.ArmorPerQuality;
        shared.m_weight=balance.Weight;
        shared.m_movementModifier=balance.MovementModifier;
        shared.m_maxQuality=4;
        shared.m_useDurability=true;
        shared.m_maxDurability=balance.MaxDurability;
        shared.m_durabilityPerLevel=balance.DurabilityPerQuality;
        item.ItemDrop.m_itemData.m_durability=balance.MaxDurability;
        UnderworldDonorArmour.Apply(item.ItemPrefab, definition.ModelId);

        if(!ItemManager.Instance.AddItem(item))
            throw new InvalidOperationException("Jotunn refused Underworld armour item "+definition.Prefab);

        var recipe=new RecipeConfig{
            Name="Magenheim_Recipe_"+definition.Prefab,Item=definition.Prefab,Amount=1,
            CraftingStation=definition.StationPrefab,RepairStation=definition.StationPrefab,
            MinStationLevel=1,Enabled=true,
        };
        foreach(var cost in definition.Costs)recipe.AddRequirement(cost.Prefab,cost.Amount);
        if(!ItemManager.Instance.AddRecipe(new CustomRecipe(recipe)))
            throw new InvalidOperationException("Jotunn refused Underworld armour recipe "+recipe.Name);
    }

    private static string Donor(UnderworldEquipmentSlot slot)=>slot switch{
        UnderworldEquipmentSlot.Helmet=>"HelmetCarapace",
        UnderworldEquipmentSlot.Chest=>"ArmorCarapaceChest",
        UnderworldEquipmentSlot.Legs=>"ArmorCarapaceLegs",
        UnderworldEquipmentSlot.Cape=>"CapeFeather",
        _=>throw new InvalidOperationException("Tool is not an armour equipment slot."),
    };

    private static void RequirePrefab(string prefab)
    {
        if(PrefabManager.Instance.GetPrefab(prefab) is null&&!CustomItem.IsCustomItem(prefab))
            throw new InvalidOperationException("Required Underworld armour dependency unavailable: "+prefab);
    }

    public void Dispose()
    {
        if(!_subscribed)return;
        PrefabManager.OnVanillaPrefabsAvailable-=RegisterArmour;
        _subscribed=false;
    }
}
