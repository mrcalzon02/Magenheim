using System;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Sparse Sulfurous Wastes Deep Sigil source. Reuses the persistent Deep Sigil core and ordinary
/// biome-residency pipeline; only canonical biome identity and donor composition are Sulfur-specific.
/// </summary>
internal sealed class SulfurDeepSigilFamily : IUnderworldBiomeStructureFamily
{
    private const int FamilySalt = 0x5A17C43D;
    private const string CanonicalBiomeId = "magenheim.underworld.biome.sulfurous_wastes";

    public string Kind => "sulfurous-wastes-deep-sigil";
    public UnderworldTerrainBiome Biome => UnderworldTerrainBiome.SulfurousWastes;
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
        var root = new GameObject($"Magenheim_SulfurDeepSigil_{key.X}_{key.Z}");
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
            core.transform.localPosition = Vector3.up * 1.2f;
            core.transform.localRotation = Quaternion.Euler(5f, yaw, -3f);
            core.transform.localScale = Vector3.Scale(core.transform.localScale, new Vector3(0.86f, 2.25f, 0.5f));
            var interaction = core.GetComponent<UnderworldDeepSigilInteraction>()
                ?? throw new InvalidOperationException("Registered Deep Sigil core is missing its interaction authority.");
            interaction.Configure(CanonicalBiomeId);

            // Broken geothermal crown: replaceable donor visuals around the durable persistent Sigil core.
            for (var i = 0; i < 9; i++)
            {
                if (i == 3 || i == 7) continue;
                var angle = (yaw + i * 40f + (float)(random.NextDouble() * 8.0 - 4.0)) * Mathf.Deg2Rad;
                var radius = 2.9f + (float)random.NextDouble() * 0.8f;
                var marker = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 1) * 1618033), $"SulfurSigilMarker_{i}");
                marker.transform.SetParent(root.transform, false);
                marker.transform.localPosition += new Vector3(Mathf.Cos(angle) * radius, -0.12f + (i % 3) * 0.16f, Mathf.Sin(angle) * radius);
                marker.transform.localRotation *= Quaternion.Euler(18f + (i % 2) * 16f, -angle * Mathf.Rad2Deg + 90f, (i - 4) * 5f);
                marker.transform.localScale = Vector3.Scale(marker.transform.localScale, new Vector3(0.5f, 0.82f + i * 0.045f, 0.5f));
            }

            // Three larger vent/lavafall-facing markers echo the biome's established geothermal language.
            for (var i = 0; i < 3; i++)
            {
                var angle = (yaw + 18f + i * 120f) * Mathf.Deg2Rad;
                var vent = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 29) * 49979687), $"GeothermalMarker_{i}");
                vent.transform.SetParent(root.transform, false);
                vent.transform.localPosition += new Vector3(Mathf.Cos(angle) * 1.85f, -0.32f, Mathf.Sin(angle) * 1.85f);
                vent.transform.localRotation *= Quaternion.Euler(68f, yaw + i * 120f, 14f + i * 9f);
                vent.transform.localScale = Vector3.Scale(vent.transform.localScale, new Vector3(0.46f, 0.9f, 0.46f));
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
