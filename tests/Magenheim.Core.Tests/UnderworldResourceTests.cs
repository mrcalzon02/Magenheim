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
  return assertions;
 }
}
