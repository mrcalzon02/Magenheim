using System;
using System.Linq;
using Magenheim.Core;
using UnityEngine;
namespace Magenheim.Runtime;
/// <summary>Loads editable assets from assets/models/source; geometry is never generated in game.</summary>
internal static class CrystalSentinelVisuals
{

    internal static GameObject Apply(GameObject prefab) => ModelAssets.Load(prefab,"crystal-sentinel");
    internal static GameObject CreateAmmoVisual(Turret turret,ElementalAlignment element,Material source) {
        var root=ModelAssets.Load(turret.gameObject,"sentinel-ammo-"+element.ToString().ToLowerInvariant(),hideOriginal:false,parent:turret.m_turretBody?turret.m_turretBody.transform:turret.transform);
        root.SetActive(false);return root;
    }
internal static void TintProjectile(GameObject projectile, ElementalAlignment element)
    {
        var tint = ElementVisualPalette.Tint(element);
        foreach (var renderer in projectile.GetComponentsInChildren<Renderer>(true))
        {
            var sources = renderer.sharedMaterials;
            var materials = new Material[sources.Length];
            for (var i = 0; i < sources.Length; i++)
            {
                var source = sources[i];
                if (!source) continue;
                var material = new Material(source) { name = $"magenheim.sentinel.projectile.{element}.{i}" };
                GeneratedSurfaceTextures.Apply(material, "crystal-projectile");
                if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", tint * 1.25f);
                    material.EnableKeyword("_EMISSION");
                }
                materials[i] = material;
            }
            renderer.sharedMaterials = materials;
        }
        foreach (var particles in projectile.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = particles.main;
            main.startColor = new ParticleSystem.MinMaxGradient(tint);
        }
    }
}
