using System;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Runtime presentation authority for the logical Underworld map. This class intentionally owns
/// only Magenheim-created textures; it never mutates or aliases vanilla Surface exploration data.
/// The later Minimap adapter can therefore swap presentation without turning the ~40 km host
/// region into player-facing geography.
/// </summary>
internal static class UnderworldMapPresentationRuntime
{
    private static Texture2D? _fogTexture;
    private static int _lastExploredCount = -1;

    internal static Texture2D? FogTexture => _fogTexture;

    internal static bool RefreshFogTexture()
    {
        if (!UnderworldMapLayerRuntime.TryGetUnderworldExploration(out var exploration)) return false;
        if (_fogTexture is null || _fogTexture.width != exploration.Width || _fogTexture.height != exploration.Height)
        {
            DestroyTexture();
            _fogTexture = new Texture2D(exploration.Width, exploration.Height, TextureFormat.RGBA32, false, true)
            {
                name = "Magenheim_UnderworldFog",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            _lastExploredCount = -1;
        }

        // Exploration only ever reveals cells during a session, so the count is a cheap dirty key.
        // Restoring another player/world creates a new state and normally changes the count; Select
        // callers may ForceRefresh after an identity transition if equal counts happen to coincide.
        if (_lastExploredCount == exploration.ExploredCount) return false;
        WriteFog(exploration);
        return true;
    }

    internal static bool ForceRefresh()
    {
        _lastExploredCount = -1;
        return RefreshFogTexture();
    }

    private static void WriteFog(UnderworldExplorationState exploration)
    {
        var pixels = new Color32[checked(exploration.Width * exploration.Height)];
        // Match minimap fog semantics rather than exposing host coordinates: unexplored logical
        // cells are opaque, explored logical cells transparent.
        var explored = new Color32(0, 0, 0, 0);
        var clouded = new Color32(0, 0, 0, 255);
        for (var y = 0; y < exploration.Height; y++)
        for (var x = 0; x < exploration.Width; x++)
            pixels[y * exploration.Width + x] = exploration.IsExplored(x, y) ? explored : clouded;
        _fogTexture!.SetPixels32(pixels);
        _fogTexture.Apply(false, false);
        _lastExploredCount = exploration.ExploredCount;
    }

    internal static void Reset()
    {
        DestroyTexture();
        _lastExploredCount = -1;
    }

    private static void DestroyTexture()
    {
        if (_fogTexture is null) return;
        UnityEngine.Object.Destroy(_fogTexture);
        _fogTexture = null;
    }
}
