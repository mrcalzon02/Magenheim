using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Magenheim.Runtime;

/// <summary>Imports the checked-in Blender model payloads. No shape generators or fallback geometry.</summary>
internal static class ModelAssets
{
    private static readonly Dictionary<string, JObject> Documents = new();
    private static readonly Dictionary<string, Mesh> Meshes = new();
    private static readonly Dictionary<string, Material> Materials = new();
    private static readonly Dictionary<string, Texture2D> Textures = new();
    private static string DirectoryPath => Path.Combine(Path.GetDirectoryName(typeof(ModelAssets).Assembly.Location)!, "assets", "models");

    // Valheim ships its shaders in addressable bundles and does not include Unity's built-in
    // Standard shader, so Shader.Find("Standard") returns null in the built player. That took out
    // every caller that needed a material for a prefab with no renderer of its own -- the Deep
    // Fracture and Dark Throne location/room visuals, and the Underworld Conclave stonework.
    private static readonly string[] SurfaceShaderNames =
    {
        "Custom/StaticRock",
        "Custom/Piece",
        "Custom/Vegetation",
        "Custom/Creature",
        "Standard",
    };

    /// <summary>Finds a usable opaque surface shader from the running game, or null if none is loaded.</summary>
    internal static Shader? FindSurfaceShader()
    {
        foreach (var name in SurfaceShaderNames)
        {
            var shader = Shader.Find(name);
            if (shader) return shader;
        }

        // Shader.Find only sees shaders that are already loaded, so a bundle-resident shader can be
        // missed by name even while it is resident. Check the loaded set before giving up.
        var loaded = Resources.FindObjectsOfTypeAll<Shader>();
        foreach (var name in SurfaceShaderNames)
            foreach (var shader in loaded)
                if (shader && shader.name == name) return shader;
        return null;
    }

    internal static Shader ResolveSurfaceShader() =>
        FindSurfaceShader() ?? throw new InvalidOperationException(
            "No Magenheim surface shader is available; expected one of " + string.Join(", ", SurfaceShaderNames) + ".");

    internal static Mesh LoadSingleMesh(string id)
    {
        if (string.IsNullOrEmpty(id) || Path.GetFileName(id) != id) throw new ArgumentException("Invalid model identity.", nameof(id));
        // The underworld registrar retains its own interaction objects and placement.
        // Only their geometry is supplied by the editable library.
        var document = JObject.Parse(File.ReadAllText(Path.Combine(DirectoryPath, "runtime", id + ".model.json")));
        if (!(document["parts"] is JArray parts) || parts.Count != 1) throw new InvalidDataException("Single mesh asset required: " + id);
        return LoadMesh(id + "/0", parts[0]);
    }

    internal static GameObject Load(GameObject prefab, string id, bool item = false, float scale = 1f, bool hideOriginal = true, bool preserveParticles = false, Transform? parent = null)
    {
        if (!prefab) throw new ArgumentNullException(nameof(prefab));
        if (scale <= 0 || float.IsNaN(scale) || float.IsInfinity(scale)) throw new ArgumentOutOfRangeException(nameof(scale));
        if (string.IsNullOrEmpty(id) || Path.GetFileName(id) != id) throw new ArgumentException("Invalid model identity.", nameof(id));
        var original = prefab.GetComponentsInChildren<Renderer>(true);
        var source = original.Where(r => !(r is ParticleSystemRenderer)).Select(r => r.sharedMaterial).FirstOrDefault(m => m);
        if (!source) source = new Material(ResolveSurfaceShader());
        if (!Documents.TryGetValue(id, out var document))
        {
            document = JObject.Parse(File.ReadAllText(Path.Combine(DirectoryPath, "runtime", id + ".model.json")));
            if (!(document["parts"] is JArray parts) || parts.Count == 0) throw new InvalidDataException("Empty model: " + id);
            Documents.Add(id, document);
        }
        // Load and validate every mesh before changing the host's visible state.
        var loaded = ((JArray)document["parts"]!).Select((p, i) => new { Data = p, Mesh = LoadMesh(id + "/" + i, p), Material = LoadMaterial(id + "/" + i, p["material"]!, source!) }).ToArray();
        foreach(var entry in loaded) { var data=(JObject)entry.Data; data.Remove("vertices");data.Remove("normals");data.Remove("uv");data.Remove("triangles"); }
        var root = new GameObject("magenheim." + id + ".visual") { layer = prefab.layer };
        root.transform.SetParent(parent ?? (item ? prefab.transform.Find("attach") ?? prefab.transform : prefab.transform), false);
        root.transform.localScale = Vector3.one * scale;
        try
        {
            foreach (var entry in loaded)
            {
                var part = new GameObject((string)entry.Data["name"]!) { layer = prefab.layer };
                part.transform.SetParent(root.transform, false);
                part.AddComponent<MeshFilter>().sharedMesh = entry.Mesh;
                part.AddComponent<MeshRenderer>().sharedMaterial = entry.Material;
                if ((bool?)entry.Data["collider"] == true) part.AddComponent<MeshCollider>().sharedMesh = entry.Mesh;
                if (entry.Data["crystal"] is JObject crystal)
                {
                    // The exported mesh is in model coordinates. Bind the hit box to those bounds.
                    var collider = part.AddComponent<BoxCollider>(); collider.center = entry.Mesh.bounds.center; collider.size = entry.Mesh.bounds.size;
                    DeepFractureCrystalComponent.Attach(part,
                        prefab.GetComponent<Character>() ?? throw new InvalidOperationException("Crystal host requires Character."),
                        prefab.GetComponent<ZNetView>() ?? throw new InvalidOperationException("Crystal host requires ZNetView."),
                        (string)crystal["id"]!, (DeepFractureCrystalFunction)Enum.Parse(typeof(DeepFractureCrystalFunction), (string)crystal["function"]!), (float)crystal["health"]!);
                }
                // The Sentinel's visible moving assembly must follow the existing aim transform.
                if (((string?)entry.Data["path"])?.StartsWith("turret-body/", StringComparison.Ordinal) == true && !id.StartsWith("sentinel-ammo-", StringComparison.Ordinal))
                {
                    var turret = prefab.GetComponent<Turret>();
                    if (turret == null || turret.m_turretBody == null) throw new InvalidOperationException("Sentinel model requires turret body.");
                    part.transform.SetParent(turret.m_turretBody.transform, false);
                }
            }
            foreach (var data in document["lights"] ?? new JArray())
            {
                var go = new GameObject("model-light") { layer = prefab.layer }; go.transform.SetParent(root.transform, false);
                go.transform.localPosition = Vector(data["position"]!); var light = go.AddComponent<Light>();
                light.color = Colour(data["color"]!); light.range = (float)data["range"]!; light.intensity = (float)data["intensity"]!;
            }
            if (hideOriginal)
            {
                foreach (var renderer in original) if (!preserveParticles || !(renderer is ParticleSystemRenderer)) renderer.enabled = false;
                foreach (var lod in prefab.GetComponentsInChildren<LODGroup>(true)) lod.enabled = false;
            }
            return root;
        }
        catch { UnityEngine.Object.DestroyImmediate(root); throw; }
    }

    private static Mesh LoadMesh(string key, JToken part)
    {
        if (Meshes.TryGetValue(key, out var cached)) return cached;
        var vertices = part["vertices"]!.Select(Vector).ToArray(); var triangles = part["triangles"]!.Select(t => (int)t).ToArray();
        if (vertices.Length == 0 || triangles.Length == 0 || triangles.Length % 3 != 0 || triangles.Any(i => i < 0 || i >= vertices.Length)) throw new InvalidDataException("Invalid model topology: " + key);
        if (vertices.Any(v => !Finite(v.x) || !Finite(v.y) || !Finite(v.z))) throw new InvalidDataException("Non-finite model position: " + key);
        var normals = part["normals"]!.Select(Vector).ToArray(); var uv = part["uv"]!.Select(v => new Vector2((float)v[0]!, (float)v[1]!)).ToArray();
        if (normals.Length != vertices.Length || uv.Length != vertices.Length) throw new InvalidDataException("Incomplete model surface: " + key);
        var mesh = new Mesh { name = key, indexFormat = vertices.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16, vertices = vertices, triangles = triangles, normals = normals, uv = uv };
        mesh.RecalculateBounds(); mesh.RecalculateTangents(); Meshes.Add(key, mesh); return mesh;
    }

    private static Material LoadMaterial(string key, JToken data, Material source)
    {
        if (Materials.TryGetValue(key, out var cached)) return cached;
        var material = new Material(source) { name = (string?)data["name"] ?? key };
        var color = Colour(data["color"]!); material.color = color;
        material.mainTexture = Texture2D.whiteTexture; material.mainTextureScale = Vector2.one; material.mainTextureOffset = Vector2.zero;
        var textureName = (string?)data["texture"];
        if (!string.IsNullOrEmpty(textureName))
        {
            if (Path.GetFileName(textureName) != textureName) throw new InvalidDataException("Invalid texture path.");
            if (!Textures.TryGetValue(textureName!, out var texture))
            {
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, true) { name = textureName, wrapMode = TextureWrapMode.Repeat };
                var loadImage=typeof(ImageConversion).GetMethod("LoadImage",new[]{typeof(Texture2D),typeof(byte[]),typeof(bool)}) ?? throw new MissingMethodException("ImageConversion.LoadImage");
                if (!(bool)(loadImage.Invoke(null,new object[]{texture,File.ReadAllBytes(Path.Combine(DirectoryPath,"textures",textureName)),false}) ?? false)) throw new InvalidDataException("Invalid model texture: " + textureName);
                Textures.Add(textureName!, texture);
            }
            material.mainTexture = texture;
        }
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", (float)data["metallic"]!);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 1f - (float)data["roughness"]!);
        if (material.HasProperty("_BumpMap")) material.SetTexture("_BumpMap", null); material.DisableKeyword("_NORMALMAP");
        var emission = Colour(data["emission"]!);
        if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", emission);
        if (emission.r + emission.g + emission.b > 0) material.EnableKeyword("_EMISSION"); else material.DisableKeyword("_EMISSION");
        if(material.HasProperty("_Cull"))material.SetInt("_Cull",(bool?)data["doubleSided"]==true?0:2);
        var transparent = color.a < .999f;
        material.SetOverrideTag("RenderType", transparent ? "Transparent" : "Opaque"); material.renderQueue = transparent ? 3000 : 2000;
        if (material.HasProperty("_ZWrite")) material.SetInt("_ZWrite", transparent ? 0 : 1);
        if (material.HasProperty("_SrcBlend")) material.SetInt("_SrcBlend", (int)(transparent ? BlendMode.SrcAlpha : BlendMode.One));
        if (material.HasProperty("_DstBlend")) material.SetInt("_DstBlend", (int)(transparent ? BlendMode.OneMinusSrcAlpha : BlendMode.Zero));
        material.DisableKeyword("_ALPHATEST_ON"); material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        if (transparent) material.EnableKeyword("_ALPHABLEND_ON"); else material.DisableKeyword("_ALPHABLEND_ON");
        Materials.Add(key, material); return material;
    }
    internal static Vector3 Vector(JToken v) => new((float)v[0]!, (float)v[1]!, (float)v[2]!);
    private static Color Colour(JToken v) => new((float)v[0]!, (float)v[1]!, (float)v[2]!, v.Count() > 3 ? (float)v[3]! : 1f);
    private static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
}
