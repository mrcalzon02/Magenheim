using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Rolling incoming elemental pressure used by the Null Mantle. Never grants immunity.</summary>
internal sealed class NowhereKingAdaptiveResistance : MonoBehaviour
{
    private const float WindowSeconds=12f,DominanceThreshold=.40f,Resistance=.38f;
    private readonly List<Sample> _samples=new List<Sample>();
    private DamageType _active=DamageType.None; private float _activeUntil;
    private struct Sample{internal float Time;internal DamageType Type;internal float Amount;}
    private enum DamageType{None,Fire,Frost,Lightning,Poison,Spirit}

    internal void Observe(HitData hit)
    {
        if(hit==null)return;var now=Time.time;Add(now,DamageType.Fire,hit.m_damage.m_fire);Add(now,DamageType.Frost,hit.m_damage.m_frost);Add(now,DamageType.Lightning,hit.m_damage.m_lightning);Add(now,DamageType.Poison,hit.m_damage.m_poison);Add(now,DamageType.Spirit,hit.m_damage.m_spirit);Prune(now);Select(now);
    }
    internal void Mitigate(HitData hit)
    {
        if(hit==null||_active==DamageType.None||Time.time>_activeUntil)return;switch(_active){case DamageType.Fire:hit.m_damage.m_fire*=1f-Resistance;break;case DamageType.Frost:hit.m_damage.m_frost*=1f-Resistance;break;case DamageType.Lightning:hit.m_damage.m_lightning*=1f-Resistance;break;case DamageType.Poison:hit.m_damage.m_poison*=1f-Resistance;break;case DamageType.Spirit:hit.m_damage.m_spirit*=1f-Resistance;break;}
    }
    internal void ResetAdaptation(){_samples.Clear();_active=DamageType.None;_activeUntil=0f;}
    private void Add(float time,DamageType type,float amount){if(amount>0f&&!float.IsNaN(amount)&&!float.IsInfinity(amount))_samples.Add(new Sample{Time=time,Type=type,Amount=amount});}
    private void Prune(float now){for(var i=_samples.Count-1;i>=0;i--)if(now-_samples[i].Time>WindowSeconds)_samples.RemoveAt(i);}
    private void Select(float now){var totals=new float[6];var all=0f;foreach(var s in _samples){totals[(int)s.Type]+=s.Amount;all+=s.Amount;}if(all<=0f)return;var best=DamageType.None;var value=0f;for(var i=1;i<totals.Length;i++)if(totals[i]>value){value=totals[i];best=(DamageType)i;}if(value/all>=DominanceThreshold){_active=best;_activeUntil=now+8f;}}
}

[HarmonyPatch(typeof(Character),nameof(Character.Damage))]
internal static class NowhereKingAdaptiveResistancePatch
{
    private static void Prefix(Character __instance,HitData hit)
    {
        if(__instance==null||hit==null||__instance.gameObject.name.IndexOf(NowhereKingRegistrar.PrefabName,StringComparison.Ordinal)<0)return;
        var runtime=__instance.GetComponent<NowhereKingAdaptiveResistance>();if(runtime==null)return;runtime.Observe(hit);runtime.Mitigate(hit);
    }
}
