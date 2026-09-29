using System;
using System.Collections.Generic;
using System.Linq;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Realizes Rootwarren topology edges as walkable authored passage segments. The graph and room
/// positions remain Core authority; runtime only instantiates and orients the physical corridor kit.
/// </summary>
internal static class UnderworldFungalRootwarrenPassageAssembler
{
    private const float NominalPassageLength = 16f;

    internal static IReadOnlyList<Room> Assemble(
        Transform interiorRoot,
        UnderworldBiomeDungeonPlan topology,
        UnderworldBiomeDungeonSpatialPlan spatial)
    {
        if (interiorRoot is null) throw new ArgumentNullException(nameof(interiorRoot));
        if (topology is null) throw new ArgumentNullException(nameof(topology));
        if (spatial is null) throw new ArgumentNullException(nameof(spatial));
        if (!string.Equals(topology.DungeonId, spatial.DungeonId, StringComparison.Ordinal) ||
            topology.Seed != spatial.Seed)
            throw new InvalidOperationException(
                "Rootwarren topology/spatial plans do not describe the same dungeon.");

        var source = UnderworldFungalRootwarrenRoomRegistrar.ResolvePassage();
        var positions = spatial.Rooms.ToDictionary(
            value => value.InstanceId,
            value => new Vector3((float)value.X, (float)value.Y, (float)value.Z),
            StringComparer.Ordinal);
        var placed = new List<Room>();

        foreach (var connection in topology.Connections)
        {
            if (!positions.TryGetValue(connection.FromInstanceId, out var from) ||
                !positions.TryGetValue(connection.ToInstanceId, out var to))
                throw new InvalidOperationException(
                    "Rootwarren connection references an unplaced room.");

            var delta = to - from;
            var distance = delta.magnitude;
            if (distance < 1f)
                throw new InvalidOperationException(
                    "Rootwarren passage endpoints are too close to connect.");

            var direction = delta / distance;
            var segmentCount = Math.Max(1, (int)Math.Ceiling(distance / NominalPassageLength));
            var segmentLength = distance / segmentCount;
            var rotation = Quaternion.LookRotation(direction, Vector3.up);

            for (var index = 0; index < segmentCount; index++)
            {
                var t = (index + .5f) / segmentCount;
                var instance = UnityEngine.Object.Instantiate(
                    source.gameObject,
                    interiorRoot,
                    false);
                instance.name =
                    UnderworldFungalRootwarrenRoomVisuals.PassagePrefabName + "_" +
                    connection.FromInstanceId.Replace('.', '_') + "_to_" +
                    connection.ToInstanceId.Replace('.', '_') + "_" +
                    (index + 1).ToString("000");
                instance.transform.localPosition = Vector3.Lerp(from, to, t);
                instance.transform.localRotation = rotation;
                instance.transform.localScale = new Vector3(
                    1f,
                    1f,
                    segmentLength / NominalPassageLength);
                instance.SetActive(true);

                var room = instance.GetComponent<Room>()
                    ?? throw new InvalidOperationException(
                        "Rootwarren passage instance has no Room component.");
                placed.Add(room);
            }
        }

        return placed.AsReadOnly();
    }
}
