using System;
using System.Linq;
using Magenheim.Core.Underworld;

internal static class UnderworldBiomeRefinementCatalogTests
{
    public static int Run()
    {
        var assertions=0;
        void Check(bool value,string message){assertions++;if(!value)throw new InvalidOperationException("Underworld biome refinement assertion "+assertions+" failed: "+message);}

        var all=UnderworldBiomeRefinementCatalog.All;
        Check(all.Count==15,"Five post-Fungal biomes require exactly fifteen processed materials.");
        Check(all.Select(x=>x.Prefab).Distinct(StringComparer.Ordinal).Count()==all.Count,"Refinement prefab identities must be unique.");
        Check(all.All(x=>x.OutputAmount>0&&x.Costs.Count>0&&x.Costs.All(c=>c.Amount>0)),"Every refinement needs positive output and positive raw costs.");

        foreach(var biome in new[]{UnderworldTerrainBiome.BlackwaterDeep,UnderworldTerrainBiome.SulfurousWastes,
            UnderworldTerrainBiome.FrozenCaverns,UnderworldTerrainBiome.FractureZones,UnderworldTerrainBiome.GreatDecay})
        {
            var rows=all.Where(x=>x.Biome==biome).ToArray();
            Check(rows.Length==3,biome+" must own exactly three processed materials.");
            var station=UnderworldStationCatalog.All.Single(x=>x.Biome==biome).Prefab;
            Check(rows.All(x=>x.StationPrefab==station),biome+" refinements must belong to the biome station.");
        }

        var raw=UnderworldResourceCatalog.All.Select(x=>x.Prefab).ToHashSet(StringComparer.Ordinal);
        Check(all.SelectMany(x=>x.Costs).All(c=>raw.Contains(c.Prefab)),"Post-Fungal refinement recipes may consume only canonical raw Underworld resources.");

        var consumed=UnderworldEquipmentCatalog.All.SelectMany(x=>x.Costs).Select(x=>x.Prefab)
            .Concat(UnderworldWeaponUpgradeCatalog.All.SelectMany(x=>x.Ingredients).Select(x=>x.Prefab))
            .ToHashSet(StringComparer.Ordinal);
        foreach(var refinement in all)
            Check(consumed.Contains(refinement.Prefab),refinement.Name+" must have at least one admitted equipment or weapon consumer.");

        return assertions;
    }
}
