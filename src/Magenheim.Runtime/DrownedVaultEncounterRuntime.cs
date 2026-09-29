using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

internal sealed class DrownedVaultEncounterAuthority : MonoBehaviour
{
    private const string ClearedPrefix = "magenheim.drownedvaults.cleared.";
    private const string HarvestedPrefix = "magenheim.drownedvaults.harvested.";
    private ZNetView _view = null!;

    private void Awake() => _view = GetComponent<ZNetView>();

    internal bool HasAuthority =>
        _view is not null &&
        _view.IsValid() &&
        _view.IsOwner() &&
        ZNet.instance is not null &&
        ZNet.instance.IsServer();

    internal bool IsCleared(string identity) =>
        Read(ClearedPrefix, identity, "encounter");

    internal void MarkCleared(string identity) =>
        Write(ClearedPrefix, identity, "encounter");

    internal bool IsHarvested(string identity) =>
        Read(HarvestedPrefix, identity, "resource");

    internal void MarkHarvested(string identity) =>
        Write(HarvestedPrefix, identity, "resource");

    private bool Read(string prefix, string identity, string kind)
    {
        if (string.IsNullOrWhiteSpace(identity))
            throw new ArgumentException($"Drowned Vault {kind} identity is required.", nameof(identity));
        return _view is not null &&
               _view.IsValid() &&
               _view.GetZDO().GetBool(prefix + identity, false);
    }

    private void Write(string prefix, string identity, string kind)
    {
        if (!HasAuthority) return;
        if (string.IsNullOrWhiteSpace(identity))
            throw new ArgumentException($"Drowned Vault {kind} identity is required.", nameof(identity));
        _view.GetZDO().Set(prefix + identity, true);
    }
}

internal sealed class DrownedVaultEncounterDeathTracker : MonoBehaviour
{
    private Character _character = null!;
    private DrownedVaultEncounterAuthority _authority = null!;
    private string _identity = string.Empty;
    private bool _recorded;

    internal void Bind(
        Character character,
        DrownedVaultEncounterAuthority authority,
        string identity)
    {
        _character = character ?? throw new ArgumentNullException(nameof(character));
        _authority = authority ?? throw new ArgumentNullException(nameof(authority));
        _identity = string.IsNullOrWhiteSpace(identity)
            ? throw new ArgumentException("Drowned Vault encounter identity is required.", nameof(identity))
            : identity;
    }

    private void Update()
    {
        if (_recorded || _character is null || !_character.IsDead()) return;
        _authority.MarkCleared(_identity);
        if (_authority.HasAuthority) _recorded = true;
    }
}

internal sealed class DrownedVaultResourceTracker : MonoBehaviour
{
    private DrownedVaultEncounterAuthority _authority = null!;
    private string _identity = string.Empty;

    internal void Bind(DrownedVaultEncounterAuthority authority, string identity)
    {
        _authority = authority ?? throw new ArgumentNullException(nameof(authority));
        _identity = string.IsNullOrWhiteSpace(identity)
            ? throw new ArgumentException("Drowned Vault resource identity is required.", nameof(identity))
            : identity;
    }

    internal void RecordHarvest()
    {
        if (_authority is null || string.IsNullOrWhiteSpace(_identity)) return;
        _authority.MarkHarvested(_identity);
    }
}

[HarmonyPatch(typeof(Pickable), "RPC_Pick")]
internal static class DrownedVaultPickablePersistencePatch
{
    [HarmonyPrefix]
    private static void Prefix(Pickable __instance)
    {
        if (__instance is null) return;
        __instance.GetComponent<DrownedVaultResourceTracker>()?.RecordHarvest();
    }
}

internal static class DrownedVaultEncounterSpawner
{
    private const string SpawnIdentityKey = "magenheim.drownedvaults.spawn.id";
    private const string ResourceIdentityKey = "magenheim.drownedvaults.resource.id";

    internal static void Populate(
        GameObject room,
        UnderworldDungeonRoomPlacement placement,
        UnderworldDrownedVaultRoomDefinition definition,
        DrownedVaultEncounterAuthority authority,
        int locationSeed)
    {
        if (room is null) throw new ArgumentNullException(nameof(room));
        if (placement is null) throw new ArgumentNullException(nameof(placement));
        if (definition is null) throw new ArgumentNullException(nameof(definition));
        if (authority is null) throw new ArgumentNullException(nameof(authority));
        definition.Validate();
        if (!authority.HasAuthority) return;

        var roster = EncounterRoster(definition);
        for (var index = 0; index < roster.Count; index++)
            SpawnCreature(
                room,
                placement,
                definition,
                authority,
                locationSeed,
                roster[index],
                index);

        if (definition.Room.Role == UnderworldDungeonRoomRole.Resource)
            PopulateResources(room, placement, definition, authority, locationSeed);
    }

    private static IReadOnlyList<string> EncounterRoster(
        UnderworldDrownedVaultRoomDefinition definition)
    {
        var suffix = definition.Room.Id.Substring(
            UnderworldBlackwaterDrownedVaultsCatalog.RoomIdPrefix.Length);
        switch (suffix)
        {
            case "lamprey-run":
                return new[] { "Blackwater Lamprey", "Blackwater Lamprey", "Blackwater Lamprey", "Gloomfin" };
            case "deep-hunter-lair":
                return new[] { "Deep Hunter", "Cave Ray", "Cave Ray" };
            case "sunken-quay":
                return new[] { "Shoreclaw", "Shoreclaw", "Abyss Shellback" };
            case "abyssal-sanctum":
                return new[] { "Lantern Angler", "Lantern Angler", "Deep Hunter" };
            case "siphon-hall":
                return new[] { "Gloomfin", "Blackwater Lamprey" };
            case "undertow-sluice":
                return new[] { "Lantern Angler", "Gloomfin" };
        }

        if (definition.Room.Role == UnderworldDungeonRoomRole.Hazard)
            return definition.RouteMode == UnderworldDrownedVaultRouteMode.Dry
                ? new[] { "Shoreclaw" }
                : new[] { "Blackwater Lamprey", "Gloomfin" };
        if (definition.Room.Role == UnderworldDungeonRoomRole.Encounter)
            return definition.RouteMode == UnderworldDrownedVaultRouteMode.Dry
                ? new[] { "Shoreclaw", "Abyss Shellback" }
                : new[] { "Gloomfin", "Lantern Angler" };
        if (definition.Room.Role == UnderworldDungeonRoomRole.Landmark &&
            definition.RouteMode != UnderworldDrownedVaultRouteMode.Dry)
            return new[] { "Cave Ray" };
        return Array.Empty<string>();
    }

    private static void SpawnCreature(
        GameObject room,
        UnderworldDungeonRoomPlacement placement,
        UnderworldDrownedVaultRoomDefinition definition,
        DrownedVaultEncounterAuthority authority,
        int locationSeed,
        string creatureName,
        int unitIndex)
    {
        var entry = UnderworldCreaturePrototypes.All.FirstOrDefault(value =>
            string.Equals(value.Name, creatureName, StringComparison.Ordinal) &&
            string.Equals(value.Biome, "Blackwater Deep", StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"Drowned Vault encounter requires unknown Blackwater creature '{creatureName}'.");
        var source = PrefabManager.Instance.GetPrefab(entry.Prefab)
            ?? throw new InvalidOperationException(
                $"Drowned Vault encounter requires registered creature prefab '{entry.Prefab}'.");

        var identity =
            $"{unchecked((uint)locationSeed):X8}.{placement.InstanceId}.creature.{unitIndex + 1:00}";
        if (authority.IsCleared(identity) ||
            FindLivingCreature(identity, room.scene.handle) is not null)
            return;

        var random = new System.Random(
            StableSeed(locationSeed, placement.InstanceId, unitIndex));
        var x = ((float)random.NextDouble() - .5f) *
                (float)definition.Room.WidthMeters * .44f;
        var z = ((float)random.NextDouble() - .5f) *
                (float)definition.Room.DepthMeters * .44f;
        var aquatic = entry.Donor == "Serpent" || entry.Donor == "Leech";
        var y = SpawnHeight(definition, aquatic, random);

        var position = room.transform.TransformPoint(new Vector3(x, y, z));
        var rotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
        var instance = UnityEngine.Object.Instantiate(source, position, rotation);
        instance.name = entry.Prefab + "_DrownedVault_" + unitIndex.ToString("00");

        var character = instance.GetComponent<Character>();
        var view = instance.GetComponent<ZNetView>();
        if (character is null || view is null || !view.IsValid())
        {
            UnityEngine.Object.Destroy(instance);
            throw new InvalidOperationException(
                $"Drowned Vault creature '{entry.Prefab}' did not create valid Character/ZNetView authority.");
        }

        view.GetZDO().Set(SpawnIdentityKey, identity);
        var tracker = instance.GetComponent<DrownedVaultEncounterDeathTracker>()
            ?? instance.AddComponent<DrownedVaultEncounterDeathTracker>();
        tracker.Bind(character, authority, identity);
        instance.SetActive(true);
    }

    private static float SpawnHeight(
        UnderworldDrownedVaultRoomDefinition definition,
        bool aquatic,
        System.Random random)
    {
        if (!aquatic)
            return definition.RouteMode == UnderworldDrownedVaultRouteMode.Dry ? .65f : 1.15f;

        if (definition.WaterDepthMeters <= .75d)
            throw new InvalidOperationException(
                $"Aquatic Drowned Vault encounter cannot spawn in effectively dry room '{definition.Room.Id}'.");

        var depth = (float)definition.WaterDepthMeters;
        var upper = Mathf.Min(.9f, depth * .25f);
        var lower = Mathf.Max(.65f, depth * .72f);
        return -Mathf.Lerp(upper, lower, (float)random.NextDouble());
    }

    private static Character? FindLivingCreature(string identity, int sceneHandle)
    {
        foreach (var character in Character.GetAllCharacters())
        {
            if (character is null || character.IsDead() ||
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
        UnderworldDrownedVaultRoomDefinition definition,
        DrownedVaultEncounterAuthority authority,
        int locationSeed)
    {
        var resources = UnderworldResourceCatalog.All
            .Where(value => value.Biome == UnderworldTerrainBiome.BlackwaterDeep)
            .ToArray();
        if (resources.Length != 4)
            throw new InvalidOperationException(
                $"Drowned Vault resource authority expected four Blackwater resources; found {resources.Length}.");

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
                    $"Drowned Vault resource room requires pickup prefab '{resource.PickupPrefab}'.");

            var random = new System.Random(
                StableSeed(locationSeed ^ 0x2147A11D, placement.InstanceId, index));
            var x = ((float)random.NextDouble() - .5f) *
                    (float)definition.Room.WidthMeters * .46f;
            var z = ((float)random.NextDouble() - .5f) *
                    (float)definition.Room.DepthMeters * .46f;
            var y = ResourceHeight(definition);
            var instance = UnityEngine.Object.Instantiate(
                source,
                room.transform.TransformPoint(new Vector3(x, y, z)),
                Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f));
            instance.name = resource.PickupPrefab + "_DrownedVault_" + index.ToString("00");

            var view = instance.GetComponent<ZNetView>();
            var pickable = instance.GetComponent<Pickable>();
            if (view is null || !view.IsValid() || pickable is null)
            {
                UnityEngine.Object.Destroy(instance);
                throw new InvalidOperationException(
                    $"Drowned Vault resource '{resource.PickupPrefab}' lacks Pickable/ZNetView authority.");
            }

            view.GetZDO().Set(ResourceIdentityKey, identity);
            var tracker = instance.GetComponent<DrownedVaultResourceTracker>()
                ?? instance.AddComponent<DrownedVaultResourceTracker>();
            tracker.Bind(authority, identity);
            instance.SetActive(true);
        }
    }

    private static float ResourceHeight(UnderworldDrownedVaultRoomDefinition definition)
    {
        if (definition.RouteMode is UnderworldDrownedVaultRouteMode.Dry or
            UnderworldDrownedVaultRouteMode.Mixed or
            UnderworldDrownedVaultRouteMode.AirPocket)
            return 1.15f;

        return -(float)definition.WaterDepthMeters + .55f;
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
