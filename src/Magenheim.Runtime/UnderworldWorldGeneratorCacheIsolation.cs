using System;
using System.Collections;
using System.Reflection;
using System.Threading;
using HarmonyLib;

namespace Magenheim.Runtime;

/// <summary>
/// Valheim 1.0 keeps the Vector2s biome and biome-area memo tables as process-global static
/// dictionaries. A second WorldGenerator constructor clears those tables, and two live generators
/// would otherwise populate the same memo with different worlds. Instance 1 therefore bypasses the
/// static memo entirely while Surface continues to use vanilla caching.
/// </summary>
internal static class UnderworldWorldGeneratorCacheIsolation
{
    internal static readonly object SyncRoot = new();

    internal static bool TryGetBiome(
        WorldGenerator generator,
        Vector2s point,
        out Heightmap.Biome biome)
    {
        biome = Heightmap.Biome.None;
        if (!UnderworldTerrainRuntime.TrySampleNativeGenerator(generator, point.x, point.y, out var terrain))
            return false;
        biome = UnderworldTerrainRuntime.ToNativeBiome(terrain.Biome);
        return true;
    }

    internal static bool TryGetBiomeArea(
        WorldGenerator generator,
        Vector2s point,
        out Heightmap.BiomeArea area)
    {
        area = Heightmap.BiomeArea.Edge;
        if (!TryGetBiome(generator, point, out var center))
            return false;

        var offsets = new[]
        {
            new BiomeOffset(-64, -64),
            new BiomeOffset(64, -64),
            new BiomeOffset(64, 64),
            new BiomeOffset(-64, 64),
            new BiomeOffset(-64, 0),
            new BiomeOffset(64, 0),
            new BiomeOffset(0, -64),
            new BiomeOffset(0, 64),
        };

        foreach (var offset in offsets)
        {
            var sample = new Vector2s(point.x + offset.X, point.y + offset.Y);
            if (!TryGetBiome(generator, sample, out var neighbor) || neighbor != center)
            {
                area = Heightmap.BiomeArea.Edge;
                return true;
            }
        }

        area = Heightmap.BiomeArea.Median;
        return true;
    }

    internal static CacheSnapshot CaptureSurfaceCaches()
    {
        Monitor.Enter(SyncRoot);
        try
        {
            return new CacheSnapshot(
                SnapshotDictionary("s_cachedBiomeAreas"),
                SnapshotDictionary("s_cachedBiomes"));
        }
        catch
        {
            Monitor.Exit(SyncRoot);
            throw;
        }
    }

    internal static void RestoreSurfaceCaches(CacheSnapshot snapshot)
    {
        try
        {
            RestoreDictionary("s_cachedBiomeAreas", snapshot.BiomeAreas);
            RestoreDictionary("s_cachedBiomes", snapshot.Biomes);
        }
        finally
        {
            Monitor.Exit(SyncRoot);
        }
    }

    private static Entry[] SnapshotDictionary(string fieldName)
    {
        var field = AccessTools.Field(typeof(WorldGenerator), fieldName)
            ?? throw new MissingFieldException(typeof(WorldGenerator).FullName, fieldName);
        var dictionary = field.GetValue(null) as IDictionary
            ?? throw new InvalidOperationException($"WorldGenerator.{fieldName} is no longer an IDictionary.");

        var entries = new Entry[dictionary.Count];
        var index = 0;
        foreach (DictionaryEntry entry in dictionary)
            entries[index++] = new Entry(entry.Key, entry.Value);
        return entries;
    }

    private static void RestoreDictionary(string fieldName, Entry[] entries)
    {
        var field = AccessTools.Field(typeof(WorldGenerator), fieldName)
            ?? throw new MissingFieldException(typeof(WorldGenerator).FullName, fieldName);
        var dictionary = field.GetValue(null) as IDictionary
            ?? throw new InvalidOperationException($"WorldGenerator.{fieldName} is no longer an IDictionary.");

        dictionary.Clear();
        foreach (var entry in entries)
            dictionary.Add(entry.Key, entry.Value);
    }

    private readonly struct BiomeOffset
    {
        internal BiomeOffset(short x, short y)
        {
            X = x;
            Y = y;
        }

        internal short X { get; }
        internal short Y { get; }
    }

    internal readonly record struct CacheSnapshot(Entry[] BiomeAreas, Entry[] Biomes);
    internal readonly record struct Entry(object Key, object? Value);
}

[HarmonyPatch(
    typeof(WorldGenerator),
    nameof(WorldGenerator.GetBiomeArea),
    new[] { typeof(Vector2s) })]
internal static class UnderworldWorldGeneratorBiomeAreaCachePatch
{
    private static void Prefix(
        WorldGenerator __instance,
        Vector2s __0,
        out bool __state,
        ref bool __runOriginal,
        ref Heightmap.BiomeArea __result)
    {
        Monitor.Enter(UnderworldWorldGeneratorCacheIsolation.SyncRoot);
        __state = true;
        if (!UnderworldWorldGeneratorCacheIsolation.TryGetBiomeArea(__instance, __0, out var area))
            return;
        __result = area;
        __runOriginal = false;
    }

    private static void Finalizer(bool __state)
    {
        if (__state) Monitor.Exit(UnderworldWorldGeneratorCacheIsolation.SyncRoot);
    }
}

[HarmonyPatch]
internal static class UnderworldWorldGeneratorVectorBiomeCachePatch
{
    internal static MethodBase TargetMethod() =>
        AccessTools.Method(typeof(WorldGenerator), "GetBiome", new[] { typeof(Vector2s) })
        ?? throw new MissingMethodException(typeof(WorldGenerator).FullName, "GetBiome(Vector2s)");

    private static void Prefix(
        WorldGenerator __instance,
        Vector2s __0,
        out bool __state,
        ref bool __runOriginal,
        ref Heightmap.Biome __result)
    {
        Monitor.Enter(UnderworldWorldGeneratorCacheIsolation.SyncRoot);
        __state = true;
        if (!UnderworldWorldGeneratorCacheIsolation.TryGetBiome(__instance, __0, out var biome))
            return;
        __result = biome;
        __runOriginal = false;
    }

    private static void Finalizer(bool __state)
    {
        if (__state) Monitor.Exit(UnderworldWorldGeneratorCacheIsolation.SyncRoot);
    }
}
