using System;
using System.Collections.Generic;
using System.Linq;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

internal sealed class RootwarrenInteriorBinding
{
    internal RootwarrenInteriorBinding(
        bool hasInterior,
        float interiorRadius,
        string environment)
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
                "Rootwarren locations may not register without a real interior.");
        if (float.IsNaN(InteriorRadius) ||
            float.IsInfinity(InteriorRadius) ||
            InteriorRadius <= 0f)
            throw new InvalidOperationException(
                "Rootwarren interior radius must be finite and positive.");
        if (string.IsNullOrWhiteSpace(Environment))
            throw new InvalidOperationException(
                "Rootwarren interior requires an environment identity.");
    }
}

internal sealed class RootwarrenInteriorBinder
{
    internal const string InteriorRootName = "Magenheim_Rootwarren_InteriorRoot";
    internal const float ConservativeInteriorRadius = 1800f;

    internal static string InteriorEnvironment =>
        UnderworldWeatherRuntime.EnvironmentName(
            UnderworldTerrainBiome.FungalForest,
            UnderworldAtmosphereEvent.None);

    internal RootwarrenInteriorBinding AttachInterior(GameObject locationContainer)
    {
        if (locationContainer is null)
            throw new ArgumentNullException(nameof(locationContainer));

        var anchor = locationContainer
            .GetComponentsInChildren<Transform>(includeInactive: true)
            .SingleOrDefault(value =>
                string.Equals(
                    value.name,
                    RootwarrenEntranceVisuals.InteriorAnchorName,
                    StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"Rootwarren location '{locationContainer.name}' has no authoritative entrance anchor '{RootwarrenEntranceVisuals.InteriorAnchorName}'.");

        var existing = anchor.Cast<Transform>()
            .SingleOrDefault(value =>
                string.Equals(value.name, InteriorRootName, StringComparison.Ordinal));
        if (existing is not null)
            throw new InvalidOperationException(
                "Rootwarren interior authority is already attached.");

        RootwarrenTravel.AttachEntrancePortal(anchor);

        var root = new GameObject(InteriorRootName);
        root.transform.SetParent(anchor, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.AddComponent<ZNetView>();
        root.AddComponent<RootwarrenEncounterAuthority>();
        root.AddComponent<RootwarrenInteriorRuntime>();

        return new RootwarrenInteriorBinding(
            true,
            ConservativeInteriorRadius,
            InteriorEnvironment);
    }
}

internal sealed class RootwarrenInteriorRuntime : MonoBehaviour
{
    private readonly List<PendingPopulation> _pendingPopulation = new();
    private RootwarrenEncounterAuthority? _authority;
    private int _locationSeed;
    private bool _geometryBuilt;
    private bool _populationApplied;

    private void Start()
    {
        if (_geometryBuilt) return;

        UnderworldFungalRootwarrenCatalog.Validate();
        _authority = GetComponent<RootwarrenEncounterAuthority>()
            ?? throw new InvalidOperationException(
                "Rootwarren interior has no persistent encounter authority.");

        _locationSeed = StableLocationSeed(transform.position);
        var seed = _locationSeed;
        var topology = UnderworldBiomeDungeonPlanner.Build(
            UnderworldDungeonCatalog.FungalForest,
            seed,
            UnderworldFungalRootwarrenCatalog.RoomFamilyIds(),
            UnderworldFungalRootwarrenCatalog.EntranceRoomId);
        var spatial = UnderworldBiomeDungeonSpatialPlanner.Build(
            topology,
            UnderworldFungalRootwarrenCatalog.Rooms);

        var spatialById = spatial.Rooms.ToDictionary(
            value => value.InstanceId,
            StringComparer.Ordinal);
        var definitions = UnderworldFungalRootwarrenCatalog.Rooms.ToDictionary(
            value => value.Id,
            StringComparer.Ordinal);

        GameObject? entranceRoom = null;
        foreach (var placement in topology.Rooms)
        {
            if (!spatialById.TryGetValue(placement.InstanceId, out var position))
                throw new InvalidOperationException(
                    $"Rootwarren topology room '{placement.InstanceId}' has no spatial placement.");
            if (!definitions.TryGetValue(placement.RoomFamilyId, out var definition))
                throw new InvalidOperationException(
                    $"Rootwarren topology references unknown room family '{placement.RoomFamilyId}'.");

            var source = RootwarrenRoomRegistrar.ResolveRoom(placement.RoomFamilyId);
            var instance = Instantiate(source.gameObject, transform, false);
            instance.name =
                RootwarrenRoomVisuals.RoomPrefabName(definition) +
                "_" + placement.InstanceId;
            instance.transform.localPosition =
                new Vector3((float)position.X, (float)position.Y, (float)position.Z);
            instance.transform.localRotation =
                Quaternion.Euler(0f, (float)position.YawDegrees, 0f);
            instance.SetActive(true);

            var room = instance.GetComponent<Room>()
                ?? throw new InvalidOperationException(
                    $"Rootwarren room '{instance.name}' has no Room component.");
            _ = room;
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

        RootwarrenPassageAssembler.Assemble(transform, topology, spatial);
        if (entranceRoom is null)
            throw new InvalidOperationException(
                "Rootwarren did not instantiate its authored entrance room.");
        RootwarrenTravel.Bind(transform, entranceRoom);
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
            RootwarrenEncounterSpawner.Populate(
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
        UnderworldDungeonRoomDefinition Definition);

    internal static int StableLocationSeed(Vector3 worldPosition)
    {
        var x = (int)Math.Round(
            worldPosition.x * 100f,
            MidpointRounding.AwayFromZero);
        var y = (int)Math.Round(
            worldPosition.y * 100f,
            MidpointRounding.AwayFromZero);
        var z = (int)Math.Round(
            worldPosition.z * 100f,
            MidpointRounding.AwayFromZero);

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
