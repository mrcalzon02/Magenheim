using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Tools.CinematicDonorExporter;

[BepInPlugin("mrcalzon02.magenheim.cinematic-donor-exporter", "Magenheim Cinematic Donor Exporter", "1.0.0")]
[BepInDependency(Jotunn.Main.ModGuid, BepInDependency.DependencyFlags.HardDependency)]
public sealed class CinematicDonorExporterPlugin : BaseUnityPlugin
{
    private const string ExportDirectoryVariable = "MAGENHEIM_EXPORT_DIR";
    private const string ExportPrefabsVariable = "MAGENHEIM_EXPORT_PREFABS";
    private const string ExportExitVariable = "MAGENHEIM_EXPORT_EXIT";
    private bool _armed;

    private void Awake()
    {
        if (_armed) return;
        _armed = true;
        PrefabManager.OnPrefabsRegistered += ExportRegisteredPrefabs;
        Logger.LogInfo("Cinematic donor exporter armed; waiting for registered Valheim prefabs.");
    }

    private void OnDestroy()
    {
        if (_armed)
            PrefabManager.OnPrefabsRegistered -= ExportRegisteredPrefabs;
    }

    private void ExportRegisteredPrefabs()
    {
        PrefabManager.OnPrefabsRegistered -= ExportRegisteredPrefabs;
        _armed = false;
        string? output = null;
        try
        {
            output = Environment.GetEnvironmentVariable(ExportDirectoryVariable);
            if (string.IsNullOrWhiteSpace(output))
                throw new InvalidOperationException(ExportDirectoryVariable + " is required.");
            Directory.CreateDirectory(output);

            var requested = ParseRequestedPrefabs();
            if (requested.Count == 0)
                throw new InvalidOperationException(ExportPrefabsVariable + " contains no prefab identities.");

            var scene = ZNetScene.instance ?? throw new InvalidOperationException("ZNetScene is unavailable during cinematic donor export.");
            var available = scene.m_prefabs
                .Where(prefab => prefab != null)
                .Select(prefab => prefab.name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            File.WriteAllLines(Path.Combine(output, "available-prefabs.txt"), available, new UTF8Encoding(false));

            var requestedPrefabs = scene.m_prefabs
                .Where(prefab => prefab != null && requested.Contains(prefab.name))
                .GroupBy(prefab => prefab.name, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(prefab => prefab.name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var found = new HashSet<string>(requestedPrefabs.Select(prefab => prefab.name), StringComparer.OrdinalIgnoreCase);
            var missing = requested.Where(name => !found.Contains(name)).OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
            if (missing.Length > 0)
                Logger.LogWarning("Requested donor alias candidate(s) not registered: " + string.Join(", ", missing));

            var records = new List<ExportRecord>();
            foreach (var prefab in requestedPrefabs)
            {
                var record = ExportPrefab(prefab, output);
                records.Add(record);
                Logger.LogInfo($"Cinematic donor export {record.Prefab}: meshes={record.Meshes}, vertices={record.Vertices}, triangles={record.Triangles}, file={record.File ?? "(none)"}");
            }

            if (records.Count(record => record.File != null) == 0)
                throw new InvalidOperationException("No requested Valheim donor prefab produced readable mesh geometry.");

            WriteManifest(output, records);
            File.WriteAllText(Path.Combine(output, "SUCCESS"), $"Exported {records.Count(record => record.File != null)} selected Valheim donor prefabs.{Environment.NewLine}", new UTF8Encoding(false));
            Logger.LogInfo("Cinematic donor export complete.");
            if (string.Equals(Environment.GetEnvironmentVariable(ExportExitVariable), "1", StringComparison.Ordinal))
                Application.Quit(0);
        }
        catch (Exception exception)
        {
            Logger.LogFatal("Cinematic donor export failed: " + exception);
            if (!string.IsNullOrWhiteSpace(output))
            {
                try
                {
                    Directory.CreateDirectory(output);
                    File.WriteAllText(Path.Combine(output, "FAILED"), exception.ToString(), new UTF8Encoding(false));
                }
                catch { }
            }
            if (string.Equals(Environment.GetEnvironmentVariable(ExportExitVariable), "1", StringComparison.Ordinal))
                Application.Quit(3);
        }
    }

    private static HashSet<string> ParseRequestedPrefabs()
    {
        var raw = Environment.GetEnvironmentVariable(ExportPrefabsVariable);
        if (string.IsNullOrWhiteSpace(raw))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return new HashSet<string>(
            raw.Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
               .Select(value => value.Trim())
               .Where(value => value.Length > 0),
            StringComparer.OrdinalIgnoreCase);
    }

    private ExportRecord ExportPrefab(GameObject prefab, string output)
    {
        var parts = new List<MeshPart>();
        foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null) continue;
            var renderer = filter.GetComponent<MeshRenderer>();
            if (renderer != null && !renderer.enabled) continue;
            parts.Add(new MeshPart(filter.transform, filter.sharedMesh, renderer));
        }
        foreach (var renderer in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (renderer.sharedMesh == null || !renderer.enabled) continue;
            parts.Add(new MeshPart(renderer.transform, renderer.sharedMesh, renderer));
        }

        var safePrefab = SafeFileName(prefab.name);
        var fileName = safePrefab + ".obj";
        var objPath = Path.Combine(output, fileName);
        var mtlName = safePrefab + ".mtl";
        var mtlPath = Path.Combine(output, mtlName);
        var vertexOffset = 1;
        var uvOffset = 1;
        var normalOffset = 1;
        var vertexCount = 0;
        var triangleCount = 0;
        var exportedMaterials = new Dictionary<Material, MaterialRecord>();

        using (var writer = new StreamWriter(objPath, false, new UTF8Encoding(false)))
        {
            writer.WriteLine("# Runtime-extracted Valheim cinematic donor");
            writer.WriteLine("# prefab: " + prefab.name);
            writer.WriteLine("mtllib " + mtlName);

            foreach (var part in parts)
            {
                var mesh = part.Mesh;
                if (!mesh.isReadable)
                {
                    Logger.LogWarning($"Skipping unreadable mesh '{mesh.name}' on '{prefab.name}/{part.Transform.name}'.");
                    continue;
                }

                writer.WriteLine("o " + SafeObjName(part.Transform.name));
                var transform = prefab.transform.worldToLocalMatrix * part.Transform.localToWorldMatrix;
                var normalMatrix = transform.inverse.transpose;
                var vertices = mesh.vertices;
                var uvs = mesh.uv ?? Array.Empty<Vector2>();
                var normals = mesh.normals ?? Array.Empty<Vector3>();
                var hasUvs = uvs.Length == vertices.Length;
                var hasNormals = normals.Length == vertices.Length;

                foreach (var vertex in vertices)
                {
                    var point = transform.MultiplyPoint3x4(vertex);
                    writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "v {0:R} {1:R} {2:R}", point.x, point.y, point.z));
                }
                if (hasUvs)
                    foreach (var uv in uvs)
                        writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "vt {0:R} {1:R}", uv.x, uv.y));
                if (hasNormals)
                    foreach (var normal in normals)
                    {
                        var n = normalMatrix.MultiplyVector(normal).normalized;
                        writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "vn {0:R} {1:R} {2:R}", n.x, n.y, n.z));
                    }

                vertexCount += vertices.Length;
                var materials = part.Renderer != null ? part.Renderer.sharedMaterials : Array.Empty<Material>();
                for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                {
                    Material? material = subMesh < materials.Length ? materials[subMesh] : null;
                    if (material != null)
                    {
                        if (!exportedMaterials.TryGetValue(material, out var materialRecord))
                        {
                            materialRecord = ExportMaterial(prefab.name, material, output, exportedMaterials.Count);
                            exportedMaterials.Add(material, materialRecord);
                        }
                        writer.WriteLine("usemtl " + materialRecord.Name);
                    }

                    var triangles = mesh.GetTriangles(subMesh);
                    for (var index = 0; index + 2 < triangles.Length; index += 3)
                    {
                        writer.WriteLine(
                            "f " +
                            ObjIndex(triangles[index], vertexOffset, uvOffset, normalOffset, hasUvs, hasNormals) + " " +
                            ObjIndex(triangles[index + 1], vertexOffset, uvOffset, normalOffset, hasUvs, hasNormals) + " " +
                            ObjIndex(triangles[index + 2], vertexOffset, uvOffset, normalOffset, hasUvs, hasNormals));
                        triangleCount++;
                    }
                }

                vertexOffset += vertices.Length;
                if (hasUvs) uvOffset += uvs.Length;
                if (hasNormals) normalOffset += normals.Length;
            }
        }

        if (vertexCount == 0 || triangleCount == 0)
        {
            File.Delete(objPath);
            if (File.Exists(mtlPath)) File.Delete(mtlPath);
            return new ExportRecord(prefab.name, null, parts.Count, vertexCount, triangleCount);
        }

        WriteMaterialLibrary(mtlPath, exportedMaterials.Values);
        return new ExportRecord(prefab.name, fileName, parts.Count, vertexCount, triangleCount);
    }

    private MaterialRecord ExportMaterial(string prefabName, Material material, string output, int ordinal)
    {
        var name = SafeObjName(prefabName + "_" + material.name + "_" + ordinal);
        var color = material.HasProperty("_Color") ? material.color : Color.white;
        var roughness = material.HasProperty("_Glossiness") ? 1f - material.GetFloat("_Glossiness") : 0.7f;
        return new MaterialRecord(
            name,
            color,
            roughness,
            ExportTexture(prefabName, material.GetTexture("_MainTex"), output, name + "-albedo"),
            ExportTexture(prefabName, material.GetTexture("_BumpMap"), output, name + "-normal"),
            ExportTexture(prefabName, material.GetTexture("_EmissionMap"), output, name + "-emission"));
    }

    private string? ExportTexture(string prefabName, Texture? texture, string output, string stem)
    {
        if (texture == null || texture.width <= 0 || texture.height <= 0) return null;
        var fileName = SafeFileName(stem) + ".png";
        var path = Path.Combine(output, fileName);
        RenderTexture? temporary = null;
        var previous = RenderTexture.active;
        Texture2D? readable = null;
        try
        {
            temporary = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            Graphics.Blit(texture, temporary);
            RenderTexture.active = temporary;
            readable = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0, false);
            readable.Apply(false, false);
            File.WriteAllBytes(path, readable.EncodeToPNG());
            return fileName;
        }
        catch (Exception exception)
        {
            Logger.LogWarning($"Texture export skipped for '{prefabName}/{texture.name}': {exception.Message}");
            return null;
        }
        finally
        {
            RenderTexture.active = previous;
            if (temporary != null) RenderTexture.ReleaseTemporary(temporary);
            if (readable != null) UnityEngine.Object.DestroyImmediate(readable);
        }
    }

    private static string ObjIndex(int localIndex, int vertexOffset, int uvOffset, int normalOffset, bool hasUvs, bool hasNormals)
    {
        var v = localIndex + vertexOffset;
        if (hasUvs && hasNormals) return $"{v}/{localIndex + uvOffset}/{localIndex + normalOffset}";
        if (hasUvs) return $"{v}/{localIndex + uvOffset}";
        if (hasNormals) return $"{v}//{localIndex + normalOffset}";
        return v.ToString(CultureInfo.InvariantCulture);
    }

    private static void WriteMaterialLibrary(string path, IEnumerable<MaterialRecord> materials)
    {
        using var writer = new StreamWriter(path, false, new UTF8Encoding(false));
        foreach (var material in materials)
        {
            writer.WriteLine("newmtl " + material.Name);
            writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "Kd {0:R} {1:R} {2:R}", material.Color.r, material.Color.g, material.Color.b));
            writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "d {0:R}", material.Color.a));
            writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "Ns {0:R}", Math.Max(0f, Math.Min(1f, 1f - material.Roughness)) * 1000f));
            if (material.Albedo != null) writer.WriteLine("map_Kd " + material.Albedo);
            if (material.Normal != null) writer.WriteLine("map_Bump " + material.Normal);
            if (material.Emission != null) writer.WriteLine("map_Ke " + material.Emission);
            writer.WriteLine();
        }
    }

    private static void WriteManifest(string output, IReadOnlyList<ExportRecord> records)
    {
        var json = new StringBuilder();
        json.AppendLine("{");
        json.AppendLine("  \"schema\": 1,");
        json.AppendLine("  \"source\": \"registered Valheim runtime prefabs\",");
        json.AppendLine("  \"models\": [");
        for (var index = 0; index < records.Count; index++)
        {
            var record = records[index];
            json.Append("    { \"prefab\": \"").Append(JsonEscape(record.Prefab)).Append("\", \"file\": ");
            if (record.File == null) json.Append("null");
            else json.Append("\"").Append(JsonEscape(record.File)).Append("\"");
            json.Append(", \"meshes\": ").Append(record.Meshes)
                .Append(", \"vertices\": ").Append(record.Vertices)
                .Append(", \"triangles\": ").Append(record.Triangles).Append(" }");
            if (index + 1 < records.Count) json.Append(',');
            json.AppendLine();
        }
        json.AppendLine("  ]");
        json.AppendLine("}");
        File.WriteAllText(Path.Combine(output, "manifest.json"), json.ToString(), new UTF8Encoding(false));
    }

    private static string SafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(value.Select(character => invalid.Contains(character) ? '_' : character).ToArray());
    }

    private static string SafeObjName(string value) => value.Replace(' ', '_').Replace('\t', '_').Replace('\r', '_').Replace('\n', '_');
    private static string JsonEscape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private sealed class MeshPart
    {
        internal MeshPart(Transform transform, Mesh mesh, Renderer? renderer)
        {
            Transform = transform;
            Mesh = mesh;
            Renderer = renderer;
        }
        internal Transform Transform { get; }
        internal Mesh Mesh { get; }
        internal Renderer? Renderer { get; }
    }

    private sealed class MaterialRecord
    {
        internal MaterialRecord(string name, Color color, float roughness, string? albedo, string? normal, string? emission)
        {
            Name = name;
            Color = color;
            Roughness = roughness;
            Albedo = albedo;
            Normal = normal;
            Emission = emission;
        }
        internal string Name { get; }
        internal Color Color { get; }
        internal float Roughness { get; }
        internal string? Albedo { get; }
        internal string? Normal { get; }
        internal string? Emission { get; }
    }

    private sealed class ExportRecord
    {
        internal ExportRecord(string prefab, string? file, int meshes, int vertices, int triangles)
        {
            Prefab = prefab;
            File = file;
            Meshes = meshes;
            Vertices = vertices;
            Triangles = triangles;
        }
        internal string Prefab { get; }
        internal string? File { get; }
        internal int Meshes { get; }
        internal int Vertices { get; }
        internal int Triangles { get; }
    }
}
