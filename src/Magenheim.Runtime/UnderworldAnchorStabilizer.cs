using System;
using System.Collections.Generic;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Presence marker for a deployed Fracture-Zone Anchor Spike stabilizer.</summary>
internal sealed class UnderworldAnchorStabilizer : MonoBehaviour
{
    private static readonly HashSet<UnderworldAnchorStabilizer> Active = new();
    [SerializeField] private float _radius = 8f;

    internal void Bind(float radius)
    {
        if (float.IsNaN(radius) || float.IsInfinity(radius) || radius <= 0f)
            throw new ArgumentOutOfRangeException(nameof(radius));
        _radius = radius;
    }

    private void OnEnable() => Active.Add(this);
    private void OnDisable() => Active.Remove(this);
    private void OnDestroy() => Active.Remove(this);

    internal static bool IsNear(Vector3 position, int sceneHandle, float extraRadius = 0f)
    {
        if (float.IsNaN(extraRadius) || float.IsInfinity(extraRadius) || extraRadius < 0f) return false;
        foreach (var anchor in Active)
        {
            if (!anchor || !anchor.isActiveAndEnabled || anchor.gameObject.scene.handle != sceneHandle) continue;
            var view = anchor.GetComponent<ZNetView>();
            if (view is null || !view.IsValid()) continue;
            var radius = anchor._radius + extraRadius;
            if ((anchor.transform.position - position).sqrMagnitude <= radius * radius) return true;
        }
        return false;
    }
}
