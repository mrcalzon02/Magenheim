using System;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

internal sealed class UnderworldWorldCenterRegistrar : IDisposable
{
    internal const string LocationName = "Magenheim_UnderworldWorldCenter";
    internal const string StandingStonesName = "Magenheim_UnderworldStandingStones";
    internal const string DescentMonolithName = "Magenheim_DescentMonolith";
    internal static readonly Vector3 ReturnGateOffset = new(18f, 0f, 0f);
    private static readonly string[] DeepstoneNames = {"StoneOfBloom","StoneOfDeepTide","StoneOfCinder","StoneOfRime","StoneOfFracture","StoneOfDecay"};
    private readonly ManualLogSource _log; private bool _subscribed; private bool _registered;
    internal UnderworldWorldCenterRegistrar(ManualLogSource log)=>_log=log??throw new ArgumentNullException(nameof(log));
    internal void Register(){if(_registered||_subscribed)return;ZoneManager.OnVanillaLocationsAvailable+=OnVanillaLocationsAvailable;_subscribed=true;}
    public void Dispose(){if(!_subscribed)return;ZoneManager.OnVanillaLocationsAvailable-=OnVanillaLocationsAvailable;_subscribed=false;}
    private void OnVanillaLocationsAvailable()
    {
        if(_registered)return;try
        {
            if(ZoneManager.Instance.GetZoneLocation(LocationName)is not null||CustomLocation.IsCustomLocation(LocationName)){_registered=true;_log.LogWarning($"Underworld world-center identity '{LocationName}' is occupied; existing content was left untouched.");return;}
            var gatePrefab=PrefabManager.Instance.GetPrefab(UnderworldDeepGateRegistrar.PrefabName)??throw new InvalidOperationException("Deep Gate must be registered before the Underworld world center is composed.");
            var container=ZoneManager.Instance.CreateLocationContainer(LocationName)??throw new InvalidOperationException($"Jotunn could not create Underworld world-center container '{LocationName}'.");BuildStandingStones(container.transform);
            var gate=UnityEngine.Object.Instantiate(gatePrefab,container.transform,false);gate.name=UnderworldDeepGateRegistrar.PrefabName;gate.transform.localPosition=ReturnGateOffset;gate.transform.localRotation=Quaternion.Euler(0f,-90f,0f);
            var endpoint=gate.GetComponent<UnderworldGateEndpoint>()??gate.AddComponent<UnderworldGateEndpoint>();endpoint.Role=UnderworldGateRole.ReturnToSurface;
            if(gate.GetComponent<UnderworldDeepGateProgressionRuntime>()==null)gate.AddComponent<UnderworldDeepGateProgressionRuntime>();
            var config=new LocationConfig{Biome=Heightmap.Biome.Meadows,BiomeArea=Heightmap.BiomeArea.Everything,Quantity=1,Priotized=true,ExteriorRadius=72f,MinAltitude=4f,MinTerrainDelta=0f,MaxTerrainDelta=5f,MinDistanceFromSimilar=0f,Group="Magenheim_UnderworldWorldCenter",ClearArea=true,RandomRotation=false,HasInterior=false};
            var custom=new CustomLocation(container,fixReference:false,config);if(!ZoneManager.Instance.AddCustomLocation(custom))throw new InvalidOperationException($"Jotunn refused Underworld world-center registration for '{LocationName}'.");_registered=true;_log.LogInfo($"Registered Underworld center '{LocationName}' with progression-gated return Deep Gate.");
        }catch(Exception exception){_log.LogError($"Underworld world-center registration failed: {exception}");throw;}finally{Dispose();}
    }
    private static void BuildStandingStones(Transform parent)
    {
        var root=new GameObject(StandingStonesName);root.transform.SetParent(parent,false);var material=CreateStoneMaterial();var box=RuntimeMeshPrimitives.Box("magenheim.underworld.standing-stone");var dais=RuntimeMeshPrimitives.Cylinder(32,"magenheim.underworld.center-dais");const float radius=12f;
        for(var i=0;i<DeepstoneNames.Length;i++){var angle=i*Mathf.PI*2f/DeepstoneNames.Length;AddMesh(root.transform,DeepstoneNames[i],box,new Vector3(Mathf.Cos(angle)*radius,2.8f,Mathf.Sin(angle)*radius),Quaternion.Euler(i%2==0?-3f:4f,-angle*Mathf.Rad2Deg+90f,i%3-1),new Vector3(2.1f,7.4f+(i%3)*0.7f,1.25f),material,parent.gameObject.layer);}
        AddMesh(root.transform,DescentMonolithName,box,new Vector3(0f,3.9f,0f),Quaternion.identity,new Vector3(2.8f,8.2f,2.8f),material,parent.gameObject.layer);AddMesh(root.transform,"CenterDais",dais,new Vector3(0f,0.45f,0f),Quaternion.identity,new Vector3(17f,0.9f,17f),material,parent.gameObject.layer);
    }
    private static Material CreateStoneMaterial(){var shader=Shader.Find("Standard")??throw new InvalidOperationException("Unity Standard shader unavailable for Underworld standing stones.");var material=new Material(shader){name="Magenheim_UnderworldStandingStone_Material",color=new Color(0.075f,0.082f,0.095f,1f)};material.SetFloat("_Metallic",0.12f);material.SetFloat("_Glossiness",0.24f);return material;}
    private static void AddMesh(Transform parent,string name,Mesh mesh,Vector3 position,Quaternion rotation,Vector3 scale,Material material,int layer){var node=new GameObject(name){layer=layer};node.transform.SetParent(parent,false);node.transform.localPosition=position;node.transform.localRotation=rotation;node.transform.localScale=scale;node.AddComponent<MeshFilter>().sharedMesh=mesh;node.AddComponent<MeshRenderer>().sharedMaterial=material;node.AddComponent<MeshCollider>().sharedMesh=mesh;}
}
