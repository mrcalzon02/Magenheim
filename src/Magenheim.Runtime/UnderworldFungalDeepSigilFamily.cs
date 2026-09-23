using System;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Sparse Fungal Forest Deep Sigil source. Reuses the registered persistent Deep Sigil core and
/// the normal biome residency pipeline; only the biome identity and donor composition are fungal.
/// </summary>
internal sealed class FungalDeepSigilFamily : IUnderworldBiomeStructureFamily
{
    private const int FamilySalt = 0x35F27A19;
    private const string CanonicalBiomeId = "magenheim.underworld.biome.fungal_forest";

    public string Kind => "fungal-deep-sigil";
    public UnderworldTerrainBiome Biome => UnderworldTerrainBiome.FungalForest;
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
            return (hash & 127) == 0;
        }
    }

    public GameObject Compose(Vector3 position, UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        var root = new GameObject($"Magenheim_FungalDeepSigil_{key.X}_{key.Z}");
        root.transform.position = position;
        var seed = identity.DerivedSeed32 ^ key.X * 19349663 ^ key.Z * 83492791 ^ FamilySalt;
        var random = new System.Random(seed);

        try
        {
            var yaw = (float)(random.NextDouble() * 360.0);
            var corePrefab = PrefabManager.Instance.GetPrefab(UnderworldDeepSigilCoreRegistrar.PrefabName)
                ?? throw new InvalidOperationException($"Persistent Deep Sigil core '{UnderworldDeepSigilCoreRegistrar.PrefabName}' is not registered.");
            var core = UnityEngine.Object.Instantiate(corePrefab, root.transform);
            core.name = "SigilCore";
            core.transform.localPosition = Vector3.up * 1.35f;
            core.transform.localRotation = Quaternion.Euler(3f, yaw, -5f);
            core.transform.localScale = Vector3.Scale(core.transform.localScale, new Vector3(0.82f, 2.35f, 0.48f));
            var interaction = core.GetComponent<UnderworldDeepSigilInteraction>()
                ?? throw new InvalidOperationException("Registered Deep Sigil core is missing its interaction authority.");
            interaction.Configure(CanonicalBiomeId);

            // Rooted fungal ring: donor visuals remain replaceable while residency and persistent core identity stay stable.
            for (var i = 0; i < 7; i++)
            {
                var angle = (yaw + i * (360f / 7f) + (float)(random.NextDouble() * 12.0 - 6.0)) * Mathf.Deg2Rad;
                var radius = 2.8f + (float)random.NextDouble() * 0.9f;
                var marker = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 1) * 104729), $"FungalSigilMarker_{i}");
                marker.transform.SetParent(root.transform, false);
                marker.transform.localPosition += new Vector3(Mathf.Cos(angle) * radius, 0.15f + (i % 3) * 0.12f, Mathf.Sin(angle) * radius);
                marker.transform.localRotation *= Quaternion.Euler(i % 2 == 0 ? 14f : -9f, -angle * Mathf.Rad2Deg + 90f, (i - 3) * 3f);
                marker.transform.localScale = Vector3.Scale(marker.transform.localScale, new Vector3(0.55f, 0.9f + i * 0.07f, 0.55f));
            }

            for (var i = 0; i < 4; i++)
            {
                var angle = (yaw + 22f + i * 91f) * Mathf.Deg2Rad;
                var rootFragment = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 23) * 49979687), $"RootFragment_{i}");
                rootFragment.transform.SetParent(root.transform, false);
                rootFragment.transform.localPosition += new Vector3(Mathf.Cos(angle) * (1.8f + i * 0.32f), -0.28f, Mathf.Sin(angle) * (1.8f + i * 0.32f));
                rootFragment.transform.localRotation *= Quaternion.Euler(72f - i * 6f, yaw + i * 37f, 11f + i * 8f);
                rootFragment.transform.localScale = Vector3.Scale(rootFragment.transform.localScale, new Vector3(0.48f, 0.8f, 0.48f));
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
