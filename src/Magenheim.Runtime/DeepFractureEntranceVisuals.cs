using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

internal static class DeepFractureEntranceVisuals
{
    internal const string InteriorAnchorName = "Magenheim_DeepFracture_InteriorAnchor";

    internal static void Build(GameObject container)
    {
        var donor = ZoneManager.Instance.GetZoneLocation("MountainCave02")
            ?? throw new InvalidOperationException("Native Frost Cave entrance MountainCave02 is unavailable.");
        donor.m_prefab.Load();
        try
        {
            var source = donor.m_prefab.Asset
                ?? throw new InvalidOperationException("Native Frost Cave entrance failed to load.");
            var nativeLocation = source.GetComponent<Location>()
                ?? throw new InvalidOperationException("Native cave donor has no Location component.");
            var location = container.GetComponent<Location>() ?? container.AddComponent<Location>();
            location.m_interiorPrefab = nativeLocation.m_interiorPrefab
                ?? throw new InvalidOperationException("Native cave interior environment prefab is unavailable.");
            // Preserve shipped exterior geometry, excluding donor world/creature behavior.
            var copies = new Dictionary<GameObject, GameObject>();
            foreach (Transform child in source.transform)
                if (child.name != "Interior") CopyExterior(child, container.transform, copies);
            foreach (var nativeLod in source.GetComponentsInChildren<LODGroup>(true))
            {
                if (!copies.TryGetValue(nativeLod.gameObject, out var copiedObject)) continue;
                var lod = copiedObject.AddComponent<LODGroup>();
                lod.fadeMode = nativeLod.fadeMode;
                lod.animateCrossFading = nativeLod.animateCrossFading;
                lod.localReferencePoint = nativeLod.localReferencePoint;
                lod.size = nativeLod.size;
                lod.SetLODs(nativeLod.GetLODs().Select(level => new LOD(level.screenRelativeTransitionHeight,
                    level.renderers.Where(renderer => renderer && copies.ContainsKey(renderer.gameObject))
                    .Select(renderer => copies[renderer.gameObject].GetComponent<Renderer>())
                    .Where(renderer => renderer).ToArray()) { fadeTransitionWidth = level.fadeTransitionWidth }).ToArray());
            }
            var gateway = source.transform.Find("Gateway")
                ?? throw new InvalidOperationException("Native Frost Cave entrance has no Gateway.");
            var anchor = new GameObject(InteriorAnchorName);
            anchor.transform.SetParent(container.transform, false);
            anchor.transform.localPosition = gateway.localPosition;
            anchor.transform.localRotation = gateway.localRotation;
        }
        finally { donor.m_prefab.Release(); }
    }

    private static void CopyExterior(Transform source, Transform parent, Dictionary<GameObject, GameObject> copies)
    {
        var target = new GameObject(source.name) { layer = source.gameObject.layer };
        copies.Add(source.gameObject, target);
        target.transform.SetParent(parent, false);
        target.transform.localPosition = source.localPosition;
        target.transform.localRotation = source.localRotation;
        target.transform.localScale = source.localScale;
        var mesh = source.GetComponent<MeshFilter>();
        var renderer = source.GetComponent<MeshRenderer>();
        if (mesh && mesh.sharedMesh && renderer)
        {
            target.AddComponent<MeshFilter>().sharedMesh = mesh.sharedMesh;
            var copied = target.AddComponent<MeshRenderer>();
            copied.sharedMaterials = renderer.sharedMaterials;
            copied.shadowCastingMode = renderer.shadowCastingMode;
            copied.receiveShadows = renderer.receiveShadows;
        }
        foreach (var collider in source.GetComponents<Collider>())
        {
            if (collider.isTrigger) continue;
            Collider? copy = null;
            if (collider is MeshCollider native)
            {
                var value = target.AddComponent<MeshCollider>();
                value.sharedMesh = native.sharedMesh; value.convex = native.convex; copy = value;
            }
            else if (collider is BoxCollider box)
            {
                var value = target.AddComponent<BoxCollider>();
                value.center = box.center; value.size = box.size; copy = value;
            }
            else if (collider is SphereCollider sphere)
            {
                var value = target.AddComponent<SphereCollider>();
                value.center = sphere.center; value.radius = sphere.radius; copy = value;
            }
            if (copy) copy.sharedMaterial = collider.sharedMaterial;
        }
        foreach (Transform child in source) CopyExterior(child, target.transform, copies);
        target.SetActive(source.gameObject.activeSelf);
    }
}
