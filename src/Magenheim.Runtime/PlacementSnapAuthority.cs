using System;
using System.Linq;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Repositions snap transforms inherited from vanilla donor prefabs so they describe the owned
/// Magenheim geometry. Donor snap coordinates are never treated as authoritative model data.
/// </summary>
internal static class PlacementSnapAuthority
{
    internal static void AlignToBase(GameObject prefab)
    {
        foreach (var snap in SnapPoints(prefab))
        {
            var position = snap.localPosition;
            position.y = 0f;
            snap.localPosition = position;
        }
    }

    internal static void FitFoundation(GameObject prefab, float size) =>
        FitRectangle(prefab, size, size);

    internal static void FitRectangle(GameObject prefab, float width, float depth)
    {
        if (width <= 0f || float.IsNaN(width) || float.IsInfinity(width))
            throw new ArgumentOutOfRangeException(nameof(width));
        if (depth <= 0f || float.IsNaN(depth) || float.IsInfinity(depth))
            throw new ArgumentOutOfRangeException(nameof(depth));

        var snaps = SnapPoints(prefab);
        if (snaps.Length == 0) return;

        var maxX = snaps.Max(snap => Mathf.Abs(snap.localPosition.x));
        var maxZ = snaps.Max(snap => Mathf.Abs(snap.localPosition.z));
        var targetHalfX = width * .5f;
        var targetHalfZ = depth * .5f;
        var scaleX = maxX > .01f ? targetHalfX / maxX : 1f;
        var scaleZ = maxZ > .01f ? targetHalfZ / maxZ : 1f;

        foreach (var snap in snaps)
        {
            var position = snap.localPosition;
            position.x *= scaleX;
            position.y = 0f;
            position.z *= scaleZ;
            snap.localPosition = position;
        }
    }

    internal static void FitVerticalBeam(GameObject prefab, float height)
    {
        var snaps = SnapPoints(prefab);
        if (snaps.Length == 0) return;

        var min = snaps.Min(snap => snap.localPosition.y);
        var max = snaps.Max(snap => snap.localPosition.y);
        var range = max - min;

        if (range <= .01f)
        {
            AlignToBase(prefab);
            return;
        }

        foreach (var snap in snaps)
        {
            var position = snap.localPosition;
            var normalized = Mathf.Clamp01((position.y - min) / range);
            position.x = 0f;
            position.y = normalized * height;
            position.z = 0f;
            snap.localPosition = position;
        }
    }

    internal static void FitRadialBase(GameObject prefab, float radius)
    {
        if (radius <= 0f || float.IsNaN(radius) || float.IsInfinity(radius))
            throw new ArgumentOutOfRangeException(nameof(radius));

        foreach (var snap in SnapPoints(prefab))
        {
            var position = snap.localPosition;
            var planar = new Vector2(position.x, position.z);
            if (planar.sqrMagnitude > .0025f)
            {
                planar = planar.normalized * radius;
                position.x = planar.x;
                position.z = planar.y;
            }
            position.y = 0f;
            snap.localPosition = position;
        }
    }

    private static Transform[] SnapPoints(GameObject prefab)
    {
        if (prefab is null) throw new ArgumentNullException(nameof(prefab));
        return prefab.GetComponentsInChildren<Transform>(true)
            .Where(transform => transform != prefab.transform &&
                transform.name.IndexOf("snap", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
    }
}
