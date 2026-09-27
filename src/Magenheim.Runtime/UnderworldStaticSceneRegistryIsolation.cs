using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Gives Valheim's static coordinate registries an instance-local view for the duration of their
/// native range/access queries. The original list object remains authoritative and is restored in
/// the Harmony finalizer; Magenheim does not maintain a duplicate registry.
/// </summary>
internal static class UnderworldStaticSceneRegistryIsolation
{
    internal static IDisposable? Enter(Type ownerType, params string[] fieldNames)
    {
        if (!ValheimWorldInstanceExecution.TryGetQueryScene(out var scene))
            return null;

        FieldInfo? field = null;
        foreach (var name in fieldNames)
        {
            field = AccessTools.Field(ownerType, name);
            if (field is not null && field.IsStatic) break;
            field = null;
        }
        if (field is null)
            throw new MissingFieldException(ownerType.FullName, string.Join("/", fieldNames));

        if (field.GetValue(null) is not IList list)
            throw new InvalidOperationException(
                $"{ownerType.Name}.{field.Name} is no longer an IList-compatible static registry.");

        return new Scope(list, scene.handle);
    }

    private sealed class Scope : IDisposable
    {
        private readonly IList _list;
        private readonly List<Removed> _removed = new();
        private bool _disposed;

        internal Scope(IList list, int sceneHandle)
        {
            _list = list;
            for (var i = list.Count - 1; i >= 0; i--)
            {
                var value = list[i];
                if (value is not Component component || !component) continue;
                if (component.gameObject.scene.handle == sceneHandle) continue;
                _removed.Add(new Removed(i, value));
                list.RemoveAt(i);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            for (var i = _removed.Count - 1; i >= 0; i--)
            {
                var removed = _removed[i];
                var index = Math.Min(removed.Index, _list.Count);
                _list.Insert(index, removed.Value);
            }
            _removed.Clear();
        }

        private sealed record Removed(int Index, object Value);
    }
}

[HarmonyPatch]
internal static class UnderworldPrivateAreaRegistryScopePatch
{
    internal static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (var method in typeof(PrivateArea).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            if (method.Name == "CheckAccess")
                yield return method;
    }

    private static void Prefix(out IDisposable? __state) =>
        __state = UnderworldStaticSceneRegistryIsolation.Enter(typeof(PrivateArea), "m_allAreas");

    private static void Finalizer(IDisposable? __state) => __state?.Dispose();
}

[HarmonyPatch]
internal static class UnderworldPieceRegistryScopePatch
{
    internal static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (var method in typeof(Piece).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            if (method.Name == "GetAllPiecesInRadius")
                yield return method;
    }

    private static void Prefix(out IDisposable? __state) =>
        __state = UnderworldStaticSceneRegistryIsolation.Enter(typeof(Piece), "s_allPieces", "m_allPieces");

    private static void Finalizer(IDisposable? __state) => __state?.Dispose();
}

[HarmonyPatch]
internal static class UnderworldCraftingStationRegistryScopePatch
{
    private static readonly HashSet<string> Names = new(StringComparer.Ordinal)
    {
        "HaveBuildStationInRange",
        "FindClosestStationInRange",
        "GetCraftingStation",
    };

    internal static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (var method in typeof(CraftingStation).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            if (Names.Contains(method.Name))
                yield return method;
    }

    private static void Prefix(out IDisposable? __state) =>
        __state = UnderworldStaticSceneRegistryIsolation.Enter(typeof(CraftingStation), "m_allStations");

    private static void Finalizer(IDisposable? __state) => __state?.Dispose();
}

[HarmonyPatch]
internal static class UnderworldStationExtensionRegistryScopePatch
{
    private static readonly HashSet<string> Names = new(StringComparer.Ordinal)
    {
        "FindExtensions",
        "FindClosestStationInRange",
        "OtherExtensionInRange",
    };

    internal static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (var method in typeof(StationExtension).GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            if (Names.Contains(method.Name))
                yield return method;
    }

    private static void Prefix(out IDisposable? __state) =>
        __state = UnderworldStaticSceneRegistryIsolation.Enter(typeof(StationExtension), "m_allExtensions");

    private static void Finalizer(IDisposable? __state) => __state?.Dispose();
}
