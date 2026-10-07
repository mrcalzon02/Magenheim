using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace Magenheim.Runtime;

/// <summary>Scope the native crown response to the observing creature's actual world.</summary>
[HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
internal static class WorldRoyaltyRuntime
{
    internal static bool Applies(bool crowned, bool nullMantle, bool underworldEnemy) =>
        crowned && nullMantle == underworldEnemy;

    internal static bool InCrownModeForEnemy(Player player, MonsterAI enemy)
    {
        if (!player || !enemy) return false;
        var view = player.GetComponent<ZNetView>();
        var helmet = view && view.IsValid()
            ? view.GetZDO().GetInt(ZDOVars.s_helmetItem, 0) : 0;
        var mantle = helmet == NowhereKingRewardRegistrar.NullMantlePrefabName.GetStableHashCode();
        var underworld = ValheimWorldInstanceExecution.TryGetContextForScene(
            enemy.gameObject.scene.handle, out var context) && context != null && context.InstanceId.IsUnderworld;
        return Applies(player.InCrownMode(), mantle, underworld);
    }

    // Replace only the native fear decision. Keep native targeting, range, faction exclusions,
    // flight and the synchronized crowned flag intact; never temporarily change player state.
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var original = AccessTools.Method(typeof(Player), nameof(Player.InCrownMode));
        var replacement = AccessTools.Method(typeof(WorldRoyaltyRuntime), nameof(InCrownModeForEnemy),
            new[] { typeof(Player), typeof(MonsterAI) });
        var found = 0;
        foreach (var instruction in instructions)
        {
            if (!instruction.Calls(original)) { yield return instruction; continue; }
            found++;
            var loadEnemy = new CodeInstruction(OpCodes.Ldarg_0);
            loadEnemy.labels.AddRange(instruction.labels);
            loadEnemy.blocks.AddRange(instruction.blocks);
            yield return loadEnemy;
            yield return new CodeInstruction(OpCodes.Call, replacement);
        }
        if (found != 1)
            throw new InvalidOperationException($"Expected one native MonsterAI crown decision, found {found}.");
    }
}
