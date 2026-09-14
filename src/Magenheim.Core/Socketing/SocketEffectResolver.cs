using System;
using System.Collections.Generic;
using System.Linq;

namespace Magenheim.Core.Socketing;

/// <summary>
/// One namespaced effect contribution supplied by definition data. The core deliberately
/// does not interpret effect ids as Valheim stats; runtime adapters own that translation.
/// </summary>
public sealed record SocketEffectModifier(string EffectId, double Value)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(EffectId))
            throw new InvalidOperationException("Socket effect id is required.");
        if (!EffectId.StartsWith("magenheim.", StringComparison.Ordinal))
            throw new InvalidOperationException($"Socket effect id '{EffectId}' must use the magenheim. namespace.");
        if (double.IsNaN(Value) || double.IsInfinity(Value))
            throw new InvalidOperationException($"Socket effect '{EffectId}' value must be finite.");
    }
}

/// <summary>
/// Data authority for the behavior of one element/tier when installed in one equipment
/// category. Rough crystals are intentionally invalid because the socketing service does
/// not permit them to be installed.
/// </summary>
public sealed record SocketEffectDefinition(
    ElementalAlignment Element,
    CrystalTier Tier,
    EquipmentCategory Category,
    IReadOnlyList<SocketEffectModifier> Modifiers)
{
    public void Validate()
    {
        if (!Enum.IsDefined(typeof(ElementalAlignment), Element))
            throw new InvalidOperationException($"Unknown socket-effect element value '{(int)Element}'.");
        if (!Enum.IsDefined(typeof(CrystalTier), Tier))
            throw new InvalidOperationException($"Unknown socket-effect tier value '{(int)Tier}'.");
        if (Tier == CrystalTier.Rough)
            throw new InvalidOperationException("Rough crystals cannot define socket effects because they cannot be installed.");
        if (!Enum.IsDefined(typeof(EquipmentCategory), Category) || Category == EquipmentCategory.Unknown)
            throw new InvalidOperationException($"Socket effects require a concrete equipment category, not '{Category}'.");
        if (Modifiers is null || Modifiers.Count == 0)
            throw new InvalidOperationException("Socket effect definitions require at least one modifier.");

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var modifier in Modifiers)
        {
            if (modifier is null)
                throw new InvalidOperationException("Socket effect modifier cannot be null.");
            modifier.Validate();
            if (!seen.Add(modifier.EffectId))
                throw new InvalidOperationException(
                    $"Socket effect definition for {Element}/{Tier}/{Category} contains duplicate effect id '{modifier.EffectId}'.");
        }
    }
}

public enum SocketEffectResolutionOutcome
{
    Resolved = 0,
    NoInstalledCrystals = 1,
    UnknownEquipmentCategory = 2,
    MissingDefinition = 3,
    InvalidDefinitionSet = 4,
}

public sealed record ResolvedSocketEffect(string EffectId, double Value);

public sealed record SocketEffectResolution(
    SocketEffectResolutionOutcome Outcome,
    IReadOnlyList<ResolvedSocketEffect> Effects,
    string Diagnostic)
{
    public bool IsResolved =>
        Outcome == SocketEffectResolutionOutcome.Resolved ||
        Outcome == SocketEffectResolutionOutcome.NoInstalledCrystals;
}

/// <summary>
/// Pure, deterministic aggregation for installed crystal effects. It does not inspect or
/// mutate ItemDrop/shared prefab state. The caller supplies an already-classified equipment
/// category and the validated per-item SocketState.
/// </summary>
public sealed class SocketEffectResolver
{
    private readonly IReadOnlyDictionary<EffectKey, SocketEffectDefinition> _definitions;

    public SocketEffectResolver(IEnumerable<SocketEffectDefinition> definitions)
    {
        if (definitions is null) throw new ArgumentNullException(nameof(definitions));

        var map = new Dictionary<EffectKey, SocketEffectDefinition>();
        foreach (var definition in definitions)
        {
            if (definition is null)
                throw new InvalidOperationException("Socket effect definition cannot be null.");
            definition.Validate();

            var key = new EffectKey(definition.Element, definition.Tier, definition.Category);
            if (!map.TryAdd(key, definition))
            {
                throw new InvalidOperationException(
                    $"Duplicate socket effect definition for {definition.Element}/{definition.Tier}/{definition.Category}.");
            }
        }

        _definitions = map;
    }

    public SocketEffectResolution Resolve(EquipmentCategory category, SocketState state)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (!Enum.IsDefined(typeof(EquipmentCategory), category) || category == EquipmentCategory.Unknown)
        {
            return new SocketEffectResolution(
                SocketEffectResolutionOutcome.UnknownEquipmentCategory,
                Array.Empty<ResolvedSocketEffect>(),
                "Unknown equipment categories remain untouched until explicitly classified or included by compatibility policy.");
        }

        if (state.InstalledCrystals.Count == 0)
        {
            return new SocketEffectResolution(
                SocketEffectResolutionOutcome.NoInstalledCrystals,
                Array.Empty<ResolvedSocketEffect>(),
                "Item has no installed crystals.");
        }

        var totals = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var crystal in state.InstalledCrystals)
        {
            var key = new EffectKey(crystal.Element, crystal.Tier, category);
            if (!_definitions.TryGetValue(key, out var definition))
            {
                return new SocketEffectResolution(
                    SocketEffectResolutionOutcome.MissingDefinition,
                    Array.Empty<ResolvedSocketEffect>(),
                    $"No socket effect definition exists for {crystal.Element}/{crystal.Tier}/{category}; effects fail closed instead of guessing.");
            }

            foreach (var modifier in definition.Modifiers)
            {
                totals.TryGetValue(modifier.EffectId, out var current);
                var next = current + modifier.Value;
                if (double.IsNaN(next) || double.IsInfinity(next))
                {
                    return new SocketEffectResolution(
                        SocketEffectResolutionOutcome.InvalidDefinitionSet,
                        Array.Empty<ResolvedSocketEffect>(),
                        $"Aggregated socket effect '{modifier.EffectId}' overflowed to a non-finite value.");
                }
                totals[modifier.EffectId] = next;
            }
        }

        var resolved = totals
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new ResolvedSocketEffect(pair.Key, pair.Value))
            .ToArray();

        return new SocketEffectResolution(
            SocketEffectResolutionOutcome.Resolved,
            resolved,
            $"Resolved {state.InstalledCrystals.Count} installed crystal(s) into {resolved.Length} effect channel(s) for {category} equipment.");
    }

    private readonly record struct EffectKey(
        ElementalAlignment Element,
        CrystalTier Tier,
        EquipmentCategory Category);
}
