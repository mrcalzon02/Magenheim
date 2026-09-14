using System;
using System.Collections.Generic;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Small procedural UI icons for Crystal Dust, fermentable Eitrwine base, and finished Eitrwine.</summary>
internal static class CrystalAlchemyIcons
{
    internal const string Dust = "crystal-alchemy-dust";
    internal const string Base = "crystal-alchemy-base";
    internal const string Wine = "crystal-alchemy-wine";

    private const int Size = 128;
    private static readonly Dictionary<string, Sprite> Cache = new();

    internal static Sprite Icon(string id)
    {
        if (Cache.TryGetValue(id, out var existing)) return existing;

        var pixels = new Color[Size * Size];
        DrawDiamond(pixels, 64, 64, 57, new Color(.055f, .06f, .07f, .96f));
        DrawDiamond(pixels, 64, 64, 53, new Color(.09f, .095f, .11f, .97f));

        switch (id)
        {
            case Dust:
                DrawDust(pixels);
                break;
            case Base:
                DrawBottle(pixels, fermented: false);
                break;
            case Wine:
                DrawBottle(pixels, fermented: true);
                break;
            default:
                throw new InvalidOperationException($"Unknown crystal alchemy icon '{id}'.");
        }

        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            name = "magenheim." + id + ".texture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };
        texture.SetPixels(pixels);
        texture.Apply(false, false);

        var sprite = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(.5f, .5f), 128f);
        sprite.name = "magenheim." + id + ".icon";
        Cache.Add(id, sprite);
        return sprite;
    }

    private static void DrawDust(Color[] pixels)
    {
        var colors = Rainbow();
        Ellipse(pixels, 64, 43, 30, 13, new Color(.58f, .69f, .76f, 1f));
        Ellipse(pixels, 64, 48, 25, 10, new Color(.78f, .91f, .96f, 1f));
        for (var i = 0; i < 18; i++)
        {
            var angle = i * 2f * Mathf.PI / 18f;
            var radius = 9f + (i % 4) * 4f;
            var x = 64 + Mathf.RoundToInt(Mathf.Cos(angle) * radius);
            var y = 51 + Mathf.RoundToInt(Mathf.Sin(angle) * radius * .44f);
            DrawDiamond(pixels, x, y, 2 + i % 3, colors[i % colors.Length]);
        }
        DrawDiamond(pixels, 51, 67, 5, new Color(.92f, .98f, 1f, 1f));
        DrawDiamond(pixels, 74, 72, 4, new Color(.56f, .92f, 1f, 1f));
        DrawDiamond(pixels, 84, 60, 3, new Color(.82f, .42f, 1f, 1f));
    }

    private static void DrawBottle(Color[] pixels, bool fermented)
    {
        var glass = new Color(.32f, .48f, .54f, 1f);
        var glassBright = new Color(.61f, .80f, .84f, 1f);
        var cork = new Color(.45f, .28f, .13f, 1f);
        Rect(pixels, 54, 88, 74, 101, glass);
        Rect(pixels, 58, 100, 70, 110, glass);
        Rect(pixels, 59, 108, 69, 114, cork);
        Ellipse(pixels, 64, 61, 30, 34, glass);
        Ellipse(pixels, 64, 62, 24, 29, fermented ? new Color(.56f, .20f, .82f, 1f) : new Color(.36f, .20f, .53f, 1f));
        Rect(pixels, 43, 59, 85, 65, new Color(.70f, .72f, .74f, 1f));

        var colors = Rainbow();
        for (var i = 0; i < colors.Length; i++)
        {
            var x0 = 45 + i * 6;
            Rect(pixels, x0, 42, x0 + 5, 57, colors[i]);
        }

        DrawDiamond(pixels, 55, 78, 5, glassBright);
        DrawDiamond(pixels, 72, 73, 4, fermented ? new Color(.96f, .78f, 1f, 1f) : new Color(.77f, .62f, .91f, 1f));
        if (fermented)
        {
            DrawDiamond(pixels, 47, 92, 3, new Color(.32f, .96f, 1f, 1f));
            DrawDiamond(pixels, 82, 88, 3, new Color(1f, .36f, .72f, 1f));
        }
    }

    private static Color[] Rainbow() => new[]
    {
        new Color(1f, .18f, .14f, 1f),
        new Color(1f, .54f, .10f, 1f),
        new Color(1f, .88f, .18f, 1f),
        new Color(.31f, .94f, .31f, 1f),
        new Color(.18f, .86f, 1f, 1f),
        new Color(.32f, .42f, 1f, 1f),
        new Color(.79f, .24f, 1f, 1f),
    };

    private static void Rect(Color[] pixels, int x0, int y0, int x1, int y1, Color color)
    {
        x0 = Mathf.Clamp(x0, 0, Size - 1);
        x1 = Mathf.Clamp(x1, 0, Size - 1);
        y0 = Mathf.Clamp(y0, 0, Size - 1);
        y1 = Mathf.Clamp(y1, 0, Size - 1);
        for (var y = y0; y <= y1; y++)
            for (var x = x0; x <= x1; x++)
                Blend(pixels, x, y, color);
    }

    private static void Ellipse(Color[] pixels, int cx, int cy, int rx, int ry, Color color)
    {
        for (var y = cy - ry; y <= cy + ry; y++)
        {
            for (var x = cx - rx; x <= cx + rx; x++)
            {
                var dx = (x - cx) / (float)rx;
                var dy = (y - cy) / (float)ry;
                if (dx * dx + dy * dy <= 1f && x >= 0 && x < Size && y >= 0 && y < Size)
                    Blend(pixels, x, y, color);
            }
        }
    }

    private static void DrawDiamond(Color[] pixels, int cx, int cy, int radius, Color color)
    {
        for (var y = cy - radius; y <= cy + radius; y++)
        {
            var half = radius - Math.Abs(y - cy);
            for (var x = cx - half; x <= cx + half; x++)
                if (x >= 0 && x < Size && y >= 0 && y < Size)
                    Blend(pixels, x, y, color);
        }
    }

    private static void Blend(Color[] pixels, int x, int y, Color color)
    {
        var index = y * Size + x;
        var prior = pixels[index];
        var alpha = color.a + prior.a * (1f - color.a);
        if (alpha <= 0f)
        {
            pixels[index] = Color.clear;
            return;
        }
        pixels[index] = new Color(
            (color.r * color.a + prior.r * prior.a * (1f - color.a)) / alpha,
            (color.g * color.a + prior.g * prior.a * (1f - color.a)) / alpha,
            (color.b * color.a + prior.b * prior.a * (1f - color.a)) / alpha,
            alpha);
    }
}
