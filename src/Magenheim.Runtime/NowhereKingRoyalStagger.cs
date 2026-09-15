using HarmonyLib;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Anti-stunlock stagger authority: sustained heavy physical pressure earns a short legitimate opening.</summary>
internal sealed class NowhereKingRoyalStagger : MonoBehaviour
{
    private const float Threshold=260f,WindowSeconds=7f,OpeningSeconds=2.1f,RecoverySeconds=8f;
    private float _meter,_lastPressure,_openingUntil,_recoveryUntil;
    internal bool IsOpening=>Time.time<_openingUntil;
    internal void Observe(HitData hit)
    {
        if(hit==null||Time.time<_recoveryUntil)return;
        if(Time.time-_lastPressure>WindowSeconds)_meter=0f;
        var physical=hit.m_damage.m_blunt+hit.m_damage.m_slash+hit.m_damage.m_pierce;
        if(physical<28f)return;
        _lastPressure=Time.time;_meter+=physical*Mathf.Max(.25f,hit.m_staggerMultiplier);
        if(_meter<Threshold)return;
        _meter=0f;_openingUntil=Time.time+OpeningSeconds;_recoveryUntil=_openingUntil+RecoverySeconds;
    }
    internal void ResetState(){_meter=0f;_lastPressure=0f;_openingUntil=0f;_recoveryUntil=0f;}
}

[HarmonyPatch(typeof(Character),nameof(Character.Damage))]
internal static class NowhereKingRoyalStaggerPatch
{
    private static void Prefix(Character __instance,HitData hit)
    {
        if(__instance==null||hit==null)return;
        var royal=__instance.GetComponent<NowhereKingRoyalStagger>();
        if(royal!=null)royal.Observe(hit);
    }
}
