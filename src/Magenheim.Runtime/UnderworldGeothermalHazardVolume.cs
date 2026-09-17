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

    [SerializeField] private string _hazardId = UnderworldGeothermalHazard.VentField;
    [SerializeField] private float _intensity = 1f;

    private void OnTriggerStay(Collider other)
    {
        var player = other.GetComponentInParent<Player>();
        if (player == null) return;
        ApplySample(player, _hazardId, _intensity, Time.fixedDeltaTime);
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
        var exposure = UnderworldGeothermalHazard.Exposure(hazardId, intensity, deltaSeconds);
        if (exposure <= 0f) return current;

        var next = FurnaceBloodRuntime.ApplyThermalBuildup(player, current, exposure, MaximumHeat);
        next = Mathf.Clamp(next, 0f, MaximumHeat);
        if (!Mathf.Approximately(next, current)) zdo.Set(ThermalStateKey, next);
        return next;
    }

    internal static float ReadHeat(Player player)
    {
        if (player == null) return 0f;
        return ReadHeat(player.GetComponent<ZNetView>());
    }

    private static float ReadHeat(ZNetView view)
    {
        if (view == null || !view.IsValid()) return 0f;
        var zdo = view.GetZDO();
        return zdo == null ? 0f : Mathf.Clamp(zdo.GetFloat(ThermalStateKey, 0f), 0f, MaximumHeat);
    }
}
