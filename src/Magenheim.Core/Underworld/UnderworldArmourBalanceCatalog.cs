using System;
using System.Collections.Generic;

namespace Magenheim.Core.Underworld;

public sealed record UnderworldArmourBalance(
    float Armor,
    float ArmorPerQuality,
    float Weight,
    float MovementModifier,
    float MaxDurability,
    float DurabilityPerQuality);

/// <summary>
/// Authoritative defensive/weight curve for the six Underworld armour tiers.
/// Values are explicit so cloned vanilla donors cannot leak Carapace/Feather balance into Magenheim.
/// </summary>
public static class UnderworldArmourBalanceCatalog
{
    private static readonly IReadOnlyDictionary<(UnderworldTerrainBiome,UnderworldEquipmentSlot),UnderworldArmourBalance> Values =
        new Dictionary<(UnderworldTerrainBiome,UnderworldEquipmentSlot),UnderworldArmourBalance>
        {
            [(UnderworldTerrainBiome.FungalForest,UnderworldEquipmentSlot.Helmet)]=B(14,2,1.6f,0f,700,90),
            [(UnderworldTerrainBiome.FungalForest,UnderworldEquipmentSlot.Chest)]=B(20,3,4.0f,-.01f,850,110),
            [(UnderworldTerrainBiome.FungalForest,UnderworldEquipmentSlot.Legs)]=B(18,3,3.2f,-.01f,800,105),
            [(UnderworldTerrainBiome.FungalForest,UnderworldEquipmentSlot.Cape)]=B(4,1,1.2f,0f,650,80),

            [(UnderworldTerrainBiome.BlackwaterDeep,UnderworldEquipmentSlot.Helmet)]=B(18,2.5f,2.2f,0f,850,105),
            [(UnderworldTerrainBiome.BlackwaterDeep,UnderworldEquipmentSlot.Chest)]=B(24,3.5f,5.2f,-.02f,1000,125),
            [(UnderworldTerrainBiome.BlackwaterDeep,UnderworldEquipmentSlot.Legs)]=B(22,3.5f,4.2f,-.01f,950,120),
            [(UnderworldTerrainBiome.BlackwaterDeep,UnderworldEquipmentSlot.Cape)]=B(5,1,1.6f,0f,800,95),

            [(UnderworldTerrainBiome.SulfurousWastes,UnderworldEquipmentSlot.Helmet)]=B(22,3,3.0f,-.01f,1000,125),
            [(UnderworldTerrainBiome.SulfurousWastes,UnderworldEquipmentSlot.Chest)]=B(28,4,7.0f,-.04f,1200,150),
            [(UnderworldTerrainBiome.SulfurousWastes,UnderworldEquipmentSlot.Legs)]=B(26,4,5.8f,-.03f,1150,145),
            [(UnderworldTerrainBiome.SulfurousWastes,UnderworldEquipmentSlot.Cape)]=B(6,1.5f,2.2f,-.01f,950,115),

            [(UnderworldTerrainBiome.FrozenCaverns,UnderworldEquipmentSlot.Helmet)]=B(26,3.5f,2.7f,0f,1150,145),
            [(UnderworldTerrainBiome.FrozenCaverns,UnderworldEquipmentSlot.Chest)]=B(32,4.5f,6.0f,-.02f,1350,170),
            [(UnderworldTerrainBiome.FrozenCaverns,UnderworldEquipmentSlot.Legs)]=B(30,4.5f,5.0f,-.02f,1300,165),
            [(UnderworldTerrainBiome.FrozenCaverns,UnderworldEquipmentSlot.Cape)]=B(7,1.5f,1.9f,0f,1100,135),

            [(UnderworldTerrainBiome.FractureZones,UnderworldEquipmentSlot.Helmet)]=B(30,4,3.8f,-.01f,1300,165),
            [(UnderworldTerrainBiome.FractureZones,UnderworldEquipmentSlot.Chest)]=B(36,5,8.5f,-.05f,1550,195),
            [(UnderworldTerrainBiome.FractureZones,UnderworldEquipmentSlot.Legs)]=B(34,5,7.0f,-.04f,1500,190),
            [(UnderworldTerrainBiome.FractureZones,UnderworldEquipmentSlot.Cape)]=B(8,2,2.8f,-.01f,1250,155),

            [(UnderworldTerrainBiome.GreatDecay,UnderworldEquipmentSlot.Helmet)]=B(34,4.5f,3.5f,-.01f,1450,180),
            [(UnderworldTerrainBiome.GreatDecay,UnderworldEquipmentSlot.Chest)]=B(40,5.5f,7.6f,-.03f,1750,220),
            [(UnderworldTerrainBiome.GreatDecay,UnderworldEquipmentSlot.Legs)]=B(38,5.5f,6.4f,-.03f,1700,215),
            [(UnderworldTerrainBiome.GreatDecay,UnderworldEquipmentSlot.Cape)]=B(9,2,2.5f,-.01f,1400,175),
        };

    public static UnderworldArmourBalance Require(UnderworldTerrainBiome biome,UnderworldEquipmentSlot slot)
    {
        if(!Values.TryGetValue((biome,slot),out var value))
            throw new InvalidOperationException($"No Underworld armour balance row for {biome}/{slot}.");
        return value;
    }

    public static double FullSetAtmosphereBonus(UnderworldTerrainBiome biome)=>biome switch
    {
        UnderworldTerrainBiome.FungalForest=>.12d,
        UnderworldTerrainBiome.BlackwaterDeep=>.08d,
        UnderworldTerrainBiome.SulfurousWastes=>.14d,
        UnderworldTerrainBiome.FrozenCaverns=>.14d,
        UnderworldTerrainBiome.FractureZones=>.10d,
        UnderworldTerrainBiome.GreatDecay=>.16d,
        _=>0d,
    };

    private static UnderworldArmourBalance B(float armor,float per,float weight,float move,float durability,float durabilityPer)=>
        new(armor,per,weight,move,durability,durabilityPer);
}
