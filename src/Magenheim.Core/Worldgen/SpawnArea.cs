using System;

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
}
