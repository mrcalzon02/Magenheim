using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Cheap, disposable runtime dressing for the first live Underworld test. Markers are generated
/// around the local player so they are actually observable during traversal instead of being
/// scattered thousands of metres away. Nothing is networked or persisted.
/// </summary>
internal sealed class UnderworldPlaceholderEcologyRuntime : MonoBehaviour
{
    private UnderworldRuntimeServices? _services;
    private ManualLogSource? _log;
    private readonly List<GameObject> _spawned=new();
    private readonly Dictionary<UnderworldTerrainBiome,Material> _materials=new();
    private string _admitted=string.Empty;
    private Vector3 _lastBuildPosition;
    private float _nextAt;
    private const float RebuildDistance=180f;

    internal void Configure(UnderworldRuntimeServices services,ManualLogSource log)
    { _services=services??throw new ArgumentNullException(nameof(services));_log=log??throw new ArgumentNullException(nameof(log)); }

    private void Update()
    {
        if(Time.unscaledTime<_nextAt)return;_nextAt=Time.unscaledTime+3f;
        if(_services is null||ZNet.instance is null||ZNet.World is null)return;
        if(!UnderworldRuntimeIdentityResolver.TryResolveWorldSession(_services.WorldPairStore,ZNet.instance,ZNet.World,
            out var identity,out var layer,out _)||identity is null||layer!=UnderworldLayer.Underworld){ClearMarkers();_admitted=string.Empty;return;}
        var player=Player.m_localPlayer;if(player is null)return;
        var first=!string.Equals(_admitted,identity.DerivedWorldId,StringComparison.Ordinal);
        if(!first&&Vector3.Distance(player.transform.position,_lastBuildPosition)<RebuildDistance)return;
        _admitted=identity.DerivedWorldId;_lastBuildPosition=player.transform.position;BuildLocalPatch(player.transform.position,identity);
    }

    private void BuildLocalPatch(Vector3 center,UnderworldWorldIdentity identity)
    {
        ClearMarkers();var generator=WorldGenerator.instance;if(generator is null)return;
        var seed=identity.DerivedSeed32^(Mathf.RoundToInt(center.x/90f)*73856093)^(Mathf.RoundToInt(center.z/90f)*19349663);
        var random=new System.Random(seed);
        for(var i=0;i<42;i++)
        {
            var angle=(float)(random.NextDouble()*Math.PI*2d);var distance=24f+(float)random.NextDouble()*155f;
            var x=center.x+Mathf.Cos(angle)*distance;var z=center.z+Mathf.Sin(angle)*distance;
            var biome=generator.GetBiome(x,z);var vanilla=generator.GetBiomeHeight(biome,x,z);
            var sample=UnderworldTerrainRuntime.SampleTerrain(x,z,vanilla);if(!sample.Admitted)continue;
            SpawnMarker(new Vector3(x,(float)sample.Height,z),sample.Biome,i+seed);
        }
        _log?.LogDebug($"Refreshed {_spawned.Count} local Underworld placeholder ecology markers near ({center.x:0},{center.z:0}).");
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
        node.transform.position=position+Vector3.up*(1.5f+Math.Abs(variant%4));
        node.transform.rotation=Quaternion.Euler(Math.Abs(variant%17),Math.Abs(variant*37%360),Math.Abs(variant%11));
        node.transform.localScale=Scale(biome,variant);
        var renderer=node.GetComponent<Renderer>();if(renderer)renderer.sharedMaterial=MaterialFor(biome);
        var collider=node.GetComponent<Collider>();if(collider&&biome==UnderworldTerrainBiome.FungalForest)collider.enabled=false;
        _spawned.Add(node);
    }

    private static Vector3 Scale(UnderworldTerrainBiome biome,int v)
    {
        v=Math.Abs(v);
        return biome switch
        {
            UnderworldTerrainBiome.FungalForest=>new Vector3(2.5f+(v%3),3f+(v%5),2.5f+(v%3)),
            UnderworldTerrainBiome.BlackwaterDeep=>new Vector3(1.2f,5f+(v%4)*2f,1.2f),
            UnderworldTerrainBiome.SulfurousWastes=>new Vector3(1.8f,4f+(v%3)*1.5f,1.8f),
            UnderworldTerrainBiome.FrozenCaverns=>new Vector3(1.2f,6f+(v%5)*1.5f,1.2f),
            UnderworldTerrainBiome.FractureZones=>new Vector3(1.4f,7f+(v%4)*2f,1.4f),
            _=>new Vector3(2f+(v%3),2f+(v%3),2f+(v%3))
        };
    }

    private Material MaterialFor(UnderworldTerrainBiome biome)
    {
        if(_materials.TryGetValue(biome,out var cached)&&cached)return cached;
        var shader=Shader.Find("Standard")??throw new InvalidOperationException("Unity Standard shader unavailable.");
        var material=new Material(shader){name="Magenheim_UnderworldPlaceholder_"+biome};
        material.color=biome switch
        {
            UnderworldTerrainBiome.FungalForest=>new Color(0.20f,0.48f,0.26f),
            UnderworldTerrainBiome.BlackwaterDeep=>new Color(0.06f,0.12f,0.18f),
            UnderworldTerrainBiome.SulfurousWastes=>new Color(0.55f,0.30f,0.08f),
            UnderworldTerrainBiome.FrozenCaverns=>new Color(0.48f,0.70f,0.82f),
            UnderworldTerrainBiome.FractureZones=>new Color(0.38f,0.16f,0.46f),
            _=>new Color(0.24f,0.18f,0.20f)
        };material.SetFloat("_Glossiness",0.18f);_materials[biome]=material;return material;
    }

    private void ClearMarkers(){foreach(var item in _spawned)if(item)Destroy(item);_spawned.Clear();}
    private void OnDestroy(){ClearMarkers();foreach(var material in _materials.Values)if(material)Destroy(material);_materials.Clear();}
}
