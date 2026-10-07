using System.Collections;
using HarmonyLib;
using UnityEngine;

namespace Magenheim.Runtime;

internal static class DeepFractureInteriorEnvironment
{
    private static readonly System.Reflection.FieldInfo Locations = AccessTools.Field(typeof(Location), "s_allLocations");

    internal static void Apply(Location location)
    {
        var root = location.m_interiorTransform;
        if (!root || !root.GetComponent<DeepFractureInteriorRuntime>()) return;
        foreach (Transform child in location.transform)
        {
            if (!child.GetComponent<EnvZone>()) continue;
            // Reuse Valheim's real cave darkness/environment enclosure at our actual
            // underground packing volume, instead of leaving it 5000 metres overhead.
            child.position = root.position + Vector3.down * 160f;
            child.localScale = new Vector3(location.m_interiorRadius * 2f, 512f, location.m_interiorRadius * 2f);
        }
    }

    internal static bool Contains(Vector3 point)
    {
        if (point.y > -1000f || Locations.GetValue(null) is not IEnumerable locations) return false;
        ValheimWorldInstanceExecution.TryGetQueryScene(out var scene);
        foreach (Location location in locations)
        {
            if (!location || (scene.IsValid() && location.gameObject.scene.handle != scene.handle)) continue;
            var root = location.m_interiorTransform;
            if (!root || !root.GetComponent<DeepFractureInteriorRuntime>()) continue;
            var local = root.InverseTransformPoint(point);
            if (local.y < -416f || local.y > 96f) continue;
            if (Mathf.Abs(local.x) <= location.m_interiorRadius && Mathf.Abs(local.z) <= location.m_interiorRadius)
                return true;
        }
        return false;
    }
}

// Vanilla classifies interiors solely as y > 3000. Preserve that native rule and
// additionally recognise only the owned underground cave envelope.
[HarmonyPatch(typeof(Character), nameof(Character.InInterior), new[] { typeof(Vector3) })]
internal static class DeepFractureUndergroundInteriorPatch
{
    private static void Postfix(Vector3 __0, ref bool __result)
    {
        if (!__result) __result = DeepFractureInteriorEnvironment.Contains(__0);
    }
}
