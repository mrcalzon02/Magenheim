using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Registers additive Magenheim-owned Deep Fracture hostile creature chassis.</summary>
internal sealed class DeepFractureCreatureRegistrar : IDisposable
{
    internal const string AnnoyanceWispPrefix="Magenheim_AnnoyanceWisp_", GeodeCrawlerPrefix="Magenheim_GeodeCrawler_", ShardlingPrefix="Magenheim_Shardling_", CrystalParasitePrefix="Magenheim_CrystalParasite_", CrystalRevenantPrefix="Magenheim_CrystalRevenant_";
    private readonly ManualLogSource _log; private bool _subscribed,_registered;
    internal DeepFractureCreatureRegistrar(ManualLogSource log)=>_log=log??throw new ArgumentNullException(nameof(log));
    internal void Register(){if(_subscribed||_registered)return;PrefabManager.OnVanillaPrefabsAvailable+=RegisterContent;_subscribed=true;}
    private void RegisterContent(){if(_registered)return;try{var w=RequireCreatureSource("Wisp");var c=RequireCreatureSource("Tick");var s=RequireCreatureSource("Greydwarf");var p=RequireCreatureSource("Leech");var r=RequireCreatureSource("Draugr");var registered=new List<string>();foreach(ElementalAlignment a in Enum.GetValues(typeof(ElementalAlignment))){registered.Add(Register(w,a,AnnoyanceWispPrefix,"Annoyance Wisp",34f,5.5f,2.5f,DeepFractureCreatureVisuals.ApplyAnnoyanceWisp));registered.Add(Register(c,a,GeodeCrawlerPrefix,"Geode Crawler",72f,4f,1.8f,DeepFractureCreatureVisuals.ApplyGeodeCrawler));registered.Add(Register(s,a,ShardlingPrefix,"Shardling",48f,6.2f,2.8f,DeepFractureCreatureVisuals.ApplyShardling));registered.Add(Register(p,a,CrystalParasitePrefix,"Crystal Parasite",58f,3.8f,1.6f,DeepFractureCreatureVisuals.ApplyCrystalParasite));registered.Add(Register(r,a,CrystalRevenantPrefix,"Crystal Revenant",118f,4.6f,2.1f,DeepFractureCreatureVisuals.ApplyCrystalRevenant));}_registered=true;_log.LogInfo($"Registered {registered.Count} additive Deep Fracture creature variants: {string.Join(", ",registered)}.");}catch(Exception e){_log.LogError($"Deep Fracture creature registration failed: {e}");throw;}finally{Dispose();}}
    private static GameObject RequireCreatureSource(string name){var source=PrefabManager.Instance.GetPrefab(name)??throw new InvalidOperationException($"Vanilla creature source '{name}' is unavailable; refusing to create an unverified chassis.");if(!source.GetComponent<Character>()||!source.GetComponent<BaseAI>()||!source.GetComponent<ZNetView>())throw new InvalidOperationException($"Vanilla creature source '{name}' lacks required Character/BaseAI/ZNetView behavior.");return source;}
    private static GameObject CloneVerified(string name,GameObject source){if(PrefabManager.Instance.GetPrefab(name))throw new InvalidOperationException($"Cannot replace occupied Deep Fracture creature identity '{name}'.");var p=PrefabManager.Instance.CreateClonedPrefab(name,source)??throw new InvalidOperationException($"Unable to clone Deep Fracture creature host '{name}'.");p.name=name;if(!p.GetComponent<Character>()||!p.GetComponent<BaseAI>()||!p.GetComponent<ZNetView>())throw new InvalidOperationException($"Cloned Deep Fracture creature '{name}' lost required behavior.");return p;}
    private static string Register(GameObject source,ElementalAlignment alignment,string prefix,string display,float health,float run,float walk,Action<GameObject,ElementalAlignment> visuals){var name=prefix+alignment;var p=CloneVerified(name,source);var ch=p.GetComponent<Character>();ch.m_name=$"{alignment} {display}";ch.m_health=health;ch.m_runSpeed=Mathf.Max(ch.m_runSpeed,run);ch.m_walkSpeed=Mathf.Max(ch.m_walkSpeed,walk);visuals(p,alignment);PrefabManager.Instance.AddPrefab(new CustomPrefab(p,true));return name;}
    public void Dispose(){if(!_subscribed)return;PrefabManager.OnVanillaPrefabsAvailable-=RegisterContent;_subscribed=false;}
}
