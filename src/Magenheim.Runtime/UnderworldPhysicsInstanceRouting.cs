using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Routes Unity's ordinary static 3D physics queries to the PhysicsScene owned by the current
/// Valheim world instance. Unity's Physics.Raycast/SphereCast/etc. wrappers dispatch through
/// Physics.defaultPhysicsScene; overriding this getter while instance 1 is scoped preserves
/// vanilla call sites without merging Surface and Underworld colliders.
/// </summary>
[HarmonyPatch]
internal static class UnderworldDefaultPhysicsScenePatch
{
    internal static System.Reflection.MethodBase TargetMethod() =>
        typeof(Physics).GetProperty(nameof(Physics.defaultPhysicsScene))?.GetGetMethod()
        ?? throw new MissingMethodException(typeof(Physics).FullName, "get_defaultPhysicsScene");

    private static void Postfix(ref PhysicsScene __result)
    {
        if (!ValheimWorldInstanceExecution.TryGetAmbientContext(out var context) ||
            context is null || !context.InstanceId.IsUnderworld || !context.PhysicsScene.IsValid())
            return;
        __result = context.PhysicsScene;
    }
}

/// <summary>
/// Heightmap keeps a global static registry. Surface and Underworld deliberately reuse logical X/Z,
/// so vanilla's unfiltered scan can otherwise return a tile from the wrong world instance.
/// </summary>
internal static class UnderworldHeightmapLookup
{
    internal static bool TryFind(Vector3 point, out Heightmap? result)
    {
        result = null;
        if (!ValheimWorldInstanceExecution.TryGetQueryScene(out var scene)) return false;

        foreach (var heightmap in Heightmap.GetAllHeightmaps())
        {
            if (!heightmap || heightmap.gameObject.scene.handle != scene.handle) continue;
            if (!heightmap.IsPointInside(point, 0f)) continue;
            result = heightmap;
            break;
        }
        return true;
    }

    internal static bool TryFind(Vector3 point, float radius, List<Heightmap> results)
    {
        if (!ValheimWorldInstanceExecution.TryGetQueryScene(out var scene)) return false;

        foreach (var heightmap in Heightmap.GetAllHeightmaps())
        {
            if (!heightmap || heightmap.gameObject.scene.handle != scene.handle) continue;
            if (!heightmap.IsPointInside(point, radius) || results.Contains(heightmap)) continue;
            results.Add(heightmap);
        }
        return true;
    }
}

[HarmonyPatch(typeof(Heightmap), nameof(Heightmap.FindHeightmap), new[] { typeof(Vector3) })]
internal static class UnderworldHeightmapFindPointPatch
{
    private static bool Prefix(Vector3 point, ref Heightmap __result)
    {
        if (!UnderworldHeightmapLookup.TryFind(point, out var result)) return true;
        __result = result!;
        return false;
    }
}

[HarmonyPatch(typeof(Heightmap), nameof(Heightmap.FindHeightmap), new[] { typeof(Vector3), typeof(float), typeof(List<Heightmap>) })]
internal static class UnderworldHeightmapFindRadiusPatch
{
    private static bool Prefix(Vector3 point, float radius, List<Heightmap> heightmaps) =>
        !UnderworldHeightmapLookup.TryFind(point, radius, heightmaps);
}


/// <summary>
/// TerrainComp also keeps a global static registry. Filter it by the same world-instance scene as
/// Heightmap so terrain operations at identical X/Z never bind to the other world's compiler.
/// </summary>
[HarmonyPatch(typeof(TerrainComp), nameof(TerrainComp.FindTerrainCompiler), new[] { typeof(Vector3) })]
internal static class UnderworldTerrainCompilerFindPatch
{
    private static readonly System.Reflection.FieldInfo Instances =
        AccessTools.Field(typeof(TerrainComp), "s_instances")
        ?? throw new MissingFieldException(typeof(TerrainComp).FullName, "s_instances");
    private static readonly AccessTools.FieldRef<TerrainComp, Heightmap> HeightmapField =
        AccessTools.FieldRefAccess<TerrainComp, Heightmap>("m_hmap");

    private static bool Prefix(Vector3 __0, ref TerrainComp __result)
    {
        if (!ValheimWorldInstanceExecution.TryGetQueryScene(out var scene)) return true;

        foreach (var compiler in (List<TerrainComp>)Instances.GetValue(null))
        {
            if (!compiler || compiler.gameObject.scene.handle != scene.handle) continue;
            var heightmap = HeightmapField(compiler);
            if (!heightmap || !heightmap.IsPointInside(__0, 0f)) continue;
            __result = compiler;
            return false;
        }

        __result = null!;
        return false;
    }
}
