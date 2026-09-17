using System;
using Magenheim.Core;
using UnityEngine;

namespace Magenheim.Runtime;

internal static class CrystalEnchantingDaisIcons
{
    private const int Size = 256;
    private const int DesignSize = 96;
    private const float Scale = Size / (float)DesignSize;
    private static Sprite? _cached;

    internal static Sprite Icon()
    {
        if (_cached is not null) return _cached;

        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            name = "magenheim.crystal-enchanting-dais.icon"
        };
        var pixels = new Color[Size * Size];
        for (var i = 0; i < pixels.Length; i++) pixels[i] = new Color(0f, 0f, 0f, 0f);

        var darkStone = new Color(.12f, .13f, .15f, 1f);
        var faceStone = new Color(.28f, .30f, .32f, 1f);
        var iron = new Color(.27f, .31f, .35f, 1f);
        var bright = new Color(.82f, .92f, 1f, 1f);

        FillEllipse(pixels, 48, 38, 36, 18, darkStone);
        FillEllipse(pixels, 48, 42, 31, 15, faceStone);
        FillEllipse(pixels, 48, 46, 25, 12, darkStone);
        StrokeEllipse(pixels, 48, 42, 31, 15, iron, 2);

        var elements = (ElementalAlignment[])Enum.GetValues(typeof(ElementalAlignment));
        for (var i = 0; i < elements.Length; i++)
        {
            var angle = i * Mathf.PI * 2f / elements.Length;
            var x = Mathf.RoundToInt(48f + Mathf.Sin(angle) * 24f);
            var y = Mathf.RoundToInt(45f + Mathf.Cos(angle) * 10f);
            var tint = ElementVisualPalette.Tint(elements[i]);
            FillRect(pixels, x - 2, y - 4, x + 3, y + 5, new Color(
                Mathf.Min(1f, tint.r + .12f),
                Mathf.Min(1f, tint.g + .12f),
                Mathf.Min(1f, tint.b + .12f),
                1f));
        }

        DrawCrystal(pixels, 48, 48, 8, 31, bright);
        FillRect(pixels, 36, 45, 60, 49, iron);
        FillRect(pixels, 39, 57, 57, 60, iron);
        Outline(pixels);

        texture.SetPixels(pixels);
        texture.Apply(false, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        _cached = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(.5f, .5f), Size);
        _cached.name = texture.name;
        return _cached;
    }

    private static int S(int value) => Mathf.RoundToInt(value * Scale);

    private static void FillEllipse(Color[] pixels, int cx, int cy, int rx, int ry, Color color)
    {
        cx = S(cx); cy = S(cy); rx = S(rx); ry = S(ry);
        for (var y = cy - ry; y <= cy + ry; y++)
        for (var x = cx - rx; x <= cx + rx; x++)
        {
            if (x < 0 || x >= Size || y < 0 || y >= Size) continue;
            var dx = (x - cx) / (float)rx;
            var dy = (y - cy) / (float)ry;
            if (dx * dx + dy * dy <= 1f)
                pixels[y * Size + x] = color;
        }
    }

    private static void StrokeEllipse(Color[] pixels, int cx, int cy, int rx, int ry, Color color, int thickness)
    {
        cx = S(cx); cy = S(cy); rx = S(rx); ry = S(ry); thickness = Mathf.Max(1, S(thickness));
        for (var y = cy - ry - thickness; y <= cy + ry + thickness; y++)
        for (var x = cx - rx - thickness; x <= cx + rx + thickness; x++)
        {
            if (x < 0 || x >= Size || y < 0 || y >= Size) continue;
            var outerX = (x - cx) / (float)(rx + thickness);
            var outerY = (y - cy) / (float)(ry + thickness);
            var innerX = (x - cx) / (float)Mathf.Max(1, rx - thickness);
            var innerY = (y - cy) / (float)Mathf.Max(1, ry - thickness);
            if (outerX * outerX + outerY * outerY <= 1f && innerX * innerX + innerY * innerY >= 1f)
                pixels[y * Size + x] = color;
        }
    }

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
        var outline = new Color(.025f, .03f, .04f, 1f);
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
