using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Cheap, disposable runtime dressing for the first live Underworld test. This intentionally uses
/// primitive geometry and shared materials so ecology, scale, traversal and province identity can
/// be tested before authored models are complete. It never runs in Surface sessions.
/// </summary>
internal sealed class UnderworldPlaceholderEcologyRuntime : MonoBehaviour
{
    private UnderworldRuntimeServices? _services;
    private ManualLogSource? _log;
    private readonly List<GameObject> _spawned = new();
    private string _admitted = string.Empty;
    private float _nextAt;

    internal void Configure(UnderworldRuntimeServices services, ManualLogSource log)
    { _services=services; _log=log; }

    private void Update()
    {
        if(Time.unscaledTime<_nextAt)return;_nextAt=Time.unscaledTime+4f;
        if(_services is null||ZNet.instance is null||ZNet.World is null)return;
        if(!UnderworldRuntimeIdentityResolver.TryResolveWorldSession(_services.WorldPairStore,ZNet.instance,ZNet.World,
            out var identity,out var layer,out _)||identity is null||layer!=UnderworldLayer.Underworld){Clear();return;}
        if(string.Equals(_admitted,identity.DerivedWorldId,StringComparison.Ordinal))return;
        Clear();_admitted=identity.DerivedWorldId;Build(identity);
    }

    private void Build(UnderworldWorldIdentity identity)
    {
        // Dense enough to make all six provinces visually obvious from traversal, sparse enough to
        // avoid turning the placeholder pass into a performance test.
        var radius=(float)_services!.SpatialDomain.RadiusMeters;
        for(var ring=1;ring<=4;ring++)
        {
            var distance=radius*(0.10f+ring*0.13f);
            for(var i=0;i<24;i++)
            {
                var angle=i*Mathf.PI*2f/24f+ring*0.37f;
                var x=Mathf.Cos(angle)*distance;var z=Mathf.Sin(angle)*distance;
                var generator=WorldGenerator.instance;if(generator is null)continue;
                var biome=generator.GetBiome(x,z);var y=generator.GetBiomeHeight(biome,x,z);
                var sample=UnderworldTerrainRuntime.SampleTerrain(x,z,y);if(!sample.Admitted)continue;
                SpawnMarker(new Vector3(x,(float)sample.Height,z),sample.Biome,i+ring*31);
            }
        }
        _log?.LogInfo($"Spawned {_spawned.Count} low-cost Underworld ecology markers for live traversal testing.");
    }

    private void SpawnMarker(Vector3 position,UnderworldTerrainBiome biome,int variant)
    {
        var primitive=biome switch
        {
            UnderworldTerrainBiome.FungalForest=>PrimitiveType.Sphere,
            UnderworldTerrainBiome.BlackwaterDeep=>PrimitiveType.Cylinder,
            UnderworldTerrainBiome.SulfurousWastes=>PrimitiveType.Capsule,
            UnderworldTerrainBiome.FrozenCaverns=>PrimitiveType.Cube,
            UnderworldTerrainBiome.FractureZones=>PrimitiveType.Cube,
            _=>PrimitiveType.Sphere
        };
        var node=GameObject.CreatePrimitive(primitive);node.name="Magenheim_UnderworldPlaceholder_"+biome;
        node.transform.position=position+Vector3.up*(2f+(variant%4));
        node.transform.rotation=Quaternion.Euler(variant%17,variant*37%360,variant%11);
        node.transform.localScale=Scale(biome,variant);
        var renderer=node.GetComponent<Renderer>();if(renderer)renderer.sharedMaterial=MaterialFor(biome);
        var collider=node.GetComponent<Collider>();if(collider&&biome==UnderworldTerrainBiome.FungalForest)collider.enabled=false;
        _spawned.Add(node);
    }

    private static Vector3 Scale(UnderworldTerrainBiome biome,int v)=>biome switch
    {
        UnderworldTerrainBiome.FungalForest=>new Vector3(4f+(v%3)*2f,2f+(v%5),4f+(v%3)*2f),
        UnderworldTerrainBiome.BlackwaterDeep=>new Vector3(2f,8f+(v%4)*3f,2f),
        UnderworldTerrainBiome.SulfurousWastes=>new Vector3(3f,6f+(v%3)*2f,3f),
        UnderworldTerrainBiome.FrozenCaverns=>new Vector3(2f,10f+(v%5)*2f,2f),
        UnderworldTerrainBiome.FractureZones=>new Vector3(2f,12f+(v%4)*3f,2f),
        _=>new Vector3(3f+(v%4),3f+(v%4),3f+(v%4))
    };

    private static Material MaterialFor(UnderworldTerrainBiome biome)
    {
        var shader=Shader.Find("Standard")??throw new InvalidOperationException("Unity Standard shader unavailable.");
        var m=new Material(shader){name="Magenheim_UnderworldPlaceholder_"+biome};
        m.color=biome switch
        {
            UnderworldTerrainBiome.FungalForest=>new Color(0.20f,0.48f,0.26f),
            UnderworldTerrainBiome.BlackwaterDeep=>new Color(0.06f,0.12f,0.18f),
            UnderworldTerrainBiome.SulfurousWastes=>new Color(0.55f,0.30f,0.08f),
            UnderworldTerrainBiome.FrozenCaverns=>new Color(0.48f,0.70f,0.82f),
            UnderworldTerrainBiome.FractureZones=>new Color(0.38f,0.16f,0.46f),
            _=>new Color(0.24f,0.18f,0.20f)
        };m.SetFloat("_Glossiness",0.18f);return m;
    }

    private void Clear(){foreach(var item in _spawned)if(item)Destroy(item);_spawned.Clear();_admitted=string.Empty;}
    private void OnDestroy()=>Clear();
}
