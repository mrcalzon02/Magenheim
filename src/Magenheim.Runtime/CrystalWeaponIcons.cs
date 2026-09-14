using System;
using System.Collections.Generic;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>In-memory Hammer/inventory icons for the physical crystal weapon family.</summary>
internal static class CrystalWeaponIcons
{
    private const int Size = 128;
    private static readonly Dictionary<string, Sprite> Cache = new();

    internal static Sprite Icon(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Weapon model id is required.", nameof(modelId));
        if (Cache.TryGetValue(modelId, out var cached)) return cached;

        var pixels = new Color[Size * Size];
        var bg = new Color(.055f, .06f, .07f, .96f);
        var metal = new Color(.20f, .24f, .27f, 1f);
        var grip = new Color(.20f, .12f, .07f, 1f);
        var crystal = new Color(.54f, .86f, .95f, 1f);
        var bright = new Color(.88f, .98f, 1f, 1f);
        var prism = new[]
        {
            new Color(1f,.22f,.14f,1f), new Color(1f,.62f,.12f,1f), new Color(1f,.90f,.18f,1f),
            new Color(.30f,.95f,.32f,1f), new Color(.14f,.88f,1f,1f), new Color(.32f,.42f,1f,1f), new Color(.78f,.22f,1f,1f)
        };

        DrawDiamond(pixels, 64, 64, 58, bg);
        DrawDiamond(pixels, 64, 64, 53, new Color(.09f, .10f, .11f, .98f));

        switch (modelId)
        {
            case CrystalWeaponVisuals.Sword:
                Rect(pixels, 60, 18, 68, 52, grip);
                Rect(pixels, 42, 49, 86, 56, metal);
                Blade(pixels, 64, 78, 13, 39, crystal, bright);
                break;
            case CrystalWeaponVisuals.Greatsword:
                Rect(pixels, 59, 13, 69, 46, grip);
                Rect(pixels, 33, 44, 95, 52, metal);
                Blade(pixels, 64, 82, 17, 47, crystal, bright);
                Rect(pixels, 62, 54, 66, 105, metal);
                break;
            case CrystalWeaponVisuals.Axe:
                Rect(pixels, 61, 18, 67, 94, grip);
                Rect(pixels, 58, 73, 72, 80, metal);
                DrawDiamond(pixels, 83, 80, 22, crystal);
                DrawDiamond(pixels, 93, 82, 11, bright);
                break;
            case CrystalWeaponVisuals.Battleaxe:
                Rect(pixels, 61, 13, 67, 96, grip);
                Rect(pixels, 43, 74, 85, 82, metal);
                DrawDiamond(pixels, 39, 81, 22, crystal);
                DrawDiamond(pixels, 89, 81, 22, crystal);
                DrawDiamond(pixels, 27, 82, 9, bright);
                DrawDiamond(pixels, 101, 82, 9, bright);
                break;
            case CrystalWeaponVisuals.Mace:
                Rect(pixels, 61, 17, 67, 79, grip);
                Rect(pixels, 58, 73, 70, 83, metal);
                DrawDiamond(pixels, 64, 91, 20, crystal);
                for (var i = 0; i < 6; i++)
                {
                    var a = Mathf.PI * 2f * i / 6f;
                    DrawDiamond(pixels, 64 + Mathf.RoundToInt(22f * Mathf.Cos(a)), 91 + Mathf.RoundToInt(22f * Mathf.Sin(a)), 6, i % 2 == 0 ? bright : prism[i]);
                }
                break;
            case CrystalWeaponVisuals.Spear:
                Rect(pixels, 61, 13, 67, 83, grip);
                Rect(pixels, 58, 76, 70, 84, metal);
                Blade(pixels, 64, 101, 11, 23, crystal, bright);
                break;
            case CrystalWeaponVisuals.Knife:
                Rect(pixels, 58, 22, 68, 54, grip);
                Rect(pixels, 47, 51, 79, 58, metal);
                Blade(pixels, 67, 78, 13, 28, crystal, bright);
                break;
            case CrystalWeaponVisuals.Atgeir:
                Rect(pixels, 61, 11, 67, 81, grip);
                Rect(pixels, 43, 75, 85, 82, metal);
                Blade(pixels, 64, 101, 12, 28, crystal, bright);
                DrawDiamond(pixels, 43, 86, 10, crystal);
                DrawDiamond(pixels, 85, 86, 10, crystal);
                break;
            case CrystalWeaponVisuals.Bow:
                Rect(pixels, 60, 48, 68, 80, grip);
                Segment(pixels, 58, 51, 43, 78, 7, crystal);
                Segment(pixels, 43, 78, 35, 105, 7, prism[4]);
                Segment(pixels, 70, 51, 85, 78, 7, crystal);
                Segment(pixels, 85, 78, 93, 105, 7, prism[6]);
                Segment(pixels, 58, 77, 43, 50, 2, metal);
                Segment(pixels, 70, 77, 85, 50, 2, metal);
                break;
            case CrystalWeaponVisuals.Crossbow:
                Rect(pixels, 59, 19, 69, 89, grip);
                Rect(pixels, 28, 73, 100, 82, metal);
                Segment(pixels, 30, 77, 52, 86, 8, crystal);
                Segment(pixels, 98, 77, 76, 86, 8, crystal);
                Rect(pixels, 62, 44, 66, 77, prism[5]);
                break;
            default:
                throw new InvalidOperationException($"Unknown crystal weapon icon '{modelId}'.");
        }

        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            name = "magenheim." + modelId + ".icon.texture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };
        texture.SetPixels(pixels);
        texture.Apply(false, false);

        var sprite = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(.5f, .5f), 128f);
        sprite.name = "magenheim." + modelId + ".icon";
        Cache.Add(modelId, sprite);
        return sprite;
    }

    private static void Blade(Color[] pixels, int cx, int cy, int halfWidth, int height, Color body, Color highlight)
    {
        for (var y = 0; y < height; y++)
        {
            var t = y / (float)Math.Max(1, height - 1);
            var width = Mathf.Max(1, Mathf.RoundToInt(halfWidth * (1f - .72f * t)));
            Rect(pixels, cx - width, cy - height / 2 + y, cx + width, cy - height / 2 + y + 1, body);
        }
        Segment(pixels, cx - halfWidth / 3, cy - height / 2 + 3, cx - 2, cy + height / 2 - 4, 2, highlight);
    }

    private static void Segment(Color[] pixels, int x0, int y0, int x1, int y1, int thickness, Color color)
    {
        var dx = x1 - x0;
        var dy = y1 - y0;
        var steps = Math.Max(Math.Abs(dx), Math.Abs(dy));
        if (steps == 0) steps = 1;
        for (var i = 0; i <= steps; i++)
        {
            var t = i / (float)steps;
            var x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
            var y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
            Rect(pixels, x - thickness / 2, y - thickness / 2, x + thickness / 2, y + thickness / 2, color);
        }
    }

    private static void Rect(Color[] pixels, int x0, int y0, int x1, int y1, Color color)
    {
        x0 = Mathf.Clamp(x0, 0, Size - 1); x1 = Mathf.Clamp(x1, 0, Size - 1);
        y0 = Mathf.Clamp(y0, 0, Size - 1); y1 = Mathf.Clamp(y1, 0, Size - 1);
        for (var y = y0; y <= y1; y++)
            for (var x = x0; x <= x1; x++)
                Blend(pixels, x, y, color);
    }

    private static void DrawDiamond(Color[] pixels, int cx, int cy, int radius, Color color)
    {
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
        if (alpha <= 0f) { pixels[index] = Color.clear; return; }
        pixels[index] = new Color(
            (color.r * color.a + existing.r * existing.a * (1f - color.a)) / alpha,
            (color.g * color.a + existing.g * existing.a * (1f - color.a)) / alpha,
            (color.b * color.a + existing.b * existing.a * (1f - color.a)) / alpha,
            alpha);
    }
}
