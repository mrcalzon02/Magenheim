using System;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Sparse Fracture Zones road station assembled from the existing donor vocabulary.
/// The station reads as an old route-service platform projected over a chasm, with
/// an anchored rear mass and hanging lower fragments. Residency, persistence and
/// networking remain owned by the shared native Underworld structure pipeline.
/// </summary>
internal sealed class FractureSuspendedRoadStationFamily : IUnderworldBiomeStructureFamily
{
    private const int FamilySalt = 0x57C41D2B;
    public string Kind => "fracture-suspended-road-station";
    public UnderworldTerrainBiome Biome => UnderworldTerrainBiome.FractureZones;
    public int PlacementSlot => 2;

    public bool Eligible(UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key) =>
        UnderworldStructureSpacingPolicy.IsLocalWinner(identity, key, FamilySalt, 2, candidate => BaseEligible(identity, candidate));

    private static bool BaseEligible(UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        unchecked
        {
            var hash = identity.DerivedSeed32 ^ FamilySalt;
            hash = (hash * 397) ^ key.X;
            hash = (hash * 397) ^ key.Z;
            hash ^= hash >> 16;
            // Road stations are uncommon infrastructure nodes, not ambient clutter.
            return (hash & 31) == 0;
        }
    }

    public GameObject Compose(Vector3 position, UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        var root = new GameObject($"Magenheim_FractureSuspendedRoadStation_{key.X}_{key.Z}");
        root.transform.position = position;
        var seed = identity.DerivedSeed32 ^ key.X * 19349663 ^ key.Z * 83492791 ^ FamilySalt;
        var random = new System.Random(seed);

        try
        {
            var heading = (float)(random.NextDouble() * Mathf.PI * 2f);
            var outward = new Vector3(Mathf.Cos(heading), 0f, Mathf.Sin(heading));
            var lateral = new Vector3(-outward.z, 0f, outward.x);

            // Heavy rear anchors imply attachment to a cliff or surviving road cut.
            for (var i = 0; i < 3; i++)
            {
                var donor = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 1) * 486187739), $"RoadStationAnchor_{i}");
                donor.transform.SetParent(root.transform, false);
                donor.transform.localPosition += -outward * (2.8f - i * 0.55f)
                    + lateral * ((i - 1) * 1.65f)
                    + Vector3.up * (0.45f + i * 0.22f);
                donor.transform.localRotation *= Quaternion.Euler(0f, heading * Mathf.Rad2Deg + (i - 1) * 6f, 0f);
                donor.transform.localScale = Vector3.Scale(donor.transform.localScale, new Vector3(1.05f, 1.35f, 0.9f));
            }

            // A broad service deck projects into open space and gives the structure
            // the readable silhouette of a route stop rather than another ruin pile.
            for (var i = 0; i < 4; i++)
            {
                var donor = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 5) * 961748927), $"RoadStationDeck_{i}");
                donor.transform.SetParent(root.transform, false);
                donor.transform.localPosition += outward * (0.25f + i * 1.45f)
                    + lateral * (((float)random.NextDouble() - 0.5f) * 0.35f)
                    + Vector3.up * (1.05f - i * 0.08f);
                donor.transform.localRotation *= Quaternion.Euler(i * 1.5f, heading * Mathf.Rad2Deg, (i - 1.5f) * 1.4f);
                donor.transform.localScale = Vector3.Scale(donor.transform.localScale, new Vector3(1.35f, 0.34f, 0.78f));
            }

            // Two lateral marker/gantry masses make the station legible from the
            // route and frame the exposed loading platform.
            for (var side = -1; side <= 1; side += 2)
            {
                var donor = UnderworldDonorVisualFactory.Create(Biome, seed ^ (side < 0 ? 0x13579BDF : 0x2468ACE), $"RoadStationMarker_{side}");
                donor.transform.SetParent(root.transform, false);
                donor.transform.localPosition += outward * 1.3f + lateral * (side * 2.15f) + Vector3.up * 2.8f;
                donor.transform.localRotation *= Quaternion.Euler(0f, heading * Mathf.Rad2Deg + side * 4f, side * 5f);
                donor.transform.localScale = Vector3.Scale(donor.transform.localScale, new Vector3(0.48f, 1.65f, 0.48f));
            }

            // Hanging fragments below the deck sell the suspension/chasm language
            // without inventing a separate physics or placement authority.
            for (var i = 0; i < 3; i++)
            {
                var donor = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 11) * 32452843), $"RoadStationHanging_{i}");
                donor.transform.SetParent(root.transform, false);
                donor.transform.localPosition += outward * (1.1f + i * 1.25f)
                    + lateral * ((i - 1) * 0.8f)
                    + Vector3.down * (1.2f + i * 0.7f);
                donor.transform.localRotation *= Quaternion.Euler(8f + i * 6f, heading * Mathf.Rad2Deg + i * 7f, -7f + i * 5f);
                donor.transform.localScale = Vector3.Scale(donor.transform.localScale, new Vector3(0.52f, 0.8f, 0.52f));
            }

            return root;
        }
        catch
        {
            UnityEngine.Object.Destroy(root);
            throw;
        }
    }
}
