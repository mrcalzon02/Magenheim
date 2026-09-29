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
    /// <summary>Set by the plugin so held-model alignment can report the correction it measured.
    /// A plain delegate rather than a ManualLogSource: ModelAssetTests compiles this file without
    /// a BepInEx reference.</summary>
    internal static Action<string>? Log { get; set; }

    /// <summary>Installed by the plugin. Kept as a delegate so this file still compiles against
    /// the ModelAssetTests Unity shim, which has no Matrix4x4 and a minimal Bounds.</summary>
    internal static Action<Transform, Renderer[], GameObject, string>? HeldModelAligner { get; set; }

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
        "Standard",
    };

    /// <summary>
    /// Donor shaders whose vertex/surface programs are unsafe for an arbitrary authored mesh.
    /// </summary>
    /// <remarks>
    /// Valheim's piece shader projects its texture from where the piece stands in the world. On a
    /// vanilla building piece that is the point -- it keeps long walls and floors tiling coherently
    /// without any UV work. On a Magenheim mesh it means the UVs the exporter carefully writes are
    /// never sampled at all, and the model renders as a smeared lattice sliding across its own
    /// faces: geometry, UVs and texture can all be perfect and it still looks wrong, which is
    /// exactly how it kept being reported from the field. The vegetation shader is more destructive:
    /// it treats mesh vertex data as wind-animation input. The Crystal Beds and Ice Box clone the
    /// beehive and inherited that shader; their correctly baked parts were consequently displaced
    /// into the exploded assemblies seen in game. Material scalar changes cannot disable that vertex
    /// program, so both donor shaders must be replaced before the authored material is populated.
    /// </remarks>
    private static readonly string[] UnsafeAuthoredMeshShaderNames = { "Custom/Piece", "Custom/Vegetation" };

    /// <summary>Surface shaders that do sample mesh UVs, in preference order.</summary>
    /// <remarks>
    /// Deliberately not <see cref="SurfaceShaderNames"/>: that list contains Custom/Piece as a
    /// fallback, so searching it to escape Custom/Piece could return Custom/Piece.
    /// </remarks>
    private static readonly string[] UvSurfaceShaderNames = { "Custom/StaticRock", "Standard" };

    /// <summary>Finds a usable opaque surface shader from the running game, or null if none is loaded.</summary>
    internal static Shader? FindSurfaceShader() => FindShader(SurfaceShaderNames);

    private static Shader? FindShader(string[] names)
    {
        foreach (var name in names)
        {
            var shader = Shader.Find(name);
            if (shader) return shader;
        }

        // Shader.Find only sees shaders that are already loaded, so a bundle-resident shader can be
        // missed by name even while it is resident. Check the loaded set before giving up.
        var loaded = Resources.FindObjectsOfTypeAll<Shader>();
        foreach (var name in names)
            foreach (var shader in loaded)
                if (shader && shader.name == name) return shader;
        return null;
    }

    /// <summary>
    /// Swaps a world-projecting donor shader for one that samples our UVs.
    /// </summary>
    /// <remarks>
    /// 0.0.74 stripped the donor's normal, metallic, occlusion and moss maps because they were
    /// authored for the donor's geometry. It left the donor's *shader*, which is the other half of
    /// the same defect: a piece-shader material ignores mesh UVs outright. CrystalArchitecture and
    /// the dais worked around it by passing Rock_4's material in explicitly, and that repair was
    /// never rolled out, so the ice box, the decor set, the furniture set, the beds, the banners and
    /// the sentinel all still inherited it -- reported from play as the same smearing on the sconce
    /// and "a number of the other crystal placeables". Doing it here instead of at each call site
    /// means a placeable authored tomorrow cannot reintroduce it by forgetting an argument.
    ///
    /// Only the shader is replaced. Colour, texture, metallic and roughness all come from the model
    /// payload in LoadMaterial regardless, so nothing authored is lost.
    /// </remarks>
    /// <summary>Decodes a PNG into a texture without tripping over Unity's Span overload.</summary>
    /// <remarks>
    /// ImageConversion.LoadImage has a ReadOnlySpan&lt;byte&gt; overload in current Unity. Resolving
    /// that call requires System.ReadOnlySpan`1 to exist, and it does not on net462 without
    /// System.Memory, which the game does not ship -- so a perfectly ordinary
    /// `ImageConversion.LoadImage(texture, bytes, false)` fails to compile with CS0518 even though
    /// the byte[] overload it wants is right there. Binding that overload by reflection sidesteps
    /// the whole overload set. This lived inline in LoadMaterial; it is named here because
    /// UnderworldTerrainTextureAssets hit the identical wall and shipped unbuilt, and the next
    /// caller should find the answer rather than the error.
    /// </remarks>
    internal static bool LoadImage(Texture2D texture, byte[] data)
    {
        var loadImage = typeof(ImageConversion).GetMethod("LoadImage", new[] { typeof(Texture2D), typeof(byte[]), typeof(bool) })
            ?? throw new MissingMethodException("ImageConversion.LoadImage");
        return (bool)(loadImage.Invoke(null, new object[] { texture, data, false }) ?? false);
    }

    private static Material? Uncouple(Material? source, string id)
    {
        if (!source || !source.shader || Array.IndexOf(UnsafeAuthoredMeshShaderNames, source.shader.name) < 0)
            return source;
        var replacement = FindShader(UvSurfaceShaderNames);
        if (!replacement) return source;
        Log?.Invoke($"Model '{id}' inherited authored-mesh-unsafe shader '{source.shader.name}' from its donor; " +
                    $"surfacing it with '{replacement.name}' so its vertices and UVs remain authored.");
        return new Material(replacement);
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

    internal static Material LoadSingleMaterial(string id)
    {
        if (string.IsNullOrEmpty(id) || Path.GetFileName(id) != id) throw new ArgumentException("Invalid model identity.", nameof(id));
        var document = Document(id);
        if (!(document["parts"] is JArray parts) || parts.Count != 1) throw new InvalidDataException("Single material asset required: " + id);
        var source = new Material(ResolveSurfaceShader());
        return LoadMaterial(id + "/0", parts[0]["material"]!, source);
    }

    /// <summary>A model payload, parsed once and cached. Mesh arrays are dropped after first load.</summary>
    private static JObject Document(string id)
    {
        if (string.IsNullOrEmpty(id) || Path.GetFileName(id) != id) throw new ArgumentException("Invalid model identity.", nameof(id));
        if (Documents.TryGetValue(id, out var document)) return document;
        document = JObject.Parse(File.ReadAllText(Path.Combine(DirectoryPath, "runtime", id + ".model.json")));
        if (!(document["parts"] is JArray parts) || parts.Count == 0) throw new InvalidDataException("Empty model: " + id);
        Documents.Add(id, document);
        return document;
    }

    /// <summary>The canonical skeleton a rigid bone-segment model was authored against, if it declares one.</summary>
    internal static JToken? Rig(string id) => Document(id)["rig"];

    internal static JToken? SkinRig(string id) => Document(id)["skinRig"];

    internal static GameObject Load(GameObject prefab, string id, bool item = false, float scale = 1f, bool hideOriginal = true, bool preserveParticles = false, Transform? parent = null, Material? materialSource = null, Action<string, Transform>? arrange = null)
    {
        if (!prefab) throw new ArgumentNullException(nameof(prefab));
        if (scale <= 0 || float.IsNaN(scale) || float.IsInfinity(scale)) throw new ArgumentOutOfRangeException(nameof(scale));
        if (string.IsNullOrEmpty(id) || Path.GetFileName(id) != id) throw new ArgumentException("Invalid model identity.", nameof(id));
        var original = prefab.GetComponentsInChildren<Renderer>(true);
        // A caller may supply the material to clone. Building-piece donors (stone_floor_2x2,
        // wood_beam, piece_workbench) carry Valheim's piece shader, which projects its own surface
        // from world position and ignores the UVs the exporter writes -- on a Magenheim mesh that
        // renders as a smeared lattice no amount of texture or UV work can correct. Static-rock
        // donors do not, which is why the geode shell reads correctly. See CrystalArchitectureVisuals.
        var source = materialSource;
        if (!source) source = original.Where(r => !(r is ParticleSystemRenderer)).Select(r => r.sharedMaterial).FirstOrDefault(m => m);
        if (!source) source = new Material(ResolveSurfaceShader());
        source = Uncouple(source, id);
        var document = Document(id);
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
                arrange?.Invoke((string?)entry.Data["path"] ?? string.Empty, part.transform);
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
            // A held item's donor authors its mesh in its own local frame under `attach`, and each
            // crystal weapon clones a different donor, so dropping the replacement in at identity
            // presents correctly only where the two frames happen to agree. Measure the donor and
            // match it. This runs before the originals are hidden, because it needs their bounds.
            if (item && hideOriginal && HeldModelAligner is not null && root.transform.parent is not null)
                HeldModelAligner(root.transform.parent, original, root, id);
            if (hideOriginal)
            {
                foreach (var renderer in original) if (!preserveParticles || !(renderer is ParticleSystemRenderer)) renderer.enabled = false;
                foreach (var lod in prefab.GetComponentsInChildren<LODGroup>(true)) lod.enabled = false;
            }
            return root;
        }
        catch { UnityEngine.Object.DestroyImmediate(root); throw; }
    }

    internal static GameObject LoadSkinnedEquipment(GameObject prefab, string id, Material? materialSource = null)
    {
        if (!prefab) throw new ArgumentNullException(nameof(prefab));
        if (string.IsNullOrEmpty(id) || Path.GetFileName(id) != id) throw new ArgumentException("Invalid model identity.", nameof(id));
        var document = Document(id);
        if (!(document["skinRig"] is JObject skinRig) || (string?)skinRig["kind"] != "valheim-player-attach-skin")
            throw new InvalidDataException("Wearable model has no Valheim attach_skin contract: " + id);
        var names = ((JArray?)skinRig["bones"])?.Select(value => (string?)value ?? string.Empty).ToArray()
            ?? throw new InvalidDataException("Wearable model has no canonical bone list: " + id);
        if (names.Length == 0 || names.Any(string.IsNullOrEmpty) || names.Distinct(StringComparer.Ordinal).Count() != names.Length)
            throw new InvalidDataException("Wearable model has invalid canonical bones: " + id);
        var attachSkin = prefab.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(value => value.gameObject.name == "attach_skin")
            ?? throw new InvalidOperationException("Equipment donor exposes no attach_skin hierarchy: " + prefab.name);
        var originals = attachSkin.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if (originals.Length == 0) throw new InvalidOperationException("Equipment donor attach_skin has no skinned renderer: " + prefab.name);
        var available = originals.SelectMany(renderer => (renderer.bones ?? Array.Empty<Transform>())
            .Concat(renderer.rootBone ? new[] { renderer.rootBone } : Array.Empty<Transform>()))
            .Where(value => value).GroupBy(value => value.gameObject.name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var bones = new Transform[names.Length];
        for (var i = 0; i < names.Length; i++)
            if (!available.TryGetValue(names[i], out bones[i]!))
                throw new InvalidOperationException($"Equipment donor {prefab.name} lacks attach_skin bone '{names[i]}' for {id}.");
        var rootName = (string?)skinRig["root"] ?? "Hips";
        if (!available.TryGetValue(rootName, out var rootBone))
            throw new InvalidOperationException($"Equipment donor {prefab.name} lacks skin root '{rootName}' for {id}.");
        var source = materialSource ?? originals.Select(renderer => renderer.sharedMaterial).FirstOrDefault(material => material);
        if (!source) source = new Material(ResolveSurfaceShader());
        source = Uncouple(source, id) ?? source;
        var root = new GameObject("magenheim." + id + ".skin") { layer = prefab.layer };
        root.transform.SetParent(attachSkin, false);
        try
        {
            var parts = (JArray)document["parts"]!;
            for (var i = 0; i < parts.Count; i++)
            {
                var data = parts[i];
                var part = new GameObject((string?)data["name"] ?? ("skin-part-" + i)) { layer = prefab.layer };
                part.transform.SetParent(root.transform, false);
                var renderer = part.AddComponent<SkinnedMeshRenderer>();
                renderer.sharedMesh = LoadSkinnedMesh(id + "/" + i, data, bones, part.transform);
                renderer.sharedMaterial = LoadMaterial(id + "/skin/" + i, data["material"]!, source!);
                renderer.bones = bones;
                renderer.rootBone = rootBone;
            }
            foreach (var renderer in originals) renderer.enabled = false;
            return root;
        }
        catch { UnityEngine.Object.DestroyImmediate(root); throw; }
    }

    private static Mesh LoadSkinnedMesh(string key, JToken part, Transform[] bones, Transform meshTransform)
    {
        var vertices = part["vertices"]!.Select(Vector).ToArray(); var triangles = part["triangles"]!.Select(v => (int)v).ToArray();
        var normals = part["normals"]!.Select(Vector).ToArray(); var uv = part["uv"]!.Select(v => new Vector2((float)v[0]!, (float)v[1]!)).ToArray();
        if (vertices.Length == 0 || triangles.Length == 0 || triangles.Length % 3 != 0 || normals.Length != vertices.Length || uv.Length != vertices.Length)
            throw new InvalidDataException("Invalid skinned model surface: " + key);
        if (!(part["skinWeights"] is JArray rows) || rows.Count != vertices.Length)
            throw new InvalidDataException("Skinned weight stream mismatch: " + key);
        var weights = new BoneWeight[vertices.Length];
        for (var vertex = 0; vertex < rows.Count; vertex++)
        {
            var influences = rows[vertex] as JArray;
            if (influences is null || influences.Count == 0 || influences.Count > 4) throw new InvalidDataException("Invalid skin influence count: " + key);
            var indices = new int[4]; var values = new float[4]; var total = 0f;
            for (var j = 0; j < influences.Count; j++)
            {
                var pair = influences[j] as JArray ?? throw new InvalidDataException("Invalid skin influence pair: " + key);
                var index = (int)pair[0]!; var weight = (float)pair[1]!;
                if (index < 0 || index >= bones.Length || !Finite(weight) || weight <= 0f) throw new InvalidDataException("Invalid skin influence: " + key);
                indices[j] = index; values[j] = weight; total += weight;
            }
            if (Math.Abs(total - 1f) > .002f) throw new InvalidDataException("Skin influences are not normalized: " + key);
            weights[vertex] = new BoneWeight { boneIndex0=indices[0],weight0=values[0],boneIndex1=indices[1],weight1=values[1],boneIndex2=indices[2],weight2=values[2],boneIndex3=indices[3],weight3=values[3] };
        }
        var mesh = new Mesh { name=key,indexFormat=vertices.Length>65535?IndexFormat.UInt32:IndexFormat.UInt16,vertices=vertices,triangles=triangles,normals=normals,uv=uv,boneWeights=weights,bindposes=bones.Select(bone=>bone.worldToLocalMatrix*meshTransform.localToWorldMatrix).ToArray() };
        mesh.RecalculateBounds(); mesh.RecalculateTangents(); return mesh;
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

    /// <summary>
    /// Texture slots a donor material can carry that describe the donor's surface, not ours.
    /// Valheim's piece and rock shaders add moss, snow and rain on top of the Standard set.
    /// </summary>
    private static readonly string[] DonorMapsToClear =
    {
        "_BumpMap", "_MetallicGlossMap", "_SpecGlossMap", "_OcclusionMap", "_ParallaxMap",
        "_DetailMask", "_DetailAlbedoMap", "_DetailNormalMap", "_EmissionMap",
        "_MossTex", "_StyleTex", "_SnowTex", "_RainTex",
    };

    private static Texture2D LoadModelTexture(string textureName, bool linear)
    {
        if (string.IsNullOrEmpty(textureName) || Path.GetFileName(textureName) != textureName)
            throw new InvalidDataException("Invalid texture path.");
        var cacheKey = (linear ? "linear:" : "srgb:") + textureName;
        if (Textures.TryGetValue(cacheKey, out var cached)) return cached;
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, true, linear)
        {
            name = textureName,
            wrapMode = TextureWrapMode.Repeat,
        };
        if (!LoadImage(texture, File.ReadAllBytes(Path.Combine(DirectoryPath, "textures", textureName))))
            throw new InvalidDataException("Invalid model texture: " + textureName);
        Textures.Add(cacheKey, texture);
        return texture;
    }

    private static Material LoadMaterial(string key, JToken data, Material source)
    {
        if (Materials.TryGetValue(key, out var cached)) return cached;
        var material = new Material(source) { name = (string?)data["name"] ?? key };
        var color = Colour(data["color"]!); material.color = color;
        material.mainTexture = Texture2D.whiteTexture; material.mainTextureScale = Vector2.one; material.mainTextureOffset = Vector2.zero;
        var textureName = (string?)data["texture"];
        // 0.0.76 deliberately exported the 1,303 obsolete generated bakes as null so the bad
        // semantic could not remain frozen into the model. Null must not mean white/untextured:
        // resolve it to the authored material-family library. An explicit texture always wins.
        if (string.IsNullOrEmpty(textureName))
            textureName = AuthoredSurfaceTextureName((string?)data["name"] ?? key);
        if (!string.IsNullOrEmpty(textureName))
            material.mainTexture = LoadModelTexture(textureName!, linear: false);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", (float)data["metallic"]!);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 1f - (float)data["roughness"]!);
        // Strip every auxiliary map the donor material brought with it. `new Material(source)` copies
        // the donor's shader *and* all of its texture slots -- normal, metallic/gloss, occlusion,
        // detail, and Valheim's moss overlay on piece shaders -- which were authored for the donor's
        // geometry and its UVs, not ours. Overriding only mainTexture left those maps sampling our
        // crystal facets through unrelated UVs, which is why structural placeables read in game as
        // flat, uniformly lit sheets with a fine speckle while the source looks correct. Only the
        // shader and our own colour/texture should survive.
        foreach (var map in DonorMapsToClear)
            if (material.HasProperty(map)) material.SetTexture(map, null);
        // Custom/StaticRock carries a non-zero parallax scalar independently of its map. Leaving
        // that donor value behind made UV-authored furniture read as offset, torn triangles even
        // after the donor texture was cleared. Owned meshes do not author displacement data.
        foreach (var scalar in new[] { "_Parallax", "_Displacement", "_DisplacementStrength", "_HeightScale", "_Tessellation" })
            if (material.HasProperty(scalar)) material.SetFloat(scalar, 0f);
        foreach (var keyword in new[] { "_NORMALMAP", "_METALLICGLOSSMAP", "_DETAIL_MULX2", "_PARALLAXMAP", "_EMISSION" })
            material.DisableKeyword(keyword);
        // Moss is blend-driven on Valheim piece shaders, so nulling its texture is not enough.
        foreach (var blend in new[] { "_MossBlend", "_MossAlpha", "_AddSnow", "_AddRain" })
            if (material.HasProperty(blend)) material.SetFloat(blend, 0f);

        // Owned Underworld PBR maps are exported as explicit runtime references. Apply them only
        // after donor maps/keywords are cleared so a donor can never win by load order.
        var normalName = (string?)data["normalTexture"];
        if (!string.IsNullOrEmpty(normalName) && material.HasProperty("_BumpMap"))
        {
            material.SetTexture("_BumpMap", LoadModelTexture(normalName!, linear: true));
            if (material.HasProperty("_BumpScale"))
                material.SetFloat("_BumpScale", (float?)data["normalScale"] ?? 1f);
            material.EnableKeyword("_NORMALMAP");
        }

        var metallicGlossName = (string?)data["metallicGlossTexture"];
        if (!string.IsNullOrEmpty(metallicGlossName) && material.HasProperty("_MetallicGlossMap"))
        {
            material.SetTexture("_MetallicGlossMap", LoadModelTexture(metallicGlossName!, linear: true));
            material.EnableKeyword("_METALLICGLOSSMAP");
        }

        var emissionMapName = (string?)data["emissionTexture"];
        if (!string.IsNullOrEmpty(emissionMapName) && material.HasProperty("_EmissionMap"))
        {
            material.SetTexture("_EmissionMap", LoadModelTexture(emissionMapName!, linear: false));
            material.EnableKeyword("_EMISSION");
        }

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
    /// <summary>File-backed neutral albedo used only when a model payload has no explicit texture.</summary>
    internal static string AuthoredSurfaceTextureName(string semantic) =>
        "surface-" + AuthoredSurfaceFamily(semantic) + "-authored.png";

    internal static string AuthoredSurfaceFamily(string semantic)
    {
        var key = SurfaceSemanticToken(semantic ?? string.Empty).ToLowerInvariant();
        if (SurfaceContainsAny(key, "water", "liquid", "solution")) return "liquid";
        if (SurfaceContainsAny(key, "hide", "leather", "pelt")) return "leather";
        if (SurfaceContainsAny(key, "stone", "marble", "rock", "earth", "strata", "slate", "basalt")) return "stone";
        if (SurfaceContainsAny(key, "wood", "timber", "root", "shaft", "bark")) return "timber";
        if (SurfaceContainsAny(key, "iron", "bronze", "silver", "gold", "metal", "band", "collar", "brace", "rail", "rim")) return "metal";
        if (SurfaceContainsAny(key, "cloth", "banner", "fabric")) return "cloth";
        if (SurfaceContainsAny(key, "bone", "ivory", "antler")) return "bone";
        if (SurfaceContainsAny(key, "crystal", "frost", "rime", "ice", "spirit", "radiance", "venom", "seidr", "fate", "eitr", "gem", "shard", "growth", "focus", "core", "light")) return "crystal";
        return "generic";
    }

    private static string SurfaceSemanticToken(string semantic)
    {
        if (string.IsNullOrEmpty(semantic)) return string.Empty;
        var segments = semantic.Split('.');
        var last = segments.Length - 1;
        while (last > 0 && SurfaceIsIndex(segments[last])) last--;
        return segments[last];
    }

    private static bool SurfaceIsIndex(string segment)
    {
        if (segment.Length == 0) return false;
        foreach (var character in segment)
            if (character < '0' || character > '9') return false;
        return true;
    }

    private static bool SurfaceContainsAny(string value, params string[] needles)
    {
        foreach (var needle in needles)
            if (value.IndexOf(needle, StringComparison.Ordinal) >= 0) return true;
        return false;
    }

    internal static Vector3 Vector(JToken v) => new((float)v[0]!, (float)v[1]!, (float)v[2]!);
    private static Color Colour(JToken v) => new((float)v[0]!, (float)v[1]!, (float)v[2]!, v.Count() > 3 ? (float)v[3]! : 1f);
    private static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
}
