using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Reversible presentation and weaker adaptive protection inherited from the Nowhere King.</summary>
internal sealed class NullMantleRuntime : MonoBehaviour
{
    private const float WindowSeconds=10f,DominanceThreshold=.45f,Resistance=.22f,ActiveSeconds=6f;
    private readonly List<Sample> _samples=new();
    private readonly Dictionary<Renderer,Material[]> _originalMaterials=new();
    private Player _player;
    private GameObject _presentation;
    private DamageType _active=DamageType.None;
    private float _activeUntil;
    private struct Sample{internal float Time;internal DamageType Type;internal float Amount;}
    private enum DamageType{None,Fire,Frost,Lightning,Poison,Spirit}

    private void Awake()=>_player=GetComponent<Player>();
    internal void SetEquipped(bool equipped){if(equipped){if(_presentation==null)ApplyPresentation();}else if(_presentation!=null)RemovePresentation();}
    internal void ObserveAndMitigate(HitData hit)
    {
        if(hit==null||_presentation==null)return;
        var now=Time.time;
        Mitigate(hit,now);
        Add(now,DamageType.Fire,hit.m_damage.m_fire);Add(now,DamageType.Frost,hit.m_damage.m_frost);Add(now,DamageType.Lightning,hit.m_damage.m_lightning);Add(now,DamageType.Poison,hit.m_damage.m_poison);Add(now,DamageType.Spirit,hit.m_damage.m_spirit);
        for(var i=_samples.Count-1;i>=0;i--)if(now-_samples[i].Time>WindowSeconds)_samples.RemoveAt(i);
        var totals=new float[6];var all=0f;foreach(var s in _samples){totals[(int)s.Type]+=s.Amount;all+=s.Amount;}if(all<=0f)return;
        var best=DamageType.None;var value=0f;for(var i=1;i<totals.Length;i++)if(totals[i]>value){value=totals[i];best=(DamageType)i;}
        if(value/all>=DominanceThreshold){_active=best;_activeUntil=now+ActiveSeconds;}
    }
    private void Mitigate(HitData hit,float now)
    {
        if(_active==DamageType.None||now>_activeUntil)return;var multiplier=1f-Resistance;
        switch(_active){case DamageType.Fire:hit.m_damage.m_fire*=multiplier;break;case DamageType.Frost:hit.m_damage.m_frost*=multiplier;break;case DamageType.Lightning:hit.m_damage.m_lightning*=multiplier;break;case DamageType.Poison:hit.m_damage.m_poison*=multiplier;break;case DamageType.Spirit:hit.m_damage.m_spirit*=multiplier;break;}
    }
    private void Add(float time,DamageType type,float amount){if(amount>0f&&!float.IsNaN(amount)&&!float.IsInfinity(amount))_samples.Add(new Sample{Time=time,Type=type,Amount=amount});}
    private void ApplyPresentation()
    {
        _presentation=new GameObject("Magenheim_NullMantle_Form");_presentation.transform.SetParent(transform,false);
        foreach(var renderer in GetComponentsInChildren<Renderer>(true))
        {
            if(renderer.transform.IsChildOf(_presentation.transform))continue;var originals=renderer.sharedMaterials;if(originals==null||originals.Length==0)continue;_originalMaterials[renderer]=originals;
            var replacements=new Material[originals.Length];for(var i=0;i<originals.Length;i++){var source=originals[i];if(source==null)continue;var material=new Material(source){name=source.name+"_NullMantle"};if(material.HasProperty("_Color"))material.SetColor("_Color",new Color(.018f,.012f,.025f,1f));replacements[i]=material;}renderer.sharedMaterials=replacements;
        }
        var crown=GameObject.CreatePrimitive(PrimitiveType.Cylinder);crown.name="BrokenCrown";crown.transform.SetParent(_presentation.transform,false);crown.transform.localPosition=new Vector3(0f,2.05f,0f);crown.transform.localScale=new Vector3(.42f,.10f,.42f);Destroy(crown.GetComponent<Collider>());
        var eyeLight=new GameObject("RedEyes");eyeLight.transform.SetParent(_presentation.transform,false);eyeLight.transform.localPosition=new Vector3(0f,1.72f,.16f);var light=eyeLight.AddComponent<Light>();light.color=new Color(.9f,.015f,.01f);light.range=2.1f;light.intensity=1.5f;
        var fog=new GameObject("NowhereFog");fog.transform.SetParent(_presentation.transform,false);fog.transform.localPosition=Vector3.up;var particles=fog.AddComponent<ParticleSystem>();var main=particles.main;main.startLifetime=2.4f;main.startSpeed=.18f;main.startSize=.75f;main.startColor=new Color(.01f,.005f,.018f,.34f);main.maxParticles=32;var emission=particles.emission;emission.rateOverTime=8f;var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=1.15f;
    }
    private void RemovePresentation()
    {
        foreach(var pair in _originalMaterials)if(pair.Key!=null)pair.Key.sharedMaterials=pair.Value;_originalMaterials.Clear();Destroy(_presentation);_presentation=null;_samples.Clear();_active=DamageType.None;_activeUntil=0f;
    }
    private void OnDestroy(){if(_presentation!=null)RemovePresentation();}
}

[HarmonyPatch(typeof(Player),nameof(Player.Update))]
internal static class NullMantleEquipmentPatch
{
    private static void Postfix(Player __instance)
    {
        if(__instance==null)return;var helmet=__instance.m_helmetItem;var equipped=helmet!=null&&helmet.m_dropPrefab!=null&&string.Equals(helmet.m_dropPrefab.name,NowhereKingRewardRegistrar.NullMantlePrefabName,StringComparison.Ordinal);
        var runtime=__instance.GetComponent<NullMantleRuntime>();if(equipped){runtime??=__instance.gameObject.AddComponent<NullMantleRuntime>();runtime.SetEquipped(true);}else if(runtime!=null)runtime.SetEquipped(false);
    }
}

[HarmonyPatch(typeof(Character),nameof(Character.Damage))]
internal static class NullMantleDamagePatch
{
    private static void Prefix(Character __instance,HitData hit){if(__instance is Player player){var runtime=player.GetComponent<NullMantleRuntime>();runtime?.ObserveAndMitigate(hit);}}
}
