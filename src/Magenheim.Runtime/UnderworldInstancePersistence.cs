using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using BepInEx.Logging;
using HarmonyLib;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>
/// Persists instance 1 with Valheim 1.0's native chunk codec inside the parent world's save
/// directory. This never creates a second World/save entry: the Underworld is a namespaced child
/// of the already-selected parent save.
/// </summary>
internal static class UnderworldInstancePersistence
{
    private const string InstanceDirectoryName = "magenheim_instances";
    private const string UnderworldDirectoryName = "1";

    private static readonly AsyncLocal<int> Reentry = new();
    private static UnderworldRuntimeServices? _services;
    private static ManualLogSource? _log;
    private static PendingLoad? _pendingLoad;

    internal static void Configure(UnderworldRuntimeServices services, ManualLogSource log)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    internal static void Reset()
    {
        _pendingLoad = null;
    }

    internal static void TryLoadBoundInstance()
    {
        var pending = _pendingLoad;
        var services = _services;
        if (pending is null || services is null ||
            !services.WorldInstances.TryGetContext(UnderworldWorldInstanceId.Underworld, out var context) ||
            context is null)
            return;

        _pendingLoad = null;
        var path = InstancePath(pending.ParentPath);
        if (!Directory.Exists(path))
        {
            _log?.LogInfo($"No persisted Underworld instance exists yet at '{path}'; starting a new instance namespace.");
            return;
        }

        InvokeInstance(context, pending.Method, ReplacePath(pending.Arguments, path));
        _log?.LogInfo($"Loaded Underworld instance ZDO chunks from parent-save namespace '{path}'.");
    }

    internal static void ObserveLoad(ZDOMan source, MethodBase original, object[] args)
    {
        if (Reentry.Value != 0 || args.Length == 0 || args[0] is not string parentPath) return;
        var services = _services;

        // The first parent-world LoadChunks happens before Magenheim's world-session lifecycle can
        // bind the Surface context. ZDOMan.instance is therefore the authoritative identification.
        if (ZDOMan.instance is not null && !ReferenceEquals(source, ZDOMan.instance)) return;

        _pendingLoad = new PendingLoad(
            (MethodInfo)original,
            parentPath,
            (object[])args.Clone());

        if (services is not null && services.WorldInstances.HasUnderworldContext)
            TryLoadBoundInstance();
    }

    internal static void MirrorPrepareSave(ZDOMan source, MethodBase original, object[] args)
    {
        if (!IsSurfaceInvocation(source)) return;
        if (!TryGetUnderworld(out var context)) return;
        InvokeInstance(context!, (MethodInfo)original, (object[])args.Clone());
    }

    internal static void MirrorSaveChunks(ZDOMan source, MethodBase original, object[] args)
    {
        if (!IsSurfaceInvocation(source) || args.Length == 0 || args[0] is not string parentPath) return;
        if (!TryGetUnderworld(out var context)) return;

        var path = InstancePath(parentPath);
        Directory.CreateDirectory(path);
        InvokeInstance(context!, (MethodInfo)original, ReplacePath(args, path));
        _log?.LogDebug($"Saved Underworld instance ZDO chunks inside parent-save namespace '{path}'.");
    }

    internal static void MirrorSaveCleanup(ZDOMan source, MethodBase original, object[] args)
    {
        if (!IsSurfaceInvocation(source)) return;
        if (!TryGetUnderworld(out var context)) return;
        InvokeInstance(context!, (MethodInfo)original, (object[])args.Clone());
    }

    private static bool IsSurfaceInvocation(ZDOMan source)
    {
        if (Reentry.Value != 0) return false;
        var services = _services;
        if (services is null ||
            !services.WorldInstances.TryGetContext(UnderworldWorldInstanceId.Surface, out var surface) ||
            surface is null)
            return ReferenceEquals(source, ZDOMan.instance);
        return ReferenceEquals(source, surface.ZdoMan);
    }

    private static bool TryGetUnderworld(out ValheimWorldInstanceContext? context)
    {
        context = null;
        return _services is not null &&
               _services.WorldInstances.TryGetContext(UnderworldWorldInstanceId.Underworld, out context) &&
               context is not null;
    }

    private static object[] ReplacePath(object[] arguments, string path)
    {
        var copy = (object[])arguments.Clone();
        copy[0] = path;
        return copy;
    }

    private static string InstancePath(string parentPath)
    {
        if (string.IsNullOrWhiteSpace(parentPath))
            throw new InvalidOperationException("Valheim supplied an empty parent save path.");

        var parent = Path.GetFullPath(parentPath);
        var instance = Path.GetFullPath(Path.Combine(parent, InstanceDirectoryName, UnderworldDirectoryName));
        var prefix = parent.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!instance.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Underworld persistence escaped the parent world save directory.");
        return instance;
    }

    private static void InvokeInstance(
        ValheimWorldInstanceContext context,
        MethodInfo method,
        object[] arguments)
    {
        Reentry.Value++;
        try
        {
            using (ValheimWorldInstanceExecution.Enter(context))
                method.Invoke(context.ZdoMan, arguments);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            throw new InvalidOperationException(
                $"Native Valheim instance persistence call {method.Name} failed.",
                exception.InnerException);
        }
        finally
        {
            Reentry.Value--;
        }
    }

    private sealed record PendingLoad(MethodInfo Method, string ParentPath, object[] Arguments);
}

[HarmonyPatch]
internal static class UnderworldZdoLoadChunksPersistencePatch
{
    internal static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (var method in typeof(ZDOMan).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            if (method.Name == "LoadChunks" &&
                method.GetParameters().Length >= 1 &&
                method.GetParameters()[0].ParameterType == typeof(string))
                yield return method;
    }

    private static void Postfix(ZDOMan __instance, MethodBase __originalMethod, object[] __args) =>
        UnderworldInstancePersistence.ObserveLoad(__instance, __originalMethod, __args);
}

[HarmonyPatch]
internal static class UnderworldZdoPrepareSavePersistencePatch
{
    internal static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (var method in typeof(ZDOMan).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            if (method.Name == "PrepareSave")
                yield return method;
    }

    private static void Postfix(ZDOMan __instance, MethodBase __originalMethod, object[] __args) =>
        UnderworldInstancePersistence.MirrorPrepareSave(__instance, __originalMethod, __args);
}

[HarmonyPatch]
internal static class UnderworldZdoSaveChunksPersistencePatch
{
    internal static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (var method in typeof(ZDOMan).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            if (method.Name == "SaveChunks" &&
                method.GetParameters().Length >= 1 &&
                method.GetParameters()[0].ParameterType == typeof(string))
                yield return method;
    }

    private static void Postfix(ZDOMan __instance, MethodBase __originalMethod, object[] __args) =>
        UnderworldInstancePersistence.MirrorSaveChunks(__instance, __originalMethod, __args);
}

[HarmonyPatch]
internal static class UnderworldZdoSaveCleanupPersistencePatch
{
    internal static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (var method in typeof(ZDOMan).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            if (method.Name == "SaveCleanup")
                yield return method;
    }

    private static void Postfix(ZDOMan __instance, MethodBase __originalMethod, object[] __args) =>
        UnderworldInstancePersistence.MirrorSaveCleanup(__instance, __originalMethod, __args);
}
