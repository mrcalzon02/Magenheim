using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

// Reports IL member references from installed mods that no longer exist in the
// current Valheim API assemblies. This is read-only and never rewrites a DLL.
internal static class AuditGameApiReferences
{
    static IEnumerable<TypeDefinition> Types(IEnumerable<TypeDefinition> roots)
    {
        foreach (var type in roots)
        {
            yield return type;
            foreach (var nested in Types(type.NestedTypes)) yield return nested;
        }
    }

    static string Parameters(IEnumerable<ParameterDefinition> parameters)
    {
        return string.Join("|", parameters.Select(p => p.ParameterType.FullName).ToArray());
    }

    static string Parameters(IEnumerable<ParameterReference> parameters)
    {
        return string.Join("|", parameters.Select(p => p.ParameterType.FullName).ToArray());
    }

    static bool SameMethod(MethodDefinition definition, MethodReference reference)
    {
        return definition.Name == reference.Name && definition.HasThis == reference.HasThis &&
               definition.ReturnType.FullName == reference.ReturnType.FullName &&
               Parameters(definition.Parameters) == Parameters(reference.Parameters) &&
               definition.GenericParameters.Count == reference.GenericParameters.Count;
    }

    static string ScopeName(TypeReference type)
    {
        var scope = type.Scope as AssemblyNameReference;
        return scope == null ? "" : scope.Name;
    }

    static int Main(string[] args)
    {
        var gamePaths = args[0].Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);
        var gameAssemblies = gamePaths.Select(AssemblyDefinition.ReadAssembly).ToArray();
        try
        {
            var gameNames = new HashSet<string>(gameAssemblies.Select(a => a.Name.Name), StringComparer.OrdinalIgnoreCase);
            var gameTypes = gameAssemblies.SelectMany(a => Types(a.MainModule.Types))
                .GroupBy(t => t.FullName).ToDictionary(g => g.Key, g => g.ToArray());
            var reports = new SortedSet<string>(StringComparer.Ordinal);

            foreach (var path in Directory.GetFiles(args[1], "*.dll", SearchOption.AllDirectories))
            {
                AssemblyDefinition mod;
                try { mod = AssemblyDefinition.ReadAssembly(path); }
                catch { continue; }
                using (mod)
                {
                    var relative = path.Substring(args[1].TrimEnd(Path.DirectorySeparatorChar).Length + 1);
                    foreach (var method in Types(mod.MainModule.Types).SelectMany(t => t.Methods).Where(m => m.HasBody))
                    foreach (var instruction in method.Body.Instructions)
                    {
                        var called = instruction.Operand as MethodReference;
                        if (called != null && !(called is GenericInstanceMethod) && !(called.DeclaringType is GenericInstanceType) && gameNames.Contains(ScopeName(called.DeclaringType)))
                        {
                            TypeDefinition[] owners;
                            if (!gameTypes.TryGetValue(called.DeclaringType.FullName, out owners))
                                reports.Add(relative + " | TYPE | " + method.FullName + " | " + called.DeclaringType.FullName);
                            else if (!owners.SelectMany(t => t.Methods).Any(m => SameMethod(m, called)))
                                reports.Add(relative + " | METHOD | " + method.FullName + " | " + called.FullName);
                        }

                        var field = instruction.Operand as FieldReference;
                        if (field != null && gameNames.Contains(ScopeName(field.DeclaringType)))
                        {
                            TypeDefinition[] owners;
                            if (!gameTypes.TryGetValue(field.DeclaringType.FullName, out owners))
                                reports.Add(relative + " | TYPE | " + method.FullName + " | " + field.DeclaringType.FullName);
                            else if (!owners.SelectMany(t => t.Fields).Any(f => f.Name == field.Name && f.FieldType.FullName == field.FieldType.FullName))
                                reports.Add(relative + " | FIELD | " + method.FullName + " | " + field.FullName);
                        }
                    }
                }
            }

            File.WriteAllLines(args[2], reports);
            foreach (var report in reports) Console.WriteLine(report);
            Console.WriteLine("MISMATCHES=" + reports.Count);
        }
        finally
        {
            foreach (var assembly in gameAssemblies) assembly.Dispose();
        }
        return 0;
    }
}
