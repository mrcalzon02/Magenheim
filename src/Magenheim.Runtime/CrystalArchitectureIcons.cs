using System;
using System.Collections.Generic;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>In-memory Hammer icons for the rainbow crystal architecture set.</summary>
internal static class CrystalArchitectureIcons
{
    private const int Size = 256;
    private const int DesignSize = 128;
    private const int Scale = Size / DesignSize;
    private static readonly Dictionary<string, Sprite> Cache = new();

    internal static Sprite Icon(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Architecture model id is required.", nameof(modelId));
        if (Cache.TryGetValue(modelId, out var cached)) return cached;

        var pixels = new Color[Size * Size];
        var stone = new Color(.34f, .34f, .35f, 1f);
        var dark = new Color(.11f, .12f, .13f, 1f);
        var iron = new Color(.34f, .38f, .40f, 1f);
        var rainbow = new[]
        {
            new Color(1f,.16f,.12f,1f), new Color(1f,.55f,.10f,1f), new Color(1f,.90f,.16f,1f),
            new Color(.30f,.95f,.28f,1f), new Color(.14f,.88f,1f,1f), new Color(.28f,.38f,1f,1f),
            new Color(.78f,.22f,1f,1f)
        };

        Diamond(pixels, 64, 64, 57, new Color(.055f, .06f, .065f, .94f));
        Diamond(pixels, 64, 64, 53, new Color(.09f, .095f, .10f, .96f));

        switch (modelId)
        {
            case CrystalArchitectureVisuals.CrystalHearth:
                Rect(pixels, 20, 30, 108, 44, stone);
                Rect(pixels, 25, 44, 41, 91, stone);
                Rect(pixels, 87, 44, 103, 91, stone);
                Rect(pixels, 39, 43, 89, 58, dark);
                Rect(pixels, 31, 87, 97, 94, iron);
                for (var i = 0; i < rainbow.Length; i++)
                    Diamond(pixels, 36 + i * 9, 68 + (i % 2) * 6, 8, rainbow[i]);
                break;
            case CrystalArchitectureVisuals.CrystalBeam2:
            case CrystalArchitectureVisuals.CrystalBeam4:
            case CrystalArchitectureVisuals.CrystalBeam8:
                Rect(pixels, 55, 18, 73, 109, iron);
                var segment = 84 / rainbow.Length;
                for (var i = 0; i < rainbow.Length; i++)
                    Rect(pixels, 59, 23 + i * segment, 69, 23 + (i + 1) * segment - 1, rainbow[i]);
                break;
            case CrystalArchitectureVisuals.CrystalFoundation2:
            case CrystalArchitectureVisuals.CrystalFoundation4:
            case CrystalArchitectureVisuals.CrystalFoundation8:
                Rect(pixels, 24, 31, 104, 96, iron);
                var cellW = 20;
                var cellH = 16;
                var index = 0;
                for (var x = 0; x < 4; x++)
                    for (var y = 0; y < 4; y++)
                    {
                        Rect(pixels, 27 + x * cellW, 34 + y * cellH, 43 + x * cellW, 46 + y * cellH,
                            rainbow[index++ % rainbow.Length]);
                    }
                break;
            default:
                throw new InvalidOperationException($"Unknown Magenheim crystal architecture icon '{modelId}'.");
        }

        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            name = "magenheim." + modelId + ".icon.texture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        texture.SetPixels(pixels);
        texture.Apply(false, false);
        var sprite = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(.5f, .5f), 128f);
        sprite.name = "magenheim." + modelId + ".icon";
        Cache.Add(modelId, sprite);
        return sprite;
    }

    private static void Rect(Color[] pixels, int x0, int y0, int x1, int y1, Color color)
    {
        x0 = Mathf.Clamp(x0 * Scale, 0, Size - 1);
        x1 = Mathf.Clamp(((x1 + 1) * Scale) - 1, 0, Size - 1);
        y0 = Mathf.Clamp(y0 * Scale, 0, Size - 1);
        y1 = Mathf.Clamp(((y1 + 1) * Scale) - 1, 0, Size - 1);
        for (var y = y0; y <= y1; y++)
            for (var x = x0; x <= x1; x++)
                Blend(pixels, x, y, color);
    }

    private static void Diamond(Color[] pixels, int cx, int cy, int radius, Color color)
    {
        var scaledCx = cx * Scale;
        var scaledCy = cy * Scale;
        var scaledRadius = radius * Scale;
        for (var y = scaledCy - scaledRadius; y <= scaledCy + scaledRadius; y++)
        {
            var dy = Math.Abs(y - scaledCy);
            var half = scaledRadius - dy;
            for (var x = scaledCx - half; x <= scaledCx + half; x++)
                if (x >= 0 && x < Size && y >= 0 && y < Size)
                    Blend(pixels, x, y, color);
        }
    }

    private static void Blend(Color[] pixels, int x, int y, Color color)
    {
        var index = y * Size + x;
        var existing = pixels[index];
        var alpha = color.a + existing.a * (1f - color.a);
        if (alpha <= 0f)
        {
            pixels[index] = Color.clear;
            return;
        }
        pixels[index] = new Color(
            (color.r * color.a + existing.r * existing.a * (1f - color.a)) / alpha,
            (color.g * color.a + existing.g * existing.a * (1f - color.a)) / alpha,
            (color.b * color.a + existing.b * existing.a * (1f - color.a)) / alpha,
            alpha);
    }
}
