using System.Collections.Generic;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Hides the loaded Underworld scene from a local player who is physically on Surface.</summary>
internal sealed class UnderworldScenePresentationIsolation : MonoBehaviour
{
    private readonly Dictionary<Renderer, bool> _renderers = new();
    private readonly Dictionary<Light, bool> _lights = new();
    private readonly Dictionary<AudioSource, bool> _audio = new();
    private bool _initialized;
    private bool _visible;
    private float _nextHiddenScan;

    private void Update()
    {
        if (!_visible && Time.unscaledTime < _nextHiddenScan) return;
        RefreshNow();
    }

    internal void RefreshNow()
    {
        RestoreTransferredObjects();
        var local = Player.m_localPlayer;
        var visible = local && local.gameObject.scene.handle == gameObject.scene.handle;
        if (!_initialized || visible != _visible)
        {
            _initialized = true;
            _visible = visible;
            if (visible) Restore();
            else CaptureAndHide();
            return;
        }

        if (!visible) CaptureAndHide();
    }

    private void CaptureAndHide()
    {
        _nextHiddenScan = Time.unscaledTime + 0.2f;
        var scene = gameObject.scene;
        if (!scene.IsValid() || !scene.isLoaded) return;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                // Inactive authoring/prefab objects retain their renderer flags so
                // future native instances do not inherit an invisible visual.
                if (!renderer || !renderer.gameObject.activeInHierarchy) continue;
                if (!_renderers.ContainsKey(renderer)) _renderers.Add(renderer, renderer.enabled);
                renderer.enabled = false;
            }
            foreach (var light in root.GetComponentsInChildren<Light>(true))
            {
                if (!light || !light.gameObject.activeInHierarchy) continue;
                if (!_lights.ContainsKey(light)) _lights.Add(light, light.enabled);
                light.enabled = false;
            }
            foreach (var source in root.GetComponentsInChildren<AudioSource>(true))
            {
                if (!source || !source.gameObject.activeInHierarchy) continue;
                if (!_audio.ContainsKey(source)) _audio.Add(source, source.mute);
                source.mute = true;
            }
        }
    }

    internal void RestoreTransferredObjects()
    {
        var scene = gameObject.scene.handle;
        var renderers = new List<Renderer>();
        foreach (var entry in _renderers)
            if (!entry.Key || entry.Key.gameObject.scene.handle != scene)
            {
                if (entry.Key) entry.Key.enabled = entry.Value;
                renderers.Add(entry.Key!);
            }
        foreach (var key in renderers) _renderers.Remove(key);
        var lights = new List<Light>();
        foreach (var entry in _lights)
            if (!entry.Key || entry.Key.gameObject.scene.handle != scene)
            {
                if (entry.Key) entry.Key.enabled = entry.Value;
                lights.Add(entry.Key!);
            }
        foreach (var key in lights) _lights.Remove(key);
        var audio = new List<AudioSource>();
        foreach (var entry in _audio)
            if (!entry.Key || entry.Key.gameObject.scene.handle != scene)
            {
                if (entry.Key) entry.Key.mute = entry.Value;
                audio.Add(entry.Key!);
            }
        foreach (var key in audio) _audio.Remove(key);
    }

    private void Restore()
    {
        foreach (var entry in _renderers) if (entry.Key) entry.Key.enabled = entry.Value;
        foreach (var entry in _lights) if (entry.Key) entry.Key.enabled = entry.Value;
        foreach (var entry in _audio) if (entry.Key) entry.Key.mute = entry.Value;
        _renderers.Clear();
        _lights.Clear();
        _audio.Clear();
    }
}
