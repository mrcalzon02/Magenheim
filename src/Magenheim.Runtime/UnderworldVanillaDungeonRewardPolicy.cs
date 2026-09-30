using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// DDE-07 reward authority. Common biome resources reward the longer expedition while bottleneck
/// resources are inversely normalized against the 3.5x room-count expansion.
/// </summary>
internal static class UnderworldVanillaDungeonRewardPolicy
{
    private enum Rarity
    {
        Common,
        Uncommon,
        Bottleneck,
    }

    private enum Fixture
    {
        Container,
        Pickable,
        Mineable,
        Destructible,
    }

    internal readonly record struct Stats(
        int Containers,
        int Pickables,
        int Mineables,
        int DestructibleDrops,
        int BottleneckPickables)
    {
        internal static Stats Empty => new(0,0,0,0,0);

        public static Stats operator +(Stats left, Stats right) =>
            new(
                left.Containers + right.Containers,
                left.Pickables + right.Pickables,
                left.Mineables + right.Mineables,
                left.DestructibleDrops + right.DestructibleDrops,
                left.BottleneckPickables + right.BottleneckPickables);
    }

    internal static Stats Rebind(
        GameObject roomObject,
        UnderworldVanillaDungeonReuseDefinition profile,
        string donorRoomName,
        int donorRoomIndex)
    {
        if (!roomObject) throw new ArgumentNullException(nameof(roomObject));
        var room = roomObject.GetComponent<Room>()
            ?? throw new InvalidOperationException("DDE reward policy received a donor clone without Room.");
        var band = UnderworldVanillaDungeonRoomPolicy.RiskFor(
            room, profile.DungeonId, donorRoomName, donorRoomIndex);
        var resources = UnderworldResourceCatalog.All
            .Where(resource => resource.Biome == profile.Biome)
            .ToArray();
        if (resources.Length == 0)
            throw new InvalidOperationException(
                "No canonical Underworld resources exist for " + profile.Biome + ".");

        var containers = roomObject.GetComponentsInChildren<Container>(true);
        for (var index = 0; index < containers.Length; index++)
            containers[index].m_defaultItems = ResourceTable(
                resources, profile, band, Fixture.Container, donorRoomName, donorRoomIndex, index);

        var bottleneckPickables = 0;
        var pickables = roomObject.GetComponentsInChildren<Pickable>(true);
        for (var index = 0; index < pickables.Length; index++)
        {
            var resource = DirectResource(
                resources, profile, band, donorRoomName, donorRoomIndex, index);
            var prefab = PrefabManager.Instance.GetPrefab(resource.Prefab)
                ?? throw new InvalidOperationException(
                    "Underworld resource item '" + resource.Prefab + "' is unavailable.");
            pickables[index].m_itemPrefab = prefab;
            pickables[index].m_amount = Math.Max(1, Math.Min(pickables[index].m_amount, 2));
            pickables[index].m_overrideName = resource.Name;
            if (RarityFor(resource) == Rarity.Bottleneck) bottleneckPickables++;
        }

        var mines = roomObject.GetComponentsInChildren<MineRock>(true);
        for (var index = 0; index < mines.Length; index++)
            mines[index].m_dropItems = ResourceTable(
                resources, profile, band, Fixture.Mineable, donorRoomName, donorRoomIndex, index);

        var mines5 = roomObject.GetComponentsInChildren<MineRock5>(true);
        for (var index = 0; index < mines5.Length; index++)
            mines5[index].m_dropItems = ResourceTable(
                resources, profile, band, Fixture.Mineable, donorRoomName, donorRoomIndex, index + mines.Length);

        var destroyed = roomObject.GetComponentsInChildren<DropOnDestroyed>(true);
        for (var index = 0; index < destroyed.Length; index++)
            destroyed[index].m_dropWhenDestroyed = ResourceTable(
                resources, profile, band, Fixture.Destructible, donorRoomName, donorRoomIndex, index);

        return new Stats(
            containers.Length,
            pickables.Length,
            mines.Length + mines5.Length,
            destroyed.Length,
            bottleneckPickables);
    }

    private static DropTable ResourceTable(
        IReadOnlyList<UnderworldResourceDefinition> resources,
        UnderworldVanillaDungeonReuseDefinition profile,
        UnderworldVanillaDungeonRiskBand band,
        Fixture fixture,
        string roomName,
        int roomIndex,
        int fixtureIndex)
    {
        var table = new DropTable
        {
            m_dropChance = 1f,
            m_oneOfEach = false,
        };

        switch (fixture)
        {
            case Fixture.Container:
                table.m_dropMin = 1;
                table.m_dropMax = (int)band >= (int)UnderworldVanillaDungeonRiskBand.Deep ? 3 : 2;
                break;
            case Fixture.Mineable:
                table.m_dropMin = 1;
                table.m_dropMax = 2;
                break;
            case Fixture.Destructible:
                table.m_dropMin = 1;
                table.m_dropMax = 1;
                break;
            default:
                table.m_dropMin = 1;
                table.m_dropMax = 1;
                break;
        }

        foreach (var resource in resources)
        {
            var rarity = RarityFor(resource);
            var weight = Weight(rarity, profile.RoomCountMultiplier, band, fixture);
            if (weight <= 0f) continue;

            var prefab = PrefabManager.Instance.GetPrefab(resource.Prefab)
                ?? throw new InvalidOperationException(
                    "Underworld resource item '" + resource.Prefab + "' is unavailable.");
            table.m_drops.Add(new DropTable.DropData
            {
                m_item = prefab,
                m_stackMin = 1,
                m_stackMax = rarity == Rarity.Common && fixture != Fixture.Destructible ? 2 : 1,
                m_weight = weight,
                m_dontScale = false,
            });
        }

        if (table.m_drops.Count == 0)
            throw new InvalidOperationException(
                "DDE reward policy produced an empty resource table for " + profile.Biome + ".");

        return table;
    }

    private static UnderworldResourceDefinition DirectResource(
        IReadOnlyList<UnderworldResourceDefinition> resources,
        UnderworldVanillaDungeonReuseDefinition profile,
        UnderworldVanillaDungeonRiskBand band,
        string roomName,
        int roomIndex,
        int fixtureIndex)
    {
        var bottlenecks = resources.Where(resource => RarityFor(resource) == Rarity.Bottleneck).ToArray();
        var common = resources.Where(resource => RarityFor(resource) == Rarity.Common).ToArray();
        var uncommon = resources.Where(resource => RarityFor(resource) == Rarity.Uncommon).ToArray();

        if (bottlenecks.Length > 0)
        {
            // If vanilla-sized content gave each resource an equal chance, inverse-normalize that
            // bottleneck share by the room-count multiplier. Deeper rooms may improve it modestly,
            // but 3.5x more rooms still cannot become 3.5x rare-material output.
            var baselineShare = bottlenecks.Length / (double)resources.Count;
            var probability = baselineShare / profile.RoomCountMultiplier * RiskFactor(band);
            if (UnderworldVanillaDungeonRoomPolicy.Roll(
                probability, profile.DungeonId, roomName, roomIndex, "reward-bottleneck", fixtureIndex))
                return bottlenecks[Index(
                    bottlenecks.Length, profile.DungeonId, roomName, roomIndex, "reward-bottleneck-choice", fixtureIndex)];
        }

        if (uncommon.Length > 0 &&
            UnderworldVanillaDungeonRoomPolicy.Roll(
                .22d * RiskFactor(band),
                profile.DungeonId, roomName, roomIndex, "reward-uncommon", fixtureIndex))
            return uncommon[Index(
                uncommon.Length, profile.DungeonId, roomName, roomIndex, "reward-uncommon-choice", fixtureIndex)];

        var pool = common.Length > 0 ? common : resources.ToArray();
        return pool[Index(
            pool.Length, profile.DungeonId, roomName, roomIndex, "reward-common-choice", fixtureIndex)];
    }

    private static float Weight(
        Rarity rarity,
        double roomCountMultiplier,
        UnderworldVanillaDungeonRiskBand band,
        Fixture fixture)
    {
        if (fixture == Fixture.Destructible && rarity == Rarity.Bottleneck)
            return 0f;

        var risk = (float)RiskFactor(band);
        return rarity switch
        {
            Rarity.Common => 1f,
            Rarity.Uncommon => .55f * Math.Min(1.25f, risk),
            Rarity.Bottleneck => (float)(1d / roomCountMultiplier) * risk,
            _ => 1f,
        };
    }

    private static double RiskFactor(UnderworldVanillaDungeonRiskBand band) => band switch
    {
        UnderworldVanillaDungeonRiskBand.Outer => .65d,
        UnderworldVanillaDungeonRiskBand.Mid => .90d,
        UnderworldVanillaDungeonRiskBand.Deep => 1.20d,
        UnderworldVanillaDungeonRiskBand.Lair => 1.50d,
        _ => 1d,
    };

    private static Rarity RarityFor(UnderworldResourceDefinition resource)
    {
        return resource.Prefab switch
        {
            "Magenheim_Underworld_Resource_BlackwaterPearl" => Rarity.Bottleneck,
            "Magenheim_Underworld_Resource_Emberiron" => Rarity.Bottleneck,
            "Magenheim_Underworld_Resource_Rimesilver" => Rarity.Bottleneck,
            "Magenheim_Underworld_Resource_CarrionAmber" => Rarity.Bottleneck,
            "Magenheim_Underworld_Resource_Understone" => Rarity.Uncommon,
            "Magenheim_Underworld_Resource_DeepSalt" => Rarity.Uncommon,
            "Magenheim_Underworld_Resource_Sulfur" => Rarity.Uncommon,
            "Magenheim_Underworld_Resource_ClearIce" => Rarity.Uncommon,
            "Magenheim_Underworld_Resource_BoneGravel" => Rarity.Uncommon,
            _ => Rarity.Common,
        };
    }

    private static int Index(int count, params object[] values)
    {
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
        return Math.Min(count - 1, (int)(UnderworldVanillaDungeonRoomPolicy.Unit(values) * count));
    }
}
