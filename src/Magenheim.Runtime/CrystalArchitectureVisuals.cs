using System;
using System.Linq;
using Magenheim.Core;
using UnityEngine;
namespace Magenheim.Runtime;
/// <summary>Loads editable assets from assets/models/source; geometry is never generated in game.</summary>
internal static class CrystalArchitectureVisuals
{
    internal const string CrystalHearth = "architecture-crystal-hearth";
    internal const string CrystalBeam2 = "architecture-crystal-beam-2m";
    internal const string CrystalBeam4 = "architecture-crystal-beam-4m";
    internal const string CrystalBeam8 = "architecture-crystal-beam-8m";
    internal const string CrystalFoundation2 = "architecture-crystal-foundation-2m";
    internal const string CrystalFoundation4 = "architecture-crystal-foundation-4m";
    internal const string CrystalFoundation8 = "architecture-crystal-foundation-8m";
    internal static GameObject Apply(GameObject prefab, string modelId) {
        var root=ModelAssets.Load(prefab,modelId,preserveParticles:true);
        if(modelId==CrystalHearth)TintVanillaFlame(prefab);
        return root;
    }
private static void TintVanillaFlame(GameObject prefab)
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, .12f, .08f), 0f), new GradientColorKey(new Color(1f, .78f, .08f), .16f),
                new GradientColorKey(new Color(.35f, 1f, .20f), .33f), new GradientColorKey(new Color(.10f, .90f, 1f), .50f),
                new GradientColorKey(new Color(.20f, .36f, 1f), .67f), new GradientColorKey(new Color(.72f, .18f, 1f), .84f),
                new GradientColorKey(new Color(1f, .16f, .62f), 1f)
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, .75f), new GradientAlphaKey(0f, 1f) });

        var tinted = 0;
        foreach (var system in prefab.GetComponentsInChildren<ParticleSystem>(true))
        {
            var name = system.gameObject.name;
            if (name.IndexOf("smoke", StringComparison.OrdinalIgnoreCase) >= 0) continue;
            if (name.IndexOf("sparks", StringComparison.OrdinalIgnoreCase) >= 0) continue;
            var colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
            var main = system.main;
            main.startColor = new ParticleSystem.MinMaxGradient(Color.white);
            tinted++;
        }
        var lightColors = new[] { new Color(1f, .22f, .12f), new Color(.16f, .72f, 1f), new Color(.78f, .22f, 1f) };
        var lights = prefab.GetComponentsInChildren<Light>(true);
        for (var i = 0; i < lights.Length; i++)
        {
            lights[i].color = lightColors[i % lightColors.Length];
            lights[i].intensity = Mathf.Max(lights[i].intensity, 1.35f);
        }
        if (tinted == 0) throw new InvalidOperationException("Crystal Hearth source contains no non-smoke flame particle system to recolor.");
    }
}
