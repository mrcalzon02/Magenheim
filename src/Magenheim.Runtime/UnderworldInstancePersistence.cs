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
    private static readonly AsyncLocal<int> CombinedExtraDataPrepare = new();
    private static readonly AsyncLocal<int> CombinedLookupReentry = new();
    private static readonly AsyncLocal<int> PreserveSharedStaticsDuringInstanceLoad = new();
    private static readonly AsyncLocal<int> PreserveSharedSaveSnapshotDuringInstanceCleanup = new();
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
            UnderworldZoneInstanceState.NotifyZdosLoaded();
            return;
        }

        InvokeInstanceLoad(context, pending.Method, ReplacePath(pending.Arguments, path));
        UnderworldZoneInstanceState.NotifyZdosLoaded();
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

        // Build the Underworld manager's own native save clone first. Both managers share Valheim's
        // static ZDOExtraData store (their ZDOIDs are globally distinct), so each native PrepareSave
        // snapshots the same scalar data. Connection hashes are different: Valheim regenerates them
        // by asking ZDOMan.instance whether both endpoints exist. A second per-instance prepare would
        // leave only the last manager's connection hashes in the global save snapshot.
        InvokeInstance(context!, (MethodInfo)original, (object[])args.Clone());
        PrepareCombinedExtraDataSnapshot();
    }

    internal static void MirrorSaveChunks(ZDOMan source, MethodBase original, object[] args)
    {
        if (!IsSurfaceInvocation(source) || args.Length == 0 || args[0] is not string parentPath) return;
        if (!TryGetUnderworld(out var context)) return;

        var path = InstancePath(parentPath);
        Directory.CreateDirectory(path);
        InvokeSnapshotIo(context!, (MethodInfo)original, ReplacePath(args, path));
        _log?.LogDebug($"Saved Underworld instance ZDO chunks inside parent-save namespace '{path}'.");
    }

    internal static void MirrorSaveCleanup(ZDOMan source, MethodBase original, object[] args)
    {
        if (!IsSurfaceInvocation(source)) return;
        if (!TryGetUnderworld(out var context)) return;

        // Surface and Underworld have both completed SaveChunks before Valheim reaches SaveCleanup.
        // Clean the detached manager first, but suppress only its process-global
        // ZDOExtraData.ClearSave. Surface's ordinary SaveCleanup then performs that global clear
        // exactly once after both managers' native per-save state has been released.
        PreserveSharedSaveSnapshotDuringInstanceCleanup.Value++;
        try
        {
            InvokeSnapshotIo(context!, (MethodInfo)original, (object[])args.Clone());
        }
        finally
        {
            PreserveSharedSaveSnapshotDuringInstanceCleanup.Value--;
        }
    }

    internal static bool AllowSharedSaveSnapshotClear() =>
        PreserveSharedSaveSnapshotDuringInstanceCleanup.Value == 0;

    private static void PrepareCombinedExtraDataSnapshot()
    {
        CombinedExtraDataPrepare.Value++;
        try
        {
            // During this single call only, the GetZDO postfix below lets Valheim's native
            // connection-hash regeneration see endpoints in either manager. The resulting static
            // snapshot remains keyed by the original globally unique ZDOIDs and is then consumed by
            // both managers' ordinary native chunk writers.
            ZDOExtraData.PrepareSave();
        }
        finally
        {
            CombinedExtraDataPrepare.Value--;
        }
    }

    internal static void ResolveCombinedExtraDataZdo(ZDOMan source, ZDOID id, ref ZDO? result)
    {
        if (result is not null || CombinedExtraDataPrepare.Value == 0 || CombinedLookupReentry.Value != 0)
            return;

        var services = _services;
        if (services is null) return;

        ValheimWorldInstanceContext? other = null;
        if (services.WorldInstances.TryGetContext(UnderworldWorldInstanceId.Surface, out var surface) &&
            surface is not null && ReferenceEquals(source, surface.ZdoMan))
        {
            services.WorldInstances.TryGetContext(UnderworldWorldInstanceId.Underworld, out other);
        }
        else if (services.WorldInstances.TryGetContext(UnderworldWorldInstanceId.Underworld, out var underworld) &&
                 underworld is not null && ReferenceEquals(source, underworld.ZdoMan))
        {
            services.WorldInstances.TryGetContext(UnderworldWorldInstanceId.Surface, out other);
        }

        if (other is null) return;
        try
        {
            CombinedLookupReentry.Value++;
            result = other.ZdoMan.GetZDO(id);
        }
        finally
        {
            CombinedLookupReentry.Value--;
        }
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

    internal static bool AllowSharedStaticReset() =>
        PreserveSharedStaticsDuringInstanceLoad.Value == 0;

    private static void InvokeInstanceLoad(
        ValheimWorldInstanceContext context,
        MethodInfo method,
        object[] arguments)
    {
        Reentry.Value++;
        PreserveSharedStaticsDuringInstanceLoad.Value++;
        try
        {
            // Surface and Underworld intentionally share Valheim's static ZDOExtraData/ZDOID support
            // stores, partitioned by globally distinct ZDOIDs. A child-instance load must add its
            // records to those stores; it must never run a session-wide Init/Reset that erases the
            // already-loaded Surface entries.
            using (ValheimWorldInstanceExecution.Enter(context))
                method.Invoke(context.ZdoMan, arguments);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            throw new InvalidOperationException(
                $"Native Valheim instance load call {method.Name} failed.",
                exception.InnerException);
        }
        finally
        {
            PreserveSharedStaticsDuringInstanceLoad.Value--;
            Reentry.Value--;
        }
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

    private static void InvokeSnapshotIo(
        ValheimWorldInstanceContext context,
        MethodInfo method,
        object[] arguments)
    {
        Reentry.Value++;
        try
        {
            // SaveChunks runs on Valheim's background save thread. The manager's PrepareSave clone
            // and the static ZDOExtraData save snapshot already exist, so chunk writing/cleanup are
            // pure persistence work against this detached manager. Do not enter the normal instance
            // execution scope here: that would call Unity SceneManager APIs and rewrite live
            // process-wide world singletons from a worker thread.
            method.Invoke(context.ZdoMan, arguments);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            throw new InvalidOperationException(
                $"Native Valheim instance snapshot I/O call {method.Name} failed.",
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

    private static void Prefix(ZDOMan __instance, MethodBase __originalMethod, object[] __args) =>
        UnderworldInstancePersistence.MirrorSaveCleanup(__instance, __originalMethod, __args);
}


/// <summary>
/// ZDOExtraData.PrepareSave regenerates connection hashes through ZDOMan.instance.GetZDO. During the
/// one combined snapshot pass, allow that lookup to fall through to the other native manager so
/// Surface and Underworld connections are both serialized. Normal gameplay lookups never use this.
/// </summary>
[HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.GetZDO), new[] { typeof(ZDOID) })]
internal static class UnderworldZdoCombinedSaveLookupPatch
{
    private static void Postfix(ZDOMan __instance, ZDOID __0, ref ZDO __result) =>
        UnderworldInstancePersistence.ResolveCombinedExtraDataZdo(__instance, __0, ref __result);
}


[HarmonyPatch(typeof(ZDOExtraData), nameof(ZDOExtraData.Init))]
internal static class UnderworldZdoExtraDataInitGuardPatch
{
    private static bool Prefix() => UnderworldInstancePersistence.AllowSharedStaticReset();
}

[HarmonyPatch(typeof(ZDOExtraData), nameof(ZDOExtraData.Reset))]
internal static class UnderworldZdoExtraDataResetGuardPatch
{
    private static bool Prefix() => UnderworldInstancePersistence.AllowSharedStaticReset();
}

[HarmonyPatch(typeof(ZDOID), nameof(ZDOID.Reset))]
internal static class UnderworldZdoIdResetGuardPatch
{
    private static bool Prefix() => UnderworldInstancePersistence.AllowSharedStaticReset();
}


/// <summary>
/// Instance 1 runs its native SaveCleanup immediately before Surface cleanup. Suppress only the
/// detached manager's process-global save-snapshot clear; Surface remains the one authority that
/// clears ZDOExtraData's shared prepared-save snapshot after both native managers are cleaned.
/// </summary>
[HarmonyPatch(typeof(ZDOExtraData), nameof(ZDOExtraData.ClearSave))]
internal static class UnderworldZdoExtraDataClearSaveGuardPatch
{
    private static bool Prefix() => UnderworldInstancePersistence.AllowSharedSaveSnapshotClear();
}
