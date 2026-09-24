using System;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Sparse Great Decay Deep Sigil source. Reuses the persistent Deep Sigil core and ordinary
/// biome-residency pipeline; only canonical biome identity and donor composition are Decay-specific.
/// </summary>
internal sealed class GreatDecayDeepSigilFamily : IUnderworldBiomeStructureFamily
{
    private const int FamilySalt = 0x6A13D52B;
    private const string CanonicalBiomeId = "magenheim.underworld.biome.great_decay";

    public string Kind => "great-decay-deep-sigil";
    public UnderworldTerrainBiome Biome => UnderworldTerrainBiome.GreatDecay;
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
        var root = new GameObject($"Magenheim_GreatDecayDeepSigil_{key.X}_{key.Z}");
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
            core.transform.localPosition = Vector3.up * 1.15f;
            core.transform.localRotation = Quaternion.Euler(3f, yaw, -5f);
            core.transform.localScale = Vector3.Scale(core.transform.localScale, new Vector3(0.82f, 2.55f, 0.48f));
            var interaction = core.GetComponent<UnderworldDeepSigilInteraction>()
                ?? throw new InvalidOperationException("Registered Deep Sigil core is missing its interaction authority.");
            interaction.Configure(CanonicalBiomeId);

            // Asymmetric carrion spines preserve Great Decay's broken silhouette without owning gameplay state.
            for (var i = 0; i < 9; i++)
            {
                if (i == 2 || i == 7) continue;
                var angle = (yaw + i * 40f + (float)(random.NextDouble() * 12.0 - 6.0)) * Mathf.Deg2Rad;
                var radius = 2.7f + (float)random.NextDouble() * 0.9f;
                var spine = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 1) * 32452843), $"DecaySigilSpine_{i}");
                spine.transform.SetParent(root.transform, false);
                spine.transform.localPosition += new Vector3(Mathf.Cos(angle) * radius, -0.05f + (i % 3) * 0.16f, Mathf.Sin(angle) * radius);
                spine.transform.localRotation *= Quaternion.Euler(12f + (i % 4) * 8f, -angle * Mathf.Rad2Deg + 90f, (i - 4) * 6f);
                spine.transform.localScale = Vector3.Scale(spine.transform.localScale, new Vector3(0.46f, 0.9f + i * 0.055f, 0.5f));
            }

            // Three low gaps make the source readable through the biome's heavy miasma.
            for (var i = 0; i < 3; i++)
            {
                var angle = (yaw + 20f + i * 120f) * Mathf.Deg2Rad;
                var marker = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 19) * 49979687), $"DecayApproach_{i}");
                marker.transform.SetParent(root.transform, false);
                marker.transform.localPosition += new Vector3(Mathf.Cos(angle) * 1.8f, -0.38f, Mathf.Sin(angle) * 1.8f);
                marker.transform.localRotation *= Quaternion.Euler(72f, yaw + i * 120f, 10f + i * 7f);
                marker.transform.localScale = Vector3.Scale(marker.transform.localScale, new Vector3(0.42f, 0.72f, 0.42f));
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
