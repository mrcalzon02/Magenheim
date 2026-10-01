using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

class AuditCompanions
{
    static int Main(string[] args)
    {
        try
        {
            using (var assembly = AssemblyDefinition.ReadAssembly(args[0]))
            {
                var method = assembly.MainModule.GetTypes().Single(t => t.FullName == "Companions.CompanionsPlugin")
                    .Methods.Single(m => m.Name == "EnsureTmpDefaultFont");
                var clear = method.Body.Instructions.Single(i => i.OpCode == OpCodes.Stfld &&
                    ((FieldReference)i.Operand).Name == "_fontFixWarned" && i.Previous.OpCode == OpCodes.Ldc_I4_0);
                if (clear.Next.OpCode != OpCodes.Ret) throw new InvalidOperationException("Unsafe global TMP reassignment remains.");

                var panel = assembly.MainModule.GetTypes().Single(t => t.FullName == "Companions.CompanionInteractPanel");
                var updateGrid = panel.Methods.Single(m => m.Name == "UpdateGrid" && !m.HasParameters);
                var localPlayerLoads = updateGrid.Body.Instructions.Where(i => i.OpCode == OpCodes.Ldsfld &&
                    ((FieldReference)i.Operand).DeclaringType.FullName == "Player" &&
                    ((FieldReference)i.Operand).Name == "m_localPlayer").ToArray();
                if (localPlayerLoads.Length != 2 ||
                    ((FieldReference)localPlayerLoads[0].Operand).FullName != ((FieldReference)localPlayerLoads[1].Operand).FullName)
                    throw new InvalidOperationException("Companion inventory grid still passes a null Player during its first render.");

                var buildUi = panel.Methods.Single(m => m.Name == "BuildUI" && !m.HasParameters);
                var canDropStores = buildUi.Body.Instructions.Where(i => i.OpCode == OpCodes.Stfld &&
                    ((FieldReference)i.Operand).Name == "CanDropDragOntoItem").ToArray();
                if (canDropStores.Length != 1 ||
                    canDropStores[0].Previous == null || canDropStores[0].Previous.OpCode != OpCodes.Ldfld ||
                    ((FieldReference)canDropStores[0].Previous.Operand).Name != "CanDropDragOntoItem" ||
                    canDropStores[0].Previous.Previous == null || canDropStores[0].Previous.Previous.OpCode != OpCodes.Ldfld ||
                    ((FieldReference)canDropStores[0].Previous.Previous.Operand).Name != "m_containerGrid")
                    throw new InvalidOperationException("Companion inventory clone does not inherit the required item-drop delegate.");
                var joystickCalls = assembly.MainModule.GetTypes().SelectMany(t => t.Methods)
                    .Where(m => m.HasBody).SelectMany(m => m.Body.Instructions)
                    .Where(i => i.Operand is MethodReference &&
                        ((MethodReference)i.Operand).DeclaringType.FullName == "ZInput" &&
                        ((MethodReference)i.Operand).Name.StartsWith("GetJoy") &&
                        ((MethodReference)i.Operand).Name.Contains("Stick")).ToArray();
                if (joystickCalls.Length != 8 || joystickCalls.Any(i =>
                        ((MethodReference)i.Operand).Parameters.Count != 0 || i.Previous.OpCode != OpCodes.Pop))
                    throw new InvalidOperationException("Companion joystick calls do not match the native zero-argument API and balanced stack.");
            }
            Console.WriteLine("PASS: Companion TMP, inventory player/drop state and all eight native joystick calls are repaired.");
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e.Message); return 1; }
    }
}
