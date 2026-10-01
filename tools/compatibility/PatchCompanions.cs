using System;
using System.Linq;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

class PatchCompanions
{
    static int Main(string[] args)
    {
        try
        {
            using (var assembly = AssemblyDefinition.ReadAssembly(args[0]))
            using (var game = AssemblyDefinition.ReadAssembly(args[2]))
            using (var utilities = AssemblyDefinition.ReadAssembly(Path.Combine(Path.GetDirectoryName(args[2]), "assembly_utils.dll")))
            {
                if (assembly.Name.Name != "Companions") throw new InvalidOperationException("Expected Offline Companions assembly.");
                var method = assembly.MainModule.GetTypes().Single(t => t.FullName == "Companions.CompanionsPlugin")
                    .Methods.Single(m => m.Name == "EnsureTmpDefaultFont");
                var clear = method.Body.Instructions.Single(i => i.OpCode == OpCodes.Stfld &&
                    ((FieldReference)i.Operand).Name == "_fontFixWarned" && i.Previous.OpCode == OpCodes.Ldc_I4_0);
                if (clear.Next.OpCode != OpCodes.Ldc_I4_0) throw new InvalidOperationException("Unexpected TMP reassignment loop prologue.");
                clear.Next.OpCode = OpCodes.Ret;
                clear.Next.Operand = null;

                var panel = assembly.MainModule.GetTypes().Single(t => t.FullName == "Companions.CompanionInteractPanel");
                var updateGrid = panel.Methods.Single(m => m.Name == "UpdateGrid" && !m.HasParameters);
                var localPlayerLoad = updateGrid.Body.Instructions.Single(i => i.OpCode == OpCodes.Ldsfld &&
                    ((FieldReference)i.Operand).DeclaringType.FullName == "Player" &&
                    ((FieldReference)i.Operand).Name == "m_localPlayer");
                var nullPlayerLoad = updateGrid.Body.Instructions.Single(i => i.OpCode == OpCodes.Ldnull &&
                    i.Next != null && i.Next.OpCode.FlowControl == FlowControl.Branch &&
                    i.Next.Next == localPlayerLoad);
                nullPlayerLoad.OpCode = OpCodes.Ldsfld;
                nullPlayerLoad.Operand = localPlayerLoad.Operand;

                // Valheim 1.0.16 invokes this delegate once per occupied slot. InventoryGui.Awake
                // initializes it on the two live grids, but Offline Companions clones the container
                // hierarchy and the managed delegate is null on that clone. UpdateGui then throws on
                // the first item and never clears the remaining elements, leaving the donor icon in
                // every slot. Copy the already-bound delegate from InventoryGui.m_containerGrid.
                var gameInventoryGui = game.MainModule.GetType("InventoryGui");
                var gameInventoryGrid = game.MainModule.GetType("InventoryGrid");
                var containerGrid = assembly.MainModule.ImportReference(
                    gameInventoryGui.Fields.Single(f => f.Name == "m_containerGrid"));
                var canDrop = assembly.MainModule.ImportReference(
                    gameInventoryGrid.Fields.Single(f => f.Name == "CanDropDragOntoItem"));
                var gridField = panel.Fields.Single(f => f.Name == "_grid");
                var buildUi = panel.Methods.Single(m => m.Name == "BuildUI" && !m.HasParameters);
                var gridNullBranch = buildUi.Body.Instructions.Single(i =>
                    i.OpCode.FlowControl == FlowControl.Cond_Branch &&
                    i.Previous != null && i.Previous.OpCode == OpCodes.Call &&
                    ((MethodReference)i.Previous.Operand).Name == "op_Equality" &&
                    i.Previous.Previous != null && i.Previous.Previous.OpCode == OpCodes.Ldnull &&
                    i.Previous.Previous.Previous != null && i.Previous.Previous.Previous.OpCode == OpCodes.Ldfld &&
                    ((FieldReference)i.Previous.Previous.Previous.Operand).Name == "_grid");
                var oldTarget = (Instruction)gridNullBranch.Operand;
                var il = buildUi.Body.GetILProcessor();
                var first = il.Create(OpCodes.Ldarg_0);
                il.InsertBefore(oldTarget, first);
                il.InsertBefore(oldTarget, il.Create(OpCodes.Ldfld, gridField));
                il.InsertBefore(oldTarget, il.Create(OpCodes.Ldloc_0));
                il.InsertBefore(oldTarget, il.Create(OpCodes.Ldfld, containerGrid));
                il.InsertBefore(oldTarget, il.Create(OpCodes.Ldfld, canDrop));
                il.InsertBefore(oldTarget, il.Create(OpCodes.Stfld, canDrop));
                gridNullBranch.Operand = first;
                var zinput = utilities.MainModule.GetType("ZInput");
                int joystickCalls = 0;
                foreach (var type in assembly.MainModule.GetTypes())
                foreach (var bodyMethod in type.Methods.Where(m => m.HasBody))
                foreach (var instruction in bodyMethod.Body.Instructions.ToArray())
                {
                    var call = instruction.Operand as MethodReference;
                    if (call == null || call.DeclaringType.FullName != "ZInput" ||
                        !call.Name.StartsWith("GetJoy") || !call.Name.Contains("Stick") ||
                        call.Parameters.Count != 1 || call.Parameters[0].ParameterType.FullName != "System.Boolean") continue;
                    var native = zinput.Methods.Single(m => m.Name == call.Name && m.Parameters.Count == 0);
                    var processor = bodyMethod.Body.GetILProcessor();
                    var pop = processor.Create(OpCodes.Pop);
                    processor.InsertBefore(instruction, pop);
                    foreach (var branch in bodyMethod.Body.Instructions)
                    {
                        if (ReferenceEquals(branch.Operand, instruction)) branch.Operand = pop;
                        var targets = branch.Operand as Instruction[];
                        if (targets != null)
                            for (int i = 0; i < targets.Length; i++) if (targets[i] == instruction) targets[i] = pop;
                    }
                    instruction.Operand = assembly.MainModule.ImportReference(native);
                    joystickCalls++;
                }
                if (joystickCalls != 8) throw new InvalidOperationException("Expected eight legacy companion joystick calls, found " + joystickCalls);
                assembly.Write(args[1]);
            }
            Console.WriteLine("Offline Companions compatibility changes: 3 UI repairs and 8 native joystick call repairs");
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}
