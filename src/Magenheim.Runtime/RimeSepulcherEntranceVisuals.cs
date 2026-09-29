using System;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

internal static class RimeSepulcherEntranceVisuals
{
    internal const string EntrancePortalAnchorName = "Magenheim_RimeSepulcher_EntrancePortalAnchor";
    internal const string InteriorAnchorName = "Magenheim_RimeSepulcher_InteriorAnchor";

    internal static void Build(GameObject locationContainer)
    {
        if (locationContainer is null)
            throw new ArgumentNullException(nameof(locationContainer));

        var entrance = UnderworldFrozenRimeSepulcherCatalog.Require(
            UnderworldFrozenRimeSepulcherCatalog.EntranceRoomId);
        ModelAssets.Load(
            locationContainer,
            entrance.Room.ModelId,
            scale: .52f,
            hideOriginal: false);

        var portal = new GameObject(EntrancePortalAnchorName);
        portal.transform.SetParent(locationContainer.transform, false);
        portal.transform.localPosition = new Vector3(0f, 1.05f, -4.7f);
        portal.transform.localRotation = Quaternion.identity;

        // Keep the complete generated dungeon well below/behind the reduced exterior Rime Mouth.
        var interior = new GameObject(InteriorAnchorName);
        interior.transform.SetParent(locationContainer.transform, false);
        interior.transform.localPosition = new Vector3(0f, -34f, -30f);
        interior.transform.localRotation = Quaternion.identity;
    }
}
