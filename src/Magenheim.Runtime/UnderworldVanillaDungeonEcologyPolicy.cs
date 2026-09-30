using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// DDE-06 encounter pacing. Every donor combat socket is scrubbed of vanilla creature ownership,
/// but only a deterministic fraction stays active. Pressure rises by room risk instead of scaling
/// simply because the dungeon contains 3.5x more rooms.
/// </summary>
internal static class UnderworldVanillaDungeonEcologyPolicy
{
    internal readonly record struct Stats(
        int CombatSocketRooms,
        int ActiveEncounterRooms,
        int QuietCombatRooms,
        int CreatureSpawnerSockets,
        int ActiveCreatureSpawners,
        int SpawnAreaSockets,
        int ActiveSpawnAreas,
        int SwarmBindings,
        int SkirmisherBindings,
        int HunterBindings,
        int BruiserBindings,
        int HeavyBindings,
        int ApexBindings)
    {
        internal static Stats Empty => new(0,0,0,0,0,0,0,0,0,0,0,0,0);

        public static Stats operator +(Stats left, Stats right) =>
            new(
                left.CombatSocketRooms + right.CombatSocketRooms,
                left.ActiveEncounterRooms + right.ActiveEncounterRooms,
                left.QuietCombatRooms + right.QuietCombatRooms,
                left.CreatureSpawnerSockets + right.CreatureSpawnerSockets,
                left.ActiveCreatureSpawners + right.ActiveCreatureSpawners,
                left.SpawnAreaSockets + right.SpawnAreaSockets,
                left.ActiveSpawnAreas + right.ActiveSpawnAreas,
                left.SwarmBindings + right.SwarmBindings,
                left.SkirmisherBindings + right.SkirmisherBindings,
                left.HunterBindings + right.HunterBindings,
                left.BruiserBindings + right.BruiserBindings,
                left.HeavyBindings + right.HeavyBindings,
                left.ApexBindings + right.ApexBindings);
    }

    internal static Stats Rebind(
        GameObject roomObject,
        UnderworldVanillaDungeonReuseDefinition profile,
        string donorRoomName,
        int donorRoomIndex)
    {
        if (!roomObject) throw new ArgumentNullException(nameof(roomObject));
        var room = roomObject.GetComponent<Room>()
            ?? throw new InvalidOperationException("DDE ecology received a donor clone without Room.");
        var band = UnderworldVanillaDungeonRoomPolicy.RiskFor(
            room, profile.DungeonId, donorRoomName, donorRoomIndex);
        return RebindAtBand(
            roomObject,
            profile,
            donorRoomName,
            donorRoomIndex,
            band);
    }

    internal static Stats RebindAtBand(
        GameObject roomObject,
        UnderworldVanillaDungeonReuseDefinition profile,
        string donorRoomName,
        int donorRoomIndex,
        UnderworldVanillaDungeonRiskBand band)
    {
        if (!roomObject) throw new ArgumentNullException(nameof(roomObject));
        var room = roomObject.GetComponent<Room>()
            ?? throw new InvalidOperationException("DDE ecology received a donor clone without Room.");

        var creatures = EligibleCreatures(profile.Biome).ToArray();
        if (creatures.Length == 0)
            throw new InvalidOperationException(
                "No dungeon-eligible Underworld creatures exist for " + profile.Biome + ".");

        var encounterRoomActive = ShouldActivateEncounterRoom(
            room,
            band,
            profile,
            donorRoomName,
            donorRoomIndex);

        var roleCounts = new int[6];
        var spawners = roomObject.GetComponentsInChildren<CreatureSpawner>(true);
        var activeSpawners = 0;
        for (var index = 0; index < spawners.Length; index++)
        {
            var selected = SelectCreature(
                creatures, room, band, profile.DungeonId, donorRoomName, donorRoomIndex, "spawner", index);
            var prefab = PrefabManager.Instance.GetPrefab(selected.Prefab)
                ?? throw new InvalidOperationException(
                    "Underworld dungeon creature prefab '" + selected.Prefab + "' is unavailable.");

            // Remove donor ownership even from suppressed sockets. If anything later re-enables the
            // component, it still cannot spawn a Surface creature.
            var spawner = spawners[index];
            spawner.m_creaturePrefab = prefab;
            spawner.m_requiredGlobalKey = string.Empty;
            spawner.m_blockingGlobalKey = string.Empty;

            var active = encounterRoomActive && ShouldActivate(
                room, band, profile.DungeonId, donorRoomName, donorRoomIndex, "spawner-active", index);
            spawner.enabled = active;
            if (!active) continue;

            activeSpawners++;
            roleCounts[(int)UnderworldCreatureCombatBalance.RoleFor(selected.Name)]++;
        }

        // Once a room has been admitted as an encounter room, do not let every individual socket
        // miss and accidentally erase that encounter. Rooms deliberately suppressed by the room-level
        // pacing roll stay quiet: that is how a 3.5x dungeon gains traversal/resource breathing space
        // without turning into 3.5x mandatory combat.
        if (encounterRoomActive && !room.m_entrance && spawners.Length > 0 && activeSpawners == 0)
        {
            var selected = SelectCreature(
                creatures, room, band, profile.DungeonId, donorRoomName, donorRoomIndex, "spawner-fallback", 0);
            var prefab = PrefabManager.Instance.GetPrefab(selected.Prefab)
                ?? throw new InvalidOperationException(
                    "Underworld dungeon creature prefab '" + selected.Prefab + "' is unavailable.");
            spawners[0].m_creaturePrefab = prefab;
            spawners[0].enabled = true;
            activeSpawners = 1;
            roleCounts[(int)UnderworldCreatureCombatBalance.RoleFor(selected.Name)]++;
        }

        var areas = roomObject.GetComponentsInChildren<SpawnArea>(true);
        var activeAreas = 0;
        for (var areaIndex = 0; areaIndex < areas.Length; areaIndex++)
        {
            var area = areas[areaIndex];
            area.m_prefabs.Clear();

            // Populate with only biome-valid Magenheim identities even if this area is suppressed.
            var selected = SelectDistinctCreatures(
                creatures,
                room,
                band,
                profile.DungeonId,
                donorRoomName,
                donorRoomIndex,
                areaIndex,
                Math.Min(3, creatures.Length));
            foreach (var entry in selected)
            {
                var prefab = PrefabManager.Instance.GetPrefab(entry.Prefab)
                    ?? throw new InvalidOperationException(
                        "Underworld dungeon creature prefab '" + entry.Prefab + "' is unavailable.");
                area.m_prefabs.Add(new SpawnArea.SpawnData
                {
                    m_prefab = prefab,
                    m_weight = RoleWeight(UnderworldCreatureCombatBalance.RoleFor(entry.Name), band),
                    m_minLevel = 1,
                    m_maxLevel = (int)band >= (int)UnderworldVanillaDungeonRiskBand.Deep ? 2 : 1,
                });
            }

            // Larger dungeons should contain breathing room. SpawnArea loops are deliberately
            // slower and lower-cap than the donor if the donor was more aggressive.
            if (area.m_maxNear > 0) area.m_maxNear = Math.Min(area.m_maxNear, 3);
            if (area.m_maxTotal > 0) area.m_maxTotal = Math.Min(area.m_maxTotal, 6);
            area.m_spawnIntervalSec = Mathf.Max(area.m_spawnIntervalSec, 45f);

            var active = encounterRoomActive && ShouldActivate(
                room, band, profile.DungeonId, donorRoomName, donorRoomIndex, "area-active", areaIndex);
            area.enabled = active;
            if (!active) continue;

            activeAreas++;
            foreach (var entry in selected)
                roleCounts[(int)UnderworldCreatureCombatBalance.RoleFor(entry.Name)]++;
        }

        var combatSocketRoom = spawners.Length > 0 || areas.Length > 0;
        return new Stats(
            combatSocketRoom ? 1 : 0,
            combatSocketRoom && encounterRoomActive ? 1 : 0,
            combatSocketRoom && !encounterRoomActive ? 1 : 0,
            spawners.Length,
            activeSpawners,
            areas.Length,
            activeAreas,
            roleCounts[(int)UnderworldCreatureCombatBalance.Role.Swarm],
            roleCounts[(int)UnderworldCreatureCombatBalance.Role.Skirmisher],
            roleCounts[(int)UnderworldCreatureCombatBalance.Role.Hunter],
            roleCounts[(int)UnderworldCreatureCombatBalance.Role.Bruiser],
            roleCounts[(int)UnderworldCreatureCombatBalance.Role.Heavy],
            roleCounts[(int)UnderworldCreatureCombatBalance.Role.Apex]);
    }

    private static IEnumerable<UnderworldCreaturePrototypes.Entry> EligibleCreatures(
        UnderworldTerrainBiome biome)
    {
        var label = BiomeLabel(biome);
        return UnderworldCreaturePrototypes.All.Where(entry =>
        {
            if (!string.Equals(entry.Biome, label, StringComparison.Ordinal)) return false;

            // Sunken Crypt rooms are damp/wet but do not provide the deep swimming volume required
            // by Serpent-bodied fauna. Keep amphibious Neck/Leech derivatives, exclude Serpents.
            if (biome == UnderworldTerrainBiome.BlackwaterDeep &&
                string.Equals(entry.Donor, "Serpent", StringComparison.Ordinal))
                return false;

            return true;
        });
    }

    private static bool ShouldActivateEncounterRoom(
        Room room,
        UnderworldVanillaDungeonRiskBand band,
        UnderworldVanillaDungeonReuseDefinition profile,
        string roomName,
        int roomIndex)
    {
        if (room.m_entrance)
            return false;

        // Normalize room-level encounter occupancy against the enlarged expedition. Per-socket
        // activation then controls local density inside an admitted combat room. At the floor
        // multiplier (3.5x) this keeps total fight count materially above vanilla while well below
        // a naive 3.5x multiplication.
        var baseline = band switch
        {
            UnderworldVanillaDungeonRiskBand.Outer => .52d,
            UnderworldVanillaDungeonRiskBand.Mid => .64d,
            UnderworldVanillaDungeonRiskBand.Deep => .76d,
            UnderworldVanillaDungeonRiskBand.Lair => .90d,
            _ => .60d,
        };

        var multiplier = Math.Max(1d, profile.RoomCountMultiplier);
        var normalization = 1d / Math.Sqrt(multiplier);
        var probability = baseline * normalization;

        if (room.m_endCap)
            probability = Math.Min(.88d, probability + .18d);
        if (band == UnderworldVanillaDungeonRiskBand.Lair)
            probability = Math.Max(.58d, probability);

        return UnderworldVanillaDungeonRoomPolicy.Roll(
            probability,
            profile.DungeonId,
            roomName,
            roomIndex,
            "encounter-room-active");
    }

    private static bool ShouldActivate(
        Room room,
        UnderworldVanillaDungeonRiskBand band,
        string dungeonId,
        string roomName,
        int roomIndex,
        string channel,
        int socketIndex)
    {
        if (room.m_entrance) return false;

        var probability = band switch
        {
            UnderworldVanillaDungeonRiskBand.Outer => .42d,
            UnderworldVanillaDungeonRiskBand.Mid => .54d,
            UnderworldVanillaDungeonRiskBand.Deep => .64d,
            UnderworldVanillaDungeonRiskBand.Lair => .72d,
            _ => .50d,
        };

        if (room.m_endCap) probability = Math.Min(.82d, probability + .10d);
        return UnderworldVanillaDungeonRoomPolicy.Roll(
            probability, dungeonId, roomName, roomIndex, channel, socketIndex);
    }

    private static UnderworldCreaturePrototypes.Entry SelectCreature(
        IReadOnlyList<UnderworldCreaturePrototypes.Entry> creatures,
        Room room,
        UnderworldVanillaDungeonRiskBand band,
        string dungeonId,
        string roomName,
        int roomIndex,
        string channel,
        int socketIndex)
    {
        var candidates = creatures
            .Where(entry =>
            {
                var role = UnderworldCreatureCombatBalance.RoleFor(entry.Name);
                return Allowed(role, band) && FitsRoom(role, room);
            })
            .ToArray();
        if (candidates.Length == 0)
            throw new InvalidOperationException("DDE encounter policy produced no eligible creature candidates.");

        var total = candidates.Sum(entry =>
            (double)RoleWeight(UnderworldCreatureCombatBalance.RoleFor(entry.Name), band));
        var roll = UnderworldVanillaDungeonRoomPolicy.Unit(
            dungeonId, roomName, roomIndex, channel, socketIndex) * total;

        foreach (var entry in candidates)
        {
            roll -= RoleWeight(UnderworldCreatureCombatBalance.RoleFor(entry.Name), band);
            if (roll <= 0d) return entry;
        }

        return candidates[candidates.Length - 1];
    }

    private static IReadOnlyList<UnderworldCreaturePrototypes.Entry> SelectDistinctCreatures(
        IReadOnlyList<UnderworldCreaturePrototypes.Entry> creatures,
        Room room,
        UnderworldVanillaDungeonRiskBand band,
        string dungeonId,
        string roomName,
        int roomIndex,
        int areaIndex,
        int count)
    {
        var selected = new List<UnderworldCreaturePrototypes.Entry>();
        for (var index = 0; index < count; index++)
        {
            var candidate = SelectCreature(
                creatures, room, band, dungeonId, roomName, roomIndex,
                "area-choice-" + areaIndex, index);
            if (!selected.Contains(candidate))
                selected.Add(candidate);
        }

        if (selected.Count == 0)
            selected.Add(SelectCreature(
                creatures, room, band, dungeonId, roomName, roomIndex, "area-fallback", areaIndex));
        return selected;
    }

    private static bool FitsRoom(
        UnderworldCreatureCombatBalance.Role role,
        Room room)
    {
        var horizontal = Math.Min(room.m_size.x, room.m_size.z);
        var height = room.m_size.y;
        return role switch
        {
            UnderworldCreatureCombatBalance.Role.Bruiser =>
                horizontal >= 8 && height >= 5,
            UnderworldCreatureCombatBalance.Role.Heavy =>
                horizontal >= 11 && height >= 6,
            UnderworldCreatureCombatBalance.Role.Apex =>
                horizontal >= 14 && height >= 7,
            _ => true,
        };
    }

    private static bool Allowed(
        UnderworldCreatureCombatBalance.Role role,
        UnderworldVanillaDungeonRiskBand band)
    {
        return band switch
        {
            UnderworldVanillaDungeonRiskBand.Outer =>
                (int)role <= (int)UnderworldCreatureCombatBalance.Role.Skirmisher,
            UnderworldVanillaDungeonRiskBand.Mid =>
                (int)role <= (int)UnderworldCreatureCombatBalance.Role.Hunter,
            UnderworldVanillaDungeonRiskBand.Deep =>
                (int)role <= (int)UnderworldCreatureCombatBalance.Role.Heavy,
            UnderworldVanillaDungeonRiskBand.Lair => true,
            _ => false,
        };
    }

    private static float RoleWeight(
        UnderworldCreatureCombatBalance.Role role,
        UnderworldVanillaDungeonRiskBand band)
    {
        return band switch
        {
            UnderworldVanillaDungeonRiskBand.Outer => role switch
            {
                UnderworldCreatureCombatBalance.Role.Swarm => 4f,
                UnderworldCreatureCombatBalance.Role.Skirmisher => 3f,
                _ => .01f,
            },
            UnderworldVanillaDungeonRiskBand.Mid => role switch
            {
                UnderworldCreatureCombatBalance.Role.Swarm => 1.5f,
                UnderworldCreatureCombatBalance.Role.Skirmisher => 2.5f,
                UnderworldCreatureCombatBalance.Role.Hunter => 4f,
                _ => .01f,
            },
            UnderworldVanillaDungeonRiskBand.Deep => role switch
            {
                UnderworldCreatureCombatBalance.Role.Swarm => .5f,
                UnderworldCreatureCombatBalance.Role.Skirmisher => 1.5f,
                UnderworldCreatureCombatBalance.Role.Hunter => 3.5f,
                UnderworldCreatureCombatBalance.Role.Bruiser => 2.5f,
                UnderworldCreatureCombatBalance.Role.Heavy => 1f,
                _ => .01f,
            },
            UnderworldVanillaDungeonRiskBand.Lair => role switch
            {
                UnderworldCreatureCombatBalance.Role.Swarm => .2f,
                UnderworldCreatureCombatBalance.Role.Skirmisher => .5f,
                UnderworldCreatureCombatBalance.Role.Hunter => 2.5f,
                UnderworldCreatureCombatBalance.Role.Bruiser => 3f,
                UnderworldCreatureCombatBalance.Role.Heavy => 2f,
                UnderworldCreatureCombatBalance.Role.Apex => .35f,
                _ => .01f,
            },
            _ => 1f,
        };
    }

    private static string BiomeLabel(UnderworldTerrainBiome biome) => biome switch
    {
        UnderworldTerrainBiome.FungalForest => "Fungal Forest",
        UnderworldTerrainBiome.BlackwaterDeep => "Blackwater Deep",
        UnderworldTerrainBiome.SulfurousWastes => "Sulfurous Wastes",
        UnderworldTerrainBiome.FrozenCaverns => "Frozen Caverns",
        UnderworldTerrainBiome.GreatDecay => "Great Decay",
        _ => throw new InvalidOperationException(
            "Ordinary vanilla-reuse dungeon ecology is undefined for " + biome + "."),
    };
}
