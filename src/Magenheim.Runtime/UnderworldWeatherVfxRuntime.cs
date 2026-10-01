using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Magenheim-owned local weather presentation. These particle systems are authored procedurally at
/// runtime so event identity does not depend on opportunistic donor particle names.
/// </summary>
internal sealed class UnderworldWeatherVfxRuntime : MonoBehaviour
{
    private GameObject? _root;
    private ParticleSystem? _primary;
    private ParticleSystem? _secondary;
    private Light? _resonanceLight;
    private Material? _particleMaterial;
    private ManualLogSource? _log;
    private UnderworldAtmosphereEvent _event = UnderworldAtmosphereEvent.None;
    private UnderworldTerrainBiome _biome;
    private float _intensity;
    private float _suppression;
    private float _nextResonanceScanAt;
    private Player? _player;
    private readonly Dictionary<Renderer, MaterialPropertyBlock> _resonanceOriginals = new();
    private bool _active;

    internal void Configure(ManualLogSource log) =>
        _log = log ?? throw new ArgumentNullException(nameof(log));

    internal void Apply(
        Player player,
        UnderworldTerrainBiome biome,
        UnderworldAtmosphereEvent atmosphereEvent,
        double intensity01,
        double suppression01)
    {
        if (player is null)
        {
            Clear();
            return;
        }

        var intensity = Mathf.Clamp01((float)intensity01);
        var suppression = Mathf.Clamp01((float)suppression01);
        if (atmosphereEvent == UnderworldAtmosphereEvent.None || intensity <= .001f)
        {
            Clear();
            return;
        }

        Ensure(player);
        if (_root is null || _primary is null || _secondary is null) return;

        if (_event != atmosphereEvent || _biome != biome)
        {
            _event = atmosphereEvent;
            _biome = biome;
            ConfigureEvent(atmosphereEvent, biome);
            _log?.LogDebug($"Underworld bespoke weather VFX -> {biome} / {atmosphereEvent}.");
        }

        _player = player;
        _intensity = intensity;
        _suppression = suppression;
        _root.SetActive(true);
        var particleScale = 1f - suppression * .65f;
        SetEmission(_primary, PrimaryRate(atmosphereEvent) * intensity * particleScale);
        SetEmission(_secondary, SecondaryRate(atmosphereEvent) * intensity * particleScale);

        if (!_primary.isPlaying) _primary.Play();
        if (!_secondary.isPlaying) _secondary.Play();
        _active = true;
    }

    private void LateUpdate()
    {
        if (!_active || _root is null) return;
        if (_resonanceLight is not null)
        {
            if (_event == UnderworldAtmosphereEvent.CrystalResonance)
            {
                var wave = .5f + .5f * Mathf.Sin(Time.unscaledTime * 3.4f);
                _resonanceLight.enabled = true;
                _resonanceLight.intensity = Mathf.Lerp(.25f, 2.4f, wave) * _intensity;
                _resonanceLight.range = Mathf.Lerp(8f, 22f, wave);
                RefreshCrystalResonance(wave);
            }
            else
            {
                _resonanceLight.enabled = false;
                RestoreCrystalResonance();
            }
        }
    }

    internal void Clear()
    {
        if (_primary is not null && _primary.isPlaying)
            _primary.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (_secondary is not null && _secondary.isPlaying)
            _secondary.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (_resonanceLight is not null) _resonanceLight.enabled = false;
        RestoreCrystalResonance();
        if (_root is not null) _root.SetActive(false);
        _event = UnderworldAtmosphereEvent.None;
        _intensity = 0f;
        _suppression = 0f;
        _player = null;
        _active = false;
    }

    private void Ensure(Player player)
    {
        if (_root is not null)
        {
            if (_root.transform.parent != player.transform)
                _root.transform.SetParent(player.transform, false);
            return;
        }

        _root = new GameObject("Magenheim_Underworld_WeatherVFX");
        _root.transform.SetParent(player.transform, false);
        _root.transform.localPosition = new Vector3(0f, 6f, 0f);

        var primaryRoot = new GameObject("Primary");
        primaryRoot.transform.SetParent(_root.transform, false);
        _primary = primaryRoot.AddComponent<ParticleSystem>();

        var secondaryRoot = new GameObject("Secondary");
        secondaryRoot.transform.SetParent(_root.transform, false);
        _secondary = secondaryRoot.AddComponent<ParticleSystem>();

        var lightRoot = new GameObject("ResonanceLight");
        lightRoot.transform.SetParent(_root.transform, false);
        _resonanceLight = lightRoot.AddComponent<Light>();
        _resonanceLight.type = LightType.Point;
        _resonanceLight.color = new Color(.48f, .72f, 1f);
        _resonanceLight.shadows = LightShadows.None;
        _resonanceLight.enabled = false;

        _particleMaterial = NativeEffectMaterials.CreateParticleMaterial();
        _particleMaterial.name = "Magenheim_Underworld_WeatherParticles";
        primaryRoot.GetComponent<ParticleSystemRenderer>().sharedMaterial = _particleMaterial;
        secondaryRoot.GetComponent<ParticleSystemRenderer>().sharedMaterial = _particleMaterial;

        ConfigureCommon(_primary);
        ConfigureCommon(_secondary);
        _root.SetActive(false);
    }

    private static void ConfigureCommon(ParticleSystem particles)
    {
        var main = particles.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 480;

        var shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 18f;

        var emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
    }

    private void ConfigureEvent(
        UnderworldAtmosphereEvent atmosphereEvent,
        UnderworldTerrainBiome biome)
    {
        if (_primary is null || _secondary is null) return;

        var profile = ProfileFor(atmosphereEvent, biome);
        ApplyProfile(_primary, profile.Primary);
        ApplyProfile(_secondary, profile.Secondary);
        if (_resonanceLight is not null)
            _resonanceLight.color = profile.LightColor;
    }

    private static void ApplyProfile(ParticleSystem particles, LayerProfile profile)
    {
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = particles.main;
        main.startLifetime = profile.Lifetime;
        main.startSpeed = profile.Speed;
        main.startSize = profile.Size;
        main.startColor = profile.Color;
        main.gravityModifier = profile.Gravity;
        main.maxParticles = profile.MaxParticles;

        var shape = particles.shape;
        shape.radius = profile.Radius;

        var velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = profile.Velocity.x;
        velocity.y = profile.Velocity.y;
        velocity.z = profile.Velocity.z;

        var rotation = particles.rotationOverLifetime;
        rotation.enabled = profile.Rotation != 0f;
        rotation.z = profile.Rotation;
    }

    private void RefreshCrystalResonance(float wave)
    {
        var player = _player;
        if (player is null) return;

        if (Time.unscaledTime >= _nextResonanceScanAt)
        {
            _nextResonanceScanAt = Time.unscaledTime + 1f;
            var scene = player.gameObject.scene.handle;
            var origin = player.transform.position;
            var captured = 0;
            foreach (var renderer in Resources.FindObjectsOfTypeAll<Renderer>())
            {
                if (!renderer || !renderer.gameObject.activeInHierarchy ||
                    renderer.gameObject.scene.handle != scene ||
                    (renderer.transform.position - origin).sqrMagnitude > 55f * 55f ||
                    !LooksCrystalline(renderer))
                    continue;

                if (!_resonanceOriginals.ContainsKey(renderer))
                {
                    var original = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(original);
                    _resonanceOriginals.Add(renderer, original);
                }

                captured++;
                if (captured >= 40) break;
            }
        }

        var color = Color.Lerp(
            new Color(.10f, .28f, .42f),
            new Color(.62f, .82f, 1f),
            wave) * (_intensity * (1f - _suppression * .45f));
        foreach (var pair in _resonanceOriginals)
        {
            var renderer = pair.Key;
            if (!renderer) continue;
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_EmissionColor", color);
            renderer.SetPropertyBlock(block);
        }
    }

    private static bool LooksCrystalline(Renderer renderer)
    {
        var objectName = renderer.name ?? string.Empty;
        if (ContainsCrystalToken(objectName)) return true;
        foreach (var material in renderer.sharedMaterials)
        {
            if (!material) continue;
            var materialName = material.name ?? string.Empty;
            if (ContainsCrystalToken(materialName) && material.HasProperty("_EmissionColor"))
                return true;
        }
        return false;
    }

    private static bool ContainsCrystalToken(string value) =>
        value.IndexOf("crystal", StringComparison.OrdinalIgnoreCase) >= 0 ||
        value.IndexOf("geode", StringComparison.OrdinalIgnoreCase) >= 0 ||
        value.IndexOf("shard", StringComparison.OrdinalIgnoreCase) >= 0;

    private void RestoreCrystalResonance()
    {
        foreach (var pair in _resonanceOriginals)
            if (pair.Key) pair.Key.SetPropertyBlock(pair.Value);
        _resonanceOriginals.Clear();
        _nextResonanceScanAt = 0f;
    }

    private static void SetEmission(ParticleSystem particles, float rate)
    {
        var emission = particles.emission;
        emission.rateOverTime = Mathf.Max(0f, rate);
    }

    private static float PrimaryRate(UnderworldAtmosphereEvent atmosphereEvent) => atmosphereEvent switch
    {
        UnderworldAtmosphereEvent.Sporefall => 72f,
        UnderworldAtmosphereEvent.DeepFog => 24f,
        UnderworldAtmosphereEvent.Ashfall => 82f,
        UnderworldAtmosphereEvent.ThermalSurge => 60f,
        UnderworldAtmosphereEvent.Whiteout => 150f,
        UnderworldAtmosphereEvent.StoneRain => 34f,
        UnderworldAtmosphereEvent.CrystalResonance => 42f,
        UnderworldAtmosphereEvent.BlackBloom => 80f,
        _ => 0f,
    };

    private static float SecondaryRate(UnderworldAtmosphereEvent atmosphereEvent) => atmosphereEvent switch
    {
        UnderworldAtmosphereEvent.Sporefall => 26f,
        UnderworldAtmosphereEvent.DeepFog => 12f,
        UnderworldAtmosphereEvent.Ashfall => 30f,
        UnderworldAtmosphereEvent.ThermalSurge => 40f,
        UnderworldAtmosphereEvent.Whiteout => 70f,
        UnderworldAtmosphereEvent.StoneRain => 58f,
        UnderworldAtmosphereEvent.CrystalResonance => 18f,
        UnderworldAtmosphereEvent.BlackBloom => 34f,
        _ => 0f,
    };

    private static EventProfile ProfileFor(
        UnderworldAtmosphereEvent atmosphereEvent,
        UnderworldTerrainBiome biome)
    {
        switch (atmosphereEvent)
        {
            case UnderworldAtmosphereEvent.Sporefall:
                return new EventProfile(
                    new LayerProfile(new Color(.42f, .94f, .68f, .62f), 6.5f, .05f, .075f, 0f,
                        new Vector3(.04f, -.28f, .03f), 18f, 360, .20f),
                    new LayerProfile(new Color(.42f, .62f, 1f, .42f), 3.8f, .08f, .035f, 0f,
                        new Vector3(-.03f, -.12f, .04f), 14f, 180, -.35f),
                    new Color(.30f, .72f, .58f));

            case UnderworldAtmosphereEvent.DeepFog:
                return new EventProfile(
                    new LayerProfile(
                        biome == UnderworldTerrainBiome.FrozenCaverns
                            ? new Color(.72f, .88f, 1f, .16f)
                            : new Color(.22f, .36f, .44f, .15f),
                        9f, .03f, .55f, -.01f, new Vector3(.05f, .01f, .04f), 20f, 180, .03f),
                    new LayerProfile(new Color(.56f, .72f, .80f, .10f), 5f, .02f, .22f, 0f,
                        new Vector3(-.04f, -.01f, .03f), 14f, 100, 0f),
                    new Color(.20f, .34f, .42f));

            case UnderworldAtmosphereEvent.Ashfall:
                return new EventProfile(
                    new LayerProfile(new Color(.20f, .18f, .14f, .72f), 5.5f, .20f, .055f, .18f,
                        new Vector3(.28f, -1.35f, .10f), 20f, 420, 1.8f),
                    new LayerProfile(new Color(.58f, .49f, .18f, .34f), 4f, .08f, .025f, .05f,
                        new Vector3(.10f, -.65f, -.08f), 15f, 180, 1.2f),
                    new Color(.58f, .30f, .12f));

            case UnderworldAtmosphereEvent.ThermalSurge:
                return new EventProfile(
                    new LayerProfile(new Color(1f, .34f, .05f, .74f), 2.4f, .85f, .045f, -.16f,
                        new Vector3(.12f, 1.8f, .08f), 15f, 250, 2.4f),
                    new LayerProfile(new Color(.74f, .57f, .16f, .24f), 5.2f, .12f, .18f, -.04f,
                        new Vector3(-.08f, .55f, .05f), 18f, 160, .30f),
                    new Color(1f, .30f, .06f));

            case UnderworldAtmosphereEvent.Whiteout:
                return new EventProfile(
                    new LayerProfile(new Color(.86f, .96f, 1f, .82f), 3.6f, 3.6f, .055f, .04f,
                        new Vector3(6.4f, -.55f, 1.4f), 21f, 480, 4.2f),
                    new LayerProfile(new Color(.62f, .82f, .94f, .34f), 4.5f, 1.4f, .025f, .01f,
                        new Vector3(3.8f, -.22f, -.9f), 18f, 300, -3.2f),
                    new Color(.62f, .84f, 1f));

            case UnderworldAtmosphereEvent.StoneRain:
                return new EventProfile(
                    new LayerProfile(new Color(.28f, .25f, .31f, .92f), 2.6f, .25f, .11f, 1.35f,
                        new Vector3(.18f, -4.8f, .14f), 17f, 190, 5.8f),
                    new LayerProfile(new Color(.50f, .42f, .56f, .26f), 3.5f, .30f, .045f, .15f,
                        new Vector3(.42f, -.72f, -.20f), 14f, 260, 2.2f),
                    new Color(.50f, .32f, .68f));

            case UnderworldAtmosphereEvent.CrystalResonance:
                return new EventProfile(
                    new LayerProfile(new Color(.46f, .82f, 1f, .88f), 2.2f, .16f, .045f, -.03f,
                        new Vector3(.02f, .24f, .03f), 16f, 220, 2.8f),
                    new LayerProfile(new Color(.78f, .44f, 1f, .70f), 1.35f, .22f, .025f, -.02f,
                        new Vector3(-.03f, .34f, .02f), 13f, 150, -3.4f),
                    new Color(.48f, .72f, 1f));

            case UnderworldAtmosphereEvent.BlackBloom:
                return new EventProfile(
                    new LayerProfile(new Color(.26f, .34f, .08f, .60f), 6.2f, .07f, .10f, -.04f,
                        new Vector3(.10f, .38f, -.08f), 19f, 390, .75f),
                    new LayerProfile(new Color(.08f, .06f, .05f, .48f), 7.5f, .04f, .22f, -.02f,
                        new Vector3(-.06f, .16f, .04f), 17f, 220, -.30f),
                    new Color(.44f, .56f, .10f));

            default:
                return default;
        }
    }

    private void OnDisable() => Clear();

    private void OnDestroy()
    {
        Clear();
        if (_root is not null) Destroy(_root);
        if (_particleMaterial is not null) Destroy(_particleMaterial);
        _root = null;
        _particleMaterial = null;
    }

    private readonly record struct LayerProfile(
        Color Color,
        float Lifetime,
        float Speed,
        float Size,
        float Gravity,
        Vector3 Velocity,
        float Radius,
        int MaxParticles,
        float Rotation);

    private readonly record struct EventProfile(
        LayerProfile Primary,
        LayerProfile Secondary,
        Color LightColor);
}
