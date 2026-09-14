using System;
using System.Collections.Generic;
using Magenheim.Core;
using UnityEngine;

namespace Magenheim.Runtime;

internal static class CrystalBannerIcons
{
    private const int Size = 96;
    private static readonly Dictionary<string, Sprite> Cache = new();

    internal static Sprite Icon(CrystalBannerVisuals.BannerStyle style, ElementalAlignment element)
    {
        var key = style + "." + element;
        if (Cache.TryGetValue(key, out var existing)) return existing;

        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            name = "magenheim.banner." + key + ".icon"
        };
        var pixels = new Color[Size * Size];
        for (var i = 0; i < pixels.Length; i++) pixels[i] = new Color(0f, 0f, 0f, 0f);

        var tint = ElementVisualPalette.Tint(element);
        var dark = new Color(tint.r * .48f, tint.g * .48f, tint.b * .48f, 1f);
        var metal = new Color(.34f, .38f, .42f, 1f);
        var wood = new Color(.30f, .18f, .09f, 1f);
        FillRect(pixels, 18, 78, 78, 83, wood);
        FillRect(pixels, 14, 76, 20, 86, metal);
        FillRect(pixels, 76, 76, 82, 86, metal);

        switch (style)
        {
            case CrystalBannerVisuals.BannerStyle.Standard:
                FillRect(pixels, 23, 24, 73, 76, dark);
                FillRect(pixels, 28, 24, 32, 75, tint);
                FillRect(pixels, 64, 24, 68, 75, tint);
                break;
            case CrystalBannerVisuals.BannerStyle.Swallowtail:
                FillRect(pixels, 23, 39, 73, 76, dark);
                FillRect(pixels, 23, 18, 46, 42, dark);
                FillRect(pixels, 50, 18, 73, 42, dark);
                break;
            case CrystalBannerVisuals.BannerStyle.Pennant:
                FillRect(pixels, 28, 58, 68, 76, dark);
                FillRect(pixels, 32, 45, 64, 59, tint);
                FillRect(pixels, 36, 33, 60, 46, dark);
                FillRect(pixels, 40, 22, 56, 34, tint);
                FillRect(pixels, 45, 15, 51, 23, dark);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(style), style, null);
        }

        DrawDiamond(pixels, 48, 51, 10, new Color(
            Mathf.Min(1f, tint.r + .20f), Mathf.Min(1f, tint.g + .20f), Mathf.Min(1f, tint.b + .20f), 1f));
        Outline(pixels);
        texture.SetPixels(pixels);
        texture.Apply(false, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        var sprite = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(.5f, .5f), 96f);
        sprite.name = texture.name;
        Cache.Add(key, sprite);
        return sprite;
    }

    private static void FillRect(Color[] pixels, int x0, int y0, int x1, int y1, Color color)
    {
        for (var y = Mathf.Max(0, y0); y < Mathf.Min(Size, y1); y++)
            for (var x = Mathf.Max(0, x0); x < Mathf.Min(Size, x1); x++)
                pixels[y * Size + x] = color;
    }

    private static void DrawDiamond(Color[] pixels, int cx, int cy, int radius, Color color)
    {
        for (var y = -radius; y <= radius; y++)
        {
            var half = radius - Mathf.Abs(y);
            for (var x = -half; x <= half; x++)
            {
                var px = cx + x;
                var py = cy + y;
                if (px >= 0 && px < Size && py >= 0 && py < Size)
                    pixels[py * Size + px] = color;
            }
        }
    }

    private static void Outline(Color[] pixels)
    {
        var source = (Color[])pixels.Clone();
        var outline = new Color(.05f, .06f, .07f, 1f);
        for (var y = 1; y < Size - 1; y++)
        for (var x = 1; x < Size - 1; x++)
        {
            if (source[y * Size + x].a > .1f) continue;
            var adjacent = source[y * Size + x - 1].a > .1f || source[y * Size + x + 1].a > .1f ||
                           source[(y - 1) * Size + x].a > .1f || source[(y + 1) * Size + x].a > .1f;
            if (adjacent) pixels[y * Size + x] = outline;
        }
    }
}
