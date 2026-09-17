using System;
using System.Collections.Generic;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>In-memory Hammer icons for the compact geology/crystal decor set.</summary>
internal static class GeologyDecorIcons
{
    private const int Size = 256;
    private const int DesignSize = 128;
    private static readonly Dictionary<string, Sprite> Cache = new();

    internal static Sprite Icon(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Decor model id is required.", nameof(modelId));
        if (Cache.TryGetValue(modelId, out var cached)) return cached;

        var pixels = new Color[Size * Size];
        var stone = new Color(.36f, .35f, .33f, 1f);
        var darkStone = new Color(.16f, .17f, .18f, 1f);
        var wood = new Color(.28f, .17f, .09f, 1f);
        var iron = new Color(.34f, .38f, .39f, 1f);
        var bronze = new Color(.50f, .33f, .16f, 1f);
        var accent = AccentFor(modelId);
        var bright = Color.Lerp(accent, Color.white, .35f);
        var secondary = SecondaryFor(modelId);

        DrawDiamond(pixels, 64, 64, 57, new Color(.055f, .06f, .065f, .94f));
        DrawDiamond(pixels, 64, 64, 53, new Color(.09f, .095f, .10f, .96f));

        switch (modelId)
        {
            case GeologyDecorVisuals.GeodeBowl:
                Rect(pixels, 38, 37, 90, 43, darkStone);
                Rect(pixels, 31, 44, 97, 53, stone);
                Rect(pixels, 34, 52, 94, 58, iron);
                DrawCrystal(pixels, 64, 69, 15, accent, bright);
                DrawCrystal(pixels, 48, 65, 8, accent, bright);
                DrawCrystal(pixels, 81, 64, 8, accent, bright);
                break;
            case GeologyDecorVisuals.CutGeodePlaque:
                Rect(pixels, 36, 26, 92, 103, wood);
                Rect(pixels, 33, 25, 95, 31, iron);
                Rect(pixels, 33, 98, 95, 104, iron);
                DrawDiamond(pixels, 64, 66, 26, stone);
                DrawDiamond(pixels, 64, 66, 19, darkStone);
                DrawCrystal(pixels, 64, 68, 13, accent, bright);
                break;
            case GeologyDecorVisuals.CrystalEndTable:
                Rect(pixels, 35, 63, 93, 72, stone);
                Rect(pixels, 40, 58, 88, 63, iron);
                Rect(pixels, 59, 31, 69, 59, wood);
                Rect(pixels, 46, 24, 82, 31, darkStone);
                DrawCrystal(pixels, 64, 51, 9, accent, bright);
                break;
            case GeologyDecorVisuals.GeologistStool:
                Rect(pixels, 42, 62, 86, 71, stone);
                Rect(pixels, 44, 57, 84, 62, iron);
                Rect(pixels, 47, 30, 54, 57, wood);
                Rect(pixels, 74, 30, 81, 57, wood);
                DrawCrystal(pixels, 64, 49, 8, accent, bright);
                break;
            case GeologyDecorVisuals.MineralDisplayCase:
                Rect(pixels, 27, 25, 101, 32, stone);
                Rect(pixels, 27, 96, 101, 103, stone);
                Rect(pixels, 31, 31, 37, 97, iron);
                Rect(pixels, 91, 31, 97, 97, iron);
                Rect(pixels, 34, 54, 94, 58, wood);
                Rect(pixels, 34, 77, 94, 81, wood);
                DrawCrystal(pixels, 48, 69, 8, accent, bright);
                DrawCrystal(pixels, 77, 91, 9, secondary, bright);
                DrawDiamond(pixels, 49, 91, 10, stone);
                break;
            case GeologyDecorVisuals.CrystalWallSconce:
                Rect(pixels, 50, 31, 78, 91, stone);
                Rect(pixels, 61, 35, 67, 87, iron);
                Rect(pixels, 61, 55, 68, 82, bronze);
                DrawCrystal(pixels, 64, 91, 14, accent, bright);
                break;
            case GeologyDecorVisuals.StrataMapTable:
                Rect(pixels, 22, 61, 106, 70, stone);
                Rect(pixels, 29, 29, 36, 61, wood);
                Rect(pixels, 92, 29, 99, 61, wood);
                Rect(pixels, 40, 72, 47, 94, darkStone);
                Rect(pixels, 49, 72, 56, 94, bronze);
                Rect(pixels, 58, 72, 65, 94, secondary);
                Rect(pixels, 67, 72, 74, 94, bronze);
                Rect(pixels, 76, 72, 83, 94, darkStone);
                DrawCrystal(pixels, 91, 88, 7, accent, bright);
                break;
            case GeologyDecorVisuals.SpecimenSideboard:
                Rect(pixels, 24, 28, 104, 69, wood);
                Rect(pixels, 21, 69, 107, 78, stone);
                Rect(pixels, 49, 32, 53, 65, iron);
                Rect(pixels, 75, 32, 79, 65, iron);
                DrawCrystal(pixels, 42, 92, 9, accent, bright);
                DrawCrystal(pixels, 64, 90, 8, secondary, bright);
                DrawDiamond(pixels, 88, 88, 10, stone);
                break;
            case GeologyDecorVisuals.CrystalCoatRack:
                Rect(pixels, 43, 24, 85, 31, darkStone);
                Rect(pixels, 60, 31, 68, 98, wood);
                Rect(pixels, 58, 52, 70, 57, iron);
                Rect(pixels, 37, 72, 91, 77, bronze);
                DrawCrystal(pixels, 64, 105, 9, accent, bright);
                DrawCrystal(pixels, 37, 82, 6, accent, bright);
                DrawCrystal(pixels, 91, 82, 6, accent, bright);
                break;
            case GeologyDecorVisuals.GeodeHearthMantel:
                Rect(pixels, 28, 26, 43, 83, stone);
                Rect(pixels, 85, 26, 100, 83, stone);
                Rect(pixels, 28, 79, 100, 94, bronze);
                Rect(pixels, 22, 94, 106, 101, stone);
                Rect(pixels, 37, 84, 91, 88, iron);
                DrawCrystal(pixels, 64, 91, 8, accent, bright);
                DrawCrystal(pixels, 38, 72, 7, accent, bright);
                DrawCrystal(pixels, 90, 72, 7, accent, bright);
                break;
            default:
                throw new InvalidOperationException($"Unknown Magenheim geology decor icon '{modelId}'.");
        }

        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            name = "magenheim." + modelId + ".icon.texture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };
        texture.SetPixels(pixels);
        texture.Apply(false, false);

        var sprite = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(.5f, .5f), Size);
        sprite.name = "magenheim." + modelId + ".icon";
        Cache.Add(modelId, sprite);
        return sprite;
    }

    private static Color AccentFor(string modelId) => modelId switch
    {
        GeologyDecorVisuals.GeodeBowl => new Color(.70f, .48f, .24f, 1f),
        GeologyDecorVisuals.CutGeodePlaque => new Color(.53f, .82f, .90f, 1f),
        GeologyDecorVisuals.CrystalEndTable => new Color(.66f, .50f, .84f, 1f),
        GeologyDecorVisuals.GeologistStool => new Color(.56f, .80f, .70f, 1f),
        GeologyDecorVisuals.MineralDisplayCase => new Color(.54f, .82f, .88f, 1f),
        GeologyDecorVisuals.CrystalWallSconce => new Color(.78f, .88f, .96f, 1f),
        GeologyDecorVisuals.StrataMapTable => new Color(.78f, .58f, .29f, 1f),
        GeologyDecorVisuals.SpecimenSideboard => new Color(.62f, .84f, .73f, 1f),
        GeologyDecorVisuals.CrystalCoatRack => new Color(.48f, .75f, .88f, 1f),
        GeologyDecorVisuals.GeodeHearthMantel => new Color(.86f, .52f, .22f, 1f),
        _ => new Color(.52f, .82f, .90f, 1f),
    };

    private static Color SecondaryFor(string modelId) => modelId switch
    {
        GeologyDecorVisuals.MineralDisplayCase => new Color(.76f, .47f, .25f, 1f),
        GeologyDecorVisuals.StrataMapTable => new Color(.46f, .68f, .58f, 1f),
        GeologyDecorVisuals.SpecimenSideboard => new Color(.74f, .52f, .25f, 1f),
        _ => new Color(.62f, .48f, .72f, 1f),
    };

    private static void DrawCrystal(Color[] pixels, int cx, int cy, int radius, Color body, Color highlight)
    {
        var outline = Color.Lerp(body, Color.black, .68f);
        DrawDiamond(pixels, cx, cy, radius + 2, outline);
        DrawDiamond(pixels, cx, cy, radius, body);
        DrawDiamond(pixels, cx - radius / 4, cy + radius / 4, Math.Max(2, radius / 3), highlight);
    }

    private static int Scale(int value) => Mathf.RoundToInt(value * (Size / (float)DesignSize));

    private static void Rect(Color[] pixels, int x0, int y0, int x1, int y1, Color color)
    {
        x0 = Mathf.Clamp(Scale(x0), 0, Size - 1);
        x1 = Mathf.Clamp(Scale(x1), 0, Size - 1);
        y0 = Mathf.Clamp(Scale(y0), 0, Size - 1);
        y1 = Mathf.Clamp(Scale(y1), 0, Size - 1);
        for (var y = y0; y <= y1; y++)
            for (var x = x0; x <= x1; x++)
                Blend(pixels, x, y, color);
    }

    private static void DrawDiamond(Color[] pixels, int cx, int cy, int radius, Color color)
    {
        cx = Scale(cx);
        cy = Scale(cy);
        radius = Scale(radius);
        for (var y = cy - radius; y <= cy + radius; y++)
        {
            var dy = Math.Abs(y - cy);
            var half = radius - dy;
            for (var x = cx - half; x <= cx + half; x++)
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
