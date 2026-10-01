using System;
using BepInEx.Logging;
using HarmonyLib;

namespace Magenheim.Runtime;

/// <summary>
/// Partitions Jotunn's process-wide worldgen catalog after the native Underworld ZoneSystem has
/// received its detached copy, then validates the instance-local catalog before startup.
///
/// Underworld-only Magenheim rows must remain available to instance 1 but must not participate in
/// Surface location generation: current Valheim asks AltBiomeWorldData for candidate points by
/// biome-mask key, and the Surface table has no entries for Magenheim's reserved 1024+ biome bits.
///
/// Legacy/foreign negative biome masks are complement-style vanilla masks (for example -33 means
/// "all native flags except one"). Their sign extension makes every future/custom high bit appear
/// set even though the foreign content never explicitly claimed those bits. Such rows are removed
/// from the detached Underworld catalog rather than mutating the shared registration object.
/// Positive foreign masks that explicitly claim a reserved Underworld bit still fail closed.
/// </summary>
internal static class UnderworldZoneCatalogGuard
{
    private const string OwnedPrefix = "Magenheim_Underworld_";
    private const string OwnedGeodePrefix = "Magenheim_Geode_";
    private static UnderworldRuntimeServices? _services;
    private static ManualLogSource? _log;

    internal static void Configure(UnderworldRuntimeServices services, ManualLogSource log)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    internal static void DetachUnderworldOnlyGenerationFromSurface(
        ZoneSystem surface,
        ZoneSystem underworld)
    {
        if (!surface) throw new ArgumentNullException(nameof(surface));
        if (!underworld) throw new ArgumentNullException(nameof(underworld));
        if (ReferenceEquals(surface, underworld))
            throw new InvalidOperationException(
                "Surface and Underworld ZoneSystem catalogs cannot be partitioned on the same instance.");

        var locations = RemoveOwnedUnderworldLocationsFromSurface(surface, underworld);
        var vegetation = RemoveOwnedUnderworldVegetationFromSurface(surface, underworld);
        if (locations > 0 || vegetation > 0)
            _log?.LogInfo(
                $"Partitioned native worldgen catalogs after Underworld cloning: removed {locations} " +
                $"Underworld-only location rows and {vegetation} Underworld-only vegetation rows from Surface generation.");
    }

    internal static void Validate(ZoneSystem zoneSystem)
    {
        var services = _services;
        if (services is null || !zoneSystem ||
            !services.WorldInstances.TryGetContextForScene(zoneSystem.gameObject.scene.handle, out var context) ||
            context is null || !context.InstanceId.IsUnderworld)
            return;

        var duplicateVegetation = RemoveReferenceDuplicateVegetation(zoneSystem);
        var wildcardVegetation = RemoveForeignImplicitWildcardVegetation(zoneSystem);
        var wildcardLocations = RemoveForeignImplicitWildcardLocations(zoneSystem);
        ValidateVegetationOwnership(zoneSystem);
        ValidateLocationOwnership(zoneSystem);
        UnderworldWorldgenContentBridge.ValidateUnderworldCatalog(zoneSystem);

        if (duplicateVegetation > 0)
            _log?.LogInfo(
                $"Removed {duplicateVegetation} reference-duplicate vegetation entries from the native Underworld ZoneSystem after Jotunn registration.");
        if (wildcardVegetation > 0 || wildcardLocations > 0)
            _log?.LogInfo(
                $"Excluded {wildcardVegetation} foreign complement-mask vegetation rows and {wildcardLocations} " +
                "foreign complement-mask location rows from the detached Underworld catalog.");
    }

    private static int RemoveOwnedUnderworldLocationsFromSurface(ZoneSystem surface, ZoneSystem underworld)
    {
        var removed = 0;
        for (var i = surface.m_locations.Count - 1; i >= 0; i--)
        {
            var location = surface.m_locations[i];
            if (location is null || !TouchesUnderworldBiome(location.m_biome)) continue;
            var name = location.m_prefab.Name ?? string.Empty;
            if (!IsMagenheimOwned(name)) continue;
            if (!ContainsLocationReference(underworld, location))
                throw new InvalidOperationException(
                    $"Cannot partition Underworld-only Surface location '{Display(name)}': the detached Underworld catalog did not receive it.");
            surface.m_locations.RemoveAt(i);
            removed++;
        }
        return removed;
    }

    private static int RemoveOwnedUnderworldVegetationFromSurface(ZoneSystem surface, ZoneSystem underworld)
    {
        var removed = 0;
        for (var i = surface.m_vegetation.Count - 1; i >= 0; i--)
        {
            var vegetation = surface.m_vegetation[i];
            if (vegetation is null || !TouchesUnderworldBiome(vegetation.m_biome)) continue;
            var name = vegetation.m_prefab ? vegetation.m_prefab.name : string.Empty;
            if (!IsMagenheimOwned(name)) continue;
            if (!ContainsVegetationReference(underworld, vegetation))
                throw new InvalidOperationException(
                    $"Cannot partition Underworld-only Surface vegetation '{Display(name)}': the detached Underworld catalog did not receive it.");
            surface.m_vegetation.RemoveAt(i);
            removed++;
        }
        return removed;
    }

    private static bool ContainsLocationReference(ZoneSystem zoneSystem, object candidate)
    {
        foreach (var value in zoneSystem.m_locations)
            if (ReferenceEquals(value, candidate))
                return true;
        return false;
    }

    private static bool ContainsVegetationReference(ZoneSystem zoneSystem, object candidate)
    {
        foreach (var value in zoneSystem.m_vegetation)
            if (ReferenceEquals(value, candidate))
                return true;
        return false;
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

    private static int RemoveForeignImplicitWildcardVegetation(ZoneSystem zoneSystem)
    {
        var removed = 0;
        for (var i = zoneSystem.m_vegetation.Count - 1; i >= 0; i--)
        {
            var vegetation = zoneSystem.m_vegetation[i];
            if (vegetation is null) continue;
            var rawMask = (int)vegetation.m_biome;
            if (rawMask >= 0) continue;
            var name = vegetation.m_prefab ? vegetation.m_prefab.name : string.Empty;
            if (IsMagenheimOwned(name)) continue;
            zoneSystem.m_vegetation.RemoveAt(i);
            removed++;
        }
        return removed;
    }

    private static int RemoveForeignImplicitWildcardLocations(ZoneSystem zoneSystem)
    {
        var removed = 0;
        for (var i = zoneSystem.m_locations.Count - 1; i >= 0; i--)
        {
            var location = zoneSystem.m_locations[i];
            if (location is null) continue;
            var rawMask = (int)location.m_biome;
            if (rawMask >= 0) continue;
            var name = location.m_prefab.Name ?? string.Empty;
            if (IsMagenheimOwned(name)) continue;
            zoneSystem.m_locations.RemoveAt(i);
            removed++;
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
                $"Underworld biome-flag collision: vegetation '{Display(name)}' explicitly claims Magenheim biome mask {(int)vegetation.m_biome}. " +
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
                $"Underworld biome-flag collision: location '{Display(name)}' explicitly claims Magenheim biome mask {(int)location.m_biome}. " +
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
        (name.StartsWith(OwnedPrefix, StringComparison.Ordinal) ||
         name.StartsWith(OwnedGeodePrefix, StringComparison.Ordinal) ||
         string.Equals(name, Magenheim.Core.DarkThrone.DarkThroneLocationCatalog.DarkThrone.PrefabName, StringComparison.Ordinal) ||
         string.Equals(name, NowhereKingBossStoneRegistrar.LocationPrefabName, StringComparison.Ordinal));

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
