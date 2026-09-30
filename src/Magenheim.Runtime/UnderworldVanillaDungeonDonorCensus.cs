using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Installed-game evidence capture for Deep Dungeon Expansion Gate 1. This reads live vanilla
/// ZoneSystem/DungeonDB authority without registering or mutating any Magenheim donor dungeon.
/// </summary>
internal static class UnderworldVanillaDungeonDonorCensus
{
    private const string ProgramDirectory = "deep-dungeon-expansion";

    internal static string CaptureAll(ManualLogSource? log)
    {
        if (ZoneSystem.instance is null)
            throw new InvalidOperationException("ZoneSystem is unavailable; load a world before capturing donor census.");
        if (DungeonDB.instance is null)
            throw new InvalidOperationException("DungeonDB is unavailable; load a world before capturing donor census.");

        UnderworldVanillaDungeonReuseCatalog.Validate();

        var root = Path.Combine(Paths.ConfigPath, "Magenheim", "validation", ProgramDirectory);
        Directory.CreateDirectory(root);

        var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var summary = new StringBuilder();
        summary.AppendLine("Deep Dungeon Expansion donor census");
        summary.AppendLine("utc=" + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        summary.AppendLine("game=" + global::Version.GetVersionString());
        summary.AppendLine("assembly_valheim_sha256=" + AssemblySha256());
        summary.AppendLine("profiles=" + UnderworldVanillaDungeonReuseCatalog.All.Count);
        summary.AppendLine();

        var passed = 0;
        foreach (var profile in UnderworldVanillaDungeonReuseCatalog.All)
        {
            var result = Capture(profile);
            if (result.Pass) passed++;

            var file = Path.Combine(
                root,
                stamp + "-" + Safe(profile.Biome.ToString()) + "-donor-census.txt");
            File.WriteAllText(file, result.Text, new UTF8Encoding(false));
            summary.AppendLine(
                $"{profile.Biome}\t{(result.Pass ? "PASS" : "FAIL")}\t{Path.GetFileName(file)}");
            log?.LogInfo(
                $"DDE donor census {profile.Biome}: {(result.Pass ? "PASS" : "FAIL")} -> '{file}'.");
        }

        var index = Path.Combine(root, stamp + "-donor-census-index.txt");
        summary.AppendLine();
        summary.AppendLine($"result={passed}/{UnderworldVanillaDungeonReuseCatalog.All.Count}");
        File.WriteAllText(index, summary.ToString(), new UTF8Encoding(false));

        if (passed != UnderworldVanillaDungeonReuseCatalog.All.Count)
            throw new InvalidOperationException(
                $"Deep Dungeon donor census failed for {UnderworldVanillaDungeonReuseCatalog.All.Count - passed} family/families. Evidence index: {index}");

        return index;
    }

    private static CensusResult Capture(UnderworldVanillaDungeonReuseDefinition profile)
    {
        var errors = new List<string>();
        var output = new StringBuilder();

        output.AppendLine("Deep Dungeon Expansion Gate 1 donor census");
        output.AppendLine("dungeon_id=" + profile.DungeonId);
        output.AppendLine("biome=" + profile.Biome);
        output.AppendLine("donor=" + profile.DonorDisplayName);
        output.AppendLine("expected_generator=" + profile.DonorGeneratorPrefab);
        output.AppendLine("expected_theme=" + profile.DonorThemeName);
        output.AppendLine("room_scale_floor=" + profile.LinearRoomScale.ToString("0.###", CultureInfo.InvariantCulture));
        output.AppendLine("room_count_multiplier=" + profile.RoomCountMultiplier.ToString("0.###", CultureInfo.InvariantCulture));

        if (!Enum.TryParse(profile.DonorThemeName, false, out Room.Theme theme) ||
            theme == Room.Theme.None)
        {
            errors.Add("Room.Theme does not resolve: " + profile.DonorThemeName);
        }

        var donorRooms = Enum.TryParse(profile.DonorThemeName, false, out Room.Theme parsedTheme)
            ? DungeonDB.GetRooms()
                .Where(room => room is not null && room.m_enabled &&
                               (room.m_theme & parsedTheme) != Room.Theme.None)
                .ToArray()
            : Array.Empty<DungeonDB.RoomData>();
        output.AppendLine("enabled_theme_rooms=" + donorRooms.Length);
        if (donorRooms.Length == 0)
            errors.Add("DungeonDB returned zero enabled donor rooms.");

        if (donorRooms.Length > 0)
        {
            output.AppendLine("room_names=" +
                string.Join(",", donorRooms.Select(room => room.m_prefab.Name).OrderBy(name => name, StringComparer.Ordinal)));
            CaptureRoomEnvelope(donorRooms, output, errors);
        }

        foreach (var entranceName in profile.DonorEntrancePrefabs)
        {
            output.AppendLine();
            output.AppendLine("[entrance " + entranceName + "]");
            var zone = ZoneManager.Instance.GetZoneLocation(entranceName);
            if (zone is null)
            {
                errors.Add("ZoneLocation does not resolve: " + entranceName);
                output.AppendLine("resolved=false");
                continue;
            }

            output.AppendLine("resolved=true");
            output.AppendLine("zone_prefab_name=" + zone.m_prefabName);
            output.AppendLine("zone_biome=" + zone.m_biome);
            output.AppendLine("zone_quantity=" + zone.m_quantity);
            output.AppendLine("zone_exterior_radius=" + zone.m_exteriorRadius.ToString("0.###", CultureInfo.InvariantCulture));

            zone.m_prefab.Load();
            try
            {
                var prefab = zone.m_prefab.Asset;
                if (!prefab)
                {
                    errors.Add("Loaded ZoneLocation asset is null: " + entranceName);
                    output.AppendLine("asset=false");
                    continue;
                }

                output.AppendLine("asset=true");
                output.AppendLine("asset_name=" + prefab.name);

                var location = prefab.GetComponent<Location>() ??
                               prefab.GetComponentInChildren<Location>(true);
                if (!location)
                {
                    errors.Add("Location component missing: " + entranceName);
                    output.AppendLine("location_component=false");
                    continue;
                }

                output.AppendLine("location_component=true");
                output.AppendLine("location_use_custom_interior=" + location.m_useCustomInteriorTransform);
                output.AppendLine("location_interior_transform=" +
                    (location.m_interiorTransform ? PathOf(location.m_interiorTransform, prefab.transform) : "<null>"));

                var generators = prefab.GetComponentsInChildren<DungeonGenerator>(true);
                output.AppendLine("generator_count=" + generators.Length);
                var matching = generators
                    .Where(value => string.Equals(
                        value.gameObject.name,
                        profile.DonorGeneratorPrefab,
                        StringComparison.Ordinal))
                    .ToArray();
                if (matching.Length != 1)
                {
                    errors.Add(
                        $"{entranceName}: expected exactly one generator '{profile.DonorGeneratorPrefab}', found {matching.Length}.");
                    output.AppendLine("expected_generator_matches=" + matching.Length);
                    foreach (var generator in generators)
                        output.AppendLine("observed_generator=" + generator.gameObject.name);
                    continue;
                }

                var dg = matching[0];
                output.AppendLine("expected_generator_matches=1");
                output.AppendLine("generator_name=" + dg.gameObject.name);
                output.AppendLine("generator_algorithm=" + dg.m_algorithm);
                output.AppendLine("generator_themes=" + dg.m_themes);
                output.AppendLine("generator_min_rooms=" + dg.m_minRooms);
                output.AppendLine("generator_max_rooms=" + dg.m_maxRooms);
                output.AppendLine("generator_min_required_rooms=" + dg.m_minRequiredRooms);
                output.AppendLine("generator_required_rooms=" +
                    string.Join(",", dg.m_requiredRooms ?? new List<string>()));
                output.AppendLine("generator_zone_size=" + Vector(dg.m_zoneSize));
                output.AppendLine("generator_tile_width=" +
                    dg.m_tileWidth.ToString("0.###", CultureInfo.InvariantCulture));
                output.AppendLine("generator_grid_size=" +
                    dg.m_gridSize.ToString(CultureInfo.InvariantCulture));
                output.AppendLine("generator_door_chance=" +
                    dg.m_doorChance.ToString("0.###", CultureInfo.InvariantCulture));
                output.AppendLine("generator_alternative_functionality=" + dg.m_alternativeFunctionality);
                output.AppendLine("generator_spawn_chance=" +
                    dg.m_spawnChance.ToString("0.###", CultureInfo.InvariantCulture));
                output.AppendLine("generator_camp_radius_min=" +
                    dg.m_campRadiusMin.ToString("0.###", CultureInfo.InvariantCulture));
                output.AppendLine("generator_camp_radius_max=" +
                    dg.m_campRadiusMax.ToString("0.###", CultureInfo.InvariantCulture));
                output.AppendLine("generator_zone_center=" + Vector(dg.m_zoneCenter));
                output.AppendLine("generator_use_custom_interior=" + dg.m_useCustomInteriorTransform);

                if (dg.m_minRooms < 1 || dg.m_maxRooms < dg.m_minRooms)
                    errors.Add(
                        $"{entranceName}: invalid donor room bounds {dg.m_minRooms}-{dg.m_maxRooms}.");

                if ((dg.m_themes & parsedTheme) == Room.Theme.None)
                    errors.Add(
                        $"{entranceName}: generator theme '{dg.m_themes}' does not include expected '{parsedTheme}'.");

                var doors = dg.m_doorTypes ?? new List<DungeonGenerator.DoorDef>();
                output.AppendLine("door_type_count=" + doors.Count);
                for (var index = 0; index < doors.Count; index++)
                {
                    var door = doors[index];
                    if (door is null)
                    {
                        output.AppendLine($"door_{index}=<null>");
                        continue;
                    }
                    output.AppendLine(
                        $"door_{index}=" +
                        $"{(door.m_prefab ? door.m_prefab.name : "<null>")}" +
                        $"|connection={door.m_connectionType}" +
                        $"|chance={door.m_chance.ToString("0.###", CultureInfo.InvariantCulture)}");
                }

                if (location.m_useCustomInteriorTransform != dg.m_useCustomInteriorTransform)
                    errors.Add(
                        $"{entranceName}: Location/DungeonGenerator custom-interior flags disagree.");
            }
            finally
            {
                zone.m_prefab.Release();
            }
        }

        output.AppendLine();
        output.AppendLine("errors=" + errors.Count);
        foreach (var error in errors)
            output.AppendLine("error=" + error);
        output.AppendLine("result=" + (errors.Count == 0 ? "PASS" : "FAIL"));

        return new CensusResult(errors.Count == 0, output.ToString());
    }

    private static void CaptureRoomEnvelope(
        DungeonDB.RoomData[] donorRooms,
        StringBuilder output,
        List<string> errors)
    {
        var roomSizes = new List<Vector3Int>();
        var connectionTypes = new SortedSet<string>(StringComparer.Ordinal);
        var totalConnections = 0;
        var entranceRooms = 0;
        var endcapRooms = 0;
        var dividerRooms = 0;
        var creatureSpawners = 0;
        var spawnAreas = 0;
        var containers = 0;
        var pickables = 0;
        var mineRocks = 0;
        var mineRock5s = 0;
        var destructibleDrops = 0;
        var vegvisirs = 0;
        var runestones = 0;
        var componentTypes = new Dictionary<string, int>(StringComparer.Ordinal);
        var doors = 0;
        var teleports = 0;
        var randomSpawns = 0;
        var destructibles = 0;
        var rootGeometryRooms = new List<string>();

        foreach (var data in donorRooms)
        {
            data.m_prefab.Load();
            try
            {
                var prefab = data.m_prefab.Asset;
                if (!prefab)
                {
                    errors.Add("Donor room asset failed to load: " + data.m_prefab.Name);
                    continue;
                }

                var room = prefab.GetComponent<Room>();
                if (!room)
                {
                    errors.Add("Donor room has no Room component: " + data.m_prefab.Name);
                    continue;
                }

                roomSizes.Add(room.m_size);
                if (prefab.GetComponent<Renderer>() ||
                    prefab.GetComponent<Collider>() ||
                    prefab.GetComponent<MeshFilter>())
                    rootGeometryRooms.Add(data.m_prefab.Name);
                if (room.m_entrance) entranceRooms++;
                if (room.m_endCap) endcapRooms++;
                if (room.m_divider) dividerRooms++;

                var connections = prefab.GetComponentsInChildren<RoomConnection>(false);
                totalConnections += connections.Length;
                foreach (var connection in connections)
                    connectionTypes.Add(string.IsNullOrEmpty(connection.m_type) ? "<empty>" : connection.m_type);

                creatureSpawners += prefab.GetComponentsInChildren<CreatureSpawner>(true).Length;
                spawnAreas += prefab.GetComponentsInChildren<SpawnArea>(true).Length;
                containers += prefab.GetComponentsInChildren<Container>(true).Length;
                pickables += prefab.GetComponentsInChildren<Pickable>(true).Length;
                mineRocks += prefab.GetComponentsInChildren<MineRock>(true).Length;
                mineRock5s += prefab.GetComponentsInChildren<MineRock5>(true).Length;
                destructibleDrops += prefab.GetComponentsInChildren<DropOnDestroyed>(true).Length;
                vegvisirs += prefab.GetComponentsInChildren<Vegvisir>(true).Length;
                runestones += prefab.GetComponentsInChildren<Runestone>(true).Length;
                foreach (var component in prefab.GetComponentsInChildren<Component>(true))
                {
                    if (!component || component is Transform) continue;
                    var typeName = component.GetType().Name;
                    componentTypes[typeName] =
                        componentTypes.TryGetValue(typeName, out var current) ? current + 1 : 1;
                    switch (typeName)
                    {
                        case "Door": doors++; break;
                        case "Teleport": teleports++; break;
                        case "RandomSpawn": randomSpawns++; break;
                        case "Destructible": destructibles++; break;
                    }
                }
            }
            finally
            {
                data.m_prefab.Release();
            }
        }

        if (roomSizes.Count == 0)
        {
            errors.Add("No donor room geometry could be inspected.");
            return;
        }

        output.AppendLine("room_size_min=" + VectorInt(new Vector3Int(
            roomSizes.Min(value => value.x),
            roomSizes.Min(value => value.y),
            roomSizes.Min(value => value.z))));
        output.AppendLine("room_size_max=" + VectorInt(new Vector3Int(
            roomSizes.Max(value => value.x),
            roomSizes.Max(value => value.y),
            roomSizes.Max(value => value.z))));
        output.AppendLine("room_entrance_count=" + entranceRooms);
        output.AppendLine("room_endcap_count=" + endcapRooms);
        output.AppendLine("room_divider_count=" + dividerRooms);
        output.AppendLine("room_connection_count=" + totalConnections);
        output.AppendLine("room_connection_types=" + string.Join(",", connectionTypes));
        output.AppendLine("socket_creature_spawners=" + creatureSpawners);
        output.AppendLine("socket_spawn_areas=" + spawnAreas);
        output.AppendLine("socket_containers=" + containers);
        output.AppendLine("socket_pickables=" + pickables);
        output.AppendLine("socket_mine_rock=" + mineRocks);
        output.AppendLine("socket_mine_rock5=" + mineRock5s);
        output.AppendLine("socket_drop_on_destroyed=" + destructibleDrops);
        output.AppendLine("socket_vegvisirs=" + vegvisirs);
        output.AppendLine("socket_runestones=" + runestones);
        output.AppendLine("mechanic_doors=" + doors);
        output.AppendLine("mechanic_teleports=" + teleports);
        output.AppendLine("mechanic_random_spawns=" + randomSpawns);
        output.AppendLine("mechanic_destructibles=" + destructibles);
        output.AppendLine("room_root_geometry_count=" + rootGeometryRooms.Count);
        output.AppendLine("room_root_geometry_names=" +
            string.Join(",", rootGeometryRooms.OrderBy(name => name, StringComparer.Ordinal)));
        output.AppendLine("room_component_types=" + string.Join(";",
            componentTypes.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => pair.Key + ":" + pair.Value.ToString(CultureInfo.InvariantCulture))));
    }

    private static string AssemblySha256()
    {
        var path = typeof(DungeonGenerator).Assembly.Location;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return "<unavailable>";
        using var stream = File.OpenRead(path);
        using var sha = SHA256.Create();
        return string.Concat(sha.ComputeHash(stream)
            .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
    }

    private static string VectorInt(Vector3Int value) =>
        string.Join(",",
            value.x.ToString(CultureInfo.InvariantCulture),
            value.y.ToString(CultureInfo.InvariantCulture),
            value.z.ToString(CultureInfo.InvariantCulture));

    private static string Vector(Vector3 value) =>
        string.Join(",",
            value.x.ToString("0.###", CultureInfo.InvariantCulture),
            value.y.ToString("0.###", CultureInfo.InvariantCulture),
            value.z.ToString("0.###", CultureInfo.InvariantCulture));

    private static string PathOf(Transform value, Transform root)
    {
        if (value == root) return ".";
        var segments = new Stack<string>();
        var current = value;
        while (current && current != root)
        {
            segments.Push(current.name);
            current = current.parent;
        }
        return current == root ? string.Join("/", segments) : value.name;
    }

    private static string Safe(string value) =>
        new string(value.Where(char.IsLetterOrDigit).ToArray());

    private readonly record struct CensusResult(bool Pass, string Text);
}
