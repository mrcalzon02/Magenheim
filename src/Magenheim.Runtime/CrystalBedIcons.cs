using System.Collections.Generic;
using Magenheim.Core;
using UnityEngine;

namespace Magenheim.Runtime;

internal static class CrystalBedIcons
{
    private const int Size = 256;
    private const int DesignSize = 96;
    private const float Scale = Size / (float)DesignSize;
    private static readonly Dictionary<ElementalAlignment, Sprite> Cache = new();

    internal static Sprite Icon(ElementalAlignment element)
    {
        if (Cache.TryGetValue(element, out var existing)) return existing;

        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            name = "magenheim.crystal-bed." + element.ToString().ToLowerInvariant() + ".icon"
        };
        var pixels = new Color[Size * Size];
        for (var i = 0; i < pixels.Length; i++) pixels[i] = new Color(0f, 0f, 0f, 0f);

        var tint = ElementVisualPalette.Tint(element);
        var stone = new Color(.10f, .12f, .14f, 1f);
        var iron = new Color(.32f, .35f, .38f, 1f);
        var water = new Color(tint.r * .70f + .12f, tint.g * .70f + .12f, tint.b * .70f + .12f, 1f);
        var bright = new Color(Mathf.Min(1f, tint.r + .20f), Mathf.Min(1f, tint.g + .20f), Mathf.Min(1f, tint.b + .20f), 1f);

        FillRect(pixels, 16, 22, 80, 58, stone);
        FillRect(pixels, 21, 29, 75, 54, water);
        FillRect(pixels, 14, 55, 82, 61, iron);
        FillRect(pixels, 14, 19, 20, 59, iron);
        FillRect(pixels, 76, 19, 82, 59, iron);
        DrawCrystal(pixels, 34, 46, 7, 18, bright);
        DrawCrystal(pixels, 50, 50, 8, 24, bright);
        DrawCrystal(pixels, 64, 44, 6, 16, bright);
        Outline(pixels);

        texture.SetPixels(pixels);
        texture.Apply(false, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        var sprite = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(.5f, .5f), Size);
        sprite.name = texture.name;
        Cache.Add(element, sprite);
        return sprite;
    }

    private static int S(int value) => Mathf.RoundToInt(value * Scale);

    private static void FillRect(Color[] pixels, int x0, int y0, int x1, int y1, Color color)
    {
        x0 = S(x0); y0 = S(y0); x1 = S(x1); y1 = S(y1);
        for (var y = Mathf.Max(0, y0); y < Mathf.Min(Size, y1); y++)
            for (var x = Mathf.Max(0, x0); x < Mathf.Min(Size, x1); x++)
                pixels[y * Size + x] = color;
    }

    private static void DrawCrystal(Color[] pixels, int cx, int baseY, int halfWidth, int height, Color color)
    {
        cx = S(cx); baseY = S(baseY); halfWidth = S(halfWidth); height = S(height);
        for (var y = 0; y < height; y++)
        {
            var t = y / (float)height;
            var width = Mathf.Max(1, Mathf.RoundToInt(halfWidth * (1f - t)));
            for (var x = -width; x <= width; x++)
            {
                var px = cx + x;
                var py = baseY + y;
                if (px >= 0 && px < Size && py >= 0 && py < Size)
                    pixels[py * Size + px] = color;
            }
        }
    }

    private static void Outline(Color[] pixels)
    {
        var source = (Color[])pixels.Clone();
        var outline = new Color(.035f, .04f, .05f, 1f);
        var radius = Mathf.Max(1, S(1));
        for (var y = radius; y < Size - radius; y++)
        for (var x = radius; x < Size - radius; x++)
        {
            if (source[y * Size + x].a > .1f) continue;
            var adjacent = source[y * Size + x - radius].a > .1f || source[y * Size + x + radius].a > .1f ||
                           source[(y - radius) * Size + x].a > .1f || source[(y + radius) * Size + x].a > .1f;
            if (adjacent) pixels[y * Size + x] = outline;
        }
    }
}
