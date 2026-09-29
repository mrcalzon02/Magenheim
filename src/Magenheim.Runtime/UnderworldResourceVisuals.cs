using System;
using System.Collections.Generic;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Authored appearances for Underworld raw and refined materials, replacing vanilla donor looks.
/// Visual only: item identity, stack, weight, pickup lifecycle and recipe authority are unchanged.
/// </summary>
internal static class UnderworldResourceVisuals
{
    // Material prefab -> authored model. Production gates prove each canonical mapping before release.
    private static readonly Dictionary<string, string> Models = new(StringComparer.Ordinal)
    {
        ["Magenheim_Underworld_Resource_WorldrootTimber"] = "underworld-resource-worldroot-timber",
        ["Magenheim_Underworld_Resource_GlowcapFlesh"] = "underworld-resource-glowcap-flesh",
        ["Magenheim_Underworld_Resource_SpireFibre"] = "underworld-resource-spire-fibre",
        ["Magenheim_Underworld_Resource_Understone"] = "underworld-resource-understone",
        ["Magenheim_Underworld_Resource_BlackwaterFlowstone"] = "underworld-resource-blackwater-flowstone",
        ["Magenheim_Underworld_Resource_PaleFibre"] = "underworld-resource-pale-fibre",
        ["Magenheim_Underworld_Resource_BlackwaterPearl"] = "underworld-resource-blackwater-pearl",
        ["Magenheim_Underworld_Resource_DeepSalt"] = "underworld-resource-deep-salt",
        ["Magenheim_Underworld_Resource_Slagstone"] = "underworld-resource-slagstone",
        ["Magenheim_Underworld_Resource_Sulfur"] = "underworld-resource-sulfur",
        ["Magenheim_Underworld_Resource_CharredTimber"] = "underworld-resource-charred-timber",
        ["Magenheim_Underworld_Resource_Emberiron"] = "underworld-resource-emberiron",
        ["Magenheim_Underworld_Resource_Rimewood"] = "underworld-resource-rimewood",
        ["Magenheim_Underworld_Resource_ClearIce"] = "underworld-resource-clear-ice",
        ["Magenheim_Underworld_Resource_Rimesilver"] = "underworld-resource-rimesilver",
        ["Magenheim_Underworld_Resource_FractureCrystal"] = "underworld-resource-fracture-crystal",
        ["Magenheim_Underworld_Resource_Shardstone"] = "underworld-resource-shardstone",
        ["Magenheim_Underworld_Resource_Titanbone"] = "underworld-resource-titanbone",
        ["Magenheim_Underworld_Resource_Rotwood"] = "underworld-resource-rotwood",
        ["Magenheim_Underworld_Resource_DecaySpore"] = "underworld-resource-decay-spore",
        ["Magenheim_Underworld_Resource_CarrionAmber"] = "underworld-resource-carrion-amber",
        ["Magenheim_Underworld_Resource_BoneGravel"] = "underworld-resource-bone-gravel",
        ["Magenheim_Underworld_Refined_WorldrootPlank"] = "underworld-refined-worldroot-plank",
        ["Magenheim_Underworld_Refined_SpireCord"] = "underworld-refined-spire-cord",
        ["Magenheim_Underworld_Refined_CuredGlowcap"] = "underworld-refined-cured-glowcap",
        ["Magenheim_Underworld_Refined_FlowstonePlate"] = "underworld-refined-flowstone-plate",
        ["Magenheim_Underworld_Refined_PaleCord"] = "underworld-refined-pale-cord",
        ["Magenheim_Underworld_Refined_BrinedPearl"] = "underworld-refined-brined-pearl",
        ["Magenheim_Underworld_Refined_EmberironBar"] = "underworld-refined-emberiron-bar",
        ["Magenheim_Underworld_Refined_TemperedSlag"] = "underworld-refined-tempered-slag",
        ["Magenheim_Underworld_Refined_CharredRootGrip"] = "underworld-refined-charred-root-grip",
        ["Magenheim_Underworld_Refined_RimesilverBar"] = "underworld-refined-rimesilver-bar",
        ["Magenheim_Underworld_Refined_IceglassLens"] = "underworld-refined-iceglass-lens",
        ["Magenheim_Underworld_Refined_RimewoodLaminate"] = "underworld-refined-rimewood-laminate",
        ["Magenheim_Underworld_Refined_TitanbonePlate"] = "underworld-refined-titanbone-plate",
        ["Magenheim_Underworld_Refined_ShardstoneBlock"] = "underworld-refined-shardstone-block",
        ["Magenheim_Underworld_Refined_FracturePrism"] = "underworld-refined-fracture-prism",
        ["Magenheim_Underworld_Refined_CarrionAmberSeal"] = "underworld-refined-carrion-amber-seal",
        ["Magenheim_Underworld_Refined_OssuaryComposite"] = "underworld-refined-ossuary-composite",
        ["Magenheim_Underworld_Refined_RotwoodLaminate"] = "underworld-refined-rotwood-laminate",
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
