using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Utils;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Runtime evidence capture for Deep Dungeon Expansion candidate/RuntimeReady donor dungeons.
/// This observes completed generation only; it does not alter room placement or progression.
/// </summary>
internal static class UnderworldVanillaDungeonGenerationDiagnostics
{
    private static ManualLogSource? _log;
    private static readonly List<GenerationSummary> LatestSummaries = new();

    internal sealed record GenerationSummary(
        string DungeonId,
        UnderworldTerrainBiome Biome,
        int Seed,
        int GeneratedRooms,
        int TargetMinimum,
        int TargetMaximum,
        int MaximumDepth,
        int ActiveCreatureSpawners,
        int CreatureSpawnerSockets,
        int ActiveSpawnAreas,
        int SpawnAreaSockets,
        long GenerationMilliseconds,
        long ManagedMemoryDeltaBytes,
        bool StructuralPass);

    internal readonly record struct GenerationProbe(
        bool Active,
        long ManagedMemoryBefore,
        Stopwatch? Stopwatch);

    internal static IReadOnlyList<GenerationSummary> Latest => LatestSummaries.ToArray();

    internal static GenerationProbe Begin(DungeonGenerator generator)
    {
        if (!IsMagenheimGenerator(generator))
            return new GenerationProbe(false, 0L, null);
        return new GenerationProbe(
            true,
            GC.GetTotalMemory(false),
            Stopwatch.StartNew());
    }

    internal static void Configure(ManualLogSource log) =>
        _log = log ?? throw new ArgumentNullException(nameof(log));

    internal static void Report(
        DungeonGenerator generator,
        int seed,
        ZoneSystem.SpawnMode mode,
        GenerationProbe probe)
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

        probe.Stopwatch?.Stop();
        var generationMilliseconds = probe.Stopwatch?.ElapsedMilliseconds ?? -1L;
        var managedMemoryDelta = probe.Active
            ? GC.GetTotalMemory(false) - probe.ManagedMemoryBefore
            : 0L;

        try
        {
            var rooms = generator.GetComponentsInChildren<Room>(true)
                .Where(room => room && room.gameObject != generator.gameObject)
                .ToArray();
            var placedNames = new HashSet<string>(
                rooms.Select(room => Utils.GetPrefabName(room.gameObject))
                    .Where(name => !string.IsNullOrWhiteSpace(name)),
                StringComparer.Ordinal);

            var required = generator.m_requiredRooms ?? new List<string>();
            var missingRequired = required
                .Where(name => !placedNames.Contains(name))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            var metrics = GraphMetrics(rooms);
            var creatureSpawnerComponents = generator.GetComponentsInChildren<CreatureSpawner>(true);
            var creatureSpawners = creatureSpawnerComponents.Length;
            var activeCreatureSpawners = creatureSpawnerComponents.Count(value => value && value.enabled);
            var spawnAreaComponents = generator.GetComponentsInChildren<SpawnArea>(true);
            var spawnAreas = spawnAreaComponents.Length;
            var activeSpawnAreas = spawnAreaComponents.Count(value => value && value.enabled);
            var containers = generator.GetComponentsInChildren<Container>(true).Length;
            var pickables = generator.GetComponentsInChildren<Pickable>(true).Length;
            var mineables =
                generator.GetComponentsInChildren<MineRock>(true).Length +
                generator.GetComponentsInChildren<MineRock5>(true).Length;
            var destructibleDrops = generator.GetComponentsInChildren<DropOnDestroyed>(true).Length;
            var networkViews = generator.GetComponentsInChildren<ZNetView>(true);
            var validNetworkViews = networkViews.Count(value => value && value.IsValid());
            var fingerprint = GenerationFingerprint(rooms);

            var result =
                $"DDE GENERATED {profile.Biome}: seed={seed} mode={mode} " +
                $"rooms={rooms.Length} target={generator.m_minRooms}-{generator.m_maxRooms} " +
                $"required={required.Count - missingRequired.Length}/{required.Count} " +
                $"branch-depth={metrics.MaximumDepth} components={metrics.Components} " +
                $"connections={metrics.ConnectionEdges} dead-ends={metrics.DeadEnds} " +
                $"creature-spawners={activeCreatureSpawners}/{creatureSpawners} " +
                $"spawn-areas={activeSpawnAreas}/{spawnAreas} " +
                $"containers={containers} pickables={pickables} mineables={mineables} " +
                $"destructible-drops={destructibleDrops} " +
                $"znetviews={validNetworkViews}/{networkViews.Length} " +
                $"generation-ms={generationMilliseconds} managed-memory-delta={managedMemoryDelta} " +
                $"fingerprint={fingerprint}";

            if (missingRequired.Length > 0)
                result += " missing-required=[" + string.Join(",", missingRequired) + "]";

            _log?.LogInfo(result);
            WriteEvidence(
                profile,
                seed,
                mode,
                rooms.Length,
                generator,
                missingRequired,
                metrics,
                creatureSpawners,
                activeCreatureSpawners,
                spawnAreas,
                activeSpawnAreas,
                containers,
                pickables,
                mineables,
                destructibleDrops,
                networkViews.Length,
                validNetworkViews,
                generationMilliseconds,
                managedMemoryDelta,
                fingerprint);

            var structuralPass =
                rooms.Length >= generator.m_minRooms &&
                missingRequired.Length == 0 &&
                metrics.Components <= 1;
            LatestSummaries.RemoveAll(value =>
                string.Equals(value.DungeonId, profile.DungeonId, StringComparison.Ordinal));
            LatestSummaries.Add(new GenerationSummary(
                profile.DungeonId,
                profile.Biome,
                seed,
                rooms.Length,
                generator.m_minRooms,
                generator.m_maxRooms,
                metrics.MaximumDepth,
                activeCreatureSpawners,
                creatureSpawners,
                activeSpawnAreas,
                spawnAreas,
                generationMilliseconds,
                managedMemoryDelta,
                structuralPass));
        }
        catch (Exception exception)
        {
            _log?.LogWarning(
                $"DDE generation diagnostics failed for '{theme.m_themeName}': {exception}");
        }
    }

    private static void WriteEvidence(
        UnderworldVanillaDungeonReuseDefinition profile,
        int seed,
        ZoneSystem.SpawnMode mode,
        int roomCount,
        DungeonGenerator generator,
        IReadOnlyList<string> missingRequired,
        GraphReport graph,
        int creatureSpawners,
        int activeCreatureSpawners,
        int spawnAreas,
        int activeSpawnAreas,
        int containers,
        int pickables,
        int mineables,
        int destructibleDrops,
        int networkViews,
        int validNetworkViews,
        long generationMilliseconds,
        long managedMemoryDeltaBytes,
        string fingerprint)
    {
        var root = Path.Combine(
            BepInEx.Paths.ConfigPath,
            "Magenheim",
            "validation",
            "deep-dungeon-expansion",
            "generated");
        Directory.CreateDirectory(root);

        var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff", CultureInfo.InvariantCulture);
        var file = Path.Combine(
            root,
            stamp + "-" + Safe(profile.Biome.ToString()) + "-generation.txt");

        var text = new StringBuilder();
        text.AppendLine("Deep Dungeon Expansion generation evidence");
        text.AppendLine("utc=" + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        text.AppendLine("game=" + global::Version.GetVersionString());
        text.AppendLine("dungeon_id=" + profile.DungeonId);
        text.AppendLine("biome=" + profile.Biome);
        text.AppendLine("donor=" + profile.DonorDisplayName);
        text.AppendLine("seed=" + seed.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("spawn_mode=" + mode);
        text.AppendLine("room_scale=" + profile.LinearRoomScale.ToString("0.###", CultureInfo.InvariantCulture));
        text.AppendLine("room_count_multiplier=" + profile.RoomCountMultiplier.ToString("0.###", CultureInfo.InvariantCulture));
        text.AppendLine("generated_rooms=" + roomCount.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("target_min_rooms=" + generator.m_minRooms.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("target_max_rooms=" + generator.m_maxRooms.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("required_room_count=" + (generator.m_requiredRooms?.Count ?? 0).ToString(CultureInfo.InvariantCulture));
        text.AppendLine("missing_required_count=" + missingRequired.Count.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("missing_required_rooms=" + string.Join(",", missingRequired));
        text.AppendLine("graph_components=" + graph.Components.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("graph_maximum_depth=" + graph.MaximumDepth.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("graph_connection_edges=" + graph.ConnectionEdges.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("graph_dead_ends=" + graph.DeadEnds.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("creature_spawners=" + creatureSpawners.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("active_creature_spawners=" + activeCreatureSpawners.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("spawn_areas=" + spawnAreas.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("active_spawn_areas=" + activeSpawnAreas.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("containers=" + containers.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("pickables=" + pickables.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("mineables=" + mineables.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("destructible_drops=" + destructibleDrops.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("znetviews=" + networkViews.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("valid_znetviews=" + validNetworkViews.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("generation_fingerprint=" + fingerprint);
        text.AppendLine("generation_ms=" + generationMilliseconds.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("managed_memory_delta_bytes=" + managedMemoryDeltaBytes.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("candidate_mode=" + UnderworldVanillaDungeonCandidatePolicy.Enabled);
        text.AppendLine("result=" +
            (roomCount >= generator.m_minRooms &&
             missingRequired.Count == 0 &&
             graph.Components <= 1
                ? "STRUCTURAL_PASS"
                : "STRUCTURAL_FAIL"));

        File.WriteAllText(file, text.ToString(), new UTF8Encoding(false));
        _log?.LogInfo("DDE generation evidence -> '" + file + "'.");
    }

    internal static string CaptureRuntimeSnapshot(Player player, ManualLogSource? log)
    {
        if (!player) throw new ArgumentNullException(nameof(player));

        var candidates = Resources.FindObjectsOfTypeAll<DungeonGenerator>()
            .Where(generator =>
                generator &&
                generator.gameObject.activeInHierarchy &&
                generator.gameObject.scene.handle == player.gameObject.scene.handle)
            .Select(generator => new
            {
                Generator = generator,
                Profile = ProfileFor(generator),
                Distance = (generator.transform.position - player.transform.position).sqrMagnitude,
            })
            .Where(value => value.Profile is not null)
            .OrderBy(value => value.Distance)
            .ToArray();

        if (candidates.Length == 0)
            throw new InvalidOperationException(
                "No loaded expanded-vanilla Underworld dungeon is present in the local player's scene.");

        var selected = candidates[0];
        var generator = selected.Generator;
        var profile = selected.Profile!;
        var rooms = generator.GetComponentsInChildren<Room>(true)
            .Where(room => room && room.gameObject != generator.gameObject)
            .ToArray();
        var networkViews = generator.GetComponentsInChildren<ZNetView>(true);
        var creatureSpawners = generator.GetComponentsInChildren<CreatureSpawner>(true);
        var spawnAreas = generator.GetComponentsInChildren<SpawnArea>(true);
        var containers = generator.GetComponentsInChildren<Container>(true);
        var pickables = generator.GetComponentsInChildren<Pickable>(true);
        var mineables =
            generator.GetComponentsInChildren<MineRock>(true).Length +
            generator.GetComponentsInChildren<MineRock5>(true).Length;
        var destructibleDrops = generator.GetComponentsInChildren<DropOnDestroyed>(true);
        var net = ZNet.instance;
        var peerCount = net is null ? 0 : net.GetPeers().Count;
        var fingerprint = GenerationFingerprint(rooms);

        var root = Path.Combine(
            BepInEx.Paths.ConfigPath,
            "Magenheim",
            "validation",
            "deep-dungeon-expansion",
            "snapshots");
        Directory.CreateDirectory(root);
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff", CultureInfo.InvariantCulture);
        var file = Path.Combine(
            root,
            stamp + "-" + Safe(profile.Biome.ToString()) + "-runtime-snapshot.txt");

        var text = new StringBuilder();
        text.AppendLine("Deep Dungeon Expansion runtime snapshot");
        text.AppendLine("utc=" + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        text.AppendLine("game=" + global::Version.GetVersionString());
        text.AppendLine("dungeon_id=" + profile.DungeonId);
        text.AppendLine("biome=" + profile.Biome);
        text.AppendLine("donor=" + profile.DonorDisplayName);
        text.AppendLine("candidate_mode=" + UnderworldVanillaDungeonCandidatePolicy.Enabled);
        text.AppendLine("player_id=" + player.GetPlayerID().ToString(CultureInfo.InvariantCulture));
        text.AppendLine("scene_handle=" + player.gameObject.scene.handle.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("network_server=" + (net is not null && net.IsServer()));
        text.AppendLine("network_peer_count=" + peerCount.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("rooms=" + rooms.Length.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("generation_fingerprint=" + fingerprint);
        text.AppendLine("znetviews=" + networkViews.Length.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("valid_znetviews=" + networkViews.Count(value => value && value.IsValid()).ToString(CultureInfo.InvariantCulture));
        text.AppendLine("creature_spawners=" + creatureSpawners.Length.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("active_creature_spawners=" + creatureSpawners.Count(value => value && value.enabled).ToString(CultureInfo.InvariantCulture));
        text.AppendLine("spawn_areas=" + spawnAreas.Length.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("active_spawn_areas=" + spawnAreas.Count(value => value && value.enabled).ToString(CultureInfo.InvariantCulture));
        text.AppendLine("containers=" + containers.Length.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("pickables=" + pickables.Length.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("active_pickables=" + pickables.Count(value => value && value.gameObject.activeInHierarchy).ToString(CultureInfo.InvariantCulture));
        text.AppendLine("mineables=" + mineables.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("destructible_drops=" + destructibleDrops.Length.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("result=SNAPSHOT_CAPTURED");

        File.WriteAllText(file, text.ToString(), new UTF8Encoding(false));
        log?.LogInfo("DDE runtime snapshot -> '" + file + "'.");
        return file;
    }

    private static UnderworldVanillaDungeonReuseDefinition? ProfileFor(DungeonGenerator generator)
    {
        var theme = generator.GetComponent<DungeonGeneratorTheme>();
        if (!theme || string.IsNullOrWhiteSpace(theme.m_themeName)) return null;
        return UnderworldVanillaDungeonReuseCatalog.All.SingleOrDefault(candidate =>
            string.Equals(
                UnderworldVanillaDungeonRegistrar.ThemeName(candidate),
                theme.m_themeName,
                StringComparison.Ordinal));
    }

    private static string GenerationFingerprint(IEnumerable<Room> rooms)
    {
        var canonical = rooms
            .Where(room => room)
            .Select(room =>
                Utils.GetPrefabName(room.gameObject) + "|" +
                room.transform.position.x.ToString("0.00", CultureInfo.InvariantCulture) + "," +
                room.transform.position.y.ToString("0.00", CultureInfo.InvariantCulture) + "," +
                room.transform.position.z.ToString("0.00", CultureInfo.InvariantCulture) + "|" +
                room.transform.rotation.eulerAngles.y.ToString("0.0", CultureInfo.InvariantCulture))
            .OrderBy(value => value, StringComparer.Ordinal);
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n", canonical)));
        return string.Concat(bytes.Select(value =>
            value.ToString("x2", CultureInfo.InvariantCulture)));
    }

    private static GraphReport GraphMetrics(Room[] rooms)
    {
        if (rooms.Length == 0) return new GraphReport(0, 0, 0, 0);

        var connections = rooms
            .Select((room, roomIndex) => new
            {
                RoomIndex = roomIndex,
                Connections = room.GetComponentsInChildren<RoomConnection>(false)
            })
            .ToArray();

        var adjacency = Enumerable.Range(0, rooms.Length)
            .Select(_ => new HashSet<int>())
            .ToArray();
        var edges = 0;

        for (var left = 0; left < connections.Length; left++)
        {
            foreach (var a in connections[left].Connections)
            {
                if (!a) continue;
                for (var right = left + 1; right < connections.Length; right++)
                {
                    foreach (var b in connections[right].Connections)
                    {
                        if (!b ||
                            !string.Equals(a.m_type, b.m_type, StringComparison.Ordinal) ||
                            Vector3.Distance(a.transform.position, b.transform.position) >= 0.12f)
                            continue;

                        if (adjacency[left].Add(right))
                        {
                            adjacency[right].Add(left);
                            edges++;
                        }
                    }
                }
            }
        }

        var visited = new bool[rooms.Length];
        var components = 0;
        var maxDepth = 0;
        for (var start = 0; start < rooms.Length; start++)
        {
            if (visited[start]) continue;
            components++;
            var queue = new Queue<(int Room, int Depth)>();
            queue.Enqueue((start, 0));
            visited[start] = true;
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                maxDepth = Math.Max(maxDepth, current.Depth);
                foreach (var next in adjacency[current.Room])
                {
                    if (visited[next]) continue;
                    visited[next] = true;
                    queue.Enqueue((next, current.Depth + 1));
                }
            }
        }

        var deadEnds = adjacency.Count(neighbors => neighbors.Count <= 1);
        return new GraphReport(components, maxDepth, edges, deadEnds);
    }

    private static bool IsMagenheimGenerator(DungeonGenerator generator)
    {
        if (!generator) return false;
        var theme = generator.GetComponent<DungeonGeneratorTheme>();
        if (!theme || string.IsNullOrWhiteSpace(theme.m_themeName)) return false;
        return UnderworldVanillaDungeonReuseCatalog.All.Any(profile =>
            string.Equals(
                UnderworldVanillaDungeonRegistrar.ThemeName(profile),
                theme.m_themeName,
                StringComparison.Ordinal));
    }

    private static string Safe(string value) =>
        new string(value.Where(char.IsLetterOrDigit).ToArray());

    private readonly record struct GraphReport(
        int Components,
        int MaximumDepth,
        int ConnectionEdges,
        int DeadEnds);
}

[HarmonyPatch(
    typeof(DungeonGenerator),
    nameof(DungeonGenerator.Generate),
    new[] { typeof(int), typeof(ZoneSystem.SpawnMode) })]
internal static class UnderworldVanillaDungeonGenerationDiagnosticsPatch
{
    private static void Prefix(
        DungeonGenerator __instance,
        out UnderworldVanillaDungeonGenerationDiagnostics.GenerationProbe __state) =>
        __state = UnderworldVanillaDungeonGenerationDiagnostics.Begin(__instance);

    private static void Postfix(
        DungeonGenerator __instance,
        int seed,
        ZoneSystem.SpawnMode mode,
        UnderworldVanillaDungeonGenerationDiagnostics.GenerationProbe __state) =>
        UnderworldVanillaDungeonGenerationDiagnostics.Report(__instance, seed, mode, __state);
}
