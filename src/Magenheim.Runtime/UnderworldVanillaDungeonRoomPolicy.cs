using System;

namespace Magenheim.Runtime;

internal enum UnderworldVanillaDungeonRiskBand
{
    Outer,
    Mid,
    Deep,
    Lair,
}

/// <summary>
/// Deterministic room-level pacing authority shared by dungeon ecology and rewards.
/// Decisions are based only on the donor room identity/metadata so every peer builds the
/// same private room prefab before world generation.
/// </summary>
internal static class UnderworldVanillaDungeonRoomPolicy
{
    internal static UnderworldVanillaDungeonRiskBand RiskFor(
        Room room,
        string dungeonId,
        string donorRoomName,
        int donorRoomIndex)
    {
        if (room is null) throw new ArgumentNullException(nameof(room));
        if (room.m_entrance) return UnderworldVanillaDungeonRiskBand.Outer;

        var score = 0;
        if (room.m_minPlaceOrder >= 3) score++;
        if (room.m_minPlaceOrder >= 7) score++;
        if (room.m_endCap) score++;
        if (room.m_divider && score > 0) score--;

        // Donor rooms frequently leave m_minPlaceOrder at zero. Stable variation prevents that
        // from collapsing an entire dungeon family into one pressure tier while still keeping the
        // result deterministic across host/client and save/reload.
        var variation = Unit(dungeonId, donorRoomName, donorRoomIndex, "risk");
        if (variation >= .82d) score++;
        else if (variation < .18d && score > 0) score--;

        return score switch
        {
            <= 0 => UnderworldVanillaDungeonRiskBand.Outer,
            1 => UnderworldVanillaDungeonRiskBand.Mid,
            2 => UnderworldVanillaDungeonRiskBand.Deep,
            _ => UnderworldVanillaDungeonRiskBand.Lair,
        };
    }

    internal static bool Roll(
        double probability,
        string dungeonId,
        string donorRoomName,
        int donorRoomIndex,
        string channel,
        int socketIndex) =>
        Unit(dungeonId, donorRoomName, donorRoomIndex, channel, socketIndex) <
        Clamp01(probability);

    private static double Clamp01(double value) =>
        value < 0d ? 0d : value > 1d ? 1d : value;

    internal static double Unit(params object[] values)
    {
        unchecked
        {
            uint hash = 2166136261u;
            foreach (var value in values)
            {
                var text = value?.ToString() ?? "<null>";
                for (var index = 0; index < text.Length; index++)
                {
                    hash ^= text[index];
                    hash *= 16777619u;
                }

                hash ^= 0xffu;
                hash *= 16777619u;
            }

            return hash / ((double)uint.MaxValue + 1d);
        }
    }
}
