using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

// Offline, narrowly scoped migration of obsolete Valheim API call sites.
// Writes staged copies only. Originals are not modified by this program.
internal static class RepairLegacyApis
{
    static IEnumerable<TypeDefinition> Types(IEnumerable<TypeDefinition> types)
    { foreach (var t in types) { yield return t; foreach(var n in Types(t.NestedTypes)) yield return n; } }
    static string Signature(MethodReference m) { return string.Join("|", m.Parameters.Select(p => p.ParameterType.FullName).ToArray()); }
    static int RepairBindings(TypeDefinition type, ModuleDefinition module)
    {
        int count = 0;
        foreach(var attr in type.CustomAttributes.Where(a => a.AttributeType.FullName == "HarmonyLib.HarmonyPatch").ToArray())
        {
            if(attr.ConstructorArguments.Count < 2) continue;
            var target = attr.ConstructorArguments[0].Value as TypeReference;
            var name = attr.ConstructorArguments[1].Value as string;
            if(target == null || (target.FullName != "Inventory" && target.FullName != "WaterVolume" && target.FullName != "ItemDrop/ItemData")) continue;
            TypeReference[] signature = null;
            if(name == "AddItem" && attr.ConstructorArguments.Count == 3)
            {
                var old = attr.ConstructorArguments[2].Value as CustomAttributeArgument[];
                if(old != null && string.Join("|", old.Select(p => ((TypeReference)p.Value).FullName).ToArray()) == "ItemDrop/ItemData|System.Int32|System.Int32|System.Int32")
                    signature = old.Select(p => (TypeReference)p.Value).Concat(new[] {module.TypeSystem.Boolean}).ToArray();
            }
            if(name == "Load" && attr.ConstructorArguments.Count == 2 && type.FullName == "Seasons.SeasonStatePatches/Inventory_Load_TorchPatch")
                signature = new[] { new TypeReference("", "ZPackage", module, target.Scope) };
            if(target.FullName == "WaterVolume" && name == "CalcWave" && attr.ConstructorArguments.Count == 3)
            {
                var old = attr.ConstructorArguments[2].Value as CustomAttributeArgument[];
                if(old != null && new[] {"UnityEngine.Vector3|System.Single|System.Single|System.Single", "UnityEngine.Vector3|System.Single|UnityEngine.Vector4|System.Single|System.Single"}.Contains(string.Join("|", old.Select(p => ((TypeReference)p.Value).FullName).ToArray())))
                    signature = old.Select(p => (TypeReference)p.Value).Concat(new[] {module.TypeSystem.Single}).ToArray();
            }
            if(target.FullName == "ItemDrop/ItemData" && name == "GetTooltip" && attr.ConstructorArguments.Count == 3)
            {
                var old = attr.ConstructorArguments[2].Value as CustomAttributeArgument[];
                if(old != null && string.Join("|", old.Select(p => ((TypeReference)p.Value).FullName).ToArray()) == "ItemDrop/ItemData|System.Int32|System.Boolean|System.Single|System.Int32")
                    signature = old.Select(p => (TypeReference)p.Value).Concat(new[] {module.TypeSystem.Boolean}).ToArray();
            }
            if(signature == null) continue;
            var systemType = attr.ConstructorArguments[0].Type;
            var ctor = new MethodReference(".ctor", module.TypeSystem.Void, attr.AttributeType) { HasThis = true };
            ctor.Parameters.Add(new ParameterDefinition(systemType));
            ctor.Parameters.Add(new ParameterDefinition(module.TypeSystem.String));
            ctor.Parameters.Add(new ParameterDefinition(new ArrayType(systemType)));
            var replacement = new CustomAttribute(ctor);
            replacement.ConstructorArguments.Add(attr.ConstructorArguments[0]);
            replacement.ConstructorArguments.Add(attr.ConstructorArguments[1]);
            replacement.ConstructorArguments.Add(new CustomAttributeArgument(new ArrayType(systemType), signature.Select(t => new CustomAttributeArgument(systemType, t)).ToArray()));
            type.CustomAttributes.Remove(attr); type.CustomAttributes.Add(replacement); count++;
        }
        if(type.FullName == "ItemDataManager.ItemInfo")
        foreach(var method in type.Methods.Where(m => m.Name == ".cctor" && m.HasBody))
        foreach(var op in method.Body.Instructions.ToArray())
        {
            if(op.OpCode != OpCodes.Ldstr || (string)op.Operand != "AddItem" || (op.Next.OpCode != OpCodes.Ldc_I4_4 && op.Next.OpCode != OpCodes.Ldc_I4_7 && op.Next.OpCode != OpCodes.Ldc_I4_8)) continue;
            bool seven = op.Next.OpCode == OpCodes.Ldc_I4_7;
            bool eight = op.Next.OpCode == OpCodes.Ldc_I4_8;
            var cursor = op.Next;
            var call = cursor;
            while(call != null && !(call.Operand is MethodReference && ((MethodReference)call.Operand).Name == "DeclaredMethod")) call = call.Next;
            if(call == null || call.Previous.OpCode != OpCodes.Ldnull) throw new Exception("Unexpected ItemDataManager reflection pattern");
            if(eight)
            {
                var probe = cursor;
                bool vector = false;
                while(probe != call) { var t = probe.Operand as TypeReference; if(t != null && t.FullName == "Vector2i") vector = true; probe = probe.Next; }
                if(!vector) continue;
            }
            var getType = method.Body.Instructions.First(i => i.Operand is MethodReference && ((MethodReference)i.Operand).Name == "GetTypeFromHandle").Operand as MethodReference;
            cursor.OpCode = eight ? OpCodes.Ldc_I4 : (seven ? OpCodes.Ldc_I4_8 : OpCodes.Ldc_I4_5);
            if(eight) cursor.Operand = 10;
            var il = method.Body.GetILProcessor();
            var before = call.Previous;
            il.InsertBefore(before, Instruction.Create(OpCodes.Dup));
            il.InsertBefore(before, Instruction.Create(eight ? OpCodes.Ldc_I4_8 : (seven ? OpCodes.Ldc_I4_7 : OpCodes.Ldc_I4_4)));
            il.InsertBefore(before, Instruction.Create(OpCodes.Ldtoken, module.TypeSystem.Boolean));
            il.InsertBefore(before, Instruction.Create(OpCodes.Call, getType));
            il.InsertBefore(before, Instruction.Create(OpCodes.Stelem_Ref));
            if(eight)
            {
                il.InsertBefore(before, Instruction.Create(OpCodes.Dup));
                il.InsertBefore(before, Instruction.Create(OpCodes.Ldc_I4, 9));
                il.InsertBefore(before, Instruction.Create(OpCodes.Ldtoken, module.TypeSystem.Boolean));
                il.InsertBefore(before, Instruction.Create(OpCodes.Call, getType));
                il.InsertBefore(before, Instruction.Create(OpCodes.Stelem_Ref));
            }
            count++;
        }
        return count;
    }
    static int Main(string[] args)
    {
        using (var game = AssemblyDefinition.ReadAssembly(args[0]))
        {
            var all = Types(game.MainModule.Types).ToArray();
            var message = all.Single(t => t.Name == "Character").Methods.Single(m => m.Name == "Message" && m.Parameters.Count == 5);
            if(message.Parameters[4].Name != "log" || !Equals(message.Parameters[4].Constant, false)) throw new Exception("Unexpected Message signature");
            var everybody = all.Single(t => t.Name == "ZRoutedRpc").Fields.Single(f => f.Name == "Everybody");
            if(!everybody.IsLiteral || !Equals(everybody.Constant, (long)0)) throw new Exception("Unexpected Everybody field");
            var constructors = all.Single(t => t.Name == "ConsoleCommand").Methods.Where(m => m.IsConstructor && m.Parameters.Count == 13).ToArray();
            var reports = new List<string>();
            foreach(var file in Directory.GetFiles(args[1], "*.dll", SearchOption.AllDirectories))
            using(var mod = AssemblyDefinition.ReadAssembly(file))
            {
                int messages = 0, fields = 0, commands = 0, hovers = 0, equipment = 0, environment = 0, zones = 0, ui = 0, appearance = 0;
                var stableHash = Types(mod.MainModule.Types).SelectMany(t => t.Methods).Where(m => m.HasBody)
                    .SelectMany(m => m.Body.Instructions).Select(i => i.Operand as MethodReference)
                    .FirstOrDefault(m => m != null && m.DeclaringType.FullName == "StringExtensionMethods" && m.Name == "GetStableHashCode" && m.Parameters.Count == 1);
                var visEquipment = all.Single(t => t.Name == "VisEquipment");
                foreach(var type in Types(mod.MainModule.Types).ToArray())
                {
                    commands += RepairBindings(type, mod.MainModule);
                    if(type.FullName == "Seasons.Compatibility.EWDCompat/EnvMan_GetAvailableEnvironments_ApplySeasonalRulesAfterEWD")
                    {
                        var postfix = type.Methods.Single(m => m.Name == "Postfix");
                        if(postfix.Parameters[0].ParameterType.FullName == "Heightmap/Biome")
                        {
                            var sector = all.Single(t => t.Name == "BiomeSector");
                            postfix.Parameters[0].ParameterType = mod.MainModule.ImportReference(sector);
                            foreach(var op in postfix.Body.Instructions.ToArray().Where(i => i.OpCode == OpCodes.Ldarg_0))
                                postfix.Body.GetILProcessor().InsertAfter(op, Instruction.Create(OpCodes.Ldfld, mod.MainModule.ImportReference(sector.Fields.Single(f => f.Name == "Biome"))));
                            commands++;
                        }
                    }
                    foreach(var method in type.Methods.ToArray())
                    {
                        if(!method.HasBody) continue;
                        foreach(var instruction in method.Body.Instructions.ToArray())
                        {
                            var obsoleteEquipmentField = instruction.Operand as FieldReference;
                            if(type.FullName == "Seasons.SeasonState" && method.Name == "UpdateTorchFireWarmth" &&
                                obsoleteEquipmentField != null && obsoleteEquipmentField.DeclaringType.FullName == "VisEquipment" &&
                                (obsoleteEquipmentField.Name == "m_rightItem" || obsoleteEquipmentField.Name == "m_leftItem") &&
                                obsoleteEquipmentField.FieldType.FullName == "System.String")
                            {
                                if(stableHash == null || instruction.Next == null || instruction.Next.OpCode != OpCodes.Ldarg_1 || instruction.Next.Next == null ||
                                    !(instruction.Next.Next.Operand is MethodReference) || ((MethodReference)instruction.Next.Next.Operand).FullName != "System.Boolean System.String::op_Equality(System.String,System.String)")
                                    throw new Exception("Unexpected Seasons torch equipment comparison pattern");
                                instruction.Operand = mod.MainModule.ImportReference(visEquipment.Fields.Single(f => f.Name == obsoleteEquipmentField.Name && f.FieldType.FullName == "System.Int32"));
                                method.Body.GetILProcessor().InsertAfter(instruction.Next, Instruction.Create(OpCodes.Call, mod.MainModule.ImportReference(stableHash)));
                                instruction.Next.Next.Next.OpCode = OpCodes.Ceq;
                                instruction.Next.Next.Next.Operand = null;
                                equipment++;
                                continue;
                            }
                            var obsoleteEnvironmentField = instruction.Operand as FieldReference;
                            if(obsoleteEnvironmentField != null && obsoleteEnvironmentField.DeclaringType.FullName == "EnvMan" &&
                                obsoleteEnvironmentField.Name == "m_currentBiome" && obsoleteEnvironmentField.FieldType.FullName == "Heightmap/Biome")
                            {
                                var sector = all.Single(t => t.Name == "BiomeSector");
                                instruction.Operand = mod.MainModule.ImportReference(all.Single(t => t.Name == "EnvMan").Fields.Single(f => f.Name == "m_currentBiome" && f.FieldType.FullName == "BiomeSector"));
                                method.Body.GetILProcessor().InsertAfter(instruction, Instruction.Create(OpCodes.Ldfld, mod.MainModule.ImportReference(sector.Fields.Single(f => f.Name == "Biome"))));
                                environment++;
                                continue;
                            }
                            if(obsoleteEnvironmentField != null && obsoleteEnvironmentField.DeclaringType.FullName == "InventoryGui" &&
                                obsoleteEnvironmentField.Name == "m_splitSlider" && obsoleteEnvironmentField.FieldType.FullName == "UnityEngine.UI.Slider" && instruction.OpCode == OpCodes.Ldfld)
                            {
                                instruction.OpCode = OpCodes.Pop; instruction.Operand = null;
                                method.Body.GetILProcessor().InsertAfter(instruction, Instruction.Create(OpCodes.Ldnull));
                                ui++;
                                continue;
                            }
                            var called = instruction.Operand as MethodReference;
                            bool supportedOptionalCall = called != null &&
                                ((called.DeclaringType.FullName == "SEMan" && called.Name == "AddStatusEffect" && called.Parameters.Count == 4) ||
                                 (called.DeclaringType.FullName == "EffectList" && called.Name == "Create" && called.Parameters.Count == 5) ||
                                 (called.DeclaringType.FullName == "Game" && called.Name == "SavePlayerProfile" && called.Parameters.Count == 1));
                            if(supportedOptionalCall &&
                                (instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt))
                            {
                                var owner = all.FirstOrDefault(t => t.FullName == called.DeclaringType.FullName);
                                var candidates = owner == null ? new MethodDefinition[0] : owner.Methods.Where(m => m.Name == called.Name && m.HasThis == called.HasThis && m.ReturnType.FullName == called.ReturnType.FullName).ToArray();
                                if(!candidates.Any(m => Signature(m) == Signature(called)))
                                {
                                    var newer = candidates.Where(m => m.Parameters.Count > called.Parameters.Count &&
                                        string.Join("|", m.Parameters.Take(called.Parameters.Count).Select(p => p.ParameterType.FullName).ToArray()) == Signature(called) &&
                                        m.Parameters.Skip(called.Parameters.Count).All(p => p.IsOptional)).ToArray();
                                    if(newer.Length == 1)
                                    {
                                        var target = newer[0];
                                        var wrapper = new MethodDefinition("MagenheimCompat_Defaults_" + commands, MethodAttributes.Private | MethodAttributes.Static, mod.MainModule.ImportReference(target.ReturnType));
                                        if(called.HasThis) wrapper.Parameters.Add(new ParameterDefinition("instance", ParameterAttributes.None, mod.MainModule.ImportReference(called.DeclaringType)));
                                        foreach(var p in called.Parameters) wrapper.Parameters.Add(new ParameterDefinition(p.Name, ParameterAttributes.None, mod.MainModule.ImportReference(p.ParameterType)));
                                        var il = wrapper.Body.GetILProcessor();
                                        foreach(var p in wrapper.Parameters) il.Append(Instruction.Create(OpCodes.Ldarg, p));
                                        foreach(var p in target.Parameters.Skip(called.Parameters.Count))
                                        {
                                            if(p.Constant is bool) il.Append(Instruction.Create((bool)p.Constant ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
                                            else if(p.Constant is int || p.Constant is short) il.Append(Instruction.Create(OpCodes.Ldc_I4, Convert.ToInt32(p.Constant)));
                                            else if(p.Constant is float) il.Append(Instruction.Create(OpCodes.Ldc_R4, (float)p.Constant));
                                            else if(p.Constant == null && !p.ParameterType.IsValueType) il.Append(Instruction.Create(OpCodes.Ldnull));
                                            else if(p.Constant == null && p.ParameterType.IsValueType)
                                            {
                                                var local = new VariableDefinition(mod.MainModule.ImportReference(p.ParameterType)); wrapper.Body.Variables.Add(local); wrapper.Body.InitLocals = true;
                                                il.Append(Instruction.Create(OpCodes.Ldloca, local)); il.Append(Instruction.Create(OpCodes.Initobj, local.VariableType)); il.Append(Instruction.Create(OpCodes.Ldloc, local));
                                            }
                                            else throw new Exception("Unsupported optional default: " + target.FullName);
                                        }
                                        il.Append(Instruction.Create(instruction.OpCode, mod.MainModule.ImportReference(target))); il.Append(Instruction.Create(OpCodes.Ret));
                                        wrapper.Body.MaxStackSize = target.Parameters.Count + 1;
                                        type.Methods.Add(wrapper); instruction.OpCode = OpCodes.Call; instruction.Operand = wrapper; commands++;
                                        continue;
                                    }
                                }
                            }
                            if(called != null && called.DeclaringType.FullName == "ZoneSystem" && called.Name == "GetZone" && called.Parameters.Count == 1 &&
                                called.Parameters[0].ParameterType.FullName == "UnityEngine.Vector3" && called.ReturnType.FullName == "Vector2i")
                            {
                                var zoneOwner = all.Single(t => t.Name == "ZoneSystem");
                                var target = zoneOwner.Methods.Single(m => m.Name == "GetZone" && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "UnityEngine.Vector3" && m.ReturnType.FullName == "Vector2s");
                                var wrapper = new MethodDefinition("MagenheimCompat_GetZone_" + commands, MethodAttributes.Private | MethodAttributes.Static, mod.MainModule.ImportReference(called.ReturnType));
                                wrapper.Parameters.Add(new ParameterDefinition("position", ParameterAttributes.None, mod.MainModule.ImportReference(called.Parameters[0].ParameterType)));
                                var sectorType = mod.MainModule.ImportReference(target.ReturnType);
                                var local = new VariableDefinition(sectorType); wrapper.Body.Variables.Add(local); wrapper.Body.InitLocals = true;
                                var vectorCtor = new MethodReference(".ctor", mod.MainModule.TypeSystem.Void, mod.MainModule.ImportReference(called.ReturnType)) { HasThis = true };
                                vectorCtor.Parameters.Add(new ParameterDefinition(mod.MainModule.TypeSystem.Int32));
                                vectorCtor.Parameters.Add(new ParameterDefinition(mod.MainModule.TypeSystem.Int32));
                                var il = wrapper.Body.GetILProcessor();
                                il.Append(Instruction.Create(OpCodes.Ldarg_0));
                                il.Append(Instruction.Create(OpCodes.Call, mod.MainModule.ImportReference(target)));
                                il.Append(Instruction.Create(OpCodes.Stloc, local));
                                il.Append(Instruction.Create(OpCodes.Ldloca, local));
                                il.Append(Instruction.Create(OpCodes.Ldfld, new FieldReference("x", mod.MainModule.TypeSystem.Int16, sectorType)));
                                il.Append(Instruction.Create(OpCodes.Conv_I4));
                                il.Append(Instruction.Create(OpCodes.Ldloca, local));
                                il.Append(Instruction.Create(OpCodes.Ldfld, new FieldReference("y", mod.MainModule.TypeSystem.Int16, sectorType)));
                                il.Append(Instruction.Create(OpCodes.Conv_I4));
                                il.Append(Instruction.Create(OpCodes.Newobj, vectorCtor));
                                il.Append(Instruction.Create(OpCodes.Ret));
                                wrapper.Body.MaxStackSize = 2;
                                type.Methods.Add(wrapper); instruction.OpCode = OpCodes.Call; instruction.Operand = wrapper; zones++; commands++;
                                continue;
                            }
                            if(called != null && called.DeclaringType.FullName == "ZoneSystem" && called.Name == "IsZoneLoaded" && called.Parameters.Count == 1 &&
                                called.Parameters[0].ParameterType.FullName == "Vector2i")
                            {
                                var zoneOwner = all.Single(t => t.Name == "ZoneSystem");
                                var target = zoneOwner.Methods.Single(m => m.Name == "IsZoneLoaded" && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "Vector2s");
                                var ctor = new MethodReference(".ctor", mod.MainModule.TypeSystem.Void, mod.MainModule.ImportReference(target.Parameters[0].ParameterType)) { HasThis = true };
                                ctor.Parameters.Add(new ParameterDefinition(mod.MainModule.ImportReference(called.Parameters[0].ParameterType)));
                                method.Body.GetILProcessor().InsertBefore(instruction, Instruction.Create(OpCodes.Newobj, ctor));
                                instruction.Operand = mod.MainModule.ImportReference(target); zones++;
                                continue;
                            }
                            if(called != null && called.DeclaringType.FullName == "VisEquipment" &&
                                (called.Name == "SetHairItem" || called.Name == "SetBeardItem") && called.Parameters.Count == 1 && called.Parameters[0].ParameterType.FullName == "System.String")
                            {
                                if(stableHash == null) throw new Exception("Stable hash helper not found for VisEquipment appearance repair");
                                var target = visEquipment.Methods.Single(m => m.Name == called.Name && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "System.Int32");
                                method.Body.GetILProcessor().InsertBefore(instruction, Instruction.Create(OpCodes.Call, mod.MainModule.ImportReference(stableHash)));
                                instruction.Operand = mod.MainModule.ImportReference(target); appearance++;
                                continue;
                            }
                            if(called != null && called.DeclaringType.FullName == "Character" && called.Name == "Message" && called.Parameters.Count == 4)
                            {
                                if(Signature(called) != string.Join("|", message.Parameters.Take(4).Select(p => p.ParameterType.FullName).ToArray())) throw new Exception("Unknown legacy Message signature");
                                if(instruction.OpCode != OpCodes.Call && instruction.OpCode != OpCodes.Callvirt) throw new Exception("Unsupported Message instruction");
                                var opcode = instruction.OpCode;
                                instruction.OpCode = OpCodes.Ldc_I4_0; instruction.Operand = null;
                                method.Body.GetILProcessor().InsertAfter(instruction, Instruction.Create(opcode, mod.MainModule.ImportReference(message)));
                                method.Body.MaxStackSize++;
                                messages++;
                            }
                            var field = instruction.Operand as FieldReference;
                            if(field != null && field.DeclaringType.FullName == "ZRoutedRpc" && field.Name == "Everybody" && instruction.OpCode == OpCodes.Ldsfld)
                            { instruction.OpCode = OpCodes.Ldc_I8; instruction.Operand = (long)0; fields++; }
                            if(called != null && called.DeclaringType.FullName == "Terminal/ConsoleCommand" && called.Name == ".ctor" && called.Parameters.Count == 12)
                            {
                                if(instruction.OpCode != OpCodes.Newobj) throw new Exception("Unsupported ConsoleCommand instruction");
                                var target = constructors.Single(c => string.Join("|", c.Parameters.Where((p,i) => i != 8).Select(p => p.ParameterType.FullName).ToArray()) == Signature(called));
                                if(target.Parameters[8].Name != "hideBehindDevCommands" || !Equals(target.Parameters[8].Constant, false)) throw new Exception("Unexpected console default");
                                var wrapper = new MethodDefinition("MagenheimCompat_CreateConsoleCommand_" + commands, MethodAttributes.Private | MethodAttributes.Static, mod.MainModule.ImportReference(target.DeclaringType));
                                foreach(var p in called.Parameters) wrapper.Parameters.Add(new ParameterDefinition(p.Name, ParameterAttributes.None, mod.MainModule.ImportReference(p.ParameterType)));
                                var il = wrapper.Body.GetILProcessor();
                                for(int i = 0; i < 13; i++)
                                    il.Append(i == 8 ? Instruction.Create(OpCodes.Ldc_I4_0) : Instruction.Create(OpCodes.Ldarg, wrapper.Parameters[i < 8 ? i : i - 1]));
                                il.Append(Instruction.Create(OpCodes.Newobj, mod.MainModule.ImportReference(target)));
                                il.Append(Instruction.Create(OpCodes.Ret));
                                wrapper.Body.MaxStackSize = 13;
                                type.Methods.Add(wrapper);
                                instruction.OpCode = OpCodes.Call; instruction.Operand = wrapper; commands++;
                            }
                        }
                    }
                    if(type.FullName == "Seasons.IceFloeClimb" && type.Interfaces.Any(i => i.InterfaceType.FullName == "Hoverable") && !type.Methods.Any(m => m.Name == "GetHoverOffset"))
                    {
                        var hover = new MethodDefinition("GetHoverOffset", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.NewSlot | MethodAttributes.HideBySig, mod.MainModule.TypeSystem.Single);
                        hover.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_R4, 0f));
                        hover.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                        type.Methods.Add(hover); hovers++;
                    }
                }
                if(messages + fields + commands + hovers + equipment + environment + zones + ui + appearance == 0) continue;
                string relative = file.Substring(args[1].TrimEnd(Path.DirectorySeparatorChar).Length + 1);
                string output = Path.Combine(args[2], relative);
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                mod.Write(output);
                using(var check = AssemblyDefinition.ReadAssembly(output))
                {
                    foreach(var method in Types(check.MainModule.Types).SelectMany(t => t.Methods).Where(m => m.HasBody))
                    foreach(var op in method.Body.Instructions)
                    {
                        var m = op.Operand as MethodReference;
                        if(m != null && m.DeclaringType.FullName == "Character" && m.Name == "Message" && m.Parameters.Count == 4) throw new Exception("Unrepaired Message");
                        if(m != null && m.DeclaringType.FullName == "Terminal/ConsoleCommand" && m.Name == ".ctor" && m.Parameters.Count == 12) throw new Exception("Unrepaired console constructor");
                        var f = op.Operand as FieldReference;
                        if(f != null && f.DeclaringType.FullName == "VisEquipment" && (f.Name == "m_rightItem" || f.Name == "m_leftItem") && f.FieldType.FullName == "System.String") throw new Exception("Unrepaired VisEquipment field");
                        if(f != null && f.DeclaringType.FullName == "EnvMan" && f.Name == "m_currentBiome" && f.FieldType.FullName == "Heightmap/Biome") throw new Exception("Unrepaired EnvMan biome field");
                        if(m != null && m.DeclaringType.FullName == "EffectList" && m.Name == "Create" && m.Parameters.Count == 5) throw new Exception("Unrepaired EffectList Create");
                        if(m != null && m.DeclaringType.FullName == "Game" && m.Name == "SavePlayerProfile" && m.Parameters.Count == 1) throw new Exception("Unrepaired SavePlayerProfile");
                        if(m != null && m.DeclaringType.FullName == "ZoneSystem" && m.Name == "GetZone" && m.ReturnType.FullName == "Vector2i") throw new Exception("Unrepaired ZoneSystem GetZone");
                        if(m != null && m.DeclaringType.FullName == "ZoneSystem" && m.Name == "IsZoneLoaded" && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "Vector2i") throw new Exception("Unrepaired ZoneSystem IsZoneLoaded");
                        if(m != null && m.DeclaringType.FullName == "VisEquipment" && (m.Name == "SetHairItem" || m.Name == "SetBeardItem") && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "System.String") throw new Exception("Unrepaired VisEquipment appearance call");
                        if(f != null && f.DeclaringType.FullName == "InventoryGui" && f.Name == "m_splitSlider") throw new Exception("Unrepaired InventoryGui split slider");
                    }
                }
                reports.Add(relative + " | Message=" + messages + " | Everybody=" + fields + " | Bindings=" + commands + " | Hover=" + hovers + " | Equipment=" + equipment + " | Environment=" + environment + " | Zones=" + zones + " | UI=" + ui + " | Appearance=" + appearance);
            }
            Directory.CreateDirectory(args[2]);
            File.WriteAllLines(Path.Combine(args[2], "repair-report.txt"), reports);
            foreach(var report in reports) Console.WriteLine(report);
        }
        return 0;
    }
}
