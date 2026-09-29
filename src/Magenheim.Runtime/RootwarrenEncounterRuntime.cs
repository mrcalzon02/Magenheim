using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Managers;
using HarmonyLib;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

internal sealed class RootwarrenEncounterAuthority : MonoBehaviour
{
    private const string ClearedPrefix = "magenheim.rootwarren.cleared.";
    private const string HarvestedPrefix = "magenheim.rootwarren.harvested.";
    private ZNetView _view = null!;

    private void Awake() => _view = GetComponent<ZNetView>();

    internal bool HasAuthority =>
        _view is not null &&
        _view.IsValid() &&
        _view.IsOwner() &&
        ZNet.instance is not null &&
        ZNet.instance.IsServer();

    internal bool IsCleared(string identity)
    {
        if (string.IsNullOrWhiteSpace(identity))
            throw new ArgumentException("Rootwarren encounter identity is required.", nameof(identity));
        return _view is not null &&
               _view.IsValid() &&
               _view.GetZDO().GetBool(ClearedPrefix + identity, false);
    }

    internal void MarkCleared(string identity)
    {
        if (!HasAuthority) return;
        if (string.IsNullOrWhiteSpace(identity))
            throw new ArgumentException("Rootwarren encounter identity is required.", nameof(identity));
        _view.GetZDO().Set(ClearedPrefix + identity, true);
    }

    internal bool IsHarvested(string identity)
    {
        if (string.IsNullOrWhiteSpace(identity))
            throw new ArgumentException("Rootwarren resource identity is required.", nameof(identity));
        return _view is not null &&
               _view.IsValid() &&
               _view.GetZDO().GetBool(HarvestedPrefix + identity, false);
    }

    internal void MarkHarvested(string identity)
    {
        if (!HasAuthority) return;
        if (string.IsNullOrWhiteSpace(identity))
            throw new ArgumentException("Rootwarren resource identity is required.", nameof(identity));
        _view.GetZDO().Set(HarvestedPrefix + identity, true);
    }
}

internal sealed class RootwarrenEncounterDeathTracker : MonoBehaviour
{
    private Character _character = null!;
    private RootwarrenEncounterAuthority _authority = null!;
    private string _identity = string.Empty;
    private bool _recorded;

    internal void Bind(
        Character character,
        RootwarrenEncounterAuthority authority,
        string identity)
    {
        _character = character ?? throw new ArgumentNullException(nameof(character));
        _authority = authority ?? throw new ArgumentNullException(nameof(authority));
        _identity = string.IsNullOrWhiteSpace(identity)
            ? throw new ArgumentException("Rootwarren encounter identity is required.", nameof(identity))
            : identity;
    }

    private void Update()
    {
        if (_recorded || _character is null || !_character.IsDead()) return;
        _authority.MarkCleared(_identity);
        if (_authority.HasAuthority) _recorded = true;
    }
}

internal sealed class RootwarrenResourceTracker : MonoBehaviour
{
    private RootwarrenEncounterAuthority _authority = null!;
    private string _identity = string.Empty;

    internal void Bind(RootwarrenEncounterAuthority authority, string identity)
    {
        _authority = authority ?? throw new ArgumentNullException(nameof(authority));
        _identity = string.IsNullOrWhiteSpace(identity)
            ? throw new ArgumentException("Rootwarren resource identity is required.", nameof(identity))
            : identity;
    }

    internal void RecordHarvest()
    {
        if (_authority is null || string.IsNullOrWhiteSpace(_identity)) return;
        _authority.MarkHarvested(_identity);
    }
}

[HarmonyPatch(typeof(Pickable), "RPC_Pick")]
internal static class RootwarrenPickablePersistencePatch
{
    [HarmonyPrefix]
    private static void Prefix(Pickable __instance)
    {
        if (__instance is null) return;
        __instance.GetComponent<RootwarrenResourceTracker>()?.RecordHarvest();
    }
}

internal static class RootwarrenEncounterSpawner
{
    private const string SpawnIdentityKey = "magenheim.rootwarren.spawn.id";
    private const string ResourceIdentityKey = "magenheim.rootwarren.resource.id";

    internal static void Populate(
        GameObject room,
        UnderworldDungeonRoomPlacement placement,
        UnderworldDungeonRoomDefinition definition,
        RootwarrenEncounterAuthority authority,
        int locationSeed)
    {
        if (room is null) throw new ArgumentNullException(nameof(room));
        if (placement is null) throw new ArgumentNullException(nameof(placement));
        if (definition is null) throw new ArgumentNullException(nameof(definition));
        if (authority is null) throw new ArgumentNullException(nameof(authority));
        if (!authority.HasAuthority) return;

        var roster = EncounterRoster(definition);
        for (var unitIndex = 0; unitIndex < roster.Count; unitIndex++)
            SpawnCreature(
                room,
                placement,
                definition,
                authority,
                locationSeed,
                roster[unitIndex],
                unitIndex);

        if (definition.Role == UnderworldDungeonRoomRole.Resource)
            PopulateResources(room, placement, definition, authority, locationSeed);
    }

    private static IReadOnlyList<string> EncounterRoster(
        UnderworldDungeonRoomDefinition definition)
    {
        var suffix = definition.Id.Substring(
            UnderworldFungalRootwarrenCatalog.RoomIdPrefix.Length);
        switch (suffix)
        {
            case "crawler-nest":
                return new[] { "Capcrawler", "Capcrawler", "Capcrawler", "Sporeling", "Sporeling" };
            case "stalker-den":
                return new[] { "Mycelial Stalker", "Mycelial Stalker", "Shelf Lurker" };
            case "puffback-graze":
                return new[] { "Puffback" };
            case "heartcap-sanctum":
                return new[] { "Crowncap Brute", "Sporeling", "Sporeling", "Capcrawler" };
        }

        if (definition.Role == UnderworldDungeonRoomRole.Hazard)
            return new[] { "Capcrawler", "Sporeling" };
        if (definition.Role == UnderworldDungeonRoomRole.Encounter)
            return new[] { "Capcrawler", "Mycelial Stalker" };
        if (definition.Role == UnderworldDungeonRoomRole.Landmark)
            return new[] { "Sporeling" };
        return Array.Empty<string>();
    }

    private static void SpawnCreature(
        GameObject room,
        UnderworldDungeonRoomPlacement placement,
        UnderworldDungeonRoomDefinition definition,
        RootwarrenEncounterAuthority authority,
        int locationSeed,
        string creatureName,
        int unitIndex)
    {
        var entry = UnderworldCreaturePrototypes.All.FirstOrDefault(value =>
            string.Equals(value.Name, creatureName, StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"Rootwarren encounter requires unknown Underworld creature '{creatureName}'.");
        var source = PrefabManager.Instance.GetPrefab(entry.Prefab)
            ?? throw new InvalidOperationException(
                $"Rootwarren encounter requires registered creature prefab '{entry.Prefab}'.");

        var identity =
            $"{unchecked((uint)locationSeed):X8}.{placement.InstanceId}.creature.{unitIndex + 1:00}";
        if (authority.IsCleared(identity) ||
            FindLivingCreature(identity, room.scene.handle) is not null)
            return;

        var random = new System.Random(StableSeed(locationSeed, placement.InstanceId, unitIndex));
        var x = ((float)random.NextDouble() - .5f) * (float)definition.WidthMeters * .46f;
        var z = ((float)random.NextDouble() - .5f) * (float)definition.DepthMeters * .46f;
        var y = creatureName == "Lantern Moth" ? 4f : .6f;
        var position = room.transform.TransformPoint(new Vector3(x, y, z));
        var rotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
        var instance = UnityEngine.Object.Instantiate(source, position, rotation);
        instance.name = entry.Prefab + "_Rootwarren_" + unitIndex.ToString("00");

        var character = instance.GetComponent<Character>();
        var view = instance.GetComponent<ZNetView>();
        if (character is null || view is null || !view.IsValid())
        {
            UnityEngine.Object.Destroy(instance);
            throw new InvalidOperationException(
                $"Rootwarren creature '{entry.Prefab}' did not create valid Character/ZNetView authority.");
        }

        view.GetZDO().Set(SpawnIdentityKey, identity);
        var tracker = instance.GetComponent<RootwarrenEncounterDeathTracker>()
            ?? instance.AddComponent<RootwarrenEncounterDeathTracker>();
        tracker.Bind(character, authority, identity);
        instance.SetActive(true);
    }

    private static Character? FindLivingCreature(string identity, int sceneHandle)
    {
        foreach (var character in Character.GetAllCharacters())
        {
            if (character is null ||
                character.IsDead() ||
                character.gameObject.scene.handle != sceneHandle)
                continue;
            var view = character.GetComponent<ZNetView>();
            if (view is null || !view.IsValid()) continue;
            if (string.Equals(
                    view.GetZDO().GetString(SpawnIdentityKey, string.Empty),
                    identity,
                    StringComparison.Ordinal))
                return character;
        }
        return null;
    }

    private static void PopulateResources(
        GameObject room,
        UnderworldDungeonRoomPlacement placement,
        UnderworldDungeonRoomDefinition definition,
        RootwarrenEncounterAuthority authority,
        int locationSeed)
    {
        var resources = UnderworldResourceCatalog.All
            .Where(value => value.Biome == UnderworldTerrainBiome.FungalForest)
            .ToArray();
        if (resources.Length == 0)
            throw new InvalidOperationException(
                "Rootwarren has no registered Fungal Forest resource vocabulary.");

        const int nodes = 6;
        for (var index = 0; index < nodes; index++)
        {
            var resource = resources[index % resources.Length];
            var identity =
                $"{unchecked((uint)locationSeed):X8}.{placement.InstanceId}.resource.{index + 1:00}";
            if (authority.IsHarvested(identity) ||
                FindResource(identity, room.scene.handle) is not null)
                continue;

            var source = PrefabManager.Instance.GetPrefab(resource.PickupPrefab)
                ?? throw new InvalidOperationException(
                    $"Rootwarren resource room requires pickup prefab '{resource.PickupPrefab}'.");

            var random = new System.Random(
                StableSeed(locationSeed ^ 0x4A3B291D, placement.InstanceId, index));
            var x = ((float)random.NextDouble() - .5f) * (float)definition.WidthMeters * .52f;
            var z = ((float)random.NextDouble() - .5f) * (float)definition.DepthMeters * .52f;
            var position = room.transform.TransformPoint(new Vector3(x, .25f, z));
            var instance = UnityEngine.Object.Instantiate(
                source,
                position,
                Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f));
            instance.name = resource.PickupPrefab + "_Rootwarren_" + index.ToString("00");

            var view = instance.GetComponent<ZNetView>();
            var pickable = instance.GetComponent<Pickable>();
            if (view is null || !view.IsValid() || pickable is null)
            {
                UnityEngine.Object.Destroy(instance);
                throw new InvalidOperationException(
                    $"Rootwarren resource '{resource.PickupPrefab}' lacks Pickable/ZNetView authority.");
            }

            view.GetZDO().Set(ResourceIdentityKey, identity);
            var tracker = instance.GetComponent<RootwarrenResourceTracker>()
                ?? instance.AddComponent<RootwarrenResourceTracker>();
            tracker.Bind(authority, identity);
            instance.SetActive(true);
        }
    }

    private static Pickable? FindResource(string identity, int sceneHandle)
    {
        foreach (var pickable in Resources.FindObjectsOfTypeAll<Pickable>())
        {
            if (!pickable || pickable.gameObject.scene.handle != sceneHandle) continue;
            var view = pickable.GetComponent<ZNetView>();
            if (view is null || !view.IsValid()) continue;
            if (string.Equals(
                    view.GetZDO().GetString(ResourceIdentityKey, string.Empty),
                    identity,
                    StringComparison.Ordinal))
                return pickable;
        }
        return null;
    }

    private static int StableSeed(int seed, string identity, int index)
    {
        unchecked
        {
            uint hash = (uint)seed ^ (uint)(index * 486187739);
            foreach (var character in identity)
            {
                hash ^= character;
                hash *= 16777619u;
            }
            return (int)hash;
        }
    }
}
