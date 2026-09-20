using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Reports any Magenheim build or craft requirement that names a prefab the game does not have.
/// </summary>
/// <remarks>
/// A requirement naming a prefab that does not exist does not fail. Jotunn logs one
/// `MockResolveFailure` warning and carries on, leaving `Requirement.m_resItem` null, and the piece
/// registers and places and looks entirely correct.
///
/// It is not correct. Live play against 0.0.85 found the Crystal Sentinel refunding its build cost
/// on every hammer strike without ever being removed -- an unbounded resource duplication. The
/// cause was `Cost("RefinedEitr", 8)`: Valheim's refined eitr item is `Eitr`, and `RefinedEitr` is
/// not a prefab. `WearNTear.Destroy` calls `Piece.DropResources`, which instantiates each
/// requirement in turn, throws `ArgumentException` on the null one, and never reaches the
/// `ZNetScene.Destroy` at the end of its own caller. The resources were already on the ground.
/// The same typo class had also reached the Crystal Bed, and `CoreWood` -- Valheim's core wood is
/// `RoundLog` -- had reached the Earth Simple staff.
///
/// The warning that would have caught all three was already being printed, nine times, into a
/// 50,000-line log nobody reads. So this states the failure in Magenheim's own voice, at error
/// level, and reports clean when there is nothing to say so that silence is never ambiguous.
///
/// It reads the registered pieces and recipes rather than the call sites, which is what lets it
/// cover every requirement however it was built -- but it also means the wanted prefab name is
/// already gone by the time it looks, since an unresolved requirement keeps no record of what it
/// asked for. The position within the piece's own cost list is reported instead, which lands on the
/// exact `Cost(...)` line in the registrar because that list is built in order.
///
/// It deliberately does not throw: an exception here would take every registrar behind it down with
/// it, which is a far worse outcome than a wrong build cost.
/// </remarks>
internal static class RequirementResolutionAudit
{
    private const string Prefix = "Magenheim_";

    private static bool _armed;
    private static ManualLogSource? _log;

    internal static void Arm(ManualLogSource log)
    {
        if (_armed) return;
        _log = log ?? throw new ArgumentNullException(nameof(log));
        PrefabManager.OnPrefabsRegistered += Report;
        _armed = true;
    }

    private static void Report()
    {
        var log = _log;
        if (log is null) return;
        try
        {
            var failures = new List<string>();
            failures.AddRange(AuditPieces());
            failures.AddRange(AuditRecipes());

            if (failures.Count == 0)
            {
                log.LogInfo("Requirement audit: every Magenheim build and craft requirement resolved to a real prefab.");
                return;
            }

            log.LogError(
                $"Requirement audit: {failures.Count} Magenheim requirement(s) name a prefab this game does not have. " +
                "A piece built from one of these refunds its cost without being removed, because Piece.DropResources " +
                "throws before WearNTear.Destroy can remove it. Correct the prefab name at the source:");
            foreach (var failure in failures)
                log.LogError("  - " + failure);
        }
        catch (Exception exception)
        {
            log.LogWarning("Requirement audit failed: " + exception.Message);
        }
        finally
        {
            PrefabManager.OnPrefabsRegistered -= Report;
            _armed = false;
        }
    }

    private static IEnumerable<string> AuditPieces()
    {
        var scene = ZNetScene.instance;
        if (scene?.m_prefabs is null) yield break;

        foreach (var prefab in scene.m_prefabs)
        {
            if (!prefab || !prefab.name.StartsWith(Prefix, StringComparison.Ordinal)) continue;
            var piece = prefab.GetComponent<Piece>();
            if (piece?.m_resources is null) continue;

            foreach (var requirement in piece.m_resources)
            {
                if (requirement is null || requirement.m_resItem) continue;
                yield return $"piece {prefab.name}: build requirement #{Array.IndexOf(piece.m_resources, requirement)} is unresolved";
            }
        }
    }

    private static IEnumerable<string> AuditRecipes()
    {
        var database = ObjectDB.instance;
        if (database?.m_recipes is null) yield break;

        foreach (var recipe in database.m_recipes)
        {
            if (!recipe || recipe.m_resources is null) continue;
            var output = recipe.m_item ? recipe.m_item.name : recipe.name;
            if (!Owned(recipe.name) && !Owned(output)) continue;

            foreach (var requirement in recipe.m_resources)
            {
                if (requirement is null || requirement.m_resItem) continue;
                yield return $"recipe {recipe.name} -> {output}: requirement #{Array.IndexOf(recipe.m_resources, requirement)} is unresolved";
            }
        }
    }

    private static bool Owned(string? name) =>
        name is not null && name.StartsWith(Prefix, StringComparison.Ordinal);
}
