using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace Magenheim.Runtime;

/// <summary>
/// ZoneSystem.OnDestroy unconditionally nulls Valheim's process-static singleton. Unity Destroy is
/// deferred, so instance 1 can reach OnDestroy after the world-instance registry has already been
/// reset. Keep an explicit component-to-Surface pairing solely for this teardown seam and restore
/// the still-live Surface singleton after the Underworld ZoneSystem releases its native prefab refs.
/// </summary>
internal static class UnderworldZoneSystemLifetimeGuard
{
    private static readonly object Sync = new();
    private static readonly Dictionary<int, ZoneSystem> SurfaceByUnderworld = new();
    private static readonly FieldInfo ZoneInstance =
        AccessTools.Field(typeof(ZoneSystem), "s_instance")
        ?? throw new MissingFieldException(typeof(ZoneSystem).FullName, "s_instance");

    internal static void Register(ZoneSystem underworld, ZoneSystem surface)
    {
        if (!underworld) throw new ArgumentNullException(nameof(underworld));
        if (!surface) throw new ArgumentNullException(nameof(surface));
        lock (Sync)
            SurfaceByUnderworld[underworld.GetInstanceID()] = surface;
    }

    internal static bool TryCaptureSurface(ZoneSystem underworld, out ZoneSystem? surface)
    {
        lock (Sync)
        {
            if (SurfaceByUnderworld.TryGetValue(underworld.GetInstanceID(), out var candidate) &&
                candidate)
            {
                surface = candidate;
                return true;
            }
        }

        surface = null;
        return false;
    }

    internal static void RestoreAndForget(ZoneSystem underworld, ZoneSystem? surface)
    {
        lock (Sync)
            SurfaceByUnderworld.Remove(underworld.GetInstanceID());

        if (!surface) return;
        ZoneInstance.SetValue(null, surface);
    }
}

[HarmonyPatch]
internal static class UnderworldZoneSystemDestroySingletonPatch
{
    internal static MethodBase TargetMethod() =>
        typeof(ZoneSystem).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, Type.EmptyTypes, null)
        ?? throw new MissingMethodException(typeof(ZoneSystem).FullName, "OnDestroy()");

    private static void Prefix(ZoneSystem __instance, out ZoneSystem? __state)
    {
        __state = null;
        if (!__instance) return;
        UnderworldZoneSystemLifetimeGuard.TryCaptureSurface(__instance, out __state);
    }

    private static void Postfix(ZoneSystem __instance, ZoneSystem? __state)
    {
        if (__state is null) return;
        UnderworldZoneSystemLifetimeGuard.RestoreAndForget(__instance, __state);
    }
}
