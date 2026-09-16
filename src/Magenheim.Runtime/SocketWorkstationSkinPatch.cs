using HarmonyLib;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Gives the transaction-safe socket surface a deliberate Magenheim visual language without
/// replacing its authoritative mutation path. The skin is scoped to the overlay's OnGUI call
/// and restores the caller's GUI state immediately afterwards.
/// </summary>
[HarmonyPatch(typeof(SocketWorkstationOverlay), "OnGUI")]
internal static class SocketWorkstationSkinPatch
{
    private static GUISkin? _skin;
    private static GUISkin? _previousSkin;
    private static Color _previousContentColor;
    private static Color _previousBackgroundColor;
    private static Texture2D? _panel;
    private static Texture2D? _button;
    private static Texture2D? _buttonHover;
    private static Texture2D? _buttonActive;
    private static Texture2D? _field;
    private static Texture2D? _section;

    private static void Prefix()
    {
        _previousSkin = GUI.skin;
        _previousContentColor = GUI.contentColor;
        _previousBackgroundColor = GUI.backgroundColor;
        _skin ??= BuildSkin(_previousSkin);
        GUI.skin = _skin;
        GUI.contentColor = new Color(.93f, .87f, .74f, 1f);
        GUI.backgroundColor = Color.white;
    }

    private static void Postfix()
    {
        if (_previousSkin != null) GUI.skin = _previousSkin;
        GUI.contentColor = _previousContentColor;
        GUI.backgroundColor = _previousBackgroundColor;
        _previousSkin = null;
    }

    private static GUISkin BuildSkin(GUISkin source)
    {
        _panel = MakeTexture(
            "panel", 256, 256,
            new Color(.070f, .061f, .050f, .988f),
            new Color(.31f, .235f, .135f, 1f),
            mineralVeins: true,
            brushed: false);
        _button = MakeTexture(
            "button", 128, 64,
            new Color(.155f, .118f, .068f, .99f),
            new Color(.45f, .325f, .155f, 1f),
            mineralVeins: false,
            brushed: true);
        _buttonHover = MakeTexture(
            "button-hover", 128, 64,
            new Color(.245f, .175f, .078f, 1f),
            new Color(.76f, .52f, .19f, 1f),
            mineralVeins: true,
            brushed: true);
        _buttonActive = MakeTexture(
            "button-active", 128, 64,
            new Color(.090f, .155f, .150f, 1f),
            new Color(.33f, .76f, .70f, 1f),
            mineralVeins: true,
            brushed: true);
        _field = MakeTexture(
            "field", 128, 128,
            new Color(.030f, .028f, .024f, .975f),
            new Color(.27f, .22f, .145f, 1f),
            mineralVeins: false,
            brushed: false);
        _section = MakeTexture(
            "section", 128, 64,
            new Color(.090f, .076f, .055f, .97f),
            new Color(.38f, .285f, .15f, 1f),
            mineralVeins: true,
            brushed: false);

        var skin = Object.Instantiate(source);
        skin.name = "Magenheim.SocketWorkstationSkin.HighFidelity";

        skin.window = new GUIStyle(source.window)
        {
            name = "Magenheim Socket Window",
            normal = { background = _panel, textColor = new Color(.98f, .82f, .44f, 1f) },
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperCenter,
            padding = new RectOffset(20, 20, 38, 18),
            border = new RectOffset(18, 18, 18, 18),
        };

        skin.label = new GUIStyle(source.label)
        {
            normal = { textColor = new Color(.89f, .83f, .72f, 1f) },
            fontSize = 13,
            wordWrap = true,
            richText = true,
            padding = new RectOffset(4, 4, 3, 3),
        };

        skin.button = new GUIStyle(source.button)
        {
            normal = { background = _button, textColor = new Color(.93f, .86f, .73f, 1f) },
            hover = { background = _buttonHover, textColor = new Color(1f, .94f, .68f, 1f) },
            active = { background = _buttonActive, textColor = Color.white },
            focused = { background = _buttonHover, textColor = new Color(1f, .94f, .68f, 1f) },
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(12, 10, 8, 8),
            margin = new RectOffset(2, 2, 3, 3),
            border = new RectOffset(14, 14, 14, 14),
        };

        skin.textArea = new GUIStyle(source.textArea)
        {
            normal = { background = _field, textColor = new Color(.74f, .91f, .86f, 1f) },
            focused = { background = _field, textColor = new Color(.86f, .98f, .94f, 1f) },
            fontSize = 12,
            wordWrap = true,
            padding = new RectOffset(10, 10, 8, 8),
            border = new RectOffset(12, 12, 12, 12),
        };

        skin.box = new GUIStyle(source.box)
        {
            normal = { background = _section, textColor = new Color(.90f, .84f, .71f, 1f) },
            border = new RectOffset(12, 12, 12, 12),
            padding = new RectOffset(8, 8, 8, 8),
        };

        skin.scrollView = new GUIStyle(source.scrollView)
        {
            normal = { background = _field },
            border = new RectOffset(10, 10, 10, 10),
            padding = new RectOffset(4, 4, 4, 4),
        };

        return skin;
    }

    private static Texture2D MakeTexture(
        string role,
        int width,
        int height,
        Color center,
        Color edge,
        bool mineralVeins,
        bool brushed)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false)
        {
            name = "Magenheim.SocketUI." + role,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave,
        };

        var pixels = new Color[width * height];
        var borderWidth = Mathf.Max(8f, Mathf.Min(width, height) * .13f);
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var distance = Mathf.Min(Mathf.Min(x, width - 1 - x), Mathf.Min(y, height - 1 - y));
            var border = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(distance / borderWidth));
            var color = Color.Lerp(edge, center, border);

            // Multi-scale deterministic grain removes the old flat-color/debug-panel read while
            // remaining subtle enough that labels and item names stay legible.
            var coarse = Hash01(x / 8, y / 8, 17) - .5f;
            var fine = Hash01(x, y, 31) - .5f;
            var grain = coarse * .040f + fine * .020f;
            color.r += grain;
            color.g += grain * .82f;
            color.b += grain * .55f;

            if (mineralVeins && border > .58f)
            {
                var veinSignal = Mathf.Abs(Mathf.Sin(x * .081f + y * .039f + Hash01(x / 20, y / 20, 47) * 4f));
                if (veinSignal > .965f)
                {
                    color.r += .085f;
                    color.g += .062f;
                    color.b += .025f;
                }
            }

            if (brushed && border > .45f)
            {
                var brush = .017f * Mathf.Sin(y * .62f + Hash01(x / 16, 0, 61) * 3f);
                color.r += brush;
                color.g += brush;
                color.b += brush;
            }

            // A fine inner bevel keeps stretched GUI backgrounds crisp at 1080p/1440p rather
            // than reading as enlarged 24-48px blobs.
            var innerBand = Mathf.Abs(distance - borderWidth * .62f) <= 1.5f;
            if (innerBand)
            {
                color.r += .045f;
                color.g += .034f;
                color.b += .017f;
            }

            pixels[y * width + x] = new Color(
                Mathf.Clamp01(color.r),
                Mathf.Clamp01(color.g),
                Mathf.Clamp01(color.b),
                Mathf.Clamp01(color.a));
        }

        texture.SetPixels(pixels);
        texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
        return texture;
    }

    private static float Hash01(int x, int y, int salt)
    {
        unchecked
        {
            var value = x * 374761393 + y * 668265263 + salt * 1442695041;
            value = (value ^ (value >> 13)) * 1274126177;
            value ^= value >> 16;
            return (value & 0x7fffffff) / (float)int.MaxValue;
        }
    }
}
