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
[HarmonyPatch(typeof(Physics), nameof(Physics.defaultPhysicsScene), MethodType.Getter)]
internal static class UnderworldDefaultPhysicsScenePatch
{
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

        foreach (var heightmap in Heightmap.s_heightmaps)
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

        foreach (var heightmap in Heightmap.s_heightmaps)
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
