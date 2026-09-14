using System;
using System.Globalization;

namespace Magenheim.Core.Worldgen;

[Flags]
public enum SpawnArea
{
    None = 0,
    Median = 1 << 0,
    Edge = 1 << 1,
    All = Median | Edge,
}

public enum InvalidAreaBehavior
{
    Reject = 0,
    ClampKnownBits = 1,
    FallbackToAll = 2,
}

public readonly record struct AreaValidationResult(
    bool IsValid,
    SpawnArea Area,
    string Diagnostic)
{
    public static AreaValidationResult Valid(SpawnArea area) =>
        new(true, area, string.Empty);

    public static AreaValidationResult Invalid(string diagnostic) =>
        new(false, SpawnArea.None, diagnostic);
}

public static class SpawnAreaValidator
{
    private const SpawnArea KnownMask = SpawnArea.All;

    public static AreaValidationResult Normalize(
        SpawnArea requested,
        InvalidAreaBehavior behavior = InvalidAreaBehavior.Reject)
    {
        var raw = (int)requested;

        // Negative enum values are especially dangerous for flags because sign-extension can
        // make every known bit appear set. Never treat them as clampable area definitions.
        if (raw < 0)
        {
            return behavior == InvalidAreaBehavior.FallbackToAll
                ? new AreaValidationResult(
                    true,
                    SpawnArea.All,
                    $"Spawn area value {raw} was negative and was replaced by All by configured policy.")
                : AreaValidationResult.Invalid(
                    $"Invalid negative spawn area value {raw}. Allowed values are Median, Edge, or All.");
        }

        var unknownBits = raw & ~(int)KnownMask;
        var knownBits = requested & KnownMask;

        if (unknownBits == 0 && knownBits != SpawnArea.None)
        {
            return AreaValidationResult.Valid(knownBits);
        }

        return behavior switch
        {
            InvalidAreaBehavior.Reject => AreaValidationResult.Invalid(
                $"Invalid spawn area value {raw}. Allowed values are Median, Edge, or All."),

            InvalidAreaBehavior.ClampKnownBits when knownBits != SpawnArea.None =>
                new AreaValidationResult(
                    true,
                    knownBits,
                    $"Spawn area value {raw} contained unknown bits and was clamped to {knownBits}."),

            InvalidAreaBehavior.ClampKnownBits => AreaValidationResult.Invalid(
                $"Spawn area value {raw} contained no usable known area bits after clamping."),

            InvalidAreaBehavior.FallbackToAll => new AreaValidationResult(
                true,
                SpawnArea.All,
                $"Spawn area value {raw} was invalid and was replaced by All by configured policy."),

            _ => throw new ArgumentOutOfRangeException(nameof(behavior), behavior, null),
        };
    }

    public static AreaValidationResult ParseConfiguredArea(
        string? configuredValue,
        InvalidAreaBehavior behavior = InvalidAreaBehavior.Reject)
    {
        if (configuredValue is null || string.IsNullOrWhiteSpace(configuredValue))
        {
            return behavior == InvalidAreaBehavior.FallbackToAll
                ? new AreaValidationResult(
                    true,
                    SpawnArea.All,
                    "Spawn area configuration was empty and was replaced by All by configured policy.")
                : AreaValidationResult.Invalid("Spawn area configuration cannot be empty.");
        }

        var trimmed = configuredValue.Trim();
        if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericValue))
        {
            return Normalize((SpawnArea)numericValue, behavior);
        }

        var tokens = trimmed.Split(new[] { ',', '|', '+' }, StringSplitOptions.RemoveEmptyEntries);
        var parsedArea = SpawnArea.None;
        var unknownToken = false;

        foreach (var token in tokens)
        {
            switch (token.Trim().ToLowerInvariant())
            {
                case "median":
                    parsedArea |= SpawnArea.Median;
                    break;
                case "edge":
                    parsedArea |= SpawnArea.Edge;
                    break;
                case "all":
                case "everywhere":
                    parsedArea |= SpawnArea.All;
                    break;
                default:
                    unknownToken = true;
                    break;
            }
        }

        if (!unknownToken)
        {
            return Normalize(parsedArea, behavior);
        }

        if (behavior == InvalidAreaBehavior.ClampKnownBits && parsedArea != SpawnArea.None)
        {
            return new AreaValidationResult(
                true,
                parsedArea,
                $"Spawn area configuration '{configuredValue}' contained unknown tokens; recognized areas were retained as {parsedArea}.");
        }

        if (behavior == InvalidAreaBehavior.FallbackToAll)
        {
            return new AreaValidationResult(
                true,
                SpawnArea.All,
                $"Spawn area configuration '{configuredValue}' contained unknown tokens and was replaced by All by configured policy.");
        }

        return AreaValidationResult.Invalid(
            $"Spawn area configuration '{configuredValue}' contains unknown tokens. Allowed names are Median, Edge, All, or Everywhere.");
    }
}
