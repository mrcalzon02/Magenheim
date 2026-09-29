using System;
using System.Collections.Generic;
using System.Linq;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

internal sealed class DrownedVaultInteriorBinding
{
    internal DrownedVaultInteriorBinding(bool hasInterior, float interiorRadius, string environment)
    {
        HasInterior = hasInterior;
        InteriorRadius = interiorRadius;
        Environment = environment;
    }

    internal bool HasInterior { get; }
    internal float InteriorRadius { get; }
    internal string Environment { get; }

    internal void Validate()
    {
        if (!HasInterior)
            throw new InvalidOperationException(
                "Drowned Vault locations may not register without a real interior.");
        if (float.IsNaN(InteriorRadius) || float.IsInfinity(InteriorRadius) || InteriorRadius <= 0f)
            throw new InvalidOperationException(
                "Drowned Vault interior radius must be finite and positive.");
        if (string.IsNullOrWhiteSpace(Environment))
            throw new InvalidOperationException(
                "Drowned Vault interior requires a Blackwater environment identity.");
    }
}

internal sealed class DrownedVaultInteriorBinder
{
    internal const string InteriorRootName = "Magenheim_DrownedVaults_InteriorRoot";
    internal const float ConservativeInteriorRadius = 1900f;

    internal static string InteriorEnvironment =>
        UnderworldWeatherRuntime.EnvironmentName(
            UnderworldTerrainBiome.BlackwaterDeep,
            UnderworldAtmosphereEvent.None);

    internal DrownedVaultInteriorBinding AttachInterior(GameObject locationContainer)
    {
        if (locationContainer is null)
            throw new ArgumentNullException(nameof(locationContainer));

        var transforms = locationContainer.GetComponentsInChildren<Transform>(includeInactive: true);
        var portalAnchor = transforms.SingleOrDefault(value =>
            string.Equals(
                value.name,
                DrownedVaultEntranceVisuals.EntrancePortalAnchorName,
                StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                "Drowned Vault location has no authoritative entrance anchor.");
        var interiorAnchor = transforms.SingleOrDefault(value =>
            string.Equals(
                value.name,
                DrownedVaultEntranceVisuals.InteriorAnchorName,
                StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                "Drowned Vault location has no authoritative interior anchor.");

        var existing = interiorAnchor.Cast<Transform>()
            .SingleOrDefault(value =>
                string.Equals(value.name, InteriorRootName, StringComparison.Ordinal));
        if (existing is not null)
            throw new InvalidOperationException(
                "Drowned Vault interior authority is already attached.");

        DrownedVaultTravel.AttachEntrancePortal(portalAnchor);

        var root = new GameObject(InteriorRootName);
        root.transform.SetParent(interiorAnchor, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.AddComponent<ZNetView>();
        root.AddComponent<DrownedVaultEncounterAuthority>();
        root.AddComponent<DrownedVaultInteriorRuntime>();

        return new DrownedVaultInteriorBinding(
            true,
            ConservativeInteriorRadius,
            InteriorEnvironment);
    }
}

internal sealed class DrownedVaultInteriorRuntime : MonoBehaviour
{
    private readonly List<PendingPopulation> _pendingPopulation = new();
    private DrownedVaultEncounterAuthority? _authority;
    private int _locationSeed;
    private bool _geometryBuilt;
    private bool _populationApplied;

    private void Start()
    {
        if (_geometryBuilt) return;

        UnderworldBlackwaterDrownedVaultsCatalog.Validate();
        _authority = GetComponent<DrownedVaultEncounterAuthority>()
            ?? throw new InvalidOperationException(
                "Drowned Vault interior has no persistent encounter authority.");

        _locationSeed = StableLocationSeed(transform.position);
        var topology = UnderworldBiomeDungeonPlanner.Build(
            UnderworldDungeonCatalog.BlackwaterDeep,
            _locationSeed,
            UnderworldBlackwaterDrownedVaultsCatalog.RoomFamilyIds(),
            UnderworldBlackwaterDrownedVaultsCatalog.EntranceRoomId);
        var spatial = UnderworldBiomeDungeonSpatialPlanner.Build(
            topology,
            UnderworldBlackwaterDrownedVaultsCatalog.SpatialDefinitions());

        var spatialById = spatial.Rooms.ToDictionary(
            value => value.InstanceId,
            StringComparer.Ordinal);
        var definitions = UnderworldBlackwaterDrownedVaultsCatalog.Rooms.ToDictionary(
            value => value.Room.Id,
            StringComparer.Ordinal);

        GameObject? entranceRoom = null;
        foreach (var placement in topology.Rooms)
        {
            if (!spatialById.TryGetValue(placement.InstanceId, out var position))
                throw new InvalidOperationException(
                    $"Drowned Vault topology room '{placement.InstanceId}' has no spatial placement.");
            if (!definitions.TryGetValue(placement.RoomFamilyId, out var definition))
                throw new InvalidOperationException(
                    $"Drowned Vault topology references unknown room family '{placement.RoomFamilyId}'.");

            var source = DrownedVaultRoomRegistrar.ResolveRoom(placement.RoomFamilyId);
            var instance = Instantiate(source.gameObject, transform, false);
            instance.name =
                DrownedVaultRoomVisuals.RoomPrefabName(definition) +
                "_" + placement.InstanceId;
            instance.transform.localPosition =
                new Vector3((float)position.X, (float)position.Y, (float)position.Z);
            instance.transform.localRotation =
                Quaternion.Euler(0f, (float)position.YawDegrees, 0f);
            instance.SetActive(true);

            var room = instance.GetComponent<Room>()
                ?? throw new InvalidOperationException(
                    $"Drowned Vault room '{instance.name}' has no Room component.");
            _ = room;
            foreach (var water in instance.GetComponentsInChildren<DrownedVaultWaterBinding>(true))
                water.Validate();

            _pendingPopulation.Add(new PendingPopulation(
                instance,
                placement,
                definition));

            if (string.Equals(
                    placement.InstanceId,
                    topology.Rooms[0].InstanceId,
                    StringComparison.Ordinal))
                entranceRoom = instance;
        }

        DrownedVaultPassageAssembler.Assemble(transform, topology, spatial);
        if (entranceRoom is null)
            throw new InvalidOperationException(
                "Drowned Vaults did not instantiate the authored entrance room.");
        DrownedVaultTravel.Bind(transform, entranceRoom);
        _geometryBuilt = true;
        TryPopulate();
    }

    private void Update()
    {
        if (!_geometryBuilt || _populationApplied) return;
        TryPopulate();
    }

    private void TryPopulate()
    {
        var authority = _authority;
        if (authority is null || !authority.HasAuthority) return;

        foreach (var pending in _pendingPopulation)
            DrownedVaultEncounterSpawner.Populate(
                pending.Room,
                pending.Placement,
                pending.Definition,
                authority,
                _locationSeed);

        _pendingPopulation.Clear();
        _populationApplied = true;
    }

    private sealed record PendingPopulation(
        GameObject Room,
        UnderworldDungeonRoomPlacement Placement,
        UnderworldDrownedVaultRoomDefinition Definition);

    internal static int StableLocationSeed(Vector3 worldPosition)
    {
        var x = (int)Math.Round(worldPosition.x * 100f, MidpointRounding.AwayFromZero);
        var y = (int)Math.Round(worldPosition.y * 100f, MidpointRounding.AwayFromZero);
        var z = (int)Math.Round(worldPosition.z * 100f, MidpointRounding.AwayFromZero);
        unchecked
        {
            uint hash = 2166136261u;
            hash = (hash ^ (uint)x) * 16777619u;
            hash = (hash ^ (uint)y) * 16777619u;
            hash = (hash ^ (uint)z) * 16777619u;
            return (int)hash;
        }
    }
}
