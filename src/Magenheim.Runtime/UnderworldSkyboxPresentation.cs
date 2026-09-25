using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SoftReferenceableAssets;

namespace Magenheim.Runtime;

/// <summary>
/// A camera-centred background shell using Valheim's shipped unlit shader. No roof collider,
/// celestial body or second clock. The weather clones still supply Valheim's day/night light.
/// </summary>
internal sealed class UnderworldSkyboxPresentation : IDisposable
{
    private Material? _material;
    private Texture2D? _texture;
    private Material? _surfaceSky;
    private Skybox? _cameraSky;
    private Material? _surfaceCameraSky;
    private GameObject? _shell;
    private Mesh? _shellMesh;
    private Color[]? _shellColors;
    private float _exposure = -1f;
    private SoftReference<Shader> _shaderSource;
    private bool _shaderLoaded;
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
        // The shipped player strips Skybox/Panoramic. Draw our background through its real
        // unlit particle shader, with fog/fading disabled and ordinary world depth testing.
        RenderSettings.skybox = null;
        if (_cameraSky) _cameraSky.material = null;
        EnsureShell();
        if (camera) _shell!.transform.position = camera.transform.position;
        _shell!.SetActive(true);

        var manager = EnvMan.instance;
        if (!manager) return;
        ApplyHaze(manager.m_clouds, camera);
        HideClouds(manager.m_clouds);
        HideClouds(manager.m_rainClouds);
        HideClouds(manager.m_rainCloudsDownside);
        // Native daylight interpolation is also used by the dim EnvSetup clones. The roof
        // remains visible at night; its lava and fungus do not switch off with a sun disc.
        var phase = manager.GetDayFraction();
        var exposure = (float)Magenheim.Core.Underworld.UnderworldSkyLighting.Exposure(phase);
        if (Mathf.Abs(exposure - _exposure) > .005f)
        {
            _exposure = exposure;
            for (var i = 0; i < _shellColors!.Length; i++) _shellColors[i] = new Color(exposure, exposure, exposure, 1f);
            _shellMesh!.colors = _shellColors;
        }
    }

    private void EnsureMaterial()
    {
        if (_material) return;
        // Native asset ID verified against StreamingAssets/SoftRef/manifest_extended.
        if (!_shaderLoaded)
        {
            if (!AssetID.TryParse("e1e858596580e684788c10ca60865118", out var id))
                throw new InvalidOperationException("Invalid native unlit sky shader ID.");
            _shaderSource = new SoftReference<Shader>(id);
            _shaderSource.Load();
            _shaderLoaded = true;
        }
        var shader = _shaderSource.Asset;
        if (!shader || !shader.isSupported) throw new InvalidOperationException("Native ParticleUnlit shader could not load.");

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
            // background shader can then render self-lit fissures without a custom shader or per-frame
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
            _material.renderQueue = 1000;
            _material.SetFloat("_SrcBlend", 1f);
            _material.SetFloat("_DstBlend", 0f);
            _material.SetFloat("_Cull", 0f);
            _material.SetFloat("_FejdFog", 0f);
            _material.SetFloat("_SkyMask", 0f);
            _material.SetFloat("_SoftParticles", 0f);
            _material.SetFloat("_CameraFadeFactor", 1f);
            _material.DisableKeyword("SOFTPARTICLES_ON");
            _material.DisableKeyword("_FEJDFOG_ON");
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

    private void EnsureShell()
    {
        if (_shell) return;
        const int longitude = 96, latitude = 48;
        var vertices = new Vector3[(longitude + 1) * (latitude + 1)];
        var uv = new Vector2[vertices.Length];
        _shellColors = new Color[vertices.Length];
        var triangles = new List<int>();
        for (var y = 0; y <= latitude; y++)
        for (var x = 0; x <= longitude; x++)
        {
            var u = x / (float)longitude; var v = y / (float)latitude;
            var a = u * Mathf.PI * 2f; var p = v * Mathf.PI;
            var i = y * (longitude + 1) + x;
            vertices[i] = new Vector3(Mathf.Sin(p)*Mathf.Cos(a), -Mathf.Cos(p), Mathf.Sin(p)*Mathf.Sin(a)) * 18000f;
            uv[i] = new Vector2(u, v);
            _shellColors[i] = Color.white;
            if (x == longitude || y == latitude) continue;
            var b = i + longitude + 1;
            triangles.Add(i); triangles.Add(i+1); triangles.Add(b);
            triangles.Add(i+1); triangles.Add(b+1); triangles.Add(b);
        }
        _shellMesh = new Mesh { name = "Magenheim_CavernSky_Background" };
        _shellMesh.vertices = vertices; _shellMesh.uv = uv; _shellMesh.colors = _shellColors;
        _shellMesh.SetTriangles(triangles, 0); _shellMesh.RecalculateBounds();
        _shell = new GameObject("Magenheim_CavernSky_Background");
        _shell.AddComponent<MeshFilter>().sharedMesh = _shellMesh;
        var renderer = _shell.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = _material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
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
        if (_shell) _shell.SetActive(false);
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
        if (_shell) UnityEngine.Object.Destroy(_shell);
        if (_shellMesh) UnityEngine.Object.Destroy(_shellMesh);
        if (_shaderLoaded) _shaderSource.Release();
        _shaderLoaded = false;
        if (_material) UnityEngine.Object.Destroy(_material);
        if (_texture) UnityEngine.Object.Destroy(_texture);
        _material = null;
        _texture = null;
    }
}
