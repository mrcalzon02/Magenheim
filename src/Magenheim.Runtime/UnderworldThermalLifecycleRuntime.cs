using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Owner-only runtime lifecycle for synchronized Underworld thermal pressure.
/// Geothermal trigger volumes own accumulation; this component owns passive recovery
/// between admitted samples and exposes Core's canonical severity to later consequence UI.
/// </summary>
internal sealed class UnderworldThermalLifecycleRuntime : MonoBehaviour
{
    internal const float ExposureGraceSeconds = 0.25f;

    private Player? _player;
    private ZNetView? _view;
    private float _lastObservedHeat;
    private float _graceRemaining;

    private void Awake()
    {
        _player = GetComponent<Player>();
        _view = GetComponent<ZNetView>();
        _lastObservedHeat = UnderworldGeothermalHazardVolume.ReadHeat(_player);
    }

    private void FixedUpdate()
    {
        if (_player == null || _view == null || !_view.IsValid() || !_view.IsOwner()) return;
        var zdo = _view.GetZDO();
        if (zdo == null) return;

        var current = UnderworldGeothermalHazardVolume.ReadHeat(_player);
        if (current > _lastObservedHeat + 0.0001f)
        {
            // A canonical hazard volume accumulated heat since our previous tick. Hold recovery
            // briefly so trigger/callback ordering cannot cool the same exposure sample away.
            _graceRemaining = ExposureGraceSeconds;
            _lastObservedHeat = current;
            return;
        }

        if (_graceRemaining > 0f)
        {
            _graceRemaining = Mathf.Max(0f, _graceRemaining - Time.fixedDeltaTime);
            _lastObservedHeat = current;
            return;
        }

        var next = UnderworldThermalState.Cool(current, Time.fixedDeltaTime);
        if (!Mathf.Approximately(next, current))
            zdo.Set(UnderworldGeothermalHazardVolume.ThermalStateKey, next);
        _lastObservedHeat = next;
    }

    internal UnderworldThermalSeverity Severity =>
        UnderworldThermalState.Severity(UnderworldGeothermalHazardVolume.ReadHeat(_player), UnderworldGeothermalHazardVolume.MaximumHeat);
}
