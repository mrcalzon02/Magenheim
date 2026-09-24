using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// A local material override on Unity's existing sky renderer. No world roof mesh, collider,
/// celestial body or second clock. The weather clones still supply Valheim's day/night light.
/// </summary>
internal sealed class UnderworldSkyboxPresentation : IDisposable
{
    private Material? _material;
    private Texture2D? _texture;
    private Material? _surfaceSky;
    private Skybox? _cameraSky;
    private Material? _surfaceCameraSky;
    private readonly Dictionary<Renderer, bool> _surfaceClouds = new();
    private bool _active;
    private GameObject? _haze;
    private Mesh? _hazeSource;
    private Material[] _hazeMaterials = Array.Empty<Material>();

    internal void Apply(Camera? camera)
    {
        EnsureMaterial();
        if (!_active)
        {
            _surfaceSky = RenderSettings.skybox;
            _active = true;
        }
        var cameraSky = camera ? camera.GetComponent<Skybox>() : null;
        if (cameraSky != _cameraSky)
        {
            RestoreCamera();
            _cameraSky = cameraSky;
            if (_cameraSky) _surfaceCameraSky = _cameraSky.material;
        }
        RenderSettings.skybox = _material;
        if (_cameraSky) _cameraSky.material = _material;

        var manager = EnvMan.instance;
        if (!manager) return;
        ApplyHaze(manager.m_clouds, camera);
        HideClouds(manager.m_clouds);
        HideClouds(manager.m_rainClouds);
        HideClouds(manager.m_rainCloudsDownside);
        // Native daylight interpolation is also used by the dim EnvSetup clones. The roof
        // remains visible at night; its lava and fungus do not switch off with a sun disc.
        var phase = manager.GetDayFraction();
        _material!.SetFloat("_Exposure", (float)Magenheim.Core.Underworld.UnderworldSkyLighting.Exposure(phase));
    }

    private void EnsureMaterial()
    {
        if (_material) return;
        // These built-in sky shaders are listed in the installed player's globalgamemanagers.
        // Panoramic renders only the authored texture: unlike Procedural, it has no sun disc.
        var shader = Shader.Find("Skybox/Panoramic");
        if (!shader)
            foreach (var loaded in Resources.FindObjectsOfTypeAll<Shader>())
                if (loaded && loaded.name == "Skybox/Panoramic") { shader = loaded; break; }
        if (!shader) throw new InvalidOperationException("Underworld sky requires the player's Skybox/Panoramic shader.");

        var path = Path.Combine(Path.GetDirectoryName(typeof(UnderworldSkyboxPresentation).Assembly.Location)!,
            "assets", "textures", "underworld", "sky", "lava-fungal-roof-v2.png");
        var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        var mask = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
        Texture2D? texture = null;
        try
        {
            if (!ModelAssets.LoadImage(source, File.ReadAllBytes(path)) || source.width != source.height * 2)
                throw new InvalidDataException("Underworld roof must be a readable 2:1 panorama.");
            var maskPath = Path.Combine(Path.GetDirectoryName(path)!, "lava-fungal-roof-emission.png");
            if (!ModelAssets.LoadImage(mask, File.ReadAllBytes(maskPath)) ||
                mask.width != source.width || mask.height != source.height)
                throw new InvalidDataException("Roof emission mask must align exactly with the panorama.");
            // Bake the independent material mask once into an HDR panorama. Unity's existing
            // sky shader can then render self-lit fissures without a custom shader or per-frame
            // million-pixel texture updates. The native directional/ambient lights light terrain.
            var colour = source.GetPixels32();
            var emission = mask.GetPixels32();
            var hdr = new Color[colour.Length];
            for (var i = 0; i < hdr.Length; i++)
            {
                var linear = ((Color)colour[i]).linear;
                var peak = Mathf.Max(.001f, Mathf.Max(linear.r, Mathf.Max(linear.g, linear.b)));
                var glow = emission[i].r / 255f * 4f;
                hdr[i] = new Color(linear.r + linear.r / peak * glow,
                    linear.g + linear.g / peak * glow, linear.b + linear.b / peak * glow, 1f);
            }
            texture = new Texture2D(source.width, source.height, TextureFormat.RGBAHalf, true, true);
            texture.SetPixels(hdr);
            texture.Apply(true, true);
            texture.name = "Magenheim_Underworld_LavaFungalRoof";
            texture.wrapModeU = TextureWrapMode.Repeat;
            texture.wrapModeV = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Trilinear;
            _material = new Material(shader) { name = "Magenheim_Underworld_SunlessSky" };
            _material.SetTexture("_MainTex", texture);
            _material.SetFloat("_Mapping", 1f); // latitude/longitude, not six-frame layout
            _material.SetFloat("_ImageType", 0f); // 360 degrees
            _material.SetFloat("_Layout", 0f); // monoscopic
            _material.SetFloat("_Rotation", 0f);
            _material.SetColor("_Tint", Color.gray); // built-in sky shader's neutral tint
            _material.SetFloat("_Exposure", .65f);
            _texture = texture;
        }
        catch
        {
            if (texture) UnityEngine.Object.Destroy(texture);
            if (_material) UnityEngine.Object.Destroy(_material);
            _material = null;
            throw;
        }
        finally
        {
            UnityEngine.Object.Destroy(source);
            UnityEngine.Object.Destroy(mask);
        }
    }

    private void ApplyHaze(MeshRenderer? donor, Camera? camera)
    {
        if (!donor || !camera) return;
        var filter = donor.GetComponent<MeshFilter>();
        if (!filter || !filter.sharedMesh)
            throw new InvalidOperationException("Valheim's cloud renderer has no mesh for the cavern haze band.");
        if (!_haze || _hazeSource != filter.sharedMesh)
        {
            DestroyHaze();
            // Reuse the game's cloud geometry/shader; do not generate a second world roof.
            _haze = new GameObject("Magenheim_Underworld_OverheadHaze");
            _hazeSource = filter.sharedMesh;
            _haze.AddComponent<MeshFilter>().sharedMesh = _hazeSource;
            var renderer = _haze.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var sources = donor.sharedMaterials;
            _hazeMaterials = new Material[sources.Length];
            for (var i = 0; i < sources.Length; i++)
            {
                _hazeMaterials[i] = new Material(sources[i]) { name = "Magenheim_CavernHaze" };
                if (_hazeMaterials[i].HasProperty("_Opacity")) _hazeMaterials[i].SetFloat("_Opacity", .30f);
            }
            renderer.sharedMaterials = _hazeMaterials;
            _haze.transform.rotation = donor.transform.rotation;
            _haze.transform.localScale = donor.transform.lossyScale;
        }
        _haze.SetActive(true);
        var position = camera.transform.position;
        position.y = UnderworldInstanceLayer.EngineBaseY +
                     (float)Magenheim.Core.Underworld.UnderworldSkyLighting.HazeTopMeters;
        _haze.transform.position = position;
        // Anchor the native mesh's top at the visual line, accounting for its imported pivot.
        var top = _haze.GetComponent<MeshRenderer>().bounds.max.y;
        position.y += UnderworldInstanceLayer.EngineBaseY +
                      (float)Magenheim.Core.Underworld.UnderworldSkyLighting.HazeTopMeters - top;
        _haze.transform.position = position;
    }

    private void DestroyHaze()
    {
        if (_haze) UnityEngine.Object.Destroy(_haze);
        foreach (var material in _hazeMaterials)
            if (material) UnityEngine.Object.Destroy(material);
        _hazeMaterials = Array.Empty<Material>();
        _haze = null;
        _hazeSource = null;
    }

    private void HideClouds(Renderer? renderer)
    {
        if (!renderer) return;
        if (!_surfaceClouds.ContainsKey(renderer)) _surfaceClouds.Add(renderer, renderer.enabled);
        renderer.enabled = false;
    }

    internal void Restore()
    {
        if (!_active) return;
        if (_haze) _haze.SetActive(false);
        RenderSettings.skybox = _surfaceSky;
        RestoreCamera();
        foreach (var pair in _surfaceClouds)
            if (pair.Key) pair.Key.enabled = pair.Value;
        _surfaceClouds.Clear();
        _surfaceSky = null;
        _active = false;
    }

    private void RestoreCamera()
    {
        if (_cameraSky) _cameraSky.material = _surfaceCameraSky;
        _cameraSky = null;
        _surfaceCameraSky = null;
    }

    public void Dispose()
    {
        Restore();
        DestroyHaze();
        if (_material) UnityEngine.Object.Destroy(_material);
        if (_texture) UnityEngine.Object.Destroy(_texture);
        _material = null;
        _texture = null;
    }
}
