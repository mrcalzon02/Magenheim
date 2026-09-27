using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.UI;

namespace Magenheim.Runtime;

internal static class UnderworldLoadingScreenRuntime
{
    private static readonly List<Sprite> Slides = new();
    private static GameObject? _root;
    private static Image? _image;
    private static Text? _caption;
    private static int _index;
    private static float _nextRotation;

    internal static void Show(ManualLogSource log, string caption)
    {
        if (ZNet.instance is not null && ZNet.instance.IsDedicated()) return;
        Ensure(log);
        if (_root is null) return;
        _root.SetActive(true);
        if (_caption is not null) _caption.text = caption;
        ApplySlide();
        _nextRotation = Time.unscaledTime + 8f;
    }

    internal static void Tick()
    {
        if (_root is null || !_root.activeSelf || Slides.Count < 2 || Time.unscaledTime < _nextRotation) return;
        _index = (_index + 1) % Slides.Count;
        _nextRotation = Time.unscaledTime + 8f;
        ApplySlide();
    }

    internal static void Hide()
    {
        if (_root is not null) _root.SetActive(false);
    }

    internal static void Reset()
    {
        if (_root is not null) UnityEngine.Object.Destroy(_root);
        _root = null; _image = null; _caption = null; _index = 0;
        foreach (var sprite in Slides)
        {
            if (sprite is null) continue;
            var texture = sprite.texture;
            UnityEngine.Object.Destroy(sprite);
            if (texture is not null) UnityEngine.Object.Destroy(texture);
        }
        Slides.Clear();
    }

    private static void Ensure(ManualLogSource log)
    {
        if (_root is not null) return;
        _root = new GameObject("Magenheim_UnderworldLoadingScreen");
        UnityEngine.Object.DontDestroyOnLoad(_root);
        var canvas = _root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        _root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        var art = new GameObject("Artwork");
        art.transform.SetParent(_root.transform, false);
        _image = art.AddComponent<Image>();
        Stretch(_image.rectTransform);
        _image.color = Color.black;

        var label = new GameObject("Caption");
        label.transform.SetParent(_root.transform, false);
        _caption = label.AddComponent<Text>();
        _caption.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        _caption.fontSize = 24;
        _caption.alignment = TextAnchor.LowerCenter;
        _caption.color = Color.white;
        var rect = _caption.rectTransform;
        rect.anchorMin = new Vector2(.08f, .04f); rect.anchorMax = new Vector2(.92f, .16f);
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;

        var plugin = Path.GetDirectoryName(typeof(MagenheimPlugin).Assembly.Location) ?? string.Empty;
        var directory = Path.Combine(plugin, "assets", "loading", "underworld");
        if (Directory.Exists(directory))
        {
            foreach (var path in Directory.GetFiles(directory, "*.png").OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false)) { UnityEngine.Object.Destroy(texture); continue; }
                    texture.wrapMode = TextureWrapMode.Clamp;
                    Slides.Add(Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f)));
                }
                catch (Exception e) { log.LogWarning($"Underworld loading artwork '{path}' failed: {e.Message}"); }
            }
        }
        log.LogInfo($"Underworld loading presenter found {Slides.Count} packaged splash image(s).");
    }

    private static void ApplySlide()
    {
        if (_image is null) return;
        if (Slides.Count == 0) { _image.sprite = null; _image.color = Color.black; return; }
        _image.sprite = Slides[_index % Slides.Count]; _image.color = Color.white;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
    }
}
