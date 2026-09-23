using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Developer console access to the Underworld: <c>magenheim_underworld enter|return|status</c>.
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
    private static ManualLogSource? _log;

    internal static void Register(ManualLogSource log)
    {
        _log = log;
        if (_registered) return;
        CommandManager.Instance.AddConsoleCommand(new UnderworldDevCommands());
        _registered = true;
    }

    public override string Name => "magenheim_underworld";
    public override string Help => "enter | return | status -- move to or from the Underworld through the Deep Gate path (devcommands)";
    public override bool IsCheat => true;
    public override List<string> CommandOptionList() => new() { "enter", "return", "status" };

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
            default:
                Say($"Unknown option '{verb}'. Use: {Name} enter | return | status");
                break;
        }
    }

    private static void Report(bool ok, string success, string diagnostic) => Say(ok ? success : "Refused: " + diagnostic);

    private static void Say(string text)
    {
        Console.instance?.Print(text);
        _log?.LogInfo("magenheim_underworld: " + text);
    }
}
