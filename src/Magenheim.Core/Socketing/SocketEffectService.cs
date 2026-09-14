using System;
using System.Collections.Generic;
using System.Linq;

namespace Magenheim.Core.Socketing;

public enum SocketEffectKind
{
    BluntDamage = 0,
    FireDamage = 1,
    FrostDamage = 2,
    LightningDamage = 3,
    PoisonDamage = 4,
    SpiritDamage = 5,
    Armor = 6,
    Knockback = 7,
    Stagger = 8,
    KnockbackResistance = 9,
    CarryWeight = 10,
    MiningEfficiency = 11,
    MovementSpeed = 12,
    StaminaRegeneration = 13,
    EitrRegeneration = 14,
    HealthRegeneration = 15,
    Illumination = 16
}

public sealed record SocketEffectRule(
    ElementalAlignment Element,
    EquipmentCategory Category,
    SocketEffectKind Effect,
    double SimpleMagnitude)
{
    public void Validate()
    {
        if (!Enum.IsDefined(typeof(ElementalAlignment), Element))
            throw new InvalidOperationException($"Unknown socket-effect element value '{(int)Element}'.");
        if (!Enum.IsDefined(typeof(EquipmentCategory), Category) || Category == EquipmentCategory.Unknown)
            throw new InvalidOperationException($"Socket-effect rules require a known equipment category, not '{Category}'.");
        if (!Enum.IsDefined(typeof(SocketEffectKind), Effect))
            throw new InvalidOperationException($"Unknown socket-effect kind value '{(int)Effect}'.");
        if (double.IsNaN(SimpleMagnitude) || double.IsInfinity(SimpleMagnitude))
            throw new InvalidOperationException("Socket-effect magnitude must be finite.");
    }
}

public sealed class SocketEffectDefinitionSet
{
    private readonly SocketEffectRule[] _rules;

    public SocketEffectDefinitionSet(IEnumerable<SocketEffectRule> rules)
    {
        if (rules is null) throw new ArgumentNullException(nameof(rules));
        _rules = rules.ToArray();

        var identities = new HashSet<string>(StringComparer.Ordinal);
        foreach (var rule in _rules)
        {
            if (rule is null)
                throw new InvalidOperationException("Socket-effect definitions cannot contain null rules.");
            rule.Validate();

            var identity = $"{(int)rule.Element}:{(int)rule.Category}:{(int)rule.Effect}";
            if (!identities.Add(identity))
            {
                throw new InvalidOperationException(
                    $"Duplicate socket-effect rule for {rule.Element}/{rule.Category}/{rule.Effect}.");
            }
        }
    }

    public IReadOnlyList<SocketEffectRule> Rules => _rules;

    public static double TierScalar(CrystalTier tier)
    {
        return tier switch
        {
            CrystalTier.Simple => 1.00d,
            CrystalTier.Crystal => 1.50d,
            CrystalTier.Advanced => 2.25d,
            CrystalTier.Master => 3.50d,
            CrystalTier.Rough => throw new InvalidOperationException(
                "Rough crystals have no socket-effect scalar because they are not socketable."),
            _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, "Unknown crystal tier.")
        };
    }
}

public enum SocketEffectCalculationOutcome
{
    Success = 0,
    UnknownEquipmentCategory = 1,
    InvalidSocketState = 2
}

public sealed record SocketEffectCalculationResult(
    SocketEffectCalculationOutcome Outcome,
    IReadOnlyDictionary<SocketEffectKind, double> Effects,
    string Diagnostic)
{
    public bool IsSuccess => Outcome == SocketEffectCalculationOutcome.Success;

    public double Get(SocketEffectKind effect) =>
        Effects.TryGetValue(effect, out var value) ? value : 0d;
}

/// <summary>
/// Pure additive effect calculator. It consumes per-item socket state plus validated
/// effect definitions and returns channel magnitudes without touching shared prefabs.
/// Runtime adapters decide how each channel maps into current Valheim APIs.
/// </summary>
public static class SocketEffectService
{
    public static SocketEffectCalculationResult Calculate(
        SocketState state,
        EquipmentCategory category,
        SocketEffectDefinitionSet definitions)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (definitions is null) throw new ArgumentNullException(nameof(definitions));

        if (!Enum.IsDefined(typeof(EquipmentCategory), category) || category == EquipmentCategory.Unknown)
        {
            return new SocketEffectCalculationResult(
                SocketEffectCalculationOutcome.UnknownEquipmentCategory,
                new Dictionary<SocketEffectKind, double>(),
                "Socket effects are not applied when equipment classification is unknown.");
        }

        var totals = new Dictionary<SocketEffectKind, double>();
        foreach (var crystal in state.InstalledCrystals)
        {
            if (crystal.Tier == CrystalTier.Rough)
            {
                return new SocketEffectCalculationResult(
                    SocketEffectCalculationOutcome.InvalidSocketState,
                    new Dictionary<SocketEffectKind, double>(),
                    "Socket metadata contains a Rough crystal, which cannot be socketed safely.");
            }

            double scalar;
            try
            {
                scalar = SocketEffectDefinitionSet.TierScalar(crystal.Tier);
            }
            catch (Exception exception)
            {
                return new SocketEffectCalculationResult(
                    SocketEffectCalculationOutcome.InvalidSocketState,
                    new Dictionary<SocketEffectKind, double>(),
                    $"Socket metadata contains an unsupported tier: {exception.Message}");
            }

            foreach (var rule in definitions.Rules)
            {
                if (rule.Element != crystal.Element || rule.Category != category)
                    continue;

                var amount = rule.SimpleMagnitude * scalar;
                if (totals.TryGetValue(rule.Effect, out var existing))
                    totals[rule.Effect] = existing + amount;
                else
                    totals.Add(rule.Effect, amount);
            }
        }

        return new SocketEffectCalculationResult(
            SocketEffectCalculationOutcome.Success,
            totals,
            $"Calculated {totals.Count} socket-effect channel(s) for {state.InstalledCrystals.Count} installed crystal(s) on {category} equipment.");
    }
}
