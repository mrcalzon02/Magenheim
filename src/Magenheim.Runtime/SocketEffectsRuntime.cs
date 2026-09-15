using System;
using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using Magenheim.Core.Definitions;
using Magenheim.Core.Socketing;
using Magenheim.Runtime.Compatibility;

namespace Magenheim.Runtime;

/// <summary>
/// Runtime-only application boundary for socket effects. Balance remains in the pure core.
/// The selected equipment provider supplies crystal state; these adapters only translate
/// resolved channels into Valheim return values. Shared ItemDrop/SharedData prefabs are never mutated.
/// </summary>
internal static class SocketEffectsRuntime
{
    private static SocketEffectDefinitionSet? _definitions;
    private static ManualLogSource? _log;
    private static EquipmentSocketBackend _backend = EquipmentSocketBackend.Magenheim;
    private static IReadOnlyList<float> _resonanceMultipliers = new[] { 1f };

    internal static void Configure(
        MagenheimDefinitionSet definitions,
        ManualLogSource log,
        EquipmentSocketBackend backend = EquipmentSocketBackend.Magenheim,
        IReadOnlyList<float>? resonanceMultipliers = null)
    {
        if (definitions is null) throw new ArgumentNullException(nameof(definitions));
        _definitions = definitions.SocketEffects;
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _backend = backend;
        _resonanceMultipliers = resonanceMultipliers ?? new[] { 1f };
    }

    internal static void ApplyDamage(ItemDrop.ItemData item, ref HitData.DamageTypes damage)
    {
        if (!TryResolve(item, out var category, out var effects)) return;

        damage.m_blunt += ToFloat(effects.Get(SocketEffectKind.BluntDamage));
        damage.m_fire += ToFloat(effects.Get(SocketEffectKind.FireDamage));
        damage.m_frost += ToFloat(effects.Get(SocketEffectKind.FrostDamage));
        damage.m_lightning += ToFloat(effects.Get(SocketEffectKind.LightningDamage));
        damage.m_poison += ToFloat(effects.Get(SocketEffectKind.PoisonDamage));
        damage.m_spirit += ToFloat(effects.Get(SocketEffectKind.SpiritDamage));

        if (category == EquipmentCategory.Tool)
        {
            var efficiency = effects.Get(SocketEffectKind.MiningEfficiency);
            if (efficiency != 0d)
                damage.m_pickaxe *= Math.Max(0f, 1f + ToFloat(efficiency));
        }
    }

    internal static void ApplyArmor(ItemDrop.ItemData item, ref float armor)
    {
        if (!TryResolve(item, out var category, out var effects) || category != EquipmentCategory.Armor)
            return;
        armor += ToFloat(effects.Get(SocketEffectKind.Armor));
    }

    internal static void ApplyBlockPower(ItemDrop.ItemData item, ref float blockPower)
    {
        if (!TryResolve(item, out var category, out var effects) || category != EquipmentCategory.Shield)
            return;
        blockPower += ToFloat(effects.Get(SocketEffectKind.Armor));
    }

    internal static void ApplyCarryWeight(Player player, ref float maximumCarryWeight)
    {
        if (player is null || _definitions is null) return;
        foreach (var item in player.GetInventory().GetEquippedItems())
        {
            if (!TryResolve(item, out var category, out var effects) || category != EquipmentCategory.Utility)
                continue;
            maximumCarryWeight += ToFloat(effects.Get(SocketEffectKind.CarryWeight));
        }
    }

    private static bool TryResolve(ItemDrop.ItemData item, out EquipmentCategory category, out SocketEffectCalculationResult effects)
    {
        category = EquipmentCategory.Unknown;
        effects = new SocketEffectCalculationResult(
            SocketEffectCalculationOutcome.UnknownEquipmentCategory,
            new Dictionary<SocketEffectKind, double>(),
            "Socket effects are not configured.");

        if (item is null || _definitions is null)
            return false;

        category = ItemSocketAdapter.Classify(item);
        if (category == EquipmentCategory.Unknown)
            return false;

        if (_backend == EquipmentSocketBackend.Jewelcrafting)
        {
            if (_log is null || !JewelcraftingSocketReader.TryReadMagenheimCrystals(item, _log, out var crystals))
                return false;
            if (crystals.Count == 0)
                return false;

            effects = SocketEffectService.CalculateCrystals(crystals, category, _definitions, _resonanceMultipliers);
        }
        else
        {
            if (!ItemSocketAdapter.TryRead(item, out var state, out var metadataDiagnostic))
            {
                _log?.LogWarning($"Ignoring malformed Magenheim socket metadata on '{ItemIdentity(item)}': {metadataDiagnostic}");
                return false;
            }
            if (state.InstalledCrystals.Count == 0)
                return false;

            effects = SocketEffectService.Calculate(state, category, _definitions);
        }

        if (!effects.IsSuccess)
        {
            _log?.LogWarning($"Ignoring unresolved Magenheim socket effects on '{ItemIdentity(item)}': {effects.Diagnostic}");
            return false;
        }

        return true;
    }

    private static string ItemIdentity(ItemDrop.ItemData item) =>
        item.m_dropPrefab ? item.m_dropPrefab.name : item.m_shared.m_name;

    private static float ToFloat(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value > float.MaxValue || value < float.MinValue)
            throw new InvalidOperationException($"Socket-effect value '{value}' cannot be represented by the Valheim runtime.");
        return (float)value;
    }
}

[HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetDamage), typeof(int), typeof(float))]
internal static class SocketDamagePatch
{
    private static void Postfix(ItemDrop.ItemData __instance, ref HitData.DamageTypes __result) =>
        SocketEffectsRuntime.ApplyDamage(__instance, ref __result);
}

[HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetArmor), typeof(int), typeof(float))]
internal static class SocketArmorPatch
{
    private static void Postfix(ItemDrop.ItemData __instance, ref float __result) =>
        SocketEffectsRuntime.ApplyArmor(__instance, ref __result);
}

[HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetBlockPower), typeof(float))]
internal static class SocketBlockPowerPatch
{
    private static void Postfix(ItemDrop.ItemData __instance, ref float __result) =>
        SocketEffectsRuntime.ApplyBlockPower(__instance, ref __result);
}

[HarmonyPatch(typeof(Player), nameof(Player.GetMaxCarryWeight))]
internal static class SocketCarryWeightPatch
{
    private static void Postfix(Player __instance, ref float __result) =>
        SocketEffectsRuntime.ApplyCarryWeight(__instance, ref __result);
}
