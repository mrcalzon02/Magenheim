using System;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Exterior Blackwater presentation for a Drowned Vault. The location is snapped to native world
/// water; only authored masonry/cave geometry is loaded here. Interior WaterVolume authority stays
/// exclusively inside the buried dungeon.
/// </summary>
internal static class DrownedVaultEntranceVisuals
{
    internal const string EntrancePortalAnchorName = "Magenheim_DrownedVaults_EntrancePortalAnchor";
    internal const string InteriorAnchorName = "Magenheim_DrownedVaults_InteriorAnchor";

    internal static void Build(GameObject locationContainer)
    {
        if (locationContainer is null)
            throw new ArgumentNullException(nameof(locationContainer));

        var entrance = UnderworldBlackwaterDrownedVaultsCatalog.Require(
            UnderworldBlackwaterDrownedVaultsCatalog.EntranceRoomId);
        ModelAssets.Load(
            locationContainer,
            entrance.Room.ModelId,
            scale: .48f,
            hideOriginal: false);

        var portalAnchor = new GameObject(EntrancePortalAnchorName);
        portalAnchor.transform.SetParent(locationContainer.transform, false);
        // Location SnapToWater puts Y=0 at the world waterline. Keep the entrance interaction
        // straddling that line so swimmers and players standing on a bank shelf can both reach it.
        portalAnchor.transform.localPosition = new Vector3(0f, -.45f, -4.8f);
        portalAnchor.transform.localRotation = Quaternion.identity;

        var interiorAnchor = new GameObject(InteriorAnchorName);
        interiorAnchor.transform.SetParent(locationContainer.transform, false);
        interiorAnchor.transform.localPosition = new Vector3(0f, -38f, -32f);
        interiorAnchor.transform.localRotation = Quaternion.identity;
    }
}
