using System;
using System.Linq;
using HarmonyLib;
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

        return AttachNativeWater(
            room,
            definition.Room.Id,
            definition.RouteMode,
            CheckedFloat(definition.Room.WidthMeters * HorizontalInsetFactor, nameof(definition.Room.WidthMeters)),
            CheckedFloat(definition.Room.DepthMeters * HorizontalInsetFactor, nameof(definition.Room.DepthMeters)),
            CheckedFloat(definition.WaterDepthMeters, nameof(definition.WaterDepthMeters)));
    }

    internal static DrownedVaultWaterBinding AttachExpandedDonorPool(
        GameObject room,
        string identity,
        float width,
        float depth,
        float waterDepth,
        float localSurfaceY)
    {
        if (room is null) throw new ArgumentNullException(nameof(room));
        if (string.IsNullOrWhiteSpace(identity))
            throw new ArgumentException("Expanded donor pool identity is required.", nameof(identity));
        if (width < 2f || depth < 2f)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (waterDepth <= .1f || waterDepth > 2.5f)
            throw new ArgumentOutOfRangeException(nameof(waterDepth));

        var binding = AttachNativeWater(
            room,
            identity,
            UnderworldDrownedVaultRouteMode.Mixed,
            width,
            depth,
            waterDepth);
        binding.transform.localPosition = new Vector3(0f, localSurfaceY, 0f);
        binding.Validate();
        return binding;
    }

    internal static DrownedVaultWaterBinding? AttachPassage(
        GameObject passage,
        string connectionIdentity,
        UnderworldDrownedVaultPassageState state)
    {
        if (passage is null) throw new ArgumentNullException(nameof(passage));
        if (string.IsNullOrWhiteSpace(connectionIdentity))
            throw new ArgumentException("Drowned Vault passage identity is required.", nameof(connectionIdentity));
        if (state is null) throw new ArgumentNullException(nameof(state));
        state.Validate();
        if (!state.HasWater) return null;

        return AttachNativeWater(
            passage,
            connectionIdentity,
            state.RouteMode,
            DrownedVaultRoomVisuals.PassageWidthMeters * HorizontalInsetFactor,
            DrownedVaultRoomVisuals.PassageDepthMeters * HorizontalInsetFactor,
            CheckedFloat(state.WaterDepthMeters, nameof(state.WaterDepthMeters)));
    }

    private static DrownedVaultWaterBinding AttachNativeWater(
        GameObject host,
        string identity,
        UnderworldDrownedVaultRouteMode routeMode,
        float targetWidth,
        float targetDepth,
        float waterDepth)
    {
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
        waterRoot.transform.SetParent(host.transform, worldPositionStays: false);
        // Dungeon passages may pitch between spatial cells. Water may yaw with the corridor, but its
        // surface must remain level in world space or Valheim's swim threshold becomes a ramp.
        waterRoot.transform.position = host.transform.position;
        waterRoot.transform.rotation = Quaternion.Euler(0f, host.transform.eulerAngles.y, 0f);
        waterRoot.transform.localScale = Vector3.one;

        StripHotSpringGameplay(waterRoot);

        var volume = waterRoot.GetComponent<WaterVolume>()
            ?? throw new InvalidOperationException(
                "Cloned Drowned Vault water donor lost its WaterVolume component.");
        // HotSpring1 supplies a native Collider, not necessarily a box. Rectangular dungeon
        // pools own a box trigger and must replace WaterVolume's already-cached Awake reference.
        foreach(var old in waterRoot.GetComponents<Collider>())
            UnityEngine.Object.DestroyImmediate(old);
        var collider = waterRoot.AddComponent<BoxCollider>();
        var surface = volume.m_waterSurface
            ?? throw new InvalidOperationException(
                "Native Drowned Vault WaterVolume donor has no configured water-surface renderer.");
        var filter = surface.GetComponent<MeshFilter>()
            ?? throw new InvalidOperationException(
                "Native Drowned Vault water surface has no MeshFilter.");
        if (filter.sharedMesh is null)
            throw new InvalidOperationException(
                "Native Drowned Vault water surface has no mesh.");

        collider.isTrigger = true;
        collider.center = new Vector3(
            0f,
            (PlayerCheckAboveSurfaceMeters - waterDepth) * .5f,
            0f);
        collider.size = new Vector3(
            targetWidth,
            waterDepth + PlayerCheckAboveSurfaceMeters,
            targetDepth);
        var cachedCollider=AccessTools.Field(typeof(WaterVolume), "m_collider")
            ?? throw new MissingFieldException(typeof(WaterVolume).FullName, "m_collider");
        cachedCollider.SetValue(volume,collider);

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

        volume.m_heightmap = null;
        volume.m_forceDepth = Mathf.Clamp01(waterDepth / 10f);
        volume.m_surfaceOffset = 0f;
        volume.m_useGlobalWind = false;

        SetLayerRecursively(waterRoot, donorVolume.gameObject.layer);
        var marker = waterRoot.AddComponent<DrownedVaultWaterBinding>();
        marker.Bind(identity, routeMode, waterDepth, volume, collider);
        waterRoot.SetActive(true);
        marker.Validate();
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
        if (volume.GetComponent<Collider>() is null)
            throw new InvalidOperationException(
                $"Drowned Vault water donor '{DonorPrefab}' WaterVolume has no root Collider.");
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
