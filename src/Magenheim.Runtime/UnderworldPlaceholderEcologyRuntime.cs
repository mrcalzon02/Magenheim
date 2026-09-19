using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Disposable local Underworld ecology preview assembled from Valheim-owned donor prefabs.
/// Donors are resolved from the running game and rebuilt as stripped render-only copies: no
/// vanilla prefab identity is replaced, no vanilla meshes/textures are packaged by Magenheim,
/// and no donor gameplay/network components are carried into the preview objects.
/// </summary>
internal sealed class UnderworldPlaceholderEcologyRuntime : MonoBehaviour
{
    private UnderworldRuntimeServices? _services;
    private ManualLogSource? _log;
    private readonly List<GameObject> _spawned = new();
    private readonly Dictionary<string, GameObject> _donorSources = new(StringComparer.Ordinal);
    private readonly HashSet<string> _warnedDonors = new(StringComparer.Ordinal);
    private string _admitted = string.Empty;
    private Vector3 _lastBuildPosition;
    private float _nextAt;
    private const float RebuildDistance = 180f;

    internal void Configure(UnderworldRuntimeServices services, ManualLogSource log)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    private void Update()
    {
        if (Time.unscaledTime < _nextAt) return;
        _nextAt = Time.unscaledTime + 3f;
        if (_services is null || ZNet.instance is null || ZNet.World is null) return;

        // Host coordinates describe where Valheim may simulate the layer; they do not make an
        // Underworld instance exist. Ecology is a gameplay consumer and therefore appears only
        // after the shared lifecycle has completed admission. Releasing/faulted/inactive states
        // clear disposable visuals immediately instead of leaving a second coordinate-based
        // definition of instance availability behind.
        var lifecycle = _services.InstanceLifecycle;
        if (lifecycle.Phase != UnderworldInstancePhase.Active || lifecycle.Identity is null)
        {
            ClearMarkers();
            _admitted = string.Empty;
            return;
        }

        if (!UnderworldRuntimeIdentityResolver.TryResolveWorldSession(
                _services.SpatialDomain, ZNet.instance, ZNet.World,
                out var identity, out var layer, out _) ||
            identity is null || layer != UnderworldLayer.Underworld ||
            !string.Equals(identity.DerivedWorldId, lifecycle.Identity.DerivedWorldId, StringComparison.Ordinal) ||
            !string.Equals(identity.ParentWorldId, lifecycle.Identity.ParentWorldId, StringComparison.Ordinal) ||
            !string.Equals(identity.DerivedSeedFingerprint, lifecycle.Identity.DerivedSeedFingerprint, StringComparison.Ordinal))
        {
            ClearMarkers();
            _admitted = string.Empty;
            return;
        }

        var player = Player.m_localPlayer;
        if (player is null) return;

        var first = !string.Equals(_admitted, identity.DerivedWorldId, StringComparison.Ordinal);
        if (!first && Vector3.Distance(player.transform.position, _lastBuildPosition) < RebuildDistance) return;

        _admitted = identity.DerivedWorldId;
        _lastBuildPosition = player.transform.position;
        BuildLocalPatch(player.transform.position, identity);
    }

    private void BuildLocalPatch(Vector3 center, UnderworldWorldIdentity identity)
    {
        ClearMarkers();
        var generator = WorldGenerator.instance;
        if (generator is null) return;

        var seed = identity.DerivedSeed32 ^
            (Mathf.RoundToInt(center.x / 90f) * 73856093) ^
            (Mathf.RoundToInt(center.z / 90f) * 19349663);
        var random = new System.Random(seed);

        for (var i = 0; i < 56; i++)
        {
            var angle = (float)(random.NextDouble() * Math.PI * 2d);
            var distance = 18f + (float)random.NextDouble() * 165f;
            var x = center.x + Mathf.Cos(angle) * distance;
            var z = center.z + Mathf.Sin(angle) * distance;
            var surfaceBiome = generator.GetBiome(x, z);
            var vanillaHeight = generator.GetBiomeHeight(surfaceBiome, x, z, out _);
            var sample = UnderworldTerrainRuntime.SampleTerrain(x, z, vanillaHeight);
            if (!sample.Admitted) continue;
            SpawnDonor(new Vector3(x, (float)sample.Height, z), sample.Biome, i + seed);
        }

        _log?.LogDebug(
            $"Refreshed {_spawned.Count} vanilla-donor Underworld ecology objects near ({center.x:0},{center.z:0}).");
    }

    private void SpawnDonor(Vector3 position, UnderworldTerrainBiome biome, int variant)
    {
        UnderworldVanillaDonorCatalog.Donor donor = default;
        GameObject? source = null;
        var selectedVariant = variant;

        for (var attempt = 0; attempt < 8 && source is null; attempt++)
        {
            selectedVariant = variant + attempt;
            try { donor = UnderworldVanillaDonorCatalog.Select(biome, selectedVariant); }
            catch (InvalidOperationException exception)
            {
                _log?.LogWarning(exception.Message);
                return;
            }
            source = ResolveDonor(donor.PrefabName);
        }

        if (source is null) return;
        var node = BuildVisualClone(source, donor, selectedVariant);
        if (node is null) return;

        node.name = $"Magenheim_UnderworldDonor_{biome}_{donor.PrefabName}";
        node.transform.position = position + Vector3.up * donor.GroundOffset;
        node.transform.rotation = Quaternion.Euler(
            Mathf.Abs(selectedVariant % 7) - 3f,
            Mathf.Abs(selectedVariant * 37 % 360),
            Mathf.Abs(selectedVariant % 9) - 4f);

        var scale = UnderworldVanillaDonorCatalog.Scale(donor, selectedVariant);
        node.transform.localScale = scale;
        var lightScale = Mathf.Clamp(Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z)), 1f, 12f);
        foreach (var light in node.GetComponentsInChildren<Light>(true))
            light.range *= lightScale;

        _spawned.Add(node);
    }

    private GameObject? ResolveDonor(string prefabName)
    {
        if (_donorSources.TryGetValue(prefabName, out var cached) && cached) return cached;
        var source = PrefabManager.Instance.GetPrefab(prefabName);
        if (source is null)
        {
            if (_warnedDonors.Add(prefabName))
                _log?.LogWarning(
                    $"Underworld donor '{prefabName}' is not available yet or is absent in this Valheim build; skipping this placement and retrying later.");
            return null;
        }

        _donorSources[prefabName] = source;
        return source;
    }

    private GameObject? BuildVisualClone(
        GameObject source,
        UnderworldVanillaDonorCatalog.Donor donor,
        int variant)
    {
        var root = new GameObject($"Magenheim_UnderworldDonorVisual_{source.name}_{variant}");
        var transformMap = new Dictionary<Transform, Transform>();
        var rendererMap = new Dictionary<Renderer, Renderer>();
        var copiedRenderers = 0;

        CopyNode(source.transform, root.transform, false, transformMap, rendererMap, ref copiedRenderers);
        if (copiedRenderers == 0)
        {
            Destroy(root);
            if (_warnedDonors.Add(source.name))
                _log?.LogWarning($"Underworld donor '{source.name}' contained no supported mesh renderers.");
            return null;
        }

        RebuildLodGroups(source, transformMap, rendererMap);
        if (donor.Collidable) AddConservativeCollider(root);
        return root;
    }

    private static void CopyNode(
        Transform source,
        Transform target,
        bool copyTransform,
        IDictionary<Transform, Transform> transformMap,
        IDictionary<Renderer, Renderer> rendererMap,
        ref int copiedRenderers)
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
            var sourceChild = source.GetChild(i);
            var targetChild = new GameObject(sourceChild.name).transform;
            targetChild.SetParent(target, false);
            CopyNode(sourceChild, targetChild, true, transformMap, rendererMap, ref copiedRenderers);
        }
    }

    private static void RebuildLodGroups(
        GameObject source,
        IReadOnlyDictionary<Transform, Transform> transformMap,
        IReadOnlyDictionary<Renderer, Renderer> rendererMap)
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
                    if (renderer is not null && rendererMap.TryGetValue(renderer, out var targetRenderer))
                        mapped.Add(targetRenderer);

                targetLods[i] = new LOD(sourceLods[i].screenRelativeTransitionHeight, mapped.ToArray())
                {
                    fadeTransitionWidth = sourceLods[i].fadeTransitionWidth
                };
            }

            var targetGroup = targetTransform.gameObject.AddComponent<LODGroup>();
            targetGroup.localReferencePoint = sourceGroup.localReferencePoint;
            targetGroup.size = sourceGroup.size;
            targetGroup.fadeMode = sourceGroup.fadeMode;
            targetGroup.animateCrossFading = sourceGroup.animateCrossFading;
            targetGroup.SetLODs(targetLods);
            targetGroup.RecalculateBounds();
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

    private void ClearMarkers()
    {
        foreach (var item in _spawned)
            if (item) Destroy(item);
        _spawned.Clear();
    }

    private void OnDestroy()
    {
        ClearMarkers();
        _donorSources.Clear();
        _warnedDonors.Clear();
    }
}
