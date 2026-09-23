using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

internal sealed class FractureAnchorTowerFamily : IUnderworldBiomeStructureFamily
{
    private const int FamilySalt = 0x2E6B41D7;
    public string Kind => "fracture-anchor-tower";
    public UnderworldTerrainBiome Biome => UnderworldTerrainBiome.FractureZones;
    public int PlacementSlot => 3;

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
            return (hash & 63) == 0;
        }
    }

    public GameObject Compose(Vector3 position, UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        var root = new GameObject($"Magenheim_FractureAnchorTower_{key.X}_{key.Z}");
        root.transform.position = position;
        var seed = identity.DerivedSeed32 ^ key.X * 19349663 ^ key.Z * 83492791 ^ FamilySalt;
        var random = new System.Random(seed);
        try
        {
            var heading = (float)(random.NextDouble() * Mathf.PI * 2f);
            var forward = new Vector3(Mathf.Cos(heading), 0f, Mathf.Sin(heading));
            var lateral = new Vector3(-forward.z, 0f, forward.x);
            var yaw = heading * Mathf.Rad2Deg;

            for (var level = 0; level < 5; level++)
            {
                var donor = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((level + 1) * 104729), $"AnchorTowerCore_{level}");
                donor.transform.SetParent(root.transform, false);
                donor.transform.localPosition += Vector3.up * (level * 2.15f) + forward * (level * 0.16f);
                donor.transform.localRotation *= Quaternion.Euler(0f, yaw + level * 3f, level * 0.8f);
                donor.transform.localScale = Vector3.Scale(donor.transform.localScale, new Vector3(1.25f - level * 0.08f, 1.2f, 1.25f - level * 0.08f));
            }

            for (var side = -1; side <= 1; side += 2)
            {
                var donor = UnderworldDonorVisualFactory.Create(Biome, seed ^ (side < 0 ? 0x13579BDF : 0x02468ACE), $"AnchorTowerButtress_{side}");
                donor.transform.SetParent(root.transform, false);
                donor.transform.localPosition += lateral * (side * 2.25f) + forward * 0.4f + Vector3.up * 1.2f;
                donor.transform.localRotation *= Quaternion.Euler(side * 8f, yaw, side * 12f);
                donor.transform.localScale = Vector3.Scale(donor.transform.localScale, new Vector3(0.7f, 1.65f, 0.8f));
            }

            var crown = UnderworldDonorVisualFactory.Create(Biome, seed ^ 0x31415926, "AnchorTowerCrown");
            crown.transform.SetParent(root.transform, false);
            crown.transform.localPosition += Vector3.up * 10.8f + forward * 0.7f;
            crown.transform.localRotation *= Quaternion.Euler(0f, yaw + 45f, 0f);
            crown.transform.localScale = Vector3.Scale(crown.transform.localScale, new Vector3(1.65f, 0.45f, 1.65f));
            return root;
        }
        catch
        {
            UnityEngine.Object.Destroy(root);
            throw;
        }
    }
}
