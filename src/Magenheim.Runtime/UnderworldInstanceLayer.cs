using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Lowest-level engine-space adapter for the dedicated Underworld instance.
///
/// Gameplay, terrain, biome, map and persistence authorities remain in native instance coordinates.
/// Valheim still needs physical Unity coordinates for meshes, physics and Character.TeleportTo, so
/// this adapter backs instance index 1 in a vertical engine layer that is disjoint from both the
/// Surface and Valheim's ordinary dungeon-interior band. The Y offset is never gameplay authority.
/// </summary>
internal static class UnderworldInstanceLayer
{
    internal const int InstanceIndex = 1;
    internal const float EngineBaseY = 12000f;
    internal const float EngineMinimumY = 11000f;
    internal const float EngineMaximumY = 14500f;

    internal static Vector3 ToEngine(Vector3 logical) =>
        new(logical.x, logical.y + EngineBaseY, logical.z);

    internal static Vector3 ToLogical(Vector3 engine) =>
        new(engine.x, engine.y - EngineBaseY, engine.z);

    internal static bool IsUnderworldEnginePosition(Vector3 engine) =>
        engine.y >= EngineMinimumY && engine.y <= EngineMaximumY;
}
