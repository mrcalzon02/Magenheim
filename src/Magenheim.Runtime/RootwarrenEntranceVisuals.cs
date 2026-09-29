using System;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Builds the physical Fungal Forest cave-mouth presentation for a Rootwarren location. The same
/// authored Fracture Mouth vocabulary is reused at reduced exterior scale so the dungeon looks
/// geologically related to its first interior chamber without duplicating a second entrance asset.
/// </summary>
internal static class RootwarrenEntranceVisuals
{
    internal const string EntrancePortalAnchorName = "Magenheim_Rootwarren_EntrancePortalAnchor";
    internal const string InteriorAnchorName = "Magenheim_Rootwarren_InteriorAnchor";

    internal static void Build(GameObject locationContainer)
    {
        if (locationContainer is null)
            throw new ArgumentNullException(nameof(locationContainer));

        var entrance = FindEntranceDefinition();
        ModelAssets.Load(
            locationContainer,
            entrance.ModelId,
            scale: .55f,
            hideOriginal: false);

        var portalAnchor = new GameObject(EntrancePortalAnchorName);
        portalAnchor.transform.SetParent(locationContainer.transform, false);
        portalAnchor.transform.localPosition = new Vector3(0f, 1.05f, -4.5f);
        portalAnchor.transform.localRotation = Quaternion.identity;

        // The full-size room kit is a buried dungeon, not a duplicate prop sitting on the mouth.
        // Keep room zero well below/behind the visible entrance so its colliders and cave shell do
        // not overlap the reduced exterior Fracture Mouth presentation.
        var interiorAnchor = new GameObject(InteriorAnchorName);
        interiorAnchor.transform.SetParent(locationContainer.transform, false);
        interiorAnchor.transform.localPosition = new Vector3(0f, -28f, -26f);
        interiorAnchor.transform.localRotation = Quaternion.identity;
    }

    private static UnderworldDungeonRoomDefinition FindEntranceDefinition()
    {
        foreach (var room in UnderworldFungalRootwarrenCatalog.Rooms)
            if (string.Equals(
                    room.Id,
                    UnderworldFungalRootwarrenCatalog.EntranceRoomId,
                    StringComparison.Ordinal))
                return room;

        throw new InvalidOperationException(
            "Rootwarren room catalog no longer contains its canonical Fracture Mouth entrance.");
    }
}
