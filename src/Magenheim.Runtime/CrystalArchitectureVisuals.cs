using System;
using System.Linq;
using Jotunn.Managers;
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
    /// <summary>Vanilla static-rock material, cloned as the surface source for slabs and pillars.</summary>
    /// <remarks>
    /// Foundations and beams clone building-piece donors (stone_floor_2x2, wood_beam,
    /// piece_workbench). Those donors' materials use Valheim's piece shader, which projects its
    /// surface from world position and ignores the UVs the exporter writes, so a Magenheim mesh
    /// wearing one renders as a smeared lattice regardless of its own geometry, UVs or texture --
    /// reported repeatedly from the field, and confirmed by the source .blend and the exported
    /// runtime payload both being clean when rendered directly. Rock_4's material is a plain static
    /// surface that honours mesh UVs, which is exactly why the geode shell reads correctly, so the
    /// same donor is used here. The hearth keeps its existing path untouched: it is confirmed
    /// working in the field and is not part of this repair.
    /// </remarks>
    private const string SurfaceDonorPrefab = "Rock_4";
    private static Material? _surfaceDonor;
    private static bool _surfaceDonorResolved;

    internal static Material? SurfaceDonorMaterial() => SurfaceDonor();

    private static Material? SurfaceDonor() {
        if (_surfaceDonorResolved) return _surfaceDonor;
        _surfaceDonorResolved = true;
        var rock = PrefabManager.Instance?.GetPrefab(SurfaceDonorPrefab);
        if (rock is null) return _surfaceDonor = null;
        var group = rock.GetComponentInChildren<LODGroup>(true);
        var levels = group is not null ? group.GetLODs() : Array.Empty<LOD>();
        var renderers = levels.Length > 0 && levels[0].renderers is { Length: > 0 }
            ? levels[0].renderers
            : rock.GetComponentsInChildren<MeshRenderer>(true);
        foreach (var renderer in renderers) {
            if (!renderer || renderer.sharedMaterial is null) continue;
            _surfaceDonor = renderer.sharedMaterial; break;
        }
        return _surfaceDonor;
    }

    internal static GameObject Apply(GameObject prefab, string modelId) {
        var source = modelId == CrystalHearth ? null : SurfaceDonor();
        var root=ModelAssets.Load(prefab,modelId,preserveParticles:true,materialSource:source);
        if(modelId==CrystalHearth){TintVanillaFlame(prefab);FitFlameToPiece(prefab,root);}
        return root;
    }

    /// <summary>
    /// Rescales the donor's retained fire to the Magenheim hearth it now sits in.
    /// </summary>
    /// <remarks>
    /// <c>preserveParticles</c> keeps the donor's particle systems untouched while the visible mesh
    /// is replaced, and nothing reconciled the two: the flame kept the size it was authored for on a
    /// donor of a different footprint, and in play it towered over the piece. The ratio is measured
    /// rather than assumed, from the donor's own disabled renderers against the loaded model, so it
    /// stays correct if either the donor or the hearth model is re-authored.
    /// </remarks>
    private static void FitFlameToPiece(GameObject prefab, GameObject root)
    {
        if (!prefab || !root) return;
        if (!TryMeasure(root.GetComponentsInChildren<Renderer>(true), null, out var piece)) return;
        if (!TryMeasure(prefab.GetComponentsInChildren<Renderer>(true), root.transform, out var donor)) return;
        if (donor.size.y <= 0.01f || piece.size.y <= 0.01f) return;

        // Height is the axis the flame reads on. Clamp so a pathological donor cannot invert the
        // fire into a spark or blow it up further than it already was.
        var ratio = Mathf.Clamp(piece.size.y / donor.size.y, 0.2f, 1f);
        if (ratio > 0.98f) return;
        foreach (var system in prefab.GetComponentsInChildren<ParticleSystem>(true))
        {
            var transform = system.transform;
            if (transform == prefab.transform) continue;
            // Scaling the transform alone is why this had no visible effect and the fire still
            // towered over the piece. A particle system only inherits its transform's scale into
            // particle size under Hierarchy scaling; under Shape -- which is what a fire authored
            // to sit in one specific fireplace typically uses -- the transform scales the emission
            // volume and the flames stay exactly as big as they were. State the mode rather than
            // inherit whichever one the donor happened to ship.
            var main = system.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            transform.localScale *= ratio;
        }
    }

    private static bool TryMeasure(Renderer[] renderers, Transform? exclude, out Bounds bounds)
    {
        bounds = default;
        var found = false;
        foreach (var renderer in renderers)
        {
            if (!renderer || renderer is ParticleSystemRenderer) continue;
            if (exclude is not null && renderer.transform.IsChildOf(exclude)) continue;
            if (!found) { bounds = renderer.bounds; found = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        return found && bounds.size.sqrMagnitude > 0f;
    }

private static void TintVanillaFlame(GameObject prefab)
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, .12f, .08f), 0f), new GradientColorKey(new Color(1f, .78f, .08f), .16f),
                new GradientColorKey(new Color(.30f, .85f, .18f), .33f), new GradientColorKey(new Color(.10f, .90f, 1f), .50f),
                new GradientColorKey(new Color(.20f, .36f, 1f), .67f), new GradientColorKey(new Color(.72f, .18f, 1f), .84f),
                new GradientColorKey(new Color(1f, .16f, .62f), 1f)
            },
            // Opaque across the whole span. Under RandomColor a particle samples this gradient at a
            // random position -- colour *and* alpha -- so a ramp that fades to zero at 1 does not
            // fade anything over time, it makes every particle drawn from the violet end of the
            // spectrum translucent and the magenta end invisible. That left the opaque red-to-cyan
            // half carrying the fire, and additive blending resolved it as green, which is what
            // play reported. The fade over a particle's life is colorOverLifetime's job below, and
            // it already does exactly that. Green is also pulled down from full intensity here: it
            // is the most luminous hue in the ramp and reads brighter than its neighbours even at
            // equal sampling.
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });

        // The spectrum has to be distributed across the particles alive at any instant, not swept
        // along one particle's lifetime. On colorOverLifetime every particle is born at key 0 -- red
        // -- reaches green only at 33% and blue at 50%, while the alpha keys hold it opaque to 75%
        // and fade it out by 100%. Each particle therefore spends its bright phase in the red-to-
        // green half and dies out through blue and purple, so the fire read as red with an
        // occasional green flicker rather than as a rainbow. RandomColor picks each particle's
        // colour from the gradient at birth; colorOverLifetime is reduced to the alpha fade so it
        // no longer overrides that choice.
        var fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, .75f), new GradientAlphaKey(0f, 1f) });

        var tinted = 0;
        foreach (var system in prefab.GetComponentsInChildren<ParticleSystem>(true))
        {
            var name = system.gameObject.name;
            if (name.IndexOf("smoke", StringComparison.OrdinalIgnoreCase) >= 0) continue;
            if (name.IndexOf("sparks", StringComparison.OrdinalIgnoreCase) >= 0) continue;
            var colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(fade);
            var main = system.main;
            main.startColor = new ParticleSystem.MinMaxGradient(gradient)
            {
                mode = ParticleSystemGradientMode.RandomColor,
            };
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
