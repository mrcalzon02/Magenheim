using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Sparse Fracture-zone Deep Sigil source placed through the normal biome structure
/// residency pipeline. The physical structure is intentionally separate from the
/// future interaction/discovery authority: that authority can bind to the named
/// SigilCore without making structure placement responsible for boss coordinates.
/// </summary>
internal sealed class FractureDeepSigilFamily : IUnderworldBiomeStructureFamily
{
    private const int FamilySalt = 0x6A31C5E9;
    public string Kind => "fracture-deep-sigil";
    public UnderworldTerrainBiome Biome => UnderworldTerrainBiome.FractureZones;
    public int PlacementSlot => 5;

    public bool Eligible(UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key) =>
        UnderworldStructureSpacingPolicy.IsLocalWinner(identity, key, FamilySalt, 4, candidate => BaseEligible(identity, candidate));

    private static bool BaseEligible(UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        unchecked
        {
            var hash = identity.DerivedSeed32 ^ FamilySalt;
            hash = (hash * 397) ^ key.X;
            hash = (hash * 397) ^ key.Z;
            hash ^= hash >> 16;
            // Sigils are exploration clues, not routine roadside decoration.
            return (hash & 127) == 0;
        }
    }

    public GameObject Compose(Vector3 position, UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        var root = new GameObject($"Magenheim_FractureDeepSigil_{key.X}_{key.Z}");
        root.transform.position = position;
        var seed = identity.DerivedSeed32 ^ key.X * 19349663 ^ key.Z * 83492791 ^ FamilySalt;
        var random = new System.Random(seed);

        try
        {
            var yaw = (float)(random.NextDouble() * 360.0);

            // Tall central tablet. Its stable child name is the seam for the later
            // server-authoritative boss-location discovery interaction.
            var core = UnderworldDonorVisualFactory.Create(Biome, seed ^ 0x31415926, "SigilCore");
            core.transform.SetParent(root.transform, false);
            core.transform.localPosition += Vector3.up * 1.7f;
            core.transform.localRotation *= Quaternion.Euler(-4f, yaw, 3f);
            core.transform.localScale = Vector3.Scale(core.transform.localScale, new Vector3(0.78f, 2.7f, 0.42f));

            // Six broken marker stones make the clue readable from multiple approach
            // angles while keeping the central tablet visually dominant.
            for (var i = 0; i < 6; i++)
            {
                var angle = (yaw + i * 60f + (float)(random.NextDouble() * 14.0 - 7.0)) * Mathf.Deg2Rad;
                var radius = 3.1f + (float)random.NextDouble() * 0.7f;
                var marker = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 1) * 104729), $"SigilMarker_{i}");
                marker.transform.SetParent(root.transform, false);
                marker.transform.localPosition += new Vector3(Mathf.Cos(angle) * radius, 0.35f - i * 0.05f, Mathf.Sin(angle) * radius);
                marker.transform.localRotation *= Quaternion.Euler((i % 2 == 0 ? 9f : -12f), -angle * Mathf.Rad2Deg + 90f, (i - 2) * 4f);
                marker.transform.localScale = Vector3.Scale(marker.transform.localScale, new Vector3(0.48f, 1.15f + i * 0.06f, 0.48f));
            }

            // Fallen fragments break the ritual-circle regularity and communicate age.
            for (var i = 0; i < 3; i++)
            {
                var angle = (yaw + 35f + i * 113f) * Mathf.Deg2Rad;
                var fragment = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 19) * 49979687), $"SigilFragment_{i}");
                fragment.transform.SetParent(root.transform, false);
                fragment.transform.localPosition += new Vector3(Mathf.Cos(angle) * (2.0f + i * 0.45f), -0.15f, Mathf.Sin(angle) * (2.0f + i * 0.45f));
                fragment.transform.localRotation *= Quaternion.Euler(68f - i * 7f, yaw + i * 31f, 18f + i * 9f);
                fragment.transform.localScale = Vector3.Scale(fragment.transform.localScale, new Vector3(0.42f, 0.72f, 0.42f));
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
