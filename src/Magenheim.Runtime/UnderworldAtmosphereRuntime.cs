using System;
using BepInEx.Logging;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Thin local presentation adapter for the shared pure Underworld atmosphere authority.
/// It does not register a competing Valheim environment, select events, or persist player state.
/// Current Valheim fog settings are restored whenever the local player leaves the active instance.
/// </summary>
internal sealed class UnderworldAtmosphereRuntime : MonoBehaviour
{
    private const float SampleIntervalSeconds = 0.20f;
    private const float TransitionRate = 3.5f;
    private const float MinimumFogDensity = 0.00005f;
    private const float MaximumFogDensity = 0.065f;

    private static readonly int DensityShaderId = Shader.PropertyToID("_MagenheimUnderworldAtmosphereDensity");
    private static readonly int ExposureShaderId = Shader.PropertyToID("_MagenheimUnderworldAtmosphereExposure");
    private static readonly int ParticleShaderId = Shader.PropertyToID("_MagenheimUnderworldAtmosphereParticles");
    private static readonly int AudioShaderId = Shader.PropertyToID("_MagenheimUnderworldAtmosphereAudioDamping");
    private static readonly int FogColorShaderId = Shader.PropertyToID("_MagenheimUnderworldAtmosphereColor");

    private UnderworldRuntimeServices? _services;
    private ManualLogSource? _log;
    private float _nextSampleAt;
    private bool _active;
    private FogSnapshot _surfaceFog;
    private Camera? _instanceCamera;
    private float _surfaceFarClip;
    private UnderworldAtmosphereState _target;
    private float _visualDensity;
    private float _visibility;
    private float _exposure;
    private float _particles;
    private float _audioDamping;
    private Color _fogColor;
    private UnderworldTerrainBiome? _lastBiome;
    private UnderworldAtmosphereEvent _event;
    private double _eventIntensity;
    private double _resistance;
    private double _suppression;

    internal void Configure(UnderworldRuntimeServices services, ManualLogSource log)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    /// <summary>
    /// Event choice remains server authority. A future synchronized event runtime feeds that state
    /// here; this component deliberately never rolls or selects an event by itself.
    /// </summary>
    internal void ApplySynchronizedEvent(UnderworldAtmosphereEvent atmosphereEvent, double intensity01)
    {
        if (double.IsNaN(intensity01) || double.IsInfinity(intensity01))
            throw new ArgumentOutOfRangeException(nameof(intensity01));
        _event = atmosphereEvent;
        _eventIntensity = Clamp01(intensity01);
    }

    /// <summary>
    /// Supplies local effect results without teaching the atmosphere system how equipment works.
    /// Resistance reduces gameplay pressure; suppression additionally clears the visual field.
    /// </summary>
    internal void ApplyMitigation(double resistance01, double suppression01)
    {
        if (double.IsNaN(resistance01) || double.IsInfinity(resistance01))
            throw new ArgumentOutOfRangeException(nameof(resistance01));
        if (double.IsNaN(suppression01) || double.IsInfinity(suppression01))
            throw new ArgumentOutOfRangeException(nameof(suppression01));
        _resistance = Clamp01(resistance01);
        _suppression = Clamp01(suppression01);
    }

    private void Update()
    {
        if (_services is null) return;
        if (!TryResolveActivePlayer(out var player) || player is null)
        {
            Deactivate();
            return;
        }

        if (Time.unscaledTime < _nextSampleAt) return;
        _nextSampleAt = Time.unscaledTime + SampleIntervalSeconds;

        var position = UnderworldInstanceLayer.ToLogical(player.transform.position);
        var terrain = UnderworldTerrainRuntime.SampleInstanceTerrain(position.x, position.y, position.z);
        if (!terrain.Admitted)
        {
            Deactivate();
            return;
        }

        var target = UnderworldAtmosphere.Evaluate(new UnderworldAtmosphereInput(
            terrain.Biome,
            terrain.Height,
            terrain.WaterDepth,
            terrain.Hazard01,
            _event,
            _eventIntensity,
            _resistance,
            _suppression));

        if (!_active)
        {
            _surfaceFog = FogSnapshot.Capture();
            _active = true;
            _visualDensity = (float)target.VisualDensity01;
            _visibility = (float)target.VisibilityMeters;
            _exposure = (float)target.Exposure01;
            _particles = (float)target.ParticleDensity01;
            _audioDamping = (float)target.AudioDamping01;
            _fogColor = ToColor(target);
        }

        _target = target;
        if (_lastBiome != terrain.Biome)
        {
            _lastBiome = terrain.Biome;
            _log?.LogDebug($"Underworld atmosphere -> {terrain.Biome} / {target.Kind}; visibility target {target.VisibilityMeters:0}m, density {target.VisualDensity01:0.00}.");
        }
    }

    private void LateUpdate()
    {
        if (!_active) return;
        var camera = Camera.main;
        if (camera != _instanceCamera)
        {
            RestoreCamera();
            _instanceCamera = camera;
            if (_instanceCamera) _surfaceFarClip = _instanceCamera.farClipPlane;
        }
        if (_instanceCamera) _instanceCamera.farClipPlane = Mathf.Max(_surfaceFarClip, 22000f);
        var amount = 1f - Mathf.Exp(-Mathf.Max(0f, Time.unscaledDeltaTime) * TransitionRate);
        _visualDensity = Mathf.Lerp(_visualDensity, (float)_target.VisualDensity01, amount);
        _visibility = Mathf.Lerp(_visibility, (float)_target.VisibilityMeters, amount);
        _exposure = Mathf.Lerp(_exposure, (float)_target.Exposure01, amount);
        _particles = Mathf.Lerp(_particles, (float)_target.ParticleDensity01, amount);
        _audioDamping = Mathf.Lerp(_audioDamping, (float)_target.AudioDamping01, amount);
        _fogColor = Color.Lerp(_fogColor, ToColor(_target), amount);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = _fogColor;
        RenderSettings.fogDensity = Mathf.Clamp(1.55f / Mathf.Max(8f, _visibility),
            MinimumFogDensity, MaximumFogDensity);
        RenderSettings.fogStartDistance = 0f;
        RenderSettings.fogEndDistance = Mathf.Max(8f, _visibility);

        Shader.SetGlobalFloat(DensityShaderId, _visualDensity);
        Shader.SetGlobalFloat(ExposureShaderId, _exposure);
        Shader.SetGlobalFloat(ParticleShaderId, _particles);
        Shader.SetGlobalFloat(AudioShaderId, _audioDamping);
        Shader.SetGlobalColor(FogColorShaderId, _fogColor);
    }

    private bool TryResolveActivePlayer(out Player? player)
    {
        player = null;
        var services = _services;
        if (services is null) return false;
        var lifecycle = services.InstanceLifecycle;
        if (lifecycle.Phase != UnderworldInstancePhase.Active || lifecycle.Identity is null) return false;
        if (!services.TryResolveLocalSession(out var identity, out var layer, out _, out _) ||
            identity is null || layer != UnderworldLayer.Underworld)
            return false;
        if (!string.Equals(identity.DerivedWorldId, lifecycle.Identity.DerivedWorldId, StringComparison.Ordinal) ||
            !string.Equals(identity.ParentWorldId, lifecycle.Identity.ParentWorldId, StringComparison.Ordinal) ||
            !string.Equals(identity.DerivedSeedFingerprint, lifecycle.Identity.DerivedSeedFingerprint, StringComparison.Ordinal))
            return false;
        player = Player.m_localPlayer;
        return player is not null;
    }

    private void Deactivate()
    {
        if (!_active) return;
        _surfaceFog.Restore();
        RestoreCamera();
        _active = false;
        _lastBiome = null;
        _event = UnderworldAtmosphereEvent.None;
        _eventIntensity = 0d;
        ClearShaderGlobals();
    }

    private void RestoreCamera()
    {
        if (_instanceCamera) _instanceCamera.farClipPlane = _surfaceFarClip;
        _instanceCamera = null;
    }

    private void OnDisable() => Deactivate();

    private void OnDestroy() => Deactivate();

    private static Color ToColor(UnderworldAtmosphereState state) =>
        new((float)state.FogRed01, (float)state.FogGreen01, (float)state.FogBlue01, 1f);

    private static double Clamp01(double value) => Math.Max(0d, Math.Min(1d, value));

    private static void ClearShaderGlobals()
    {
        Shader.SetGlobalFloat(DensityShaderId, 0f);
        Shader.SetGlobalFloat(ExposureShaderId, 0f);
        Shader.SetGlobalFloat(ParticleShaderId, 0f);
        Shader.SetGlobalFloat(AudioShaderId, 0f);
        Shader.SetGlobalColor(FogColorShaderId, Color.black);
    }

    private readonly struct FogSnapshot
    {
        private readonly bool _enabled;
        private readonly FogMode _mode;
        private readonly Color _color;
        private readonly float _density;
        private readonly float _start;
        private readonly float _end;

        private FogSnapshot(bool enabled, FogMode mode, Color color, float density, float start, float end)
        {
            _enabled = enabled;
            _mode = mode;
            _color = color;
            _density = density;
            _start = start;
            _end = end;
        }

        internal static FogSnapshot Capture() => new(
            RenderSettings.fog,
            RenderSettings.fogMode,
            RenderSettings.fogColor,
            RenderSettings.fogDensity,
            RenderSettings.fogStartDistance,
            RenderSettings.fogEndDistance);

        internal void Restore()
        {
            RenderSettings.fog = _enabled;
            RenderSettings.fogMode = _mode;
            RenderSettings.fogColor = _color;
            RenderSettings.fogDensity = _density;
            RenderSettings.fogStartDistance = _start;
            RenderSettings.fogEndDistance = _end;
        }
    }
}
