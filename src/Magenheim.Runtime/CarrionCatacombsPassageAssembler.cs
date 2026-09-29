using System;
using System.Linq;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

internal static class CarrionCatacombsPassageAssembler
{
    internal static void Assemble(
        Transform interiorRoot,
        UnderworldBiomeDungeonPlan topology,
        UnderworldBiomeDungeonSpatialPlan spatial)
    {
        if(interiorRoot is null)throw new ArgumentNullException(nameof(interiorRoot));
        if(topology is null)throw new ArgumentNullException(nameof(topology));
        if(spatial is null)throw new ArgumentNullException(nameof(spatial));
        if(!string.Equals(topology.DungeonId,spatial.DungeonId,StringComparison.Ordinal)||
           topology.Seed!=spatial.Seed)
            throw new InvalidOperationException(
                "Carrion Catacombs topology and spatial plan do not describe the same dungeon.");

        var rooms=spatial.Rooms.ToDictionary(x=>x.InstanceId,StringComparer.Ordinal);
        var definitions=UnderworldGreatDecayCarrionCatacombsCatalog.Rooms.ToDictionary(
            x=>x.Room.Id,StringComparer.Ordinal);
        var source=CarrionCatacombsRoomRegistrar.ResolvePassage();

        foreach(var connection in spatial.Connections)
        {
            if(!rooms.TryGetValue(connection.FromInstanceId,out var from)||
               !rooms.TryGetValue(connection.ToInstanceId,out var to))
                throw new InvalidOperationException(
                    "Carrion Catacombs passage references an unknown spatial room.");
            if(!definitions.TryGetValue(from.RoomFamilyId,out var fromDefinition)||
               !definitions.TryGetValue(to.RoomFamilyId,out var toDefinition))
                throw new InvalidOperationException(
                    "Carrion Catacombs passage cannot resolve endpoint contamination authority.");
            if(connection.Waypoints.Count<2)
                throw new InvalidOperationException(
                    "Carrion Catacombs routed passage has fewer than two waypoints.");

            var exposure=UnderworldCarrionCatacombsContaminationPolicy.ResolvePassage(
                fromDefinition,toDefinition);
            var sequence=0;
            for(var leg=0;leg<connection.Waypoints.Count-1;leg++)
            {
                var a=connection.Waypoints[leg];
                var b=connection.Waypoints[leg+1];
                var start=new Vector3((float)a.X,(float)a.Y+.55f,(float)a.Z);
                var end=new Vector3((float)b.X,(float)b.Y+.55f,(float)b.Z);
                var delta=end-start;
                var horizontal=new Vector3(delta.x,0f,delta.z);
                if(horizontal.sqrMagnitude<.01f)continue;
                var direction=horizontal.normalized;

                if(leg==0)
                    start+=direction*ExitDistance(fromDefinition.Room,direction);
                if(leg==connection.Waypoints.Count-2)
                    end-=direction*ExitDistance(toDefinition.Room,-direction);

                BuildLeg(
                    interiorRoot,source,connection,exposure,start,end,ref sequence);
            }
        }
    }

    private static void BuildLeg(
        Transform interiorRoot,
        Room source,
        UnderworldDungeonSpatialConnection connection,
        UnderworldCarrionCatacombsExposureState exposure,
        Vector3 start,
        Vector3 end,
        ref int sequence)
    {
        var delta=end-start;
        var distance=delta.magnitude;
        if(distance<1f)return;

        var spacing=CarrionCatacombsRoomVisuals.PassageLengthMeters*.86f;
        var count=Math.Max(1,Mathf.CeilToInt(distance/spacing));
        var rotation=Quaternion.LookRotation(delta.normalized,Vector3.up);

        for(var index=0;index<count;index++)
        {
            var t=(index+.5f)/count;
            var instance=UnityEngine.Object.Instantiate(
                source.gameObject,interiorRoot,false);
            var identity=
                $"{Sanitize(connection.FromInstanceId)}_{Sanitize(connection.ToInstanceId)}_{sequence:000}";
            instance.name=CarrionCatacombsRoomVisuals.PassagePrefabName+"_"+identity;
            sequence++;
            instance.transform.localPosition=Vector3.Lerp(start,end,t);
            instance.transform.localRotation=rotation;
            CarrionCatacombsContaminationRuntime.AttachPassage(instance,exposure);
            instance.SetActive(true);
        }
    }

    private static float ExitDistance(
        UnderworldDungeonRoomDefinition definition,
        Vector3 direction)
    {
        var x=Mathf.Abs(direction.x)*(float)definition.WidthMeters*.42f;
        var z=Mathf.Abs(direction.z)*(float)definition.DepthMeters*.42f;
        return Mathf.Max(3f,x+z);
    }

    private static string Sanitize(string value)
    {
        var last=value.LastIndexOf('.');
        return (last>=0?value.Substring(last+1):value).Replace('-','_');
    }
}
