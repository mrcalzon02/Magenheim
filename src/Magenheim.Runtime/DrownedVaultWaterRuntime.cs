using System;
using System.Linq;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Creates real Valheim water inside Drowned Vault rooms by cloning the configured WaterVolume
/// subtree from a native hot-spring donor. This deliberately preserves Valheim's own in-water /
/// swimming detection instead of emulating immersion with trigger flags or decorative meshes.
///
/// Coordinate contract for every Drowned Vault room:
///   Y = 0        structural waterline / routed-passage datum
///   Y < 0        flooded floor, down to -WaterDepthMeters
///   Y > 0        dry ledges, docks, causeways and air-pocket space
/// </summary>
internal static class DrownedVaultWaterRuntime
{
    internal const string DonorPrefab = "HotSpring1";
    internal const float PlayerCheckAboveSurfaceMeters = 1.25f;
    private const float HorizontalInsetFactor = 0.84f;

    internal static DrownedVaultWaterBinding? Attach(
        GameObject room,
        UnderworldDrownedVaultRoomDefinition definition)
    {
        if (room is null) throw new ArgumentNullException(nameof(room));
        if (definition is null) throw new ArgumentNullException(nameof(definition));
        definition.Validate();

        if (definition.RouteMode == UnderworldDrownedVaultRouteMode.Dry)
        {
            if (definition.WaterDepthMeters != 0d)
                throw new InvalidOperationException("Dry Drowned Vault rooms cannot carry a water volume.");
            return null;
        }

        var donor = PrefabManager.Instance.GetPrefab(DonorPrefab)
            ?? throw new InvalidOperationException(
                $"Drowned Vault water donor '{DonorPrefab}' is unavailable.");
        var donorVolume = donor.GetComponentInChildren<WaterVolume>(includeInactive: true)
            ?? throw new InvalidOperationException(
                $"Drowned Vault water donor '{DonorPrefab}' has no native WaterVolume.");

        // Clone the WaterVolume-owned subtree rather than constructing a lookalike component graph.
        // Unity remaps the donor's WaterVolume -> surface renderer references within the clone.
        var waterRoot = UnityEngine.Object.Instantiate(donorVolume.gameObject);
        waterRoot.name = "Magenheim_DrownedVault_WaterVolume";
        waterRoot.SetActive(false);
        waterRoot.transform.SetParent(room.transform, worldPositionStays: false);
        waterRoot.transform.localPosition = Vector3.zero;
        waterRoot.transform.localRotation = Quaternion.identity;
        waterRoot.transform.localScale = Vector3.one;

        StripHotSpringGameplay(waterRoot);

        var volume = waterRoot.GetComponent<WaterVolume>()
            ?? throw new InvalidOperationException(
                "Cloned Drowned Vault water donor lost its WaterVolume component.");
        var collider = waterRoot.GetComponent<BoxCollider>()
            ?? throw new InvalidOperationException(
                "Native Drowned Vault WaterVolume donor must expose its trigger as a root BoxCollider.");
        var surface = volume.m_waterSurface
            ?? throw new InvalidOperationException(
                "Native Drowned Vault WaterVolume donor has no configured water-surface renderer.");
        var filter = surface.GetComponent<MeshFilter>()
            ?? throw new InvalidOperationException(
                "Native Drowned Vault water surface has no MeshFilter.");
        if (filter.sharedMesh is null)
            throw new InvalidOperationException(
                "Native Drowned Vault water surface has no mesh.");

        var targetWidth = CheckedFloat(
            definition.Room.WidthMeters * HorizontalInsetFactor,
            nameof(definition.Room.WidthMeters));
        var targetDepth = CheckedFloat(
            definition.Room.DepthMeters * HorizontalInsetFactor,
            nameof(definition.Room.DepthMeters));
        var waterDepth = CheckedFloat(
            definition.WaterDepthMeters,
            nameof(definition.WaterDepthMeters));

        // The trigger extends slightly above the visible waterline so Character/Floating queries
        // cannot flicker at wave crests. Its bottom is the authored flooded-floor depth.
        collider.isTrigger = true;
        collider.center = new Vector3(
            0f,
            (PlayerCheckAboveSurfaceMeters - waterDepth) * .5f,
            0f);
        collider.size = new Vector3(
            targetWidth,
            waterDepth + PlayerCheckAboveSurfaceMeters,
            targetDepth);

        // The native surface is kept at the room's Y=0 datum. Resize only the horizontal surface;
        // the WaterVolume itself owns wave/material behavior and remains the swimming authority.
        var meshSize = filter.sharedMesh.bounds.size;
        if (meshSize.x <= .001f || meshSize.z <= .001f)
            throw new InvalidOperationException(
                "Native Drowned Vault water surface has a degenerate horizontal mesh.");
        var surfaceTransform = surface.transform;
        var local = surfaceTransform.localPosition;
        surfaceTransform.localPosition = new Vector3(local.x, 0f, local.z);
        surfaceTransform.localRotation = Quaternion.identity;
        surfaceTransform.localScale = new Vector3(
            targetWidth / meshSize.x,
            surfaceTransform.localScale.y,
            targetDepth / meshSize.z);

        // Interior pools are locally bounded and should not query overworld terrain heightmaps or
        // use outdoor wind amplitude. The donor still supplies the actual Valheim water material,
        // trigger integration and WaterVolume implementation.
        volume.m_heightmap = null;
        volume.m_forceDepth = Mathf.Clamp01(waterDepth / 10f);
        volume.m_surfaceOffset = 0f;
        volume.m_useGlobalWind = false;

        SetLayerRecursively(waterRoot, donorVolume.gameObject.layer);
        var marker = waterRoot.AddComponent<DrownedVaultWaterBinding>();
        marker.Bind(definition.Room.Id, definition.RouteMode, waterDepth, volume, collider);
        waterRoot.SetActive(true);
        return marker;
    }

    internal static void ValidateDonor()
    {
        var donor = PrefabManager.Instance.GetPrefab(DonorPrefab)
            ?? throw new InvalidOperationException(
                $"Drowned Vault water donor '{DonorPrefab}' is unavailable.");
        var volume = donor.GetComponentInChildren<WaterVolume>(includeInactive: true)
            ?? throw new InvalidOperationException(
                $"Drowned Vault water donor '{DonorPrefab}' has no WaterVolume.");
        if (volume.GetComponent<BoxCollider>() is null)
            throw new InvalidOperationException(
                $"Drowned Vault water donor '{DonorPrefab}' WaterVolume has no root BoxCollider.");
        if (volume.m_waterSurface is null ||
            volume.m_waterSurface.GetComponent<MeshFilter>()?.sharedMesh is null)
            throw new InvalidOperationException(
                $"Drowned Vault water donor '{DonorPrefab}' has no reusable surface mesh.");
    }

    private static void StripHotSpringGameplay(GameObject root)
    {
        // Water is the only donor behavior retained. A cloned hot spring must never bring healing,
        // random spawns, terrain edits, smoke, audio or network identities into a dungeon room.
        DestroyAll<EffectArea>(root);
        DestroyAll<RandomSpawn>(root);
        DestroyAll<TerrainModifier>(root);
        DestroyAll<SmokeSpawner>(root);
        DestroyAll<ZSFX>(root);
        DestroyAll<AudioSource>(root);
        DestroyAll<ParticleSystem>(root);
        DestroyAll<Light>(root);
        DestroyAll<ZNetView>(root);
        DestroyAll<Piece>(root);
        DestroyAll<WearNTear>(root);
    }

    private static void DestroyAll<T>(GameObject root) where T : Component
    {
        foreach (var component in root.GetComponentsInChildren<T>(includeInactive: true))
        {
            if (component is WaterVolume) continue;
            UnityEngine.Object.DestroyImmediate(component);
        }
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        foreach (var transform in root.GetComponentsInChildren<Transform>(includeInactive: true))
            transform.gameObject.layer = layer;
    }

    private static float CheckedFloat(double value, string field)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) ||
            value < 0d || value > float.MaxValue)
            throw new InvalidOperationException(
                $"Drowned Vault water field '{field}' cannot be represented by Valheim's float API.");
        return (float)value;
    }
}

internal sealed class DrownedVaultWaterBinding : MonoBehaviour
{
    internal string RoomFamilyId { get; private set; } = string.Empty;
    internal UnderworldDrownedVaultRouteMode RouteMode { get; private set; }
    internal float WaterDepthMeters { get; private set; }
    internal WaterVolume? Volume { get; private set; }
    internal BoxCollider? Trigger { get; private set; }

    internal void Bind(
        string roomFamilyId,
        UnderworldDrownedVaultRouteMode routeMode,
        float waterDepthMeters,
        WaterVolume volume,
        BoxCollider trigger)
    {
        if (string.IsNullOrWhiteSpace(roomFamilyId))
            throw new ArgumentException("Drowned Vault room-family identity is required.", nameof(roomFamilyId));
        RoomFamilyId = roomFamilyId;
        RouteMode = routeMode;
        WaterDepthMeters = waterDepthMeters;
        Volume = volume ?? throw new ArgumentNullException(nameof(volume));
        Trigger = trigger ?? throw new ArgumentNullException(nameof(trigger));
    }

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(RoomFamilyId) ||
            Volume is null ||
            Trigger is null ||
            !Trigger.isTrigger ||
            RouteMode == UnderworldDrownedVaultRouteMode.Dry ||
            WaterDepthMeters <= 0f)
            throw new InvalidOperationException(
                "Drowned Vault native water binding is incomplete.");
    }
}
