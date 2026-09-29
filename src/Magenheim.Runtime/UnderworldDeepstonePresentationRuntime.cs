using System;
using System.Collections.Generic;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Visual-only projection of the authoritative Deepstone persistent state. It never grants or
/// mutates progression; it only makes the Conclave visibly remember which stones are awakened.
/// </summary>
internal sealed class UnderworldDeepstonePresentationRuntime : MonoBehaviour
{
    private UnderworldDeepstoneRuntime? _runtime;
    private readonly List<MaterialState> _materials = new();
    private readonly List<LightState> _lights = new();
    private bool? _lastAwakened;
    private float _nextRefresh;

    internal void Bind(UnderworldDeepstoneRuntime runtime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        CaptureOwnedPresentation();
        Refresh(force: true);
    }

    private void CaptureOwnedPresentation()
    {
        _materials.Clear();
        _lights.Clear();

        foreach (var renderer in GetComponentsInChildren<MeshRenderer>(true))
        {
            // ModelAssets caches materials by model-part identity. Clone before changing emission
            // so progression on one Deepstone can never alter another cached authored material.
            var source = renderer.sharedMaterial;
            if (!source || !source.HasProperty("_EmissionColor")) continue;
            var material = new Material(source)
            {
                name = source.name + ".deepstone-state"
            };
            renderer.sharedMaterial = material;
            _materials.Add(new MaterialState(material, material.GetColor("_EmissionColor")));
        }

        foreach (var light in GetComponentsInChildren<Light>(true))
            if (light)
                _lights.Add(new LightState(light, light.intensity));
    }

    private void Update()
    {
        if (Time.time < _nextRefresh) return;
        _nextRefresh = Time.time + .25f;
        Refresh(force: false);
    }

    private void Refresh(bool force)
    {
        if (_runtime is null || !_runtime.IsBound) return;
        var awakened = _runtime.TrophyMounted && _runtime.BoonUnlocked;
        if (!force && _lastAwakened == awakened)
        {
            if (awakened) ApplyLightPulse();
            return;
        }

        _lastAwakened = awakened;
        var emissionScale = awakened ? 1.70f : .42f;
        foreach (var state in _materials)
        {
            if (!state.Material) continue;
            state.Material.SetColor("_EmissionColor", state.BaseEmission * emissionScale);
            if (state.BaseEmission.r + state.BaseEmission.g + state.BaseEmission.b > 0f)
                state.Material.EnableKeyword("_EMISSION");
        }

        foreach (var state in _lights)
            if (state.Light)
                state.Light.intensity = state.BaseIntensity * (awakened ? 1.35f : .45f);
    }

    private void ApplyLightPulse()
    {
        var pulse = 1.0f + Mathf.Sin(Time.time * 1.65f) * .08f;
        foreach (var state in _lights)
            if (state.Light)
                state.Light.intensity = state.BaseIntensity * 1.35f * pulse;
    }

    private readonly record struct MaterialState(Material Material, Color BaseEmission);
    private readonly record struct LightState(Light Light, float BaseIntensity);
}
