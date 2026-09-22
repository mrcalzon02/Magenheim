using System;
using System.Collections.Generic;
namespace Magenheim.Core.Underworld;

public sealed record UnderworldResourceDefinition(string Id, string Prefab, string Name,
    UnderworldTerrainBiome Biome, string ItemDonor, string PickupDonor, string PlannedSource)
{
    public string PickupPrefab => "Magenheim_Underworld_ResourcePickup_" + Prefab.Substring("Magenheim_Underworld_Resource_".Length);
}

/// <summary>Raw resources from the flora/terrain plan. Donors are visual placeholders;
/// pickup prototypes do not imply natural world population or progression admission.</summary>
public static class UnderworldResourceCatalog
{
    public static IReadOnlyList<UnderworldResourceDefinition> All { get; } = Array.AsReadOnly(new[]
    {
        new UnderworldResourceDefinition("magenheim.underworld.resource.worldroot_timber", "Magenheim_Underworld_Resource_WorldrootTimber", "Worldroot Timber", UnderworldTerrainBiome.FungalForest, "RoundLog", "Pickable_Branch", "Shed root and dead fungal trunks"),
        new UnderworldResourceDefinition("magenheim.underworld.resource.glowcap_flesh", "Magenheim_Underworld_Resource_GlowcapFlesh", "Glowcap Flesh", UnderworldTerrainBiome.FungalForest, "MushroomMagecap", "Pickable_Mushroom_Magecap", "Glowcap clusters"),
        new UnderworldResourceDefinition("magenheim.underworld.resource.spire_fibre", "Magenheim_Underworld_Resource_SpireFibre", "Spire Fibre", UnderworldTerrainBiome.FungalForest, "Flax", "Pickable_Branch", "Spirestalk fibre bundles"),
        new UnderworldResourceDefinition("magenheim.underworld.resource.understone", "Magenheim_Underworld_Resource_Understone", "Understone", UnderworldTerrainBiome.FungalForest, "Stone", "Pickable_Stone", "Loose understone and outcrops"),
        new UnderworldResourceDefinition("magenheim.underworld.resource.blackwater_flowstone", "Magenheim_Underworld_Resource_BlackwaterFlowstone", "Blackwater Flowstone", UnderworldTerrainBiome.BlackwaterDeep, "Stone", "Pickable_Stone", "Shore and lakebed flowstone"),
        new UnderworldResourceDefinition("magenheim.underworld.resource.pale_fibre", "Magenheim_Underworld_Resource_PaleFibre", "Pale Fibre", UnderworldTerrainBiome.BlackwaterDeep, "Flax", "Pickable_Branch", "Bank vegetation"),
        new UnderworldResourceDefinition("magenheim.underworld.resource.blackwater_pearl", "Magenheim_Underworld_Resource_BlackwaterPearl", "Blackwater Pearl", UnderworldTerrainBiome.BlackwaterDeep, "AmberPearl", "Pickable_Stone", "Shore deposits; shellfish loot pending"),
        new UnderworldResourceDefinition("magenheim.underworld.resource.deep_salt", "Magenheim_Underworld_Resource_DeepSalt", "Deep Salt", UnderworldTerrainBiome.BlackwaterDeep, "Crystal", "Pickable_Stone", "Shore salt crusts"),
        new UnderworldResourceDefinition("magenheim.underworld.resource.slagstone", "Magenheim_Underworld_Resource_Slagstone", "Slagstone", UnderworldTerrainBiome.SulfurousWastes, "Stone", "Pickable_Stone", "Volcanic scree"),
        new UnderworldResourceDefinition("magenheim.underworld.resource.sulfur", "Magenheim_Underworld_Resource_Sulfur", "Sulfur", UnderworldTerrainBiome.SulfurousWastes, "Resin", "Pickable_Stone", "Vent mineral crusts"),
        new UnderworldResourceDefinition("magenheim.underworld.resource.charred_timber", "Magenheim_Underworld_Resource_CharredTimber", "Charred Timber", UnderworldTerrainBiome.SulfurousWastes, "Coal", "Pickable_Branch", "Charred root debris"),
        new UnderworldResourceDefinition("magenheim.underworld.resource.emberiron", "Magenheim_Underworld_Resource_Emberiron", "Emberiron", UnderworldTerrainBiome.SulfurousWastes, "IronScrap", "Pickable_Stone", "Ore seams; mining conversion pending"),
        new UnderworldResourceDefinition("magenheim.underworld.resource.rimewood", "Magenheim_Underworld_Resource_Rimewood", "Rimewood", UnderworldTerrainBiome.FrozenCaverns, "RoundLog", "Pickable_Branch", "Frozen root debris"),
        new UnderworldResourceDefinition("magenheim.underworld.resource.clear_ice", "Magenheim_Underworld_Resource_ClearIce", "Clear Ice", UnderworldTerrainBiome.FrozenCaverns, "Crystal", "Pickable_Stone", "Ice deposits"),
        new UnderworldResourceDefinition("magenheim.underworld.resource.rimesilver", "Magenheim_Underworld_Resource_Rimesilver", "Rimesilver", UnderworldTerrainBiome.FrozenCaverns, "SilverOre", "Pickable_Stone", "Ore seams; mining conversion pending"),
        new UnderworldResourceDefinition("magenheim.underworld.resource.fracture_crystal", "Magenheim_Underworld_Resource_FractureCrystal", "Fracture Crystal", UnderworldTerrainBiome.FractureZones, "Crystal", "Pickable_Stone", "Crystal seams"),
        new UnderworldResourceDefinition("magenheim.underworld.resource.shardstone", "Magenheim_Underworld_Resource_Shardstone", "Shardstone", UnderworldTerrainBiome.FractureZones, "Stone", "Pickable_Stone", "Fault scree"),
        new UnderworldResourceDefinition("magenheim.underworld.resource.titanbone", "Magenheim_Underworld_Resource_Titanbone", "Titanbone", UnderworldTerrainBiome.FractureZones, "BoneFragments", "Pickable_Stone", "Ancient bone deposits"),
        new UnderworldResourceDefinition("magenheim.underworld.resource.rotwood", "Magenheim_Underworld_Resource_Rotwood", "Rotwood", UnderworldTerrainBiome.GreatDecay, "Wood", "Pickable_Branch", "Decaying root debris"),
        new UnderworldResourceDefinition("magenheim.underworld.resource.decay_spore", "Magenheim_Underworld_Resource_DecaySpore", "Decay Spore", UnderworldTerrainBiome.GreatDecay, "Ooze", "Pickable_Mushroom_Magecap", "Rotcap clusters"),
        new UnderworldResourceDefinition("magenheim.underworld.resource.carrion_amber", "Magenheim_Underworld_Resource_CarrionAmber", "Carrion Amber", UnderworldTerrainBiome.GreatDecay, "Amber", "Pickable_Stone", "Amber-bearing deposits"),
        new UnderworldResourceDefinition("magenheim.underworld.resource.bone_gravel", "Magenheim_Underworld_Resource_BoneGravel", "Bone Gravel", UnderworldTerrainBiome.GreatDecay, "BoneFragments", "Pickable_Stone", "Ossuary scree"),
    });
}
