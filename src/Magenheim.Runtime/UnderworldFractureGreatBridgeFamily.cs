using System;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Sparse repeatable broken great bridges for the Fracture Zones. This is ordinary
/// biome infrastructure, distinct from the unique Broken Ancient Bridge landmark.
/// Residency, persistence and networking remain owned by the shared native
/// Underworld structure pipeline.
/// </summary>
internal sealed class FractureGreatBridgeFamily : IUnderworldBiomeStructureFamily
{
    private const int FamilySalt = 0x4B61D6E3;
    public string Kind => "fracture-great-bridge";
    public UnderworldTerrainBiome Biome => UnderworldTerrainBiome.FractureZones;
    public int PlacementSlot => 4;

    public bool Eligible(UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key) =>
        UnderworldStructureSpacingPolicy.IsLocalWinner(identity, key, FamilySalt, 3, candidate => BaseEligible(identity, candidate));

    private static bool BaseEligible(UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        unchecked
        {
            var hash = identity.DerivedSeed32 ^ FamilySalt;
            hash = (hash * 397) ^ key.X;
            hash = (hash * 397) ^ key.Z;
            hash ^= hash >> 16;
            // Great bridges are rare route-scale infrastructure, not ambient ruins.
            return (hash & 63) == 0;
        }
    }

    public GameObject Compose(Vector3 position, UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        var root = new GameObject($"Magenheim_FractureGreatBridge_{key.X}_{key.Z}");
        root.transform.position = position;
        var seed = identity.DerivedSeed32 ^ key.X * 19349663 ^ key.Z * 83492791 ^ FamilySalt;
        var random = new System.Random(seed);

        try
        {
            var heading = (float)(random.NextDouble() * Mathf.PI * 2f);
            var forward = new Vector3(Mathf.Cos(heading), 0f, Mathf.Sin(heading));
            var lateral = new Vector3(-forward.z, 0f, forward.x);
            var headingDegrees = heading * Mathf.Rad2Deg;

            // Two surviving approaches frame a deliberately missing central span.
            for (var side = -1; side <= 1; side += 2)
            {
                for (var i = 0; i < 4; i++)
                {
                    var donor = UnderworldDonorVisualFactory.Create(Biome, seed ^ (side * 486187739) ^ ((i + 1) * 32452843), $"GreatBridgeDeck_{side}_{i}");
                    donor.transform.SetParent(root.transform, false);
                    var distance = 3.6f + i * 1.65f;
                    donor.transform.localPosition += forward * (side * distance)
                        + lateral * (((float)random.NextDouble() - 0.5f) * 0.28f)
                        + Vector3.down * (i * i * 0.09f);
                    donor.transform.localRotation *= Quaternion.Euler(side * i * 1.8f, headingDegrees, side * i * 2.2f);
                    donor.transform.localScale = Vector3.Scale(donor.transform.localScale, new Vector3(1.55f, 0.38f, 0.95f));
                }
            }

            // Massive paired piers establish bridge scale; the inner pair lean toward
            // the failed span to make the break read as structural collapse.
            for (var side = -1; side <= 1; side += 2)
            {
                for (var row = 0; row < 2; row++)
                {
                    var donor = UnderworldDonorVisualFactory.Create(Biome, seed ^ (side < 0 ? 0x13579BDF : 0x2468ACE) ^ (row * 961748927), $"GreatBridgePier_{side}_{row}");
                    donor.transform.SetParent(root.transform, false);
                    donor.transform.localPosition += forward * (side * (3.1f + row * 4.7f)) + Vector3.down * (2.2f + row * 0.5f);
                    donor.transform.localRotation *= Quaternion.Euler(0f, headingDegrees, side * (row == 0 ? 7f : 2f));
                    donor.transform.localScale = Vector3.Scale(donor.transform.localScale, new Vector3(1.15f, 2.65f, 1.15f));
                }
            }

            // Fallen central fragments below the gap communicate where the missing
            // roadway went without reconnecting the traversable silhouette.
            for (var i = 0; i < 4; i++)
            {
                var donor = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 17) * 49979687), $"GreatBridgeFallen_{i}");
                donor.transform.SetParent(root.transform, false);
                donor.transform.localPosition += forward * ((i - 1.5f) * 0.85f)
                    + lateral * (((float)random.NextDouble() - 0.5f) * 2.1f)
                    + Vector3.down * (3.2f + i * 0.75f);
                donor.transform.localRotation *= Quaternion.Euler(18f + i * 11f, headingDegrees + i * 13f, -16f + i * 9f);
                donor.transform.localScale = Vector3.Scale(donor.transform.localScale, new Vector3(0.72f, 0.62f, 0.72f));
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
