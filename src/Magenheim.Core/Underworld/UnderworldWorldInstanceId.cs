using System;

namespace Magenheim.Core.Underworld;

/// <summary>
/// Stable world-instance discriminator inside one Magenheim/Valheim save.
/// This is identity, never a coordinate transform.
/// </summary>
public readonly struct UnderworldWorldInstanceId : IEquatable<UnderworldWorldInstanceId>
{
    public static readonly UnderworldWorldInstanceId Surface = new(0);
    public static readonly UnderworldWorldInstanceId Underworld = new(1);

    public UnderworldWorldInstanceId(int value)
    {
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        Value = value;
    }

    public int Value { get; }

    public bool IsSurface => Value == Surface.Value;
    public bool IsUnderworld => Value == Underworld.Value;

    public bool Equals(UnderworldWorldInstanceId other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is UnderworldWorldInstanceId other && Equals(other);
    public override int GetHashCode() => Value;
    public override string ToString() => Value.ToString();

    public static bool operator ==(UnderworldWorldInstanceId left, UnderworldWorldInstanceId right) => left.Equals(right);
    public static bool operator !=(UnderworldWorldInstanceId left, UnderworldWorldInstanceId right) => !left.Equals(right);
}
