using System;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Sparse Fracture-zone Deep Sigil source placed through the normal biome structure residency
/// pipeline. Its central core is the registered persistent Valheim network prefab; admission binds
/// that core to the structure's deterministic native-instance identity.
/// </summary>
internal sealed class FractureDeepSigilFamily : IUnderworldBiomeStructureFamily
{
    private const int FamilySalt = 0x6A31C5E9;
    private const string CanonicalBiomeId = "magenheim.underworld.biome.fracture";
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
            var corePrefab = PrefabManager.Instance.GetPrefab(UnderworldDeepSigilCoreRegistrar.PrefabName)
                ?? throw new InvalidOperationException($"Persistent Deep Sigil core '{UnderworldDeepSigilCoreRegistrar.PrefabName}' is not registered.");
            var core = UnityEngine.Object.Instantiate(corePrefab, root.transform);
            core.name = "SigilCore";
            core.transform.localPosition = Vector3.up * 1.7f;
            core.transform.localRotation = Quaternion.Euler(-4f, yaw, 3f);
            core.transform.localScale = Vector3.Scale(core.transform.localScale, new Vector3(0.78f, 2.7f, 0.42f));
            var interaction = core.GetComponent<UnderworldDeepSigilInteraction>()
                ?? throw new InvalidOperationException("Registered Deep Sigil core is missing its interaction authority.");
            interaction.Configure(CanonicalBiomeId);

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
