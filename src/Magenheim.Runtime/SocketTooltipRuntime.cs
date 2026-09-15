using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using HarmonyLib;
using Magenheim.Core.Definitions;
using Magenheim.Core.Socketing;

namespace Magenheim.Runtime;

/// <summary>
/// Player-facing socket inspection. Socket metadata already persists per item instance;
/// this layer makes the installed crystals and resolved elemental bonuses visible anywhere
/// Valheim renders that item's normal tooltip.
/// </summary>
internal static class SocketTooltipRuntime
{
    private static SocketEffectDefinitionSet? _definitions;

    internal static void Configure(MagenheimDefinitionSet definitions)
    {
        if (definitions is null) throw new ArgumentNullException(nameof(definitions));
        _definitions = definitions.SocketEffects;
    }

    internal static string Append(ItemDrop.ItemData item, string original)
    {
        if (item is null || _definitions is null) return original;
        var customData = item.m_customData;
        if (customData is null || !customData.ContainsKey(SocketMetadataCodec.CustomDataKey))
            return original;

        if (!ItemSocketAdapter.TryRead(item, out var state, out var metadataError))
            return original + "\n\n<color=#d97070>Magenheim socket data invalid: " + metadataError + "</color>";

        var builder = new StringBuilder(original);
        builder.Append("\n\n<color=#d8bd78><b>Magenheim Sockets</b></color>");
        builder.Append("\nSlots: ").Append(state.InstalledCrystals.Count).Append('/').Append(state.UnlockedSlots);

        if (state.InstalledCrystals.Count == 0)
        {
            builder.Append("\n<color=#aaaaaa>Empty socket</color>");
            return builder.ToString();
        }

        for (var index = 0; index < state.InstalledCrystals.Count; index++)
        {
            var crystal = state.InstalledCrystals[index];
            builder.Append("\n").Append(index + 1).Append(". ")
                .Append(crystal.Tier).Append(' ').Append(crystal.Element).Append(" Crystal");
        }

        var category = ItemSocketAdapter.Classify(item);
        if (category == EquipmentCategory.Unknown)
            return builder.Append("\n<color=#aaaaaa>Effects unavailable for unknown equipment category.</color>").ToString();

        var resolved = SocketEffectService.Calculate(state, category, _definitions);
        if (!resolved.IsSuccess)
            return builder.Append("\n<color=#d97070>").Append(resolved.Diagnostic).Append("</color>").ToString();

        var effects = resolved.Effects
            .Where(entry => Math.Abs(entry.Value) > 0.0000001d)
            .OrderBy(entry => (int)entry.Key)
            .ToArray();
        if (effects.Length == 0)
            return builder.Append("\n<color=#aaaaaa>No configured effect for this equipment category.</color>").ToString();

        builder.Append("\n<color=#9fcf9f>Resolved bonuses:</color>");
        foreach (var effect in effects)
            builder.Append("\n  ").Append(FormatEffect(effect.Key, effect.Value));
        return builder.ToString();
    }

    private static string FormatEffect(SocketEffectKind kind, double value)
    {
        var sign = value >= 0d ? "+" : string.Empty;
        switch (kind)
        {
            case SocketEffectKind.Knockback:
            case SocketEffectKind.Stagger:
            case SocketEffectKind.KnockbackResistance:
            case SocketEffectKind.MiningEfficiency:
            case SocketEffectKind.MovementSpeed:
            case SocketEffectKind.StaminaRegeneration:
            case SocketEffectKind.EitrRegeneration:
            case SocketEffectKind.HealthRegeneration:
                return $"{DisplayName(kind)} {sign}{value * 100d:0.#}%";
            default:
                return $"{DisplayName(kind)} {sign}{value.ToString("0.##", CultureInfo.InvariantCulture)}";
        }
    }

    private static string DisplayName(SocketEffectKind kind) => kind switch
    {
        SocketEffectKind.BluntDamage => "Blunt damage",
        SocketEffectKind.FireDamage => "Fire damage",
        SocketEffectKind.FrostDamage => "Frost damage",
        SocketEffectKind.LightningDamage => "Lightning damage",
        SocketEffectKind.PoisonDamage => "Poison damage",
        SocketEffectKind.SpiritDamage => "Spirit damage",
        SocketEffectKind.Armor => "Armor",
        SocketEffectKind.Knockback => "Knockback",
        SocketEffectKind.Stagger => "Stagger",
        SocketEffectKind.KnockbackResistance => "Knockback resistance",
        SocketEffectKind.CarryWeight => "Carry weight",
        SocketEffectKind.MiningEfficiency => "Mining efficiency",
        SocketEffectKind.MovementSpeed => "Movement speed",
        SocketEffectKind.StaminaRegeneration => "Stamina regeneration",
        SocketEffectKind.EitrRegeneration => "Eitr regeneration",
        SocketEffectKind.HealthRegeneration => "Health regeneration",
        SocketEffectKind.Illumination => "Illumination",
        _ => kind.ToString()
    };
}

[HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip),
    typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool))]
internal static class SocketTooltipPatch
{
    private static void Postfix(ItemDrop.ItemData item, ref string __result) =>
        __result = SocketTooltipRuntime.Append(item, __result);
}
