using System;
using System.Collections.Generic;
using System.Linq;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Reclassifies generated ordinary-dungeon room instances by their real shortest-path depth from
/// the placed entrance, then reapplies ecology/reward pacing to those instances. Prefab-level risk
/// remains a deterministic pre-generation fallback only.
/// </summary>
internal static class UnderworldVanillaDungeonDepthPacing
{
    internal static void Apply(DungeonGenerator generator)
    {
        if (!generator) return;

        var theme = generator.GetComponent<DungeonGeneratorTheme>();
        if (!theme || string.IsNullOrWhiteSpace(theme.m_themeName)) return;

        var profile = UnderworldVanillaDungeonReuseCatalog.All.SingleOrDefault(candidate =>
            string.Equals(
                UnderworldVanillaDungeonRegistrar.ThemeName(candidate),
                theme.m_themeName,
                StringComparison.Ordinal));
        if (profile is null) return;

        var rooms = generator.GetComponentsInChildren<Room>(true)
            .Where(room => room && room.gameObject != generator.gameObject)
            .ToArray();
        if (rooms.Length == 0) return;

        var adjacency = BuildAdjacency(rooms);
        var depths = ShortestDepths(rooms, adjacency);
        var maximumDepth = depths.Where(value => value >= 0).DefaultIfEmpty(0).Max();

        for (var index = 0; index < rooms.Length; index++)
        {
            var room = rooms[index];
            var metadata = room.GetComponent<UnderworldVanillaDungeonRoomAuditMetadata>();
            if (!metadata ||
                string.IsNullOrWhiteSpace(metadata.DonorRoomName) ||
                metadata.DonorRoomIndex < 0)
                continue;

            var depth = depths[index];
            var band = BandFor(room, depth, maximumDepth);
            metadata.GeneratedDepth = depth;
            metadata.GeneratedRiskBand = band.ToString();

            var ecology = UnderworldVanillaDungeonEcologyPolicy.RebindAtBand(
                room.gameObject,
                profile,
                metadata.DonorRoomName,
                metadata.DonorRoomIndex,
                band);
            UnderworldVanillaDungeonRewardPolicy.RebindAtBand(
                room.gameObject,
                profile,
                metadata.DonorRoomName,
                metadata.DonorRoomIndex,
                ecology,
                band);

            UnderworldVanillaDungeonBiomeDressingPolicy.RebindGeneratedDepth(
                room.gameObject,
                profile,
                metadata.DonorRoomName,
                metadata.DonorRoomIndex,
                band);
        }
    }

    private static HashSet<int>[] BuildAdjacency(Room[] rooms)
    {
        var adjacency = Enumerable.Range(0, rooms.Length)
            .Select(_ => new HashSet<int>())
            .ToArray();
        var connections = rooms
            .Select(room => room.GetComponentsInChildren<RoomConnection>(false))
            .ToArray();

        for (var left = 0; left < connections.Length; left++)
        {
            foreach (var a in connections[left])
            {
                if (!a) continue;
                for (var right = left + 1; right < connections.Length; right++)
                {
                    foreach (var b in connections[right])
                    {
                        if (!b ||
                            !string.Equals(a.m_type, b.m_type, StringComparison.Ordinal) ||
                            Vector3.Distance(a.transform.position, b.transform.position) >= .12f)
                            continue;

                        adjacency[left].Add(right);
                        adjacency[right].Add(left);
                    }
                }
            }
        }

        return adjacency;
    }

    private static int[] ShortestDepths(Room[] rooms, IReadOnlyList<HashSet<int>> adjacency)
    {
        var depths = Enumerable.Repeat(-1, rooms.Length).ToArray();
        var queue = new Queue<int>();

        for (var index = 0; index < rooms.Length; index++)
        {
            if (!rooms[index].m_entrance) continue;
            depths[index] = 0;
            queue.Enqueue(index);
        }

        // Donors are expected to mark an entrance. Fail soft here so generation diagnostics can
        // capture the malformed graph rather than crashing the entire location generation pass.
        if (queue.Count == 0)
            return depths;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var next in adjacency[current])
            {
                if (depths[next] >= 0) continue;
                depths[next] = depths[current] + 1;
                queue.Enqueue(next);
            }
        }

        return depths;
    }

    private static UnderworldVanillaDungeonRiskBand BandFor(
        Room room,
        int depth,
        int maximumDepth)
    {
        if (room.m_entrance || depth <= 0 || maximumDepth <= 0)
            return UnderworldVanillaDungeonRiskBand.Outer;

        if (depth < 0)
            return UnderworldVanillaDungeonRiskBand.Outer;

        var fraction = depth / (double)maximumDepth;
        if (room.m_endCap && fraction >= .68d)
            return UnderworldVanillaDungeonRiskBand.Lair;
        if (fraction < .28d)
            return UnderworldVanillaDungeonRiskBand.Outer;
        if (fraction < .58d)
            return UnderworldVanillaDungeonRiskBand.Mid;
        if (fraction < .84d)
            return UnderworldVanillaDungeonRiskBand.Deep;
        return UnderworldVanillaDungeonRiskBand.Lair;
    }
}
