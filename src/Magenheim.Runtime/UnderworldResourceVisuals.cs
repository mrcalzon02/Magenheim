using System;
using System.Collections.Generic;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Authored appearances for Underworld raw resources, replacing the vanilla item and pickup donors'
/// looks. Visual only: the item's identity, stack, weight and the pickup's behaviour are unchanged.
/// </summary>
internal static class UnderworldResourceVisuals
{
    // Resource prefab -> authored model (tools/author-underworld-fungal-forest.py). Resources without
    // an entry keep their vanilla appearance until their biome's custom pass.
    private static readonly Dictionary<string, string> Models = new(StringComparer.Ordinal)
    {
        ["Magenheim_Underworld_Resource_WorldrootTimber"] = "underworld-resource-worldroot-timber",
        ["Magenheim_Underworld_Resource_GlowcapFlesh"] = "underworld-resource-glowcap-flesh",
        ["Magenheim_Underworld_Resource_SpireFibre"] = "underworld-resource-spire-fibre",
        ["Magenheim_Underworld_Resource_Understone"] = "underworld-resource-understone",
    };

    internal static string? ModelFor(string prefab) => Models.TryGetValue(prefab, out var model) ? model : null;

    /// <summary>
    /// Replaces a dropped item's or pickup's visible body with the authored model and refits its
    /// solid collider to that body: a Worldroot bundle must not keep a two-metre RoundLog capsule.
    /// Trigger colliders are left alone. Returns the visual root, which a Pickable hides when picked.
    /// </summary>
    internal static GameObject Apply(GameObject prefab, string model)
    {
        var solids = new List<Collider>();
        foreach (var collider in prefab.GetComponentsInChildren<Collider>(true))
            if (!collider.isTrigger) solids.Add(collider);
        var visual = ModelAssets.Load(prefab, model);
        var filters = visual.GetComponentsInChildren<MeshFilter>(true);
        if (filters.Length == 0) throw new InvalidOperationException($"Resource model {model} has no geometry.");
        var bounds = new Bounds();
        var first = true;
        foreach (var filter in filters)
        {
            var mesh = filter.sharedMesh.bounds;
            for (var corner = 0; corner < 8; corner++)
            {
                var point = prefab.transform.InverseTransformPoint(filter.transform.TransformPoint(new Vector3(
                    (corner & 1) == 0 ? mesh.min.x : mesh.max.x, (corner & 2) == 0 ? mesh.min.y : mesh.max.y,
                    (corner & 4) == 0 ? mesh.min.z : mesh.max.z)));
                if (first) { bounds = new Bounds(point, Vector3.zero); first = false; }
                else bounds.Encapsulate(point);
            }
        }
        foreach (var collider in solids) UnityEngine.Object.DestroyImmediate(collider);
        var box = prefab.AddComponent<BoxCollider>();
        box.center = bounds.center;
        box.size = Vector3.Max(bounds.size, Vector3.one * 0.05f);
        return visual;
    }
}
