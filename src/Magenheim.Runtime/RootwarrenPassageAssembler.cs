using System;
using System.Collections.Generic;
using System.Linq;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

internal static class RootwarrenPassageAssembler
{
    internal static void Assemble(
        Transform interiorRoot,
        UnderworldBiomeDungeonPlan topology,
        UnderworldBiomeDungeonSpatialPlan spatial)
    {
        if (interiorRoot is null) throw new ArgumentNullException(nameof(interiorRoot));
        if (topology is null) throw new ArgumentNullException(nameof(topology));
        if (spatial is null) throw new ArgumentNullException(nameof(spatial));

        var rooms = spatial.Rooms.ToDictionary(
            value => value.InstanceId,
            StringComparer.Ordinal);
        var source = RootwarrenRoomRegistrar.ResolvePassage();

        foreach (var connection in topology.Connections)
        {
            if (!rooms.TryGetValue(connection.FromInstanceId, out var from) ||
                !rooms.TryGetValue(connection.ToInstanceId, out var to))
                throw new InvalidOperationException(
                    "Rootwarren passage references an unknown spatial room.");

            var start = new Vector3((float)from.X, (float)from.Y + .6f, (float)from.Z);
            var end = new Vector3((float)to.X, (float)to.Y + .6f, (float)to.Z);
            var delta = end - start;
            var distance = delta.magnitude;
            if (distance < 1f) continue;

            var segmentSpacing = RootwarrenRoomVisuals.PassageLengthMeters * .86f;
            var count = Math.Max(1, Mathf.CeilToInt(distance / segmentSpacing));
            var rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);

            for (var index = 0; index < count; index++)
            {
                var t = (index + .5f) / count;
                var instance = UnityEngine.Object.Instantiate(
                    source.gameObject,
                    interiorRoot,
                    false);
                instance.name =
                    $"{RootwarrenRoomVisuals.PassagePrefabName}_" +
                    $"{Sanitize(connection.FromInstanceId)}_{Sanitize(connection.ToInstanceId)}_{index:000}";
                instance.transform.localPosition = Vector3.Lerp(start, end, t);
                instance.transform.localRotation = rotation;
                instance.SetActive(true);
            }
        }
    }

    private static string Sanitize(string value)
    {
        var last = value.LastIndexOf('.');
        return (last >= 0 ? value.Substring(last + 1) : value)
            .Replace('-', '_');
    }
}
