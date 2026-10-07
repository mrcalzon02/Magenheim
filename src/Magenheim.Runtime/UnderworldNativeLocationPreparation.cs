using System;
using System.Collections.Generic;
using System.Reflection;

namespace Magenheim.Runtime;

internal static class UnderworldNativeLocationPreparation
{
    private const int UnderworldMask = 1024 | 2048 | 4096 | 8192 | 16384 | 32768;
    private static readonly MethodInfo Clone = typeof(object).GetMethod("MemberwiseClone",
        BindingFlags.Instance | BindingFlags.NonPublic)!;

    internal static int Prepare(List<ZoneSystem.ZoneLocation> locations)
    {
        var removed = 0;
        for (var i = locations.Count - 1; i >= 0; i--)
        {
            var entry = locations[i];
            if (entry == null || (int)entry.m_biome < 0 || ((int)entry.m_biome & UnderworldMask) == 0)
            {
                locations.RemoveAt(i);
                removed++;
                continue;
            }
            // Surface AltBiomeWorldData does not describe the separately generated Underworld.
            // The native center-first sampler uses GetRandomZone + WorldGenerator biome checks
            // instead of dereferencing that absent point index. Retain all native admission rules.
            // Never change the shared Jotunn/Surface catalog row.
            var detached = (ZoneSystem.ZoneLocation)Clone.Invoke(entry, Array.Empty<object>())!;
            detached.m_centerFirst = true;
            locations[i] = detached;
        }
        return removed;
    }
}
