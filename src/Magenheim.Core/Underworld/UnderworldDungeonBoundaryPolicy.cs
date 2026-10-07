using System;
using System.Linq;

namespace Magenheim.Core.Underworld;

/// <summary>Identity and enclosure rules for private vanilla dungeon derivatives.</summary>
public static class UnderworldDungeonBoundaryPolicy
{
    public static string? SurfaceDonorFor(string locationName)
    {
        foreach (var profile in UnderworldVanillaDungeonReuseCatalog.All)
        {
            var definition = UnderworldDungeonCatalog.All.Single(x => x.Id == profile.DungeonId);
            if (string.Equals(locationName, definition.PrefabName, StringComparison.Ordinal))
                return profile.DonorEntrancePrefabs[0];
        }
        return null;
    }

    public static double EnvelopeDimension(double nativeSize, double packingSize, double tileWidth)
    {
        if (!(nativeSize > 0d) || !(packingSize > 0d) || !(tileWidth > 0d) ||
            double.IsInfinity(nativeSize + packingSize + tileWidth))
            throw new ArgumentOutOfRangeException(nameof(packingSize));
        return Math.Max(nativeSize, packingSize + tileWidth * 2d);
    }

    public static bool IsEdibleFungalCluster(string donor) => donor is
        "model:underworld-flora-fungal-amber-bed" or
        "model:underworld-flora-fungal-lantern-caps" or
        "model:underworld-flora-fungal-nursery-caps";
}
