using HarmonyLib;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Sizes Valheim's spawned interior mask/environment to the private packing volume.</summary>
internal sealed class UnderworldDungeonInteriorEnvelope : MonoBehaviour
{
    [SerializeField] private Vector3 _packingSize;
    [SerializeField] private float _tileWidth;
    internal void Bind(DungeonGenerator generator)
    {
        _packingSize = generator.m_zoneSize;
        _tileWidth = generator.m_tileWidth;
    }

    internal void BindRetainedLayout(DungeonGenerator nativeGenerator, UnderworldVanillaDungeonReuseDefinition profile)
    {
        // A recovered Surface site keeps its existing native room/loot ZDOs. Its
        // original expanded layout still needs clearance even after restoring the exterior.
        _packingSize = nativeGenerator.m_zoneSize * (float)(profile.LinearRoomScale * System.Math.Sqrt(profile.RoomCountMultiplier));
        _tileWidth = nativeGenerator.m_tileWidth * (float)profile.LinearRoomScale;
    }

    internal void Apply(Location location)
    {
        // Location.Awake creates this native EnvZone as a direct child with scale
        // 64/500/64. Its interior_cube renderer is a darkness enclosure, not room art.
        for (var index = 0; index < location.transform.childCount; index++)
        {
            var child = location.transform.GetChild(index);
            if (!child.GetComponent<EnvZone>()) continue;
            var native = child.localScale;
            child.localScale = new Vector3(
                (float)UnderworldDungeonBoundaryPolicy.EnvelopeDimension(native.x, _packingSize.x, _tileWidth),
                (float)UnderworldDungeonBoundaryPolicy.EnvelopeDimension(native.y, _packingSize.y, _tileWidth),
                (float)UnderworldDungeonBoundaryPolicy.EnvelopeDimension(native.z, _packingSize.z, _tileWidth));
        }
    }
}

[HarmonyPatch(typeof(Location), "Awake")]
internal static class UnderworldDungeonInteriorEnvelopePatch
{
    private static void Postfix(Location __instance)
    {
        var envelope = __instance.GetComponent<UnderworldDungeonInteriorEnvelope>();
        if (envelope) envelope.Apply(__instance);
        DeepFractureInteriorEnvironment.Apply(__instance);
    }
}
