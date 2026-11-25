using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

internal static class UnderworldBossSite
{
    internal static UnderworldBossLocationRegistrar.BossLocation? Find(ZoneSystem.ZoneLocation row)
    {
        var name = UnderworldZoneCatalogGuard.LocationName(row);
        return UnderworldBossLocationRegistrar.Locations.FirstOrDefault(boss => boss.PrefabName == name);
    }

    internal static bool TryGetOrigin(UnderworldTerrainBiome biome, Vector3 point, float waterLevel,
        Func<double, double, double> groundHeight, out double origin)
    {
        origin = point.y;
        if (!UnderworldArenaGrounding.IsOutsideArrival(point.x, point.z)) return false;
        var terrain = UnderworldTerrainRuntime.SampleForDiagnostics(point.x, point.z);
        return terrain.Admitted && terrain.Biome == biome &&
            UnderworldArenaGrounding.TryResolveOriginHeight(point.x, point.z, point.y, waterLevel,
                biome == UnderworldTerrainBiome.BlackwaterDeep, groundHeight, out origin);
    }

    internal static double GeneratedHeight(double x, double z) =>
        UnderworldTerrainRuntime.SampleForDiagnostics(x, z) is { Admitted: true } terrain
            ? terrain.Height : double.NaN;

    internal static bool HasClearance(ZoneSystem zone, ZoneSystem.ZoneLocation row, Vector3 point,
        bool isBoss, Vector2s? ignoredZone = null)
    {
        foreach (var pair in zone.m_locationInstances)
        {
            if (ignoredZone is { } ignored && pair.Key.Equals(ignored)) continue;
            var other = pair.Value;
            if (other.m_location is null) continue;
            var dx = point.x - other.m_position.x;
            var dz = point.z - other.m_position.z;
            if (UnderworldArenaGrounding.HasFeatureClearance(
                    dx, dz, row.m_exteriorRadius, other.m_location.m_exteriorRadius))
                continue;
            if (isBoss || Find(other.m_location) is not null) return false;
        }

        return true;
    }

    internal static bool IsSuitable(ZoneSystem zone, ZoneSystem.ZoneLocation row, Vector3 point,
        UnderworldBossLocationRegistrar.BossLocation boss, Vector2s? ignoredZone = null)
    {
        point.y = (float)GeneratedHeight(point.x, point.z);
        return TryGetOrigin(boss.Family.Biome, point, zone.m_waterLevel, GeneratedHeight, out _) &&
            HasClearance(zone, row, point, isBoss: true, ignoredZone);
    }

    internal static bool AcceptCandidate(ZoneSystem zone, ZoneSystem.ZoneLocation row, Vector3 point)
    {
        if (ValheimWorldInstanceExecution.Active is not { } context ||
            !context.InstanceId.IsUnderworld || !ReferenceEquals(context.ZoneSystem, zone))
            return true;

        var boss = Find(row);
        return boss is null
            ? HasClearance(zone, row, point, isBoss: false)
            : IsSuitable(zone, row, point, boss);
    }
}

/// <summary>Reject an unsuitable native candidate before Valheim counts it as placed.</summary>
[HarmonyPatch]
internal static class UnderworldBossSitePlacementPatch
{
    internal static IEnumerable<MethodBase> TargetMethods()
    {
        yield return UnderworldZoneSystemLocationIteratorScopePatch.TargetMethods()
            .Where(method => method.DeclaringType?.GetFields(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Any(field => field.Name.StartsWith("<placed>", StringComparison.Ordinal)) == true)
            .Single();
    }

    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        var code = instructions.ToList();
        var register = AccessTools.Method(typeof(ZoneSystem), "RegisterLocation",
            new[] { typeof(ZoneSystem.ZoneLocation), typeof(Vector3), typeof(bool) });
        var call = code.FindIndex(instruction => instruction.Calls(register));
        if (call < 6 || code.FindIndex(call + 1, instruction => instruction.Calls(register)) >= 0 ||
            code[call - 1].opcode != OpCodes.Ldc_I4_0 ||
            code[call - 3].operand is not FieldInfo locationField ||
            locationField.FieldType != typeof(ZoneSystem.ZoneLocation))
            throw new InvalidOperationException("Cannot identify the native location candidate registration.");

        var placed = code.FindIndex(call + 1, instruction => instruction.opcode == OpCodes.Stfld &&
            instruction.operand is FieldInfo field && field.Name.StartsWith("<placed>", StringComparison.Ordinal));
        if (placed < 0 || placed + 3 >= code.Count || code[placed + 1].opcode.FlowControl != FlowControl.Branch ||
            code[placed + 3].operand is not FieldInfo counter ||
            !counter.Name.StartsWith("<j>", StringComparison.Ordinal))
            throw new InvalidOperationException("Cannot identify the native rejected-candidate continuation.");

        var reject = generator.DefineLabel();
        code[placed + 2].labels.Add(reject);
        var entry = call - 6;
        var guard = code.GetRange(entry, 5)
            .Select(instruction => new CodeInstruction(instruction.opcode, instruction.operand)).ToList();
        guard[0].labels.AddRange(code[entry].labels);
        code[entry].labels.Clear();
        guard.Add(new CodeInstruction(OpCodes.Call,
            AccessTools.Method(typeof(UnderworldBossSite), nameof(UnderworldBossSite.AcceptCandidate))));
        guard.Add(new CodeInstruction(OpCodes.Brfalse, reject));
        code.InsertRange(entry, guard);
        return code;
    }

}
