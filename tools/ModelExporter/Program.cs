using System.Reflection;
using System.Text;
using UnityEngine;

namespace Magenheim.ModelExporter;

internal static class Program
{
    private static int Main(string[] args)
    {
        var output = GetArg(args, "--output") ?? Path.Combine(Directory.GetCurrentDirectory(), "exported-models");
        var generated = Path.Combine(output, "procedural");
        var existing = Path.Combine(output, "existing-assets");
        Directory.CreateDirectory(generated);
        Directory.CreateDirectory(existing);
        CopyExistingObjAssets(existing);

        var manifest = new List<string>();
        var failures = new List<string>();
        var visualTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.Namespace == "Magenheim.Runtime" && t.Name.EndsWith("Visuals", StringComparison.Ordinal))
            .OrderBy(t => t.Name)
            .ToArray();

        foreach (var type in visualTypes)
        {
            var applies = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.Name == "Apply")
                .OrderBy(m => m.GetParameters().Length)
                .ToArray();

            foreach (var apply in applies)
            {
                var p = apply.GetParameters();
                if (p.Length == 0 || p[0].ParameterType != typeof(GameObject)) continue;

                if (p.Length >= 2 && p[1].ParameterType == typeof(string))
                {
                    var ids = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                        .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
                        .Select(f => (Field: f.Name, Id: (string?)f.GetRawConstantValue()))
                        .Where(x => !string.IsNullOrWhiteSpace(x.Id))
                        .DistinctBy(x => x.Id)
                        .ToArray();
                    foreach (var model in ids)
                        ExportInvocation(type, apply, model.Id!, model.Field, model.Id!, output, generated, manifest, failures);
                    continue;
                }

                if (p.Length >= 2 && p[1].ParameterType.IsEnum)
                {
                    foreach (var value in Enum.GetValues(p[1].ParameterType))
                    {
                        var suffix = value!.ToString()!.ToLowerInvariant();
                        var id = TypeId(type) + "-" + suffix;
                        ExportInvocation(type, apply, id, suffix, value, output, generated, manifest, failures);
                    }
                    continue;
                }

                if (p.Length == 1)
                {
                    var id = TypeId(type);
                    ExportInvocation(type, apply, id, "default", null, output, generated, manifest, failures);
                    continue;
                }

                if (p.Length >= 2 && p[1].ParameterType == typeof(Color))
                {
                    var id = TypeId(type) + "-sample";
                    ExportInvocation(type, apply, id, "sample", new Color(.55f, .86f, .94f, 1f), output, generated, manifest, failures);
                }
            }
        }

        var existingObjs = Directory.EnumerateFiles(existing, "*.obj", SearchOption.AllDirectories).Count();
        File.WriteAllLines(Path.Combine(output, "MODEL_MANIFEST.tsv"),
            new[] { "kind\tmodel_id\tsource\tmesh_parts\tfile" }.Concat(manifest));
        File.WriteAllLines(Path.Combine(output, "EXPORT_FAILURES.txt"), failures.Count == 0 ? new[] { "none" } : failures);
        File.WriteAllText(Path.Combine(output, "README.txt"),
            $"Magenheim model export\n\nExisting checked-in OBJ files copied: {existingObjs}\nProcedural OBJ files exported from runtime visual authority: {manifest.Count}\nFailures: {failures.Count}\n\nEach procedural OBJ is produced by compiling and executing the repository's actual visual geometry code against a lightweight geometry-only Unity/Valheim shim.\n");

        Console.WriteLine($"Existing OBJ: {existingObjs}");
        Console.WriteLine($"Procedural OBJ: {manifest.Count}");
        Console.WriteLine($"Failures: {failures.Count}");
        foreach (var failure in failures) Console.Error.WriteLine("EXPORT FAILURE: " + failure);
        if (manifest.Count == 0) return 2;
        return failures.Count == 0 ? 0 : 3;
    }

    private static void ExportInvocation(
        Type type, MethodInfo apply, string id, string label, object? second,
        string output, string generated, List<string> manifest, List<string> failures)
    {
        try
        {
            var host = CreateHost(type.Name + "." + label);
            var args = BuildArguments(apply, host, second);
            var result = apply.Invoke(null, args);
            var root = result as GameObject ?? host;
            var path = Path.Combine(generated, Safe(id) + ".obj");
            var meshes = ExportObj(root, path);
            if (meshes == 0) throw new InvalidOperationException("Apply produced no MeshFilter geometry.");
            manifest.Add($"PROCEDURAL\t{id}\t{type.Name}\t{meshes}\t{Relative(output, path)}");
        }
        catch (Exception ex)
        {
            failures.Add($"{type.Name}.{apply.Name} [{label}]: {Unwrap(ex).Message}");
        }
    }

    private static object?[] BuildArguments(MethodInfo method, GameObject host, object? second)
    {
        var p = method.GetParameters();
        var values = new object?[p.Length];
        values[0] = host;
        if (p.Length >= 2) values[1] = second;
        for (var i = 2; i < p.Length; i++)
        {
            if (p[i].HasDefaultValue) values[i] = p[i].DefaultValue;
            else if (p[i].ParameterType == typeof(bool)) values[i] = false;
            else if (p[i].ParameterType == typeof(float)) values[i] = 1f;
            else if (p[i].ParameterType.IsValueType) values[i] = Activator.CreateInstance(p[i].ParameterType);
            else values[i] = null;
        }
        return values;
    }

    private static GameObject CreateHost(string name)
    {
        var host = new GameObject(name);
        host.AddComponent<MeshRenderer>().sharedMaterial = new Material { name = "export-source", color = Color.white };
        var attach = new GameObject("attach");
        attach.transform.SetParent(host.transform, false);
        var turretBody = new GameObject("turret-body");
        turretBody.transform.SetParent(host.transform, false);
        host.AddComponent<Turret>().m_turretBody = turretBody;
        return host;
    }

    private static int ExportObj(GameObject root, string path)
    {
        var filters = root.GetComponentsInChildren<MeshFilter>(true)
            .Where(f => f.sharedMesh is { vertices.Length: > 0, triangles.Length: > 0 })
            .ToArray();
        if (filters.Length == 0) return 0;

        using var writer = new StreamWriter(path, false, new UTF8Encoding(false));
        writer.WriteLine("# Magenheim procedural export");
        var vertexOffset = 1;
        foreach (var filter in filters)
        {
            var mesh = filter.sharedMesh!;
            writer.WriteLine($"o {Safe(filter.gameObject.name)}");
            foreach (var v in mesh.vertices)
            {
                var w = filter.transform.TransformPoint(v);
                writer.WriteLine(FormattableString.Invariant($"v {w.x:0.######} {w.y:0.######} {w.z:0.######}"));
            }
            for (var i = 0; i + 2 < mesh.triangles.Length; i += 3)
                writer.WriteLine($"f {vertexOffset + mesh.triangles[i]} {vertexOffset + mesh.triangles[i + 1]} {vertexOffset + mesh.triangles[i + 2]}");
            vertexOffset += mesh.vertices.Length;
        }
        return filters.Length;
    }

    private static void CopyExistingObjAssets(string output)
    {
        var repo = FindRepoRoot();
        foreach (var source in Directory.EnumerateFiles(Path.Combine(repo, "assets"), "*.*", SearchOption.AllDirectories)
                     .Where(p => p.EndsWith(".obj", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".mtl", StringComparison.OrdinalIgnoreCase)))
        {
            var relative = Path.GetRelativePath(Path.Combine(repo, "assets"), source);
            var dest = Path.Combine(output, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(source, dest, true);
        }
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "src")) && Directory.Exists(Path.Combine(current.FullName, "assets"))) return current.FullName;
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate Magenheim repository root.");
    }

    private static string TypeId(Type type)
    {
        var name = type.Name.EndsWith("Visuals", StringComparison.Ordinal) ? type.Name[..^7] : type.Name;
        var sb = new StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1])) sb.Append('-');
            sb.Append(char.ToLowerInvariant(name[i]));
        }
        return sb.ToString();
    }

    private static string? GetArg(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
        return null;
    }

    private static string Safe(string value)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        return new string(value.Select(c => invalid.Contains(c) || char.IsWhiteSpace(c) ? '_' : c).ToArray());
    }

    private static Exception Unwrap(Exception ex) => ex is TargetInvocationException { InnerException: not null } tie ? tie.InnerException : ex;
    private static string Relative(string root, string path) => Path.GetRelativePath(root, path).Replace('\\', '/');
}
