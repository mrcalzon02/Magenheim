using System;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Sparse Blackwater Deep Sigil source. Reuses the persistent Deep Sigil core and ordinary
/// biome-residency pipeline; only canonical biome identity and donor composition are Blackwater-specific.
/// </summary>
internal sealed class BlackwaterDeepSigilFamily : IUnderworldBiomeStructureFamily
{
    private const int FamilySalt = 0x42B71D63;
    private const string CanonicalBiomeId = "magenheim.underworld.biome.blackwater_deep";

    public string Kind => "blackwater-deep-sigil";
    public UnderworldTerrainBiome Biome => UnderworldTerrainBiome.BlackwaterDeep;
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
        var root = new GameObject($"Magenheim_BlackwaterDeepSigil_{key.X}_{key.Z}");
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
            core.transform.localPosition = Vector3.up * 1.1f;
            core.transform.localRotation = Quaternion.Euler(-6f, yaw, 4f);
            core.transform.localScale = Vector3.Scale(core.transform.localScale, new Vector3(0.9f, 2.15f, 0.52f));
            var interaction = core.GetComponent<UnderworldDeepSigilInteraction>()
                ?? throw new InvalidOperationException("Registered Deep Sigil core is missing its interaction authority.");
            interaction.Configure(CanonicalBiomeId);

            // Drowned broken ring: donor visuals are replaceable; source identity and persistent core are not.
            for (var i = 0; i < 8; i++)
            {
                if (i == 2 || i == 6) continue;
                var angle = (yaw + i * 45f + (float)(random.NextDouble() * 10.0 - 5.0)) * Mathf.Deg2Rad;
                var radius = 3.1f + (float)random.NextDouble() * 0.75f;
                var marker = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 1) * 130363), $"BlackwaterSigilMarker_{i}");
                marker.transform.SetParent(root.transform, false);
                marker.transform.localPosition += new Vector3(Mathf.Cos(angle) * radius, -0.25f + (i % 3) * 0.12f, Mathf.Sin(angle) * radius);
                marker.transform.localRotation *= Quaternion.Euler(76f + (i % 2) * 8f, -angle * Mathf.Rad2Deg + 90f, (i - 4) * 4f);
                marker.transform.localScale = Vector3.Scale(marker.transform.localScale, new Vector3(0.52f, 0.72f + i * 0.05f, 0.52f));
            }

            for (var i = 0; i < 3; i++)
            {
                var angle = (yaw + 30f + i * 121f) * Mathf.Deg2Rad;
                var fragment = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 19) * 49979687), $"DrownedFragment_{i}");
                fragment.transform.SetParent(root.transform, false);
                fragment.transform.localPosition += new Vector3(Mathf.Cos(angle) * (1.7f + i * 0.4f), -0.48f, Mathf.Sin(angle) * (1.7f + i * 0.4f));
                fragment.transform.localRotation *= Quaternion.Euler(84f, yaw + i * 43f, 18f + i * 7f);
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
