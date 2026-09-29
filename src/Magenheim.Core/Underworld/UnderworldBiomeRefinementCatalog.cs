using System;
using System.Collections.Generic;
using System.Linq;

namespace Magenheim.Core.Underworld;

public sealed record UnderworldBiomeRefinementCost(string Prefab,int Amount);

public sealed record UnderworldBiomeRefinementDefinition(
    string Prefab,
    string Name,
    string DonorPrefab,
    UnderworldTerrainBiome Biome,
    string StationPrefab,
    int OutputAmount,
    IReadOnlyList<UnderworldBiomeRefinementCost> Costs);

/// <summary>
/// Processed materials for the five post-Fungal Underworld biomes. Every definition sits between
/// raw ecology and finished equipment/weapons so later stations are actual processing tiers.
/// </summary>
public static class UnderworldBiomeRefinementCatalog
{
    public const string FlowstonePlate="Magenheim_Underworld_Refined_FlowstonePlate";
    public const string PaleCord="Magenheim_Underworld_Refined_PaleCord";
    public const string BrinedPearl="Magenheim_Underworld_Refined_BrinedPearl";

    public const string EmberironBar="Magenheim_Underworld_Refined_EmberironBar";
    public const string TemperedSlag="Magenheim_Underworld_Refined_TemperedSlag";
    public const string CharredRootGrip="Magenheim_Underworld_Refined_CharredRootGrip";

    public const string RimesilverBar="Magenheim_Underworld_Refined_RimesilverBar";
    public const string IceglassLens="Magenheim_Underworld_Refined_IceglassLens";
    public const string RimewoodLaminate="Magenheim_Underworld_Refined_RimewoodLaminate";

    public const string TitanbonePlate="Magenheim_Underworld_Refined_TitanbonePlate";
    public const string ShardstoneBlock="Magenheim_Underworld_Refined_ShardstoneBlock";
    public const string FracturePrism="Magenheim_Underworld_Refined_FracturePrism";

    public const string CarrionAmberSeal="Magenheim_Underworld_Refined_CarrionAmberSeal";
    public const string OssuaryComposite="Magenheim_Underworld_Refined_OssuaryComposite";
    public const string RotwoodLaminate="Magenheim_Underworld_Refined_RotwoodLaminate";

    public static IReadOnlyList<UnderworldBiomeRefinementDefinition> All { get; }=Array.AsReadOnly(new[]
    {
        Def(FlowstonePlate,"Flowstone Plate","Stone",UnderworldTerrainBiome.BlackwaterDeep,UnderworldStationCatalog.TidalBasinPrefab,2,
            Cost("Magenheim_Underworld_Resource_BlackwaterFlowstone",3),Cost("Magenheim_Underworld_Resource_DeepSalt",1)),
        Def(PaleCord,"Pale Cord","LinenThread",UnderworldTerrainBiome.BlackwaterDeep,UnderworldStationCatalog.TidalBasinPrefab,2,
            Cost("Magenheim_Underworld_Resource_PaleFibre",4),Cost("Magenheim_Underworld_Resource_DeepSalt",1)),
        Def(BrinedPearl,"Brined Pearl","AmberPearl",UnderworldTerrainBiome.BlackwaterDeep,UnderworldStationCatalog.TidalBasinPrefab,1,
            Cost("Magenheim_Underworld_Resource_BlackwaterPearl",2),Cost("Magenheim_Underworld_Resource_DeepSalt",1)),

        Def(EmberironBar,"Emberiron Bar","Iron",UnderworldTerrainBiome.SulfurousWastes,UnderworldStationCatalog.FurnaceHeartForgePrefab,1,
            Cost("Magenheim_Underworld_Resource_Emberiron",2),Cost("Magenheim_Underworld_Resource_Slagstone",1),Cost("Magenheim_Underworld_Resource_Sulfur",1)),
        Def(TemperedSlag,"Tempered Slag Plate","Iron",UnderworldTerrainBiome.SulfurousWastes,UnderworldStationCatalog.FurnaceHeartForgePrefab,2,
            Cost("Magenheim_Underworld_Resource_Slagstone",4),Cost("Magenheim_Underworld_Resource_Sulfur",1)),
        Def(CharredRootGrip,"Charred Root Grip","FineWood",UnderworldTerrainBiome.SulfurousWastes,UnderworldStationCatalog.FurnaceHeartForgePrefab,2,
            Cost("Magenheim_Underworld_Resource_CharredTimber",3),Cost("Magenheim_Underworld_Resource_Sulfur",1)),

        Def(RimesilverBar,"Rimesilver Bar","Silver",UnderworldTerrainBiome.FrozenCaverns,UnderworldStationCatalog.SilenceTablePrefab,1,
            Cost("Magenheim_Underworld_Resource_Rimesilver",2),Cost("Magenheim_Underworld_Resource_ClearIce",1)),
        Def(IceglassLens,"Iceglass Lens","Crystal",UnderworldTerrainBiome.FrozenCaverns,UnderworldStationCatalog.SilenceTablePrefab,2,
            Cost("Magenheim_Underworld_Resource_ClearIce",4),Cost("Magenheim_Underworld_Resource_Rimesilver",1)),
        Def(RimewoodLaminate,"Rimewood Laminate","FineWood",UnderworldTerrainBiome.FrozenCaverns,UnderworldStationCatalog.SilenceTablePrefab,2,
            Cost("Magenheim_Underworld_Resource_Rimewood",3),Cost("Magenheim_Underworld_Resource_ClearIce",1)),

        Def(TitanbonePlate,"Titanbone Plate","BoneFragments",UnderworldTerrainBiome.FractureZones,UnderworldStationCatalog.AnchorForgePrefab,2,
            Cost("Magenheim_Underworld_Resource_Titanbone",4),Cost("Magenheim_Underworld_Resource_Shardstone",1)),
        Def(ShardstoneBlock,"Shardstone Block","Stone",UnderworldTerrainBiome.FractureZones,UnderworldStationCatalog.AnchorForgePrefab,2,
            Cost("Magenheim_Underworld_Resource_Shardstone",4),Cost("Magenheim_Underworld_Resource_FractureCrystal",1)),
        Def(FracturePrism,"Fracture Prism","Crystal",UnderworldTerrainBiome.FractureZones,UnderworldStationCatalog.AnchorForgePrefab,1,
            Cost("Magenheim_Underworld_Resource_FractureCrystal",3),Cost("Magenheim_Underworld_Resource_Titanbone",1)),

        Def(CarrionAmberSeal,"Carrion Amber Seal","Amber",UnderworldTerrainBiome.GreatDecay,UnderworldStationCatalog.CrownReliquaryPrefab,1,
            Cost("Magenheim_Underworld_Resource_CarrionAmber",2),Cost("Magenheim_Underworld_Resource_DecaySpore",1)),
        Def(OssuaryComposite,"Ossuary Composite","BoneFragments",UnderworldTerrainBiome.GreatDecay,UnderworldStationCatalog.CrownReliquaryPrefab,2,
            Cost("Magenheim_Underworld_Resource_BoneGravel",4),Cost("Magenheim_Underworld_Resource_DecaySpore",1)),
        Def(RotwoodLaminate,"Rotwood Laminate","FineWood",UnderworldTerrainBiome.GreatDecay,UnderworldStationCatalog.CrownReliquaryPrefab,2,
            Cost("Magenheim_Underworld_Resource_Rotwood",3),Cost("Magenheim_Underworld_Resource_CarrionAmber",1)),
    });

    static UnderworldBiomeRefinementCatalog()
    {
        if(All.Count!=15)throw new InvalidOperationException("Five post-Fungal biomes require exactly fifteen refinements.");
        if(All.Select(x=>x.Prefab).Distinct(StringComparer.Ordinal).Count()!=All.Count)
            throw new InvalidOperationException("Underworld refinement prefab identities must be unique.");
        foreach(var biome in new[]{UnderworldTerrainBiome.BlackwaterDeep,UnderworldTerrainBiome.SulfurousWastes,
            UnderworldTerrainBiome.FrozenCaverns,UnderworldTerrainBiome.FractureZones,UnderworldTerrainBiome.GreatDecay})
        {
            var rows=All.Where(x=>x.Biome==biome).ToArray();
            if(rows.Length!=3)throw new InvalidOperationException(biome+" must own exactly three processed materials.");
            var station=UnderworldStationCatalog.All.Single(x=>x.Biome==biome).Prefab;
            if(rows.Any(x=>x.StationPrefab!=station))throw new InvalidOperationException(biome+" refinement station drift.");
        }
    }

    private static UnderworldBiomeRefinementDefinition Def(
        string prefab,string name,string donor,UnderworldTerrainBiome biome,string station,int output,
        params UnderworldBiomeRefinementCost[] costs)=>
        new(prefab,name,donor,biome,station,output,Array.AsReadOnly(costs));

    private static UnderworldBiomeRefinementCost Cost(string prefab,int amount)=>new(prefab,amount);
}
