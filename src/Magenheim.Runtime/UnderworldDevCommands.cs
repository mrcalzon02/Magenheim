using System;
using System.Collections.Generic;
using System.Linq;
using Magenheim.Core.Underworld;
using BepInEx.Logging;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Developer console access to the Underworld: <c>magenheim_underworld enter|return|status|audit|survey|dungeons|donors</c>.
/// </summary>
/// <remarks>
/// A cheat command, so Valheim's own <c>devcommands</c> gate applies. It is not a second transit
/// system: <c>enter</c> and <c>return</c> call <see cref="UnderworldGateTransitRuntime"/>, the same path
/// the Deep Gate uses, so layer switching, return anchors and logging are identical. The only
/// differences are the ones a tester needs: <c>enter</c> skips the progression unlock (reaching the
/// Underworld legitimately means finishing the game), and <c>return</c> falls back to the player's bed
/// or home point when no return anchor exists, for example after relogging while below.
/// Documented in TESTING.md.
/// </remarks>
internal sealed class UnderworldDevCommands : ConsoleCommand
{
    private static bool _registered;
    private static UnderworldRuntimeServices? _services;
    private static ManualLogSource? _log;

    internal static void Register(UnderworldRuntimeServices services, ManualLogSource log)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _log = log;
        if (_registered) return;
        CommandManager.Instance.AddConsoleCommand(new UnderworldDevCommands());
        _registered = true;
    }

    public override string Name => "magenheim_underworld";
    public override string Help => "enter | return | status | audit | survey | dungeons | donors -- transit/status, admission audit, terrain/dungeon audit, or Deep Dungeon donor census (devcommands)";
    public override bool IsCheat => true;
    public override List<string> CommandOptionList() => new() { "enter", "return", "status", "audit", "survey", "dungeons", "donors" };

    public override void Run(string[] args)
    {
        var verb = args.Length > 0 ? args[0].ToLowerInvariant() : "status";
        var player = Player.m_localPlayer;
        if (player is null)
        {
            Say("No local player; load a world first.");
            return;
        }
        switch (verb)
        {
            case "enter":
                Report(UnderworldGateTransitRuntime.TryTransit(UnderworldGateRole.EnterUnderworld, player, out var entered, ignoreProgression: true),
                       "Entered the Underworld.", entered);
                break;
            case "return":
                if (UnderworldGateTransitRuntime.HasReturnAnchor(player))
                {
                    Report(UnderworldGateTransitRuntime.TryTransit(UnderworldGateRole.ReturnToSurface, player, out var returned),
                           "Returned to the Surface.", returned);
                    break;
                }
                var profile = Game.instance?.GetPlayerProfile();
                if (profile is null)
                {
                    Say("No return anchor and no player profile to fall back on.");
                    break;
                }
                var fallback = profile.HaveCustomSpawnPoint() ? profile.GetCustomSpawnPoint() : profile.GetHomePoint();
                Report(UnderworldGateTransitRuntime.TryReturnTo(player, fallback, out var fell),
                       $"No return anchor; returned to the Surface at your {(profile.HaveCustomSpawnPoint() ? "bed" : "home point")}.", fell);
                break;
            case "status":
                Say(UnderworldGateTransitRuntime.Describe(player));
                break;
            case "audit":
                RunAudit();
                break;
            case "survey":
                RunSurvey(player, args);
                break;
            case "dungeons":
                RunDungeonAudit();
                break;
            case "donors":
                RunDonorCensus();
                break;
            default:
                Say($"Unknown option '{verb}'. Use: {Name} enter | return | status | audit | survey | dungeons | donors");
                break;
        }
    }

    private static void RunAudit()
    {
        var services = _services;
        if (services is null)
        {
            Say("AUDIT FAIL: Underworld runtime services are unavailable.");
            return;
        }

        var identity = services.InstanceLifecycle.Identity;
        if (identity is null)
        {
            Say("AUDIT FAIL: no admitted Underworld world identity.");
            return;
        }

        try
        {
            var proof = UnderworldInstanceAdmissionAudit.Validate(services, identity);
            Say("AUDIT PASS: " + proof);
        }
        catch (Exception exception)
        {
            _log?.LogError("magenheim_underworld audit failed: " + exception);
            Say("AUDIT FAIL: " + exception.Message);
        }
    }

    private static void RunDonorCensus()
    {
        try
        {
            var index = UnderworldVanillaDungeonDonorCensus.CaptureAll(_log);
            Say("DDE-01 DONOR CENSUS PASS: " + index);
        }
        catch (Exception exception)
        {
            _log?.LogError("Deep Dungeon donor census failed: " + exception);
            Say("DDE-01 DONOR CENSUS FAIL: " + exception.Message);
        }
    }

    private static void RunDungeonAudit()
    {
        Say(
            "DUNGEONS state=" + UnderworldDungeonPlacementRuntime.State +
            " -- " + UnderworldDungeonPlacementRuntime.Diagnostic);

        foreach (var report in UnderworldDungeonPlacementRuntime.LastReport)
        {
            var spacing = double.IsPositiveInfinity(report.MinimumObservedSpacingMeters)
                ? "n/a"
                : report.MinimumObservedSpacingMeters.ToString(
                    "0",
                    System.Globalization.CultureInfo.InvariantCulture) + "m";
            Say(
                $"DUNGEONS {report.PrefabName}: {report.Found}/{report.Expected} " +
                $"biome={report.Biome} min-spacing={spacing}");
        }
    }

    private static void RunSurvey(Player player, string[] args)
    {
        var services = _services;
        if (services is null || services.InstanceLifecycle.Identity is not { } identity)
        {
            Say("SURVEY FAIL: Underworld instance authority is unavailable.");
            return;
        }
        if (!services.WorldInstances.TryGetPlayerInstance(player.GetPlayerID(), out var instance) ||
            !instance.IsUnderworld)
        {
            Say("SURVEY REFUSED: enter the Underworld first so the sample uses actual instance-local coordinates.");
            return;
        }

        var radius = 512f;
        if (args.Length > 1 && float.TryParse(args[1], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var requested))
            radius = Mathf.Clamp(requested, 128f, 1600f);
        const int steps = 8;
        var spacing = radius / steps;
        var origin = player.transform.position;
        var byBiome = new Dictionary<UnderworldTerrainBiome, List<double>>();
        var rareMassifSamples = 0;
        var admitted = 0;
        for (var iz = -steps; iz <= steps; iz++)
        for (var ix = -steps; ix <= steps; ix++)
        {
            var x = origin.x + ix * spacing;
            var z = origin.z + iz * spacing;
            var sample = UnderworldTerrainRuntime.SampleForDiagnostics(x, z);
            if (!sample.Admitted) continue;
            admitted++;
            if (!byBiome.TryGetValue(sample.Biome, out var heights))
                byBiome.Add(sample.Biome, heights = new List<double>());
            heights.Add(sample.Height);
            if (UnderworldTerrainLifecycle.RareCellMassifLiftAt(
                    services.TerrainDomain,
                    identity.DerivedSeed32,
                    x,
                    z) > 1d)
                rareMassifSamples++;
        }

        Say($"SURVEY terrain: center=({origin.x:0},{origin.z:0}) radius={radius:0}m grid={(steps * 2 + 1)}x{(steps * 2 + 1)} admitted={admitted} rare-cell-massif-samples={rareMassifSamples}.");
        foreach (var pair in byBiome.OrderBy(p => p.Key.ToString(), StringComparer.Ordinal))
        {
            pair.Value.Sort();
            var values = pair.Value;
            var median = values[values.Count / 2];
            Say($"SURVEY biome {pair.Key}: n={values.Count} y={values[0]:0.0}..{values[values.Count - 1]:0.0}m median={median:0.0}m.");
        }

        var scene = player.gameObject.scene.handle;
        var structures = Resources.FindObjectsOfTypeAll<UnderworldNativeStructureLocationRuntime>()
            .Where(value => value && value.gameObject.scene.handle == scene)
            .Select(value => new
            {
                Runtime = value,
                Distance = Vector2.Distance(
                    new Vector2(origin.x, origin.z),
                    new Vector2(value.transform.position.x, value.transform.position.z))
            })
            .Where(value => value.Distance <= radius)
            .OrderBy(value => value.Distance)
            .ToArray();
        Say($"SURVEY structures: {structures.Length} loaded native Magenheim locations within {radius:0}m.");
        foreach (var entry in structures.Take(16))
            Say($"SURVEY structure {entry.Runtime.FamilyKind} distance={entry.Distance:0}m composed={entry.Runtime.IsComposed} at ({entry.Runtime.transform.position.x:0},{entry.Runtime.transform.position.z:0}).");
        if (structures.Length > 16) Say($"SURVEY structures: {structures.Length - 16} additional locations omitted from console detail.");

        var inhabitants = Character.GetAllCharacters()
            .Where(character => character != null && character != player && !character.IsDead() &&
                                character.gameObject.scene.handle == scene)
            .Select(character => new
            {
                Character = character,
                Distance = Vector2.Distance(
                    new Vector2(origin.x, origin.z),
                    new Vector2(character.transform.position.x, character.transform.position.z))
            })
            .Where(value => value.Distance <= Mathf.Min(radius, 500f))
            .GroupBy(value => value.Character.m_name ?? value.Character.name)
            .Select(group => new { Name = group.Key, Count = group.Count(), Nearest = group.Min(value => value.Distance) })
            .OrderBy(value => value.Nearest)
            .ToArray();
        Say($"SURVEY inhabitants: {inhabitants.Sum(value => value.Count)} living non-player characters within {Mathf.Min(radius, 500f):0}m across {inhabitants.Length} types.");
        foreach (var entry in inhabitants.Take(16))
            Say($"SURVEY inhabitant {entry.Name}: count={entry.Count}, nearest={entry.Nearest:0}m.");
    }

    private static void Report(bool ok, string success, string diagnostic) => Say(ok ? success : "Refused: " + diagnostic);

    private static void Say(string text)
    {
        Console.instance?.Print(text);
        _log?.LogInfo("magenheim_underworld: " + text);
    }
}
