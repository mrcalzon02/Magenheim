using System;
using HarmonyLib;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

internal static class UnderworldToolIdentity
{
    internal static bool Held(Player player,string prefab)
    {
        var item=player?.GetCurrentWeapon();
        return item?.m_dropPrefab is not null&&string.Equals(item.m_dropPrefab.name,prefab,StringComparison.Ordinal);
    }
    internal static bool Equipped(Player player,string prefab)
    {
        if(player is null)return false;
        foreach(var item in player.GetInventory().GetEquippedItems())
            if(item?.m_dropPrefab is not null&&string.Equals(item.m_dropPrefab.name,prefab,StringComparison.Ordinal))return true;
        return false;
    }
}

internal sealed class UnderworldSporelightOutput:MonoBehaviour
{
    private static readonly int Density=Shader.PropertyToID("_MagenheimUnderworldAtmosphereDensity");
    private Light[] _lights=Array.Empty<Light>();
    private float[] _intensity=Array.Empty<float>();
    private float[] _range=Array.Empty<float>();
    private float _next;

    private void Start()
    {
        _lights=GetComponentsInChildren<Light>(true);
        _intensity=new float[_lights.Length];_range=new float[_lights.Length];
        for(var i=0;i<_lights.Length;i++){_intensity[i]=_lights[i].intensity;_range[i]=_lights[i].range;}
    }

    private void Update()
    {
        if(Time.unscaledTime<_next)return;_next=Time.unscaledTime+.2f;
        var p=transform.position;var terrain=UnderworldTerrainRuntime.SampleInstanceTerrain(p.x,p.y,p.z);
        var amount=1f;
        if(terrain.Admitted&&terrain.Biome==UnderworldTerrainBiome.FungalForest)
            amount=Mathf.Lerp(1f,.38f,Mathf.Clamp01(Shader.GetGlobalFloat(Density)));
        for(var i=0;i<_lights.Length;i++)if(_lights[i])
        {
            _lights[i].intensity=_intensity[i]*amount;
            _lights[i].range=_range[i]*Mathf.Lerp(.72f,1f,amount);
        }
    }
}

[HarmonyPatch(typeof(Player),nameof(Player.UseStamina))]
internal static class DivingBellSwimmingStaminaPatch
{
    [HarmonyPrefix]
    private static void Prefix(Player __instance,ref float v)
    {
        if(__instance is null||v<=0f||!__instance.IsSwimming()||
           !UnderworldToolIdentity.Equipped(__instance,UnderworldToolRegistrar.DivingBellPrefab))return;
        var p=__instance.transform.position;var terrain=UnderworldTerrainRuntime.SampleInstanceTerrain(p.x,p.y,p.z);
        if(terrain.Admitted&&terrain.Biome==UnderworldTerrainBiome.BlackwaterDeep)v*=.65f;
    }
}

[HarmonyPatch(typeof(Pickable),nameof(Pickable.Interact))]
internal static class UnderworldToolPickablePatch
{
    private const string Fracture="Magenheim_Underworld_ResourcePickup_FractureCrystal";
    private const string Ice="Magenheim_Underworld_ResourcePickup_ClearIce";

    [HarmonyPrefix]
    private static bool Prefix(Pickable __instance,Humanoid character,bool repeat,bool alt,ref bool __result,out int __state)
    {
        __state=-1;
        if(__instance is null||character is not Player player)return true;
        var prefab=Utils.GetPrefabName(__instance.gameObject);
        if(string.Equals(prefab,Fracture,StringComparison.Ordinal)&&
           !UnderworldToolIdentity.Held(player,UnderworldToolRegistrar.SlagPickPrefab))
        {
            __result=false;
            if(!repeat)player.Message(MessageHud.MessageType.Center,"Fracture Crystal seams require a Slag Pick.");
            return false;
        }
        if(string.Equals(prefab,Ice,StringComparison.Ordinal)&&
           UnderworldToolIdentity.Held(player,UnderworldToolRegistrar.RimeChiselPrefab))
        {
            __state=__instance.m_amount;
            __instance.m_amount=Math.Max(2,__instance.m_amount);
        }
        return true;
    }

    [HarmonyPostfix]
    private static void Postfix(Pickable __instance,int __state)
    {
        if(__state>=0&&__instance)__instance.m_amount=__state;
    }
}
