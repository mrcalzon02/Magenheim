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
        return assertions;
    }
}
