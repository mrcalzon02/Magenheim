using System;
using System.Collections.Generic;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Runtime bridge between a concrete Magenheim geothermal volume and the player-owned
/// Underworld thermal-pressure state. The volume supplies only canonical source identity
/// and intensity; Core remains authoritative for source admission and bounded exposure,
/// while FurnaceBloodRuntime remains authoritative for boon mitigation.
/// </summary>
internal sealed class UnderworldGeothermalHazardVolume : MonoBehaviour
{
    internal const string ThermalStateKey = "magenheim.underworld.thermal.v1";
    internal const float MaximumHeat = 100f;

    private static readonly HashSet<UnderworldGeothermalHazardVolume> Active = new();

    [SerializeField] private string _hazardId = UnderworldGeothermalHazard.VentField;
    [SerializeField] private float _intensity = 1f;
    [SerializeField] private float _placementRadius = 4.5f;

    internal void Bind(string hazardId, float intensity, float placementRadius)
    {
        if (string.IsNullOrWhiteSpace(hazardId)) throw new ArgumentException("Geothermal hazard id is required.", nameof(hazardId));
        if (float.IsNaN(intensity) || float.IsInfinity(intensity) || intensity <= 0f) throw new ArgumentOutOfRangeException(nameof(intensity));
        if (float.IsNaN(placementRadius) || float.IsInfinity(placementRadius) || placementRadius <= 0f) throw new ArgumentOutOfRangeException(nameof(placementRadius));
        _hazardId = hazardId; _intensity = intensity; _placementRadius = placementRadius;
    }

    private void OnEnable() => Active.Add(this);
    private void OnDisable() => Active.Remove(this);
    private void OnDestroy() => Active.Remove(this);

    internal static bool IsNearLiveVent(Vector3 position, float extraRadius)
    {
        if (float.IsNaN(extraRadius) || float.IsInfinity(extraRadius) || extraRadius < 0f) return false;
        foreach (var volume in Active)
        {
            if (!volume || !volume.isActiveAndEnabled) continue;
            var radius = volume._placementRadius + extraRadius;
            if ((volume.transform.position - position).sqrMagnitude <= radius * radius) return true;
        }
        return false;
    }

    private void OnTriggerStay(Collider other)
    {
        var player = other.GetComponentInParent<Player>();
        if (player == null) return;
        EnsureThermalLifecycle(player);
        ApplySample(player, _hazardId, _intensity, Time.fixedDeltaTime);
    }

    /// <summary>
    /// Thermal state only exists after a player has actually contacted a canonical geothermal
    /// gameplay surface. This avoids attaching an idle component to every Player in every world.
    /// The component is owner-aware and will not mutate remote peers.
    /// </summary>
    private static void EnsureThermalLifecycle(Player player)
    {
        if (player.GetComponent<UnderworldThermalLifecycleRuntime>() == null)
            player.gameObject.AddComponent<UnderworldThermalLifecycleRuntime>();
    }

    /// <summary>
    /// Applies one admitted geothermal sample to the owning player's synchronized ZDO.
    /// Unknown sources and malformed samples are neutral through Core authority. Only the
    /// network owner writes state, preventing competing peers from accumulating the same tick.
    /// </summary>
    internal static float ApplySample(Player player, string hazardId, float intensity, float deltaSeconds)
    {
        if (player == null) return 0f;
        var view = player.GetComponent<ZNetView>();
        if (view == null || !view.IsValid() || !view.IsOwner()) return ReadHeat(view);

        var zdo = view.GetZDO();
        if (zdo == null) return 0f;

        var current = Mathf.Clamp(zdo.GetFloat(ThermalStateKey, 0f), 0f, MaximumHeat);
        var weatherMultiplier = UnderworldWeatherGameplayRuntime.GeothermalIntensityMultiplier(player);
        var exposure = UnderworldGeothermalHazard.Exposure(
            hazardId,
            intensity * weatherMultiplier,
            deltaSeconds);
        if (exposure <= 0f) return current;

        var next = FurnaceBloodRuntime.ApplyThermalBuildup(player, current, exposure, MaximumHeat);
        next = Mathf.Clamp(next, 0f, MaximumHeat);
        if (!Mathf.Approximately(next, current)) zdo.Set(ThermalStateKey, next);
        return next;
    }

    // Declared nullable because this overload already treats a missing player as zero heat; the
    // non-null signature only forced callers holding a Player? to invent a redundant guard.
    internal static float ReadHeat(Player? player)
    {
        if (player == null) return 0f;
        return ReadHeat(player.GetComponent<ZNetView>());
    }

    private static float ReadHeat(ZNetView? view)
    {
        if (view == null || !view.IsValid()) return 0f;
        var zdo = view.GetZDO();
        return zdo == null ? 0f : Mathf.Clamp(zdo.GetFloat(ThermalStateKey, 0f), 0f, MaximumHeat);
    }
}
