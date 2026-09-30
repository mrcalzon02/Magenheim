using System;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Refreshes the shared local atmosphere override while a player occupies a Carrion route volume.
/// Defiant Flesh/armour resistance and Defiant Censer suppression remain owned by the normal
/// Underworld mitigation runtime, so this component never grants immunity itself.
/// </summary>
internal sealed class CarrionCatacombsContaminationVolume : MonoBehaviour
{
    private UnderworldCarrionCatacombsExposureState _state;
    private bool _bound;

    internal void Bind(UnderworldCarrionCatacombsExposureState state)
    {
        state.Validate();
        _state=state;
        _bound=true;
    }

    private void OnTriggerStay(Collider other)
    {
        if(!_bound)return;
        var player=other.GetComponentInParent<Player>();
        if(player is null||player.IsDead())return;
        UnderworldLocalWeatherOverride.Refresh(
            player,
            UnderworldTerrainBiome.GreatDecay,
            _state.Event,
            _state.EventIntensity01,
            _state.HazardFloor01,
            holdSeconds:.75f);
    }
}

internal static class CarrionCatacombsContaminationRuntime
{
    internal static void Attach(
        GameObject room,
        UnderworldCarrionCatacombsRoomDefinition definition)
    {
        if(room is null)throw new ArgumentNullException(nameof(room));
        if(definition is null)throw new ArgumentNullException(nameof(definition));
        var state=UnderworldCarrionCatacombsContaminationPolicy.Resolve(definition);
        Attach(
            room,state,
            (float)definition.Room.WidthMeters,
            (float)definition.Room.DepthMeters,
            "Magenheim_CarrionCatacombs_Contamination");
    }

    internal static void AttachExpandedDonor(
        GameObject room,
        UnderworldCarrionCatacombsExposureState state,
        float width,
        float depth)
    {
        if (room is null) throw new ArgumentNullException(nameof(room));
        state.Validate();
        Attach(
            room,
            state,
            Mathf.Max(4f, width),
            Mathf.Max(5f, depth),
            "Magenheim_DDE_DecayContamination");
    }

    internal static void RebindExpandedDonor(
        GameObject room,
        UnderworldCarrionCatacombsExposureState state,
        float width,
        float depth)
    {
        if(room is null)throw new ArgumentNullException(nameof(room));
        state.Validate();

        var volume=room.GetComponentInChildren<CarrionCatacombsContaminationVolume>(true);
        if(!volume)
        {
            AttachExpandedDonor(room,state,width,depth);
            return;
        }

        volume.Bind(state);
        var collider=volume.GetComponent<BoxCollider>();
        if(!collider)
            throw new InvalidOperationException("Great Decay DDE contamination volume lost its BoxCollider.");

        collider.isTrigger=true;
        collider.center=new Vector3(0f,1.4f,0f);
        var widthScale=state.RoomWide?.86f:.50f;
        var depthScale=state.RoomWide?.86f:.74f;
        collider.size=new Vector3(
            Mathf.Max(4f,Mathf.Max(4f,width)*widthScale),
            3.8f,
            Mathf.Max(5f,Mathf.Max(5f,depth)*depthScale));
    }

    internal static void AttachPassage(
        GameObject passage,
        UnderworldCarrionCatacombsExposureState state)
    {
        if(passage is null)throw new ArgumentNullException(nameof(passage));
        state.Validate();
        Attach(passage,state,14f,16f,"Magenheim_CarrionCatacombs_PassageContamination");
    }

    private static void Attach(
        GameObject owner,
        UnderworldCarrionCatacombsExposureState state,
        float width,
        float depth,
        string name)
    {
        var root=new GameObject(name){layer=owner.layer};
        root.transform.SetParent(owner.transform,false);
        var collider=root.AddComponent<BoxCollider>();
        collider.isTrigger=true;
        collider.center=new Vector3(0f,1.4f,0f);
        var widthScale=state.RoomWide?.86f:.50f;
        var depthScale=state.RoomWide?.86f:.74f;
        collider.size=new Vector3(
            Mathf.Max(4f,width*widthScale),
            3.8f,
            Mathf.Max(5f,depth*depthScale));
        root.AddComponent<CarrionCatacombsContaminationVolume>().Bind(state);
    }
}
