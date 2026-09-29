using System;
using System.Collections.Generic;
using System.Linq;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

internal sealed class CarrionCatacombsInteriorBinding
{
    internal CarrionCatacombsInteriorBinding(bool hasInterior,float radius,string environment)
    {
        HasInterior=hasInterior;
        InteriorRadius=radius;
        Environment=environment;
    }

    internal bool HasInterior{get;}
    internal float InteriorRadius{get;}
    internal string Environment{get;}

    internal void Validate()
    {
        if(!HasInterior||
           float.IsNaN(InteriorRadius)||
           float.IsInfinity(InteriorRadius)||
           InteriorRadius<=0f||
           string.IsNullOrWhiteSpace(Environment))
            throw new InvalidOperationException(
                "Invalid Carrion Catacombs interior binding.");
    }
}

internal sealed class CarrionCatacombsInteriorBinder
{
    internal const string InteriorRootName="Magenheim_CarrionCatacombs_InteriorRoot";
    internal const float ConservativeInteriorRadius=2050f;
    internal static string InteriorEnvironment=>
        UnderworldWeatherRuntime.EnvironmentName(
            UnderworldTerrainBiome.GreatDecay,
            UnderworldAtmosphereEvent.None);

    internal CarrionCatacombsInteriorBinding AttachInterior(GameObject locationContainer)
    {
        if(locationContainer is null)
            throw new ArgumentNullException(nameof(locationContainer));

        var all=locationContainer.GetComponentsInChildren<Transform>(true);
        var portal=all.SingleOrDefault(x=>string.Equals(
                x.name,CarrionCatacombsEntranceVisuals.EntrancePortalAnchorName,
                StringComparison.Ordinal))
            ??throw new InvalidOperationException(
                "Carrion Catacombs entrance anchor missing.");
        var interior=all.SingleOrDefault(x=>string.Equals(
                x.name,CarrionCatacombsEntranceVisuals.InteriorAnchorName,
                StringComparison.Ordinal))
            ??throw new InvalidOperationException(
                "Carrion Catacombs interior anchor missing.");

        CarrionCatacombsTravel.AttachEntrancePortal(portal);
        var root=new GameObject(InteriorRootName);
        root.transform.SetParent(interior,false);
        root.AddComponent<ZNetView>();
        root.AddComponent<CarrionCatacombsEncounterAuthority>();
        root.AddComponent<CarrionCatacombsInteriorRuntime>();

        return new CarrionCatacombsInteriorBinding(
            true,ConservativeInteriorRadius,InteriorEnvironment);
    }
}

internal sealed class CarrionCatacombsInteriorRuntime : MonoBehaviour
{
    private readonly List<PendingPopulation> _pending=new();
    private CarrionCatacombsEncounterAuthority? _authority;
    private int _seed;
    private bool _built;
    private bool _populationApplied;

    private void Start()
    {
        if(_built)return;

        UnderworldGreatDecayCarrionCatacombsCatalog.Validate();
        _authority=GetComponent<CarrionCatacombsEncounterAuthority>()
            ??throw new InvalidOperationException(
                "Carrion Catacombs encounter authority is missing.");

        _seed=StableLocationSeed(transform.position);
        var topology=UnderworldBiomeDungeonPlanner.Build(
            UnderworldDungeonCatalog.GreatDecay,
            _seed,
            UnderworldGreatDecayCarrionCatacombsCatalog.RoomFamilyIds(),
            UnderworldGreatDecayCarrionCatacombsCatalog.EntranceRoomId);
        var spatial=UnderworldBiomeDungeonSpatialPlanner.Build(
            topology,
            UnderworldGreatDecayCarrionCatacombsCatalog.SpatialDefinitions());

        var positions=spatial.Rooms.ToDictionary(x=>x.InstanceId,StringComparer.Ordinal);
        var definitions=UnderworldGreatDecayCarrionCatacombsCatalog.Rooms.ToDictionary(
            x=>x.Room.Id,StringComparer.Ordinal);

        GameObject? entrance=null;
        foreach(var placement in topology.Rooms)
        {
            if(!positions.TryGetValue(placement.InstanceId,out var position)||
               !definitions.TryGetValue(placement.RoomFamilyId,out var definition))
                throw new InvalidOperationException(
                    "Carrion Catacombs topology/spatial definition mismatch.");

            var source=CarrionCatacombsRoomRegistrar.ResolveRoom(placement.RoomFamilyId);
            var instance=Instantiate(source.gameObject,transform,false);
            instance.name=
                CarrionCatacombsRoomVisuals.RoomPrefabName(definition)+"_"+placement.InstanceId;
            instance.transform.localPosition=
                new Vector3((float)position.X,(float)position.Y,(float)position.Z);
            instance.transform.localRotation=
                Quaternion.Euler(0f,(float)position.YawDegrees,0f);
            instance.SetActive(true);
            _pending.Add(new PendingPopulation(instance,placement,definition));

            if(placement.InstanceId==topology.Rooms[0].InstanceId)
                entrance=instance;
        }

        CarrionCatacombsPassageAssembler.Assemble(transform,topology,spatial);
        if(entrance is null)
            throw new InvalidOperationException(
                "Carrion Catacombs entrance room was not instantiated.");
        CarrionCatacombsTravel.Bind(transform,entrance);

        _built=true;
        TryPopulate();
    }

    private void Update()
    {
        if(_built&&!_populationApplied)TryPopulate();
    }

    private void TryPopulate()
    {
        if(_authority is null||!_authority.HasAuthority)return;

        foreach(var pending in _pending)
            CarrionCatacombsEncounterSpawner.Populate(
                pending.Room,pending.Placement,pending.Definition,_authority,_seed);
        _pending.Clear();
        _populationApplied=true;
    }

    private sealed record PendingPopulation(
        GameObject Room,
        UnderworldDungeonRoomPlacement Placement,
        UnderworldCarrionCatacombsRoomDefinition Definition);

    internal static int StableLocationSeed(Vector3 position)
    {
        unchecked
        {
            uint hash=2166136261u;
            foreach(var value in new[]
                    {
                        (int)Math.Round(position.x*100f),
                        (int)Math.Round(position.y*100f),
                        (int)Math.Round(position.z*100f),
                    })
                hash=(hash^(uint)value)*16777619u;
            return (int)hash;
        }
    }
}
