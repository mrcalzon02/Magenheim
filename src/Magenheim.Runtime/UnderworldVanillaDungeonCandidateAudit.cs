using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using Jotunn.Managers;
using Jotunn.Utils;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Installed-game audit for one loaded expanded-vanilla Underworld dungeon.
/// Candidate mode may exercise a Planned family without altering promotion state.
/// </summary>
internal static class UnderworldVanillaDungeonCandidateAudit
{
    internal sealed record Result(bool Pass, string EvidenceFile, IReadOnlyList<string> Failures);

    internal static Result Capture(Player player, ManualLogSource? log)
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
        var failures = new List<string>();
        var notes = new List<string>();

        if (UnderworldVanillaDungeonCandidatePolicy.TryGetCandidate(out var admitted) &&
            !string.Equals(admitted.DungeonId, profile.DungeonId, StringComparison.Ordinal))
            failures.Add(
                "Loaded donor family does not match the single candidate admitted by startup policy.");

        AuditGenerator(profile, generator, failures, notes);
        AuditRooms(profile, generator, failures, notes);
        AuditEcology(profile, generator, failures, notes);
        AuditRewards(profile, generator, failures, notes);
        AuditLore(generator, failures, notes);
        AuditWorldgen(profile, failures, notes);

        var root = Path.Combine(
            BepInEx.Paths.ConfigPath,
            "Magenheim",
            "validation",
            "deep-dungeon-expansion",
            "audits");
        Directory.CreateDirectory(root);
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff", CultureInfo.InvariantCulture);
        var file = Path.Combine(
            root,
            stamp + "-" + Safe(profile.Biome.ToString()) + "-candidate-audit.txt");

        var text = new StringBuilder();
        text.AppendLine("Deep Dungeon Expansion candidate audit");
        text.AppendLine("utc=" + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        text.AppendLine("game=" + global::Version.GetVersionString());
        text.AppendLine("dungeon_id=" + profile.DungeonId);
        text.AppendLine("biome=" + profile.Biome);
        text.AppendLine("donor=" + profile.DonorDisplayName);
        text.AppendLine("candidate_policy=" + UnderworldVanillaDungeonCandidatePolicy.Describe());
        text.AppendLine("room_scale=" + profile.LinearRoomScale.ToString("0.###", CultureInfo.InvariantCulture));
        text.AppendLine("room_count_multiplier=" + profile.RoomCountMultiplier.ToString("0.###", CultureInfo.InvariantCulture));
        text.AppendLine("notes=" + notes.Count.ToString(CultureInfo.InvariantCulture));
        foreach (var note in notes)
            text.AppendLine("NOTE " + note);
        text.AppendLine("failures=" + failures.Count.ToString(CultureInfo.InvariantCulture));
        foreach (var failure in failures)
            text.AppendLine("FAIL " + failure);
        text.AppendLine("result=" + (failures.Count == 0 ? "PASS" : "FAIL"));

        File.WriteAllText(file, text.ToString(), new UTF8Encoding(false));
        log?.LogInfo(
            $"DDE candidate audit {profile.Biome}: {(failures.Count == 0 ? "PASS" : "FAIL")} -> '{file}'.");

        return new Result(failures.Count == 0, file, failures);
    }

    private static void AuditGenerator(
        UnderworldVanillaDungeonReuseDefinition profile,
        DungeonGenerator generator,
        ICollection<string> failures,
        ICollection<string> notes)
    {
        var theme = generator.GetComponent<DungeonGeneratorTheme>();
        var expectedTheme = UnderworldVanillaDungeonRegistrar.ThemeName(profile);
        if (!theme || !string.Equals(theme.m_themeName, expectedTheme, StringComparison.Ordinal))
            failures.Add("Generator is not bound to the expected private Magenheim theme '" + expectedTheme + "'.");

        if (generator.m_themes != Room.Theme.None)
            failures.Add("Generator still exposes a vanilla Room.Theme bitmask.");

        if (generator.m_minRooms < 1 || generator.m_maxRooms < generator.m_minRooms)
            failures.Add($"Expanded generator has invalid room target {generator.m_minRooms}-{generator.m_maxRooms}.");

        var required = generator.m_requiredRooms ?? new List<string>();
        var nonPrivateRequired = required
            .Where(name => !name.StartsWith("Magenheim_Underworld_VanillaRoom_", StringComparison.Ordinal))
            .ToArray();
        if (nonPrivateRequired.Length > 0)
            failures.Add("Required-room list still contains non-private identities: " +
                         string.Join(",", nonPrivateRequired));

        if (generator.m_doorTypes is not null)
        {
            var foreignDoors = generator.m_doorTypes
                .Where(value => value is not null && value.m_prefab)
                .Select(value => Utils.GetPrefabName(value.m_prefab))
                .Where(name =>
                    string.IsNullOrWhiteSpace(name) ||
                    !name.StartsWith("Magenheim_Underworld_DungeonDoor_", StringComparison.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (foreignDoors.Length > 0)
                failures.Add("Generator still references non-Magenheim door prefabs: " +
                             string.Join(",", foreignDoors));
        }

        notes.Add(
            $"generator rooms={generator.m_minRooms}-{generator.m_maxRooms} required={required.Count} " +
            $"doors={generator.m_doorTypes?.Count ?? 0}");
    }

    private static void AuditRooms(
        UnderworldVanillaDungeonReuseDefinition profile,
        DungeonGenerator generator,
        ICollection<string> failures,
        ICollection<string> notes)
    {
        var rooms = generator.GetComponentsInChildren<Room>(true)
            .Where(room => room && room.gameObject != generator.gameObject)
            .ToArray();

        if (rooms.Length < generator.m_minRooms)
            failures.Add($"Placed room count {rooms.Length} is below target minimum {generator.m_minRooms}.");

        const string privatePrefix = "Magenheim_Underworld_VanillaRoom_";
        var scale = (float)profile.LinearRoomScale;
        var audited = 0;

        foreach (var room in rooms)
        {
            var prefabName = Utils.GetPrefabName(room.gameObject);
            if (!prefabName.StartsWith(privatePrefix, StringComparison.Ordinal))
                failures.Add("Placed room is not a Magenheim private clone: " + prefabName);

            var metadata = room.GetComponent<UnderworldVanillaDungeonRoomAuditMetadata>();
            if (!metadata)
            {
                failures.Add("Placed room lacks donor provenance metadata: " + prefabName);
                continue;
            }

            audited++;
            if (!Mathf.Approximately(metadata.LinearScale, scale))
                failures.Add(
                    $"{prefabName}: provenance scale {metadata.LinearScale:0.###} != profile scale {scale:0.###}.");

            if (!Approximately(room.transform.localScale, Vector3.one, 0.001f))
                failures.Add(
                    prefabName + ": Room root itself is scaled; connection-safe DDE layout requires root scale 1.");

            var scaleRoot = room.transform.Cast<Transform>()
                .FirstOrDefault(child =>
                    string.Equals(child.name, "Magenheim_DDE_ScaleRoot", StringComparison.Ordinal));
            if (!scaleRoot)
                failures.Add(prefabName + ": missing Magenheim_DDE_ScaleRoot.");
            else if (!Approximately(scaleRoot.localScale, Vector3.one * scale, 0.001f))
                failures.Add(
                    $"{prefabName}: scale-root value {scaleRoot.localScale} does not match {scale:0.###}x.");

            var expectedSize = new Vector3Int(
                Mathf.CeilToInt(metadata.DonorRoomSize.x * scale),
                Mathf.CeilToInt(metadata.DonorRoomSize.y * scale),
                Mathf.CeilToInt(metadata.DonorRoomSize.z * scale));
            if (room.m_size != expectedSize)
                failures.Add(
                    $"{prefabName}: Room.m_size {room.m_size} != expected donor-scaled {expectedSize}.");

            var connections = room.GetConnections().Where(value => value).ToArray();
            if (connections.Length != metadata.DonorConnectionLocalPositions.Length)
                failures.Add(
                    $"{prefabName}: connection count {connections.Length} != donor count " +
                    metadata.DonorConnectionLocalPositions.Length + ".");

            foreach (var connection in connections)
                if (connection.transform.parent != room.transform)
                    failures.Add(prefabName + ": RoomConnection is no longer a direct Room child.");

            var expectedConnections = metadata.DonorConnectionLocalPositions
                .Select(value => value * scale)
                .ToList();
            foreach (var connection in connections)
            {
                var match = expectedConnections.FindIndex(value =>
                    Vector3.Distance(value, connection.transform.localPosition) <= 0.01f);
                if (match < 0)
                    failures.Add(
                        $"{prefabName}: scaled RoomConnection at {connection.transform.localPosition} " +
                        "has no donor-position match.");
                else
                    expectedConnections.RemoveAt(match);
            }

            if (expectedConnections.Count > 0)
                failures.Add(
                    $"{prefabName}: {expectedConnections.Count} donor connection position(s) " +
                    "were not represented after scaling.");
        }

        notes.Add($"rooms placed={rooms.Length} provenance-audited={audited}");
    }

    private static void AuditEcology(
        UnderworldVanillaDungeonReuseDefinition profile,
        DungeonGenerator generator,
        ICollection<string> failures,
        ICollection<string> notes)
    {
        var label = BiomeLabel(profile.Biome);
        var allowed = new HashSet<string>(
            UnderworldCreaturePrototypes.All
                .Where(entry =>
                    string.Equals(entry.Biome, label, StringComparison.Ordinal) &&
                    !(profile.Biome == UnderworldTerrainBiome.BlackwaterDeep &&
                      string.Equals(entry.Donor, "Serpent", StringComparison.Ordinal)))
                .Select(entry => entry.Prefab),
            StringComparer.Ordinal);

        var spawners = generator.GetComponentsInChildren<CreatureSpawner>(true);
        foreach (var spawner in spawners)
        {
            var name = spawner.m_creaturePrefab
                ? Utils.GetPrefabName(spawner.m_creaturePrefab)
                : string.Empty;
            if (!allowed.Contains(name))
                failures.Add("CreatureSpawner retains non-biome creature prefab '" + name + "'.");
        }

        var areas = generator.GetComponentsInChildren<SpawnArea>(true);
        foreach (var area in areas)
        foreach (var spawn in area.m_prefabs)
        {
            var name = spawn.m_prefab ? Utils.GetPrefabName(spawn.m_prefab) : string.Empty;
            if (!allowed.Contains(name))
                failures.Add("SpawnArea retains non-biome creature prefab '" + name + "'.");
        }

        notes.Add(
            $"ecology creature-spawners={spawners.Length} active={spawners.Count(value => value && value.enabled)} " +
            $"spawn-areas={areas.Length} active={areas.Count(value => value && value.enabled)}");
    }

    private static void AuditRewards(
        UnderworldVanillaDungeonReuseDefinition profile,
        DungeonGenerator generator,
        ICollection<string> failures,
        ICollection<string> notes)
    {
        var allowed = new HashSet<string>(
            UnderworldResourceCatalog.All
                .Where(resource => resource.Biome == profile.Biome)
                .Select(resource => resource.Prefab),
            StringComparer.Ordinal);

        var containers = generator.GetComponentsInChildren<Container>(true);
        foreach (var container in containers)
            AuditDropTable("Container", container.m_defaultItems, allowed, failures);

        var pickables = generator.GetComponentsInChildren<Pickable>(true);
        foreach (var pickable in pickables)
        {
            var name = pickable.m_itemPrefab
                ? Utils.GetPrefabName(pickable.m_itemPrefab)
                : string.Empty;
            if (!allowed.Contains(name))
                failures.Add("Pickable retains non-biome reward prefab '" + name + "'.");
        }

        var mines = generator.GetComponentsInChildren<MineRock>(true);
        foreach (var mine in mines)
            AuditDropTable("MineRock", mine.m_dropItems, allowed, failures);

        var mines5 = generator.GetComponentsInChildren<MineRock5>(true);
        foreach (var mine in mines5)
            AuditDropTable("MineRock5", mine.m_dropItems, allowed, failures);

        var destroyed = generator.GetComponentsInChildren<DropOnDestroyed>(true);
        foreach (var drop in destroyed)
            AuditDropTable("DropOnDestroyed", drop.m_dropWhenDestroyed, allowed, failures);

        notes.Add(
            $"rewards containers={containers.Length} pickables={pickables.Length} " +
            $"mineables={mines.Length + mines5.Length} destructible-drops={destroyed.Length}");
    }

    private static void AuditDropTable(
        string fixture,
        DropTable? table,
        HashSet<string> allowed,
        ICollection<string> failures)
    {
        if (table is null)
        {
            failures.Add(fixture + " has no rebound DropTable.");
            return;
        }

        foreach (var drop in table.m_drops)
        {
            var name = drop.m_item ? Utils.GetPrefabName(drop.m_item) : string.Empty;
            if (!allowed.Contains(name))
                failures.Add(fixture + " retains non-biome reward prefab '" + name + "'.");
        }
    }

    private static void AuditLore(
        DungeonGenerator generator,
        ICollection<string> failures,
        ICollection<string> notes)
    {
        var vegvisirs = generator.GetComponentsInChildren<Vegvisir>(true).Length;
        var runestones = generator.GetComponentsInChildren<RuneStone>(true).Length;
        if (vegvisirs > 0)
            failures.Add("Surface Vegvisir components remain in the derivative: " + vegvisirs + ".");
        if (runestones > 0)
            failures.Add("Surface Runestone components remain in the derivative: " + runestones + ".");
        notes.Add($"surface-lore vegvisirs={vegvisirs} runestones={runestones}");
    }

    private static void AuditWorldgen(
        UnderworldVanillaDungeonReuseDefinition profile,
        ICollection<string> failures,
        ICollection<string> notes)
    {
        var definition = UnderworldDungeonCatalog.All.Single(value =>
            string.Equals(value.Id, profile.DungeonId, StringComparison.Ordinal));
        var zone = ZoneManager.Instance.GetZoneLocation(definition.PrefabName);
        if (zone is null)
        {
            failures.Add("Expanded donor location is missing from ZoneManager.");
            return;
        }

        var expected = UnderworldTerrainRuntime.ToNativeBiome(profile.Biome);
        if (zone.m_biome != expected)
            failures.Add(
                $"Expanded donor location biome {zone.m_biome} != owning Underworld biome {expected}.");

        notes.Add(
            $"worldgen prefab={definition.PrefabName} biome={zone.m_biome} quantity={zone.m_quantity} " +
            $"min-distance={zone.m_minDistanceFromSimilar:0.###}");
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

    private static bool Approximately(Vector3 left, Vector3 right, float epsilon) =>
        Mathf.Abs(left.x - right.x) <= epsilon &&
        Mathf.Abs(left.y - right.y) <= epsilon &&
        Mathf.Abs(left.z - right.z) <= epsilon;

    private static string BiomeLabel(UnderworldTerrainBiome biome) => biome switch
    {
        UnderworldTerrainBiome.FungalForest => "Fungal Forest",
        UnderworldTerrainBiome.BlackwaterDeep => "Blackwater Deep",
        UnderworldTerrainBiome.SulfurousWastes => "Sulfurous Wastes",
        UnderworldTerrainBiome.FrozenCaverns => "Frozen Caverns",
        UnderworldTerrainBiome.GreatDecay => "Great Decay",
        _ => throw new InvalidOperationException(
            "Ordinary vanilla-reuse dungeon audit is undefined for " + biome + "."),
    };

    private static string Safe(string value) =>
        new string(value.Where(char.IsLetterOrDigit).ToArray());
}
