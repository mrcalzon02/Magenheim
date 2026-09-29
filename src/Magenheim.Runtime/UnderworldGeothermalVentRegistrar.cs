using System;
using System.IO;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Registers three persistent owned geothermal vent variants in Sulfurous Wastes.</summary>
internal sealed class UnderworldGeothermalVentRegistrar : IDisposable
{
    private static readonly (string Prefab,string Model)[] Vents={
        ("Magenheim_Underworld_GeothermalVent_Crown","underworld-geothermal-vent-crown"),
        ("Magenheim_Underworld_GeothermalVent_Split","underworld-geothermal-vent-split"),
        ("Magenheim_Underworld_GeothermalVent_Rootbound","underworld-geothermal-vent-rootbound"),
    };
    private readonly ManualLogSource _log;
    private bool _subscribed;
    internal UnderworldGeothermalVentRegistrar(ManualLogSource log)=>_log=log??throw new ArgumentNullException(nameof(log));

    internal void Register()
    {
        if(_subscribed)return;
        PrefabManager.OnVanillaPrefabsAvailable+=RegisterVents;_subscribed=true;
    }

    private void RegisterVents()
    {
        var admitted=0;
        try
        {
            var donor=PrefabManager.Instance.GetPrefab("Ashlands_rock1")
                ?? throw new InvalidOperationException("Geothermal vent donor Ashlands_rock1 is unavailable.");
            foreach(var entry in Vents)
            {
                if(!HasModel(entry.Model))
                {
                    _log.LogWarning("Geothermal vent model not installed yet: "+entry.Model+". Production forge must generate it before release admission.");
                    continue;
                }
                if(PrefabManager.Instance.GetPrefab(entry.Prefab))
                    throw new InvalidOperationException("Occupied geothermal vent identity: "+entry.Prefab);
                var clone=PrefabManager.Instance.CreateClonedPrefab(entry.Prefab,donor);
                foreach(var collider in clone.GetComponentsInChildren<Collider>(true))
                    if(!collider.isTrigger)collider.enabled=false;
                ModelAssets.Load(clone,entry.Model);
                var hazard=new GameObject("Magenheim_Geothermal_Hazard");
                hazard.transform.SetParent(clone.transform,false);
                hazard.transform.localPosition=new Vector3(0f,.75f,0f);
                var trigger=hazard.AddComponent<SphereCollider>();trigger.isTrigger=true;trigger.radius=3.1f;
                hazard.AddComponent<UnderworldGeothermalHazardVolume>().Bind(UnderworldGeothermalHazard.VentField,1f,4.5f);

                var config=new VegetationConfig{
                    Biome=UnderworldTerrainRuntime.ToNativeBiome(UnderworldTerrainBiome.SulfurousWastes),
                    BiomeArea=JotunnWorldgenAdapter.MapArea(Magenheim.Core.Worldgen.SpawnArea.All),
                    BlockCheck=true,ForcePlacement=false,Min=.18f,Max=.34f,
                    MinAltitude=-1000f,MaxAltitude=1000f,MinOceanDepth=0f,MaxOceanDepth=0f,
                    MinTerrainDelta=0f,MaxTerrainDelta=1.8f,TerrainDeltaRadius=3f,
                    MinTilt=0f,MaxTilt=32f,InForest=false,ForestThresholdMin=0f,ForestThresholdMax=1f,
                    ScaleMin=.92f,ScaleMax=1.08f,GroupSizeMin=1,GroupSizeMax=1,GroupRadius=0f,GroundOffset=0f,
                };
                if(!ZoneManager.Instance.AddCustomVegetation(new CustomVegetation(clone,fixReference:true,config)))
                    throw new InvalidOperationException("Jotunn refused geothermal vent worldgen row "+entry.Prefab);
                admitted++;
            }
            _log.LogInfo($"Underworld geothermal vents: {admitted}/{Vents.Length} owned persistent variants registered in Sulfurous Wastes.");
        }
        finally{Dispose();}
    }

    private static bool HasModel(string model)
    {
        var directory=Path.GetDirectoryName(typeof(UnderworldGeothermalVentRegistrar).Assembly.Location);
        return !string.IsNullOrEmpty(directory)&&File.Exists(Path.Combine(directory,"assets","models","runtime",model+".model.json"));
    }

    public void Dispose()
    {
        if(!_subscribed)return;
        PrefabManager.OnVanillaPrefabsAvailable-=RegisterVents;_subscribed=false;
    }
}
