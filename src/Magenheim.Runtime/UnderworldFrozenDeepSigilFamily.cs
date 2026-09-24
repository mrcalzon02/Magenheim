using System;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Sparse Frozen Caverns Deep Sigil source. Reuses the persistent Deep Sigil core and ordinary
/// biome-residency pipeline; only canonical biome identity and donor composition are Frozen-specific.
/// </summary>
internal sealed class FrozenDeepSigilFamily : IUnderworldBiomeStructureFamily
{
    private const int FamilySalt = 0x4F2A71C3;
    private const string CanonicalBiomeId = "magenheim.underworld.biome.frozen_caverns";

    public string Kind => "frozen-caverns-deep-sigil";
    public UnderworldTerrainBiome Biome => UnderworldTerrainBiome.FrozenCaverns;
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
        var root = new GameObject($"Magenheim_FrozenDeepSigil_{key.X}_{key.Z}");
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
            core.transform.localPosition = Vector3.up * 1.25f;
            core.transform.localRotation = Quaternion.Euler(-4f, yaw, 2f);
            core.transform.localScale = Vector3.Scale(core.transform.localScale, new Vector3(0.78f, 2.4f, 0.46f));
            var interaction = core.GetComponent<UnderworldDeepSigilInteraction>()
                ?? throw new InvalidOperationException("Registered Deep Sigil core is missing its interaction authority.");
            interaction.Configure(CanonicalBiomeId);

            // Broken ice-vault ribs: replaceable donor visuals around the durable persistent Sigil core.
            for (var i = 0; i < 8; i++)
            {
                if (i == 2 || i == 6) continue;
                var angle = (yaw + i * 45f + (float)(random.NextDouble() * 6.0 - 3.0)) * Mathf.Deg2Rad;
                var radius = 3.0f + (float)random.NextDouble() * 0.65f;
                var rib = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 1) * 32452843), $"FrozenSigilRib_{i}");
                rib.transform.SetParent(root.transform, false);
                rib.transform.localPosition += new Vector3(Mathf.Cos(angle) * radius, 0.08f + (i % 2) * 0.14f, Mathf.Sin(angle) * radius);
                rib.transform.localRotation *= Quaternion.Euler(10f + (i % 3) * 7f, -angle * Mathf.Rad2Deg + 90f, (i - 4) * 4f);
                rib.transform.localScale = Vector3.Scale(rib.transform.localScale, new Vector3(0.48f, 1.05f + i * 0.045f, 0.48f));
            }

            // Four low cardinal threshold fragments preserve readable approaches to the Sigil.
            for (var i = 0; i < 4; i++)
            {
                var angle = (yaw + 22.5f + i * 90f) * Mathf.Deg2Rad;
                var threshold = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 17) * 49979687), $"FrozenThreshold_{i}");
                threshold.transform.SetParent(root.transform, false);
                threshold.transform.localPosition += new Vector3(Mathf.Cos(angle) * 1.9f, -0.3f, Mathf.Sin(angle) * 1.9f);
                threshold.transform.localRotation *= Quaternion.Euler(74f, yaw + i * 90f, 8f + i * 5f);
                threshold.transform.localScale = Vector3.Scale(threshold.transform.localScale, new Vector3(0.44f, 0.78f, 0.44f));
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
