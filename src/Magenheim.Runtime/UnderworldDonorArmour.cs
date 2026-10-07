using System;
using System.Collections.Generic;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Palette modifications of cloned native equipment; never replaces its mesh or rig.</summary>
internal static class UnderworldDonorArmour
{
    private static readonly Dictionary<string, Texture2D> Overlays = new(StringComparer.Ordinal);

    internal static void Apply(GameObject prefab, string modelId)
    {
        var tint = UnderworldDonorArmourProfiles.Tint(modelId);
        var cape = modelId.EndsWith("-cape", StringComparison.Ordinal);
        foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer is ParticleSystemRenderer) continue;
            var sources = renderer.sharedMaterials;
            var owned = new Material[sources.Length];
            for (var i = 0; i < sources.Length; i++)
            {
                var source = sources[i];
                if (!source) { owned[i] = source!; continue; }
                var material = new Material(source) { name = "magenheim.donor-armour." + modelId + "." + source.name };
                if (cape && material.HasProperty("_MainTex") && material.GetTexture("_MainTex") is { } cloth)
                    material.SetTexture("_MainTex", Recolour(cloth, tint, modelId));
                else if (material.HasProperty("_Color")) material.color = material.color * tint;
                owned[i] = material;
            }
            renderer.sharedMaterials = owned;
        }

        // VisEquipment copies these overlay textures to the player's body; it does not copy
        // the armour material's tint. Recolour owned copies of the native texture, preserving alpha.
        var shared = prefab.GetComponent<ItemDrop>().m_itemData.m_shared;
        if (!shared.m_armorMaterial) return;
        var overlay = new Material(shared.m_armorMaterial);
        foreach (var property in new[] { "_ChestTex", "_LegTex", "_MainTex" })
        {
            if (!overlay.HasProperty(property)) continue;
            var texture = overlay.GetTexture(property);
            if (texture) overlay.SetTexture(property, Recolour(texture, tint, modelId));
        }
        shared.m_armorMaterial = overlay;
    }

    private static Texture2D Recolour(Texture source, Color tint, string modelId)
    {
        var key = source.GetInstanceID() + ":" + modelId;
        if (Overlays.TryGetValue(key, out var cached)) return cached;
        var previous = RenderTexture.active;
        var temporary = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Texture2D? owned = null;
        try
        {
            Graphics.Blit(source, temporary);
            RenderTexture.active = temporary;
            owned = new Texture2D(source.width, source.height, TextureFormat.RGBA32, true, false)
                { name = "magenheim.donor-armour." + modelId + "." + source.name, wrapMode = source.wrapMode, filterMode = source.filterMode };
            owned.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            var pixels = owned.GetPixels();
            for (var i = 0; i < pixels.Length; i++)
            {
                var alpha = pixels[i].a;
                pixels[i] *= tint;
                if (modelId.EndsWith("-cape", StringComparison.Ordinal) && alpha > .1f)
                {
                    var x = i % source.width;
                    var y = i / source.width;
                    // Owned woven panels and stitched hems retain the donor's
                    // alpha, fur relief, UV layout, skinning and cloth behavior.
                    var u = (float)x / source.width;
                    var v = (float)y / source.height;
                    var hem = u < .045f || u > .955f || v < .05f;
                    var stitch = (x / 5 + y / 5) % 2 == 0;
                    var rib = x % 24 < 2;
                    if (hem && stitch) pixels[i] = Color.Lerp(pixels[i], tint * .72f, .65f);
                    else if (rib) pixels[i] *= .78f;
                }
                pixels[i].a = alpha;
            }
            owned.SetPixels(pixels);
            owned.Apply(true, true);
            Overlays.Add(key, owned);
            return owned;
        }
        catch { if (owned) UnityEngine.Object.Destroy(owned); throw; }
        finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(temporary); }
    }
}
