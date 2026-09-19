using System;
using System.Collections.Generic;
using HarmonyLib;
using Magenheim.Core.Underworld;
using UnityEngine;
using UnityEngine.UI;

namespace Magenheim.Runtime;

/// <summary>
/// Thin Valheim UI boundary for the logical Underworld map. World generation, projection,
/// exploration and persistence remain owned by their existing authorities; this component only
/// presents their textures and lets the player choose which logical map is being inspected.
/// </summary>
internal sealed class UnderworldMinimapAdapter : MonoBehaviour
{
    private const string RootName = "Magenheim_UnderworldMapPresentation";
    private readonly Dictionary<Minimap.PinData, Vector3> _projectedPins = new();
    private Minimap? _minimap;
    private GameObject? _root;
    private GameObject? _selector;
    private RawImage? _biomes;
    private RawImage? _fog;
    private Button? _surfaceButton;
    private Button? _underworldButton;
    private MagenheimMapLayer _lastLayer = (MagenheimMapLayer)(-1);
    private bool _lastAvailable;

    internal static void Attach(Minimap minimap)
    {
        if (!minimap || minimap.GetComponent<UnderworldMinimapAdapter>()) return;
        var adapter = minimap.gameObject.AddComponent<UnderworldMinimapAdapter>();
        adapter._minimap = minimap;
        adapter.Build();
    }

    private void Build()
    {
        if (_minimap is null || !_minimap.m_mapImageLarge) return;
        var map = _minimap.m_mapImageLarge.rectTransform;

        _root = new GameObject(RootName, typeof(RectTransform));
        var rootRect = _root.GetComponent<RectTransform>();
        rootRect.SetParent(map, false);
        Stretch(rootRect);
        rootRect.SetAsLastSibling();

        _biomes = CreateLayer("Biomes", rootRect);
        _fog = CreateLayer("Fog", rootRect);
        _biomes.raycastTarget = false;
        _fog.raycastTarget = false;

        _selector = new GameObject("Magenheim_MapLayerSelector", typeof(RectTransform));
        var selectorRect = _selector.GetComponent<RectTransform>();
        selectorRect.SetParent(_minimap.m_largeRoot.transform, false);
        selectorRect.anchorMin = new Vector2(0.5f, 1f);
        selectorRect.anchorMax = new Vector2(0.5f, 1f);
        selectorRect.pivot = new Vector2(0.5f, 1f);
        selectorRect.anchoredPosition = new Vector2(0f, -18f);
        selectorRect.sizeDelta = new Vector2(260f, 34f);

        _surfaceButton = CreateButton("Surface", selectorRect, new Vector2(-65f, 0f), () => Select(MagenheimMapLayer.Surface));
        _underworldButton = CreateButton("Underworld", selectorRect, new Vector2(65f, 0f), () => Select(MagenheimMapLayer.Underworld));
        ApplySelection(true);
    }

    private void Update()
    {
        if (_root is null) { Build(); return; }
        ApplySelection(false);
    }

    private void Select(MagenheimMapLayer layer)
    {
        if (layer != MagenheimMapLayer.Underworld) RestoreProjectedPins();
        UnderworldMapLayerRuntime.Select(layer);
        ApplySelection(true);
    }

    private void ApplySelection(bool force)
    {
        var available = UnderworldMapLayerRuntime.IsUnderworldMapAvailable();
        if (!available && UnderworldMapLayerRuntime.SelectedLayer == MagenheimMapLayer.Underworld)
        {
            RestoreProjectedPins();
            UnderworldMapLayerRuntime.Select(MagenheimMapLayer.Surface);
        }

        if (_selector) _selector.SetActive(available);
        var layer = UnderworldMapLayerRuntime.SelectedLayer;
        if (!force && available == _lastAvailable && layer == _lastLayer)
        {
            if (layer == MagenheimMapLayer.Underworld)
            {
                BindTextures();
                ProjectUnderworldPins();
            }
            return;
        }

        _lastAvailable = available;
        _lastLayer = layer;
        var underworld = available && layer == MagenheimMapLayer.Underworld;
        _root?.SetActive(underworld);
        if (underworld)
        {
            UnderworldMapPresentationRuntime.ForceRefresh();
            BindTextures();
            ProjectUnderworldPins();
        }
        else RestoreProjectedPins();

        SetButtonState(_surfaceButton, !underworld);
        SetButtonState(_underworldButton, underworld);
    }

    /// <summary>
    /// Projects only the live presentation copy of each pin. The original world position is retained
    /// here and restored before leaving the logical layer, so vanilla save data and foreign pin
    /// providers never observe Magenheim logical coordinates as authoritative world coordinates.
    /// Pins outside the Underworld host domain are left untouched; visibility filtering can be added
    /// separately without coupling persistence to presentation.
    /// </summary>
    private void ProjectUnderworldPins()
    {
        if (_minimap is null) return;
        foreach (var pin in RuntimeGameApi.GetMapPins(_minimap))
        {
            if (pin is null || _projectedPins.ContainsKey(pin)) continue;
            var world = pin.m_pos;
            if (!UnderworldMapLayerRuntime.TryProjectWorldToSelectedMap(world.x, world.z, out var logical)) continue;
            _projectedPins.Add(pin, world);
            pin.m_pos = new Vector3((float)logical.X, world.y, (float)logical.Z);
        }
    }

    private void RestoreProjectedPins()
    {
        if (_projectedPins.Count == 0) return;
        foreach (var entry in _projectedPins)
            if (entry.Key is not null) entry.Key.m_pos = entry.Value;
        _projectedPins.Clear();
    }

    private void BindTextures()
    {
        if (_biomes) _biomes.texture = UnderworldMapPresentationRuntime.BiomeTexture;
        if (_fog) _fog.texture = UnderworldMapPresentationRuntime.FogTexture;
    }

    private static RawImage CreateLayer(string name, RectTransform parent)
    {
        var go = new GameObject("Magenheim_" + name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        Stretch(rect);
        return go.GetComponent<RawImage>();
    }

    private static Button CreateButton(string label, RectTransform parent, Vector2 position, Action action)
    {
        var go = new GameObject("Magenheim_" + label + "Tab", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(124f, 30f);
        rect.anchoredPosition = position;

        var button = go.GetComponent<Button>();
        button.onClick.AddListener(() => action());

        var textGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.SetParent(rect, false);
        Stretch(textRect);
        var text = textGo.GetComponent<Text>();
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 14;
        text.color = Color.white;
        text.raycastTarget = false;
        return button;
    }

    private static void SetButtonState(Button? button, bool selected)
    {
        if (!button) return;
        var image = button.GetComponent<Image>();
        if (image) image.color = selected ? new Color(0.36f, 0.30f, 0.20f, 0.95f) : new Color(0.10f, 0.10f, 0.10f, 0.82f);
        button.interactable = !selected;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void OnDisable() => RestoreProjectedPins();

    private void OnDestroy()
    {
        RestoreProjectedPins();
        if (_root) Destroy(_root);
        if (_selector) Destroy(_selector);
    }
}

[HarmonyPatch(typeof(Minimap), "Awake", new Type[0])]
internal static class UnderworldMinimapAwakePatch
{
    private static void Postfix(Minimap __instance) => UnderworldMinimapAdapter.Attach(__instance);
}
