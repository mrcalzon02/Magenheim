using System;
using System.Collections.Generic;
using System.Linq;

namespace Magenheim.Core.Underworld;

public sealed record UnderworldDungeonSpatialRoom(
    string InstanceId,
    string RoomFamilyId,
    double X,
    double Y,
    double Z,
    double YawDegrees);

public sealed record UnderworldBiomeDungeonSpatialPlan(
    string DungeonId,
    int Seed,
    double CellSizeMeters,
    IReadOnlyList<UnderworldDungeonSpatialRoom> Rooms);

/// <summary>
/// Deterministically embeds an ordinary biome-dungeon topology into physical dungeon space.
/// A conservative cell envelope derived from the largest authored room prevents room overlap;
/// corridor/passage runtime owns the empty distance between those cells.
/// </summary>
public static class UnderworldBiomeDungeonSpatialPlanner
{
    private static readonly GridPoint[] Directions =
    {
        new(1, 0), new(0, 1), new(-1, 0), new(0, -1),
        new(1, 1), new(-1, 1), new(-1, -1), new(1, -1),
    };

    public static UnderworldBiomeDungeonSpatialPlan Build(
        UnderworldBiomeDungeonPlan topology,
        IReadOnlyCollection<UnderworldDungeonRoomDefinition> roomDefinitions)
    {
        if (topology is null) throw new ArgumentNullException(nameof(topology));
        if (roomDefinitions is null) throw new ArgumentNullException(nameof(roomDefinitions));
        if (topology.Rooms.Count == 0)
            throw new InvalidOperationException("Cannot spatially embed an empty biome dungeon.");

        var definitions = roomDefinitions.ToDictionary(value => value.Id, StringComparer.Ordinal);
        foreach (var room in topology.Rooms)
            if (!definitions.ContainsKey(room.RoomFamilyId))
                throw new InvalidOperationException(
                    $"Dungeon topology references unknown room family '{room.RoomFamilyId}'.");

        var largestDiagonal = definitions.Values.Max(value =>
            Math.Sqrt(value.WidthMeters * value.WidthMeters +
                      value.DepthMeters * value.DepthMeters));
        var cellSize = Math.Ceiling(largestDiagonal + 12d);
        if (cellSize < 40d) cellSize = 40d;

        var parents = ParentIndex(topology);
        var occupied = new HashSet<GridPoint>();
        var grid = new GridPoint[topology.Rooms.Count];
        grid[0] = new GridPoint(0, 0);
        occupied.Add(grid[0]);

        for (var index = 1; index < topology.Rooms.Count; index++)
        {
            var parentIndex = parents[index];
            var parent = grid[parentIndex];
            var room = topology.Rooms[index];
            var offset = DirectionOffset(topology.Seed, room.InstanceId);

            var placed = false;
            for (var radius = 1; radius <= 9 && !placed; radius++)
            {
                for (var attempt = 0; attempt < Directions.Length; attempt++)
                {
                    var direction = Directions[(offset + attempt) % Directions.Length];
                    var candidate = new GridPoint(
                        parent.X + direction.X * radius,
                        parent.Z + direction.Z * radius);
                    if (!occupied.Add(candidate)) continue;
                    grid[index] = candidate;
                    placed = true;
                    break;
                }
            }

            if (!placed)
                throw new InvalidOperationException(
                    $"Could not place biome-dungeon room '{room.InstanceId}' without grid collision.");
        }

        var spatial = new List<UnderworldDungeonSpatialRoom>(topology.Rooms.Count);
        for (var index = 0; index < topology.Rooms.Count; index++)
        {
            var room = topology.Rooms[index];
            var point = grid[index];
            var yaw = 0d;
            if (index > 0)
            {
                var parent = grid[parents[index]];
                yaw = Math.Atan2(
                    point.X - parent.X,
                    point.Z - parent.Z) * 180d / Math.PI;
            }

            // A modest depth slope makes the dungeon feel excavated downward without requiring
            // every branch to occupy a separate vertical floor.
            var y = -Math.Min(room.Depth, 12) * 2.75d;
            spatial.Add(new UnderworldDungeonSpatialRoom(
                room.InstanceId,
                room.RoomFamilyId,
                point.X * cellSize,
                y,
                point.Z * cellSize,
                yaw));
        }

        var result = new UnderworldBiomeDungeonSpatialPlan(
            topology.DungeonId,
            topology.Seed,
            cellSize,
            spatial.AsReadOnly());
        Validate(result, definitions);
        return result;
    }

    public static void Validate(
        UnderworldBiomeDungeonSpatialPlan plan,
        IReadOnlyDictionary<string, UnderworldDungeonRoomDefinition> definitions)
    {
        if (plan is null) throw new ArgumentNullException(nameof(plan));
        if (definitions is null) throw new ArgumentNullException(nameof(definitions));
        if (plan.Rooms.Count == 0)
            throw new InvalidOperationException("Spatial dungeon plan cannot be empty.");
        if (double.IsNaN(plan.CellSizeMeters) ||
            double.IsInfinity(plan.CellSizeMeters) ||
            plan.CellSizeMeters <= 0d)
            throw new InvalidOperationException("Spatial dungeon cell size must be finite and positive.");

        for (var left = 0; left < plan.Rooms.Count; left++)
        {
            var a = plan.Rooms[left];
            if (!definitions.TryGetValue(a.RoomFamilyId, out var aDef))
                throw new InvalidOperationException(
                    $"Spatial room '{a.InstanceId}' has no room definition.");
            if (!Finite(a.X) || !Finite(a.Y) || !Finite(a.Z) || !Finite(a.YawDegrees))
                throw new InvalidOperationException(
                    $"Spatial room '{a.InstanceId}' contains non-finite coordinates.");

            for (var right = left + 1; right < plan.Rooms.Count; right++)
            {
                var b = plan.Rooms[right];
                if (!definitions.TryGetValue(b.RoomFamilyId, out var bDef))
                    throw new InvalidOperationException(
                        $"Spatial room '{b.InstanceId}' has no room definition.");

                var dx = a.X - b.X;
                var dz = a.Z - b.Z;
                var centreDistance = Math.Sqrt(dx * dx + dz * dz);
                var aRadius = Math.Sqrt(aDef.WidthMeters * aDef.WidthMeters +
                                        aDef.DepthMeters * aDef.DepthMeters) * .5d;
                var bRadius = Math.Sqrt(bDef.WidthMeters * bDef.WidthMeters +
                                        bDef.DepthMeters * bDef.DepthMeters) * .5d;
                if (centreDistance + .001d < aRadius + bRadius + 6d)
                    throw new InvalidOperationException(
                        $"Spatial dungeon rooms '{a.InstanceId}' and '{b.InstanceId}' overlap their authored envelopes.");
            }
        }
    }

    private static int[] ParentIndex(UnderworldBiomeDungeonPlan topology)
    {
        var indices = topology.Rooms
            .Select((room, index) => new { room.InstanceId, Index = index })
            .ToDictionary(value => value.InstanceId, value => value.Index, StringComparer.Ordinal);
        var parents = Enumerable.Repeat(-1, topology.Rooms.Count).ToArray();
        parents[0] = 0;

        foreach (var connection in topology.Connections)
        {
            if (connection.IsLoop) continue;
            if (!indices.TryGetValue(connection.FromInstanceId, out var from) ||
                !indices.TryGetValue(connection.ToInstanceId, out var to))
                throw new InvalidOperationException(
                    "Biome dungeon topology contains an unknown parent edge.");
            if (to == 0 || parents[to] >= 0)
                throw new InvalidOperationException(
                    $"Biome dungeon room '{connection.ToInstanceId}' has multiple/non-root parent edges.");
            parents[to] = from;
        }

        for (var index = 1; index < parents.Length; index++)
            if (parents[index] < 0)
                throw new InvalidOperationException(
                    $"Biome dungeon room '{topology.Rooms[index].InstanceId}' is not connected to the root.");
        return parents;
    }

    private static int DirectionOffset(int seed, string instanceId)
    {
        unchecked
        {
            uint hash = (uint)seed ^ 2166136261u;
            foreach (var character in instanceId)
            {
                hash ^= character;
                hash *= 16777619u;
            }
            return (int)(hash % (uint)Directions.Length);
        }
    }

    private static bool Finite(double value) =>
        !double.IsNaN(value) && !double.IsInfinity(value);

    private readonly struct GridPoint : IEquatable<GridPoint>
    {
        internal GridPoint(int x, int z)
        {
            X = x;
            Z = z;
        }

        internal int X { get; }
        internal int Z { get; }

        public bool Equals(GridPoint other) => X == other.X && Z == other.Z;
        public override bool Equals(object? obj) => obj is GridPoint other && Equals(other);
        public override int GetHashCode()
        {
            unchecked { return (X * 397) ^ Z; }
        }
    }
}
