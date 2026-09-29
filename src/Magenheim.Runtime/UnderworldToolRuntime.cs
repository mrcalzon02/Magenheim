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

    [HarmonyPrefix]
    private static bool Prefix(Pickable __instance,Humanoid character,bool repeat,ref bool __result)
    {
        if(__instance is null||character is not Player player)return true;
        if(!string.Equals(Utils.GetPrefabName(__instance.gameObject),Fracture,StringComparison.Ordinal))return true;
        if(UnderworldToolIdentity.Held(player,UnderworldToolRegistrar.SlagPickPrefab))return true;
        __result=false;
        if(!repeat)player.Message(MessageHud.MessageType.Center,"Fracture Crystal seams require a Slag Pick.");
        return false;
    }
}

[HarmonyPatch(typeof(Pickable),"RPC_Pick")]
internal static class UnderworldToolPickRpcPatch
{
    private const string Ice="Magenheim_Underworld_ResourcePickup_ClearIce";

    [HarmonyPrefix]
    private static void Prefix(Pickable __instance,long sender,ref int bonus)
    {
        if(__instance is null||!string.Equals(Utils.GetPrefabName(__instance.gameObject),Ice,StringComparison.Ordinal))return;
        if(!TryResolveSender(sender,out var player)||player.gameObject.scene.handle!=__instance.gameObject.scene.handle)return;
        if(UnderworldToolIdentity.Held(player,UnderworldToolRegistrar.RimeChiselPrefab))bonus+=1;
    }

    private static bool TryResolveSender(long sender,out Player player)
    {
        player=null!;
        if(ZDOMan.instance is not null&&sender==ZDOMan.GetSessionID()&&Player.m_localPlayer)
        {
            player=Player.m_localPlayer;
            return true;
        }
        if(ZNet.instance is null||ZNetScene.instance is null)return false;
        var peer=ZNet.instance.GetPeer(sender);
        var instance=peer is null?null:ZNetScene.instance.FindInstance(peer.m_characterID);
        var resolved=instance?instance.GetComponent<Player>():null;
        if(!resolved)return false;
        player=resolved;
        return true;
    }
}


[HarmonyPatch(typeof(Player),nameof(Player.TryPlacePiece))]
internal static class AnchorSpikeConsumeOnPlacePatch
{
    [HarmonyPostfix]
    private static void Postfix(Player __instance,bool __result)
    {
        if(!__result||__instance is null||__instance.m_buildPieces is null)return;
        var selected=__instance.m_buildPieces.GetSelectedPiece();
        if(selected is null||!string.Equals(Utils.GetPrefabName(selected.gameObject),UnderworldToolPlaceables.AnchorPlacedPrefab,StringComparison.Ordinal))return;
        var tool=__instance.GetCurrentWeapon();
        if(tool?.m_dropPrefab is null||!string.Equals(tool.m_dropPrefab.name,UnderworldToolRegistrar.AnchorSpikePrefab,StringComparison.Ordinal))return;
        var inventory=__instance.GetInventory();
        if(inventory.ContainsItem(tool))inventory.RemoveItem(tool,1);
    }
}
