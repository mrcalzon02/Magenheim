using UnityEngine;

namespace Magenheim.Runtime;

internal static class CrystalSentinelIcons
{
    private const int Size = 96;
    private static Sprite? _cached;

    internal static Sprite Icon()
    {
        if (_cached is not null) return _cached;

        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            name = "magenheim.crystal-sentinel.icon",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };
        var pixels = new Color[Size * Size];
        var stone = new Color(.10f, .12f, .14f, 1f);
        var iron = new Color(.27f, .31f, .34f, 1f);
        var crystal = new Color(.58f, .86f, .98f, 1f);
        var core = new Color(.90f, .98f, 1f, 1f);

        Ellipse(pixels, 48, 21, 30, 9, stone);
        Ellipse(pixels, 48, 29, 24, 5, iron);
        Crystal(pixels, 48, 53, 17, 35, crystal);
        Crystal(pixels, 48, 55, 7, 29, core);
        Rect(pixels, 30, 50, 66, 55, iron);
        Rect(pixels, 34, 38, 62, 43, iron);
        Rect(pixels, 34, 65, 62, 70, iron);

        // Forward emitter points down-right, making facing readable even at hammer-menu scale.
        Rect(pixels, 57, 50, 77, 55, iron);
        Rect(pixels, 66, 45, 72, 60, iron);
        Crystal(pixels, 78, 52, 6, 12, core);

        Outline(pixels);
        texture.SetPixels(pixels);
        texture.Apply(false, false);
        var sprite = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(.5f, .5f), 96f);
        sprite.name = texture.name;
        _cached = sprite;
        return sprite;
    }

    private static void Rect(Color[] pixels, int x0, int y0, int x1, int y1, Color color)
    {
        for (var y = Mathf.Max(0, y0); y < Mathf.Min(Size, y1); y++)
        for (var x = Mathf.Max(0, x0); x < Mathf.Min(Size, x1); x++)
            pixels[y * Size + x] = color;
    }

    private static void Ellipse(Color[] pixels, int cx, int cy, int rx, int ry, Color color)
    {
        for (var y = -ry; y <= ry; y++)
        for (var x = -rx; x <= rx; x++)
        {
            if ((x * x) / (float)(rx * rx) + (y * y) / (float)(ry * ry) > 1f) continue;
            var px = cx + x;
            var py = cy + y;
            if (px >= 0 && px < Size && py >= 0 && py < Size) pixels[py * Size + px] = color;
        }
    }

    private static void Crystal(Color[] pixels, int cx, int cy, int halfWidth, int height, Color color)
    {
        var bottom = cy - height / 2;
        for (var y = 0; y < height; y++)
        {
            var normalized = y / (float)Mathf.Max(1, height - 1);
            var widthFactor = normalized < .62f ? Mathf.Lerp(.58f, 1f, normalized / .62f) : Mathf.Lerp(1f, 0f, (normalized - .62f) / .38f);
            var width = Mathf.Max(1, Mathf.RoundToInt(halfWidth * widthFactor));
            for (var x = -width; x <= width; x++)
            {
                var px = cx + x;
                var py = bottom + y;
                if (px >= 0 && px < Size && py >= 0 && py < Size) pixels[py * Size + px] = color;
            }
        }
    }

    private static void Outline(Color[] pixels)
    {
        var source = (Color[])pixels.Clone();
        var outline = new Color(.02f, .03f, .04f, 1f);
        for (var y = 1; y < Size - 1; y++)
        for (var x = 1; x < Size - 1; x++)
        {
            if (source[y * Size + x].a > .1f) continue;
            if (source[y * Size + x - 1].a > .1f || source[y * Size + x + 1].a > .1f ||
                source[(y - 1) * Size + x].a > .1f || source[(y + 1) * Size + x].a > .1f)
                pixels[y * Size + x] = outline;
        }
    }
}
