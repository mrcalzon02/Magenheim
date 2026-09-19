using System;
using System.Collections.Generic;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Runtime presentation authority for the logical Underworld map. This class intentionally owns
/// only Magenheim-created textures and projected pin positions; it never mutates or aliases vanilla
/// Surface exploration data. The Minimap adapter can therefore swap presentation without turning
/// the ~40 km host region into player-facing geography.
/// </summary>
internal static class UnderworldMapPresentationRuntime
{
    private static Texture2D? _fogTexture;
    private static Texture2D? _biomeTexture;
    private static int _lastExploredCount = -1;
    private static int? _biomeSeed;
    private static readonly Dictionary<Minimap.PinData, Vector3> ProjectedPins = new();

    internal static Texture2D? FogTexture => _fogTexture;
    internal static Texture2D? BiomeTexture => _biomeTexture;

    internal static bool RefreshFogTexture()
    {
        if (!UnderworldMapLayerRuntime.TryGetUnderworldExploration(out var exploration)) return false;
        if (_fogTexture is null || _fogTexture.width != exploration.Width || _fogTexture.height != exploration.Height)
        {
            Destroy(ref _fogTexture);
            _fogTexture = NewTexture(exploration.Width, exploration.Height, "Magenheim_UnderworldFog", FilterMode.Bilinear);
            _lastExploredCount = -1;
        }
        if (_lastExploredCount == exploration.ExploredCount) return false;
        WriteFog(exploration);
        return true;
    }

    /// <summary>
    /// Materializes the pure logical biome raster as a Unity texture. Terrain supplies the raster
    /// from its canonical spatial-domain and captured surface-seed authorities, so no host coordinate,
    /// duplicate domain, or second/derived seed enters player-facing map presentation.
    /// </summary>
    internal static bool RefreshBiomeTexture()
    {
        if (!UnderworldTerrainRuntime.TryGetCapturedSeed(out var seed)) return false;
        if (!UnderworldMapLayerRuntime.TryGetUnderworldExploration(out var exploration)) return false;
        if (_biomeTexture is not null && _biomeSeed == seed && _biomeTexture.width == exploration.Width && _biomeTexture.height == exploration.Height) return false;
        if (!UnderworldTerrainRuntime.TryBuildBiomeRaster(exploration.Width, exploration.Height, out var raster)) return false;

        Destroy(ref _biomeTexture);
        _biomeTexture = NewTexture(exploration.Width, exploration.Height, "Magenheim_UnderworldBiomes", FilterMode.Bilinear);
        var pixels = new Color32[raster.Length];
        for (var i = 0; i < raster.Length; i++) pixels[i] = BiomeColor(raster[i]);
        _biomeTexture.SetPixels32(pixels);
        _biomeTexture.Apply(false, false);
        _biomeSeed = seed;
        return true;
    }

    internal static bool ForceRefresh()
    {
        _lastExploredCount = -1;
        var fog = RefreshFogTexture();
        var biome = RefreshBiomeTexture();
        return fog || biome;
    }

    /// <summary>
    /// Projects pins that physically live in the reserved host domain into logical Underworld
    /// coordinates while the Underworld tab is selected. Surface/foreign pins are hidden outside
    /// the logical map rather than deleted. Every touched pin is restored byte-for-byte to its
    /// original world position before Surface presentation resumes, so vanilla/foreign ownership
    /// remains authoritative and no save data is rewritten with projected coordinates.
    /// </summary>
    internal static void ApplySelectedLayerPins(Minimap map)
    {
        if (map == null) return;
        RestoreProjectedPins();

        if (UnderworldMapLayerRuntime.SelectedLayer != MagenheimMapLayer.Underworld) return;
        var pins = RuntimeGameApi.GetMapPins(map);
        for (var i = 0; i < pins.Count; i++)
        {
            var pin = pins[i];
            if (pin == null) continue;

            var original = pin.m_pos;
            ProjectedPins[pin] = original;
            if (UnderworldMapLayerRuntime.TryProjectWorldToSelectedMap(original.x, original.z, out var logical))
                pin.m_pos = new Vector3((float)logical.X, original.y, (float)logical.Z);
            else
                pin.m_pos = HiddenPinPosition(original.y);
        }
    }

    internal static void RestoreProjectedPins()
    {
        if (ProjectedPins.Count == 0) return;
        foreach (var entry in ProjectedPins)
        {
            if (entry.Key != null) entry.Key.m_pos = entry.Value;
        }
        ProjectedPins.Clear();
    }

    private static Vector3 HiddenPinPosition(float y) => new(float.MaxValue * 0.25f, y, float.MaxValue * 0.25f);

    private static void WriteFog(UnderworldExplorationState exploration)
    {
        var pixels = new Color32[checked(exploration.Width * exploration.Height)];
        var explored = new Color32(0, 0, 0, 0);
        var clouded = new Color32(0, 0, 0, 255);
        for (var y = 0; y < exploration.Height; y++)
        for (var x = 0; x < exploration.Width; x++) pixels[y * exploration.Width + x] = exploration.IsExplored(x, y) ? explored : clouded;
        _fogTexture!.SetPixels32(pixels);
        _fogTexture.Apply(false, false);
        _lastExploredCount = exploration.ExploredCount;
    }

    private static Color32 BiomeColor(UnderworldTerrainBiome biome) => biome switch
    {
        UnderworldTerrainBiome.FungalForest => new Color32(48, 82, 69, 255),
        UnderworldTerrainBiome.BlackwaterDeep => new Color32(18, 32, 48, 255),
        UnderworldTerrainBiome.SulfurousWastes => new Color32(105, 78, 38, 255),
        UnderworldTerrainBiome.FrozenCaverns => new Color32(91, 124, 137, 255),
        UnderworldTerrainBiome.FractureZones => new Color32(82, 57, 67, 255),
        UnderworldTerrainBiome.GreatDecay => new Color32(65, 72, 45, 255),
        _ => new Color32(32, 32, 32, 255),
    };

    private static Texture2D NewTexture(int width, int height, string name, FilterMode filterMode) => new(width, height, TextureFormat.RGBA32, false, true)
    {
        name = name, filterMode = filterMode, wrapMode = TextureWrapMode.Clamp,
    };

    internal static void Reset()
    {
        RestoreProjectedPins();
        Destroy(ref _fogTexture); Destroy(ref _biomeTexture);
        _lastExploredCount = -1; _biomeSeed = null;
    }

    private static void Destroy(ref Texture2D? texture)
    {
        if (texture is null) return;
        UnityEngine.Object.Destroy(texture); texture = null;
    }
}
