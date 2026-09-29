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

        var anchor = new GameObject(InteriorAnchorName);
        anchor.transform.SetParent(locationContainer.transform, false);
        anchor.transform.localPosition = new Vector3(0f, 1.15f, -6.8f);
        anchor.transform.localRotation = Quaternion.identity;
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
