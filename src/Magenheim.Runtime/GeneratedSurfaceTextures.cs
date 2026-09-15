using System;
using System.Collections.Generic;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Shared runtime surface authority for procedural Magenheim geometry. Generated models use a
/// repeatable grayscale albedo pattern here and tint it through their material color instead of
/// replacing every source texture with Unity's one-pixel white texture.
/// </summary>
internal static class GeneratedSurfaceTextures
{
    private const int TextureSize = 64;
    private static readonly Dictionary<SurfaceKind, Texture2D> Cache = new();

    internal static void Apply(Material material, string semantic)
    {
        if (material is null) throw new ArgumentNullException(nameof(material));
        material.mainTexture = ForSemantic(semantic);
        material.mainTextureScale = Vector2.one;
        material.mainTextureOffset = Vector2.zero;
    }

    internal static Texture2D ForSemantic(string semantic)
    {
        var kind = Classify(semantic);
        if (Cache.TryGetValue(kind, out var existing) && existing) return existing;

        var texture = Build(kind);
        Cache[kind] = texture;
        return texture;
    }

    private static SurfaceKind Classify(string semantic)
    {
        var key = (semantic ?? string.Empty).ToLowerInvariant();
        if (ContainsAny(key, "crystal", "frost", "rime", "spirit", "radiance", "venom", "seidr", "fate", "eitr", "gem", "shard", "growth", "focus", "core", "light"))
            return SurfaceKind.Crystal;
        if (ContainsAny(key, "stone", "marble", "rock", "earth", "strata", "slate", "basalt"))
            return SurfaceKind.Stone;
        if (ContainsAny(key, "wood", "timber", "root", "shaft", "bark"))
            return SurfaceKind.Timber;
        if (ContainsAny(key, "iron", "bronze", "silver", "gold", "metal", "band", "collar", "brace", "rail", "rim"))
            return SurfaceKind.Metal;
        if (ContainsAny(key, "cloth", "banner", "fabric"))
            return SurfaceKind.Cloth;
        if (ContainsAny(key, "bone", "ivory", "antler"))
            return SurfaceKind.Bone;
        return SurfaceKind.Generic;
    }

    private static bool ContainsAny(string value, params string[] needles)
    {
        foreach (var needle in needles)
            if (value.IndexOf(needle, StringComparison.Ordinal) >= 0)
                return true;
        return false;
    }

    private static Texture2D Build(SurfaceKind kind)
    {
        var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, mipChain: true)
        {
            name = "magenheim.surface." + kind.ToString().ToLowerInvariant(),
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
            anisoLevel = 2,
            hideFlags = HideFlags.HideAndDontSave,
        };

        var pixels = new Color[TextureSize * TextureSize];
        for (var y = 0; y < TextureSize; y++)
        for (var x = 0; x < TextureSize; x++)
        {
            var value = kind switch
            {
                SurfaceKind.Crystal => Crystal(x, y),
                SurfaceKind.Stone => Stone(x, y),
                SurfaceKind.Timber => Timber(x, y),
                SurfaceKind.Metal => Metal(x, y),
                SurfaceKind.Cloth => Cloth(x, y),
                SurfaceKind.Bone => Bone(x, y),
                _ => Generic(x, y),
            };
            pixels[y * TextureSize + x] = new Color(value, value, value, 1f);
        }

        texture.SetPixels(pixels);
        texture.Apply(updateMipmaps: true, makeNoLongerReadable: true);
        return texture;
    }

    private static float Crystal(int x, int y)
    {
        const int cell = 8;
        var cx = x / cell;
        var cy = y / cell;
        var lx = x % cell;
        var ly = y % cell;
        var seed = Hash(cx, cy, 13);
        var facet = .64f + .25f * seed;
        var diagonalA = Math.Abs(lx - ly) <= 1;
        var diagonalB = Math.Abs((cell - 1 - lx) - ly) <= 1;
        var edge = lx == 0 || ly == 0 || diagonalA || diagonalB;
        var glint = ((x * 3 + y * 5 + cx * 7) & 15) == 0 ? .08f : 0f;
        return Mathf.Clamp01(facet + (edge ? .12f : 0f) + glint);
    }

    private static float Stone(int x, int y)
    {
        var coarse = Hash(x / 5, y / 5, 29);
        var fine = Hash(x, y, 47);
        var strata = .045f * Mathf.Sin((y + 3f * Mathf.Sin(x * .17f)) * .52f);
        var vein = Math.Abs(Mathf.Sin(x * .23f + y * .11f + coarse * 4f)) > .94f ? -.11f : 0f;
        return Mathf.Clamp01(.66f + coarse * .16f + fine * .06f + strata + vein);
    }

    private static float Timber(int x, int y)
    {
        var wobble = 3.5f * Mathf.Sin(y * .12f) + 1.5f * Mathf.Sin(y * .31f);
        var grain = Mathf.Abs(Mathf.Sin((x + wobble) * .34f));
        var knotX = x - 39f;
        var knotY = y - 24f;
        var knot = Mathf.Abs(Mathf.Sin(Mathf.Sqrt(knotX * knotX + knotY * knotY) * .55f));
        var knotWeight = Mathf.Clamp01(1f - Mathf.Sqrt(knotX * knotX + knotY * knotY) / 16f);
        return Mathf.Clamp01(.58f + grain * .22f + knot * knotWeight * .14f);
    }

    private static float Metal(int x, int y)
    {
        var hammer = Hash(x / 3, y / 3, 71);
        var scratch = (x + y * 3) % 23 == 0 || (x * 5 + y) % 31 == 0 ? .13f : 0f;
        var band = .035f * Mathf.Sin(y * .78f);
        return Mathf.Clamp01(.66f + hammer * .16f + scratch + band);
    }

    private static float Cloth(int x, int y)
    {
        var warp = x % 4 == 0 ? .11f : 0f;
        var weft = y % 4 == 0 ? .08f : 0f;
        var crossing = x % 4 == 0 && y % 4 == 0 ? .06f : 0f;
        return Mathf.Clamp01(.64f + warp + weft + crossing + Hash(x / 8, y / 8, 83) * .05f);
    }

    private static float Bone(int x, int y)
    {
        var striation = .07f * Mathf.Sin(y * .48f + Mathf.Sin(x * .14f) * 2f);
        var pores = Hash(x, y, 97) > .94f ? -.13f : 0f;
        return Mathf.Clamp01(.76f + striation + pores);
    }

    private static float Generic(int x, int y) =>
        Mathf.Clamp01(.70f + Hash(x / 2, y / 2, 113) * .18f + .03f * Mathf.Sin((x + y) * .43f));

    private static float Hash(int x, int y, int salt)
    {
        unchecked
        {
            var value = x * 374761393 + y * 668265263 + salt * 1442695041;
            value = (value ^ (value >> 13)) * 1274126177;
            value ^= value >> 16;
            return (value & 0x7fffffff) / (float)int.MaxValue;
        }
    }

    private enum SurfaceKind
    {
        Crystal,
        Stone,
        Timber,
        Metal,
        Cloth,
        Bone,
        Generic,
    }
}
