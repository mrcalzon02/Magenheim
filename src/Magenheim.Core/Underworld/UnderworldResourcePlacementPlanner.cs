using System;
using System.Collections.Generic;
using System.Linq;
namespace Magenheim.Core.Underworld;
public static class UnderworldResourcePlacementPlanner
{
 public const int SlotsPerCell=6;
 public const double EdgeInsetMeters=6.0;
 public sealed record Placement(string ResourceId,string PickupPrefab,UnderworldTerrainBiome Biome,UnderworldInstanceChunkKey Cell,int Slot,double X,double Z,double RotationDegrees);
 public static IReadOnlyList<Placement> Plan(int worldSeed,UnderworldInstanceChunkKey cell,UnderworldTerrainBiome biome)
 {
  var resources=UnderworldResourceCatalog.All.Where(x=>x.Biome==biome).ToArray();
  if(resources.Length==0)return Array.Empty<Placement>();
  var random=new CellRandom(UnderworldEcologyCells.Seed(worldSeed,cell));
  var result=new Placement[SlotsPerCell];
  var span=UnderworldEcologyCells.SizeMeters-EdgeInsetMeters*2.0;
  var ox=cell.X*UnderworldEcologyCells.SizeMeters;
  var oz=cell.Z*UnderworldEcologyCells.SizeMeters;
  for(var slot=0;slot<result.Length;slot++){
   var resource=resources[random.Next(resources.Length)];
   result[slot]=new Placement(resource.Id,resource.PickupPrefab,biome,cell,slot,ox+EdgeInsetMeters+random.Unit()*span,oz+EdgeInsetMeters+random.Unit()*span,random.Unit()*360.0);
  }
  return Array.AsReadOnly(result);
 }
 private sealed class CellRandom{
  private uint state;
  internal CellRandom(int seed){state=unchecked((uint)seed);if(state==0)state=0x6D2B79F5u;}
  private uint NextUInt(){var x=state;x^=x<<13;x^=x>>17;x^=x<<5;state=x;return x;}
  internal int Next(int max)=>(int)(NextUInt()%(uint)max);
  internal double Unit()=>NextUInt()/((double)uint.MaxValue+1.0);
 }
}
