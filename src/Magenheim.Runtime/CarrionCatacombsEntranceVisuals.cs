using System;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

internal static class CarrionCatacombsEntranceVisuals
{
    internal const string EntrancePortalAnchorName=
        "Magenheim_CarrionCatacombs_EntrancePortalAnchor";
    internal const string InteriorAnchorName=
        "Magenheim_CarrionCatacombs_InteriorAnchor";

    internal static void Build(GameObject locationContainer)
    {
        if(locationContainer is null)
            throw new ArgumentNullException(nameof(locationContainer));

        var entrance=UnderworldGreatDecayCarrionCatacombsCatalog.Require(
            UnderworldGreatDecayCarrionCatacombsCatalog.EntranceRoomId);
        ModelAssets.Load(locationContainer,entrance.Room.ModelId,scale:.50f,hideOriginal:false);

        var portal=new GameObject(EntrancePortalAnchorName);
        portal.transform.SetParent(locationContainer.transform,false);
        portal.transform.localPosition=new Vector3(0f,1.05f,-4.8f);

        var interior=new GameObject(InteriorAnchorName);
        interior.transform.SetParent(locationContainer.transform,false);
        interior.transform.localPosition=new Vector3(0f,-38f,-32f);
    }
}
