using System;
using HarmonyLib;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

internal static class UnderworldArmourRuntime
{
    internal static int CountMatchingPieces(Player player,UnderworldTerrainBiome biome)
    {
        if(player is null)return 0;
        var count=0;
        foreach(var item in player.GetInventory().GetEquippedItems())
        {
            if(item?.m_dropPrefab is null)continue;
            var prefab=item.m_dropPrefab.name;
            foreach(var definition in UnderworldEquipmentCatalog.Armour)
            {
                if(definition.Biome==biome&&string.Equals(definition.Prefab,prefab,StringComparison.Ordinal))
                {
                    count++;
                    break;
                }
            }
        }
        return Math.Min(4,count);
    }

    internal static float AdjustSwimmingStamina(Player player,float requested)
    {
        if(player is null||requested<=0f||!player.IsSwimming())return requested;
        var p=player.transform.position;
        var terrain=UnderworldTerrainRuntime.SampleInstanceTerrain(p.x,p.y,p.z);
        if(!terrain.Admitted||terrain.Biome!=UnderworldTerrainBiome.BlackwaterDeep)return requested;
        var pieces=CountMatchingPieces(player,UnderworldTerrainBiome.BlackwaterDeep);
        var reduction=(float)UnderworldArmourBalanceCatalog.PalewaterSwimmingReduction(pieces);
        return requested*(1f-reduction);
    }

    internal static void AdjustIncomingKnockback(Player player,HitData hit)
    {
        if(player is null||hit is null||hit.m_pushForce<=0f)return;
        var p=player.transform.position;
        var terrain=UnderworldTerrainRuntime.SampleInstanceTerrain(p.x,p.y,p.z);
        if(!terrain.Admitted||terrain.Biome!=UnderworldTerrainBiome.FractureZones)return;
        var pieces=CountMatchingPieces(player,UnderworldTerrainBiome.FractureZones);
        var reduction=(float)UnderworldArmourBalanceCatalog.StoneanchorKnockbackReduction(pieces);
        if(reduction>0f)hit.m_pushForce*=1f-reduction;
    }
}

[HarmonyPatch(typeof(Player),nameof(Player.UseStamina))]
internal static class PalewaterSwimmingStaminaPatch
{
    [HarmonyPrefix]
    private static void Prefix(Player __instance,ref float v)=>v=UnderworldArmourRuntime.AdjustSwimmingStamina(__instance,v);
}

[HarmonyPatch(typeof(Character),nameof(Character.Damage))]
internal static class StoneanchorKnockbackPatch
{
    [HarmonyPrefix]
    private static void Prefix(Character __instance,HitData hit)
    {
        if(__instance is Player player)UnderworldArmourRuntime.AdjustIncomingKnockback(player,hit);
    }
}
