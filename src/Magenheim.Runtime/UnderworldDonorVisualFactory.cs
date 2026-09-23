using System;
using System.Collections.Generic;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Reusable, non-destructive bridge from Valheim-owned runtime prefabs to native Underworld visuals.
/// Copies render/light/LOD data only; it never carries prefab identity, ZDO/network state, AI,
/// drops, wear, or other donor gameplay components into the derived Underworld instance.
/// </summary>
internal static class UnderworldDonorVisualFactory
{
    private static readonly Dictionary<string, GameObject> Sources = new(StringComparer.Ordinal);

    internal static GameObject Create(UnderworldTerrainBiome biome, int variant, string name)
    {
        Exception? last = null;
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var selected = variant + attempt;
            var donor = UnderworldVanillaDonorCatalog.Select(biome, selected);
            var model = UnderworldVanillaDonorCatalog.IsModel(donor);
            var source = model ? null : Resolve(donor.PrefabName);
            if (!model && source is null) continue;
            try
            {
                var root = model ? BuildModelVisual(donor) : BuildVisualClone(source!, donor);
                root.name = name;
                root.transform.localPosition = Vector3.up * donor.GroundOffset;
                root.transform.localRotation = Quaternion.Euler(
                    Mathf.Abs(selected % 7) - 3f,
                    Mathf.Abs(selected * 37 % 360),
                    Mathf.Abs(selected % 9) - 4f);
                var scale = UnderworldVanillaDonorCatalog.Scale(donor, selected);
                root.transform.localScale = scale;
                var lightScale = Mathf.Clamp(Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z)), 1f, 12f);
                foreach (var light in root.GetComponentsInChildren<Light>(true)) light.range *= lightScale;
                return root;
            }
            catch (Exception exception) { last = exception; }
        }
        throw new InvalidOperationException($"No usable Valheim donor prefab is available for Underworld biome {biome}.", last);
    }

    internal static GameObject? CreateCover(UnderworldTerrainBiome biome, int variant, string name,
        double heightAboveWater, double slopeDegrees)
    {
        Exception? last = null;
        var eligible = false;
        for (var attempt = 0; attempt < UnderworldVanillaDonorCatalog.CoverCount(biome); attempt++)
        {
            var selected = unchecked(variant + attempt);
            var cover = UnderworldVanillaDonorCatalog.SelectCover(biome, selected);
            if (!UnderworldFloraPlacement.CanPlaceCover(heightAboveWater, slopeDegrees,
                cover.MinHeightAboveWater, cover.MaxHeightAboveWater, cover.MaxSlope)) continue;
            eligible = true;
            var model = UnderworldVanillaDonorCatalog.IsModel(cover.Donor);
            var source = model ? null : Resolve(cover.Donor.PrefabName);
            if (!model && !source) continue;
            try
            {
                var root = model ? BuildModelVisual(cover.Donor) : BuildVisualClone(source!, cover.Donor);
                root.name = name + "_" + cover.Name;
                root.transform.localPosition = Vector3.up * cover.Donor.GroundOffset;
                root.transform.localRotation = Quaternion.Euler(0f, (selected & int.MaxValue) % 360, 0f);
                root.transform.localScale = UnderworldVanillaDonorCatalog.Scale(cover.Donor, selected);
                // An authored model carries its own colour; tinting it would muddy the albedo.
                if (!model) UnderworldCreaturePrototypeRegistrar.Tint(root, cover.Color);
                return root;
            }
            catch (InvalidOperationException exception) { last = exception; }
        }
        if (!eligible) return null; // Bare ground/deep water is a valid habitat result.
        throw new InvalidOperationException($"No usable habitat-compatible cover donor for {biome}.", last);
    }

    private static GameObject? Resolve(string prefabName)
    {
        if (Sources.TryGetValue(prefabName, out var cached) && cached) return cached;
        var source = PrefabManager.Instance?.GetPrefab(prefabName);
        if (source is not null) Sources[prefabName] = source;
        return source;
    }

    /// <summary>
    /// Builds an authored Magenheim model (<c>model:&lt;id&gt;</c> catalog entries) as the same kind of
    /// stripped visual a donor copy is: geometry, materials and the model's own authored colliders,
    /// no gameplay components. Meshes and materials are shared through ModelAssets' cache.
    /// </summary>
    private static GameObject BuildModelVisual(UnderworldVanillaDonorCatalog.Donor donor)
    {
        var id = UnderworldVanillaDonorCatalog.ModelId(donor);
        var root = new GameObject("Magenheim_UnderworldModelVisual_" + id);
        try
        {
            ModelAssets.Load(root, id, hideOriginal: false);
            if (donor.Collidable && !root.GetComponentInChildren<Collider>(true)) AddConservativeCollider(root);
            return root;
        }
        catch
        {
            UnityEngine.Object.Destroy(root);
            throw;
        }
    }

    private static GameObject BuildVisualClone(GameObject source, UnderworldVanillaDonorCatalog.Donor donor)
    {
        var root = new GameObject($"Magenheim_UnderworldDonorVisual_{source.name}");
        var transformMap = new Dictionary<Transform, Transform>();
        var rendererMap = new Dictionary<Renderer, Renderer>();
        var copiedRenderers = 0;
        try
        {
            CopyNode(source.transform, root.transform, false, transformMap, rendererMap, ref copiedRenderers);
            if (copiedRenderers == 0) throw new InvalidOperationException($"Underworld donor '{source.name}' has no supported mesh renderers.");
            RebuildLodGroups(source, transformMap, rendererMap);
            if (donor.Collidable) AddConservativeCollider(root);
            return root;
        }
        catch
        {
            UnityEngine.Object.Destroy(root);
            throw;
        }
    }

    private static void CopyNode(Transform source, Transform target, bool copyTransform, IDictionary<Transform, Transform> transformMap, IDictionary<Renderer, Renderer> rendererMap, ref int copiedRenderers)
    {
        transformMap[source] = target;
        if (copyTransform)
        {
            target.localPosition = source.localPosition;
            target.localRotation = source.localRotation;
            target.localScale = source.localScale;
            target.gameObject.SetActive(source.gameObject.activeSelf);
        }
        var sourceFilter = source.GetComponent<MeshFilter>();
        var sourceRenderer = source.GetComponent<MeshRenderer>();
        if (sourceFilter is not null && sourceFilter.sharedMesh is not null && sourceRenderer is not null)
        {
            var targetFilter = target.gameObject.AddComponent<MeshFilter>();
            targetFilter.sharedMesh = sourceFilter.sharedMesh;
            var targetRenderer = target.gameObject.AddComponent<MeshRenderer>();
            targetRenderer.sharedMaterials = sourceRenderer.sharedMaterials;
            targetRenderer.enabled = sourceRenderer.enabled;
            targetRenderer.shadowCastingMode = sourceRenderer.shadowCastingMode;
            targetRenderer.receiveShadows = sourceRenderer.receiveShadows;
            targetRenderer.lightProbeUsage = sourceRenderer.lightProbeUsage;
            targetRenderer.reflectionProbeUsage = sourceRenderer.reflectionProbeUsage;
            rendererMap[sourceRenderer] = targetRenderer;
            copiedRenderers++;
        }
        var sourceLight = source.GetComponent<Light>();
        if (sourceLight is not null && sourceLight.enabled)
        {
            var targetLight = target.gameObject.AddComponent<Light>();
            targetLight.type = sourceLight.type;
            targetLight.color = sourceLight.color;
            targetLight.intensity = sourceLight.intensity;
            targetLight.range = sourceLight.range;
            targetLight.spotAngle = sourceLight.spotAngle;
            targetLight.shadows = sourceLight.shadows;
            targetLight.cullingMask = sourceLight.cullingMask;
        }
        for (var i = 0; i < source.childCount; i++)
        {
            var child = source.GetChild(i);
            var targetChild = new GameObject(child.name).transform;
            targetChild.SetParent(target, false);
            CopyNode(child, targetChild, true, transformMap, rendererMap, ref copiedRenderers);
        }
    }

    private static void RebuildLodGroups(GameObject source, IReadOnlyDictionary<Transform, Transform> transformMap, IReadOnlyDictionary<Renderer, Renderer> rendererMap)
    {
        foreach (var sourceGroup in source.GetComponentsInChildren<LODGroup>(true))
        {
            if (!transformMap.TryGetValue(sourceGroup.transform, out var targetTransform)) continue;
            var sourceLods = sourceGroup.GetLODs();
            var targetLods = new LOD[sourceLods.Length];
            for (var i = 0; i < sourceLods.Length; i++)
            {
                var mapped = new List<Renderer>();
                foreach (var renderer in sourceLods[i].renderers)
                    if (renderer is not null && rendererMap.TryGetValue(renderer, out var targetRenderer)) mapped.Add(targetRenderer);
                targetLods[i] = new LOD(sourceLods[i].screenRelativeTransitionHeight, mapped.ToArray()) { fadeTransitionWidth = sourceLods[i].fadeTransitionWidth };
            }
            var group = targetTransform.gameObject.AddComponent<LODGroup>();
            group.localReferencePoint = sourceGroup.localReferencePoint;
            group.size = sourceGroup.size;
            group.fadeMode = sourceGroup.fadeMode;
            group.animateCrossFading = sourceGroup.animateCrossFading;
            group.SetLODs(targetLods);
            group.RecalculateBounds();
        }
    }

    private static void AddConservativeCollider(GameObject root)
    {
        foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh is null) continue;
            var collider = filter.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = filter.sharedMesh;
            return;
        }
    }
}