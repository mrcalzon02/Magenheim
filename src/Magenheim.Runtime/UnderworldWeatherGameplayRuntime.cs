using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Server-authoritative material consequences for the deterministic Underworld weather schedule.
/// Clients may render the same pure schedule, but damage/aggression is admitted here from server
/// world time, instance identity, terrain biome and player-owned progression state.
/// </summary>
internal sealed class UnderworldWeatherGameplayRuntime : MonoBehaviour
{
    private const float PollSeconds = 0.50f;
    private const float BlackBloomAggressionRadius = 72f;
    private const float AggressionHoldSeconds = 2.0f;

    private static UnderworldRuntimeServices? _sharedServices;

    private readonly Dictionary<long, long> _lastPulseByPlayer = new();
    private UnderworldRuntimeServices? _services;
    private ManualLogSource? _log;
    private float _nextPollAt;

    internal void Configure(UnderworldRuntimeServices services, ManualLogSource log)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _sharedServices = services;
    }

    private void Update()
    {
        if (Time.unscaledTime < _nextPollAt) return;
        _nextPollAt = Time.unscaledTime + PollSeconds;

        var services = _services;
        var network = ZNet.instance;
        if (services is null || network is null || !network.IsServer()) return;

        var identity = services.InstanceLifecycle.Identity;
        if (services.InstanceLifecycle.Phase != UnderworldInstancePhase.Active || identity is null)
            return;

        var worldTime = network.GetTimeSeconds();
        foreach (var player in Player.GetAllPlayers())
        {
            if (player is null || player.IsDead()) continue;
            if (!services.WorldInstances.TryGetPlayerInstance(player.GetPlayerID(), out var instance) ||
                !instance.IsUnderworld)
                continue;

            var position = player.transform.position;
            var terrain = UnderworldTerrainRuntime.SampleInstanceTerrain(
                position.x, position.y, position.z);
            if (!terrain.Admitted) continue;

            var weather = UnderworldWeatherCycle.Evaluate(
                terrain.Biome,
                identity.DerivedSeed32,
                worldTime);
            var mitigation = UnderworldWeatherMitigationRuntime.Resolve(player, terrain.Biome);
            var atmosphere = UnderworldAtmosphere.Evaluate(new UnderworldAtmosphereInput(
                terrain.Biome,
                terrain.Height,
                terrain.WaterDepth,
                terrain.Hazard01,
                weather.Event,
                weather.Intensity01,
                mitigation.Resistance01,
                mitigation.Suppression01));
            var gameplay = UnderworldWeatherGameplay.Evaluate(
                terrain.Biome,
                weather.Event,
                weather.Intensity01,
                atmosphere.Exposure01);

            ApplyDamagePulse(player, identity.DerivedSeed32, weather, gameplay, worldTime);

            if (gameplay.CreatureActivityMultiplier > 1.001d)
                BoostNearbyUnderworldCreatures(
                    player,
                    (float)gameplay.CreatureActivityMultiplier);
        }
    }

    private void ApplyDamagePulse(
        Player player,
        int seed,
        UnderworldWeatherState weather,
        UnderworldWeatherGameplayState gameplay,
        double worldTime)
    {
        if (!gameplay.HasDamage || gameplay.PulseIntervalSeconds <= 0d) return;

        var pulse = (long)Math.Floor(worldTime / gameplay.PulseIntervalSeconds);
        var playerId = player.GetPlayerID();
        if (_lastPulseByPlayer.TryGetValue(playerId, out var last) && last == pulse)
            return;
        _lastPulseByPlayer[playerId] = pulse;

        if (gameplay.PulseChance01 < 1d)
        {
            var roll = DeterministicPulseRoll(seed, playerId, weather.Period, pulse);
            if (roll > gameplay.PulseChance01) return;
        }

        var hit = new HitData
        {
            m_point = player.GetCenterPoint(),
            m_dir = Vector3.down,
            m_staggerMultiplier = (float)Math.Max(0d, gameplay.StaggerMultiplier),
        };
        var amount = (float)Math.Max(0d, gameplay.DamagePerPulse);
        switch (gameplay.DamageKind)
        {
            case UnderworldWeatherDamageKind.Fire:
                hit.m_damage.m_fire = amount;
                break;
            case UnderworldWeatherDamageKind.Frost:
                hit.m_damage.m_frost = amount;
                break;
            case UnderworldWeatherDamageKind.Blunt:
                hit.m_damage.m_blunt = amount;
                break;
            case UnderworldWeatherDamageKind.Poison:
                hit.m_damage.m_poison = amount;
                break;
            default:
                return;
        }

        player.Damage(hit);
    }

    private static double DeterministicPulseRoll(
        int seed,
        long playerId,
        long period,
        long pulse)
    {
        unchecked
        {
            var mixed = (uint)seed ^
                        (uint)playerId ^
                        (uint)(playerId >> 32) ^
                        ((uint)period * 0x9e3779b9u) ^
                        ((uint)pulse * 0x85ebca6bu);
            return UnderworldTerrainNoise.Mix(mixed) / ((double)uint.MaxValue + 1d);
        }
    }

    private static void BoostNearbyUnderworldCreatures(Player player, float multiplier)
    {
        multiplier = Mathf.Clamp(multiplier, 1f, 1.45f);
        var maxSq = BlackBloomAggressionRadius * BlackBloomAggressionRadius;
        foreach (var character in Character.GetAllCharacters())
        {
            if (character is null || character is Player || character.IsDead() ||
                character.gameObject.scene.handle != player.gameObject.scene.handle)
                continue;
            if ((character.transform.position - player.transform.position).sqrMagnitude > maxSq)
                continue;

            var prefab = Utils.GetPrefabName(character.gameObject);
            if (!prefab.StartsWith("Magenheim_Underworld_Prototype_", StringComparison.Ordinal) &&
                !prefab.StartsWith("Magenheim_Underworld_Surtling_", StringComparison.Ordinal))
                continue;

            var ai = character.GetComponent<MonsterAI>();
            if (ai is null) continue;
            var boost = ai.GetComponent<UnderworldWeatherAggressionBoost>() ??
                        ai.gameObject.AddComponent<UnderworldWeatherAggressionBoost>();
            boost.Refresh(ai, multiplier, AggressionHoldSeconds);
        }
    }

    /// <summary>
    /// Existing geothermal gameplay surfaces call this so Thermal Surge intensifies the actual
    /// vent/lava hazard rather than existing only as an unrelated weather damage pulse.
    /// </summary>
    internal static float GeothermalIntensityMultiplier(Player player)
    {
        var services = _sharedServices;
        var network = ZNet.instance;
        var identity = services?.InstanceLifecycle.Identity;
        if (player is null || services is null || network is null || identity is null ||
            services.InstanceLifecycle.Phase != UnderworldInstancePhase.Active ||
            !services.WorldInstances.TryGetPlayerInstance(player.GetPlayerID(), out var instance) ||
            !instance.IsUnderworld)
            return 1f;

        var position = player.transform.position;
        var terrain = UnderworldTerrainRuntime.SampleInstanceTerrain(
            position.x, position.y, position.z);
        if (!terrain.Admitted || terrain.Biome != UnderworldTerrainBiome.SulfurousWastes)
            return 1f;

        var weather = UnderworldWeatherCycle.Evaluate(
            terrain.Biome,
            identity.DerivedSeed32,
            network.GetTimeSeconds());
        if (weather.Event != UnderworldAtmosphereEvent.ThermalSurge)
            return 1f;

        return 1f + Mathf.Clamp01((float)weather.Intensity01) * 1.25f;
    }

    private void OnDestroy()
    {
        _lastPulseByPlayer.Clear();
        if (ReferenceEquals(_sharedServices, _services))
            _sharedServices = null;
        _services = null;
    }
}

/// <summary>Temporary Black Bloom perception boost that restores the creature's authored AI.</summary>
internal sealed class UnderworldWeatherAggressionBoost : MonoBehaviour
{
    private MonsterAI? _ai;
    private bool _captured;
    private float _baseAlert;
    private float _baseView;
    private float _baseHear;
    private float _until;

    internal void Refresh(MonsterAI ai, float multiplier, float holdSeconds)
    {
        if (ai is null) return;
        _ai = ai;
        if (!_captured)
        {
            _baseAlert = ai.m_alertRange;
            _baseView = ai.m_viewRange;
            _baseHear = ai.m_hearRange;
            _captured = true;
        }

        ai.m_alertRange = Mathf.Clamp(_baseAlert * multiplier, _baseAlert, 60f);
        ai.m_viewRange = Mathf.Clamp(_baseView * multiplier, _baseView, 70f);
        ai.m_hearRange = Mathf.Clamp(_baseHear * multiplier, _baseHear, 55f);
        _until = Mathf.Max(_until, Time.time + Mathf.Max(.25f, holdSeconds));
    }

    private void Update()
    {
        if (!_captured || _ai is null || Time.time < _until) return;
        Restore();
        Destroy(this);
    }

    private void OnDisable() => Restore();
    private void OnDestroy() => Restore();

    private void Restore()
    {
        if (!_captured || _ai is null) return;
        _ai.m_alertRange = _baseAlert;
        _ai.m_viewRange = _baseView;
        _ai.m_hearRange = _baseHear;
        _captured = false;
    }
}
