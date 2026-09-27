using System;
using BepInEx.Logging;
using HarmonyLib;

namespace Magenheim.Runtime;

/// <summary>
/// Final validation boundary after Jotunn has populated an Underworld ZoneSystem.
/// The host copies Surface configuration before the second ZoneSystem starts; Jotunn then
/// legitimately injects its CustomVegetation catalog into that second system. Because the copied
/// list can already contain those same object instances, remove only reference-identical duplicates.
/// Also fail closed if another mod claims any of Magenheim's six reserved Underworld biome bits.
/// </summary>
internal static class UnderworldZoneCatalogGuard
{
    private const string OwnedPrefix = "Magenheim_Underworld_";
    private static UnderworldRuntimeServices? _services;
    private static ManualLogSource? _log;

    internal static void Configure(UnderworldRuntimeServices services, ManualLogSource log)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    internal static void Validate(ZoneSystem zoneSystem)
    {
        var services = _services;
        if (services is null || !zoneSystem ||
            !services.WorldInstances.TryGetContextForScene(zoneSystem.gameObject.scene.handle, out var context) ||
            context is null || !context.InstanceId.IsUnderworld)
            return;

        var removed = RemoveReferenceDuplicateVegetation(zoneSystem);
        ValidateVegetationOwnership(zoneSystem);
        ValidateLocationOwnership(zoneSystem);

        if (removed > 0)
            _log?.LogInfo($"Removed {removed} reference-duplicate vegetation entries from the native Underworld ZoneSystem after Jotunn registration.");
    }

    private static int RemoveReferenceDuplicateVegetation(ZoneSystem zoneSystem)
    {
        var vegetation = zoneSystem.m_vegetation;
        var removed = 0;
        for (var i = vegetation.Count - 1; i >= 0; i--)
        {
            var candidate = vegetation[i];
            for (var j = 0; j < i; j++)
            {
                if (!ReferenceEquals(vegetation[j], candidate)) continue;
                vegetation.RemoveAt(i);
                removed++;
                break;
            }
        }
        return removed;
    }

    private static void ValidateVegetationOwnership(ZoneSystem zoneSystem)
    {
        foreach (var vegetation in zoneSystem.m_vegetation)
        {
            if (vegetation is null || !TouchesUnderworldBiome(vegetation.m_biome)) continue;
            var name = vegetation.m_prefab ? vegetation.m_prefab.name : string.Empty;
            if (IsMagenheimOwned(name)) continue;
            throw new InvalidOperationException(
                $"Underworld biome-flag collision: vegetation '{Display(name)}' claims Magenheim biome mask {(int)vegetation.m_biome}. " +
                "The Underworld instance will not start with ambiguous foreign biome ownership.");
        }
    }

    private static void ValidateLocationOwnership(ZoneSystem zoneSystem)
    {
        foreach (var location in zoneSystem.m_locations)
        {
            if (location is null || !TouchesUnderworldBiome(location.m_biome)) continue;
            var name = location.m_prefab.Name ?? string.Empty;
            if (IsMagenheimOwned(name)) continue;
            throw new InvalidOperationException(
                $"Underworld biome-flag collision: location '{Display(name)}' claims Magenheim biome mask {(int)location.m_biome}. " +
                "The Underworld instance will not start with ambiguous foreign biome ownership.");
        }
    }

    private static bool TouchesUnderworldBiome(Heightmap.Biome biome)
    {
        var mask =
            UnderworldTerrainRuntime.FungalForestBiome |
            UnderworldTerrainRuntime.BlackwaterDeepBiome |
            UnderworldTerrainRuntime.SulfurousWastesBiome |
            UnderworldTerrainRuntime.FrozenCavernsBiome |
            UnderworldTerrainRuntime.FractureZonesBiome |
            UnderworldTerrainRuntime.GreatDecayBiome;
        return (biome & mask) != Heightmap.Biome.None;
    }

    private static bool IsMagenheimOwned(string name) =>
        !string.IsNullOrWhiteSpace(name) &&
        name.StartsWith(OwnedPrefix, StringComparison.Ordinal);

    private static string Display(string name) =>
        string.IsNullOrWhiteSpace(name) ? "<unnamed>" : name;
}

[HarmonyPatch(typeof(ZoneSystem), "SetupLocations")]
internal static class UnderworldZoneCatalogGuardPatch
{
    [HarmonyPriority(Priority.Last)]
    private static void Postfix(ZoneSystem __instance) =>
        UnderworldZoneCatalogGuard.Validate(__instance);
}
