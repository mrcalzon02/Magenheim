using System;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Uses native pickup/felling prefabs for edible clusters and larger fungal growths.</summary>
internal static class UnderworldDungeonHarvestableDressing
{
    internal static GameObject? Cover(UnderworldTerrainBiome biome, int seed, Transform parent)
    {
        if (biome != UnderworldTerrainBiome.FungalForest) return null;
        var cover = UnderworldVanillaDonorCatalog.SelectCover(biome, seed);
        if (!UnderworldDungeonBoundaryPolicy.IsEdibleFungalCluster(cover.Donor.PrefabName)) return null;
        var model = UnderworldVanillaDonorCatalog.ModelId(cover.Donor);
        var name = "Magenheim_Underworld_DungeonPickup_" + model;
        var prefab = PrefabManager.Instance.GetPrefab(name);
        if (!prefab)
        {
            var source = PrefabManager.Instance.GetPrefab("Magenheim_Underworld_ResourcePickup_GlowcapFlesh")
                ?? throw new InvalidOperationException("Native Glowcap Flesh pickup is unavailable.");
            prefab = PrefabManager.Instance.CreateClonedPrefab(name, source);
            var pickable = prefab.GetComponent<Pickable>();
            pickable.m_hideWhenPicked = UnderworldResourceVisuals.Apply(prefab, model);
            pickable.m_overrideName = cover.Name;
            PrefabManager.Instance.AddPrefab(prefab);
        }
        var result = UnityEngine.Object.Instantiate(prefab, parent);
        // Keep the registered identity: renaming a nested ZNetView changes its prefab hash.
        result.name = name;
        result.transform.localPosition = Vector3.up * cover.Donor.GroundOffset;
        result.transform.localRotation = Quaternion.Euler(0f, (seed & int.MaxValue) % 360, 0f);
        result.transform.localScale = UnderworldVanillaDonorCatalog.Scale(cover.Donor, seed);
        return result;
    }

    internal static GameObject? Major(UnderworldTerrainBiome biome, int seed, Transform parent)
    {
        if (biome != UnderworldTerrainBiome.FungalForest) return null;
        var donor = UnderworldVanillaDonorCatalog.Select(biome, seed);
        var model = UnderworldVanillaDonorCatalog.ModelId(donor);
        var species = model.Contains("spirestalk") ? "Spirestalk" : "Glowcap";
        var name = "Magenheim_Underworld_DungeonFlora_" + model;
        var prefab = PrefabManager.Instance.GetPrefab(name);
        if (!prefab)
        {
            var source = PrefabManager.Instance.GetPrefab("Magenheim_Underworld_" + species)
                ?? throw new InvalidOperationException("Native fungal harvest prefab is unavailable: " + species);
            prefab = PrefabManager.Instance.CreateClonedPrefab(name, source);
            prefab.GetComponent<TreeBase>().m_trunk = UnderworldFloraHarvest.ApplyVisual(prefab, model, moving: false);
            PrefabManager.Instance.AddPrefab(prefab);
        }
        var result = UnityEngine.Object.Instantiate(prefab, parent);
        result.name = name;
        result.transform.localPosition = Vector3.up * donor.GroundOffset;
        result.transform.localRotation = Quaternion.Euler(0f, Math.Abs(seed % 360), 0f);
        result.transform.localScale = UnderworldVanillaDonorCatalog.Scale(donor, seed);
        return result;
    }
}
