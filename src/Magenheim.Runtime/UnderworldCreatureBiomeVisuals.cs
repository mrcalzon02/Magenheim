using System;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Gives donor-backed Underworld creatures an owned surface language while preserving the donor rig,
/// animator, AI, attack sockets, colliders and networking. This is intentionally a retexture layer,
/// not a claim that the donor silhouette is final art.
/// </summary>
internal static class UnderworldCreatureBiomeVisuals
{
    private const int TextureSize = 128;

    private enum SurfaceFamily
    {
        Hide,
        Carapace,
        Scale,
        Stone,
        Membrane,
        Decay
    }

    private readonly struct Palette
    {
        internal Palette(Color baseColor, Color secondary, Color accent, float smoothness)
        {
            Base = baseColor;
            Secondary = secondary;
            Accent = accent;
            Smoothness = smoothness;
        }

        internal Color Base { get; }
        internal Color Secondary { get; }
        internal Color Accent { get; }
        internal float Smoothness { get; }
    }

    internal static int Apply(GameObject root, UnderworldCreaturePrototypes.Entry entry)
    {
        if (!root) throw new ArgumentNullException(nameof(root));
        if (entry is null) throw new ArgumentNullException(nameof(entry));

        var palette = PaletteFor(entry);
        var family = SurfaceFor(entry);
        var replaced = 0;

        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;

            var originals = renderer.sharedMaterials;
            if (originals is null || originals.Length == 0) continue;

            var materials = new Material[originals.Length];
            for (var slot = 0; slot < originals.Length; slot++)
            {
                var original = originals[slot];
                if (!original)
                {
                    materials[slot] = original!;
                    continue;
                }

                var material = new Material(original)
                {
                    name = $"Magenheim_{entry.Name.Replace(" ", "")}_{renderer.name}_{slot}",
                    hideFlags = HideFlags.HideAndDontSave
                };

                var texture = BuildTexture(entry, renderer.name, slot, palette, family);
                if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
                if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);

                if (material.HasProperty("_Glossiness"))
                    material.SetFloat("_Glossiness", palette.Smoothness);
                if (material.HasProperty("_Smoothness"))
                    material.SetFloat("_Smoothness", palette.Smoothness);
                if (material.HasProperty("_Metallic"))
                    material.SetFloat("_Metallic", family == SurfaceFamily.Stone ? 0.06f : 0f);

                materials[slot] = material;
                replaced++;
            }

            renderer.sharedMaterials = materials;
            renderer.SetPropertyBlock(null);
        }

        return replaced;
    }

    private static Texture2D BuildTexture(
        UnderworldCreaturePrototypes.Entry entry,
        string rendererName,
        int slot,
        Palette palette,
        SurfaceFamily family)
    {
        var seed = StableHash(entry.Name + "|" + entry.Biome + "|" + rendererName + "|" + slot);
        var pixels = new Color[TextureSize * TextureSize];

        for (var y = 0; y < TextureSize; y++)
        {
            for (var x = 0; x < TextureSize; x++)
            {
                var u = (x + 0.5f) / TextureSize;
                var v = (y + 0.5f) / TextureSize;
                var macro = ValueNoise(x >> 3, y >> 3, seed);
                var fine = ValueNoise(x, y, seed ^ 0xA511E9B3u);
                var creature = ValueNoise(x >> 2, y >> 2, seed ^ 0x63D83595u);

                var biomeMask = BiomeMask(entry.Biome, u, v, macro, fine, seed);
                var surfaceMask = SurfaceMask(family, u, v, creature, seed);
                var signature = SignatureMask(entry.Name, family, u, v, macro, creature, seed);
                var body = Mathf.Clamp01(0.08f + macro * 0.34f + surfaceMask * 0.28f + signature * 0.30f);
                var color = Color.Lerp(palette.Base, palette.Secondary, body);

                // Biome language and creature-specific markings are both intentionally strong. Two
                // Wolf-derived creatures should read as different animals before the player notices
                // that their locomotion chassis is shared.
                var biomeAccent = Mathf.Clamp01((biomeMask - 0.46f) * 2.15f);
                var signatureAccent = Mathf.Clamp01((signature - 0.56f) * 2.30f);
                var accent = Mathf.Max(biomeAccent, signatureAccent);
                if (accent > 0f)
                    color = Color.Lerp(color, palette.Accent, accent);

                // Break the flat donor read without turning the creature into visual noise.
                var grain = (fine - 0.5f) * 0.10f;
                color.r = Mathf.Clamp01(color.r + grain);
                color.g = Mathf.Clamp01(color.g + grain);
                color.b = Mathf.Clamp01(color.b + grain);
                color.a = 1f;
                pixels[y * TextureSize + x] = color;
            }
        }

        var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false, false)
        {
            name = $"Magenheim_{entry.Name.Replace(" ", "")}_{rendererName}_{slot}_Albedo",
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
            anisoLevel = 2,
            hideFlags = HideFlags.HideAndDontSave
        };
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }

    private static float BiomeMask(string biome, float u, float v, float macro, float fine, uint seed)
    {
        if (biome == "Fungal Forest")
        {
            var mycelium = 1f - Mathf.Abs(Mathf.Sin((u * 9f + v * 5f + macro * 2f) * Mathf.PI));
            var spores = ValueNoise((int)(u * 31f), (int)(v * 31f), seed ^ 0x0F0F0F0Fu);
            return Mathf.Max(mycelium * 0.78f, spores > 0.91f ? 1f : 0f);
        }

        if (biome == "Blackwater Deep")
        {
            var pressureBands = 0.5f + 0.5f * Mathf.Sin((v * 18f + macro * 3f) * Mathf.PI);
            var glints = fine > 0.94f ? 1f : 0f;
            return Mathf.Max(pressureBands * 0.66f, glints);
        }

        if (biome == "Sulfurous Wastes")
        {
            var gx = Mathf.Abs(Mathf.Repeat(u * 11f + macro, 1f) - 0.5f);
            var gy = Mathf.Abs(Mathf.Repeat(v * 9f + macro * 0.7f, 1f) - 0.5f);
            var cracks = 1f - Mathf.Clamp01(Mathf.Min(gx, gy) * 9f);
            return Mathf.Max(cracks, fine > 0.965f ? 1f : 0f);
        }

        if (biome == "Frozen Caverns")
        {
            var rime = 0.5f + 0.5f * Mathf.Sin((u * 14f - v * 8f + macro * 2f) * Mathf.PI);
            var frost = fine > 0.88f ? fine : 0f;
            return Mathf.Max(rime * 0.72f, frost);
        }

        if (biome == "Fracture Zones")
        {
            var cx = Mathf.Abs(Mathf.Repeat(u * 8f + macro * 0.4f, 1f) - 0.5f);
            var cy = Mathf.Abs(Mathf.Repeat(v * 8f - macro * 0.3f, 1f) - 0.5f);
            var seams = 1f - Mathf.Clamp01(Mathf.Min(cx, cy) * 13f);
            return Mathf.Max(seams, fine > 0.93f ? 1f : 0f);
        }

        if (biome == "Great Decay")
        {
            var rot = ValueNoise((int)(u * 13f), (int)(v * 13f), seed ^ 0xDEC0A11u);
            var lesions = 1f - Mathf.Abs(rot * 2f - 1f);
            return Mathf.Max(lesions * 0.74f, fine > 0.95f ? 1f : 0f);
        }

        throw new InvalidOperationException($"No Underworld creature palette exists for biome '{biome}'.");
    }

    private static float SignatureMask(
        string creatureName,
        SurfaceFamily family,
        float u,
        float v,
        float macro,
        float noise,
        uint seed)
    {
        var signatureSeed = StableHash(creatureName) ^ seed;
        var mode = (int)(signatureSeed % 6u);
        var scale = 4f + (signatureSeed % 5u);

        switch (mode)
        {
            case 0: // broad broken bands
                return 0.5f + 0.5f * Mathf.Sin((v * scale + macro * 1.8f + u * 1.7f) * Mathf.PI);
            case 1: // blotches / rosettes
            {
                var a = 0.5f + 0.5f * Mathf.Sin((u * scale + noise * 1.6f) * Mathf.PI * 2f);
                var b = 0.5f + 0.5f * Mathf.Sin((v * (scale - 1f) - macro * 1.4f) * Mathf.PI * 2f);
                return Mathf.Clamp01(a * b * 1.35f);
            }
            case 2: // long vein network
            {
                var veins = Mathf.Abs(Mathf.Sin((u * scale * 1.4f - v * 2.7f + macro * 2.2f) * Mathf.PI));
                return 1f - Mathf.Clamp01(veins * 2.4f);
            }
            case 3: // large asymmetric saddle patches
                return Mathf.Clamp01(ValueNoise((int)(u * 5f), (int)(v * 4f), signatureSeed) * 1.55f - 0.30f);
            case 4: // cross-grain fracture / rime pattern
            {
                var x = 1f - Mathf.Abs(Mathf.Sin((u * scale + macro) * Mathf.PI));
                var y = 1f - Mathf.Abs(Mathf.Sin((v * (scale + 2f) - noise) * Mathf.PI));
                return Mathf.Max(x, y) * 0.92f;
            }
            default: // mottled field with sparse high-contrast islands
            {
                var coarse = ValueNoise((int)(u * 9f), (int)(v * 9f), signatureSeed ^ 0xC0FFEEu);
                return coarse > 0.70f ? Mathf.Clamp01((coarse - 0.70f) * 3.34f) : coarse * 0.38f;
            }
        }
    }

    private static float SurfaceMask(SurfaceFamily family, float u, float v, float noise, uint seed)
    {
        switch (family)
        {
            case SurfaceFamily.Carapace:
                return 0.5f + 0.5f * Mathf.Sin((v * 16f + noise * 2f) * Mathf.PI);
            case SurfaceFamily.Scale:
                return 0.5f + 0.5f * Mathf.Sin((u * 18f + v * 9f + noise) * Mathf.PI);
            case SurfaceFamily.Stone:
                return Mathf.Clamp01(noise * 0.8f + ValueNoise((int)(u * 7f), (int)(v * 7f), seed ^ 0x51570E1u) * 0.4f);
            case SurfaceFamily.Membrane:
                return 1f - Mathf.Abs(Mathf.Sin((u * 5f - v * 7f + noise) * Mathf.PI));
            case SurfaceFamily.Decay:
                return Mathf.Clamp01(noise * 1.25f);
            default:
                return Mathf.Clamp01(0.25f + noise * 0.75f);
        }
    }

    private static SurfaceFamily SurfaceFor(UnderworldCreaturePrototypes.Entry entry)
    {
        switch (entry.Donor)
        {
            case "Tick":
            case "Seeker":
            case "SeekerBrute":
                return SurfaceFamily.Carapace;
            case "Serpent":
            case "Leech":
            case "Neck":
                return SurfaceFamily.Scale;
            case "StoneGolem":
                return SurfaceFamily.Stone;
            case "Bat":
            case "Hatchling":
            case "Wraith":
            case "FrostWisp":
                return SurfaceFamily.Membrane;
            case "Draugr":
            case "Greydwarf_Shaman":
                return SurfaceFamily.Decay;
            default:
                return SurfaceFamily.Hide;
        }
    }

    private static Palette PaletteFor(UnderworldCreaturePrototypes.Entry entry)
    {
        // Shared rigs receive deliberately different material identities where the donor would be
        // especially obvious. These are not subtle hue shifts: base value, contrast and accent
        // placement diverge while remaining inside the owning biome's art language.
        switch (entry.Name)
        {
            case "Mycelial Stalker":
                return new Palette(new Color(0.035f, 0.055f, 0.045f), new Color(0.40f, 0.52f, 0.34f), new Color(0.82f, 0.96f, 0.68f), 0.20f);
            case "Cinder Hound":
                return new Palette(new Color(0.020f, 0.018f, 0.017f), new Color(0.38f, 0.105f, 0.035f), new Color(1.00f, 0.55f, 0.08f), 0.14f);
            case "Iceblind":
                return new Palette(new Color(0.48f, 0.60f, 0.64f), new Color(0.11f, 0.22f, 0.31f), new Color(0.89f, 0.98f, 1.00f), 0.58f);
            case "Glacier Stalker":
                return new Palette(new Color(0.025f, 0.080f, 0.14f), new Color(0.30f, 0.58f, 0.76f), new Color(0.90f, 0.98f, 1.00f), 0.76f);
            case "Decay Hound":
                return new Palette(new Color(0.12f, 0.075f, 0.055f), new Color(0.34f, 0.40f, 0.12f), new Color(0.80f, 0.74f, 0.48f), 0.17f);
            case "Lantern Moth":
                return new Palette(new Color(0.045f, 0.065f, 0.060f), new Color(0.22f, 0.46f, 0.33f), new Color(0.78f, 1.00f, 0.70f), 0.36f);
            case "Rime Moth":
                return new Palette(new Color(0.035f, 0.085f, 0.12f), new Color(0.45f, 0.74f, 0.88f), new Color(0.94f, 1.00f, 1.00f), 0.78f);
            case "Furnace Golem":
                return new Palette(new Color(0.025f, 0.022f, 0.020f), new Color(0.24f, 0.095f, 0.035f), new Color(1.00f, 0.62f, 0.10f), 0.10f);
            case "Cryolith Guardian":
                return new Palette(new Color(0.06f, 0.12f, 0.17f), new Color(0.40f, 0.68f, 0.82f), new Color(0.90f, 0.99f, 1.00f), 0.74f);
            case "Stonebound":
                return new Palette(new Color(0.065f, 0.055f, 0.085f), new Color(0.30f, 0.23f, 0.40f), new Color(0.78f, 0.60f, 1.00f), 0.32f);
            case "Rift Colossus":
                return new Palette(new Color(0.025f, 0.020f, 0.038f), new Color(0.22f, 0.15f, 0.34f), new Color(0.92f, 0.70f, 1.00f), 0.40f);
        }

        var biome = entry.Biome;
        switch (biome)
        {
            case "Fungal Forest":
                return new Palette(
                    new Color(0.075f, 0.105f, 0.095f),
                    new Color(0.25f, 0.42f, 0.30f),
                    new Color(0.62f, 0.93f, 0.70f),
                    0.28f);
            case "Blackwater Deep":
                return new Palette(
                    new Color(0.025f, 0.055f, 0.075f),
                    new Color(0.10f, 0.27f, 0.34f),
                    new Color(0.38f, 0.83f, 0.90f),
                    0.68f);
            case "Sulfurous Wastes":
                return new Palette(
                    new Color(0.075f, 0.065f, 0.055f),
                    new Color(0.33f, 0.17f, 0.08f),
                    new Color(0.95f, 0.72f, 0.20f),
                    0.18f);
            case "Frozen Caverns":
                return new Palette(
                    new Color(0.10f, 0.16f, 0.20f),
                    new Color(0.46f, 0.70f, 0.82f),
                    new Color(0.82f, 0.96f, 1.00f),
                    0.72f);
            case "Fracture Zones":
                return new Palette(
                    new Color(0.095f, 0.085f, 0.12f),
                    new Color(0.31f, 0.23f, 0.43f),
                    new Color(0.72f, 0.52f, 0.96f),
                    0.42f);
            case "Great Decay":
                return new Palette(
                    new Color(0.11f, 0.10f, 0.065f),
                    new Color(0.32f, 0.34f, 0.14f),
                    new Color(0.76f, 0.73f, 0.47f),
                    0.23f);
            default:
                throw new InvalidOperationException($"No Underworld creature palette exists for biome '{biome}'.");
        }
    }

    private static uint StableHash(string text)
    {
        unchecked
        {
            var hash = 2166136261u;
            for (var i = 0; i < text.Length; i++)
            {
                hash ^= text[i];
                hash *= 16777619u;
            }
            return hash;
        }
    }

    private static float ValueNoise(int x, int y, uint seed)
    {
        unchecked
        {
            var value = (uint)x * 0x9E3779B9u ^ (uint)y * 0x85EBCA6Bu ^ seed;
            value ^= value >> 16;
            value *= 0x7FEB352Du;
            value ^= value >> 15;
            value *= 0x846CA68Bu;
            value ^= value >> 16;
            return (value & 0x00FFFFFFu) / 16777215f;
        }
    }
}
