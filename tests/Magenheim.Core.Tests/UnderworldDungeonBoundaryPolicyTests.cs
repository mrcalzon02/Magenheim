using System;
using System.Linq;
using Magenheim.Core.Underworld;

internal static class UnderworldDungeonBoundaryPolicyTests
{
    public static int Run()
    {
        var count = 0;
        void Check(bool value, string message)
        {
            count++;
            if (!value) throw new InvalidOperationException("Dungeon boundary: " + message);
        }
        foreach (var profile in UnderworldVanillaDungeonReuseCatalog.All)
        {
            var definition = UnderworldDungeonCatalog.All.Single(x => x.Id == profile.DungeonId);
            Check(UnderworldDungeonBoundaryPolicy.SurfaceDonorFor(definition.PrefabName) == profile.DonorEntrancePrefabs[0],
                "Owned saved derivative must restore its native entrance.");
            foreach (var donor in profile.DonorEntrancePrefabs)
                Check(UnderworldDungeonBoundaryPolicy.SurfaceDonorFor(donor) is null, "Native entrance must remain untouched.");
        }
        foreach (var name in new[] { "Foreign_Crypt2", "Crypt2", "Magenheim_Underworld_Dungeon_Unknown", "", "Magenheim_DeepFracture" })
            Check(UnderworldDungeonBoundaryPolicy.SurfaceDonorFor(name) is null, "Unowned identities must remain untouched.");
        Check(UnderworldDungeonBoundaryPolicy.EnvelopeDimension(64, 179.6, 12) == 203.6, "Enclosure must surround expanded room packing.");
        Check(UnderworldDungeonBoundaryPolicy.EnvelopeDimension(500, 179.6, 12) == 500, "Native vertical clearance must be preserved.");
        Check(UnderworldDungeonBoundaryPolicy.EnvelopeDimension(500, 718.4, 12) == 742.4, "Tall caves must also fit their enclosure.");
        foreach (var invalid in new[] { 0d, -1d, double.NaN, double.PositiveInfinity })
        {
            var rejected = false;
            try { UnderworldDungeonBoundaryPolicy.EnvelopeDimension(64, invalid, 12); }
            catch (ArgumentOutOfRangeException) { rejected = true; }
            Check(rejected, "Invalid packing size must be rejected.");
        }
        foreach (var cap in new[] { "amber-bed", "lantern-caps", "nursery-caps" })
            Check(UnderworldDungeonBoundaryPolicy.IsEdibleFungalCluster("model:underworld-flora-fungal-" + cap), "Edible cap cluster must be pickable.");
        foreach (var decoration in new[] { "fern", "moss-slab", "glowcap", "sporebrush" })
            Check(!UnderworldDungeonBoundaryPolicy.IsEdibleFungalCluster("model:underworld-flora-fungal-" + decoration), "Other vegetation must not become an edible pickup.");
        return count;
    }
}
