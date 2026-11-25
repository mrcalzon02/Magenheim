using System;
using System.IO;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Binds the packaged biome artwork to Valheim's native terrain material instances.
/// </summary>
internal static class UnderworldTerrainTextureAssets
{
    private const int AuthoredTextureSize = 256;
    private static readonly string RelativeTerrainDirectory = Path.Combine("assets", "textures", "underworld", "terrain");
    private static readonly int DiffuseArray = Shader.PropertyToID("_DiffuseArrayTex");
    private static readonly int NormalArray = Shader.PropertyToID("_NormalArrayTex");
    private static Texture2DArray? _diffuse;
    private static Texture2DArray? _normal;
    private static ManualLogSource? _log;
    private static bool _attempted;

    internal static void Configure(ManualLogSource log) => _log = log;

    internal static void Apply(Material material)
    {
        if (!material || _log is null || !material.HasProperty(DiffuseArray) ||
            !material.HasProperty(NormalArray) || !SystemInfo.supports2DArrayTextures)
            return;

        if (!_attempted)
        {
            _attempted = true;
            BuildArrays(material, _log);
        }
        if (!_diffuse || !_normal || material.GetTexture(DiffuseArray) == _diffuse) return;
        material.SetTexture(DiffuseArray, _diffuse);
        material.SetTexture(NormalArray, _normal);
    }

    // Custom/Heightmap blends these fixed color keys and array slots for near and distant
    // terrain. Reuse the shader's native cliff, shore and ground-paint layers.
    private static readonly UnderworldTerrainBiome[] DiffuseSlots =
    {
        UnderworldTerrainBiome.FungalForest,      // grass surface
        UnderworldTerrainBiome.FrozenCaverns,    // snow
        UnderworldTerrainBiome.FrozenCaverns,    // ice
        UnderworldTerrainBiome.GreatDecay,       // forest floor
        UnderworldTerrainBiome.FractureZones,    // cliff
        UnderworldTerrainBiome.BlackwaterDeep,   // base soil and submerged terrain
        UnderworldTerrainBiome.BlackwaterDeep,   // wet shoreline
        UnderworldTerrainBiome.SulfurousWastes,   // ash
        UnderworldTerrainBiome.FractureZones,    // dry ground
        UnderworldTerrainBiome.GreatDecay,       // cultivated ground
        UnderworldTerrainBiome.FractureZones,    // paved ground
        UnderworldTerrainBiome.BlackwaterDeep,   // wet sediment
        UnderworldTerrainBiome.FrozenCaverns,    // packed snow
        UnderworldTerrainBiome.SulfurousWastes,   // ash variation
        UnderworldTerrainBiome.FractureZones,    // cliffs
        UnderworldTerrainBiome.FungalForest,
    };

    private static readonly UnderworldTerrainBiome[] NormalSlots =
    {
        UnderworldTerrainBiome.FungalForest,
        UnderworldTerrainBiome.FractureZones,
        UnderworldTerrainBiome.FrozenCaverns,
        UnderworldTerrainBiome.BlackwaterDeep,
        UnderworldTerrainBiome.SulfurousWastes,
        UnderworldTerrainBiome.GreatDecay,
    };

    private static void BuildArrays(Material native, ManualLogSource log)
    {
        if (native.GetTexture(DiffuseArray) is not Texture2DArray nativeDiffuse ||
            native.GetTexture(NormalArray) is not Texture2DArray nativeNormal ||
            nativeDiffuse.depth != DiffuseSlots.Length || nativeNormal.depth != NormalSlots.Length)
        {
            log.LogWarning("Underworld terrain artwork requires the native heightmap texture array layout.");
            return;
        }

        var albedos = new Color32[NormalSlots.Length][];
        var normals = new Color32[NormalSlots.Length][];
        Texture2DArray? diffuse = null;
        Texture2DArray? normal = null;
        try
        {
            foreach (var biome in NormalSlots)
            {
                if (!TryLoadFamily(biome, log, out var albedoMap, out var normalMap, out var roughnessMap))
                {
                    log.LogWarning($"Underworld terrain artwork is incomplete for {biome}; using native textures.");
                    return;
                }
                try
                {
                    albedos[(int)biome] = albedoMap!.GetPixels32();
                    normals[(int)biome] = normalMap!.GetPixels32();
                }
                finally { DestroyFamily(albedoMap, normalMap, roughnessMap); }
            }
            diffuse = MakeArray("Magenheim_Underworld_Diffuse", DiffuseSlots, albedos, linear: false);
            normal = MakeArray("Magenheim_Underworld_Normal", NormalSlots, normals, linear: true);
            _diffuse = diffuse;
            _normal = normal;
            log.LogInfo("Bound six authored Underworld terrain palettes to native heightmap texture arrays.");
        }
        catch (Exception exception)
        {
            if (diffuse) UnityEngine.Object.Destroy(diffuse);
            if (normal) UnityEngine.Object.Destroy(normal);
            log.LogWarning("Underworld terrain arrays could not be built; using native textures. " + exception.Message);
        }
    }

    private static Texture2DArray MakeArray(string name, UnderworldTerrainBiome[] slots, Color32[][] pixels, bool linear)
    {
        var array = new Texture2DArray(AuthoredTextureSize, AuthoredTextureSize, slots.Length,
            TextureFormat.RGBA32, true, linear)
        {
            name = name,
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Trilinear,
            anisoLevel = 8,
        };
        try
        {
            for (var index = 0; index < slots.Length; index++) array.SetPixels32(pixels[(int)slots[index]], index);
            array.Apply(updateMipmaps: true, makeNoLongerReadable: true);
            return array;
        }
        catch
        {
            UnityEngine.Object.Destroy(array);
            throw;
        }
    }

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

[HarmonyPatch(typeof(Heightmap), "GetBiomeColor", new[] { typeof(Heightmap.Biome) })]
internal static class UnderworldHeightmapBiomeColorPatch
{
    private static void Prefix(ref Heightmap.Biome __0)
    {
        __0 = __0 switch
        {
            UnderworldTerrainRuntime.FungalForestBiome => Heightmap.Biome.Meadows,
            UnderworldTerrainRuntime.BlackwaterDeepBiome => Heightmap.Biome.Swamp,
            UnderworldTerrainRuntime.SulfurousWastesBiome => Heightmap.Biome.AshLands,
            UnderworldTerrainRuntime.FrozenCavernsBiome => Heightmap.Biome.Mountain,
            UnderworldTerrainRuntime.FractureZonesBiome => Heightmap.Biome.Plains,
            UnderworldTerrainRuntime.GreatDecayBiome => Heightmap.Biome.BlackForest,
            _ => __0,
        };
    }
}

[HarmonyPatch(typeof(Heightmap), "RebuildRenderMesh")]
internal static class UnderworldHeightmapTerrainMaterialPatch
{
    private static void Postfix(Heightmap __instance, Material ___m_materialInstance)
    {
        if (ValheimWorldInstanceExecution.TryGetContextForScene(__instance.gameObject.scene.handle, out var context) &&
            context is not null && context.InstanceId.IsUnderworld)
            UnderworldTerrainTextureAssets.Apply(___m_materialInstance);
    }
}
