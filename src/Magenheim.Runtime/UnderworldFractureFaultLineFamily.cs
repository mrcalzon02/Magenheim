using System;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Durable Fracture Zones fault-line structure. Composition is presentation-only;
/// residency, persistence, lifecycle and networking remain owned by the shared
/// Underworld native-structure pipeline.
/// </summary>
internal sealed class FractureFaultLineFamily : IUnderworldBiomeStructureFamily
{
    private const int FamilySalt = 0x46AC7E21;
    public string Kind => "fracture-fault-line";
    public UnderworldTerrainBiome Biome => UnderworldTerrainBiome.FractureZones;
    public int PlacementSlot => 0;

    public bool Eligible(UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key) =>
        UnderworldStructureSpacingPolicy.IsLocalWinner(identity, key, FamilySalt, 1, candidate => BaseEligible(identity, candidate));

    private static bool BaseEligible(UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        unchecked
        {
            var hash = identity.DerivedSeed32 ^ FamilySalt;
            hash = (hash * 397) ^ key.X;
            hash = (hash * 397) ^ key.Z;
            hash ^= hash >> 16;
            return (hash & 7) <= 2;
        }
    }

    public GameObject Compose(Vector3 position, UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        var root = new GameObject($"Magenheim_FractureFaultLine_{key.X}_{key.Z}");
        root.transform.position = position;
        var seed = identity.DerivedSeed32 ^ key.X * 19349663 ^ key.Z * 83492791 ^ FamilySalt;
        var random = new System.Random(seed);

        try
        {
            var heading = (float)(random.NextDouble() * Mathf.PI * 2f);
            var forward = new Vector3(Mathf.Cos(heading), 0f, Mathf.Sin(heading));
            var normal = new Vector3(-forward.z, 0f, forward.x);

            // Nearly linear displaced strata: alternating lateral offsets keep the
            // fault readable while leaving a broken seam through its center.
            for (var i = 0; i < 6; i++)
            {
                var along = -4.2f + i * 1.68f + ((float)random.NextDouble() - 0.5f) * 0.38f;
                var side = ((i & 1) == 0 ? 1f : -1f) * (0.55f + (float)random.NextDouble() * 0.52f);
                var donor = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 1) * 67867967), $"FractureStrataDonor_{i}");
                donor.transform.SetParent(root.transform, false);
                donor.transform.localPosition += forward * along + normal * side + new Vector3(0f, -0.12f + (float)random.NextDouble() * 0.24f, 0f);
                donor.transform.localRotation *= Quaternion.Euler(
                    70f + (float)random.NextDouble() * 16f,
                    heading * Mathf.Rad2Deg + ((i & 1) == 0 ? 8f : -8f),
                    -12f + (float)random.NextDouble() * 24f);
                donor.transform.localScale *= 0.78f + (float)random.NextDouble() * 0.58f;
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
