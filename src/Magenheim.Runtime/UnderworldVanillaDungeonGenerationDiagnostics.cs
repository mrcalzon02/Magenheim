using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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

    internal static void Configure(ManualLogSource log) =>
        _log = log ?? throw new ArgumentNullException(nameof(log));

    internal static void Report(DungeonGenerator generator, int seed, ZoneSystem.SpawnMode mode)
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

        try
        {
            var rooms = generator.GetComponentsInChildren<Room>(true)
                .Where(room => room && room.gameObject != generator.gameObject)
                .ToArray();
            var placedNames = rooms
                .Select(room => Utils.GetPrefabName(room.gameObject))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToHashSet(StringComparer.Ordinal);

            var required = generator.m_requiredRooms ?? new List<string>();
            var missingRequired = required
                .Where(name => !placedNames.Contains(name))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            var metrics = GraphMetrics(rooms);
            var creatureSpawners = generator.GetComponentsInChildren<CreatureSpawner>(true).Length;
            var spawnAreas = generator.GetComponentsInChildren<SpawnArea>(true).Length;
            var containers = generator.GetComponentsInChildren<Container>(true).Length;
            var pickables = generator.GetComponentsInChildren<Pickable>(true).Length;
            var mineables =
                generator.GetComponentsInChildren<MineRock>(true).Length +
                generator.GetComponentsInChildren<MineRock5>(true).Length;
            var destructibleDrops = generator.GetComponentsInChildren<DropOnDestroyed>(true).Length;

            var result =
                $"DDE GENERATED {profile.Biome}: seed={seed} mode={mode} " +
                $"rooms={rooms.Length} target={generator.m_minRooms}-{generator.m_maxRooms} " +
                $"required={required.Count - missingRequired.Length}/{required.Count} " +
                $"branch-depth={metrics.MaximumDepth} components={metrics.Components} " +
                $"connections={metrics.ConnectionEdges} dead-ends={metrics.DeadEnds} " +
                $"creature-spawners={creatureSpawners} spawn-areas={spawnAreas} " +
                $"containers={containers} pickables={pickables} mineables={mineables} " +
                $"destructible-drops={destructibleDrops}";

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
                spawnAreas,
                containers,
                pickables,
                mineables,
                destructibleDrops);
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
        int spawnAreas,
        int containers,
        int pickables,
        int mineables,
        int destructibleDrops)
    {
        var root = Path.Combine(
            Paths.ConfigPath,
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
        text.AppendLine("spawn_areas=" + spawnAreas.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("containers=" + containers.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("pickables=" + pickables.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("mineables=" + mineables.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("destructible_drops=" + destructibleDrops.ToString(CultureInfo.InvariantCulture));
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
    private static void Postfix(
        DungeonGenerator __instance,
        int seed,
        ZoneSystem.SpawnMode mode) =>
        UnderworldVanillaDungeonGenerationDiagnostics.Report(__instance, seed, mode);
}
