using System;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Projects Cinderworks room heat into the existing geothermal accumulator. The trigger covers the
/// room's industrial centre lane only; authored side ledges remain outside it as the non-boon route.
/// </summary>
internal sealed class CinderworksThermalHazardRuntime : MonoBehaviour
{
    private UnderworldCinderworksThermalRouteState? _state;
    private UnderworldGeothermalHazardVolume? _hazard;
    private Collider? _trigger;
    private int _phaseSeed;

    internal void Bind(
        UnderworldCinderworksRoomDefinition definition,
        Collider trigger,
        UnderworldGeothermalHazardVolume hazard,
        int phaseSeed)
    {
        if (definition is null) throw new ArgumentNullException(nameof(definition));
        _state = UnderworldCinderworksThermalRoutePolicy.Resolve(definition);
        if (!_state.HasHazard)
            throw new InvalidOperationException("Safe Cinderworks rooms cannot bind a thermal hazard.");
        _trigger = trigger ?? throw new ArgumentNullException(nameof(trigger));
        _hazard = hazard ?? throw new ArgumentNullException(nameof(hazard));
        _phaseSeed = phaseSeed;
        _hazard.Bind(
            _state.HazardId!,
            _state.Intensity,
            placementRadius: 0.1f);
        _trigger.enabled = !_state.Cycles;
    }

    private void FixedUpdate()
    {
        var state = _state;
        var trigger = _trigger;
        if (state is null || trigger is null || !state.Cycles) return;

        var network = ZNet.instance;
        var seconds = network is null
            ? Time.timeAsDouble
            : network.GetTimeSeconds();
        trigger.enabled =
            UnderworldCinderworksThermalRoutePolicy.IsActive(
                state,
                seconds,
                _phaseSeed);
    }
}

internal static class CinderworksThermalRuntime
{
    internal static void Attach(
        GameObject room,
        UnderworldCinderworksRoomDefinition definition)
    {
        if (room is null) throw new ArgumentNullException(nameof(room));
        if (definition is null) throw new ArgumentNullException(nameof(definition));
        definition.Validate();

        var state = UnderworldCinderworksThermalRoutePolicy.Resolve(definition);
        if (!state.HasHazard) return;

        var root = new GameObject("Magenheim_Cinderworks_ThermalLane")
        {
            layer = room.layer,
        };
        root.transform.SetParent(room.transform, false);

        // Hot machinery occupies the centre ~44% of the room. The authored side ledges live beyond
        // +/- 28% width, so this trigger cannot silently consume the deliberate bypass route.
        var collider = root.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.center = new Vector3(0f, 1.25f, 0f);
        collider.size = new Vector3(
            Mathf.Max(4f, (float)definition.Room.WidthMeters * .44f),
            3.4f,
            Mathf.Max(6f, (float)definition.Room.DepthMeters * .72f));

        var hazard = root.AddComponent<UnderworldGeothermalHazardVolume>();
        var runtime = root.AddComponent<CinderworksThermalHazardRuntime>();
        runtime.Bind(
            definition,
            collider,
            hazard,
            StablePhaseSeed(definition.Room.Id));
    }

    private static int StablePhaseSeed(string identity)
    {
        unchecked
        {
            uint hash = 2166136261u;
            foreach (var character in identity)
            {
                hash ^= character;
                hash *= 16777619u;
            }
            return (int)hash;
        }
    }
}
