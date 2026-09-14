using System;

namespace Magenheim.Core.DeepFractures;

internal sealed class DeterministicSequence
{
    private uint _state;

    public DeterministicSequence(int seed, uint streamSalt = 0u)
    {
        _state = unchecked((uint)seed) ^ 0x9E3779B9u ^ streamSalt;
        if (_state == 0u)
            _state = 0xA341316Cu;
    }

    public int NextInt(int exclusiveMaximum)
    {
        if (exclusiveMaximum <= 0)
            throw new ArgumentOutOfRangeException(nameof(exclusiveMaximum), "Exclusive maximum must be positive.");
        return (int)(NextUInt() % (uint)exclusiveMaximum);
    }

    public double NextDouble() => NextUInt() / ((double)uint.MaxValue + 1d);

    private uint NextUInt()
    {
        var value = _state;
        value ^= value << 13;
        value ^= value >> 17;
        value ^= value << 5;
        _state = value;
        return value;
    }
}
