using System;
using System.Globalization;

namespace Magenheim.Core.Underworld;

/// <summary>
/// Canonical deterministic placement identity for repeatable native Underworld structures.
/// The key is derived only from instance-local chunk coordinates plus a deterministic slot within
/// that chunk; it deliberately contains no Surface-world coordinates or runtime object identity.
/// </summary>
public static class UnderworldStructurePlacementKey
{
    public static string For(UnderworldInstanceChunkKey chunk, int slot)
    {
        if (slot < 0) throw new ArgumentOutOfRangeException(nameof(slot));
        return string.Concat(
            "chunk:", chunk.X.ToString(CultureInfo.InvariantCulture), ",",
            chunk.Z.ToString(CultureInfo.InvariantCulture), "/slot:",
            slot.ToString(CultureInfo.InvariantCulture));
    }
}
