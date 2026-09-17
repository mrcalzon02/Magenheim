using UnityEngine;

namespace Magenheim.Runtime;

internal static class CrystallineIceBoxIcons
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
            name = "magenheim.crystalline-ice-box.icon"
        };
        var pixels = new Color[Size * Size];
        for (var i = 0; i < pixels.Length; i++) pixels[i] = new Color(0f, 0f, 0f, 0f);

        var stone = new Color(.075f, .095f, .12f, 1f);
        var iron = new Color(.30f, .35f, .40f, 1f);
        var silver = new Color(.62f, .72f, .80f, 1f);
        var ice = new Color(.48f, .84f, 1f, 1f);
        var bright = new Color(.76f, .94f, 1f, 1f);

        FillRect(pixels, 15, 20, 81, 65, stone);
        FillRect(pixels, 23, 30, 73, 57, ice);
        FillRect(pixels, 13, 61, 83, 69, stone);
        FillRect(pixels, 11, 66, 85, 72, iron);
        FillRect(pixels, 13, 17, 19, 68, iron);
        FillRect(pixels, 77, 17, 83, 68, iron);
        FillRect(pixels, 16, 56, 80, 61, silver);
        FillRect(pixels, 37, 72, 59, 76, iron);

        DrawCrystal(pixels, 25, 69, 5, 16, bright);
        DrawCrystal(pixels, 38, 70, 5, 20, bright);
        DrawCrystal(pixels, 50, 70, 6, 22, bright);
        DrawCrystal(pixels, 63, 70, 5, 18, bright);
        DrawCrystal(pixels, 74, 69, 4, 14, bright);

        Outline(pixels);
        texture.SetPixels(pixels);
        texture.Apply(false, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        var sprite = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(.5f, .5f), Size);
        sprite.name = texture.name;
        _cached = sprite;
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
        var outline = new Color(.025f, .035f, .045f, 1f);
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
