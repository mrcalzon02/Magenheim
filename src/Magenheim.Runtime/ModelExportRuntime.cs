using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx.Logging;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Optional acceptance/export path for converting registered Magenheim runtime geometry
/// into inspectable OBJ model files. Disabled unless MAGENHEIM_EXPORT_MODELS=1.
/// </summary>
internal static class ModelExportRuntime
{
    private const string ExportFlag = "MAGENHEIM_EXPORT_MODELS";
    private const string ExportDirectoryVariable = "MAGENHEIM_EXPORT_DIR";
    private const string ExportExitFlag = "MAGENHEIM_EXPORT_EXIT";
    private static ManualLogSource? _logger;
    private static bool _armed;

    internal static void EnableIfRequested(ManualLogSource logger)
    {
        if (_armed) return;
        if (!string.Equals(Environment.GetEnvironmentVariable(ExportFlag), "1", StringComparison.Ordinal)) return;

        _armed = true;
        _logger = logger;
        PrefabManager.OnPrefabsRegistered += ExportRegisteredPrefabs;
        logger.LogInfo("Magenheim runtime model export armed; waiting for Jotunn prefab registration.");
    }

    private static void ExportRegisteredPrefabs()
    {
        PrefabManager.OnPrefabsRegistered -= ExportRegisteredPrefabs;

        try
        {
            var output = Environment.GetEnvironmentVariable(ExportDirectoryVariable);
            if (string.IsNullOrWhiteSpace(output))
                output = Path.Combine(Paths.PluginPath, "Magenheim", "model-export");

            Directory.CreateDirectory(output);

            var scene = ZNetScene.instance ?? throw new InvalidOperationException("ZNetScene is unavailable during model export.");
            var prefabs = scene.m_prefabs
                .Where(prefab => prefab != null && IsMagenheimPrefab(prefab))
                .GroupBy(prefab => prefab.name, StringComparer.Ordinal)
                .Select(group => group.First())
                .OrderBy(prefab => prefab.name, StringComparer.Ordinal)
                .ToArray();

            var records = new List<ExportRecord>();
            foreach (var prefab in prefabs)
            {
                var record = ExportPrefab(prefab, output);
                records.Add(record);
                _logger?.LogInfo($"Model export {record.Prefab}: meshes={record.Meshes}, vertices={record.Vertices}, triangles={record.Triangles}, file={record.File ?? "(none)"}");
            }

            var exported = records.Count(record => record.File != null);
            if (exported == 0)
                throw new InvalidOperationException("No Magenheim runtime meshes were exported.");

            WriteManifest(output, records);
            File.WriteAllText(Path.Combine(output, "SUCCESS"), $"Exported {exported} Magenheim model prefabs.{Environment.NewLine}", new UTF8Encoding(false));
            _logger?.LogInfo($"Magenheim runtime model export complete: {exported}/{records.Count} Magenheim prefabs produced OBJ geometry in '{output}'.");

            if (string.Equals(Environment.GetEnvironmentVariable(ExportExitFlag), "1", StringComparison.Ordinal))
                Application.Quit(0);
        }
        catch (Exception exception)
        {
            _logger?.LogFatal($"Magenheim runtime model export failed: {exception}");
            try
            {
                var output = Environment.GetEnvironmentVariable(ExportDirectoryVariable);
                if (!string.IsNullOrWhiteSpace(output))
                {
                    Directory.CreateDirectory(output);
                    File.WriteAllText(Path.Combine(output, "FAILED"), exception.ToString(), new UTF8Encoding(false));
                }
            }
            catch { }

            if (string.Equals(Environment.GetEnvironmentVariable(ExportExitFlag), "1", StringComparison.Ordinal))
                Application.Quit(3);
            else
                throw;
        }
    }

    private static bool IsMagenheimPrefab(GameObject prefab)
    {
        if (prefab.name.StartsWith("Magenheim_", StringComparison.OrdinalIgnoreCase)) return true;
        return prefab.GetComponentsInChildren<Transform>(true)
            .Any(transform => transform.name.StartsWith("magenheim.", StringComparison.OrdinalIgnoreCase));
    }

    private static ExportRecord ExportPrefab(GameObject prefab, string output)
    {
        var parts = new List<MeshPart>();

        foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null) continue;
            var renderer = filter.GetComponent<MeshRenderer>();
            if (!ShouldExportRenderer(prefab.transform, filter.transform, renderer)) continue;
            parts.Add(new MeshPart(filter.transform, filter.sharedMesh, renderer));
        }

        foreach (var renderer in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (renderer.sharedMesh == null) continue;
            if (!ShouldExportRenderer(prefab.transform, renderer.transform, renderer)) continue;
            parts.Add(new MeshPart(renderer.transform, renderer.sharedMesh, renderer));
        }

        if (parts.Count == 0)
            return new ExportRecord(prefab.name, null, 0, 0, 0);

        var fileName = SafeFileName(prefab.name) + ".obj";
        var path = Path.Combine(output, fileName);
        var vertexOffset = 1;
        var vertexCount = 0;
        var triangleCount = 0;

        using (var writer = new StreamWriter(path, false, new UTF8Encoding(false)))
        {
            writer.WriteLine("# Magenheim runtime mesh export");
            writer.WriteLine("# prefab: " + prefab.name);

            foreach (var part in parts)
            {
                var mesh = part.Mesh;
                if (!mesh.isReadable)
                {
                    _logger?.LogWarning($"Skipping unreadable mesh '{mesh.name}' on '{prefab.name}/{part.Transform.name}'.");
                    continue;
                }

                writer.WriteLine("o " + SafeObjName(part.Transform.name));
                var transform = prefab.transform.worldToLocalMatrix * part.Transform.localToWorldMatrix;
                var vertices = mesh.vertices;
                foreach (var vertex in vertices)
                {
                    var point = transform.MultiplyPoint3x4(vertex);
                    writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "v {0:R} {1:R} {2:R}", point.x, point.y, point.z));
                }

                vertexCount += vertices.Length;
                var materials = part.Renderer != null ? part.Renderer.sharedMaterials : Array.Empty<Material>();
                for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                {
                    if (subMesh < materials.Length && materials[subMesh] != null)
                        writer.WriteLine("g " + SafeObjName(materials[subMesh].name));

                    var triangles = mesh.GetTriangles(subMesh);
                    for (var index = 0; index + 2 < triangles.Length; index += 3)
                    {
                        writer.WriteLine($"f {triangles[index] + vertexOffset} {triangles[index + 1] + vertexOffset} {triangles[index + 2] + vertexOffset}");
                        triangleCount++;
                    }
                }

                vertexOffset += vertices.Length;
            }
        }

        if (vertexCount == 0 || triangleCount == 0)
        {
            File.Delete(path);
            return new ExportRecord(prefab.name, null, parts.Count, vertexCount, triangleCount);
        }

        return new ExportRecord(prefab.name, fileName, parts.Count, vertexCount, triangleCount);
    }

    private static bool ShouldExportRenderer(Transform prefabRoot, Transform candidate, Renderer? renderer)
    {
        var current = candidate;
        while (current != null && current != prefabRoot)
        {
            if (current.name.StartsWith("magenheim.", StringComparison.OrdinalIgnoreCase)) return true;
            current = current.parent;
        }

        return renderer == null || renderer.enabled;
    }

    private static void WriteManifest(string output, IReadOnlyList<ExportRecord> records)
    {
        var json = new StringBuilder();
        json.AppendLine("{");
        json.AppendLine("  \"schema\": 1,");
        json.AppendLine("  \"source\": \"registered Valheim/Jotunn runtime prefabs\",");
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
