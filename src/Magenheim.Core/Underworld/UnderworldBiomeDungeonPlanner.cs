using System;
using System.Collections.Generic;
using System.Linq;

namespace Magenheim.Core.Underworld;

public sealed record UnderworldDungeonRoomPlacement(
    string InstanceId,
    string RoomFamilyId,
    int FamilyUseIndex,
    int Depth,
    int Branch);

public sealed record UnderworldDungeonConnection(
    string FromInstanceId,
    string ToInstanceId,
    bool IsLoop);

public sealed record UnderworldBiomeDungeonPlan(
    string DungeonId,
    int Seed,
    IReadOnlyList<UnderworldDungeonRoomPlacement> Rooms,
    IReadOnlyList<UnderworldDungeonConnection> Connections);

/// <summary>
/// Deterministic topology authority for the ordinary biome-dungeon program. These dungeons use
/// 15-20 large authored room families and reuse each family 2-3 times in a run. Deep Fracture is
/// intentionally excluded because its existing exact-plan expedition owns a different topology.
/// </summary>
public static class UnderworldBiomeDungeonPlanner
{
    public static UnderworldBiomeDungeonPlan Build(
        UnderworldDungeonDefinition definition,
        int seed,
        IEnumerable<string> roomFamilyIds,
        string? rootFamilyId = null)
    {
        if (definition is null) throw new ArgumentNullException(nameof(definition));
        if (roomFamilyIds is null) throw new ArgumentNullException(nameof(roomFamilyIds));
        definition.Validate();

        if (string.Equals(
                definition.Id,
                UnderworldDungeonCatalog.DeepFracture.Id,
                StringComparison.Ordinal))
            throw new InvalidOperationException(
                "Deep Fracture uses its established exact-plan expedition, not the generic biome-dungeon planner.");

        var families = roomFamilyIds
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();
        if (families.Length < definition.TargetRoomFamilyMinimum ||
            families.Length > definition.TargetRoomFamilyMaximum)
            throw new InvalidOperationException(
                $"{definition.DisplayName} requires {definition.TargetRoomFamilyMinimum}-" +
                $"{definition.TargetRoomFamilyMaximum} authored room families; received {families.Length}.");
        if (families.Distinct(StringComparer.Ordinal).Count() != families.Length)
            throw new InvalidOperationException("Biome dungeon room-family identities must be unique.");
        if (!string.IsNullOrWhiteSpace(rootFamilyId) &&
            !families.Contains(rootFamilyId, StringComparer.Ordinal))
            throw new InvalidOperationException(
                $"Required dungeon root family '{rootFamilyId}' is not present in the authored room kit.");

        var random = new Sequence(seed ^ StableHash(definition.Id));
        var pool = new List<RoomToken>();
        foreach (var family in families.OrderBy(value => value, StringComparer.Ordinal))
        {
            var uses = definition.MinimumRoomFamilyUses +
                       random.NextInt(
                           definition.MaximumRoomFamilyUses -
                           definition.MinimumRoomFamilyUses + 1);
            for (var use = 1; use <= uses; use++)
                pool.Add(new RoomToken(family, use));
        }

        if (!string.IsNullOrWhiteSpace(rootFamilyId))
        {
            var rootIndex = pool.FindIndex(token =>
                string.Equals(token.Family, rootFamilyId, StringComparison.Ordinal) &&
                token.Use == 1);
            if (rootIndex < 0)
                throw new InvalidOperationException(
                    $"Dungeon root family '{rootFamilyId}' did not produce its canonical first use.");
            var root = pool[rootIndex];
            pool.RemoveAt(rootIndex);
            Shuffle(pool, random);
            pool.Insert(0, root);
        }
        else
        {
            Shuffle(pool, random);
        }

        var rooms = new List<UnderworldDungeonRoomPlacement>(pool.Count);
        var connections = new List<UnderworldDungeonConnection>(pool.Count + pool.Count / 8);
        var children = new List<int>(pool.Count);
        var nextBranch = 1;

        for (var index = 0; index < pool.Count; index++)
        {
            var token = pool[index];
            if (index == 0)
            {
                rooms.Add(Room(definition, seed, index, token, depth: 0, branch: 0));
                children.Add(0);
                continue;
            }

            var parentIndex = SelectParent(rooms, children, random);
            var parent = rooms[parentIndex];
            var branch = parent.Branch;
            if (children[parentIndex] > 0 || parentIndex == 0)
                branch = nextBranch++;

            var room = Room(
                definition,
                seed,
                index,
                token,
                parent.Depth + 1,
                branch);
            rooms.Add(room);
            children.Add(0);
            children[parentIndex]++;

            connections.Add(new UnderworldDungeonConnection(
                parent.InstanceId,
                room.InstanceId,
                IsLoop: false));

            // Periodic cross-links keep the topology from reading as a pure tree. Restrict loop
            // candidates to already-placed rooms at similar depth so loops remain navigable rather
            // than becoming entrance-to-end shortcuts.
            if (index >= 8 && index % 7 == 0)
            {
                var loopIndex = SelectLoopTarget(
                    rooms,
                    parentIndex,
                    index,
                    random);
                if (loopIndex >= 0)
                    connections.Add(new UnderworldDungeonConnection(
                        rooms[loopIndex].InstanceId,
                        room.InstanceId,
                        IsLoop: true));
            }
        }

        var plan = new UnderworldBiomeDungeonPlan(
            definition.Id,
            seed,
            rooms.AsReadOnly(),
            connections.AsReadOnly());
        ValidatePlan(definition, families, plan);
        return plan;
    }

    public static void ValidatePlan(
        UnderworldDungeonDefinition definition,
        IReadOnlyCollection<string> roomFamilies,
        UnderworldBiomeDungeonPlan plan)
    {
        if (definition is null) throw new ArgumentNullException(nameof(definition));
        if (roomFamilies is null) throw new ArgumentNullException(nameof(roomFamilies));
        if (plan is null) throw new ArgumentNullException(nameof(plan));

        if (!string.Equals(plan.DungeonId, definition.Id, StringComparison.Ordinal))
            throw new InvalidOperationException("Biome dungeon plan id does not match its definition.");
        if (plan.Rooms.Count == 0)
            throw new InvalidOperationException("Biome dungeon plan cannot be empty.");
        if (plan.Rooms.Select(value => value.InstanceId).Distinct(StringComparer.Ordinal).Count() !=
            plan.Rooms.Count)
            throw new InvalidOperationException("Biome dungeon room instance ids must be unique.");

        var roomIds = new HashSet<string>(
            plan.Rooms.Select(value => value.InstanceId),
            StringComparer.Ordinal);
        foreach (var connection in plan.Connections)
        {
            if (!roomIds.Contains(connection.FromInstanceId) ||
                !roomIds.Contains(connection.ToInstanceId) ||
                string.Equals(
                    connection.FromInstanceId,
                    connection.ToInstanceId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Biome dungeon connection references an invalid room endpoint.");
        }

        // Every room after the root must have a non-loop parent edge.
        var parented = new HashSet<string>(StringComparer.Ordinal);
        foreach (var connection in plan.Connections)
            if (!connection.IsLoop)
                parented.Add(connection.ToInstanceId);
        for (var index = 1; index < plan.Rooms.Count; index++)
            if (!parented.Contains(plan.Rooms[index].InstanceId))
                throw new InvalidOperationException(
                    "Biome dungeon plan is disconnected from its entrance.");

        foreach (var family in roomFamilies)
        {
            var uses = plan.Rooms.Count(room =>
                string.Equals(room.RoomFamilyId, family, StringComparison.Ordinal));
            if (uses < definition.MinimumRoomFamilyUses ||
                uses > definition.MaximumRoomFamilyUses)
                throw new InvalidOperationException(
                    $"Room family '{family}' is used {uses} times outside the allowed " +
                    $"{definition.MinimumRoomFamilyUses}-{definition.MaximumRoomFamilyUses} range.");
        }

        var unknown = plan.Rooms
            .Select(value => value.RoomFamilyId)
            .Except(roomFamilies, StringComparer.Ordinal)
            .FirstOrDefault();
        if (unknown is not null)
            throw new InvalidOperationException(
                $"Biome dungeon plan contains unknown room family '{unknown}'.");
    }

    private static UnderworldDungeonRoomPlacement Room(
        UnderworldDungeonDefinition definition,
        int seed,
        int index,
        RoomToken token,
        int depth,
        int branch) =>
        new(
            InstanceId:
                definition.Id + "." + unchecked((uint)seed).ToString("x8") +
                "." + (index + 1).ToString("00"),
            RoomFamilyId: token.Family,
            FamilyUseIndex: token.Use,
            Depth: depth,
            Branch: branch);

    private static int SelectParent(
        IReadOnlyList<UnderworldDungeonRoomPlacement> rooms,
        IReadOnlyList<int> children,
        Sequence random)
    {
        var candidates = new List<int>();
        var start = Math.Max(0, rooms.Count - 10);
        for (var index = start; index < rooms.Count; index++)
            if (children[index] < 3)
                candidates.Add(index);

        if (candidates.Count == 0)
            for (var index = 0; index < rooms.Count; index++)
                if (children[index] < 3)
                    candidates.Add(index);

        if (candidates.Count == 0)
            throw new InvalidOperationException(
                "Biome dungeon topology exhausted all parent capacity.");

        // Favor the latter half of the frontier so the dungeon progresses outward, but retain
        // enough older candidates to create side branches.
        var lower = candidates.Count / 3;
        return candidates[lower + random.NextInt(candidates.Count - lower)];
    }

    private static int SelectLoopTarget(
        IReadOnlyList<UnderworldDungeonRoomPlacement> rooms,
        int parentIndex,
        int currentIndex,
        Sequence random)
    {
        var currentDepth = rooms[currentIndex].Depth;
        var candidates = new List<int>();
        for (var index = 1; index < currentIndex - 2; index++)
        {
            if (index == parentIndex) continue;
            var difference = Math.Abs(rooms[index].Depth - currentDepth);
            if (difference <= 3)
                candidates.Add(index);
        }

        return candidates.Count == 0
            ? -1
            : candidates[random.NextInt(candidates.Count)];
    }

    private static void Shuffle<T>(IList<T> values, Sequence random)
    {
        for (var index = values.Count - 1; index > 0; index--)
        {
            var target = random.NextInt(index + 1);
            var value = values[index];
            values[index] = values[target];
            values[target] = value;
        }
    }

    private static int StableHash(string value)
    {
        unchecked
        {
            uint hash = 2166136261u;
            foreach (var character in value)
            {
                hash ^= character;
                hash *= 16777619u;
            }
            return (int)hash;
        }
    }

    private readonly struct RoomToken
    {
        internal RoomToken(string family, int use)
        {
            Family = family;
            Use = use;
        }

        internal string Family { get; }
        internal int Use { get; }
    }

    private sealed class Sequence
    {
        private uint _state;

        internal Sequence(int seed)
        {
            _state = unchecked((uint)seed) ^ 0x9E3779B9u;
            if (_state == 0u) _state = 0xA341316Cu;
        }

        internal int NextInt(int exclusiveMaximum)
        {
            if (exclusiveMaximum <= 0)
                throw new ArgumentOutOfRangeException(nameof(exclusiveMaximum));
            return (int)(NextUInt() % (uint)exclusiveMaximum);
        }

        private uint NextUInt()
        {
            var value = _state;
            value ^= value << 13;
            value ^= value >> 17;
            value ^= value << 5;
            _state = value;
            return value;
        }
    }
}
