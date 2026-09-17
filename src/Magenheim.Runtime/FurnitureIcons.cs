using System;
using System.Collections.Generic;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Generates original Hammer-menu icons for the geology/crystal furniture family.
/// Icons are produced in memory so the furniture set does not masquerade as its
/// vanilla clone source in the build menu.
/// </summary>
internal static class FurnitureIcons
{
    private const int Size = 256;
    private const int DesignSize = 128;
    private static readonly Dictionary<string, Sprite> Cache = new();

    internal static Sprite Icon(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Furniture model id is required.", nameof(modelId));
        if (Cache.TryGetValue(modelId, out var cached)) return cached;

        var pixels = new Color[Size * Size];
        var stone = new Color(.36f, .35f, .33f, 1f);
        var darkStone = new Color(.16f, .17f, .18f, 1f);
        var wood = new Color(.28f, .17f, .09f, 1f);
        var iron = new Color(.34f, .38f, .39f, 1f);
        var bronze = new Color(.50f, .33f, .16f, 1f);
        var hide = new Color(.33f, .23f, .16f, 1f);
        var accent = AccentFor(modelId);
        var bright = Color.Lerp(accent, Color.white, .35f);

        DrawDiamond(pixels, 64, 64, 57, new Color(.055f, .06f, .065f, .94f));
        DrawDiamond(pixels, 64, 64, 53, new Color(.09f, .095f, .10f, .96f));

        switch (modelId)
        {
            case FurnitureVisuals.GeodeTable:
                Rect(pixels, 24, 61, 104, 70, stone); Rect(pixels, 30, 70, 98, 75, iron);
                Rect(pixels, 31, 31, 39, 61, wood); Rect(pixels, 89, 31, 97, 61, wood);
                DrawCrystal(pixels, 64, 76, 12, accent, bright); break;
            case FurnitureVisuals.GeodeChair:
                Rect(pixels, 39, 50, 89, 61, stone); Rect(pixels, 43, 25, 50, 50, wood);
                Rect(pixels, 78, 25, 85, 50, wood); Rect(pixels, 42, 61, 49, 98, wood);
                Rect(pixels, 79, 61, 86, 98, wood); Rect(pixels, 45, 87, 83, 93, iron);
                DrawCrystal(pixels, 64, 99, 10, accent, bright); break;
            case FurnitureVisuals.CrystalBench:
                Rect(pixels, 21, 54, 107, 65, stone); Rect(pixels, 29, 29, 39, 54, wood);
                Rect(pixels, 89, 29, 99, 54, wood); Rect(pixels, 34, 37, 94, 42, wood);
                DrawCrystal(pixels, 31, 72, 9, accent, bright); DrawCrystal(pixels, 97, 72, 9, accent, bright); break;
            case FurnitureVisuals.CrystalBed:
                Rect(pixels, 26, 39, 102, 58, wood); Rect(pixels, 30, 45, 98, 63, hide);
                Rect(pixels, 27, 58, 101, 66, iron); Rect(pixels, 76, 65, 101, 97, stone);
                Rect(pixels, 82, 70, 95, 94, darkStone); DrawCrystal(pixels, 82, 101, 8, accent, bright);
                DrawCrystal(pixels, 96, 101, 8, accent, bright); break;
            case FurnitureVisuals.MineralShelf:
                Rect(pixels, 30, 24, 38, 101, wood); Rect(pixels, 90, 24, 98, 101, wood);
                Rect(pixels, 27, 34, 101, 40, stone); Rect(pixels, 27, 59, 101, 65, stone);
                Rect(pixels, 27, 84, 101, 90, stone); DrawCrystal(pixels, 48, 49, 8, accent, bright);
                DrawCrystal(pixels, 77, 75, 9, accent, bright); DrawCrystal(pixels, 57, 99, 7, accent, bright); break;
            case FurnitureVisuals.LapidaryCabinet:
                Rect(pixels, 28, 28, 100, 87, wood); Rect(pixels, 25, 87, 103, 96, stone);
                Rect(pixels, 62, 33, 66, 84, iron); Rect(pixels, 34, 37, 38, 80, iron);
                Rect(pixels, 90, 37, 94, 80, iron); Rect(pixels, 55, 58, 60, 64, bronze);
                Rect(pixels, 68, 58, 73, 64, bronze); DrawCrystal(pixels, 64, 104, 9, accent, bright); break;
            case FurnitureVisuals.GeoDesk:
                Rect(pixels, 22, 60, 106, 69, stone); Rect(pixels, 28, 29, 36, 60, wood);
                Rect(pixels, 92, 29, 100, 60, wood); Rect(pixels, 73, 45, 101, 58, wood);
                Rect(pixels, 75, 48, 99, 51, iron); Rect(pixels, 85, 52, 89, 56, bronze);
                DrawCrystal(pixels, 47, 78, 9, accent, bright); break;
            case FurnitureVisuals.GeodePedestal:
                Rect(pixels, 44, 25, 84, 34, darkStone); Rect(pixels, 49, 34, 79, 72, stone);
                Rect(pixels, 42, 72, 86, 79, iron); DrawDiamond(pixels, 64, 91, 18, accent);
                DrawCrystal(pixels, 64, 98, 10, accent, bright); break;
            case FurnitureVisuals.CrystalDivider:
                Rect(pixels, 24, 22, 31, 104, wood); Rect(pixels, 49, 22, 56, 104, wood);
                Rect(pixels, 72, 22, 79, 104, wood); Rect(pixels, 97, 22, 104, 104, wood);
                Rect(pixels, 21, 22, 107, 29, stone); Rect(pixels, 21, 99, 107, 106, iron);
                DrawCrystal(pixels, 40, 67, 9, accent, bright); DrawCrystal(pixels, 64, 58, 11, accent, bright);
                DrawCrystal(pixels, 88, 67, 9, accent, bright); break;
            case FurnitureVisuals.CrystalThrone:
                Rect(pixels, 37, 44, 91, 60, stone); Rect(pixels, 43, 60, 85, 101, darkStone);
                Rect(pixels, 31, 53, 39, 92, wood); Rect(pixels, 89, 53, 97, 92, wood);
                Rect(pixels, 43, 78, 85, 83, bronze); DrawCrystal(pixels, 64, 83, 13, accent, bright);
                DrawCrystal(pixels, 34, 102, 9, accent, bright); DrawCrystal(pixels, 94, 102, 9, accent, bright); break;
            default: throw new InvalidOperationException($"Unknown Magenheim furniture icon '{modelId}'.");
        }

        AddMaterialReadability(pixels, modelId);
        AddSurfaceGrain(pixels, modelId);

        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            name = "magenheim." + modelId + ".icon.texture", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp,
        };
        texture.SetPixels(pixels); texture.Apply(false, false);
        var sprite = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(.5f, .5f), Size);
        sprite.name = "magenheim." + modelId + ".icon"; Cache.Add(modelId, sprite); return sprite;
    }

    private static Color AccentFor(string modelId) => modelId switch
    {
        FurnitureVisuals.GeodeTable => new Color(.72f, .48f, .20f, 1f), FurnitureVisuals.GeodeChair => new Color(.44f, .76f, .88f, 1f),
        FurnitureVisuals.CrystalBench => new Color(.50f, .82f, .90f, 1f), FurnitureVisuals.CrystalBed => new Color(.54f, .72f, .92f, 1f),
        FurnitureVisuals.MineralShelf => new Color(.62f, .86f, .72f, 1f), FurnitureVisuals.LapidaryCabinet => new Color(.74f, .55f, .25f, 1f),
        FurnitureVisuals.GeoDesk => new Color(.64f, .46f, .80f, 1f), FurnitureVisuals.GeodePedestal => new Color(.78f, .52f, .23f, 1f),
        FurnitureVisuals.CrystalDivider => new Color(.48f, .82f, .88f, 1f), FurnitureVisuals.CrystalThrone => new Color(.82f, .68f, .31f, 1f),
        _ => new Color(.50f, .82f, .92f, 1f),
    };

    private static void AddMaterialReadability(Color[] pixels, string modelId)
    {
        var source = (Color[])pixels.Clone();
        var light = modelId.IndexOf("crystal", StringComparison.OrdinalIgnoreCase) >= 0 ? new Color(.72f, .84f, .88f, .18f) : new Color(.78f, .72f, .58f, .15f);
        var shadow = new Color(.015f, .018f, .022f, .32f); var radius = Math.Max(1, Size / DesignSize);
        for (var y = radius; y < Size - radius; y++) for (var x = radius; x < Size - radius; x++)
        {
            var index = y * Size + x; if (source[index].a < .35f) continue;
            var upperOpen = source[(y + radius) * Size + x].a < .20f; var leftOpen = source[y * Size + x - radius].a < .20f;
            var lowerOpen = source[(y - radius) * Size + x].a < .20f; var rightOpen = source[y * Size + x + radius].a < .20f;
            if (upperOpen || leftOpen) Blend(pixels, x, y, light); if (lowerOpen || rightOpen) Blend(pixels, x, y, shadow);
        }
    }

    // Fine deterministic grain keeps broad flat icon masses from reading as placeholder vector blocks.
    // Amplitude stays deliberately low so it survives downsampling without becoming the UV-island
    // patchwork that previously damaged the crystal buildable textures.
    private static void AddSurfaceGrain(Color[] pixels, string modelId)
    {
        var seed = StableHash(modelId);
        for (var y = 0; y < Size; y++) for (var x = 0; x < Size; x++)
        {
            var index = y * Size + x; var c = pixels[index]; if (c.a < .35f) continue;
            var n = Hash01(x / 2, y / 2, seed) - .5f;
            var directional = .5f * Mathf.Sin((x + y * .37f + seed % 31) * .11f);
            var delta = n * .055f + directional * .018f;
            pixels[index] = new Color(Mathf.Clamp01(c.r + delta), Mathf.Clamp01(c.g + delta), Mathf.Clamp01(c.b + delta), c.a);
        }
    }

    private static int StableHash(string value)
    {
        unchecked { var hash = 17; foreach (var c in value) hash = hash * 31 + c; return hash; }
    }

    private static float Hash01(int x, int y, int seed)
    {
        unchecked { var n = x * 374761393 + y * 668265263 + seed * 69069; n = (n ^ (n >> 13)) * 1274126177; return ((n ^ (n >> 16)) & 0x7fffffff) / 2147483647f; }
    }

    private static void DrawCrystal(Color[] pixels, int cx, int cy, int radius, Color body, Color highlight)
    { DrawDiamond(pixels, cx, cy, radius, body); DrawDiamond(pixels, cx - radius / 4, cy + radius / 4, Math.Max(2, radius / 3), highlight); }

    private static void Rect(Color[] pixels, int x0, int y0, int x1, int y1, Color color)
    {
        x0 = Mathf.Clamp(x0 * Size / DesignSize, 0, Size - 1); x1 = Mathf.Clamp(x1 * Size / DesignSize, 0, Size - 1);
        y0 = Mathf.Clamp(y0 * Size / DesignSize, 0, Size - 1); y1 = Mathf.Clamp(y1 * Size / DesignSize, 0, Size - 1);
        for (var y = y0; y <= y1; y++) for (var x = x0; x <= x1; x++) Blend(pixels, x, y, color);
    }

    private static void DrawDiamond(Color[] pixels, int cx, int cy, int radius, Color color)
    {
        cx = cx * Size / DesignSize; cy = cy * Size / DesignSize; radius = Math.Max(1, radius * Size / DesignSize);
        for (var y = cy - radius; y <= cy + radius; y++) { var dy = Math.Abs(y - cy); var half = radius - dy;
            for (var x = cx - half; x <= cx + half; x++) if (x >= 0 && x < Size && y >= 0 && y < Size) Blend(pixels, x, y, color); }
    }

    private static void Blend(Color[] pixels, int x, int y, Color color)
    {
        var index = y * Size + x; var existing = pixels[index]; var alpha = color.a + existing.a * (1f - color.a);
        if (alpha <= 0f) { pixels[index] = Color.clear; return; }
        pixels[index] = new Color((color.r * color.a + existing.r * existing.a * (1f - color.a)) / alpha,
            (color.g * color.a + existing.g * existing.a * (1f - color.a)) / alpha,
            (color.b * color.a + existing.b * existing.a * (1f - color.a)) / alpha, alpha);
    }
}
