using System;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Durable Blackwater Deep sparse-pocket structure. Composition is presentation-only;
/// residency, persistence, lifecycle and networking remain owned by the shared
/// Underworld native-structure pipeline.
/// </summary>
internal sealed class BlackwaterSparsePocketFamily : IUnderworldBiomeStructureFamily
{
    public string Kind => "blackwater-sparse-pocket";
    public UnderworldTerrainBiome Biome => UnderworldTerrainBiome.BlackwaterDeep;
    public int PlacementSlot => 0;

    public bool Eligible(UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        unchecked
        {
            var hash = identity.DerivedSeed32 ^ 0x2B1AC7E5;
            hash = (hash * 397) ^ key.X;
            hash = (hash * 397) ^ key.Z;
            hash ^= hash >> 16;
            return (hash & 7) <= 1;
        }
    }

    public GameObject Compose(Vector3 position, UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        var root = new GameObject($"Magenheim_BlackwaterSparsePocket_{key.X}_{key.Z}");
        root.transform.position = position;
        var seed = identity.DerivedSeed32 ^ key.X * 73856093 ^ key.Z * 19349663 ^ 0x2B1AC7E5;
        var random = new System.Random(seed);

        try
        {
            var heading = (float)(random.NextDouble() * Mathf.PI * 2f);
            var forward = new Vector3(Mathf.Cos(heading), 0f, Mathf.Sin(heading));
            var bank = new Vector3(-forward.z, 0f, forward.x);

            // Keep Blackwater deliberately open. Four supports trace one bank/depression
            // edge; a fifth low formation hugs the same edge instead of closing a ring.
            for (var i = 0; i < 5; i++)
            {
                var along = i < 4
                    ? -3.0f + i * 1.85f + ((float)random.NextDouble() - 0.5f) * 0.45f
                    : 1.25f + ((float)random.NextDouble() - 0.5f) * 0.55f;
                var lateral = i < 4
                    ? 2.25f + (float)random.NextDouble() * 1.35f
                    : 1.65f + (float)random.NextDouble() * 0.75f;
                var donor = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 1) * 86028121), $"BlackwaterPocketDonor_{i}");
                donor.transform.SetParent(root.transform, false);
                donor.transform.localPosition += forward * along + bank * lateral + new Vector3(0f, -0.18f + (float)random.NextDouble() * 0.20f, 0f);
                donor.transform.localRotation *= Quaternion.Euler(
                    68f + (float)random.NextDouble() * 18f,
                    heading * Mathf.Rad2Deg + 90f + ((float)random.NextDouble() - 0.5f) * 24f,
                    -10f + (float)random.NextDouble() * 20f);
                donor.transform.localScale *= (i == 4 ? 0.62f : 0.78f) + (float)random.NextDouble() * 0.46f;
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
