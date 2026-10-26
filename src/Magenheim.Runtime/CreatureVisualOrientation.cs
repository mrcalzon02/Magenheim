using System;
using UnityEngine;

namespace Magenheim.Runtime;

internal static class CreatureVisualOrientation
{
    // The rigid v1 exporter stores Blender +Y anatomy as Unity -Z ([x,z,-y]).
    // Weighted v1 stores that same forward direction as Unity +Z ([-x,z,y]).
    // Rotate the complete legacy visual and skeleton together after binding;
    // native character forward is +Z. Geometry, joint frames and clips stay in
    // their own consistent export frame rather than being independently flipped.
    internal static Quaternion Correction(string kind) => kind switch
    {
        "rigid-segment-creature-v1" => Quaternion.Euler(0f, 180f, 0f),
        "weighted-creature-v1" => Quaternion.identity,
        _ => throw new InvalidOperationException("Unknown creature forward convention: " + kind)
    };
}
