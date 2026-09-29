using System;
using System.Linq;
using Magenheim.Core.Underworld;

internal static class UnderworldStationPlacementTests
{
    public static int Run()
    {
        var assertions=0;
        void Check(bool value,string message){assertions++;if(!value)throw new InvalidOperationException("Underworld station placement assertion "+assertions+" failed: "+message);}
        UnderworldStationDefinition Station(UnderworldTerrainBiome biome)=>UnderworldStationCatalog.All.Single(x=>x.Biome==biome);
        UnderworldStationPlacementSite Site(UnderworldTerrainBiome biome,double centerWater=0,double minWater=0,double maxWater=0,double span=.3,double hazard=.2,bool vent=false,bool anchorSpike=false,bool attuned=false,bool underworld=true)=>
            new(underworld,biome,centerWater,minWater,maxWater,span,hazard,vent,anchorSpike,attuned);

        foreach(var station in UnderworldStationCatalog.All)
        {
            var wrong=UnderworldStationPlacement.Evaluate(station,Site(station.Biome,underworld:false));
            Check(!wrong.Allowed&&wrong.Diagnostic.Length>0,station.Name+" must reject Surface/non-instance placement.");
            var other=station.Biome==UnderworldTerrainBiome.FungalForest?UnderworldTerrainBiome.GreatDecay:UnderworldTerrainBiome.FungalForest;
            Check(!UnderworldStationPlacement.Evaluate(station,Site(other)).Allowed,station.Name+" must reject the wrong biome.");
        }

        var fungal=Station(UnderworldTerrainBiome.FungalForest);
        Check(UnderworldStationPlacement.Evaluate(fungal,Site(fungal.Biome,span:1.0)).Allowed,"Mycelial Bench admits modest dry relief.");
        Check(!UnderworldStationPlacement.Evaluate(fungal,Site(fungal.Biome,maxWater:.4)).Allowed,"Mycelial Bench rejects flooded ground.");
        Check(!UnderworldStationPlacement.Evaluate(fungal,Site(fungal.Biome,span:1.5)).Allowed,"Mycelial Bench rejects excessive footprint relief.");

        var tidal=Station(UnderworldTerrainBiome.BlackwaterDeep);
        Check(UnderworldStationPlacement.Evaluate(tidal,Site(tidal.Biome,.4,0,.8)).Allowed,"Tidal Basin admits a dry/wet shoreline footprint.");
        Check(!UnderworldStationPlacement.Evaluate(tidal,Site(tidal.Biome,0,0,.1)).Allowed,"Tidal Basin rejects wholly dry ground.");
        Check(!UnderworldStationPlacement.Evaluate(tidal,Site(tidal.Biome,2,1.5,3)).Allowed,"Tidal Basin rejects fully submerged deep water.");

        var furnace=Station(UnderworldTerrainBiome.SulfurousWastes);
        Check(UnderworldStationPlacement.Evaluate(furnace,Site(furnace.Biome,span:1.0,hazard:.9,vent:true)).Allowed,"Furnace Heart uses an actual vent, not low ambient hazard.");
        Check(!UnderworldStationPlacement.Evaluate(furnace,Site(furnace.Biome,span:1.0,vent:false)).Allowed,"Furnace Heart rejects a Sulfur site with no live vent.");

        var silence=Station(UnderworldTerrainBiome.FrozenCaverns);
        Check(UnderworldStationPlacement.Evaluate(silence,Site(silence.Biome,span:.7,hazard:.39)).Allowed,"Silence Table admits a quiet deterministic pocket.");
        Check(!UnderworldStationPlacement.Evaluate(silence,Site(silence.Biome,span:.7,hazard:.41)).Allowed,"Silence Table rejects a noisy/hazardous pocket.");
        Check(!UnderworldStationPlacement.Evaluate(silence,Site(silence.Biome,span:1.1,hazard:.3)).Allowed,"Silence Table rejects rough ground.");

        var anchor=Station(UnderworldTerrainBiome.FractureZones);
        Check(UnderworldStationPlacement.Evaluate(anchor,Site(anchor.Biome,span:.5,hazard:.34)).Allowed,"Anchor Forge admits stable low-hazard Fracture ground.");
        Check(!UnderworldStationPlacement.Evaluate(anchor,Site(anchor.Biome,span:.5,hazard:.36)).Allowed,"Anchor Forge rejects unreinforced tremor-prone hazard.");
        Check(UnderworldStationPlacement.Evaluate(anchor,Site(anchor.Biome,span:.5,hazard:.80,anchorSpike:true)).Allowed,"A deployed Anchor Spike stabilizes otherwise tremor-prone Fracture ground.");
        Check(!UnderworldStationPlacement.Evaluate(anchor,Site(anchor.Biome,span:.8,hazard:.25,anchorSpike:true)).Allowed,"Anchor Spike does not flatten excessive local relief.");

        var crown=Station(UnderworldTerrainBiome.GreatDecay);
        Check(UnderworldStationPlacement.Evaluate(crown,Site(crown.Biome,attuned:true)).Allowed,"Crown Reliquary admits an attuned endgame site.");
        Check(!UnderworldStationPlacement.Evaluate(crown,Site(crown.Biome,attuned:false)).Allowed,"Crown Reliquary rejects an unattuned Decay Deepstone state.");

        var invalid=UnderworldStationPlacement.Evaluate(fungal,Site(fungal.Biome,span:double.NaN));
        Check(!invalid.Allowed,"Non-finite site metrics fail closed.");
        return assertions;
    }
}
