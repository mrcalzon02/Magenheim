using System;
using System.IO;
using System.Reflection;
using BepInEx.Logging;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Resolves optional packaged production terrain artwork beside the runtime assembly.
/// Missing or malformed authored maps are non-fatal: the chunk materializer remains
/// responsible for its deterministic procedural fallback.
/// </summary>
internal static class UnderworldTerrainTextureAssets
{
    private const int AuthoredTextureSize = 256;
    private static readonly string RelativeTerrainDirectory = Path.Combine("assets", "textures", "underworld", "terrain");

    internal static bool TryLoadFamily(
        UnderworldTerrainBiome biome,
        ManualLogSource log,
        out Texture2D? albedo,
        out Texture2D? normal,
        out Texture2D? roughness)
    {
        albedo = null;
        normal = null;
        roughness = null;

        var stem = BiomeStem(biome);
        if (stem == null) return false;

        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
        if (string.IsNullOrWhiteSpace(assemblyDirectory)) return false;
        var terrainDirectory = Path.Combine(assemblyDirectory, RelativeTerrainDirectory);

        try
        {
            albedo = LoadMap(Path.Combine(terrainDirectory, $"{stem}_albedo.png"), false, $"Magenheim_Underworld_Terrain_{biome}_Albedo");
            normal = LoadMap(Path.Combine(terrainDirectory, $"{stem}_normal.png"), true, $"Magenheim_Underworld_Terrain_{biome}_Normal");
            roughness = LoadMap(Path.Combine(terrainDirectory, $"{stem}_roughness.png"), true, $"Magenheim_Underworld_Terrain_{biome}_Roughness");
            if (albedo == null || normal == null || roughness == null)
            {
                DestroyFamily(albedo, normal, roughness);
                albedo = normal = roughness = null;
                return false;
            }

            if (!HasExpectedDimensions(albedo) || !HasExpectedDimensions(normal) || !HasExpectedDimensions(roughness))
            {
                log.LogWarning($"Ignoring authored Underworld terrain family '{stem}': expected {AuthoredTextureSize}x{AuthoredTextureSize} maps.");
                DestroyFamily(albedo, normal, roughness);
                albedo = normal = roughness = null;
                return false;
            }

            Configure(albedo);
            Configure(normal);
            Configure(roughness);
            return true;
        }
        catch (Exception ex)
        {
            DestroyFamily(albedo, normal, roughness);
            albedo = normal = roughness = null;
            log.LogWarning($"Could not load authored Underworld terrain family '{stem}'; procedural terrain remains active. {ex.Message}");
            return false;
        }
    }

    private static Texture2D? LoadMap(string path, bool linear, string name)
    {
        if (!File.Exists(path)) return null;
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, true, linear) { name = name };
        try
        {
            if (!ModelAssets.LoadImage(texture, File.ReadAllBytes(path)))
            {
                UnityEngine.Object.Destroy(texture);
                return null;
            }
            return texture;
        }
        catch
        {
            UnityEngine.Object.Destroy(texture);
            throw;
        }
    }

    private static bool HasExpectedDimensions(Texture2D texture) =>
        texture.width == AuthoredTextureSize && texture.height == AuthoredTextureSize;

    private static void Configure(Texture2D texture)
    {
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Trilinear;
        texture.anisoLevel = 8;
    }

    private static void DestroyFamily(Texture2D? albedo, Texture2D? normal, Texture2D? roughness)
    {
        if (albedo) UnityEngine.Object.Destroy(albedo);
        if (normal) UnityEngine.Object.Destroy(normal);
        if (roughness) UnityEngine.Object.Destroy(roughness);
    }

    private static string? BiomeStem(UnderworldTerrainBiome biome) => biome switch
    {
        UnderworldTerrainBiome.FungalForest => "fungal_forest",
        UnderworldTerrainBiome.BlackwaterDeep => "blackwater_deep",
        UnderworldTerrainBiome.SulfurousWastes => "sulfurous_wastes",
        UnderworldTerrainBiome.FrozenCaverns => "frozen_caverns",
        UnderworldTerrainBiome.FractureZones => "fracture_zones",
        UnderworldTerrainBiome.GreatDecay => "great_decay",
        _ => null,
    };
}