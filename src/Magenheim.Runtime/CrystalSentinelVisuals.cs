using UnityEngine;
namespace Magenheim.Runtime;
/// <summary>Loads editable assets from assets/models/source; geometry is never generated in game.</summary>
internal static class CrystalSentinelVisuals
{
    // The Sentinel now takes a single standardized Magenheim_CrystalMunition rather than one
    // item per element, so its loaded-ammo visual and projectile are element-agnostic. No
    // neutral munition model is authored yet, so the standardized visual borrows this body
    // until one exists; see docs/RENDERING_AND_CONTENT_DEFECTS.md (C5).
    private const string StandardMunitionModel = "sentinel-ammo-radiance";

    internal static GameObject Apply(GameObject prefab) => ModelAssets.Load(prefab, "crystal-sentinel");

    internal static GameObject CreateAmmoVisual(Turret turret, Material source)
    {
        var root = ModelAssets.Load(
            turret.gameObject,
            StandardMunitionModel,
            hideOriginal: false,
            parent: turret.m_turretBody ? turret.m_turretBody.transform : turret.transform);
        root.SetActive(false);
        return root;
    }

    internal static void TintProjectile(GameObject projectile)
    {
        foreach (var renderer in projectile.GetComponentsInChildren<Renderer>(true))
        {
            var sources = renderer.sharedMaterials;
            var materials = new Material[sources.Length];
            for (var i = 0; i < sources.Length; i++)
            {
                var source = sources[i];
                if (!source) continue;
                var material = new Material(source) { name = $"magenheim.sentinel.projectile.{i}" };
                GeneratedSurfaceTextures.Apply(material, "crystal-projectile");
                materials[i] = material;
            }
            renderer.sharedMaterials = materials;
        }
    }
}
