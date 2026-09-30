using System;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

internal static class UnderworldToolPlaceables
{
    internal const string SporelightPlacedPrefab="Magenheim_Underworld_SporelightLantern_Placed";
    internal const string AnchorPlacedPrefab="Magenheim_Underworld_AnchorSpike_Deployed";

    internal static void Register(
        UnderworldEquipmentDefinition sporelight,
        UnderworldEquipmentDefinition anchor,
        CustomPieceTable anchorTable)
    {
        RegisterSporelight(sporelight);
        RegisterAnchor(anchor,anchorTable);
    }

    private static void RegisterSporelight(UnderworldEquipmentDefinition d)
    {
        RequirePrefab("piece_groundtorch_wood");
        if(PrefabManager.Instance.GetPrefab(SporelightPlacedPrefab))
            throw new InvalidOperationException("Occupied placed Sporelight identity.");
        var config=new PieceConfig{
            Name="Sporelight Lantern",
            Description="A fuel-free cultivated glowcap lamp. Dense Fungal spores dim its useful light.",
            PieceTable="Hammer",Category="Furniture",
            Requirements=new[]{new RequirementConfig(d.Prefab,1,0,true)}};
        if(EarthAssets.IconExists(d.ModelId))config.Icon=EarthAssets.Icon(d.ModelId);
        var custom=new CustomPiece(SporelightPlacedPrefab,"piece_groundtorch_wood",config);
        custom.Piece.m_dlc=string.Empty;var prefab=custom.PiecePrefab;
        foreach(var fireplace in prefab.GetComponentsInChildren<Fireplace>(true))
            UnityEngine.Object.DestroyImmediate(fireplace);
        var root=ModelAssets.Exists(d.ModelId)?ModelAssets.Load(prefab,d.ModelId):prefab;
        PreferOwnedLights(prefab,root);
        prefab.AddComponent<UnderworldSporelightOutput>();
        PlacementSnapAuthority.AlignToBase(prefab);
        if(!PieceManager.Instance.AddPiece(custom))throw new InvalidOperationException("Jotunn refused placed Sporelight.");
    }

    private static void RegisterAnchor(UnderworldEquipmentDefinition d,CustomPieceTable table)
    {
        if(table is null)throw new ArgumentNullException(nameof(table));
        RequirePrefab("wood_floor");
        if(PrefabManager.Instance.GetPrefab(AnchorPlacedPrefab))
            throw new InvalidOperationException("Occupied deployed Anchor Spike identity.");
        var config=new PieceConfig{
            Name="Deployed Anchor Spike",
            Description="A driven Fracture stabilizer. The one-use Anchor Spike tool is spent by deployment.",
            PieceTable=UnderworldToolRegistrar.AnchorPieceTable,Category="Misc"};
        if(EarthAssets.IconExists(d.ModelId))config.Icon=EarthAssets.Icon(d.ModelId);
        var custom=new CustomPiece(AnchorPlacedPrefab,"wood_floor",config);
        custom.Piece.m_dlc=string.Empty;
        custom.Piece.m_resources=Array.Empty<Piece.Requirement>();
        var prefab=custom.PiecePrefab;
        if(ModelAssets.Exists(d.ModelId))ModelAssets.Load(prefab,d.ModelId);
        foreach(var collider in prefab.GetComponentsInChildren<Collider>(true))
            if(!collider.isTrigger)collider.enabled=false;
        var collision=new GameObject("magenheim.anchor.collision"){layer=prefab.layer};
        collision.transform.SetParent(prefab.transform,false);
        var capsule=collision.AddComponent<CapsuleCollider>();
        capsule.center=Vector3.zero;capsule.radius=.32f;capsule.height=1.85f;
        prefab.AddComponent<UnderworldAnchorStabilizer>().Bind(8f);
        PlacementSnapAuthority.AlignToBase(prefab);
        if(!PieceManager.Instance.AddPiece(custom))throw new InvalidOperationException("Jotunn refused deployed Anchor Spike.");
    }

    internal static void PreferOwnedLights(GameObject prefab,GameObject ownedRoot)
    {
        var owned=ownedRoot.GetComponentsInChildren<Light>(true);
        if(owned.Length==0)return;
        foreach(var light in prefab.GetComponentsInChildren<Light>(true))
            if(!light.transform.IsChildOf(ownedRoot.transform))light.enabled=false;
    }

    private static void RequirePrefab(string prefab)
    {
        if(PrefabManager.Instance.GetPrefab(prefab) is null)
            throw new InvalidOperationException("Required Underworld tool placeable donor unavailable: "+prefab);
    }
}
