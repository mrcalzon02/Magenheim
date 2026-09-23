using System;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Fracture Zones cliff monastery/temple silhouette assembled from the existing
/// biome donor vocabulary. Residency, persistence, lifecycle and networking stay
/// owned by the shared native Underworld structure pipeline.
/// </summary>
internal sealed class FractureCliffMonasteryFamily : IUnderworldBiomeStructureFamily
{
    private const int FamilySalt = 0x3D7A2C19;
    public string Kind => "fracture-cliff-monastery";
    public UnderworldTerrainBiome Biome => UnderworldTerrainBiome.FractureZones;
    public int PlacementSlot => 1;

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
            // Deliberately sparse: these should read as destinations, not clutter.
            return (hash & 31) == 0;
        }
    }

    public GameObject Compose(Vector3 position, UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        var root = new GameObject($"Magenheim_FractureCliffMonastery_{key.X}_{key.Z}");
        root.transform.position = position;
        var seed = identity.DerivedSeed32 ^ key.X * 19349663 ^ key.Z * 83492791 ^ FamilySalt;
        var random = new System.Random(seed);

        try
        {
            var heading = (float)(random.NextDouble() * Mathf.PI * 2f);
            var outward = new Vector3(Mathf.Cos(heading), 0f, Mathf.Sin(heading));
            var lateral = new Vector3(-outward.z, 0f, outward.x);

            // A stepped complex clinging to an implied cliff face. The rear masses
            // rise sharply; the front terraces project into open space.
            for (var tier = 0; tier < 4; tier++)
            {
                var donor = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((tier + 1) * 486187739), $"MonasteryTier_{tier}");
                donor.transform.SetParent(root.transform, false);
                donor.transform.localPosition += -outward * (2.8f - tier * 1.35f)
                    + lateral * (((float)random.NextDouble() - 0.5f) * 1.15f)
                    + Vector3.up * (tier * 1.55f);
                donor.transform.localRotation *= Quaternion.Euler(0f, heading * Mathf.Rad2Deg + ((float)random.NextDouble() - 0.5f) * 12f, 0f);
                donor.transform.localScale *= new Vector3(1.25f - tier * 0.08f, 0.72f + tier * 0.18f, 0.9f);
            }

            // Two projecting shrine/terrace masses establish the dangerous exposed
            // approach and keep the silhouette distinct from ordinary fault debris.
            for (var i = 0; i < 2; i++)
            {
                var donor = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 7) * 961748927), $"MonasteryTerrace_{i}");
                donor.transform.SetParent(root.transform, false);
                donor.transform.localPosition += outward * (1.1f + i * 1.75f)
                    + lateral * (i == 0 ? -1.4f : 1.15f)
                    + Vector3.up * (0.65f + i * 0.5f);
                donor.transform.localRotation *= Quaternion.Euler(4f + i * 5f, heading * Mathf.Rad2Deg, i == 0 ? -4f : 5f);
                donor.transform.localScale *= new Vector3(1.15f, 0.48f, 0.72f);
            }

            // A tall rear sanctum/spire makes the site legible from a distance.
            var sanctum = UnderworldDonorVisualFactory.Create(Biome, seed ^ 0x13579BDF, "MonasterySanctum");
            sanctum.transform.SetParent(root.transform, false);
            sanctum.transform.localPosition += -outward * 1.15f + lateral * 0.25f + Vector3.up * 5.1f;
            sanctum.transform.localRotation *= Quaternion.Euler(0f, heading * Mathf.Rad2Deg + 7f, -3f);
            sanctum.transform.localScale *= new Vector3(0.72f, 1.85f, 0.72f);

            return root;
        }
        catch
        {
            UnityEngine.Object.Destroy(root);
            throw;
        }
    }
}
