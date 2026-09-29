using System;
using System.Linq;
using HarmonyLib;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

internal sealed class UnderworldStationPlacementConstraint : MonoBehaviour
{
    [SerializeField] private string _stationPrefab=string.Empty;
    internal void Bind(string stationPrefab)
    {
        if(string.IsNullOrWhiteSpace(stationPrefab))throw new ArgumentException("Station prefab is required.",nameof(stationPrefab));
        _stationPrefab=stationPrefab;
    }
    internal bool TryEvaluate(out UnderworldStationPlacementDecision decision)
    {
        if(string.IsNullOrWhiteSpace(_stationPrefab)){decision=UnderworldStationPlacementDecision.Reject("Underworld station siting identity is missing.");return true;}
        decision=UnderworldStationPlacementRuntime.Evaluate(UnderworldStationCatalog.Require(_stationPrefab),gameObject);
        return true;
    }
}

internal static class UnderworldStationPlacementRuntime
{
    private const string DecayDeepstoneId="magenheim.underworld.deepstone.decay";

    internal static UnderworldStationPlacementDecision Evaluate(UnderworldStationDefinition station,GameObject ghost)
    {
        if(station is null)throw new ArgumentNullException(nameof(station));
        if(!ghost)throw new ArgumentNullException(nameof(ghost));
        var halfX=station.Dimensions.WidthMeters*.43f;
        var halfZ=station.Dimensions.DepthMeters*.43f;
        var local=new[]{
            Vector3.zero,new Vector3(-halfX,0,-halfZ),new Vector3(halfX,0,-halfZ),
            new Vector3(-halfX,0,halfZ),new Vector3(halfX,0,halfZ),
            new Vector3(-halfX,0,0),new Vector3(halfX,0,0),
            new Vector3(0,0,-halfZ),new Vector3(0,0,halfZ),
        };
        var samples=new UnderworldTerrainResult[local.Length];
        for(var i=0;i<local.Length;i++)
        {
            var point=ghost.transform.TransformPoint(local[i]);
            samples[i]=UnderworldTerrainRuntime.SampleInstanceTerrain(point.x,point.y,point.z);
            if(!samples[i].Admitted)
                return UnderworldStationPlacement.Evaluate(station,new UnderworldStationPlacementSite(false,default,0,0,0,0,0,false,false,false));
        }
        var minHeight=samples.Min(value=>value.Height);var maxHeight=samples.Max(value=>value.Height);
        var site=new UnderworldStationPlacementSite(
            true,samples[0].Biome,samples[0].WaterDepth,
            samples.Min(value=>value.WaterDepth),samples.Max(value=>value.WaterDepth),
            maxHeight-minHeight,samples.Max(value=>value.Hazard01),
            UnderworldGeothermalHazardVolume.IsNearLiveVent(ghost.transform.position,Mathf.Max(halfX,halfZ)*.35f),
            UnderworldAnchorStabilizer.IsNear(ghost.transform.position,ghost.scene.handle,Mathf.Max(halfX,halfZ)*.35f),
            DecayDeepstoneAttuned());
        return UnderworldStationPlacement.Evaluate(station,site);
    }

    private static bool DecayDeepstoneAttuned()
    {
        try
        {
            var state=UnderworldDeepstoneRuntimeAuthority.ReconstructWorldState()
                .SingleOrDefault(value=>string.Equals(value.DeepstoneId,DecayDeepstoneId,StringComparison.Ordinal));
            return state is not null&&state.TrophyMounted&&state.BoonUnlocked;
        }
        catch{return false;}
    }

    internal static bool TryEvaluateGhost(GameObject? ghost,out UnderworldStationPlacementDecision decision)
    {
        decision=default;
        if(!ghost)return false;
        var constraint=ghost.GetComponentInChildren<UnderworldStationPlacementConstraint>(true);
        return constraint&&constraint.TryEvaluate(out decision);
    }
}

[HarmonyPatch(typeof(Player),nameof(Player.UpdatePlacementGhost))]
internal static class UnderworldStationPlacementGhostPatch
{
    [HarmonyPostfix]
    private static void Postfix(Player __instance)
    {
        if(__instance!=Player.m_localPlayer||!__instance.m_placementGhost||__instance.m_placementStatus!=Player.PlacementStatus.Valid)return;
        if(!UnderworldStationPlacementRuntime.TryEvaluateGhost(__instance.m_placementGhost,out var decision)||decision.Allowed)return;
        __instance.m_placementStatus=Player.PlacementStatus.Invalid;
        __instance.SetPlacementGhostValid(false);
    }
}

[HarmonyPatch(typeof(Player),nameof(Player.TryPlacePiece))]
internal static class UnderworldStationTryPlacePatch
{
    [HarmonyPrefix]
    private static bool Prefix(Player __instance,ref bool __result)
    {
        if(!UnderworldStationPlacementRuntime.TryEvaluateGhost(__instance.m_placementGhost,out var decision)||decision.Allowed)return true;
        __result=false;
        if(__instance==Player.m_localPlayer&&!string.IsNullOrWhiteSpace(decision.Diagnostic))
            __instance.Message(MessageHud.MessageType.Center,decision.Diagnostic);
        return false;
    }

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
