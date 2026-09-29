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
    internal const string InteriorAnchorName = "Magenheim_Rootwarren_InteriorAnchor";
    internal const string InteriorRootName = "Magenheim_Rootwarren_InteriorRoot";
    internal const string InteriorEnvironment = "Crypt";
    internal const float ConservativeInteriorRadius = 1800f;

    internal RootwarrenInteriorBinding AttachInterior(GameObject locationContainer)
    {
        if (locationContainer is null)
            throw new ArgumentNullException(nameof(locationContainer));

        var existing = locationContainer
            .GetComponentsInChildren<Transform>(includeInactive: true)
            .FirstOrDefault(value =>
                string.Equals(value.name, InteriorRootName, StringComparison.Ordinal));
        if (existing is not null)
            throw new InvalidOperationException(
                "Rootwarren interior authority is already attached.");

        var anchorObject = new GameObject(InteriorAnchorName);
        anchorObject.transform.SetParent(locationContainer.transform, false);
        anchorObject.transform.localPosition = Vector3.zero;
        anchorObject.transform.localRotation = Quaternion.identity;

        var root = new GameObject(InteriorRootName);
        root.transform.SetParent(anchorObject.transform, false);
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
    private bool _built;

    private void Start()
    {
        if (_built) return;

        UnderworldFungalRootwarrenCatalog.Validate();
        var authority = GetComponent<RootwarrenEncounterAuthority>()
            ?? throw new InvalidOperationException(
                "Rootwarren interior has no persistent encounter authority.");

        var seed = StableLocationSeed(transform.position);
        var topology = UnderworldBiomeDungeonPlanner.Build(
            UnderworldDungeonCatalog.FungalForest,
            seed,
            UnderworldFungalRootwarrenCatalog.RoomFamilyIds());
        var spatial = UnderworldBiomeDungeonSpatialPlanner.Build(
            topology,
            UnderworldFungalRootwarrenCatalog.Rooms);

        var spatialById = spatial.Rooms.ToDictionary(
            value => value.InstanceId,
            StringComparer.Ordinal);
        var definitions = UnderworldFungalRootwarrenCatalog.Rooms.ToDictionary(
            value => value.Id,
            StringComparer.Ordinal);

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
            RootwarrenEncounterSpawner.Populate(
                instance,
                placement,
                definition,
                authority,
                seed);
        }

        RootwarrenPassageAssembler.Assemble(transform, topology, spatial);
        _built = true;
    }

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
