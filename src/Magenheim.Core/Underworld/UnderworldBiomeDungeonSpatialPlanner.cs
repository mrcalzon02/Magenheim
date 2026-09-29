using System;
using System.Collections.Generic;
using System.Linq;

namespace Magenheim.Core.Underworld;

public sealed record UnderworldDungeonSpatialRoom(
    string InstanceId,
    string RoomFamilyId,
    int GridX,
    int GridZ,
    double X,
    double Y,
    double Z,
    double YawDegrees);

public sealed record UnderworldDungeonSpatialWaypoint(
    int GridX,
    int GridZ,
    double X,
    double Y,
    double Z);

public sealed record UnderworldDungeonSpatialConnection(
    string FromInstanceId,
    string ToInstanceId,
    bool IsLoop,
    IReadOnlyList<UnderworldDungeonSpatialWaypoint> Waypoints);

public sealed record UnderworldBiomeDungeonSpatialPlan(
    string DungeonId,
    int Seed,
    double CellSizeMeters,
    IReadOnlyList<UnderworldDungeonSpatialRoom> Rooms,
    IReadOnlyList<UnderworldDungeonSpatialConnection> Connections);

/// <summary>
/// Deterministically embeds an ordinary biome-dungeon topology into physical dungeon space.
/// Room cells are sized from the largest authored room envelope. Corridors are then routed on the
/// same grid around occupied room cells, so a valid graph cannot turn into a tunnel through a third
/// chamber merely because two connected rooms happened to align.
/// </summary>
public static class UnderworldBiomeDungeonSpatialPlanner
{
    private static readonly GridPoint[] PlacementDirections =
    {
        new(1, 0), new(0, 1), new(-1, 0), new(0, -1),
        new(1, 1), new(-1, 1), new(-1, -1), new(1, -1),
    };

    private static readonly GridPoint[] RouteDirections =
    {
        new(1, 0), new(0, 1), new(-1, 0), new(0, -1),
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
            var parent = grid[parents[index]];
            var room = topology.Rooms[index];
            var offset = DirectionOffset(topology.Seed, room.InstanceId);

            var placed = false;
            for (var radius = 1; radius <= 9 && !placed; radius++)
            {
                for (var attempt = 0; attempt < PlacementDirections.Length; attempt++)
                {
                    var direction =
                        PlacementDirections[(offset + attempt) % PlacementDirections.Length];
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

        var spatialRooms = new List<UnderworldDungeonSpatialRoom>(topology.Rooms.Count);
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

            var y = -Math.Min(room.Depth, 12) * 2.75d;
            spatialRooms.Add(new UnderworldDungeonSpatialRoom(
                room.InstanceId,
                room.RoomFamilyId,
                point.X,
                point.Z,
                point.X * cellSize,
                y,
                point.Z * cellSize,
                yaw));
        }

        var roomById = spatialRooms.ToDictionary(
            value => value.InstanceId,
            StringComparer.Ordinal);
        var spatialConnections = new List<UnderworldDungeonSpatialConnection>(
            topology.Connections.Count);
        for (var index = 0; index < topology.Connections.Count; index++)
        {
            var connection = topology.Connections[index];
            if (!roomById.TryGetValue(connection.FromInstanceId, out var from) ||
                !roomById.TryGetValue(connection.ToInstanceId, out var to))
                throw new InvalidOperationException(
                    "Biome dungeon connection references an unknown spatial room.");

            var route = Route(
                new GridPoint(from.GridX, from.GridZ),
                new GridPoint(to.GridX, to.GridZ),
                occupied,
                topology.Seed,
                index);
            var waypoints = new List<UnderworldDungeonSpatialWaypoint>(route.Count);
            for (var routeIndex = 0; routeIndex < route.Count; routeIndex++)
            {
                var point = route[routeIndex];
                var t = route.Count <= 1
                    ? 0d
                    : routeIndex / (double)(route.Count - 1);
                var y = from.Y + (to.Y - from.Y) * t;
                waypoints.Add(new UnderworldDungeonSpatialWaypoint(
                    point.X,
                    point.Z,
                    point.X * cellSize,
                    y,
                    point.Z * cellSize));
            }

            spatialConnections.Add(new UnderworldDungeonSpatialConnection(
                connection.FromInstanceId,
                connection.ToInstanceId,
                connection.IsLoop,
                waypoints.AsReadOnly()));
        }

        var result = new UnderworldBiomeDungeonSpatialPlan(
            topology.DungeonId,
            topology.Seed,
            cellSize,
            spatialRooms.AsReadOnly(),
            spatialConnections.AsReadOnly());
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

        var roomsById = plan.Rooms.ToDictionary(value => value.InstanceId, StringComparer.Ordinal);
        var occupied = new Dictionary<GridPoint, UnderworldDungeonSpatialRoom>();
        for (var left = 0; left < plan.Rooms.Count; left++)
        {
            var a = plan.Rooms[left];
            if (!definitions.TryGetValue(a.RoomFamilyId, out var aDef))
                throw new InvalidOperationException(
                    $"Spatial room '{a.InstanceId}' has no room definition.");
            if (!Finite(a.X) || !Finite(a.Y) || !Finite(a.Z) || !Finite(a.YawDegrees))
                throw new InvalidOperationException(
                    $"Spatial room '{a.InstanceId}' contains non-finite coordinates.");

            var key = new GridPoint(a.GridX, a.GridZ);
            if (occupied.ContainsKey(key))
                throw new InvalidOperationException(
                    $"Spatial room grid cell {a.GridX},{a.GridZ} is occupied twice.");
            occupied.Add(key, a);

            for (var right = left + 1; right < plan.Rooms.Count; right++)
            {
                var b = plan.Rooms[right];
                if (!definitions.TryGetValue(b.RoomFamilyId, out var bDef))
                    throw new InvalidOperationException(
                        $"Spatial room '{b.InstanceId}' has no room definition.");

                var dx = a.X - b.X;
                var dz = a.Z - b.Z;
                var centreDistance = Math.Sqrt(dx * dx + dz * dz);
                var aRadius = Math.Sqrt(
                    aDef.WidthMeters * aDef.WidthMeters +
                    aDef.DepthMeters * aDef.DepthMeters) * .5d;
                var bRadius = Math.Sqrt(
                    bDef.WidthMeters * bDef.WidthMeters +
                    bDef.DepthMeters * bDef.DepthMeters) * .5d;
                if (centreDistance + .001d < aRadius + bRadius + 6d)
                    throw new InvalidOperationException(
                        $"Spatial dungeon rooms '{a.InstanceId}' and '{b.InstanceId}' overlap their authored envelopes.");
            }
        }

        foreach (var connection in plan.Connections)
        {
            if (!roomsById.TryGetValue(connection.FromInstanceId, out var from) ||
                !roomsById.TryGetValue(connection.ToInstanceId, out var to))
                throw new InvalidOperationException(
                    "Spatial corridor references an unknown room.");
            if (connection.Waypoints is null || connection.Waypoints.Count < 2)
                throw new InvalidOperationException(
                    "Spatial corridor must contain at least start and end waypoints.");

            var first = connection.Waypoints[0];
            var last = connection.Waypoints[connection.Waypoints.Count - 1];
            if (first.GridX != from.GridX || first.GridZ != from.GridZ ||
                last.GridX != to.GridX || last.GridZ != to.GridZ)
                throw new InvalidOperationException(
                    "Spatial corridor endpoints do not match their rooms.");

            for (var index = 0; index < connection.Waypoints.Count; index++)
            {
                var waypoint = connection.Waypoints[index];
                if (!Finite(waypoint.X) || !Finite(waypoint.Y) || !Finite(waypoint.Z))
                    throw new InvalidOperationException(
                        "Spatial corridor contains non-finite coordinates.");
                if (index == 0 || index == connection.Waypoints.Count - 1) continue;

                var key = new GridPoint(waypoint.GridX, waypoint.GridZ);
                if (occupied.TryGetValue(key, out var blocking))
                    throw new InvalidOperationException(
                        $"Spatial corridor {connection.FromInstanceId}->{connection.ToInstanceId} " +
                        $"passes through room '{blocking.InstanceId}'.");
            }
        }
    }

    private static IReadOnlyList<GridPoint> Route(
        GridPoint start,
        GridPoint end,
        IReadOnlyCollection<GridPoint> occupiedRooms,
        int seed,
        int connectionIndex)
    {
        if (start.Equals(end))
            throw new InvalidOperationException(
                "Biome dungeon corridor cannot route a room to itself.");

        var blocked = new HashSet<GridPoint>(occupiedRooms);
        blocked.Remove(start);
        blocked.Remove(end);

        var minX = Math.Min(start.X, end.X);
        var maxX = Math.Max(start.X, end.X);
        var minZ = Math.Min(start.Z, end.Z);
        var maxZ = Math.Max(start.Z, end.Z);
        foreach (var point in occupiedRooms)
        {
            minX = Math.Min(minX, point.X);
            maxX = Math.Max(maxX, point.X);
            minZ = Math.Min(minZ, point.Z);
            maxZ = Math.Max(maxZ, point.Z);
        }

        for (var expansion = 4; expansion <= 20; expansion += 4)
        {
            var route = RouteWithin(
                start,
                end,
                blocked,
                minX - expansion,
                maxX + expansion,
                minZ - expansion,
                maxZ + expansion,
                seed,
                connectionIndex);
            if (route is not null)
                return route.AsReadOnly();
        }

        throw new InvalidOperationException(
            $"Could not route biome-dungeon corridor from {start.X},{start.Z} to {end.X},{end.Z} around occupied rooms.");
    }

    private static List<GridPoint>? RouteWithin(
        GridPoint start,
        GridPoint end,
        ISet<GridPoint> blocked,
        int minX,
        int maxX,
        int minZ,
        int maxZ,
        int seed,
        int connectionIndex)
    {
        var queue = new Queue<GridPoint>();
        var previous = new Dictionary<GridPoint, GridPoint>();
        var visited = new HashSet<GridPoint> { start };
        queue.Enqueue(start);
        var offset = DirectionOffset(seed ^ (connectionIndex * 486187739), $"{end.X},{end.Z}") % RouteDirections.Length;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current.Equals(end))
            {
                var path = new List<GridPoint> { end };
                while (!path[path.Count - 1].Equals(start))
                    path.Add(previous[path[path.Count - 1]]);
                path.Reverse();
                return path;
            }

            for (var index = 0; index < RouteDirections.Length; index++)
            {
                var direction = RouteDirections[(offset + index) % RouteDirections.Length];
                var next = new GridPoint(
                    current.X + direction.X,
                    current.Z + direction.Z);
                if (next.X < minX || next.X > maxX ||
                    next.Z < minZ || next.Z > maxZ ||
                    blocked.Contains(next) ||
                    !visited.Add(next))
                    continue;
                previous[next] = current;
                queue.Enqueue(next);
            }
        }

        return null;
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
            return (int)(hash % (uint)PlacementDirections.Length);
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
