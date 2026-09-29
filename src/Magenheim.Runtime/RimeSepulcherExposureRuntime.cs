using System;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Refreshes the shared short-lived local weather override while a player occupies a Rime room
/// exposure volume. There is no dungeon-specific damage loop: normal weather/atmosphere gameplay
/// consumes the override and applies Rimebound/Rimeward resistance.
/// </summary>
internal sealed class RimeSepulcherExposureVolume : MonoBehaviour
{
    private UnderworldRimeSepulcherExposureState _state;
    private bool _bound;

    internal void Bind(UnderworldRimeSepulcherExposureState state)
    {
        state.Validate();
        _state = state;
        _bound = true;
    }

    private void OnTriggerStay(Collider other)
    {
        if (!_bound) return;
        var player = other.GetComponentInParent<Player>();
        if (player is null || player.IsDead()) return;
        UnderworldLocalWeatherOverride.Refresh(
            player,
            UnderworldTerrainBiome.FrozenCaverns,
            _state.Event,
            _state.EventIntensity01,
            _state.HazardFloor01,
            holdSeconds: .75f);
    }
}

internal static class RimeSepulcherExposureRuntime
{
    internal static void Attach(
        GameObject room,
        UnderworldRimeSepulcherRoomDefinition definition)
    {
        if (room is null) throw new ArgumentNullException(nameof(room));
        if (definition is null) throw new ArgumentNullException(nameof(definition));
        var state = UnderworldRimeSepulcherExposurePolicy.Resolve(definition);
        Attach(
            room,
            state,
            (float)definition.Room.WidthMeters,
            (float)definition.Room.DepthMeters,
            "Magenheim_RimeSepulcher_Exposure");
    }

    internal static void AttachPassage(
        GameObject passage,
        UnderworldRimeSepulcherExposureState state)
    {
        if (passage is null) throw new ArgumentNullException(nameof(passage));
        state.Validate();
        Attach(passage,state,14f,16f,"Magenheim_RimeSepulcher_PassageExposure");
    }

    private static void Attach(
        GameObject owner,
        UnderworldRimeSepulcherExposureState state,
        float width,
        float depth,
        string name)
    {
        var root=new GameObject(name){layer=owner.layer};
        root.transform.SetParent(owner.transform,false);
        var collider=root.AddComponent<BoxCollider>();
        collider.isTrigger=true;
        collider.center=new Vector3(0f,1.4f,0f);
        var widthScale=state.RoomWide?.86f:.46f;
        var depthScale=state.RoomWide?.86f:.72f;
        collider.size=new Vector3(
            Mathf.Max(4f,width*widthScale),
            3.8f,
            Mathf.Max(5f,depth*depthScale));
        root.AddComponent<RimeSepulcherExposureVolume>().Bind(state);
    }
}
