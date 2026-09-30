using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Builds the five ordinary Underworld dungeons from private clones of vanilla dungeon assets.
/// It never mutates vanilla DungeonDB rooms, locations, generators or door prefabs.
/// Deep Fracture deliberately remains outside this registrar.
/// </summary>
internal sealed class UnderworldVanillaDungeonRegistrar : IDisposable
{
    private readonly ManualLogSource _log;
    private readonly Dictionary<string, Dictionary<string, string>> _roomNames =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, DungeonGenerator> _generators =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, string[]> _donorRequiredRooms =
        new(StringComparer.Ordinal);
    private readonly List<string> _registeredRooms = new();
    private readonly List<string> _registeredLocations = new();
    private readonly List<string> _registeredDoorPrefabs = new();
    private readonly List<GameObject> _themeCarriers = new();
    private bool _roomsSubscribed;
    private bool _locationsSubscribed;

    internal UnderworldVanillaDungeonRegistrar(ManualLogSource log) =>
        _log = log ?? throw new ArgumentNullException(nameof(log));

    internal void Register()
    {
        UnderworldVanillaDungeonReuseCatalog.Validate();
        var active = ActiveProfiles().ToArray();
        if (active.Length == 0)
        {
            _log.LogDebug(
                "Expanded vanilla Underworld dungeons remain fail-closed: all five ordinary dungeon definitions are Planned and DDE candidate admission is disabled.");
            return;
        }

        if (UnderworldVanillaDungeonCandidatePolicy.TryGetCandidate(out var candidate))
            _log.LogWarning(
                $"DDE CANDIDATE MODE: admitting Planned donor derivative '{candidate.DungeonId}' for disposable validation only. This is not RuntimeReady promotion.");

        DungeonManager.OnVanillaRoomsAvailable += RegisterRooms;
        ZoneManager.OnVanillaLocationsAvailable += RegisterLocations;
        _roomsSubscribed = true;
        _locationsSubscribed = true;
    }

    private IEnumerable<UnderworldVanillaDungeonReuseDefinition> ActiveProfiles()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var profile in UnderworldVanillaDungeonReuseCatalog.All)
        {
            if (Definition(profile).Status != UnderworldDungeonStatus.RuntimeReady) continue;
            seen.Add(profile.DungeonId);
            yield return profile;
        }

        if (UnderworldVanillaDungeonCandidatePolicy.TryGetCandidate(out var candidate) &&
            seen.Add(candidate.DungeonId))
            yield return candidate;
    }

    private void RegisterRooms()
    {
        try
        {
            foreach (var profile in ActiveProfiles())
                RegisterRoomFamily(profile);
        }
        catch (Exception exception)
        {
            _log.LogError("Expanded vanilla Underworld room registration failed: " + exception);
            throw;
        }
        finally
        {
            if (_roomsSubscribed)
            {
                DungeonManager.OnVanillaRoomsAvailable -= RegisterRooms;
                _roomsSubscribed = false;
            }
        }
    }

    private void RegisterRoomFamily(UnderworldVanillaDungeonReuseDefinition profile)
    {
        if (!Enum.TryParse(profile.DonorThemeName, ignoreCase: false, out Room.Theme donorTheme) ||
            donorTheme == Room.Theme.None)
            throw new InvalidOperationException(
                $"Vanilla dungeon donor theme '{profile.DonorThemeName}' is unavailable.");

        var themeName = ThemeName(profile);
        var carrier = new GameObject(themeName + "_ThemeCarrier");
        carrier.SetActive(false);
        carrier.AddComponent<DungeonGenerator>();
        if (!DungeonManager.Instance.RegisterDungeonTheme(carrier, themeName))
        {
            UnityEngine.Object.Destroy(carrier);
            throw new InvalidOperationException(
                $"Jotunn refused expanded vanilla dungeon theme '{themeName}'.");
        }
        _themeCarriers.Add(carrier);

        var sourceRooms = DungeonDB.GetRooms()
            .Where(data => data is not null && data.m_enabled &&
                           (data.m_theme & donorTheme) != Room.Theme.None)
            .ToArray();
        if (sourceRooms.Length == 0)
            throw new InvalidOperationException(
                $"Vanilla dungeon donor '{profile.DonorDisplayName}' resolved zero loaded room definitions.");

        var names = new Dictionary<string, string>(StringComparer.Ordinal);
        var scale = (float)profile.LinearRoomScale;
        for (var index = 0; index < sourceRooms.Length; index++)
        {
            var data = sourceRooms[index];
            data.m_prefab.Load();
            try
            {
                var source = data.m_prefab.Asset;
                if (!source)
                    throw new InvalidOperationException(
                        $"Vanilla dungeon room soft reference '{data.m_prefab.Name}' did not load.");

                var clone = UnityEngine.Object.Instantiate(source);
                var sourceName = data.m_prefab.Name;
                clone.name = RoomPrefabName(profile, sourceName, index);

                // The recognizable vanilla tile itself is enlarged, not replaced. Because the
                // RoomConnection transforms live beneath this root, their world-space separation
                // scales with the room geometry; Room.m_size below is expanded to the same linear
                // factor so vanilla packing/collision authority sees the true larger footprint.
                clone.transform.localScale *= scale;

                var room = clone.GetComponent<Room>()
                    ?? throw new InvalidOperationException(
                        $"Cloned donor room '{sourceName}' has no Room component.");
                room.m_size = new Vector3Int(
                    Mathf.CeilToInt(room.m_size.x * scale),
                    Mathf.CeilToInt(room.m_size.y * scale),
                    Mathf.CeilToInt(room.m_size.z * scale));

                RebindPopulation(clone, profile, index);

                var custom = new CustomRoom(
                    clone,
                    fixReference: false,
                    new RoomConfig(themeName)
                    {
                        Enabled = true,
                    });
                if (!DungeonManager.Instance.AddCustomRoom(custom))
                    throw new InvalidOperationException(
                        $"Jotunn refused cloned donor room '{clone.name}'.");

                _registeredRooms.Add(clone.name);
                names[sourceName] = clone.name;
            }
            finally
            {
                data.m_prefab.Release();
            }
        }

        _roomNames[profile.DungeonId] = names;
        if (_generators.TryGetValue(profile.DungeonId, out var existingGenerator))
            RemapRequiredRooms(profile, existingGenerator);
        _log.LogInfo(
            $"Registered {names.Count} private {profile.DonorDisplayName} room clones for {profile.Biome} at {profile.LinearRoomScale:0.##}x linear scale.");
    }

    private void RegisterLocations()
    {
        try
        {
            foreach (var profile in ReadyProfiles())
                RegisterLocation(profile);
        }
        catch (Exception exception)
        {
            _log.LogError("Expanded vanilla Underworld location registration failed: " + exception);
            throw;
        }
        finally
        {
            if (_locationsSubscribed)
            {
                ZoneManager.OnVanillaLocationsAvailable -= RegisterLocations;
                _locationsSubscribed = false;
            }
        }
    }

    private void RegisterLocation(UnderworldVanillaDungeonReuseDefinition profile)
    {
        var definition = Definition(profile);
        if (ZoneManager.Instance.GetZoneLocation(definition.PrefabName) is not null ||
            CustomLocation.IsCustomLocation(definition.PrefabName))
            throw new InvalidOperationException(
                $"Occupied expanded vanilla dungeon identity '{definition.PrefabName}'.");

        var donorEntrance = profile.DonorEntrancePrefabs
            .FirstOrDefault(name => ZoneManager.Instance.GetZoneLocation(name) is not null)
            ?? throw new InvalidOperationException(
                $"{definition.DisplayName} found none of its vanilla entrance donors: " +
                string.Join(", ", profile.DonorEntrancePrefabs));

        var custom = ZoneManager.Instance.CreateClonedLocation(definition.PrefabName, donorEntrance)
            ?? throw new InvalidOperationException(
                $"Jotunn could not clone vanilla dungeon entrance '{donorEntrance}'.");
        _registeredLocations.Add(definition.PrefabName);

        var generator = custom.Prefab.GetComponentInChildren<DungeonGenerator>(true)
            ?? throw new InvalidOperationException(
                $"Cloned vanilla entrance '{donorEntrance}' has no DungeonGenerator.");
        if (!string.Equals(generator.gameObject.name, profile.DonorGeneratorPrefab, StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"Vanilla entrance '{donorEntrance}' resolved generator '{generator.gameObject.name}', " +
                $"expected '{profile.DonorGeneratorPrefab}'.");

        // A private theme is mandatory. Leaving the donor bitmask active would mix ordinary
        // vanilla-size rooms back into the 1.5x Magenheim room family.
        if (!DungeonManager.Instance.RegisterDungeonTheme(custom.Prefab, ThemeName(profile)))
            throw new InvalidOperationException(
                $"Jotunn refused private dungeon theme on '{definition.PrefabName}'.");
        generator.m_themes = Room.Theme.None;

        var vanillaMin = generator.m_minRooms;
        var vanillaMax = generator.m_maxRooms;
        if (vanillaMin < 1 || vanillaMax < vanillaMin)
            throw new InvalidOperationException(
                $"Vanilla generator '{profile.DonorGeneratorPrefab}' has invalid room bounds {vanillaMin}-{vanillaMax}.");

        generator.m_minRooms = profile.ExpandedMinimumRooms(vanillaMin);
        generator.m_maxRooms = profile.ExpandedMaximumRooms(vanillaMax);

        // More rooms plus larger room footprints require a larger legal packing volume. Use a
        // derived linear scale rather than a magic fixed zone size so donor updates remain valid.
        var zoneScale = (float)(profile.LinearRoomScale * Math.Sqrt(profile.RoomCountMultiplier));
        generator.m_zoneSize *= zoneScale;
        generator.m_tileWidth *= (float)profile.LinearRoomScale;

        _generators[profile.DungeonId] = generator;
        _donorRequiredRooms[profile.DungeonId] =
            generator.m_requiredRooms?.ToArray() ?? Array.Empty<string>();
        RemapRequiredRooms(profile, generator);

        CloneAndScaleDoors(generator, profile);

        var zone = custom.ZoneLocation;
        zone.m_biome = UnderworldTerrainRuntime.ToNativeBiome(profile.Biome);
        zone.m_biomeArea = JotunnWorldgenAdapter.MapArea(Magenheim.Core.Worldgen.SpawnArea.All);
        zone.m_quantity = definition.Quantity;
        zone.m_prioritized = false;
        zone.m_exteriorRadius = (float)definition.ExteriorRadiusMeters;
        zone.m_minTerrainDelta = 0f;
        zone.m_maxTerrainDelta = (float)definition.MaxTerrainDeltaMeters;
        zone.m_minDistanceFromSimilar = (float)definition.MinDistanceFromSimilarMeters;
        zone.m_group = definition.Id;
        zone.m_unique = false;

        _log.LogInfo(
            $"Registered {definition.DisplayName} from vanilla {profile.DonorDisplayName}: " +
            $"{vanillaMin}-{vanillaMax} donor rooms -> {generator.m_minRooms}-{generator.m_maxRooms}, " +
            $"{profile.LinearRoomScale:0.##}x room scale, {zoneScale:0.##}x legal generator span.");
    }

    private void RemapRequiredRooms(
        UnderworldVanillaDungeonReuseDefinition profile,
        DungeonGenerator generator)
    {
        if (!_donorRequiredRooms.TryGetValue(profile.DungeonId, out var donorRequired))
            donorRequired = generator.m_requiredRooms?.ToArray() ?? Array.Empty<string>();
        if (!_roomNames.TryGetValue(profile.DungeonId, out var roomNames))
        {
            // Location and DungeonDB callbacks are independent. Preserve donor names until the
            // room-clone callback arrives, then remap them atomically to private room identities.
            generator.m_requiredRooms = donorRequired.ToList();
            return;
        }

        var mapped = new List<string>();
        foreach (var required in donorRequired)
            if (roomNames.TryGetValue(required, out var replacement))
                mapped.Add(replacement);

        generator.m_requiredRooms = mapped;
        generator.m_minRequiredRooms = Math.Min(generator.m_minRequiredRooms, mapped.Count);
    }

    private void CloneAndScaleDoors(
        DungeonGenerator generator,
        UnderworldVanillaDungeonReuseDefinition profile)
    {
        if (generator.m_doorTypes is null) return;

        for (var index = 0; index < generator.m_doorTypes.Count; index++)
        {
            var definition = generator.m_doorTypes[index];
            if (definition?.m_prefab is null) continue;

            var name =
                "Magenheim_Underworld_DungeonDoor_" +
                Safe(profile.Biome.ToString()) + "_" + index.ToString("00");
            if (PrefabManager.Instance.GetPrefab(name) is not null)
                throw new InvalidOperationException(
                    $"Occupied expanded dungeon door identity '{name}'.");

            var clone = PrefabManager.Instance.CreateClonedPrefab(name, definition.m_prefab)
                ?? throw new InvalidOperationException(
                    $"Unable to clone dungeon door '{definition.m_prefab.name}'.");
            clone.transform.localScale *= (float)profile.LinearRoomScale;
            PrefabManager.Instance.AddPrefab(clone);
            definition.m_prefab = clone;
            _registeredDoorPrefabs.Add(name);
        }
    }

    private static void RebindPopulation(
        GameObject room,
        UnderworldVanillaDungeonReuseDefinition profile,
        int roomIndex)
    {
        var creatures = Creatures(profile.Biome);
        var resources = Resources(profile.Biome);
        if (creatures.Length == 0 || resources.Length == 0)
            throw new InvalidOperationException(
                $"Biome '{profile.Biome}' has no registered dungeon ecology.");

        var spawners = room.GetComponentsInChildren<CreatureSpawner>(true);
        for (var index = 0; index < spawners.Length; index++)
        {
            var prefab = PrefabManager.Instance.GetPrefab(
                creatures[(roomIndex + index) % creatures.Length].Prefab)
                ?? throw new InvalidOperationException(
                    $"Underworld creature prefab '{creatures[(roomIndex + index) % creatures.Length].Prefab}' is unavailable.");
            spawners[index].m_creaturePrefab = prefab;
            spawners[index].m_requiredGlobalKey = string.Empty;
            spawners[index].m_blockingGlobalKey = string.Empty;
        }

        var areas = room.GetComponentsInChildren<SpawnArea>(true);
        for (var areaIndex = 0; areaIndex < areas.Length; areaIndex++)
        {
            var area = areas[areaIndex];
            area.m_prefabs.Clear();
            var count = Math.Min(4, creatures.Length);
            for (var index = 0; index < count; index++)
            {
                var entry = creatures[(roomIndex + areaIndex + index) % creatures.Length];
                var prefab = PrefabManager.Instance.GetPrefab(entry.Prefab)
                    ?? throw new InvalidOperationException(
                        $"Underworld creature prefab '{entry.Prefab}' is unavailable.");
                area.m_prefabs.Add(new SpawnArea.SpawnData
                {
                    m_prefab = prefab,
                    m_weight = 1f,
                    m_minLevel = 1,
                    m_maxLevel = 2,
                });
            }
            area.m_maxNear = Math.Min(area.m_maxNear, 4);
            area.m_maxTotal = Math.Min(area.m_maxTotal, 10);
        }

        var containers = room.GetComponentsInChildren<Container>(true);
        for (var index = 0; index < containers.Length; index++)
            containers[index].m_defaultItems = ResourceTable(resources, roomIndex + index);

        var pickables = room.GetComponentsInChildren<Pickable>(true);
        for (var index = 0; index < pickables.Length; index++)
        {
            var resource = resources[(roomIndex + index) % resources.Length];
            var prefab = PrefabManager.Instance.GetPrefab(resource.Prefab)
                ?? throw new InvalidOperationException(
                    $"Underworld resource item '{resource.Prefab}' is unavailable.");
            pickables[index].m_itemPrefab = prefab;
            pickables[index].m_amount = Math.Max(1, pickables[index].m_amount);
            pickables[index].m_overrideName = resource.Name;
        }

        // Donor mineables/destructibles must not smuggle Surface progression drops back in after
        // the obvious chests/pickables have been replaced.
        foreach (var mine in room.GetComponentsInChildren<MineRock>(true))
            mine.m_dropItems = ResourceTable(resources, roomIndex + 11);
        foreach (var mine in room.GetComponentsInChildren<MineRock5>(true))
            mine.m_dropItems = ResourceTable(resources, roomIndex + 17);
        foreach (var destroyed in room.GetComponentsInChildren<DropOnDestroyed>(true))
            destroyed.m_dropWhenDestroyed = ResourceTable(resources, roomIndex + 23);

        // Keep the donor stonework/props, but remove Surface progression/lore interactions.
        foreach (var vegvisir in room.GetComponentsInChildren<Vegvisir>(true))
            UnityEngine.Object.DestroyImmediate(vegvisir);
        foreach (var runestone in room.GetComponentsInChildren<Runestone>(true))
            UnityEngine.Object.DestroyImmediate(runestone);
    }

    private static DropTable ResourceTable(
        UnderworldResourceDefinition[] resources,
        int offset)
    {
        var table = new DropTable
        {
            m_dropMin = 1,
            m_dropMax = 3,
            m_dropChance = 1f,
            m_oneOfEach = false,
        };
        for (var index = 0; index < resources.Length; index++)
        {
            var resource = resources[(offset + index) % resources.Length];
            var prefab = PrefabManager.Instance.GetPrefab(resource.Prefab)
                ?? throw new InvalidOperationException(
                    $"Underworld resource item '{resource.Prefab}' is unavailable.");
            table.m_drops.Add(new DropTable.DropData
            {
                m_item = prefab,
                m_stackMin = 1,
                m_stackMax = index == resources.Length - 1 ? 1 : 3,
                m_weight = index == resources.Length - 1 ? .35f : 1f,
                m_dontScale = false,
            });
        }
        return table;
    }

    private static UnderworldCreaturePrototypes.Entry[] Creatures(UnderworldTerrainBiome biome)
    {
        var label = BiomeLabel(biome);
        return UnderworldCreaturePrototypes.All
            .Where(entry => string.Equals(entry.Biome, label, StringComparison.Ordinal))
            .ToArray();
    }

    private static UnderworldResourceDefinition[] Resources(UnderworldTerrainBiome biome) =>
        UnderworldResourceCatalog.All.Where(entry => entry.Biome == biome).ToArray();

    private static string BiomeLabel(UnderworldTerrainBiome biome) => biome switch
    {
        UnderworldTerrainBiome.FungalForest => "Fungal Forest",
        UnderworldTerrainBiome.BlackwaterDeep => "Blackwater Deep",
        UnderworldTerrainBiome.SulfurousWastes => "Sulfurous Wastes",
        UnderworldTerrainBiome.FrozenCaverns => "Frozen Caverns",
        UnderworldTerrainBiome.FractureZones => "Fracture Zones",
        UnderworldTerrainBiome.GreatDecay => "Great Decay",
        _ => throw new ArgumentOutOfRangeException(nameof(biome), biome, null),
    };

    private static UnderworldDungeonDefinition Definition(
        UnderworldVanillaDungeonReuseDefinition profile) =>
        UnderworldDungeonCatalog.All.Single(value =>
            string.Equals(value.Id, profile.DungeonId, StringComparison.Ordinal));

    private static string ThemeName(UnderworldVanillaDungeonReuseDefinition profile) =>
        "MagenheimUnderworldVanilla" + Safe(profile.Biome.ToString());

    private static string RoomPrefabName(
        UnderworldVanillaDungeonReuseDefinition profile,
        string donorName,
        int index) =>
        "Magenheim_Underworld_VanillaRoom_" +
        Safe(profile.Biome.ToString()) + "_" + index.ToString("000") + "_" + Safe(donorName);

    private static string Safe(string value) =>
        new string(value.Where(char.IsLetterOrDigit).ToArray());

    public void Dispose()
    {
        if (_roomsSubscribed)
        {
            DungeonManager.OnVanillaRoomsAvailable -= RegisterRooms;
            _roomsSubscribed = false;
        }
        if (_locationsSubscribed)
        {
            ZoneManager.OnVanillaLocationsAvailable -= RegisterLocations;
            _locationsSubscribed = false;
        }

        foreach (var room in _registeredRooms)
            DungeonManager.Instance.RemoveRoom(room);
        _registeredRooms.Clear();

        foreach (var location in _registeredLocations)
            ZoneManager.Instance.RemoveCustomLocation(location);
        _registeredLocations.Clear();

        foreach (var carrier in _themeCarriers)
            if (carrier) UnityEngine.Object.Destroy(carrier);
        _themeCarriers.Clear();

        // PrefabManager does not expose a symmetric removal API for registered network prefabs.
        // Door clones are lifetime-scoped to the plugin session and use unique Magenheim identities.
        _registeredDoorPrefabs.Clear();
        _roomNames.Clear();
        _generators.Clear();
        _donorRequiredRooms.Clear();
    }
}
