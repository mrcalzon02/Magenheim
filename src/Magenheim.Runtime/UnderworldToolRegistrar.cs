using System;
using System.Linq;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

internal sealed class UnderworldToolRegistrar:IDisposable
{
    internal const string SporelightPrefab="Magenheim_Underworld_Tool_SporelightLantern";
    internal const string DivingBellPrefab="Magenheim_Underworld_Tool_DivingBellHood";
    internal const string SlagPickPrefab="Magenheim_Underworld_Tool_SlagPick";
    internal const string RimeChiselPrefab="Magenheim_Underworld_Tool_RimeChisel";
    internal const string AnchorSpikePrefab="Magenheim_Underworld_Tool_AnchorSpike";
    internal const string CenserPrefab="Magenheim_Underworld_Tool_DefiantCenser";
    internal const string AnchorPieceTable="Magenheim_Underworld_AnchorSpikeTable";

    private readonly ManualLogSource _log;
    private bool _subscribed,_registered;
    private CustomPieceTable? _anchorTable;

    internal UnderworldToolRegistrar(ManualLogSource log)=>_log=log??throw new ArgumentNullException(nameof(log));

    internal void Register()
    {
        if(_subscribed||_registered)return;
        PrefabManager.OnVanillaPrefabsAvailable+=RegisterTools;_subscribed=true;
    }

    private void RegisterTools()
    {
        if(_registered)return;
        var count=0;
        try
        {
            _anchorTable=new CustomPieceTable(AnchorPieceTable,new PieceTableConfig{CanRemovePieces=false,GuessUsage=true});
            if(!PieceManager.Instance.AddPieceTable(_anchorTable))throw new InvalidOperationException("Jotunn refused Anchor Spike piece table.");
            foreach(var definition in UnderworldEquipmentCatalog.Tools)
            {
                if(string.Equals(definition.Prefab,CenserPrefab,StringComparison.Ordinal))continue;
                AddItem(definition);count++;
            }
            UnderworldToolPlaceables.Register(
                RequireDefinition(SporelightPrefab),RequireDefinition(AnchorSpikePrefab),_anchorTable);
            _registered=true;
            _log.LogInfo($"Registered {count} biome tools plus Sporelight/Anchor deployment pieces; Defiant Censer remains weather-owned.");
        }
        catch(Exception e){_log.LogError($"Underworld tool registration failed after {count}: {e}");throw;}
        finally{Dispose();}
    }

    private void AddItem(UnderworldEquipmentDefinition d)
    {
        var donor=Donor(d.Prefab);RequirePrefab(donor);RequirePrefab(d.StationPrefab);
        foreach(var cost in d.Costs)RequirePrefab(cost.Prefab);
        if(PrefabManager.Instance.GetPrefab(d.Prefab)||CustomItem.IsCustomItem(d.Prefab))
            throw new InvalidOperationException("Occupied Underworld tool identity: "+d.Prefab);

        var item=new CustomItem(d.Prefab,donor);var shared=item.ItemDrop.m_itemData.m_shared;
        shared.m_name=d.Name;shared.m_description=d.GameplayRole;shared.m_maxStackSize=1;
        shared.m_value=0;shared.m_dlc=string.Empty;shared.m_icons=new[]{EarthAssets.Icon(d.ModelId)};

        if(d.Prefab==DivingBellPrefab)
        {
            shared.m_armor=10f;shared.m_armorPerLevel=2f;shared.m_maxQuality=4;
            ModelAssets.LoadSkinnedEquipment(item.ItemPrefab,d.ModelId);
        }
        else
        {
            var root=ModelAssets.Load(item.ItemPrefab,d.ModelId,item:true);
            if(d.Prefab==SporelightPrefab)
            {
                shared.m_useDurability=false;shared.m_damages=new HitData.DamageTypes();shared.m_damagesPerLevel=new HitData.DamageTypes();
                UnderworldToolPlaceables.PreferOwnedLights(item.ItemPrefab,root);
                item.ItemPrefab.AddComponent<UnderworldSporelightOutput>();
            }
            else if(d.Prefab==AnchorSpikePrefab)
            {
                shared.m_buildPieces=_anchorTable?.PieceTable??throw new InvalidOperationException("Anchor piece table unavailable.");
                shared.m_useDurability=true;shared.m_useDurabilityDrain=1f;shared.m_maxDurability=1f;shared.m_destroyBroken=true;
                shared.m_canBeReparied=false;shared.m_maxQuality=1;item.ItemDrop.m_itemData.m_durability=1f;
                shared.m_damages=new HitData.DamageTypes();shared.m_damagesPerLevel=new HitData.DamageTypes();
            }
        }

        if(!ItemManager.Instance.AddItem(item))throw new InvalidOperationException("Jotunn refused "+d.Prefab);
        var recipe=new RecipeConfig{Name="Magenheim_Recipe_"+d.Prefab,Item=d.Prefab,Amount=1,CraftingStation=d.StationPrefab,RepairStation=d.StationPrefab,MinStationLevel=1,Enabled=true};
        foreach(var cost in d.Costs)recipe.AddRequirement(cost.Prefab,cost.Amount);
        if(!ItemManager.Instance.AddRecipe(new CustomRecipe(recipe)))throw new InvalidOperationException("Jotunn refused "+recipe.Name);
    }

    private static string Donor(string prefab)=>prefab switch{
        SporelightPrefab=>"Torch",DivingBellPrefab=>"HelmetCarapace",
        SlagPickPrefab=>"PickaxeBlackMetal",RimeChiselPrefab=>"PickaxeBlackMetal",
        AnchorSpikePrefab=>"Hammer",_=>throw new InvalidOperationException("No tool donor for "+prefab)};

    private static UnderworldEquipmentDefinition RequireDefinition(string prefab)=>
        UnderworldEquipmentCatalog.Tools.Single(x=>string.Equals(x.Prefab,prefab,StringComparison.Ordinal));

    private static void RequirePrefab(string prefab)
    {
        if(PrefabManager.Instance.GetPrefab(prefab) is null&&!CustomItem.IsCustomItem(prefab))
            throw new InvalidOperationException("Required Underworld tool dependency unavailable: "+prefab);
    }

    public void Dispose()
    {
        if(!_subscribed)return;PrefabManager.OnVanillaPrefabsAvailable-=RegisterTools;_subscribed=false;
    }
}
