using System;
using System.Collections.Generic;
using System.Linq;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

internal sealed class CinderworksInteriorBinding
{
    internal CinderworksInteriorBinding(bool hasInterior,float radius,string environment)
    {HasInterior=hasInterior;InteriorRadius=radius;Environment=environment;}
    internal bool HasInterior{get;} internal float InteriorRadius{get;} internal string Environment{get;}
    internal void Validate()
    {
        if(!HasInterior||float.IsNaN(InteriorRadius)||float.IsInfinity(InteriorRadius)||InteriorRadius<=0f||string.IsNullOrWhiteSpace(Environment))
            throw new InvalidOperationException("Invalid Cinderworks interior binding.");
    }
}

internal sealed class CinderworksInteriorBinder
{
    internal const string InteriorRootName="Magenheim_Cinderworks_InteriorRoot";
    internal const float ConservativeInteriorRadius=1900f;
    internal static string InteriorEnvironment=>UnderworldWeatherRuntime.EnvironmentName(UnderworldTerrainBiome.SulfurousWastes,UnderworldAtmosphereEvent.None);

    internal CinderworksInteriorBinding AttachInterior(GameObject locationContainer)
    {
        if(locationContainer is null)throw new ArgumentNullException(nameof(locationContainer));
        var all=locationContainer.GetComponentsInChildren<Transform>(true);
        var portal=all.SingleOrDefault(x=>x.name==CinderworksEntranceVisuals.EntrancePortalAnchorName)
            ??throw new InvalidOperationException("Cinderworks entrance anchor missing.");
        var interior=all.SingleOrDefault(x=>x.name==CinderworksEntranceVisuals.InteriorAnchorName)
            ??throw new InvalidOperationException("Cinderworks interior anchor missing.");
        CinderworksTravel.AttachEntrancePortal(portal);
        var root=new GameObject(InteriorRootName);root.transform.SetParent(interior,false);
        root.AddComponent<ZNetView>();root.AddComponent<CinderworksEncounterAuthority>();root.AddComponent<CinderworksInteriorRuntime>();
        return new CinderworksInteriorBinding(true,ConservativeInteriorRadius,InteriorEnvironment);
    }
}

internal sealed class CinderworksInteriorRuntime:MonoBehaviour
{
    private readonly List<Pending> _pending=new();
    private CinderworksEncounterAuthority? _authority;
    private int _seed; private bool _built,_populated;

    private void Start()
    {
        if(_built)return;
        UnderworldSulfurCinderworksCatalog.Validate();
        _authority=GetComponent<CinderworksEncounterAuthority>()??throw new InvalidOperationException("Cinderworks encounter authority missing.");
        _seed=StableLocationSeed(transform.position);
        var topology=UnderworldBiomeDungeonPlanner.Build(UnderworldDungeonCatalog.SulfurousWastes,_seed,
            UnderworldSulfurCinderworksCatalog.RoomFamilyIds(),UnderworldSulfurCinderworksCatalog.EntranceRoomId);
        var spatial=UnderworldBiomeDungeonSpatialPlanner.Build(topology,UnderworldSulfurCinderworksCatalog.SpatialDefinitions());
        var positions=spatial.Rooms.ToDictionary(x=>x.InstanceId,StringComparer.Ordinal);
        var defs=UnderworldSulfurCinderworksCatalog.Rooms.ToDictionary(x=>x.Room.Id,StringComparer.Ordinal);
        GameObject? entrance=null;
        foreach(var placement in topology.Rooms)
        {
            if(!positions.TryGetValue(placement.InstanceId,out var pos)||!defs.TryGetValue(placement.RoomFamilyId,out var def))
                throw new InvalidOperationException("Cinderworks topology/spatial definition mismatch.");
            var source=CinderworksRoomRegistrar.ResolveRoom(placement.RoomFamilyId);
            var instance=Instantiate(source.gameObject,transform,false);
            instance.name=CinderworksRoomVisuals.RoomPrefabName(def)+"_"+placement.InstanceId;
            instance.transform.localPosition=new Vector3((float)pos.X,(float)pos.Y,(float)pos.Z);
            instance.transform.localRotation=Quaternion.Euler(0f,(float)pos.YawDegrees,0f);instance.SetActive(true);
            _pending.Add(new Pending(instance,placement,def));
            if(placement.InstanceId==topology.Rooms[0].InstanceId)entrance=instance;
        }
        CinderworksPassageAssembler.Assemble(transform,topology,spatial);
        if(entrance is null)throw new InvalidOperationException("Cinderworks entrance room was not instantiated.");
        CinderworksTravel.Bind(transform,entrance);_built=true;TryPopulate();
    }

    private void Update(){if(_built&&!_populated)TryPopulate();}
    private void TryPopulate()
    {
        if(_authority is null||!_authority.HasAuthority)return;
        foreach(var p in _pending)CinderworksEncounterSpawner.Populate(p.Room,p.Placement,p.Definition,_authority,_seed);
        _pending.Clear();_populated=true;
    }
    private sealed record Pending(GameObject Room,UnderworldDungeonRoomPlacement Placement,UnderworldCinderworksRoomDefinition Definition);
    internal static int StableLocationSeed(Vector3 p)
    {
        unchecked{uint h=2166136261u;foreach(var v in new[]{(int)Math.Round(p.x*100f),(int)Math.Round(p.y*100f),(int)Math.Round(p.z*100f)})h=(h^(uint)v)*16777619u;return(int)h;}
    }
}
