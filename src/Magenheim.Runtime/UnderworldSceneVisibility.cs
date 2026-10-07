using System.Collections.Generic;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Additive physics scenes do not isolate rendering. Suppress Surface draws only
/// while the local view is in the Underworld, preserving LOD/renderer enable states.</summary>
internal sealed class UnderworldSceneVisibility
{
    private readonly Dictionary<Renderer, bool> _original = new();
    private readonly List<Renderer> _surface = new();
    private float _nextScan;

    internal void Apply(int surfaceSceneHandle)
    {
        if (Time.unscaledTime < _nextScan) return;
        _nextScan = Time.unscaledTime + .25f;
        _surface.Clear();
        foreach (var renderer in Resources.FindObjectsOfTypeAll<Renderer>())
            if (renderer.gameObject.scene.handle == surfaceSceneHandle &&
                renderer.name != "Magenheim_CavernSky_Background" &&
                renderer.name != "Magenheim_Underworld_OverheadHaze") _surface.Add(renderer);
        foreach (var renderer in _surface)
        {
            if (!renderer || _original.ContainsKey(renderer)) continue;
            _original.Add(renderer, renderer.forceRenderingOff);
            renderer.forceRenderingOff = true;
        }
    }

    internal void Restore()
    {
        foreach (var pair in _original) if (pair.Key) pair.Key.forceRenderingOff = pair.Value;
        _original.Clear();
        _surface.Clear();
        _nextScan = 0;
    }
}
