using System;
using System.Linq;
using Magenheim.Core.Underworld;
internal static class UnderworldResourceTests
{
 internal static int Run()
 {
  var assertions = 0;
  void Check(bool value) { assertions++; if (!value) throw new InvalidOperationException("Resource catalog contract failed: " + assertions); }
  var all = UnderworldResourceCatalog.All;
  Check(all.Count == 22);
  Check(all.Select(x => x.Id).Distinct().Count() == all.Count);
  Check(all.Select(x => x.Prefab).Distinct().Count() == all.Count);
  Check(all.Select(x => x.PickupPrefab).Distinct().Count() == all.Count);
  foreach (UnderworldTerrainBiome biome in Enum.GetValues(typeof(UnderworldTerrainBiome)))
   Check(all.Count(x => x.Biome == biome) >= 3);
  Check(all.Any(x => x.Id == UnderworldArchitectureValidator.WorldrootTimberResourceId));
  Check(all.Any(x => x.Id == UnderworldArchitectureValidator.UnderstoneResourceId));
  foreach (var item in all)
  {
   Check(item.Id.StartsWith("magenheim.underworld.resource.") && !string.IsNullOrWhiteSpace(item.PlannedSource));
   Check(item.ItemDonor != item.Prefab && !string.IsNullOrWhiteSpace(item.ItemDonor));
   Check(item.PickupDonor.StartsWith("Pickable_") && item.PickupPrefab.StartsWith("Magenheim_Underworld_ResourcePickup_"));
  }

  var cell = new UnderworldInstanceChunkKey(-3, 5);
  var first = UnderworldResourcePlacementPlanner.Plan(918273, cell, UnderworldTerrainBiome.SulfurousWastes);
  var second = UnderworldResourcePlacementPlanner.Plan(918273, cell, UnderworldTerrainBiome.SulfurousWastes);
  Check(first.Count == UnderworldResourcePlacementPlanner.SlotsPerCell);
  Check(first.SequenceEqual(second));
  var allowed = all.Where(x => x.Biome == UnderworldTerrainBiome.SulfurousWastes).Select(x => x.Id).ToHashSet();
  Check(first.All(x => x.Cell == cell && x.Biome == UnderworldTerrainBiome.SulfurousWastes));
  Check(first.All(x => allowed.Contains(x.ResourceId)));
  Check(first.Select(x => x.Slot).SequenceEqual(Enumerable.Range(0, UnderworldResourcePlacementPlanner.SlotsPerCell)));
  var minX = cell.X * UnderworldEcologyCells.SizeMeters + UnderworldResourcePlacementPlanner.EdgeInsetMeters;
  var minZ = cell.Z * UnderworldEcologyCells.SizeMeters + UnderworldResourcePlacementPlanner.EdgeInsetMeters;
  var maxX = (cell.X + 1) * UnderworldEcologyCells.SizeMeters - UnderworldResourcePlacementPlanner.EdgeInsetMeters;
  var maxZ = (cell.Z + 1) * UnderworldEcologyCells.SizeMeters - UnderworldResourcePlacementPlanner.EdgeInsetMeters;
  Check(first.All(x => x.X >= minX && x.X < maxX && x.Z >= minZ && x.Z < maxZ));
  Check(first.All(x => x.RotationDegrees >= 0d && x.RotationDegrees < 360d));
  var otherCell = UnderworldResourcePlacementPlanner.Plan(918273, new UnderworldInstanceChunkKey(cell.X + 1, cell.Z), UnderworldTerrainBiome.SulfurousWastes);
  Check(!first.SequenceEqual(otherCell));
  Check(UnderworldResourcePlacementPlanner.Plan(918273, cell, (UnderworldTerrainBiome)int.MaxValue).Count == 0);

  foreach (UnderworldTerrainBiome biome in Enum.GetValues(typeof(UnderworldTerrainBiome)))
  {
   var planned = UnderworldResourcePlacementPlanner.Plan(-44129, cell, biome);
   var biomeResources = all.Where(x => x.Biome == biome).Select(x => x.Id).ToHashSet();
   Check(planned.Count == UnderworldResourcePlacementPlanner.SlotsPerCell);
   Check(planned.All(x => x.Cell == cell && x.Biome == biome));
   Check(planned.All(x => biomeResources.Contains(x.ResourceId)));
   Check(planned.SequenceEqual(UnderworldResourcePlacementPlanner.Plan(-44129, cell, biome)));
  }
  return assertions;
 }
}
