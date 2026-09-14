using System;
using System.Linq;
using Mono.Cecil;

internal static class InspectAssembly
{
    static System.Collections.Generic.IEnumerable<TypeDefinition> Types(System.Collections.Generic.IEnumerable<TypeDefinition> types)
    {
        foreach (var type in types)
        {
            yield return type;
            foreach (var nested in Types(type.NestedTypes)) yield return nested;
        }
    }

    static int Main(string[] args)
    {
        using (var assembly = AssemblyDefinition.ReadAssembly(args[0]))
        {
            foreach (var type in Types(assembly.MainModule.Types).Where(t => t.FullName.IndexOf(args[1], StringComparison.OrdinalIgnoreCase) >= 0))
            {
                Console.WriteLine("TYPE " + type.FullName);
                foreach (var field in type.Fields) Console.WriteLine("  FIELD " + field.FieldType.FullName + " " + field.Name);
                foreach (var method in type.Methods.Where(m => args.Length < 3 || m.Name.IndexOf(args[2], StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    if (args.Length >= 4 && (!method.HasBody || !method.Body.Instructions.Any(i => i.ToString().IndexOf(args[3], StringComparison.OrdinalIgnoreCase) >= 0))) continue;
                    Console.WriteLine("  METHOD " + method.FullName);
                    foreach (var parameter in method.Parameters) if (parameter.IsOptional || parameter.HasConstant)
                        Console.WriteLine("    PARAM " + parameter.Name + " optional=" + parameter.IsOptional + " constant=" + (parameter.Constant == null ? "<null>" : parameter.Constant.ToString()));
                    if (!method.HasBody) continue;
                    var instructions = method.Body.Instructions;
                    for (var index = 0; index < instructions.Count; index++)
                    {
                        if (args.Length >= 4 && !Enumerable.Range(Math.Max(0, index - 4), Math.Min(instructions.Count, index + 5) - Math.Max(0, index - 4))
                            .Any(j => instructions[j].ToString().IndexOf(args[3], StringComparison.OrdinalIgnoreCase) >= 0)) continue;
                        var instruction = instructions[index];
                        Console.WriteLine("    " + instruction.Offset.ToString("X4") + " " + instruction);
                    }
                }
            }
        }
        return 0;
    }
}
