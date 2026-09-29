using System;
using System.Linq;
using Magenheim.Core.Underworld;

internal static class UnderworldArmourBalanceCatalogTests
{
    public static int Run()
    {
        var assertions=0;
        void Check(bool value,string message){assertions++;if(!value)throw new InvalidOperationException("Underworld armour balance assertion "+assertions+" failed: "+message);}

        var biomes=Enum.GetValues(typeof(UnderworldTerrainBiome)).Cast<UnderworldTerrainBiome>().ToArray();
        var slots=new[]{UnderworldEquipmentSlot.Helmet,UnderworldEquipmentSlot.Chest,UnderworldEquipmentSlot.Legs,UnderworldEquipmentSlot.Cape};
        foreach(var biome in biomes)
        foreach(var slot in slots)
        {
            var row=UnderworldArmourBalanceCatalog.Require(biome,slot);
            Check(row.Armor>0&&row.ArmorPerQuality>=0,biome+"/"+slot+" needs positive armor.");
            Check(row.Weight>0&&row.MaxDurability>0&&row.DurabilityPerQuality>=0,biome+"/"+slot+" needs positive physical stats.");
            Check(row.MovementModifier<=0&&row.MovementModifier>=-.08f,biome+"/"+slot+" movement modifier escaped authored bounds.");
        }

        foreach(var slot in slots)
        {
            var previous=0f;
            foreach(var biome in biomes)
            {
                var armor=UnderworldArmourBalanceCatalog.Require(biome,slot).Armor;
                Check(armor>previous,slot+" armor must rise strictly through biome progression.");
                previous=armor;
            }
        }

        Check(UnderworldArmourBalanceCatalog.Require(UnderworldTerrainBiome.FungalForest,UnderworldEquipmentSlot.Chest).MovementModifier >
              UnderworldArmourBalanceCatalog.Require(UnderworldTerrainBiome.FractureZones,UnderworldEquipmentSlot.Chest).MovementModifier,
              "Light Fungal chest gear must remain faster than Stoneanchor.");
        Check(biomes.All(x=>UnderworldArmourBalanceCatalog.FullSetAtmosphereBonus(x)>0),
              "Every complete Underworld armour set needs a distinct atmosphere bonus.");
        Check(Math.Abs(UnderworldArmourBalanceCatalog.PalewaterSwimmingReduction(0))<.0001,
              "Palewater swim benefit must require equipped pieces.");
        Check(Math.Abs(UnderworldArmourBalanceCatalog.PalewaterSwimmingReduction(4)-.20d)<.0001,
              "Complete Palewater set must reduce Blackwater swim stamina by exactly 20%.");
        Check(Math.Abs(UnderworldArmourBalanceCatalog.StoneanchorKnockbackReduction(4)-.40d)<.0001,
              "Complete Stoneanchor set must reduce Fracture knockback by exactly 40%.");
        Check(Enumerable.Range(0,4).All(i=>
              UnderworldArmourBalanceCatalog.PalewaterSwimmingReduction(i)<=UnderworldArmourBalanceCatalog.PalewaterSwimmingReduction(i+1)),
              "Palewater swim utility must be monotonic by piece count.");
        Check(Enumerable.Range(0,4).All(i=>
              UnderworldArmourBalanceCatalog.StoneanchorKnockbackReduction(i)<=UnderworldArmourBalanceCatalog.StoneanchorKnockbackReduction(i+1)),
              "Stoneanchor knockback utility must be monotonic by piece count.");
        return assertions;
    }
}
