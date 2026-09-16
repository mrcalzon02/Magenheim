using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// One shared geode visual for every biome. The stone shell is a continuous cutaway solid;
/// only the recessed crystal interior palette changes by biome.
/// </summary>
internal static class GeodeVisuals
{
    private const int OpeningFace = 8;
    private const float ShellRadius = 0.56f;
    private const float InnerRadius = 0.45f;
    private const float VerticalScale = 0.88f;

    private static readonly Vector3[] BaseVertices =
    {
        new(-1f,-1f,-1f), new(-1f,-1f, 1f), new(-1f, 1f,-1f), new(-1f, 1f, 1f),
        new( 1f,-1f,-1f), new( 1f,-1f, 1f), new( 1f, 1f,-1f), new( 1f, 1f, 1f),
        new(0f,-0.61803399f,-1.61803399f), new(0f,-0.61803399f, 1.61803399f),
        new(0f, 0.61803399f,-1.61803399f), new(0f, 0.61803399f, 1.61803399f),
        new(-0.61803399f,-1.61803399f,0f), new(-0.61803399f, 1.61803399f,0f),
        new( 0.61803399f,-1.61803399f,0f), new( 0.61803399f, 1.61803399f,0f),
        new(-1.61803399f,0f,-0.61803399f), new(-1.61803399f,0f, 0.61803399f),
        new( 1.61803399f,0f,-0.61803399f), new( 1.61803399f,0f, 0.61803399f)
    };

    private static readonly int[][] Faces =
    {
        new[] { 6,18,4,8,10 }, new[] { 10,8,0,16,2 }, new[] { 0,12,1,17,16 },
        new[] { 3,13,2,16,17 }, new[] { 14,5,9,1,12 }, new[] { 4,14,12,0,8 },
        new[] { 18,19,5,14,4 }, new[] { 9,11,3,17,1 }, new[] { 5,19,7,11,9 },
        new[] { 15,6,10,2,13 }, new[] { 19,18,6,15,7 }, new[] { 7,15,13,3,11 }
    };

    private static Mesh? _outerShell;
    private static Mesh? _innerCavity;
    private static Mesh? _openingRim;
    private static Mesh? _crystalShard;
    private static readonly Dictionary<string, Sprite> Icons = new(StringComparer.Ordinal);

    internal static void Apply(GameObject prefab, Color interiorTint, float scale = 1f, bool worldObject = false)
    {
        if (!prefab) throw new ArgumentNullException(nameof(prefab));
        if (scale <= 0f || float.IsNaN(scale) || float.IsInfinity(scale)) throw new ArgumentOutOfRangeException(nameof(scale));
        var originalRenderers = prefab.GetComponentsInChildren<Renderer>(true);
        var source = originalRenderers.Select(renderer => renderer.sharedMaterial).FirstOrDefault(material => material)
            ?? throw new InvalidOperationException($"No material source exists on geode prefab '{prefab.name}'.");
        var root = new GameObject("magenheim.geode.shared.visual") { layer = prefab.layer };
        root.transform.SetParent(worldObject ? prefab.transform : prefab.transform.Find("attach") ?? prefab.transform, false);
        root.transform.localScale = Vector3.one * scale;
        root.transform.localPosition = worldObject ? new Vector3(0f, 0.50f * scale, 0f) : Vector3.zero;
        root.transform.localRotation = Quaternion.Euler(-7f, 18f, 3f);
        var crack = Material(source, "cavity", new Color(0.055f, 0.050f, 0.047f, 1f), 0f, 0.08f);
        var shell = Material(source, "shell", new Color(0.265f, 0.248f, 0.228f, 1f), 0.02f, 0.10f);
        var rim = Material(source, "rim", new Color(0.205f, 0.195f, 0.185f, 1f), 0.01f, 0.08f);
        var interior = Material(source, "interior-" + ColorUtility.ToHtmlStringRGB(interiorTint), interiorTint, 0.03f, 0.58f, 0.22f);
        var interiorBright = Material(source, "interior-bright-" + ColorUtility.ToHtmlStringRGB(interiorTint), Color.Lerp(interiorTint, Color.white, 0.28f), 0.02f, 0.72f, 0.34f);
        Part(root, "stone-shell", OuterShell(), Vector3.zero, Quaternion.identity, shell);
        Part(root, "stone-cavity", InnerCavity(), Vector3.zero, Quaternion.identity, crack);
        Part(root, "stone-opening-rim", OpeningRim(), Vector3.zero, Quaternion.identity, rim);
        AddInteriorCluster(root, interior, interiorBright);
        foreach (var lod in prefab.GetComponentsInChildren<LODGroup>(true)) lod.enabled = false;
        foreach (var renderer in originalRenderers) renderer.enabled = false;
        if (worldObject)
        {
            foreach (var inheritedCollider in prefab.GetComponentsInChildren<Collider>(true)) if (!inheritedCollider.isTrigger) inheritedCollider.enabled = false;
            var collider = prefab.AddComponent<SphereCollider>(); collider.center = new Vector3(0f, 0.50f * scale, 0f); collider.radius = 0.63f * scale;
        }
    }

    internal static Sprite Icon(string biomeKey, Color interiorTint)
    {
        var key = biomeKey + "|" + ColorUtility.ToHtmlStringRGB(interiorTint);
        if (Icons.TryGetValue(key, out var cached)) return cached;
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "magenheim.geode." + biomeKey + ".icon", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var pixels = Enumerable.Repeat(Color.clear, size * size).ToArray(); var center = new Vector2(63.5f, 62f);
        for (var y = 0; y < size; y++) for (var x = 0; x < size; x++)
        {
            var dx = (x - center.x) / 52f; var dy = (y - center.y) / 48f; var angle = Mathf.Atan2(dy, dx); var radius = Mathf.Sqrt(dx * dx + dy * dy);
            var boundary = 0.94f + 0.035f * Mathf.Cos(angle * 12f) + 0.018f * Mathf.Cos(angle * 5f + 0.8f); if (radius > boundary) continue;
            var light = Mathf.Clamp01(0.50f + (x / (float)size) * 0.22f + (y / (float)size) * 0.10f);
            pixels[y * size + x] = Color.Lerp(new Color(0.13f,0.12f,0.115f,1f), new Color(0.40f,0.37f,0.34f,1f), light);
        }
        var cutawayOuter = new[] { new Vector2(69,30), new Vector2(101,38), new Vector2(111,62), new Vector2(96,88), new Vector2(70,84), new Vector2(57,57) };
        FillPolygon(pixels, size, cutawayOuter, new Color(0.045f,0.04f,0.038f,1f));
        var cutawayInner = new[] { new Vector2(72,36), new Vector2(96,42), new Vector2(103,61), new Vector2(91,80), new Vector2(73,77), new Vector2(63,57) };
        FillPolygon(pixels, size, cutawayInner, interiorTint);
        FillPolygon(pixels, size, new[] { new Vector2(71,58), new Vector2(80,40), new Vector2(86,61), new Vector2(77,76) }, Color.Lerp(interiorTint, Color.white, .24f));
        FillPolygon(pixels, size, new[] { new Vector2(84,59), new Vector2(94,45), new Vector2(99,62), new Vector2(90,77) }, Color.Lerp(interiorTint, Color.white, .10f));
        FillPolygon(pixels, size, new[] { new Vector2(66,59), new Vector2(74,47), new Vector2(77,62), new Vector2(72,73) }, Color.Lerp(interiorTint, Color.black, .14f));
        var crackColor = new Color(0.035f,0.032f,0.03f,1f);
        DrawCrack(pixels,size,19,60,47,57,crackColor,3); DrawCrack(pixels,size,47,57,59,44,crackColor,3); DrawCrack(pixels,size,47,57,54,83,crackColor,3);
        DrawCrack(pixels,size,54,83,37,101,crackColor,3); DrawCrack(pixels,size,27,35,47,57,crackColor,3); DrawCrack(pixels,size,30,86,54,83,crackColor,3); DrawCrack(pixels,size,53,21,59,44,crackColor,3);
        texture.SetPixels(pixels); texture.Apply(false, false);
        var sprite = Sprite.Create(texture, new Rect(0,0,size,size), new Vector2(.5f,.5f), 128f); sprite.name = "magenheim.geode." + biomeKey + ".icon"; Icons.Add(key, sprite); return sprite;
    }

    private static void AddInteriorCluster(GameObject root, Material interior, Material bright)
    {
        var face = Faces[OpeningFace]; var openingCenter = Vector3.zero; foreach (var index in face) openingCenter += Vertex(index, InnerRadius); openingCenter /= face.Length;
        var normal = openingCenter.normalized; var orientation = Quaternion.FromToRotation(Vector3.up, normal); var baseCenter = openingCenter - normal * .18f;
        var offsets = new[] { new Vector3(-.075f,-.035f,-.010f), new Vector3(.060f,-.055f,.020f), new Vector3(.000f,.055f,-.018f), new Vector3(-.040f,.095f,.020f), new Vector3(.080f,.045f,-.012f) };
        var scales = new[] { new Vector3(.070f,.22f,.070f), new Vector3(.058f,.17f,.058f), new Vector3(.065f,.25f,.065f), new Vector3(.048f,.14f,.048f), new Vector3(.052f,.18f,.052f) };
        for (var i = 0; i < offsets.Length; i++) Part(root, "interior-crystal-" + i, CrystalShard(), baseCenter + orientation * offsets[i], orientation * Quaternion.Euler(0f, i * 31f, i % 2 == 0 ? 6f : -7f), i % 2 == 0 ? bright : interior, scales[i]);
    }

    private static Mesh OuterShell()
    {
        if (_outerShell) return _outerShell; var vertices = BaseVertices.Select((_, index) => Vertex(index, ShellRadius)).ToArray(); var triangles = new List<int>();
        for (var faceIndex = 0; faceIndex < Faces.Length; faceIndex++) { if (faceIndex == OpeningFace) continue; AddPentagon(triangles, vertices, Faces[faceIndex], true); }
        _outerShell = BuildMesh("magenheim.geode.outer-shell", vertices, triangles); return _outerShell;
    }

    private static Mesh InnerCavity()
    {
        if (_innerCavity) return _innerCavity; var vertices = BaseVertices.Select((_, index) => Vertex(index, InnerRadius)).ToArray(); var triangles = new List<int>();
        for (var faceIndex = 0; faceIndex < Faces.Length; faceIndex++) { if (faceIndex == OpeningFace) continue; AddPentagon(triangles, vertices, Faces[faceIndex], false); }
        _innerCavity = BuildMesh("magenheim.geode.inner-cavity", vertices, triangles); return _innerCavity;
    }

    private static Mesh OpeningRim()
    {
        if (_openingRim) return _openingRim; var face = Faces[OpeningFace]; var vertices = new List<Vector3>(10); foreach (var index in face) vertices.Add(Vertex(index, ShellRadius)); foreach (var index in face) vertices.Add(Vertex(index, InnerRadius));
        var triangles = new List<int>(30); for (var i = 0; i < 5; i++) { var next = (i + 1) % 5; var outerA = i; var outerB = next; var innerA = 5 + i; var innerB = 5 + next; triangles.Add(outerA); triangles.Add(innerA); triangles.Add(innerB); triangles.Add(outerA); triangles.Add(innerB); triangles.Add(outerB); triangles.Add(outerA); triangles.Add(innerB); triangles.Add(innerA); triangles.Add(outerA); triangles.Add(outerB); triangles.Add(innerB); }
        _openingRim = BuildMesh("magenheim.geode.opening-rim", vertices.ToArray(), triangles); return _openingRim;
    }

    private static void AddPentagon(List<int> triangles, IReadOnlyList<Vector3> vertices, IReadOnlyList<int> face, bool outwardFromOrigin)
    {
        var ordered = face.ToArray(); var a = vertices[ordered[0]]; var b = vertices[ordered[1]]; var c = vertices[ordered[2]]; var centroid = Vector3.zero; foreach (var index in ordered) centroid += vertices[index]; centroid /= ordered.Length;
        var pointsOutward = Vector3.Dot(Vector3.Cross(b - a, c - a), centroid) > 0f; if (pointsOutward != outwardFromOrigin) Array.Reverse(ordered);
        for (var i = 1; i < ordered.Length - 1; i++) { triangles.Add(ordered[0]); triangles.Add(ordered[i]); triangles.Add(ordered[i + 1]); }
    }

    private static Mesh CrystalShard()
    {
        if (_crystalShard) return _crystalShard; const int sides = 6; var vertices = new List<Vector3>();
        for (var i = 0; i < sides; i++) { var angle = Mathf.PI * 2f * i / sides; vertices.Add(new Vector3(Mathf.Cos(angle) * .5f, 0f, Mathf.Sin(angle) * .5f)); vertices.Add(new Vector3(Mathf.Cos(angle) * .42f, .72f, Mathf.Sin(angle) * .42f)); }
        vertices.Add(new Vector3(0f,1f,0f)); var tip = vertices.Count - 1; var triangles = new List<int>();
        for (var i = 0; i < sides; i++) { var next = (i + 1) % sides; var b0 = i * 2; var t0 = b0 + 1; var b1 = next * 2; var t1 = b1 + 1; triangles.Add(b0); triangles.Add(b1); triangles.Add(t0); triangles.Add(b1); triangles.Add(t1); triangles.Add(t0); triangles.Add(t0); triangles.Add(t1); triangles.Add(tip); }
        for (var i = 1; i < sides - 1; i++) { triangles.Add(0); triangles.Add(i * 2); triangles.Add((i + 1) * 2); }
        _crystalShard = BuildMesh("magenheim.geode.interior-crystal", vertices.ToArray(), triangles); return _crystalShard;
    }

    private static Mesh BuildMesh(string name, Vector3[] vertices, List<int> triangles) { var mesh = new Mesh { name = name, vertices = vertices, triangles = triangles.ToArray() }; mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh; }
    private static Vector3 Vertex(int index, float radius) { var direction = BaseVertices[index].normalized * radius; direction.y *= VerticalScale; return direction; }
    private static void Part(GameObject root, string name, Mesh mesh, Vector3 position, Quaternion rotation, Material material, Vector3? scale = null) { var part = new GameObject(name) { layer = root.layer }; part.transform.SetParent(root.transform, false); part.transform.localPosition = position; part.transform.localRotation = rotation; part.transform.localScale = scale ?? Vector3.one; part.AddComponent<MeshFilter>().sharedMesh = mesh; part.AddComponent<MeshRenderer>().sharedMaterial = material; }

    private static Material Material(Material source, string key, Color color, float metallic, float gloss, float emission = 0f)
    {
        var material = new Material(source) { name = "magenheim.geode." + key };
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", gloss);
        if (material.HasProperty("_EmissionColor") && emission > 0f) { material.SetColor("_EmissionColor", color * emission); material.EnableKeyword("_EMISSION"); }
        else material.DisableKeyword("_EMISSION");
        material.SetOverrideTag("RenderType", "Opaque"); if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 1f); material.renderQueue = 2000; return material;
    }

    private static void FillPolygon(Color[] pixels, int size, Vector2[] polygon, Color color) { var minX = Mathf.Clamp(Mathf.FloorToInt(polygon.Min(p => p.x)), 0, size - 1); var maxX = Mathf.Clamp(Mathf.CeilToInt(polygon.Max(p => p.x)), 0, size - 1); var minY = Mathf.Clamp(Mathf.FloorToInt(polygon.Min(p => p.y)), 0, size - 1); var maxY = Mathf.Clamp(Mathf.CeilToInt(polygon.Max(p => p.y)), 0, size - 1); for (var y = minY; y <= maxY; y++) for (var x = minX; x <= maxX; x++) if (InsidePolygon(new Vector2(x + .5f, y + .5f), polygon)) pixels[y * size + x] = color; }
    private static bool InsidePolygon(Vector2 point, Vector2[] polygon) { var inside = false; for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++) { var a = polygon[i]; var b = polygon[j]; if (((a.y > point.y) != (b.y > point.y)) && point.x < (b.x - a.x) * (point.y - a.y) / ((b.y - a.y) + .00001f) + a.x) inside = !inside; } return inside; }
    private static void DrawCrack(Color[] pixels, int size, int x0, int y0, int x1, int y1, Color color, int thickness) { var dx = Math.Abs(x1-x0); var sx=x0<x1?1:-1; var dy=-Math.Abs(y1-y0); var sy=y0<y1?1:-1; var err=dx+dy; while(true) { for(var oy=-thickness/2;oy<=thickness/2;oy++) for(var ox=-thickness/2;ox<=thickness/2;ox++) { var x=x0+ox; var y=y0+oy; if(x>=0&&x<size&&y>=0&&y<size) pixels[y*size+x]=color; } if(x0==x1&&y0==y1) break; var e2=2*err; if(e2>=dy){err+=dy;x0+=sx;} if(e2<=dx){err+=dx;y0+=sy;} } }
}
