using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Persists instance-1 ZoneSystem generated-zone/location state inside one reserved Underworld ZDO.
/// The ZDO itself is carried by the existing native child ZDO chunk codec, so no second World/save
/// entry and no parallel filesystem save format are introduced.
/// </summary>
internal static class UnderworldZoneInstanceState
{
    private const string MarkerKey = "magenheim_underworld_zone_state_marker_v1";
    private const string BlobKey = "magenheim_underworld_zone_state_blob_v1";
    private const string WorldVersionKey = "magenheim_underworld_zone_state_world_version_v1";

    private static readonly int MarkerHash = MarkerKey.GetStableHashCode();
    private static readonly FieldInfo ObjectsById =
        AccessTools.Field(typeof(ZDOMan), "m_objectsByID")
        ?? throw new MissingFieldException(typeof(ZDOMan).FullName, "m_objectsByID");
    private static readonly FieldInfo GlobalKeysField =
        AccessTools.Field(typeof(ZoneSystem), "m_globalKeys")
        ?? throw new MissingFieldException(typeof(ZoneSystem).FullName, "m_globalKeys");
    private static readonly FieldInfo GlobalKeyEnumsField =
        AccessTools.Field(typeof(ZoneSystem), "m_globalKeysEnums")
        ?? throw new MissingFieldException(typeof(ZoneSystem).FullName, "m_globalKeysEnums");
    private static readonly FieldInfo GlobalKeyValuesField =
        AccessTools.Field(typeof(ZoneSystem), "m_globalKeysValues")
        ?? throw new MissingFieldException(typeof(ZoneSystem).FullName, "m_globalKeysValues");
    private static readonly FieldInfo WorldVersionField =
        AccessTools.Field(typeof(World), "m_worldVersion")
        ?? throw new MissingFieldException(typeof(World).FullName, "m_worldVersion");

    private static UnderworldRuntimeServices? _services;
    private static ManualLogSource? _log;
    private static bool _zdosLoaded;
    private static bool _zoneReady;
    private static bool _loadApplied;

    internal static void Configure(UnderworldRuntimeServices services, ManualLogSource log)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        Reset();
    }

    internal static void Reset()
    {
        _zdosLoaded = false;
        _zoneReady = false;
        _loadApplied = false;
    }

    internal static void NotifyZdosLoaded()
    {
        _zdosLoaded = true;
        TryApplyLoadedState();
    }

    internal static void NotifyZoneReady(ZoneSystem zoneSystem)
    {
        if (!IsUnderworldZoneSystem(zoneSystem)) return;
        _zoneReady = true;
        TryApplyLoadedState();
    }

    internal static void CaptureBeforeZdoPrepare(ZDOMan source)
    {
        var services = _services;
        if (services is null || ZNet.instance is null || !ZNet.instance.IsServer()) return;

        if (!services.WorldInstances.TryGetContext(
                UnderworldWorldInstanceId.Surface,
                out var surface) ||
            surface is null ||
            !ReferenceEquals(source, surface.ZdoMan))
            return;

        if (!services.WorldInstances.TryGetContext(
                UnderworldWorldInstanceId.Underworld,
                out var underworld) ||
            underworld is null)
            return;

        using (ValheimWorldInstanceExecution.Enter(underworld))
        {
            underworld.ZoneSystem.PrepareSave();

            byte[] blob;
            using (var stream = new MemoryStream())
            {
                using var writer = new BinaryWriter(stream);
                underworld.ZoneSystem.Save(writer);
                writer.Flush();
                blob = stream.ToArray();
            }

            var metadata = FindMetadataZdo(underworld.ZdoMan);
            if (metadata is null)
            {
                metadata = underworld.ZdoMan.CreateNewZDO(Vector3.zero, 0);
                metadata.Persistent = true;
                metadata.Set(MarkerKey, true);
            }

            metadata.Set(BlobKey, blob);
            metadata.Set(
                WorldVersionKey,
                Convert.ToInt32(WorldVersionField.GetValue(underworld.World)));

            _log?.LogDebug(
                $"Prepared {blob.Length} bytes of native Underworld ZoneSystem state inside native chunk-indexed metadata ZDO {metadata.m_uid}.");
        }
    }

    internal static bool IsMetadataZdo(ZDO? zdo) =>
        zdo is not null && zdo.GetBool(MarkerHash, false);

    internal static bool AcceptMetadataWithoutPrefabWarning(
        ZDO zdo,
        List<ZDO> loaded)
    {
        if (!IsMetadataZdo(zdo)) return false;
        loaded.Add(zdo);
        return true;
    }

    private static void TryApplyLoadedState()
    {
        if (_loadApplied || !_zdosLoaded || !_zoneReady) return;

        var services = _services;
        if (services is null ||
            !services.WorldInstances.TryGetContext(
                UnderworldWorldInstanceId.Underworld,
                out var underworld) ||
            underworld is null ||
            !services.WorldInstances.TryGetContext(
                UnderworldWorldInstanceId.Surface,
                out var surface) ||
            surface is null)
            return;

        var metadata = FindMetadataZdo(underworld.ZdoMan);
        if (metadata is null)
        {
            _loadApplied = true;
            _log?.LogInfo(
                "No persisted Underworld ZoneSystem state exists yet; instance 1 will generate natively from its derived seed.");
            return;
        }

        var blob = metadata.GetByteArray(BlobKey);
        if (blob is null || blob.Length == 0)
            throw new InvalidOperationException(
                "Underworld ZoneSystem metadata exists without a native save blob.");

        var version = metadata.GetInt(
            WorldVersionKey,
            Convert.ToInt32(WorldVersionField.GetValue(surface.World)));
        var campaign = CaptureCampaignKeys(surface.ZoneSystem);

        try
        {
            using (ValheimWorldInstanceExecution.Enter(underworld))
            using (var stream = new MemoryStream(blob, writable: false))
            using (var reader = new BinaryReader(stream))
                underworld.ZoneSystem.Load(reader, (Version.World)version);
        }
        finally
        {
            // ZoneSystem.Load clears/rebuilds global keys because the vanilla DB stores them in the
            // same block as zone/location state. Instance 1 shares those collections with Surface,
            // so restore the already-loaded parent campaign keys immediately after the native decode.
            RestoreCampaignKeys(surface.ZoneSystem, campaign);
            using (ValheimWorldInstanceExecution.Enter(surface))
                surface.ZoneSystem.UpdateWorldRates();
        }

        _loadApplied = true;
        _log?.LogInfo(
            $"Restored native Underworld ZoneSystem generated-zone/location state ({blob.Length} bytes, world version {version}).");
    }

    private static ZDO? FindMetadataZdo(ZDOMan manager)
    {
        if (ObjectsById.GetValue(manager) is not IDictionary objects)
            throw new InvalidOperationException(
                "ZDOMan.m_objectsByID is not dictionary-compatible.");

        foreach (DictionaryEntry entry in objects)
            if (entry.Value is ZDO zdo && IsMetadataZdo(zdo))
                return zdo;
        return null;
    }

    private static CampaignKeySnapshot CaptureCampaignKeys(ZoneSystem surface)
    {
        if (GlobalKeysField.GetValue(surface) is not IEnumerable keys ||
            GlobalKeyEnumsField.GetValue(surface) is not IEnumerable enums ||
            GlobalKeyValuesField.GetValue(surface) is not IDictionary values)
            throw new InvalidOperationException(
                "Surface ZoneSystem campaign global-key collections are unavailable.");

        var keyCopy = new List<string>();
        foreach (var value in keys)
            if (value is string text) keyCopy.Add(text);

        var enumCopy = new List<GlobalKeys>();
        foreach (var value in enums)
            if (value is GlobalKeys key) enumCopy.Add(key);

        var valueCopy = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (DictionaryEntry entry in values)
            if (entry.Key is string key && entry.Value is string value)
                valueCopy[key] = value;

        return new CampaignKeySnapshot(keyCopy, enumCopy, valueCopy);
    }

    private static void RestoreCampaignKeys(
        ZoneSystem surface,
        CampaignKeySnapshot snapshot)
    {
        RestoreSet(GlobalKeysField.GetValue(surface), snapshot.Keys);
        RestoreSet(GlobalKeyEnumsField.GetValue(surface), snapshot.Enums);

        if (GlobalKeyValuesField.GetValue(surface) is not IDictionary values)
            throw new InvalidOperationException(
                "ZoneSystem.m_globalKeysValues is unavailable.");
        values.Clear();
        foreach (var pair in snapshot.Values)
            values.Add(pair.Key, pair.Value);
    }

    private static void RestoreSet<T>(object? target, IEnumerable<T> values)
    {
        if (target is null)
            throw new InvalidOperationException("Campaign key set is unavailable.");

        var clear = target.GetType().GetMethod("Clear", Type.EmptyTypes)
            ?? throw new MissingMethodException(target.GetType().FullName, "Clear()");
        var add = target.GetType().GetMethod("Add", new[] { typeof(T) })
            ?? throw new MissingMethodException(
                target.GetType().FullName,
                $"Add({typeof(T).Name})");

        clear.Invoke(target, Array.Empty<object>());
        foreach (var value in values)
            add.Invoke(target, new object?[] { value });
    }

    private static bool IsUnderworldZoneSystem(ZoneSystem zoneSystem)
    {
        var services = _services;
        return services is not null &&
               zoneSystem &&
               services.WorldInstances.TryGetContextForScene(
                   zoneSystem.gameObject.scene.handle,
                   out var context) &&
               context is not null &&
               context.InstanceId.IsUnderworld;
    }

    private sealed record CampaignKeySnapshot(
        List<string> Keys,
        List<GlobalKeys> Enums,
        Dictionary<string, string> Values);
}

[HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.PrepareSave))]
internal static class UnderworldZoneStateCapturePatch
{
    [HarmonyPriority(Priority.First)]
    private static void Prefix(ZDOMan __instance) =>
        UnderworldZoneInstanceState.CaptureBeforeZdoPrepare(__instance);
}

/// <summary>
/// Keep metadata in Valheim's native sector/chunk save index, but never instantiate it as content.
/// Native dirty-chunk selection, cloning and save mapping remain owned by ZDOMan.
/// </summary>
[HarmonyPatch(typeof(ZNetScene), "CreateObject", new[] { typeof(ZDO) })]
internal static class UnderworldZoneMetadataObjectPatch
{
    private static bool Prefix(ZDO __0, ref GameObject __result)
    {
        if (!UnderworldZoneInstanceState.IsMetadataZdo(__0)) return true;
        __result = null!;
        return false;
    }
}

/// <summary>
/// Prefab hash 0 is intentional for the non-rendered metadata record. Admit it through native load
/// without reporting it as a broken/unknown gameplay prefab.
/// </summary>
[HarmonyPatch]
internal static class UnderworldZoneMetadataLoadFilterPatch
{
    internal static MethodBase TargetMethod() =>
        typeof(ZDOMan).GetMethod(
            "FilterZDO",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null,
            new[]
            {
                typeof(bool),
                typeof(ZDO),
                typeof(List<ZDO>).MakeByRefType(),
                typeof(List<ZDO>).MakeByRefType(),
                typeof(List<ZDO>).MakeByRefType(),
            }, null)
        ?? throw new MissingMethodException(
            typeof(ZDOMan).FullName,
            "FilterZDO(bool,ZDO,ref List<ZDO>,ref List<ZDO>,ref List<ZDO>)");

    private static bool Prefix(object[] __args)
    {
        if (__args.Length < 3 ||
            __args[1] is not ZDO zdo ||
            __args[2] is not List<ZDO> loaded)
            return true;

        return !UnderworldZoneInstanceState.AcceptMetadataWithoutPrefabWarning(
            zdo,
            loaded);
    }
}

/// <summary>
/// The metadata ZDO exists only to ride the native instance-1 save codec. It is never a replicated
/// gameplay object.
/// </summary>
[HarmonyPatch]
internal static class UnderworldZoneMetadataShouldSendPatch
{
    internal static MethodBase TargetMethod()
    {
        var peerType = typeof(ZDOMan).GetNestedType("ZDOPeer", BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingMemberException(typeof(ZDOMan).FullName, "ZDOPeer");
        return peerType.GetMethod("ShouldSend", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new[] { typeof(ZDO) }, null)
            ?? throw new MissingMethodException(peerType.FullName, "ShouldSend(ZDO)");
    }

    private static bool Prefix(ZDO __0, ref bool __result)
    {
        if (!UnderworldZoneInstanceState.IsMetadataZdo(__0)) return true;
        __result = false;
        return false;
    }
}
