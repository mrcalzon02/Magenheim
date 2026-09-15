using HarmonyLib;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Gives the existing transaction-safe socket surface a deliberate Valheim/Magenheim visual
/// language without replacing its authoritative mutation path. The skin is scoped to the
/// overlay's OnGUI call and the caller's GUI state is restored immediately afterwards.
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

    private static void Prefix()
    {
        _previousSkin = GUI.skin;
        _previousContentColor = GUI.contentColor;
        _previousBackgroundColor = GUI.backgroundColor;
        _skin ??= BuildSkin(_previousSkin);
        GUI.skin = _skin;
        GUI.contentColor = new Color(.91f, .84f, .68f, 1f);
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
        _panel = MakeTexture(48, 48, new Color(.075f, .065f, .052f, .985f), new Color(.29f, .23f, .14f, 1f), true);
        _button = MakeTexture(32, 32, new Color(.16f, .125f, .075f, .98f), new Color(.43f, .32f, .16f, 1f), false);
        _buttonHover = MakeTexture(32, 32, new Color(.24f, .18f, .09f, 1f), new Color(.72f, .50f, .20f, 1f), false);
        _buttonActive = MakeTexture(32, 32, new Color(.11f, .16f, .16f, 1f), new Color(.35f, .72f, .68f, 1f), false);
        _field = MakeTexture(24, 24, new Color(.035f, .032f, .027f, .96f), new Color(.25f, .21f, .15f, 1f), false);

        var skin = Object.Instantiate(source);
        skin.name = "Magenheim.SocketWorkstationSkin";

        skin.window = new GUIStyle(source.window)
        {
            name = "Magenheim Socket Window",
            normal = { background = _panel, textColor = new Color(.95f, .80f, .43f, 1f) },
            fontSize = 19,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperCenter,
            padding = new RectOffset(18, 18, 34, 16),
            border = new RectOffset(8, 8, 8, 8),
        };

        skin.label = new GUIStyle(source.label)
        {
            normal = { textColor = new Color(.88f, .82f, .70f, 1f) },
            fontSize = 13,
            wordWrap = true,
            richText = true,
            padding = new RectOffset(3, 3, 2, 2),
        };

        skin.button = new GUIStyle(source.button)
        {
            normal = { background = _button, textColor = new Color(.91f, .84f, .68f, 1f) },
            hover = { background = _buttonHover, textColor = new Color(1f, .91f, .61f, 1f) },
            active = { background = _buttonActive, textColor = Color.white },
            focused = { background = _buttonHover, textColor = new Color(1f, .91f, .61f, 1f) },
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(11, 9, 7, 7),
            margin = new RectOffset(2, 2, 2, 2),
            border = new RectOffset(5, 5, 5, 5),
        };

        skin.textArea = new GUIStyle(source.textArea)
        {
            normal = { background = _field, textColor = new Color(.73f, .88f, .84f, 1f) },
            focused = { background = _field, textColor = new Color(.82f, .96f, .91f, 1f) },
            fontSize = 12,
            wordWrap = true,
            padding = new RectOffset(9, 9, 7, 7),
            border = new RectOffset(4, 4, 4, 4),
        };

        skin.box = new GUIStyle(source.box)
        {
            normal = { background = _field, textColor = new Color(.88f, .82f, .70f, 1f) },
            border = new RectOffset(4, 4, 4, 4),
            padding = new RectOffset(7, 7, 7, 7),
        };
        return skin;
    }

    private static Texture2D MakeTexture(int width, int height, Color center, Color edge, bool mineralNoise)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "Magenheim.SocketUI",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave,
        };
        var pixels = new Color[width * height];
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var distance = Mathf.Min(Mathf.Min(x, width - 1 - x), Mathf.Min(y, height - 1 - y));
            var border = Mathf.Clamp01(distance / 5f);
            var color = Color.Lerp(edge, center, border);
            if (mineralNoise && border > .75f)
            {
                var grain = (((x * 17 + y * 31) ^ (x * y * 3)) & 15) / 255f;
                color += new Color(grain, grain * .72f, grain * .34f, 0f);
            }
            pixels[y * width + x] = color;
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }
}
