using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Replaces Surface weather while the local player is inside the active Underworld instance.
/// Valheim still performs its normal environment interpolation, wind simulation, particle toggling
/// and ambient handling; Magenheim supplies only namespaced EnvSetup clones and a deterministic
/// biome/event choice. No Surface biome environment table is edited.
/// </summary>
internal sealed class UnderworldWeatherRuntime : MonoBehaviour
{
    private const float PollIntervalSeconds = 0.50f;
    private const string EnvironmentPrefix = "Magenheim_Underworld_";

    private UnderworldRuntimeServices? _services;
    private UnderworldAtmosphereRuntime? _atmosphere;
    private ManualLogSource? _log;
    private EnvMan? _registeredManager;
    private readonly List<string> _registeredNames = new();
    private string _forcedEnvironment = string.Empty;
    private float _nextPollAt;
    private UnderworldTerrainBiome? _lastBiome;
    private UnderworldWeatherState _lastWeather;
    private bool _hasWeather;

    internal void Configure(
        UnderworldRuntimeServices services,
        UnderworldAtmosphereRuntime atmosphere,
        ManualLogSource log)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _atmosphere = atmosphere ?? throw new ArgumentNullException(nameof(atmosphere));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    private void Update()
    {
        if (Time.unscaledTime < _nextPollAt) return;
        _nextPollAt = Time.unscaledTime + PollIntervalSeconds;

        if (!TryResolveActive(out var identity, out var player) ||
            identity is null || player is null || EnvMan.instance is null ||
            ZNet.instance is null)
        {
            Deactivate();
            return;
        }

        var manager = EnvMan.instance;
        EnsureRegistered(manager);

        var position = UnderworldInstanceLayer.ToLogical(player.transform.position);
        var terrain = UnderworldTerrainRuntime.SampleInstanceTerrain(
            position.x, position.y, position.z);
        if (!terrain.Admitted)
        {
            Deactivate();
            return;
        }

        var weather = UnderworldWeatherCycle.Evaluate(
            terrain.Biome,
            identity.DerivedSeed32,
            ZNet.instance.GetTimeSeconds());

        var environmentName = EnvironmentName(terrain.Biome, weather.Event);
        if (!string.Equals(_forcedEnvironment, environmentName, StringComparison.Ordinal))
        {
            manager.SetForceEnvironment(environmentName);
            _forcedEnvironment = environmentName;
        }

        _atmosphere?.ApplySynchronizedEvent(weather.Event, weather.Intensity01);

        if (!_hasWeather || _lastBiome != terrain.Biome || _lastWeather != weather)
        {
            _lastBiome = terrain.Biome;
            _lastWeather = weather;
            _hasWeather = true;
            _log?.LogDebug(
                $"Underworld weather -> {terrain.Biome}: {Display(weather.Event)} " +
                $"({weather.Intensity01:0.00}), period {weather.Period}, environment '{environmentName}'.");
        }
    }

    private bool TryResolveActive(out UnderworldWorldIdentity? identity, out Player? player)
    {
        identity = null;
        player = null;
        var services = _services;
        if (services is null) return false;
        var lifecycle = services.InstanceLifecycle;
        if (lifecycle.Phase != UnderworldInstancePhase.Active || lifecycle.Identity is null)
            return false;
        if (!services.TryResolveLocalSession(
                out var localIdentity, out var layer, out _, out _) ||
            localIdentity is null || layer != UnderworldLayer.Underworld)
            return false;
        if (!string.Equals(
                localIdentity.DerivedWorldId,
                lifecycle.Identity.DerivedWorldId,
                StringComparison.Ordinal) ||
            !string.Equals(
                localIdentity.ParentWorldId,
                lifecycle.Identity.ParentWorldId,
                StringComparison.Ordinal) ||
            !string.Equals(
                localIdentity.DerivedSeedFingerprint,
                lifecycle.Identity.DerivedSeedFingerprint,
                StringComparison.Ordinal))
            return false;
        player = Player.m_localPlayer;
        identity = localIdentity;
        return player is not null;
    }

    private void EnsureRegistered(EnvMan manager)
    {
        if (ReferenceEquals(_registeredManager, manager) && _registeredNames.Count > 0)
            return;

        _registeredManager = manager;
        _registeredNames.Clear();

        foreach (UnderworldTerrainBiome biome in Enum.GetValues(typeof(UnderworldTerrainBiome)))
        {
            Register(manager, biome, UnderworldAtmosphereEvent.None);
            foreach (var atmosphereEvent in EventsFor(biome))
                Register(manager, biome, atmosphereEvent);
        }

        _log?.LogInfo(
            $"Registered {_registeredNames.Count} Magenheim Underworld weather environments; " +
            "Surface storm tables remain untouched.");
    }

    private void Register(
        EnvMan manager,
        UnderworldTerrainBiome biome,
        UnderworldAtmosphereEvent atmosphereEvent)
    {
        var name = EnvironmentName(biome, atmosphereEvent);
        foreach (var existing in manager.m_environments)
        {
            if (!string.Equals(existing.m_name, name, StringComparison.Ordinal))
                continue;
            _registeredNames.Add(name);
            return;
        }

        var donor = FindDonor(manager, biome, atmosphereEvent);
        if (donor is null)
            throw new InvalidOperationException(
                $"Cannot register Underworld weather '{name}': Valheim exposes no donor environment.");

        var environment = donor.Clone();
        environment.m_name = name;
        environment.m_default = false;
        ApplyPresentation(environment, biome, atmosphereEvent);
        manager.m_environments.Add(environment);
        _registeredNames.Add(name);
    }

    private static EnvSetup? FindDonor(
        EnvMan manager,
        UnderworldTerrainBiome biome,
        UnderworldAtmosphereEvent atmosphereEvent)
    {
        var candidates = DonorCandidates(biome, atmosphereEvent);
        foreach (var candidate in candidates)
        {
            foreach (var environment in manager.m_environments)
            {
                if (string.Equals(
                        environment.m_name,
                        candidate,
                        StringComparison.OrdinalIgnoreCase))
                    return environment;
            }
        }

        foreach (var environment in manager.m_environments)
        {
            if (environment.m_default && !LooksLikeThunderStorm(environment.m_name))
                return environment;
        }

        foreach (var environment in manager.m_environments)
        {
            if (!LooksLikeThunderStorm(environment.m_name))
                return environment;
        }

        return manager.m_environments.Count > 0 ? manager.m_environments[0] : null;
    }

    private static void ApplyPresentation(
        EnvSetup environment,
        UnderworldTerrainBiome biome,
        UnderworldAtmosphereEvent atmosphereEvent)
    {
        var style = StyleFor(biome, atmosphereEvent);

        environment.m_alwaysDark = true;
        environment.m_isWet = style.Wet;
        environment.m_isCold = style.Cold;
        environment.m_isColdAtNight = false;
        environment.m_isFreezing = style.Freezing;
        environment.m_isFreezingAtNight = false;

        environment.m_ambColorDay = style.Ambient;
        environment.m_ambColorNight = Scale(style.Ambient, 0.58f);

        environment.m_fogColorDay = style.Fog;
        environment.m_fogColorMorning = style.Fog;
        environment.m_fogColorEvening = style.Fog;
        environment.m_fogColorNight = Scale(style.Fog, 0.72f);

        environment.m_fogColorSunDay = style.Sun;
        environment.m_fogColorSunMorning = style.Sun;
        environment.m_fogColorSunEvening = style.Sun;
        environment.m_fogColorSunNight = Scale(style.Sun, 0.55f);

        environment.m_sunColorDay = style.Sun;
        environment.m_sunColorMorning = style.Sun;
        environment.m_sunColorEvening = style.Sun;
        environment.m_sunColorNight = Scale(style.Sun, 0.45f);

        environment.m_lightIntensityDay = style.LightDay;
        environment.m_lightIntensityNight = style.LightNight;
        environment.m_windMin = style.WindMin;
        environment.m_windMax = style.WindMax;

        // The shared atmosphere runtime owns final fog density. Keep EnvMan's own interpolation in
        // a sane cavern range so there is no bright/storm flash before LateUpdate applies the field.
        environment.m_fogDensityDay = style.BaseFogDensity;
        environment.m_fogDensityMorning = style.BaseFogDensity;
        environment.m_fogDensityEvening = style.BaseFogDensity;
        environment.m_fogDensityNight = style.BaseFogDensity * 1.12f;

        // Never inherit Surface rain clouds or thunder/rain audio. Subterranean precipitation uses
        // filtered donor particle systems and later Magenheim-authored VFX.
        environment.m_rainCloudAlpha = 0f;
        environment.m_ambientLoop = null;
        environment.m_ambientVol = 0f;
        environment.m_psystemsOutsideOnly = false;
        environment.m_psystems = FilterParticles(
            environment.m_psystems,
            style.ParticleTokens,
            atmosphereEvent == UnderworldAtmosphereEvent.Whiteout);
    }

    private static GameObject[] FilterParticles(
        GameObject[]? donors,
        string[] tokens,
        bool allowSnowStorm)
    {
        if (donors is null || donors.Length == 0 || tokens.Length == 0)
            return Array.Empty<GameObject>();

        var accepted = new List<GameObject>();
        foreach (var donor in donors)
        {
            if (!donor) continue;
            var name = donor.name ?? string.Empty;
            var surfaceStorm = LooksLikeThunderStorm(name);
            if (surfaceStorm &&
                !(allowSnowStorm &&
                  name.IndexOf("snow", StringComparison.OrdinalIgnoreCase) >= 0))
                continue;

            foreach (var token in tokens)
            {
                if (name.IndexOf(token, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                accepted.Add(donor);
                break;
            }
        }
        return accepted.ToArray();
    }

    private static bool LooksLikeThunderStorm(string value) =>
        value.IndexOf("thunder", StringComparison.OrdinalIgnoreCase) >= 0 ||
        value.IndexOf("rain", StringComparison.OrdinalIgnoreCase) >= 0 ||
        value.IndexOf("storm", StringComparison.OrdinalIgnoreCase) >= 0;

    private static string[] DonorCandidates(
        UnderworldTerrainBiome biome,
        UnderworldAtmosphereEvent atmosphereEvent)
    {
        switch (biome)
        {
            case UnderworldTerrainBiome.FungalForest:
                return new[] { "Mistlands_clear", "Mistlands", "Clear" };
            case UnderworldTerrainBiome.BlackwaterDeep:
                return new[] { "Mistlands_clear", "Swamp", "Clear" };
            case UnderworldTerrainBiome.SulfurousWastes:
                return atmosphereEvent == UnderworldAtmosphereEvent.Ashfall ||
                       atmosphereEvent == UnderworldAtmosphereEvent.ThermalSurge
                    ? new[] { "Ashlands", "Ashlands_clear", "Mistlands_clear", "Clear" }
                    : new[] { "Ashlands_clear", "Ashlands", "Clear" };
            case UnderworldTerrainBiome.FrozenCaverns:
                return atmosphereEvent == UnderworldAtmosphereEvent.Whiteout
                    ? new[] { "SnowStorm", "Snow", "DeepNorth", "Mountain", "Clear" }
                    : new[] { "Snow", "DeepNorth", "Mountain", "Clear" };
            case UnderworldTerrainBiome.FractureZones:
                return new[] { "Mistlands_clear", "Mistlands", "Mountain", "Clear" };
            case UnderworldTerrainBiome.GreatDecay:
                return new[] { "Swamp", "Mistlands_clear", "Mistlands", "Clear" };
            default:
                return new[] { "Clear" };
        }
    }

    private static UnderworldAtmosphereEvent[] EventsFor(UnderworldTerrainBiome biome)
    {
        switch (biome)
        {
            case UnderworldTerrainBiome.FungalForest:
                return new[]
                {
                    UnderworldAtmosphereEvent.Sporefall,
                    UnderworldAtmosphereEvent.CrystalResonance
                };
            case UnderworldTerrainBiome.BlackwaterDeep:
                return new[]
                {
                    UnderworldAtmosphereEvent.DeepFog,
                    UnderworldAtmosphereEvent.CrystalResonance
                };
            case UnderworldTerrainBiome.SulfurousWastes:
                return new[]
                {
                    UnderworldAtmosphereEvent.Ashfall,
                    UnderworldAtmosphereEvent.ThermalSurge,
                    UnderworldAtmosphereEvent.CrystalResonance
                };
            case UnderworldTerrainBiome.FrozenCaverns:
                return new[]
                {
                    UnderworldAtmosphereEvent.DeepFog,
                    UnderworldAtmosphereEvent.Whiteout,
                    UnderworldAtmosphereEvent.CrystalResonance
                };
            case UnderworldTerrainBiome.FractureZones:
                return new[]
                {
                    UnderworldAtmosphereEvent.StoneRain,
                    UnderworldAtmosphereEvent.CrystalResonance
                };
            case UnderworldTerrainBiome.GreatDecay:
                return new[]
                {
                    UnderworldAtmosphereEvent.BlackBloom,
                    UnderworldAtmosphereEvent.CrystalResonance
                };
            default:
                return Array.Empty<UnderworldAtmosphereEvent>();
        }
    }

    private static WeatherStyle StyleFor(
        UnderworldTerrainBiome biome,
        UnderworldAtmosphereEvent atmosphereEvent)
    {
        var style = biome switch
        {
            UnderworldTerrainBiome.FungalForest => new WeatherStyle(
                new Color(0.18f, 0.34f, 0.36f),
                new Color(0.12f, 0.28f, 0.31f),
                new Color(0.36f, 0.58f, 0.57f),
                0.36f, 0.16f, 0.05f, 0.18f, 0.010f,
                false, false, false,
                new[] { "mist", "firefly", "pollen" }),

            UnderworldTerrainBiome.BlackwaterDeep => new WeatherStyle(
                new Color(0.10f, 0.18f, 0.22f),
                new Color(0.08f, 0.15f, 0.19f),
                new Color(0.24f, 0.36f, 0.42f),
                0.28f, 0.12f, 0.05f, 0.22f, 0.014f,
                false, false, false,
                new[] { "mist", "fog" }),

            UnderworldTerrainBiome.SulfurousWastes => new WeatherStyle(
                new Color(0.30f, 0.22f, 0.11f),
                new Color(0.42f, 0.36f, 0.14f),
                new Color(0.65f, 0.36f, 0.16f),
                0.38f, 0.14f, 0.10f, 0.30f, 0.010f,
                false, false, false,
                new[] { "ash", "smoke", "ember" }),

            UnderworldTerrainBiome.FrozenCaverns => new WeatherStyle(
                new Color(0.29f, 0.38f, 0.45f),
                new Color(0.50f, 0.64f, 0.74f),
                new Color(0.68f, 0.82f, 0.92f),
                0.40f, 0.18f, 0.07f, 0.28f, 0.010f,
                false, true, false,
                new[] { "snow", "mist" }),

            UnderworldTerrainBiome.FractureZones => new WeatherStyle(
                new Color(0.19f, 0.17f, 0.25f),
                new Color(0.31f, 0.27f, 0.38f),
                new Color(0.48f, 0.41f, 0.60f),
                0.31f, 0.13f, 0.08f, 0.30f, 0.008f,
                false, false, false,
                new[] { "mist", "dust", "ash" }),

            UnderworldTerrainBiome.GreatDecay => new WeatherStyle(
                new Color(0.16f, 0.09f, 0.08f),
                new Color(0.27f, 0.13f, 0.11f),
                new Color(0.37f, 0.19f, 0.13f),
                0.23f, 0.09f, 0.02f, 0.14f, 0.018f,
                false, false, false,
                new[] { "mist", "fog", "smoke" }),

            _ => throw new ArgumentOutOfRangeException(nameof(biome), biome, null)
        };

        switch (atmosphereEvent)
        {
            case UnderworldAtmosphereEvent.Sporefall:
                return style with
                {
                    WindMin = 0.03f,
                    WindMax = 0.12f,
                    ParticleTokens = new[] { "mist", "firefly", "pollen" }
                };
            case UnderworldAtmosphereEvent.DeepFog:
                return style with
                {
                    WindMin = 0.02f,
                    WindMax = 0.12f,
                    Wet = biome == UnderworldTerrainBiome.BlackwaterDeep,
                    ParticleTokens = new[] { "mist", "fog" }
                };
            case UnderworldAtmosphereEvent.Ashfall:
                return style with
                {
                    WindMin = 0.30f,
                    WindMax = 0.68f,
                    ParticleTokens = new[] { "ash", "smoke", "ember" }
                };
            case UnderworldAtmosphereEvent.ThermalSurge:
                return style with
                {
                    WindMin = 0.18f,
                    WindMax = 0.48f,
                    Fog = new Color(0.52f, 0.37f, 0.12f),
                    ParticleTokens = new[] { "ash", "smoke", "ember" }
                };
            case UnderworldAtmosphereEvent.Whiteout:
                return style with
                {
                    WindMin = 0.58f,
                    WindMax = 0.95f,
                    Freezing = true,
                    Fog = new Color(0.68f, 0.78f, 0.84f),
                    ParticleTokens = new[] { "snow", "blizzard" }
                };
            case UnderworldAtmosphereEvent.StoneRain:
                return style with
                {
                    WindMin = 0.42f,
                    WindMax = 0.78f,
                    ParticleTokens = new[] { "dust", "ash", "mist" }
                };
            case UnderworldAtmosphereEvent.CrystalResonance:
                return style with
                {
                    WindMin = Math.Max(0.02f, style.WindMin * 0.5f),
                    WindMax = Math.Max(0.08f, style.WindMax * 0.65f),
                    ParticleTokens = Array.Empty<string>()
                };
            case UnderworldAtmosphereEvent.BlackBloom:
                return style with
                {
                    WindMin = 0.04f,
                    WindMax = 0.18f,
                    Fog = new Color(0.32f, 0.11f, 0.10f),
                    ParticleTokens = new[] { "mist", "fog", "smoke" }
                };
            default:
                return style;
        }
    }

    private void Deactivate()
    {
        if (_forcedEnvironment.Length > 0 && EnvMan.instance is not null)
            EnvMan.instance.SetForceEnvironment(string.Empty);
        _forcedEnvironment = string.Empty;
        _lastBiome = null;
        _hasWeather = false;
        _atmosphere?.ApplySynchronizedEvent(UnderworldAtmosphereEvent.None, 0d);
    }

    private void OnDisable() => Deactivate();

    private void OnDestroy()
    {
        Deactivate();
        var manager = _registeredManager;
        if (manager is null) return;

        for (var index = manager.m_environments.Count - 1; index >= 0; index--)
        {
            var environment = manager.m_environments[index];
            if (_registeredNames.Contains(environment.m_name))
                manager.m_environments.RemoveAt(index);
        }
        _registeredNames.Clear();
        _registeredManager = null;
    }

    private static string EnvironmentName(
        UnderworldTerrainBiome biome,
        UnderworldAtmosphereEvent atmosphereEvent) =>
        EnvironmentPrefix + biome + "_" +
        (atmosphereEvent == UnderworldAtmosphereEvent.None
            ? "Still"
            : atmosphereEvent.ToString());

    private static string Display(UnderworldAtmosphereEvent atmosphereEvent) =>
        atmosphereEvent == UnderworldAtmosphereEvent.None ? "Still" : atmosphereEvent.ToString();

    private static Color Scale(Color value, float amount) =>
        new(
            Mathf.Clamp01(value.r * amount),
            Mathf.Clamp01(value.g * amount),
            Mathf.Clamp01(value.b * amount),
            value.a);

    private readonly record struct WeatherStyle(
        Color Ambient,
        Color Fog,
        Color Sun,
        float LightDay,
        float LightNight,
        float WindMin,
        float WindMax,
        float BaseFogDensity,
        bool Wet,
        bool Cold,
        bool Freezing,
        string[] ParticleTokens);
}
